namespace Story65.InvocationFixture;

using System.Net;

internal sealed class ProbeRequestContent : HttpContent
{
    private readonly CaseState state;
    private readonly HttpContent inner;
    private int disposed;
    internal ProbeRequestContent(CaseState state, HttpContent inner)
    {
        this.state = state; this.inner = inner;
        foreach (var header in inner.Headers) Headers.TryAddWithoutValidation(header.Key, header.Value);
    }
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => inner.CopyToAsync(stream);
    protected override bool TryComputeLength(out long length) { length = 0; return false; }
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref disposed, 1) == 0)
        { state.RequestDisposals++; state.Observe("request-content.dispose"); inner.Dispose(); }
        base.Dispose(disposing);
    }
}
