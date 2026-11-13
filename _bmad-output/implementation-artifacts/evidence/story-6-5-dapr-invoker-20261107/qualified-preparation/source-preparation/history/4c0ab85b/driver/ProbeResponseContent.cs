namespace Story65.DaprInvokerProof;

using System.Net;
using System.Net.Http.Headers;

internal sealed class ProbeResponseContent(byte[] bytes, FixtureState state) : HttpContent
{
    private int _disposed;
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => SerializeToStreamAsync(stream, context, CancellationToken.None);
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken token)
    {
        state.Observe("response.buffer.begin", token: token); await state.ProbeAsync().ConfigureAwait(false);
        if (state.Test.Fault == "buffer-io") { state.MarkFault("buffer-io"); throw new IOException("synthetic fixture fault"); }
        if (state.Test.Fault == "buffer-cancel") { state.MarkFault("buffer-cancel"); state.Caller.Cancel(); token.ThrowIfCancellationRequested(); }
        Headers.ContentType = new MediaTypeHeaderValue("application/json");
        await stream.WriteAsync(bytes, token).ConfigureAwait(false); state.Observe("response.buffer.end", token: token);
    }
    protected override bool TryComputeLength(out long length) { length = bytes.Length; return true; }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
        { Interlocked.Increment(ref state.ResponseDisposed); state.Observe("response.dispose"); state.ThrowIf("response-dispose"); }
    }
}
