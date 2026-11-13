namespace Story65.ReceiverPhase;
using System.Diagnostics;
internal static class Protocol
{
    internal static void Ready() => Console.WriteLine("READY:"+Stopwatch.Frequency+":"+Stopwatch.GetTimestamp());
    internal static async Task ExpectAsync(string token)
        => Check.Require(await Console.In.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false)==token,"closed-protocol-ack");
}
