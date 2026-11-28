#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Projections.ProjectReads;
using Hexalith.Tenants.Contracts.Identity;

using Xunit;

/// <summary>Qualifies the ordinary local Debug topology from its owned launch and persisted state.</summary>
public sealed class OrdinaryRunModeLiveTests
{
    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;
    private static readonly string[] Receivers = ["eventstore", "projects", "tenants"];

    /// <summary>Proves one committed generation, real delivery, and side-effect-free credential refusals.</summary>
    [Fact]
    public async Task QualifiesOwnedCatalogAndDelivery()
    {
        string configuration = Environment.GetEnvironmentVariable("PROJECTS_ORDINARY_RUN_CONFIG")
            ?? throw new InvalidOperationException("The ordinary qualification requires the exact owned launcher.");
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        budget.CancelAfter(TimeSpan.FromMinutes(4));
        CancellationToken token = budget.Token;
        using JsonDocument input = JsonDocument.Parse(await File.ReadAllBytesAsync(configuration, token).ConfigureAwait(true));
        var config = input.RootElement;
        string Value(string name) => config.GetProperty(name).GetString()!;
        string Endpoint(string name) => config.GetProperty("endpoints").GetProperty(name).GetString()!;
        string catalogPath = Value("catalogDirectory");
        string rootDigest = Value("catalogRoot");
        string tenant = Value("tenant");
        string actor = Value("actor");
        string human = Value("human");
        string wrong = Value("wrongWorkload");
        RouteCatalogGenerationReadback catalog = await ReadCommittedCatalogAsync(catalogPath, rootDigest, token).ConfigureAwait(true);
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

        async Task<HttpResponseMessage> SendAsync(string resource, string route, HttpMethod method,
            object? payload, string? bearer = null, string? workload = null, string? scope = null)
        {
            using var request = new HttpRequestMessage(method, Endpoint(resource) + "/" + route);
            if (payload is not null)
            {
                request.Content = JsonContent.Create(payload, options: Json);
            }

            if (bearer is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            }

            if (workload is not null)
            {
                request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, workload);
            }

            if (route.StartsWith("v1.0/invoke/", StringComparison.Ordinal))
            {
                request.Headers.Add(RouteCatalogInvocationHandler.RootHeader, catalog.RootDigest);
                request.Headers.Add(RouteCatalogInvocationHandler.EpochHeader, catalog.Epoch.ToString(CultureInfo.InvariantCulture));
                request.Headers.Add(RouteCatalogInvocationHandler.BindingHeader, catalog.ActivationBindingDigest);
            }

            string? platformScope = payload switch
            {
                SubmitCommandRequest command => command.Tenant,
                StreamReadRequest stream => stream.Tenant,
                _ => null,
            };
            if (platformScope is TenantPlatformScopes.Tenants or TenantPlatformScopes.GlobalAdministrators)
            {
                request.Headers.Add("X-Hexalith-Scope", platformScope);
            }

            if (scope is not null)
            {
                SupportedProjectReadDistributedFixture.BindStatusScope(request, scope);
            }

            return await http.SendAsync(request, token).ConfigureAwait(true);
        }

        async Task<StreamReadPage> StreamAsync(string scope, string domain, string aggregate)
        {
            using var response = await SendAsync("eventstore", "api/v1/streams/read", HttpMethod.Post,
                new StreamReadRequest(scope, domain, aggregate), human).ConfigureAwait(true);
            Require(response.StatusCode == HttpStatusCode.OK, "Authenticated " + domain + " stream read was refused (" + (int)response.StatusCode + ").");
            return (await response.Content.ReadFromJsonAsync<StreamReadPage>(Json, token).ConfigureAwait(true))!;
        }

        async Task AwaitCompletionAsync(string message, string scope, string operation)
        {
            for (int attempt = 0; attempt < 240; attempt++)
            {
                using var response = await SendAsync("eventstore", "api/v1/commands/status/" + message, HttpMethod.Get, null, human, scope: scope).ConfigureAwait(true);
                Require(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
                    "Durable status for " + operation + " was refused (" + (int)response.StatusCode + ").");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(true));
                    string? status = body.RootElement.GetProperty("status").GetString();
                    if (status == "Completed") return;
                    Require(status is not ("Rejected" or "TimedOut" or "PublishFailed"), "The " + operation + " command did not persist successfully (" + status + ").");
                }

                await Task.Delay(250, token).ConfigureAwait(true);
            }

            throw new InvalidOperationException("The " + operation + " command did not reach durable completion.");
        }

        async Task<string> SubmitAsync(string scope, string domain, string aggregate, string type, object payload)
        {
            var command = Command(scope, domain, aggregate, type, payload);
            using var response = await SendAsync("eventstore", "api/v1/commands", HttpMethod.Post, command, human).ConfigureAwait(true);
            Require(response.StatusCode == HttpStatusCode.Accepted, "The " + type + " command was refused (" + (int)response.StatusCode + ").");
            await AwaitCompletionAsync(command.MessageId, scope, type).ConfigureAwait(true);
            return command.MessageId;
        }

        async Task<SupportedProjectReadStateSnapshot> StateAsync(string key)
        {
            using var response = await SendAsync("eventstore-dapr-cli", "v1.0/state/statestore/" + Uri.EscapeDataString(key), HttpMethod.Get, null).ConfigureAwait(true);
            Require(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent, "The owned Dapr state read failed (" + (int)response.StatusCode + ").");
            return new(key, await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(true),
                response.Headers.TryGetValues("ETag", out var etags) ? etags.Single() : string.Empty);
        }

        async Task<SupportedProjectReadStateSnapshot> WaitForStateAsync(string key, Func<JsonElement, bool> matches, string operation)
        {
            for (int attempt = 0; attempt < 180; attempt++)
            {
                var state = await StateAsync(key).ConfigureAwait(true);
                if (state.Bytes.Length > 0 && state.ETag.Length > 0)
                {
                    using var body = JsonDocument.Parse(state.Bytes);
                    if (matches(body.RootElement)) return state;
                }

                await Task.Delay(250, token).ConfigureAwait(true);
            }

            throw new InvalidOperationException("The persisted " + operation + " did not converge.");
        }

        string bootstrapReceiptPath = Path.Combine(catalogPath, "tenants.bootstrap.json");
        using var bootstrapReceipt = JsonDocument.Parse(await WaitForFileAsync(bootstrapReceiptPath, token,
            receipt => SupportedProjectReadDistributedFixture.HostedBootstrapReceiptMatches(receipt, rootDigest, actor)
                && receipt.GetProperty("AttemptId").GetString() == catalog.AttemptId
                && receipt.GetProperty("Phase").GetString() == "accepted").ConfigureAwait(true));
        string bootstrap = bootstrapReceipt.RootElement.GetProperty("MessageId").GetString()!;
        await AwaitCompletionAsync(bootstrap, TenantPlatformScopes.GlobalAdministrators, "hosted bootstrap").ConfigureAwait(true);
        using var bootstrapSource = await SendAsync("tenants-dapr-cli", "v1.0/invoke/eventstore/method/api/v1/streams/read/workload",
            HttpMethod.Post, new StreamReadRequest(TenantPlatformScopes.GlobalAdministrators, "global-administrators", "global-administrators"),
            workload: Value("bootstrapSource")).ConfigureAwait(true);
        Require(bootstrapSource.StatusCode == HttpStatusCode.OK, "The separately bound Tenants bootstrap source read was refused ("
            + (int)bootstrapSource.StatusCode + ").");
        var globalAdmin = await bootstrapSource.Content.ReadFromJsonAsync<StreamReadPage>(Json, token).ConfigureAwait(true);
        Require(globalAdmin is { Events.Count: 1 }
            && SupportedProjectReadDistributedFixture.HostedBootstrapSourceMatches(globalAdmin, bootstrap,
                bootstrapReceipt.RootElement.GetProperty("CorrelationId").GetString()!, actor),
            "The real bootstrap did not produce one attributable source event.");

        string created = await SubmitAsync(TenantPlatformScopes.Tenants, "tenants", tenant, "CreateTenant",
            new { tenantId = tenant, name = "Disposable Projects qualification", description = "Ordinary Debug catalog" }).ConfigureAwait(true);
        string member = await SubmitAsync(TenantPlatformScopes.Tenants, "tenants", tenant, "AddUserToTenant",
            new { tenantId = tenant, userId = actor, role = "TenantOwner" }).ConfigureAwait(true);
        var membership = await StreamAsync(TenantPlatformScopes.Tenants, "tenants", tenant).ConfigureAwait(true);
        Require(SupportedProjectReadDistributedFixture.SeedSourceMatches(membership, "system", "tenants", tenant, member, actor)
            && membership.Events.Count == 2 && membership.Events[0].CausationId == created,
            "Tenant creation and membership did not persist in their exact storage scope.");
        string membershipKey = "projects:tenant-access:" + tenant;
        var tenantProjection = await WaitForStateAsync(membershipKey,
            state => SupportedProjectReadDistributedFixture.TenantAuthorityStateMatches(state, tenant, actor, membership, projectDocument: true),
            "Projects tenant membership projection").ConfigureAwait(true);

        string project = UniqueIdHelper.GenerateSortableUniqueStringId();
        const string projectName = "Ordinary enrolled project";
        const string commandType = "Hexalith.Projects.Commands.CreateProject";
        string accepted = await SubmitAsync(tenant, "projects", project, commandType, new { name = projectName }).ConfigureAwait(true);
        var history = await StreamAsync(tenant, "projects", project).ConfigureAwait(true);
        string[] types = history.Events.Select(item => item.EventTypeName.Split(',', 2)[0].Trim()).ToArray();
        bool exactWriterSequence = types.SequenceEqual([typeof(ProjectCreated).FullName, typeof(ProjectFolderCreationPending).FullName]);
        bool exactCommand = history.Events.All(item => item.CausationId == accepted && item.CorrelationId == accepted);
        Require(SupportedProjectReadDistributedFixture.SeedSourceMatches(history, tenant, "projects", project, accepted, actor)
            && exactWriterSequence && exactCommand,
            "The retained Projects writer did not persist the required create and pending-folder sequence (observed events "
                + history.Events.Count + ", exact sequence " + exactWriterSequence + ", exact command " + exactCommand + ").");
        const string projectsSidecarRoute = "v1.0/invoke/projects/method/";
        using var metadata = await SendAsync("eventstore-dapr-cli", projectsSidecarRoute + "admin/operational-index-metadata",
            HttpMethod.Post, new AdminOperationalIndexMetadata.Request(["projects"]) { AppId = "projects", ServiceVersion = "v1" },
            workload: wrong).ConfigureAwait(true);
        Require(metadata.StatusCode == HttpStatusCode.OK, "The committed Projects projection metadata was refused ("
            + (int)metadata.StatusCode + ").");
        using var projectionCatalog = JsonDocument.Parse(await metadata.Content.ReadAsByteArrayAsync(token).ConfigureAwait(true));
        string fingerprint = projectionCatalog.RootElement.GetProperty("catalogFingerprint").GetString()!;
        var projection = new ProjectionDispatchRequest(SupportedProjectReadDistributedFixture.MapProjectStream(history, tenant, project),
            ["project-reads"], UniqueIdHelper.GenerateSortableUniqueStringId(), fingerprint);
        const string receiver = projectsSidecarRoute + "project/v2";
        string detailKey = ProjectReadModelKeys.Detail(tenant, project);
        var absentDetail = await StateAsync(detailKey).ConfigureAwait(true);
        var originalIndex = await StateAsync(ProjectReadModelKeys.Index(tenant)).ConfigureAwait(true);
        Require(absentDetail.Bytes.Length == 0 && absentDetail.ETag.Length == 0,
            "The named projection detail existed before the credential controls.");
        foreach ((string? credential, HttpStatusCode expected) in new (string?, HttpStatusCode)[]
                 { (null, HttpStatusCode.Unauthorized), (wrong, HttpStatusCode.Forbidden) })
        {
            using var refused = await SendAsync("eventstore-dapr-cli", receiver, HttpMethod.Post, projection,
                workload: credential).ConfigureAwait(true);
            Require(refused.StatusCode == expected,
                "The Projects receiver did not refuse the workload credential (" + (int)refused.StatusCode + ").");
            Require(SameState(absentDetail, await StateAsync(detailKey).ConfigureAwait(true))
                && SameState(originalIndex, await StateAsync(originalIndex.Key).ConfigureAwait(true)),
                "A refused workload credential applied the pending named projection.");
        }

        using var delivery = await SendAsync("eventstore-dapr-cli", receiver, HttpMethod.Post, projection,
            workload: Value("projectionWorkload")).ConfigureAwait(true);
        Require(delivery.StatusCode == HttpStatusCode.OK, "The authorized named Projects delivery was refused ("
            + (int)delivery.StatusCode + ").");
        var delivered = await delivery.Content.ReadFromJsonAsync<ProjectionDispatchResponse>(Json, token).ConfigureAwait(true);
        Require(delivered is { Version: ProjectionDispatchProtocol.Version, Outcomes.Count: 1 }
            && delivered.Outcomes[0] is { ProjectionType: "project-reads",
                Status: ProjectionDispatchStatus.Completed },
            "The named SDK callback did not complete the Projects projection (" + delivered?.Outcomes.FirstOrDefault()?.Status + ").");
        var detail = await WaitForStateAsync(detailKey, state =>
            state.TryGetProperty("sequence", out var sequence) && sequence.GetInt64() == history.Metadata.LatestSequence
            && state.GetProperty("globalPosition").GetInt64() == history.Events[^1].GlobalPosition
            && state.GetProperty("detail").GetProperty("name").GetString() == projectName,
            "Projects detail matching the real source head and global position").ConfigureAwait(true);

        var query = new SubmitQueryRequest(tenant, "projects", project, GetProjectQuery.QueryType,
            Payload: JsonSerializer.SerializeToElement(new GetProjectQuery(InspectDescriptiveMetadata: true, InspectQuarantined: true), Json));
        using var queryResponse = await SendAsync("eventstore", "api/v1/queries", HttpMethod.Post, query, human).ConfigureAwait(true);
        Require(queryResponse.StatusCode == HttpStatusCode.OK, "The digest-bound Project query was refused (" + (int)queryResponse.StatusCode + ").");
        var result = await queryResponse.Content.ReadFromJsonAsync<SubmitQueryResponse>(Json, token).ConfigureAwait(true);
        var opened = result is { Success: true } ? result.Payload.Deserialize<GetProjectResponse>(Json) : null;
        Require(opened?.ProjectId == project && opened.Name == projectName, "The query did not return the committed projection's exact project and name.");
        var index = await StateAsync(ProjectReadModelKeys.Index(tenant)).ConfigureAwait(true);

        string deniedProject = UniqueIdHelper.GenerateSortableUniqueStringId();
        var denied = Command(tenant, "projects", deniedProject, commandType, new { name = "Credential refusal control" });
        using var missingHuman = await SendAsync("eventstore", "api/v1/commands", HttpMethod.Post, denied).ConfigureAwait(true);
        using var wrongHuman = await SendAsync("eventstore", "api/v1/commands", HttpMethod.Post, denied, wrong).ConfigureAwait(true);
        Require(missingHuman.StatusCode == HttpStatusCode.Unauthorized && wrongHuman.StatusCode == HttpStatusCode.Unauthorized,
            "The gateway admitted a missing human credential or a workload token in its place.");
        using var deniedStatus = await SendAsync("eventstore", "api/v1/commands/status/" + denied.MessageId,
            HttpMethod.Get, null, human, scope: tenant).ConfigureAwait(true);
        Require(deniedStatus.StatusCode == HttpStatusCode.NotFound, "A refused human command produced a durable command status.");

        var after = await StreamAsync(tenant, "projects", project).ConfigureAwait(true);
        using var deniedSource = await SendAsync("eventstore", "api/v1/streams/read", HttpMethod.Post,
            new StreamReadRequest(tenant, "projects", deniedProject), human).ConfigureAwait(true);
        Require(deniedSource.StatusCode == HttpStatusCode.NotFound || deniedSource.StatusCode == HttpStatusCode.OK
            && (await deniedSource.Content.ReadFromJsonAsync<StreamReadPage>(Json, token).ConfigureAwait(true))?.Metadata.LatestSequence == 0,
            "A refused human command produced source history.");
        var detailAfterRefusals = await StateAsync(detailKey).ConfigureAwait(true);
        var tenantAfterRefusals = await StateAsync(membershipKey).ConfigureAwait(true);
        var indexAfterRefusals = await StateAsync(index.Key).ConfigureAwait(true);
        Require(SerializeDigest(history) == SerializeDigest(after)
            && SameState(detail, detailAfterRefusals) && SameState(tenantProjection, tenantAfterRefusals)
            && SameState(index, indexAfterRefusals)
            && (await StateAsync(ProjectReadModelKeys.Detail(tenant, deniedProject)).ConfigureAwait(true)).Bytes.Length == 0,
            "A refusal produced or rewrote domain history or projection state.");

        await File.WriteAllBytesAsync(Value("resultPath"), JsonSerializer.SerializeToUtf8Bytes(new
        {
            catalog = new { catalog.Epoch, catalog.Generation, catalog.RootDigest, catalog.ActivationBindingDigest, servingReceivers = Receivers },
            persisted = new { bootstrap = true, tenantMembership = true, projectHistory = true, projectProjection = true, queryMatchedProjection = true, refusalsUnchanged = true },
            source = new { bootstrapEvents = globalAdmin!.Events.Count, membershipEvents = membership.Events.Count,
                projectEvents = history.Events.Count, projectHead = history.Metadata.LatestSequence, projectGlobalPosition = history.Events[^1].GlobalPosition,
                projectionDelivery = "named-sdk-callback" },
            credentials = new { human = "allowed", missingWorkload = "refused", wrongWorkload = "refused", missingHuman = "refused", wrongHuman = "refused" },
        }, Json), token).ConfigureAwait(true);
    }

    private static SubmitCommandRequest Command(string scope, string domain, string aggregate, string type, object payload)
    {
        string id = UniqueIdHelper.GenerateSortableUniqueStringId();
        return new(id, scope, domain, aggregate, type, JsonSerializer.SerializeToElement(payload, Json), id,
            new Dictionary<string, string> { ["taskId"] = id });
    }

    private static async Task<RouteCatalogGenerationReadback> ReadCommittedCatalogAsync(string path, string digest, CancellationToken token)
    {
        var reads = new List<RouteCatalogGenerationReadback>();
        foreach (string receiver in Receivers)
        {
            byte[] contents = await WaitForFileAsync(Path.Combine(path, receiver + ".committed.json"), token).ConfigureAwait(false);
            reads.Add(JsonSerializer.Deserialize<RouteCatalogGenerationReadback>(contents)!);
        }

        var expected = reads[0];
        Require(expected is { Serving: true, Epoch: > 0, Generation: > 0, ActivationBindingDigest.Length: 64, AttemptId.Length: > 0 }
            && expected.RootDigest == digest
            && expected.RequiredHosts.Select(peer => peer.HostId).Order(StringComparer.Ordinal).SequenceEqual(Receivers)
            && reads.All(value => SerializeDigest(value) == SerializeDigest(expected)),
            "EventStore, Projects, and Tenants lacked a matching committed SDK generation.");
        return expected;
    }

    private static async Task<byte[]> WaitForFileAsync(string path, CancellationToken token, Func<JsonElement, bool>? matches = null)
    {
        for (int attempt = 0; attempt < 240; attempt++)
        {
            if (File.Exists(path))
            {
                byte[] contents = await File.ReadAllBytesAsync(path, token).ConfigureAwait(false);
                if (matches is null) return contents;
                try
                {
                    using var document = JsonDocument.Parse(contents);
                    if (matches(document.RootElement)) return contents;
                }
                catch (JsonException)
                {
                    // The initial bootstrap receipt can be read while it is still being written.
                }
            }

            await Task.Delay(250, token).ConfigureAwait(false);
        }

        throw new InvalidOperationException("The SDK did not write its expected committed or bootstrap receipt.");
    }

    private static string SerializeDigest<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, Json)));

    private static bool SameState(SupportedProjectReadStateSnapshot before, SupportedProjectReadStateSnapshot after)
        => before.Key == after.Key && before.ETag == after.ETag && before.Bytes.SequenceEqual(after.Bytes);

    private static void Require(bool condition, string reason) => Assert.True(condition, reason);
}
#endif
