namespace Story65.ReceiverPhase;
using System.Diagnostics;
internal static class FocusedDriver
{
    internal static async Task<object> RunAsync(string mode,string group,RuntimeInputs inputs,FixtureAdapter api)
    {
        Check.Require(CaseGroups.All.TryGetValue(group,out string[]? names),"closed-case-group");
        var handles=new List<object>();
        try
        {
            foreach(string name in names!) handles.Add(await api.CreateAsync(name).ConfigureAwait(false));
            inputs.VerifyLoaded(); // All setup/fingerprint/startup and identity checks finish before READY.
            CounterControl? counters=inputs.Variant=="private"?new(inputs):null;
            if(counters is null){Protocol.Ready();await Protocol.ExpectAsync("GO").ConfigureAwait(false);}
            else {await counters.ReadyAsync(mode).ConfigureAwait(false);if(mode=="pre-capped")await counters.FloodAsync().ConfigureAwait(false);}
            var observations=new List<object>();
            for(int index=0;index<names!.Length;index++)
            {
                bool recording=counters is not null && mode!="disabled";
                if(recording)counters!.Open();
                long admitted=counters?.Read("Admitted")??0,ordinal=recording?counters!.Read("Ordinal")+1:0;
                CaseResult result=CaseResult.Read(await api.ExecuteAsync(handles[index]).ConfigureAwait(false));
                Check.Require(result.Case==names[index],"exact-case-returned");
                long delta=(counters?.Read("Admitted")??0)-admitted;
                Check.Require(delta==(recording?1:0),"exact-per-case-private-admission");
                if(recording){Check.Require(counters!.Read("Ordinal")==ordinal,"one-own-local-ordinal");counters.Open();}
                observations.Add(new {Result=result,CaptureOrdinal=ordinal,AdmittedDelta=delta,
                    ActualStageRowsRequired=recording && mode=="active",SyntheticDurationBracketOnly=result.Case.StartsWith("technical-double-dispose-",StringComparison.Ordinal)});
            }
            inputs.VerifyLoaded();
            return new {Mode=mode,Group=group,Passed=true,Cases=observations,AllSetupBeforeReady=true,
                RawBusinessValuesPersisted=false,ActualCounterCountsAndAllocationsZero=true};
        }
        finally {for(int i=handles.Count-1;i>=0;i--)await api.DisposeAsync(handles[i]).ConfigureAwait(false);}
    }
}
