namespace Story65.DomainReceiverPhase;
using System.Diagnostics;
internal static class FreshFlowCanary
{
    internal static async Task<object> RunAsync(string mode,RuntimeInputs inputs,FixtureAdapter api)
    {
        bool positive=mode=="canary-positive-control",suppressed=mode=="canary-suppressed";
        object first=await api.CreateAsync("concrete-delayed-result",FlowCanaryState.Probe).ConfigureAwait(false);
        object second=await api.CreateAsync("concrete-delayed-result",FlowCanaryState.Probe).ConfigureAwait(false);
        try
        {
            // No helper type/field/readiness reflection before this first complete filter/result/release call.
            using Activity activity=new("synthetic-receiver-flow");activity.Start();
            FlowCanaryState.ExpectedActivity=activity;FlowCanaryState.Local.Value=FlowCanaryState.Marker;FlowCanaryState.AssertCaller();
            Protocol.Ready();await Protocol.ExpectAsync("GO").ConfigureAwait(false);
            if(positive)
            {
                using var progress=new ManualResetEventSlim();
                Thread thread=new(()=>progress.Set()){Name="PrivateCaptureCounterDrain"};thread.Start();
                Check.Require(progress.Wait(TimeSpan.FromSeconds(2)) && thread.Join(TimeSpan.FromSeconds(2))
                    && Volatile.Read(ref FlowCanaryState.DrainObserved)==1,"unsafe-named-thread-detector-positive");
                FlowCanaryState.AssertCaller();
                return new {Mode=mode,Passed=true,UnsafeDetectorPositive=true,HelperInspected=false,
                    CallerPreserved=true,CanaryValuesPersisted=false,ExpectedStages=0,CaptureOrdinal=0L,ProbeChecks=0};
            }
            FlowCanaryState.SuppressedInvocation=suppressed;
            Task<object[]> pending=api.ExecuteAsync(first,suppressed);
            FlowCanaryState.AssertCaller();
            Check.Require(Volatile.Read(ref FlowCanaryState.FirstProbe)==(suppressed?1:0),"first-business-synchronous-probe");
            CaseResult a=CaseResult.Read(await pending.ConfigureAwait(false));FlowCanaryState.AssertCaller();
            Check.Require(a.FirstDependencyProbeObserved && a.EntrySuppressionObserved,"first-business-fixture-probes");
            var counters=new CounterControl(inputs); // First helper reflection is after actual business returns.
            await counters.WindowAsync().ConfigureAwait(false);FlowCanaryState.SuppressedInvocation=false;
            long admitted=counters.Read("Admitted"),ordinal=counters.Read("Ordinal")+1;
            counters.Open();CaseResult b=CaseResult.Read(await api.ExecuteAsync(second).ConfigureAwait(false));
            Check.Require(counters.Read("Admitted")-admitted==1 && counters.Read("Ordinal")==ordinal,"second-canary-capture-admitted");
            FlowCanaryState.AssertCaller();inputs.VerifyLoaded();
            Check.Require(Volatile.Read(ref FlowCanaryState.DrainObserved)==0 && FlowCanaryState.Checks==14,"helper-worker-did-not-inherit-canary");
            return new {Mode=mode,Passed=true,UnsafeDetectorPositive=false,HelperInspected=true,
                CallerPreserved=true,CanaryValuesPersisted=false,ExpectedStages=b.ExpectedStageMask,CaptureOrdinal=ordinal,ProbeChecks=FlowCanaryState.Checks};
        }
        finally
        {
            FlowCanaryState.Local.Value=null;FlowCanaryState.ExpectedActivity=null;
            await api.DisposeAsync(second).ConfigureAwait(false);await api.DisposeAsync(first).ConfigureAwait(false);
        }
    }
}
