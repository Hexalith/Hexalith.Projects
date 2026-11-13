namespace Story65.ReceiverPhase;
using System.Diagnostics;
internal static class FlowCanaryState
{
    internal static readonly object Marker=new();
    internal static readonly AsyncLocal<object?> Local=new(static change=>
    {
        if(change.ThreadContextChanged && Thread.CurrentThread.Name=="PrivateCaptureCounterDrain"
            && ReferenceEquals(change.CurrentValue,Marker)) Interlocked.Exchange(ref DrainObserved,1);
    });
    internal static int DrainObserved,FirstProbe=-1,Checks;
    internal static bool SuppressedInvocation;
    internal static Activity? ExpectedActivity;
    internal static void AssertCaller() => Check.Require(ReferenceEquals(Local.Value,Marker)
        && ReferenceEquals(Activity.Current,ExpectedActivity) && !ExecutionContext.IsFlowSuppressed(),"synthetic-caller-state-preserved");
    internal static void Probe(int code)
    {
        Check.Require(code is >=0 and <=3,"closed-flow-probe-code");
        bool first=code==0;
        if(first) Interlocked.CompareExchange(ref FirstProbe,ExecutionContext.IsFlowSuppressed()?1:0,-1);
        bool detached=SuppressedInvocation && !first;
        Check.Require(ReferenceEquals(Local.Value,detached?null:Marker)
            && ReferenceEquals(Activity.Current,detached?null:ExpectedActivity),"synthetic-business-flow-preserved");
        if(first) Check.Require(ExecutionContext.IsFlowSuppressed()==SuppressedInvocation,"first-synchronous-flow-state");
        Interlocked.Increment(ref Checks);
    }
}
