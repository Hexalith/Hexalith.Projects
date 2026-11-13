namespace Story65.DelegatedClientProof;

using System.Net;
using System.Net.Http.Headers;

internal sealed class ProbeResponseContent(byte[] bytes, FixtureState state) : HttpContent
{
    private int _disposed;
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => SerializeToStreamAsync(stream, context, CancellationToken.None);
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken token)
    {
        state.Observe("response.buffer.begin", token: token);
        await state.FakeAwaitAsync().ConfigureAwait(false);
        state.Trigger("buffer-throw");
        if (state.Test.Fault == "buffer-cooperative")
        { Interlocked.Increment(ref state.Triggered); await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false); }
        if (state.Test.Fault == "buffered-body-delay")
        { Interlocked.Increment(ref state.Triggered); await Task.Delay(50, token).ConfigureAwait(false); }
        Headers.ContentType = new MediaTypeHeaderValue("application/json");
        await stream.WriteAsync(bytes, token).ConfigureAwait(false);
        state.Observe("response.buffer.end", token: token);
    }
    protected override bool TryComputeLength(out long length) { length = bytes.Length; return true; }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            state.Observe("response.dispose"); Interlocked.Increment(ref state.ResponseDisposed);
            if (state.Test.Fault == "late-response-disposal") { Interlocked.Increment(ref state.Triggered); Thread.Sleep(250); }
            state.Trigger("response-disposal-throw");
        }
    }
}
