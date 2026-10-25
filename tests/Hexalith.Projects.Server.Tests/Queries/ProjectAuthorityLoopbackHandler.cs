#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net.Http.Json;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;

/// <summary>Routes to real owned hosts and observes actual attempts before forwarding, without enforcing authority itself.</summary>
internal sealed class ProjectAuthorityLoopbackHandler(ProjectAuthorityRoutes routes, string channel) : DelegatingHandler(new HttpClientHandler())
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string[] segments = request.RequestUri!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 5 || segments[1] != "invoke") throw new InvalidOperationException("Only Dapr service invocation is allowed.");
        string target = segments[2], method = string.Join('/', segments.Skip(4));
        routes.HumanBearerForwarded |= request.Headers.Authorization is not null;
        QueryEnvelope? query = null;
        string? original = request.Headers.TryGetValues(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, out var originalValues)
            ? originalValues.FirstOrDefault() : null;
        if (method == "query" && request.Content is not null)
        {
            try { query = await request.Content.ReadFromJsonAsync<QueryEnvelope>(cancellationToken).ConfigureAwait(false); }
            catch (JsonException) { }
            if (query is not null && routes.ChangeTarget == target)
            {
                if (routes.ChangeQuery is { } change)
                {
                    query = change(query);
                    request.Content = JsonContent.Create(query);
                }
                if (routes.ReplaceAssertion is { } replacement)
                {
                    request.Headers.Remove(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName);
                    if (replacement(query) is { } assertion) request.Headers.TryAddWithoutValidation(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
                }
            }
        }
        string[] assertions = request.Headers.TryGetValues(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, out var values) ? values.ToArray() : [];
        string state = assertions.Length == 0 ? "missing" : assertions.Length != 1 ? "multiple"
            : new JsonWebTokenHandler().CanReadToken(assertions[0]) ? "jwt" : "malformed";
        var attempt = new ProjectAuthorityInvocationAttempt(target, method, assertions.Length, state);
        routes.Invocations.Enqueue(attempt);
        routes.Attempts.Enqueue(attempt.Summary);
        request.RequestUri = new Uri(routes.Hosts[target], method);
        request.Headers.TryAddWithoutValidation(DaprAppChannelToken.HeaderName, channel);
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (query is not null) routes.Hops.Enqueue(new(target, query, original ?? string.Empty, (int)response.StatusCode));
        return response;
    }
}
#endif
