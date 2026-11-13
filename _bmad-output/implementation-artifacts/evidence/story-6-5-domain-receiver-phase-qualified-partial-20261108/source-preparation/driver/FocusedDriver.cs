namespace Story65.DomainReceiverPhase;
internal static class FocusedDriver
{
    internal static async Task<object> RunAsync(string mode,string group,RuntimeInputs inputs,FixtureAdapter api)
    {
        Check.Require(CaseGroups.All.TryGetValue(group,out string[]? names),"closed-case-group");
        var handles=new List<object>();
        CounterControl? counters=null;
        bool recording=false,executing=false;
        long admitted=0,completed=0,active=0;
        string currentCase=string.Empty;
        int handoffChecks=0;
        Action<int> probe=code=>
        {
            Check.Require(executing && code is >=0 and <=6,"probe-only-during-actual-execution");
            if(code!=2 || !recording || !CaseGroups.Expected[currentCase].OwnsDeferredResult)return;
            int count=CaseGroups.Expected[currentCase].Entries;
            Check.Require(counters!.Read("Admitted")==admitted+count && counters.Read("Completed")==completed
                && counters.Read("Active")==active+count,"all-transferred-captures-still-owned-at-filter-return");
            handoffChecks++;
        };
        try
        {
            foreach(string name in names!)handles.Add(await api.CreateAsync(name,probe).ConfigureAwait(false));
            inputs.VerifyLoaded(); // All actual fixture/catalog preparation finishes before READY.
            counters=inputs.Variant=="private"?new(inputs):null;
            if(counters is null){Protocol.Ready();await Protocol.ExpectAsync("GO").ConfigureAwait(false);}
            else {await counters.ReadyAsync(mode).ConfigureAwait(false);if(mode=="pre-capped")await counters.FloodAsync().ConfigureAwait(false);}
            var observations=new List<object>();
            for(int index=0;index<names!.Length;index++)
            {
                currentCase=names[index];handoffChecks=0;
                recording=counters is not null && mode!="disabled";
                if(recording)counters!.Open();
                admitted=counters?.Read("Admitted")??0;completed=counters?.Read("Completed")??0;active=counters?.Read("Active")??0;
                long ordinal=recording?counters!.Read("Ordinal")+1:0;
                executing=true;
                CaseResult result;
                try {result=CaseResult.Read(await api.ExecuteAsync(handles[index]).ConfigureAwait(false));}
                finally {executing=false;}
                Check.Require(result.Case==currentCase,"exact-case-returned");
                long delta=(counters?.Read("Admitted")??0)-admitted;
                Check.Require(delta==(recording?result.FilterEntries:0),"exact-per-filter-private-admission");
                if(recording)
                {
                    Check.Require(counters!.Read("Ordinal")==ordinal+result.FilterEntries-1
                        && counters.Read("Completed")==completed+result.FilterEntries && counters.Read("Active")==active,
                        "all-own-consecutive-captures-close-after-business");
                    counters.Open();
                }
                bool handoffRequired=recording && CaseGroups.Expected[currentCase].OwnsDeferredResult;
                Check.Require(handoffChecks==(handoffRequired?1:0),"exact-deferred-handoff-observation");
                long[] ordinals=recording?Enumerable.Range(0,result.FilterEntries).Select(i=>ordinal+i).ToArray():Array.Empty<long>();
                observations.Add(new {Result=result,CaptureOrdinals=ordinals,AdmittedDelta=delta,
                    ActualStageRowsRequired=recording && mode=="active",HandoffCounterObservationRequired=handoffRequired,
                    HandoffChecks=handoffChecks,SyntheticDurationBracketOnly=result.Case.StartsWith("filter-double-dispose-",StringComparison.Ordinal)});
            }
            inputs.VerifyLoaded();
            return new {Mode=mode,Group=group,Passed=true,Cases=observations,AllSetupBeforeReady=true,
                RawBusinessValuesPersisted=false,ActualCounterCountsAndAllocationsZero=true};
        }
        finally {for(int i=handles.Count-1;i>=0;i--)await api.DisposeAsync(handles[i]).ConfigureAwait(false);}
    }
}
