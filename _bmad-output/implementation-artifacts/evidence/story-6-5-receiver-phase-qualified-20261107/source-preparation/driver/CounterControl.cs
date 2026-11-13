namespace Story65.ReceiverPhase;
using System.Diagnostics;
using System.Reflection;
// Reflection is restricted to the helper in this process's exact private SDK, never a driver copy.
internal sealed class CounterControl(RuntimeInputs inputs)
{
    private const BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic;
    private Type Counter => inputs.Sdk.GetType("Hexalith.PrivateCaptureCounter.CaptureCounters",true)!;
    private IDisposable? Begin() => (IDisposable?)inputs.Sdk.GetType("Hexalith.PrivateCaptureCounter.ReceiverPhaseBootstrap",true)!.GetMethod("Begin",Flags)!.Invoke(null,null);
    internal long Read(string name) => Convert.ToInt64(Counter.GetField(name,Flags)!.GetValue(null));
    internal async Task ReadyAsync(string mode)
    {
        using(Begin()) { } // Starts helper only after all ordinary fixture setup has completed.
        Protocol.Ready(); await Protocol.ExpectAsync("GO").ConfigureAwait(false);
        if(mode=="disabled") { Check.Require(Read("Admitted")==0,"disabled-window-no-admission"); return; }
        await WindowAsync().ConfigureAwait(false);
    }
    internal async Task WindowAsync()
    {
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while(Read("WindowEnd")==0) await Task.Delay(5,timeout.Token).ConfigureAwait(false);
        Check.Require(Read("WindowEnd")>Stopwatch.GetTimestamp(),"fresh-window-open");
    }
    internal void Open() => Check.Require(Read("WindowEnd")>Stopwatch.GetTimestamp() && Read("WorkerClosed")==0,"case-window-still-open");
    internal async Task FloodAsync()
    {
        long dropped=Read("QueueDropped"),closed=Read("ClosedDropped"),disposed=0;
        long until=Stopwatch.GetTimestamp()+3*Stopwatch.Frequency;
        for(int i=0;i<1000000 && Stopwatch.GetTimestamp()<until;i++)
        {
            var capture=Begin(); Check.Require(capture is not null,"precap-admission-required"); capture!.Dispose(); disposed++;
            if(disposed-(Read("QueueDropped")-dropped)-(Read("ClosedDropped")-closed)>=65538) break;
            if(i%1024==0) await Task.Delay(1).ConfigureAwait(false);
        }
        Check.Require(disposed-(Read("QueueDropped")-dropped)-(Read("ClosedDropped")-closed)>=65538,"precap-accepted-scalar-row-count");
        object queue=Counter.GetField("Queue",Flags)!.GetValue(null)!;
        object reader=queue.GetType().GetProperty("Reader")!.GetValue(queue)!;
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(1));
        while((int)reader.GetType().GetProperty("Count")!.GetValue(reader)! !=0) await Task.Delay(1,timeout.Token).ConfigureAwait(false);
        // Single drainer can hold at most one dequeued row: >=65538 accepted and empty queue prove a cap drop before cases.
        Check.Require(Read("IoFailures")==0 && Read("ClosedDropped")==closed && Read("WorkerClosed")==0,"precap-open-ordinary-sink");
        Open();
    }
    internal async Task FinishAsync(string mode)
    {
        if(mode!="disabled")
        {
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(32));
            while(Read("WorkerClosed")==0) await Task.Delay(10,timeout.Token).ConfigureAwait(false);
            Check.Require(Read("Active")==0 && Read("Completed")==Read("Admitted"),"all-captures-closed");
        }
        Console.WriteLine("CASES_DONE"); await Protocol.ExpectAsync("EXIT").ConfigureAwait(false);
    }
}
