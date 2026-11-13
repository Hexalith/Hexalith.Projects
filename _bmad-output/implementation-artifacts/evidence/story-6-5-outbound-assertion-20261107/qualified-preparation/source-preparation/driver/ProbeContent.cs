namespace Story65.OutboundAssertionProof;

using System.Net;

internal sealed class ProbeContent(FixtureState state, bool response) : HttpContent
{
    private int disposed;
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        if (response)
        {
            state.Buffers++; state.Observe("response.buffer");
            Check.Require(state.ReturnedBeforeBuffer, "Outer buffering entered before the selected handler returned.");
            await state.ProbeAsync().ConfigureAwait(false);
            state.ThrowIf("buffer");
        }
        await stream.WriteAsync("synthetic-body"u8.ToArray()).ConfigureAwait(false);
    }
    protected override bool TryComputeLength(out long length) { length = 0; return false; }
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref disposed, 1) == 0)
        {
            if (response) state.ResponseDisposed++; else state.RequestDisposed++;
            state.Observe(response ? "response.dispose" : "request.dispose");
        }
        base.Dispose(disposing);
    }
}
