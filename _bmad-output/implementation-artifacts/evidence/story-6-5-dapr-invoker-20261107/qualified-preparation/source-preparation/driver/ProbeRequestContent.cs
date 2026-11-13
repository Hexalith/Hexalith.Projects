namespace Story65.DaprInvokerProof;

using System.Net;

internal sealed class ProbeRequestContent(HttpContent inner, FixtureState state) : HttpContent
{
    private int _disposed;
    internal ProbeRequestContent CopyHeaders()
    { foreach (var header in inner.Headers) Headers.TryAddWithoutValidation(header.Key, header.Value); return this; }
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => inner.CopyToAsync(stream);
    protected override bool TryComputeLength(out long length) { length = 0; return false; }
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            inner.Dispose(); Interlocked.Increment(ref state.RequestDisposed); state.Observe("request.dispose"); state.ThrowIf("request-dispose");
        }
        base.Dispose(disposing);
    }
}
