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
            bool invocation = segments.Length >= 5 && segments[1] == "invoke";
            string target = invocation ? segments[2] : request.RequestUri.Host;
            if (!routes.Hosts.ContainsKey(target)) throw new InvalidOperationException("Only enrolled service transport is allowed in this fixture.");
            string method = invocation ? string.Join('/', segments.Skip(4)) : request.RequestUri.PathAndQuery.TrimStart('/');
            routes.HumanBearerForwarded |= request.Headers.Authorization is not null;
            if (!invocation && target is "folders" or "conversations" && routes.OwnerStatusOverride is { } ownerStatus)
                return new HttpResponseMessage(ownerStatus);
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
            if (target == "projects" && method == "query")
                routes.ProjectQueries.Enqueue(System.Text.Json.JsonSerializer.Deserialize<QueryEnvelope>(
                    await request.Content!.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false), System.Text.Json.JsonSerializerOptions.Web)!);
            request.RequestUri = new Uri(routes.Hosts[target], method);
            request.Headers.TryAddWithoutValidation(DaprAppChannelToken.HeaderName, "story61-owned-local-channel");
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            routes.TransportResponses.Enqueue(target + ":" + method + ":" + (int)response.StatusCode
                + (target == "folders" && method.StartsWith("api/v1/", StringComparison.Ordinal)
                    ? ":" + await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false) : ""));
            if (target == "folders" && method == "query" && routes.OwnerResponseOverride is { } malformed && response.IsSuccessStatusCode)
            {
                using var payload = System.Text.Json.JsonDocument.Parse(malformed);
                response.Content = JsonContent.Create(QueryResult.FromPayload(payload.RootElement));
            }
            return response;
        }
    }
#endif
