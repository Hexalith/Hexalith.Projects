internal sealed class ProfileClients(HttpMessageHandler state) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => name == "route-catalog-state" ? new(state, false) : throw new InvalidOperationException("No application/network client available");
}
