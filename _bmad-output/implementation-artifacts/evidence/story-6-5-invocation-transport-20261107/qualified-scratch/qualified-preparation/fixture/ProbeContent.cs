namespace Story65.InvocationFixture;

using System.Net;

internal sealed class ProbeContent(CaseState state, bool response, byte[] bytes) : HttpContent
{
    private int disposed;
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        if (response)
        {
            state.BodyReads++; state.Observe("response.buffer");
            Verify.That(state.HandlerReturned, "Outer buffering preceded handler return.");
            if (state.Name == "outer-buffer-fault") throw state.Fault("buffer", new IOException("synthetic-buffer-fault"));
        }
        await stream.WriteAsync(bytes).ConfigureAwait(false);
    }
    protected override bool TryComputeLength(out long length) { length = bytes.Length; return true; }
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref disposed, 1) == 0)
        { if (response) state.ResponseDisposals++; else state.RequestDisposals++; state.Observe(response ? "response.dispose" : "request-content.dispose"); }
        base.Dispose(disposing);
    }
}
