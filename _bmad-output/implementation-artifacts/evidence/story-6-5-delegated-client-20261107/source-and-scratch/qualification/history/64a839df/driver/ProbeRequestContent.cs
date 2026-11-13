namespace Story65.DelegatedClientProof;

using System.Net;

internal sealed class ProbeRequestContent(HttpContent inner, FixtureState state) : HttpContent
{
    private int _disposed;
    public ProbeRequestContent CopyHeaders()
    { foreach (var header in inner.Headers) Headers.TryAddWithoutValidation(header.Key, header.Value); return this; }
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => inner.CopyToAsync(stream);
    protected override bool TryComputeLength(out long length) { length = 0; return false; }
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            state.Observe("request.dispose"); Interlocked.Increment(ref state.RequestDisposed);
            inner.Dispose();
            if (state.Test.Fault == "late-request-disposal") { Interlocked.Increment(ref state.Triggered); Thread.Sleep(250); }
            state.Trigger("request-disposal-throw");
        }
        base.Dispose(disposing);
    }
}
