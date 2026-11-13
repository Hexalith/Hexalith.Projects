namespace Story65.DelegatedClientProof;

using Microsoft.AspNetCore.Http;

internal sealed class ProbeAccessor(FixtureState state, HttpContext context) : IHttpContextAccessor
{
    private HttpContext? _context = context;
    public HttpContext? HttpContext
    {
        get { state.Observe("accessor.get"); return _context; }
        set => _context = value;
    }
}
