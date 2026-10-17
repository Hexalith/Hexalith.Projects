#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net.Http.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;

internal sealed class GatewayFixtureLoopbackHandler(GatewayFixtureLocalRoutes routes) : DelegatingHandler(new HttpClientHandler())
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string[] segments = request.RequestUri!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 5 || segments[1] != "invoke") throw new InvalidOperationException("Only service invocation is allowed in this fixture.");
            string target = segments[2], method = string.Join('/', segments.Skip(4));
            routes.HumanBearerForwarded |= request.Headers.Authorization is not null;
            if (method == "api/v1/queries/owner-authority/workload")
            {
                routes.OwnerQueries++;
                if (routes.OwnerStatusOverride is { } status) return new HttpResponseMessage(status);
            }
            if (target == "projects" && method == "query" && routes.TamperNextProjectQuery)
            {
                routes.TamperNextProjectQuery = false;
                QueryEnvelope query = (await request.Content!.ReadFromJsonAsync<QueryEnvelope>(cancellationToken).ConfigureAwait(false))!;
                request.Content = JsonContent.Create(query with { UserId = "tampered", OriginalActorId = "tampered" });
            }
            request.RequestUri = new Uri(routes.Hosts[target], method);
            request.Headers.TryAddWithoutValidation(DaprAppChannelToken.HeaderName, "story61-owned-local-channel");
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (target == "folders" && method == "query" && routes.OwnerResponseOverride is { } malformed && response.IsSuccessStatusCode)
            {
                using var payload = System.Text.Json.JsonDocument.Parse(malformed);
                response.Content = JsonContent.Create(QueryResult.FromPayload(payload.RootElement));
            }
            return response;
        }
    }
#endif
