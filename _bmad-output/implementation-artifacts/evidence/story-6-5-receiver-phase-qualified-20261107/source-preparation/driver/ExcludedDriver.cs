namespace Story65.ReceiverPhase;
using System.Diagnostics;
internal static class ExcludedDriver
{
    internal static async Task<object> RunAsync(RuntimeInputs inputs,FixtureAdapter api)
    {
        Check.Require(inputs.Role=="excluded" && inputs.Variant=="private","genuine-private-excluded-entry");
        object handle=await api.CreateAsync("native-success",FlowCanaryState.Probe).ConfigureAwait(false);
        try
        {
            using Activity activity=new("synthetic-excluded-flow");activity.Start();
            FlowCanaryState.ExpectedActivity=activity;FlowCanaryState.Local.Value=FlowCanaryState.Marker;
            long start=Stopwatch.GetTimestamp();Protocol.Ready();await Protocol.ExpectAsync("GO").ConfigureAwait(false);
            Check.Require(Stopwatch.GetTimestamp()<start+Stopwatch.Frequency,"excluded-window-start-fresh");
            // Never construct CounterControl, inspect helper types/fields or poll worker readiness.
            CaseResult result=CaseResult.Read(await api.ExecuteAsync(handle).ConfigureAwait(false));
            FlowCanaryState.AssertCaller();Check.Require(FlowCanaryState.FirstProbe==0,"excluded-first-business-flow");
            Check.Require(Stopwatch.GetTimestamp()<start+8*Stopwatch.Frequency,"excluded-actual-probe-in-window");
            while(Stopwatch.GetTimestamp()<start+12*Stopwatch.Frequency)await Task.Delay(10).ConfigureAwait(false);
            inputs.VerifyLoaded();
            return new {Mode="excluded",Passed=true,Result=result,HelperInspected=false,Admitted=(long?)null,
                LivedTicks=Stopwatch.GetTimestamp()-start,Frequency=Stopwatch.Frequency,
                SourceGateAndNoArtifactsOnly=true,InvisibleWorkerAbsenceClaimed=false,CanaryValuesPersisted=false};
        }
        finally {FlowCanaryState.Local.Value=null;FlowCanaryState.ExpectedActivity=null;await api.DisposeAsync(handle).ConfigureAwait(false);}
    }
}
