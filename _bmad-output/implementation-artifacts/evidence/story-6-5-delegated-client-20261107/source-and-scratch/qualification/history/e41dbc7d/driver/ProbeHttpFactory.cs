namespace Story65.DelegatedClientProof;

internal sealed class ProbeHttpFactory(HttpClient client, FixtureState state) : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    { state.Observe("factory.create", name); state.Trigger("factory-throw"); return client; }
}
