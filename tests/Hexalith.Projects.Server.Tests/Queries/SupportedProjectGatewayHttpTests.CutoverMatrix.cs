#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Folders.Authorization;
using Hexalith.Folders.Contracts;
using Hexalith.Folders.Projections.TenantAccess;
using Hexalith.Projects.Client;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Infrastructure;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Projections.ConversationStartSetup;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Testing.Reads;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Hexalith.Folders;
using Hexalith.Folders.Aggregates.Folder;
using IUtcClock = Hexalith.Projects.Projections.TenantAccess.IUtcClock;

public sealed partial class SupportedProjectGatewayHttpTests
{
    private static async Task ObserveCutoverMatrixAsync(WebApplication gateway, WebApplication projects,
        WebApplication folders, WebApplication conversations, GatewayFixtureLocalRoutes routes,
        GatewayFixtureOwnerHeads heads, string tenant, DateTimeOffset now, ProjectionRequest firstHistory, string daprEndpoint)
    {
        var store = projects.Services.GetRequiredService<IReadModelStore>();
        var journal = projects.Services.GetRequiredService<IProjectProjectionStore>();
        var batch = projects.Services.GetRequiredService<IReadModelBatchStore>();
        var histories = new Dictionary<string, ProjectionRequest>(StringComparer.Ordinal);
        using var supported = new HttpClient { BaseAddress = new Uri(gateway.Urls.Single()) };
        using var legacy = new HttpClient { BaseAddress = new Uri(projects.Urls.Single()) };
        using var state = new HttpClient { BaseAddress = new Uri(daprEndpoint) };
        foreach (string id in new[] { "project-a", "project-b", "project-c" })
        {
            ProjectionRequest history = id == "project-a" ? firstHistory : SupportedProjectReadFixture.Request(id, id == "project-b" ? 200 : 250);
            history = history with { TenantId = tenant, Events = history.Events.Select(item => item with
                { Payload = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(item.Payload).Replace("tenant-a", tenant, StringComparison.Ordinal)) }).ToArray() };
            await SeedAsync(history).ConfigureAwait(false);

        }
        await SeedCutoverFoldersAsync(folders, heads, tenant, now, ["project-a", "project-b", "project-c"]).ConfigureAwait(false);
        // Each replay identity has ordinary Project read authority. The replay Tenant is separate
        // from the denied foreign-Tenant scenario, so an unpaged control can prove actual admission.
        string replayTenant = tenant + "-cursor-authority";
        var tenantStore = projects.Services.GetRequiredService<IProjectTenantAccessProjectionStore>();
        var replayAccess = (await tenantStore.GetAsync(tenant, TestContext.Current.CancellationToken).ConfigureAwait(false))!;
        replayAccess.Principals["cursor-actor"] = new("cursor-actor", "TenantOwner");
        await tenantStore.SaveAsync(replayAccess, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var otherTenant = new ProjectTenantAccessProjection { TenantId = replayTenant, Enabled = true, Watermark = 1,
            ProjectionWatermark = replayTenant + ":1", LastEventTimestamp = now };
        otherTenant.Principals["actor-1"] = new("actor-1", "TenantOwner");
        await tenantStore.SaveAsync(otherTenant, TestContext.Current.CancellationToken).ConfigureAwait(false);
        foreach (ProjectionRequest original in histories.Values)
        {
            var otherHistory = original with { TenantId = replayTenant, Events = original.Events.Select(item => item with
                { Payload = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(item.Payload).Replace(tenant, replayTenant, StringComparison.Ordinal)) }).ToArray() };
            await new ProjectReadProjectionHandler(store, batch).ProjectAsync(otherHistory, "cursor-authority-" + original.AggregateId,
                TestContext.Current.CancellationToken).ConfigureAwait(false);
        }
        object pendingCoverage = await PendingAdvisoryCoverageAsync().ConfigureAwait(false);
        var receipts = new List<Dictionary<string, object?>>();
        string? output = Environment.GetEnvironmentVariable("STORY67_MATRIX_OBSERVATIONS");
        JsonElement? rollbackConfiguration = LoadRealReceiptRoutingProof(output);
        var rollbackReads = new List<object>();
        foreach (string scenario in ProjectReadCutoverGate.Scenarios)
        {
            if (scenario == "partial")
            {
                var linked = new MemoryLinked(tenant, "project-a", "opaque-memory", new("Reference label"),
                    "actor-1", "source-correlation", "source-task", "source-memory", "source-memory", SupportedProjectReadFixture.EventTime);
                var current = histories["project-a"];
                var excludedSetup = new ProjectSetupUpdated(tenant, "project-a", new([], [], [], [ProjectContextSourceKind.Memory], null),
                    "actor-1", "source-setup", "source-setup", "source-setup", "source-setup", SupportedProjectReadFixture.EventTime);
                await SeedAsync(current with { Events = [.. current.Events, SupportedProjectReadFixture.Event(linked, 3, 102),
                    SupportedProjectReadFixture.Event(excludedSetup, 4, 103)] }).ConfigureAwait(false);
                await SetAdvisoryPendingAsync(true).ConfigureAwait(false);
            }
            if (scenario == "historical-pending-folderless")
            {
                var pending = SupportedProjectReadFixture.Request("historical-project", 300) with { TenantId = tenant };
                var created = JsonSerializer.Deserialize<ProjectCreated>(pending.Events[0].Payload, JsonSerializerOptions.Web)! with { TenantId = tenant };
                var historical = new ProjectFolderCreationPending(tenant, "historical-project", "Historical", "pending", true,
                    "actor-1", "source-historical", "source-historical", "source-historical", "source-historical", SupportedProjectReadFixture.EventTime);
                pending = pending with { Events = [SupportedProjectReadFixture.Event(created, 1, 300), SupportedProjectReadFixture.Event(historical, 2, 301)] };
                await SeedAsync(pending).ConfigureAwait(false);
                await SeedCutoverConversationAsync(conversations, tenant, "historical-conversation", "historical-project", now).ConfigureAwait(false);
            }
            foreach (string operation in ProjectReadCutoverGate.Operations)
            {
                var projectClock = (ProjectReadTestClock)projects.Services.GetRequiredService<TimeProvider>();
                var tenantClock = (ProjectReadTestClock)projects.Services.GetRequiredService<IUtcClock>();
                projectClock.Now = now.AddSeconds(1); tenantClock.Now = now.AddSeconds(1);
                string actor = scenario == "absent-denied" ? "denied-actor" : "actor-1";
                string requestTenant = scenario == "foreign-tenant" ? tenant + "-foreign" : tenant;
                string project = scenario == "historical-pending-folderless" ? "historical-project" : "project-a";
                string conversation = scenario == "conversation-binding" ? "unbound-conversation" : scenario == "historical-pending-folderless" ? "historical-conversation" : "bound-conversation";
                bool inspecting = operation is "list" or "open";
                bool unsupported = operation == "resolve-attachments" && scenario is "partial" or "unavailable";
                (string Type, string Target, object Payload, string Route) path = operation switch
                {
                    "list" => (ListProjectsQuery.QueryType, "projects", new ListProjectsQuery(InspectDescriptiveMetadata: true, IncludeQuarantined: scenario == "historical-pending-folderless"), "/api/v1/projects?inspectDescriptiveMetadata=true" + (scenario == "historical-pending-folderless" ? "&includeQuarantined=true" : "")),
                    "open" => (GetProjectQuery.QueryType, project, new GetProjectQuery(true, scenario == "historical-pending-folderless"), "/api/v1/projects/" + project + "?inspectDescriptiveMetadata=true" + (scenario == "historical-pending-folderless" ? "&inspectQuarantined=true" : "")),
                    "context" => (ProjectsServerModule.GetProjectContextQueryType, project, new GetProjectContextQuery(project) { ConversationId = conversation }, "/api/v1/projects/" + project + "/context?conversationId=" + conversation),
                    "explain" => (ProjectsServerModule.ExplainContextSelectionQueryType, project, new ExplainContextSelectionQuery(project) { ConversationId = conversation }, "/api/v1/projects/" + project + "/context/explain?conversationId=" + conversation),
                    "conversation-start" => (ProjectsServerModule.GetConversationStartSetupQueryType, project, new GetConversationStartSetupQuery(project) { ConversationId = conversation }, "/api/v1/projects/" + project + "/setup/conversation-start?conversationId=" + conversation),
                    "resolve-conversation" => (ResolveProjectFromConversationQuery.QueryType, "projects", new ResolveProjectFromConversationQuery(conversation), "/api/v1/projects/resolution/from-conversation?conversationId=" + conversation),
                    _ => (ResolveProjectFromAttachmentsQuery.QueryType, "projects", new ResolveProjectFromAttachmentsQuery(scenario == "partial"
                        ? [new("folder", "folder-project-a"), new("file", "opaque-file")]
                        : [new(unsupported ? "file" : "folder", unsupported ? "opaque-file" : "folder-project-a")]),
                        "/api/v1/projects/resolution/from-attachments?" + (scenario == "partial" ? "folderId=folder-project-a&fileId=opaque-file" : unsupported ? "fileId=opaque-file" : "folderId=folder-project-a")),
                };
                var (type, target, payload, route) = path;
                supported.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(requestTenant, inspection: true, actorId: actor, projectRole: "TenantProjectAdministrator"));
                legacy.DefaultRequestHeaders.Authorization = supported.DefaultRequestHeaders.Authorization;
                supported.DefaultRequestHeaders.Remove("X-Correlation-Id"); legacy.DefaultRequestHeaders.Remove("X-Correlation-Id");
                string correlation = ByteAether.Ulid.Ulid.New().ToString();
                supported.DefaultRequestHeaders.Add("X-Correlation-Id", correlation); legacy.DefaultRequestHeaders.Add("X-Correlation-Id", correlation);
                if (scenario == "stale-authority") { projectClock.Now = now.AddMinutes(10); tenantClock.Now = now.AddMinutes(10); }
                routes.OwnerStatusOverride = scenario == "unavailable" ? HttpStatusCode.ServiceUnavailable : null;
                var observedStore = projects.Services.GetRequiredService<ProjectMcpCliReadStore>();
                observedStore.FailAuditWrites = scenario == "descriptive-audit" && inspecting;
                routes.TransportResponses.Clear();
                Dictionary<string, string> before = await SnapshotAsync().ConfigureAwait(false);
                var stores = new[] { (Name: "projects", Store: observedStore),
                    (Name: "folders", Store: folders.Services.GetRequiredService<ProjectMcpCliReadStore>()),
                    (Name: "conversations", Store: conversations.Services.GetRequiredService<ProjectMcpCliReadStore>()) };
                foreach (var observed in stores) observed.Store.Writes.Clear();
                string[] UnexpectedWrites() => stores.SelectMany(observed => observed.Store.Writes.Keys
                    .Where(key => observed.Name != "projects" || !key.StartsWith("projects:read-audit:v1:", StringComparison.Ordinal))
                    .Select(key => observed.Name + ":" + key)).ToArray();
                var request = new SubmitQueryRequest(requestTenant, "projects", target, type,
                    Payload: JsonSerializer.SerializeToElement(payload, payload.GetType(), JsonSerializerOptions.Web));
                using var supportedResponse = await supported.PostAsJsonAsync("/api/v1/queries", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
                string supportedRaw = await supportedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                using var legacyResponse = await legacy.GetAsync(route, TestContext.Current.CancellationToken).ConfigureAwait(false);
                string legacyRaw = await legacyResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                JsonElement supportedBody = MatrixPayload(supportedRaw, supportedResponse.IsSuccessStatusCode);
                JsonElement legacyBody = MatrixPayload(legacyRaw, false);
                string[] unexpectedWrites = UnexpectedWrites();
                bool unchanged = unexpectedWrites.Length == 0 && (await SnapshotAsync().ConfigureAwait(false)).OrderBy(item => item.Key).SequenceEqual(before.OrderBy(item => item.Key));
                bool sourcePositions = await SourcePositionsAsync().ConfigureAwait(false);
                bool snapshots = MatrixSupportedAssertions(operation, supportedBody, supportedResponse.StatusCode, histories, projectClock.Now, project)
                    && MatrixAdmissionAssertions(operation, scenario, supportedBody, supportedResponse.StatusCode, histories, project, projectClock.Now);
                bool isolation = scenario is not ("absent-denied" or "foreign-tenant")
                    || supportedResponse.StatusCode == HttpStatusCode.NotFound && legacyResponse.StatusCode == HttpStatusCode.NotFound
                        && !supportedRaw.Contains("Project project-", StringComparison.Ordinal) && !legacyRaw.Contains("Project project-", StringComparison.Ordinal);
                bool cursorProof = scenario != "cursor-scope-continuation-expiry";
                object legacyCursorEvidence = new { transport = "retained-v1", pagingRequested = false,
                    hasCursorProperty = legacyBody.TryGetProperty("cursor", out var oldCursor), actualCursor = oldCursor.ValueKind == JsonValueKind.Undefined ? (JsonElement?)null : oldCursor };
                object supportedCursorEvidence = new { transport = "supported-Gateway", pagingRequested = request.Paging is not null,
                    hasCursorProperty = supportedBody.TryGetProperty("cursor", out var newCursor), actualCursor = newCursor.ValueKind == JsonValueKind.Undefined ? (JsonElement?)null : newCursor };
                if (scenario == "cursor-scope-continuation-expiry")
                {
                    (cursorProof, legacyCursorEvidence, supportedCursorEvidence) = await CursorControlsAsync(operation, request, route).ConfigureAwait(false);
                }
                object? missingTargetEvidence = null;
                bool missingTargetProof = true;
                if (scenario == "absent-denied")
                    (missingTargetProof, missingTargetEvidence) = await MissingTargetControlsAsync(operation, request, route).ConfigureAwait(false);
                var sourceSetup = await store.GetAsync<Hexalith.Projects.Projections.ProjectDetail.ProjectDetailItem>(
                    ConversationStartSetupProjectionHandler.StoreName, tenant + ":projects:" + project, TestContext.Current.CancellationToken).ConfigureAwait(false);
                bool emptySetup = sourceSetup.Value is { Setup: null } && !string.IsNullOrEmpty(sourceSetup.ETag)
                    && histories.TryGetValue(project, out var setupHistory) && setupHistory.Events.All(item => !item.EventTypeName.EndsWith("ProjectSetupUpdated", StringComparison.Ordinal));
                var sourceAuthority = await new Hexalith.Projects.Authorization.TenantAccessAuthorizer(
                    projects.Services.GetRequiredService<IProjectTenantAccessProjectionStore>(), tenantClock,
                    new Hexalith.Projects.Authorization.TenantAccessOptions()).AuthorizeDiagnosticReadAsync(
                        new(requestTenant, actor, null), TestContext.Current.CancellationToken).ConfigureAwait(false);
                bool currentAuthority = sourceAuthority.IsAllowed && sourceAuthority.FreshnessStatus == Hexalith.Projects.Authorization.TenantProjectionFreshnessStatus.Fresh
                    && sourceSetup.Value?.TenantId == requestTenant && sourceSetup.Value.ProjectId == project;
                using var nativeTransport = new HttpClient(new GatewayFixtureLoopbackHandler(routes)) { BaseAddress = new Uri("http://conversations") };
                nativeTransport.DefaultRequestHeaders.Authorization = supported.DefaultRequestHeaders.Authorization;
                var nativeClient = new Hexalith.Conversations.Client.ConversationClient(nativeTransport);
                var nativeRead = await nativeClient.GetConversationAsync(new(Hexalith.Conversations.Contracts.Versioning.SchemaVersion.Current,
                    new(requestTenant), actor, correlation, new(conversation)) { NativeMembershipOnly = true }, TestContext.Current.CancellationToken).ConfigureAwait(false);
                var native = nativeRead.Value;
                bool nativeMember = nativeRead.IsSuccess && native?.ReadPurpose == Hexalith.Conversations.Contracts.Queries.GetConversationQuery.NativeMembershipPurpose
                    && native.FreshnessState == Hexalith.Conversations.Contracts.TrustStates.ProjectionTrustState.Current
                    && native.Details is { } membership && membership.TenantId.Value == requestTenant && membership.ConversationId.Value == conversation
                    && membership.ProjectId?.Value == project && membership.Freshness.FreshnessState == Hexalith.Conversations.Contracts.TrustStates.ProjectionTrustState.Current;
                bool pendingFolder = sourceSetup.Value?.ProjectFolder is { FolderId: null, ReferenceState: Hexalith.Projects.Contracts.Ui.ReferenceState.Pending }
                    && histories[project].Events.Any(item => item.EventTypeName == typeof(ProjectFolderCreationPending).FullName);
                // Check the whole request sequence, including cursor continuations/rejections.
                bool canonical = MatrixCanonical(operation, legacyBody, supportedBody, legacyResponse.StatusCode, supportedResponse.StatusCode,
                    correlation, histories, projectClock.Now, emptySetup, out var legacyCanonical, out var supportedCanonical);
                bool transport = supportedResponse.Headers.TryGetValues("X-Correlation-Id", out var supportedCorrelation)
                    && supportedCorrelation.Single() == correlation && legacyResponse.Headers.TryGetValues("X-Correlation-Id", out var legacyCorrelation)
                    && legacyCorrelation.Single() == correlation;
                if (supportedResponse.IsSuccessStatusCode)
                    transport &= JsonDocument.Parse(supportedRaw).RootElement.GetProperty("correlationId").GetString() == correlation;
                bool auditCorrelation = true;
                object? auditEvidence = null;
                if (scenario == "complete" && inspecting)
                {
                    string repeatedCorrelation = ByteAether.Ulid.Ulid.New().ToString();
                    supported.DefaultRequestHeaders.Remove("X-Correlation-Id"); legacy.DefaultRequestHeaders.Remove("X-Correlation-Id");
                    supported.DefaultRequestHeaders.Add("X-Correlation-Id", repeatedCorrelation); legacy.DefaultRequestHeaders.Add("X-Correlation-Id", repeatedCorrelation);
                    using var repeatedSupported = await supported.PostAsJsonAsync("/api/v1/queries", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
                    using var repeatedLegacy = await legacy.GetAsync(route, TestContext.Current.CancellationToken).ConfigureAwait(false);
                    auditCorrelation = repeatedSupported.StatusCode == HttpStatusCode.OK && repeatedLegacy.StatusCode == HttpStatusCode.OK
                        && repeatedSupported.Headers.GetValues("X-Correlation-Id").Single() == repeatedCorrelation
                        && repeatedLegacy.Headers.GetValues("X-Correlation-Id").Single() == repeatedCorrelation;
                    string[] auditKeys = observedStore.Writes.Keys.Where(key => key.StartsWith("projects:read-audit:v1:", StringComparison.Ordinal)).ToArray();
                    auditCorrelation &= auditKeys.Length == 4;
                    foreach (string key in auditKeys)
                    {
                        var audit = await store.GetAsync<Hexalith.Projects.Server.Queries.ProjectReadAuditRecord>("statestore", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
                        auditCorrelation &= audit.Value is { Outcome: "Inspection" } && audit.ETag is { Length: > 0 }
                            && audit.Value.RecordedAt == projectClock.Now && key == "projects:read-audit:v1:" + audit.Value.RequestSurrogate
                            && audit.Value.ProjectCount == (operation == "list" ? 3 : 1)
                            && audit.Value.FieldClass == (operation == "list" ? "ProjectName" : "ProjectNameDescriptionSetupReferenceLabels");
                    }
                    auditEvidence = new { executed = true, passed = auditCorrelation, originalCorrelation = correlation,
                        repeatedCorrelation, durableDistinctRequestSurrogates = auditKeys, identicalRequestExceptCorrelation = true };
                    supported.DefaultRequestHeaders.Remove("X-Correlation-Id"); legacy.DefaultRequestHeaders.Remove("X-Correlation-Id");
                    supported.DefaultRequestHeaders.Add("X-Correlation-Id", correlation); legacy.DefaultRequestHeaders.Add("X-Correlation-Id", correlation);
                }
                if (supportedResponse.IsSuccessStatusCode)
                {
                    var envelope = Read<SubmitQueryResponse>(JsonDocument.Parse(supportedRaw).RootElement);
                    transport &= envelope.Success && envelope.ErrorMessage is null && envelope.CorrelationId == correlation
                        && envelope.Metadata is { ETag: null, IsNotModified: null, IsStale: null, IsDegraded: null, ProjectionVersion: null,
                            Paging: null, WarningCodes: null, Provenance: QueryResponseProvenance.HandlerComputed, Lifecycle: ProjectionLifecycleState.Unknown }
                        && supportedResponse.Headers.GetValues("X-Hexalith-Query-Provenance").Single() == "HandlerComputed"
                        && envelope.Metadata.ServedAt?.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
                            == supportedResponse.Headers.GetValues("X-Hexalith-Served-At").Single();
                }
                object? legacySetupHeaders = null;
                if (operation == "conversation-start" && legacyResponse.IsSuccessStatusCode)
                {
                    string actualWatermark = legacyResponse.Headers.GetValues("X-Hexalith-Projection-Watermark").Single();
                    string actualFreshness = legacyResponse.Headers.GetValues("X-Hexalith-Freshness").Single();
                    long expectedGlobalPosition = histories[project].Events.Max(item => item.GlobalPosition);
                    transport &= actualWatermark == expectedGlobalPosition.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        && actualFreshness == Read<ConversationStartSetup>(legacyBody).Freshness.ToString();
                    legacySetupHeaders = new { projectionWatermark = actualWatermark, freshness = actualFreshness, expectedOriginalGlobalPosition = expectedGlobalPosition };
                }
                transport &= auditCorrelation;
                string? approvedPair = scenario == "historical-pending-folderless" && operation is "context" or "explain" or "conversation-start" ? "pending-folder-materialization"
                    : scenario == "stale-authority" ? "stale-tenant"
                    : unsupported ? "unsupported-file" : (scenario == "unavailable" && operation.StartsWith("resolve-", StringComparison.Ordinal) || scenario == "partial" && operation == "resolve-conversation") ? "degraded-advisory"
                    : scenario is "conversation-binding" or "unavailable" && operation is "context" or "explain" or "conversation-start" ? "native-binding-unavailable" : null;
                bool causeProof = approvedPair switch
                {
                    "native-binding-unavailable" => currentAuthority && !nativeMember && !pendingFolder,
                    "pending-folder-materialization" => currentAuthority && nativeMember && pendingFolder,
                    "stale-tenant" => sourceAuthority.IsAllowed && sourceAuthority.FreshnessStatus == Hexalith.Projects.Authorization.TenantProjectionFreshnessStatus.Stale,
                    "unsupported-file" => currentAuthority && unsupported,
                    "degraded-advisory" => currentAuthority && (routes.OwnerStatusOverride == HttpStatusCode.ServiceUnavailable || scenario == "partial" && await IsAdvisoryPendingAsync().ConfigureAwait(false)),
                    null => true,
                    _ => false,
                };
                if (approvedPair is not null)
                    canonical |= causeProof && ReadCutoverComparator.IsApprovedUnavailablePair(operation, approvedPair, (int)legacyResponse.StatusCode,
                        legacyBody, (int)supportedResponse.StatusCode, supportedBody, isolation && sourcePositions && unchanged, snapshots);
                unexpectedWrites = UnexpectedWrites();
                unchanged &= unexpectedWrites.Length == 0 && (await SnapshotAsync().ConfigureAwait(false)).OrderBy(item => item.Key).SequenceEqual(before.OrderBy(item => item.Key));
                long[] globals = histories.Values.Select(history => history.Events.Max(item => item.GlobalPosition)).ToArray();
                long[] sequences = histories.Values.Select(history => history.Events.Max(item => item.SequenceNumber)).ToArray();
                var legacyStored = new List<Hexalith.Projects.Projections.ProjectDetail.ProjectDetailItem>();
                var supportedStored = new List<ProjectReadModel>();
                foreach (string id in histories.Keys)
                {
                    legacyStored.Add((await journal.GetDetailAsync(tenant, id, TestContext.Current.CancellationToken).ConfigureAwait(false))!);
                    supportedStored.Add((await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName,
                        ProjectReadModelKeys.Detail(tenant, id), TestContext.Current.CancellationToken).ConfigureAwait(false)).Value!);
                }
                string[] oldKeys = legacyStored.Select(row => row.TenantId + ":projects:" + row.ProjectId).ToArray();
                string[] currentKeys = supportedStored.Select(row => ProjectReadModelKeys.Detail(row.TenantId, row.ProjectId)).ToArray();
                string[] legacyIdentities = legacyStored.Select(row => new AggregateIdentity(row.TenantId, "projects", row.ProjectId).ActorId).ToArray();
                string[] supportedIdentities = supportedStored.Select(row => new AggregateIdentity(row.TenantId, "projects", row.ProjectId).ActorId).ToArray();
                string[] legacyAddresses = before.Keys.Where(key => key.StartsWith("statestore/projects:projection-journal:", StringComparison.Ordinal)).ToArray();
                string[] supportedAddresses = before.Keys.Where(key => key.StartsWith("statestore/projects:reads:v1:", StringComparison.Ordinal)
                    || key.StartsWith(ConversationStartSetupProjectionHandler.StoreName + "/", StringComparison.Ordinal)).ToArray();
                // Physical containers and logical aggregate keys are distinct; both identities come from independently read stored records.
                var oldObservation = new ReadCutoverObservation(legacyCanonical, oldKeys, legacyIdentities, legacyStored.Select(row => row.Sequence).ToArray(),
                    JsonSerializer.SerializeToElement(legacyCursorEvidence), true)
                    { PhysicalAddresses = legacyAddresses, TransportCursorAssertions = cursorProof, SourceGlobalPositions = globals, SourceSequenceNumbers = sequences };
                var currentObservation = new ReadCutoverObservation(supportedCanonical, currentKeys, supportedIdentities, supportedStored.Select(row => row.GlobalPosition).ToArray(),
                    JsonSerializer.SerializeToElement(supportedCursorEvidence), snapshots)
                    { PhysicalAddresses = supportedAddresses, TransportCursorAssertions = cursorProof, SourceGlobalPositions = globals, SourceSequenceNumbers = sequences, ProjectVersions = supportedStored.Select(row => row.Sequence).ToArray() };
                IReadOnlyList<string> comparisonDifferences = ReadCutoverComparator.Compare(oldObservation, currentObservation);
                // A finite explicitly authorized wire pair is represented only after its complete independent proof.
                if (canonical && approvedPair is not null)
                    comparisonDifferences = comparisonDifferences.Where(reason => reason != "canonical-output").ToArray();
                canonical &= comparisonDifferences.Count == 0 && causeProof;
                var cell = new Dictionary<string, object?>
                {
                    ["operation"] = operation, ["scenario"] = scenario, ["correlationId"] = correlation, ["executed"] = true, ["skipped"] = false,
                    ["passed"] = canonical && sourcePositions && snapshots && cursorProof && isolation && unchanged && transport && missingTargetProof,
                    ["partialEvidence"] = scenario == "partial" ? operation == "list" ? "Partial row component; complete enumeration" : operation.StartsWith("resolve-", StringComparison.Ordinal) ? "Incomplete observed coverage" : "Partial admission with original Setup exclusion" : null,
                    ["canonicalOutput"] = canonical, ["legacyCanonical"] = legacyCanonical, ["supportedCanonical"] = supportedCanonical,
                    ["legacyConversationStartHeaders"] = legacySetupHeaders, ["transportCorrelation"] = transport, ["causeSourceProof"] = causeProof,
                    ["currentProjectAuthority"] = currentAuthority, ["nativeMembershipRead"] = new { status = nativeRead.StatusCode,
                        native?.ReadPurpose, nativeMember, native?.Details?.ProjectId, native?.FreshnessState },
                    ["pendingFolderSourceProof"] = pendingFolder, ["currentEmptySetupSourceProof"] = emptySetup, ["independentKeys"] = sourcePositions, ["exactPositions"] = sourcePositions,
                    ["cursorScope"] = cursorProof, ["isolation"] = isolation, ["supportedOnlyAssertions"] = snapshots,
                    ["noDomainWrites"] = unchanged, ["unexpectedSdkWriteKeys"] = unexpectedWrites, ["legacyStatus"] = (int)legacyResponse.StatusCode, ["supportedStatus"] = (int)supportedResponse.StatusCode,
                    ["legacyRawResponse"] = legacyRaw, ["supportedRawResponse"] = supportedRaw, ["approvedOutcomePair"] = approvedPair,
                    ["domainBytesAndEtagsBefore"] = before, ["legacyCursorEvidence"] = legacyCursorEvidence, ["supportedCursorEvidence"] = supportedCursorEvidence,
                    ["missingTargetProof"] = missingTargetProof, ["missingTargetEvidence"] = missingTargetEvidence,
                    ["ownerTransport"] = routes.TransportResponses.ToArray(),
                    ["auditCorrelationProof"] = auditEvidence,
                    ["auditWrites"] = observedStore.Writes.Keys.Count(key => key.StartsWith("projects:read-audit:v1:", StringComparison.Ordinal)),
                    ["comparisonDifferences"] = comparisonDifferences, ["rawLegacyKeys"] = legacyAddresses, ["rawSupportedKeys"] = supportedAddresses,
                    ["logicalLegacyAggregateKeys"] = oldKeys, ["logicalSupportedAggregateKeys"] = currentKeys,
                    ["legacyAggregateIdentities"] = legacyIdentities, ["supportedAggregateIdentities"] = supportedIdentities,
                    ["sourcePositions"] = histories.Select(item => new { projectId = item.Key,
                        globalPosition = item.Value.Events.Max(evt => evt.GlobalPosition), sequenceNumber = item.Value.Events.Max(evt => evt.SequenceNumber) }).ToArray(),
                    ["reason"] = canonical ? null : "unapproved-wire-output-difference-retained-without-discarding-fields",
                };
                receipts.Add(cell);
                if (scenario == "complete" && rollbackConfiguration is { } routing)
                {
                    Dictionary<string, string> rollbackBefore = await SnapshotAsync().ConfigureAwait(false);
                    foreach (var observed in stores) observed.Store.Writes.Clear();
                    // The explicit verified startup choice selects the retained transport for all seven reads.
                    RealReceiptSelection(routing, "LegacyRollback", "evidencePath", "evidenceHash").Mode.ShouldBe("LegacyRollback");
                    using var rolledBack = await legacy.GetAsync(route, TestContext.Current.CancellationToken).ConfigureAwait(false);
                    string rollbackRaw = await rolledBack.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                    rolledBack.StatusCode.ShouldBe(HttpStatusCode.OK);
                    rollbackRaw.ShouldBe(legacyRaw);
                    rolledBack.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(correlation);
                    string[] rollbackUnexpectedWrites = UnexpectedWrites();
                    Dictionary<string, string> rollbackAfter = await SnapshotAsync().ConfigureAwait(false);
                    bool rollbackReadOnly = rollbackUnexpectedWrites.Length == 0
                        && rollbackAfter.OrderBy(item => item.Key).SequenceEqual(rollbackBefore.OrderBy(item => item.Key));
                    rollbackReadOnly.ShouldBeTrue();
                    rollbackReads.Add(new { operation, mode = "LegacyRollback", route, executed = true, passed = true,
                        currentAuthority, nativeMembershipRequired = operation is "context" or "explain" or "conversation-start",
                        nativeMembershipCurrent = nativeMember, safeMetadata = canonical && transport, status = (int)rolledBack.StatusCode,
                        correlationId = correlation, rawResponse = rollbackRaw, noDomainWrites = rollbackReadOnly,
                        unexpectedSdkWriteKeys = rollbackUnexpectedWrites, domainBytesAndEtagsBefore = rollbackBefore,
                        domainBytesAndEtagsAfter = rollbackAfter });
                }
                if (!string.IsNullOrWhiteSpace(output))
                    await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { normalizationRevision = ReadCutoverComparator.NormalizationRevision,
                        transport = "authenticated-existing-Gateway-and-retained-v1-REST", cases = receipts,
                        pendingAdvisoryCoverage = pendingCoverage, originalSourceEnvelopes = histories.Values }, new JsonSerializerOptions(JsonSerializerOptions.Web) { WriteIndented = true }), TestContext.Current.CancellationToken).ConfigureAwait(false);
                unchanged.ShouldBeTrue("A read changed persisted source/projection bytes or ETags.");
                routes.OwnerStatusOverride = null; observedStore.FailAuditWrites = false;
            }
            if (scenario == "partial")
            {
                await SetAdvisoryPendingAsync(false).ConfigureAwait(false);
                var resetSetup = new ProjectSetupUpdated(tenant, "project-a", ProjectSetup.Empty,
                    "actor-1", "source-setup-reset", "source-setup-reset", "source-setup-reset", "source-setup-reset", SupportedProjectReadFixture.EventTime);
                var source = histories["project-a"];
                await SeedAsync(source with { Events = [.. source.Events, SupportedProjectReadFixture.Event(resetSetup, 5, 104)] }).ConfigureAwait(false);
            }
        }
        receipts.Count.ShouldBe(77);
        receipts.ShouldAllBe(cell => (bool)cell["passed"]!, "Every executed comparison cell must pass; raw diagnostics were retained before this assertion.");
        receipts.Select(cell => cell["operation"] + "/" + cell["scenario"]).Distinct(StringComparer.Ordinal).Count().ShouldBe(77);
        if (rollbackConfiguration is { } completedRouting)
        {
            rollbackReads.Count.ShouldBe(7);
            await File.WriteAllTextAsync(completedRouting.GetProperty("outputPath").GetString()!,
                JsonSerializer.Serialize(new { executed = true, passed = true, normalizationRevision = ProjectReadCutoverGate.NormalizationRevision,
                    supportedFromRealReceipt = "Supported", regressionRefusal = "Unavailable", immutableSelectionAfterRegression = "Supported",
                    explicitRollbackFromRealReceipt = "LegacyRollback", incompleteLegacySecurityRefusal = "Unavailable",
                    evidencePath = completedRouting.GetProperty("evidencePath").GetString(), evidenceHash = completedRouting.GetProperty("evidenceHash").GetString(),
                    reads = rollbackReads }, new JsonSerializerOptions(JsonSerializerOptions.Web) { WriteIndented = true }),
                TestContext.Current.CancellationToken).ConfigureAwait(false);
        }

        async Task<bool> IsAdvisoryPendingAsync()
        {
            string suffix = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tenant)))
                + ":" + Convert.ToHexStringLower(SHA256.HashData("bound-conversation"u8));
            return (await conversations.Services.GetRequiredService<IReadModelStore>().GetCompactedAsync<Hexalith.Conversations.Server.Projections.ConversationResolutionMetadataProof>(
                "statestore", "projection:conversations-resolution-proof:" + suffix, TestContext.Current.CancellationToken).ConfigureAwait(false)).Value?.Current?.IsPending == true;
        }

        async Task SetAdvisoryPendingAsync(bool pending)
        {
            string suffix = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tenant)))
                + ":" + Convert.ToHexStringLower(SHA256.HashData("bound-conversation"u8));
            var metadata = (await conversations.Services.GetRequiredService<IReadModelStore>().GetCompactedAsync<Hexalith.Conversations.Server.Projections.ConversationResolutionMetadataReadModel>(
                "statestore", "projection:conversations-resolution:" + suffix, TestContext.Current.CancellationToken).ConfigureAwait(false)).Value!;
            Type type = typeof(Hexalith.Conversations.Server.Projections.ConversationProjectionReadModelWriter).Assembly.GetType(
                "Hexalith.Conversations.Server.Projections.ConversationResolutionMetadataPersistence")!;
            object writer = Activator.CreateInstance(type, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, [conversations.Services.GetRequiredService<DaprReadModelStore>()], null)!;
            await ((Task)type.GetMethod(pending ? "MarkPendingAsync" : "CompleteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(writer, [metadata, TestContext.Current.CancellationToken])!).ConfigureAwait(false);
            (await IsAdvisoryPendingAsync().ConfigureAwait(false)).ShouldBe(pending);
        }

        async Task<object> PendingAdvisoryCoverageAsync()
        {
            string suffix = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tenant)))
                + ":" + Convert.ToHexStringLower(SHA256.HashData("bound-conversation"u8));
            var model = (await conversations.Services.GetRequiredService<IReadModelStore>()
                .GetCompactedAsync<Hexalith.Conversations.Server.Projections.ConversationResolutionMetadataReadModel>(
                    "statestore", "projection:conversations-resolution:" + suffix, TestContext.Current.CancellationToken).ConfigureAwait(false)).Value!;
            Type persistenceType = typeof(Hexalith.Conversations.Server.Projections.ConversationProjectionReadModelWriter).Assembly.GetType(
                "Hexalith.Conversations.Server.Projections.ConversationResolutionMetadataPersistence")!;
            object persistence = Activator.CreateInstance(persistenceType, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, [conversations.Services.GetRequiredService<DaprReadModelStore>()], null)!;
            await ((Task)persistenceType.GetMethod("MarkPendingAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(persistence, [model, TestContext.Current.CancellationToken])!).ConfigureAwait(false);
            Dictionary<string, string> before = await SnapshotAsync().ConfigureAwait(false);
            supported.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: true, projectRole: "TenantProjectAdministrator"));
            legacy.DefaultRequestHeaders.Authorization = supported.DefaultRequestHeaders.Authorization;
            var query = new SubmitQueryRequest(tenant, "projects", "projects", ResolveProjectFromConversationQuery.QueryType,
                Payload: JsonSerializer.SerializeToElement(new ResolveProjectFromConversationQuery("bound-conversation"), JsonSerializerOptions.Web));
            using var current = await supported.PostAsJsonAsync("/api/v1/queries", query, TestContext.Current.CancellationToken).ConfigureAwait(false);
            using var retained = await legacy.GetAsync("/api/v1/projects/resolution/from-conversation?conversationId=bound-conversation", TestContext.Current.CancellationToken).ConfigureAwait(false);
            string currentRaw = await current.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            string retainedRaw = await retained.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            using var richTransport = new HttpClient(new GatewayFixtureLoopbackHandler(routes)) { BaseAddress = new Uri("http://conversations") };
            richTransport.DefaultRequestHeaders.Authorization = supported.DefaultRequestHeaders.Authorization;
            var rich = await new Hexalith.Conversations.Client.ConversationClient(richTransport).GetConversationAsync(
                new(Hexalith.Conversations.Contracts.Versioning.SchemaVersion.Current, new(tenant), "actor-1", "pending-proof", new("bound-conversation")), TestContext.Current.CancellationToken).ConfigureAwait(false);
            bool richPresent = rich.IsSuccess && rich.Value?.ReadPurpose is null && rich.Value?.Details is
                { Label: "private-native-label", ProjectId.Value: "project-a" };
            richPresent.ShouldBeTrue();
            var currentResult = Read<ProjectAdvisoryResolutionResponse>(MatrixPayload(currentRaw, true));
            current.StatusCode.ShouldBe(HttpStatusCode.OK); retained.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            currentResult.Incomplete.ShouldBeTrue(); currentResult.Result.ShouldBeNull(); currentResult.Candidates.ShouldBeEmpty();
            currentRaw.ShouldNotContain("private-native-"); retainedRaw.ShouldNotContain("private-native-");
            bool unchanged = (await SnapshotAsync().ConfigureAwait(false)).OrderBy(item => item.Key).SequenceEqual(before.OrderBy(item => item.Key));
            unchanged.ShouldBeTrue();
            await ((Task)persistenceType.GetMethod("CompleteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(persistence, [model, TestContext.Current.CancellationToken])!).ConfigureAwait(false);
            return new { executed = true, passed = true, richNativeSourcePresent = richPresent, classifiedIsolatedSourcePending = true,
                legacyStatus = (int)retained.StatusCode, supportedStatus = (int)current.StatusCode,
                legacyRawResponse = retainedRaw, supportedRawResponse = currentRaw, noDomainWrites = unchanged,
                domainBytesAndEtagsBefore = before };
        }

        async Task SeedAsync(ProjectionRequest history)
        {
            foreach (ProjectionEventDto item in history.Events)
            {
                var metadata = new EventMetadata(history.AggregateId + "-" + item.SequenceNumber, history.AggregateId, "projects", tenant,
                    "projects", item.SequenceNumber, item.GlobalPosition, item.Timestamp, "source-correlation", "source-command", "actor-1", "v1", item.EventTypeName, 1, "json");
                await journal.AppendAsync(new EventEnvelope(metadata, item.Payload, null), TestContext.Current.CancellationToken).ConfigureAwait(false);
            }
            await new ProjectReadProjectionHandler(store, batch).ProjectAsync(history, "matrix-reads-" + history.AggregateId + "-" + history.Events.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
            await new ConversationStartSetupProjectionHandler(store).ProjectAsync(history, "matrix-start-" + history.AggregateId + "-" + history.Events.Length, TestContext.Current.CancellationToken).ConfigureAwait(false);
            histories[history.AggregateId] = history;
        }

        async Task<bool> SourcePositionsAsync()
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var persistedJournal = await projects.Services.GetRequiredService<IProjectsStateStore>().GetAsync<JsonElement>(
                "statestore", "projects:projection-journal:" + tenant, TestContext.Current.CancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(persistedJournal.ETag) || persistedJournal.Value.GetProperty("tenantId").GetString() != tenant) return false;
            var persistedIdentities = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonElement envelope in persistedJournal.Value.GetProperty("events").EnumerateArray())
            {
                byte[] payload = Convert.FromBase64String(envelope.GetProperty("payloadBase64").GetString()!);
                using var sourcePayload = JsonDocument.Parse(payload);
                string sourceTenant = sourcePayload.RootElement.GetProperty("tenantId").GetString()!;
                string sourceProject = sourcePayload.RootElement.GetProperty("projectId").GetString()!;
                if (sourceTenant != tenant || envelope.GetProperty("tenantId").GetString() != sourceTenant
                    || !histories.TryGetValue(sourceProject, out var original)
                    || !original.Events.Any(item => item.GlobalPosition == envelope.GetProperty("sequence").GetInt64()
                        && item.EventTypeName == envelope.GetProperty("eventTypeName").GetString() && item.Payload.SequenceEqual(payload))) return false;
                persistedIdentities.Add(new AggregateIdentity(sourceTenant, "projects", sourceProject).ActorId);
            }
            foreach (var source in histories)
            {
                string expectedKey = $"projects:reads:v1:{tenant.Length}:{tenant}:{source.Key.Length}:{source.Key}";
                if (ProjectReadModelKeys.Detail(tenant, source.Key) != expectedKey
                    || ConversationStartSetupProjectionHandler.Key(tenant, source.Key) != tenant + ":projects:" + source.Key) return false;
                var old = await journal.GetDetailAsync(tenant, source.Key, TestContext.Current.CancellationToken).ConfigureAwait(false);
                var current = (await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail(tenant, source.Key), TestContext.Current.CancellationToken).ConfigureAwait(false)).Value;
                if (old is null || current is null || old.TenantId != tenant || current.TenantId != tenant || current.ProjectId != source.Key
                    || old.Sequence != source.Value.Events.Max(item => item.GlobalPosition)
                    || current.GlobalPosition != source.Value.Events.Max(item => item.GlobalPosition)
                    || current.Sequence != source.Value.Events.Max(item => item.SequenceNumber)
                    || !persistedIdentities.Contains(new AggregateIdentity(old.TenantId, "projects", old.ProjectId).ActorId)
                    || !identities.Add(new AggregateIdentity(old.TenantId, "projects", old.ProjectId).ActorId)
                    || new AggregateIdentity(old.TenantId, "projects", old.ProjectId).ActorId != new AggregateIdentity(current.TenantId, "projects", current.ProjectId).ActorId) return false;
            }
            return true;
        }

        async Task<Dictionary<string, string>> SnapshotAsync()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var keys = new List<(string Store, string Key)> { ("statestore", "projects:projection-journal:" + tenant),
                ("statestore", ProjectReadModelKeys.Index(tenant)) };
            foreach (string id in histories.Keys)
            {
                keys.Add(("statestore", ProjectReadModelKeys.Detail(tenant, id)));
                keys.Add((ConversationStartSetupProjectionHandler.StoreName, ConversationStartSetupProjectionHandler.Key(tenant, id)));
            }
            string Encode(string input) => Convert.ToBase64String(Encoding.UTF8.GetBytes(input)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            keys.Add(("statestore", "projection:conversations:" + Encode(tenant) + ":" + Encode("bound-conversation")));
            keys.Add(("statestore", "projection:conversations-index:" + Encode(tenant)));
            keys.Add(("statestore", GatewayFixtureConversationTenants.Key(tenant)));
            keys.Add(("statestore", "projection:folders:tenant-access:" + tenant));
            foreach (string id in new[] { "project-a", "project-b", "project-c" })
                keys.Add(("statestore", "projection:folders:folder-permissions:" + tenant + ":folder-" + id));
            if (histories.ContainsKey("historical-project"))
                keys.Add(("statestore", "projection:conversations:" + Encode(tenant) + ":" + Encode("historical-conversation")));
            string metadataSuffix = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tenant)))
                + ":" + Convert.ToHexStringLower(SHA256.HashData("bound-conversation"u8));
            foreach (string prefix in new[] { "projection:conversations-resolution:", "projection:conversations-resolution-proof:", "projection:conversations-resolution-fence:" })
                keys.Add(("statestore", prefix + metadataSuffix));
            foreach (var pair in keys)
            {
                using var response = await state.GetAsync("/v1.0/state/" + pair.Store + "/" + Uri.EscapeDataString(pair.Key), TestContext.Current.CancellationToken).ConfigureAwait(false);
                response.StatusCode.ShouldBe(HttpStatusCode.OK, pair.Store + "/" + pair.Key + " must be a persisted source record");
                response.Headers.NonValidated.TryGetValues("ETag", out var etags).ShouldBeTrue(pair.Store + "/" + pair.Key);
                string etag = etags.ToString(); etag.ShouldNotBeNullOrWhiteSpace();
                byte[] bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                result[pair.Store + "/" + pair.Key] = Convert.ToHexString(SHA256.HashData(bytes)) + ":" + etag;
            }
            var nativeStore = conversations.Services.GetRequiredService<IReadModelStore>();
            await LogicalOwnerAsync<Hexalith.Conversations.Server.Projections.ConversationResolutionMetadataReadModel>("projection:conversations-resolution:" + metadataSuffix).ConfigureAwait(false);
            await LogicalOwnerAsync<Hexalith.Conversations.Server.Projections.ConversationResolutionMetadataProof>("projection:conversations-resolution-proof:" + metadataSuffix).ConfigureAwait(false);
            await LogicalOwnerAsync<Hexalith.Conversations.Server.Projections.ConversationResolutionMetadataProof>("projection:conversations-resolution-fence:" + metadataSuffix).ConfigureAwait(false);
            async Task LogicalOwnerAsync<T>(string key) where T : class
            {
                var value = await nativeStore.GetCompactedAsync<T>("statestore", key, TestContext.Current.CancellationToken).ConfigureAwait(false);
                value.Value.ShouldNotBeNull(); value.ETag.ShouldNotBeNullOrWhiteSpace();
                result["logical:owner:" + key] = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value.Value, JsonSerializerOptions.Web))) + ":" + value.ETag;
            }
            foreach (string id in histories.Keys)
            {
                var detail = await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail(tenant, id), TestContext.Current.CancellationToken).ConfigureAwait(false);
                detail.Value.ShouldNotBeNull(); detail.ETag.ShouldNotBeNullOrWhiteSpace();
                result["logical:project:" + id] = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(detail.Value, JsonSerializerOptions.Web))) + ":" + detail.ETag;
                var setup = await store.GetAsync<Hexalith.Projects.Projections.ProjectDetail.ProjectDetailItem>(ConversationStartSetupProjectionHandler.StoreName, tenant + ":projects:" + id, TestContext.Current.CancellationToken).ConfigureAwait(false);
                setup.Value.ShouldNotBeNull(); setup.ETag.ShouldNotBeNullOrWhiteSpace();
                result["logical:setup:" + id] = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(setup.Value, JsonSerializerOptions.Web))) + ":" + setup.ETag;
            }
            return result;
        }

        async Task<(bool Passed, object Legacy, object Supported)> CursorControlsAsync(string operation, SubmitQueryRequest request, string route)
        {
            using var legacyRejected = await legacy.GetAsync(route + "&cursor=unsupported-cursor", TestContext.Current.CancellationToken).ConfigureAwait(false);
            string legacyRejectedRaw = await legacyRejected.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            if (operation != "list")
            {
                using var rejected = await supported.PostAsJsonAsync("/api/v1/queries", request with { Paging = new QueryPagingOptions(Cursor: "unsupported-cursor") }, TestContext.Current.CancellationToken).ConfigureAwait(false);
                int expected = operation is "context" or "explain" or "conversation-start" ? 404 : 400;
                return ((int)rejected.StatusCode == expected && legacyRejected.StatusCode == HttpStatusCode.BadRequest,
                    new { transport = "retained-v1", pagingSupported = false, requestCursor = "unsupported-cursor", rejectedStatus = (int)legacyRejected.StatusCode, rawResponse = legacyRejectedRaw },
                    new { transport = "supported-Gateway", pagingSupported = false, requestCursor = "unsupported-cursor", expectedStatus = expected, rejectedStatus = (int)rejected.StatusCode });
            }
            using var legacyPage = await legacy.GetAsync(route, TestContext.Current.CancellationToken).ConfigureAwait(false);
            string legacyPageRaw = await legacyPage.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            JsonElement retained = MatrixPayload(legacyPageRaw, false);
            string[] expectedProjects = histories.Keys.Where(id => id != "historical-project").Order(StringComparer.Ordinal).ToArray();
            string[] legacyProjects = retained.GetProperty("items").EnumerateArray().Select(row => row.GetProperty("projectId").GetString()!).ToArray();
            bool legacyProof = legacyPage.StatusCode == HttpStatusCode.OK && legacyRejected.StatusCode == HttpStatusCode.BadRequest
                && legacyProjects.SequenceEqual(expectedProjects) && !retained.TryGetProperty("cursor", out _);
            var firstRequest = request with { Payload = JsonSerializer.SerializeToElement(new ListProjectsQuery(PageSize: 1, InspectDescriptiveMetadata: true), JsonSerializerOptions.Web) };
            var clock = (ProjectReadTestClock)projects.Services.GetRequiredService<TimeProvider>();
            DateTimeOffset expectedExpiry = clock.Now.AddMinutes(15);
            ProjectListPage first = await ReadAsync<ProjectListPage>(supported, firstRequest).ConfigureAwait(false);
            if (first.Cursor is null) return (false, new { legacyProof }, new { missingContinuation = true });
            QueryEnvelope transported = routes.ProjectQueries.Last(query => query.QueryType == ListProjectsQuery.QueryType);
            transported.TenantId.ShouldBe(tenant); transported.OriginalActorId.ShouldBe("actor-1");
            transported.AuthenticatedWorkloadId.ShouldBe("operator-web"); transported.DelegationId.ShouldBe("delegation-1");
            transported.IsDelegated.ShouldBeTrue(); transported.Scopes.ShouldBe(["projects.read", "projects.list"]); transported.Audience.ShouldBe([Audience]);
            string expectedScope = await ExpectedMatrixCursorScopeAsync().ConfigureAwait(false);
            string decoded = projects.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("Hexalith.Projects.ReadCursor.v1").Unprotect(first.Cursor);
            using var cursor = JsonDocument.Parse(decoded);
            using var position = JsonDocument.Parse(cursor.RootElement.GetProperty("position").GetString()!);
            string boundary = position.RootElement.GetProperty("projectId").GetString()!;
            DateTimeOffset expiry = position.RootElement.GetProperty("expiresAt").GetDateTimeOffset();
            bool exactScope = cursor.RootElement.GetProperty("queryType").GetString() == ListProjectsQuery.QueryType
                && cursor.RootElement.GetProperty("scope").GetString() == expectedScope && boundary == expectedProjects[0] && expiry == expectedExpiry;
            var continued = firstRequest with { Payload = JsonSerializer.SerializeToElement(new ListProjectsQuery(Cursor: first.Cursor, PageSize: 1, InspectDescriptiveMetadata: true), JsonSerializerOptions.Web) };
            var tenantClock = (ProjectReadTestClock)projects.Services.GetRequiredService<IUtcClock>();
            clock.Now = clock.Now.AddMinutes(1); tenantClock.Now = clock.Now;
            ProjectListPage second = await ReadAsync<ProjectListPage>(supported, continued).ConfigureAwait(false);
            second.HasMore.ShouldBeTrue(); second.Cursor.ShouldNotBeNull();
            string secondDecoded = projects.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("Hexalith.Projects.ReadCursor.v1").Unprotect(second.Cursor);
            using var secondCursor = JsonDocument.Parse(secondDecoded);
            using var secondPosition = JsonDocument.Parse(secondCursor.RootElement.GetProperty("position").GetString()!);
            DateTimeOffset continuedExpiry = secondPosition.RootElement.GetProperty("expiresAt").GetDateTimeOffset();
            continuedExpiry.ShouldBe(expiry);
            ProjectListPage third = await ReadAsync<ProjectListPage>(supported, firstRequest with
                { Payload = JsonSerializer.SerializeToElement(new ListProjectsQuery(Cursor: second.Cursor, PageSize: 1, InspectDescriptiveMetadata: true), JsonSerializerOptions.Web) }).ConfigureAwait(false);
            var authorityReplays = new List<object>();
            var originalBearer = supported.DefaultRequestHeaders.Authorization;
            try
            {
                foreach (string changedAuthority in new[] { "actor", "workload", "delegation", "tenant" })
                {
                    string currentTenant = changedAuthority == "tenant" ? replayTenant : tenant;
                    supported.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(currentTenant, inspection: true,
                        projectRole: "TenantProjectAdministrator", actorId: changedAuthority == "actor" ? "cursor-actor" : "actor-1",
                        workload: changedAuthority == "workload" ? "alternate-web" : "operator-web",
                        delegation: changedAuthority == "delegation" ? "cursor-delegation" : "delegation-1"));
                    using var control = await supported.PostAsJsonAsync("/api/v1/queries", firstRequest with { Tenant = currentTenant }, TestContext.Current.CancellationToken).ConfigureAwait(false);
                    string controlRaw = await control.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                    control.StatusCode.ShouldBe(HttpStatusCode.OK);
                    var controlPage = JsonSerializer.Deserialize<SubmitQueryResponse>(controlRaw, JsonSerializerOptions.Web)!;
                    controlPage.Payload.GetProperty("items").GetArrayLength().ShouldBe(1);
                    using var replay = await supported.PostAsJsonAsync("/api/v1/queries", continued with { Tenant = currentTenant }, TestContext.Current.CancellationToken).ConfigureAwait(false);
                    string replayRaw = await replay.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                    replay.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                    replayRaw.ShouldNotContain("Project project-");
                    QueryEnvelope authenticated = routes.ProjectQueries.Last(query => query.QueryType == ListProjectsQuery.QueryType);
                    authorityReplays.Add(new { changedAuthority, executed = true, passed = true,
                        controlStatus = (int)control.StatusCode, replayStatus = (int)replay.StatusCode, controlRaw, replayRaw,
                        authenticated.TenantId, authenticated.OriginalActorId, authenticated.AuthenticatedWorkloadId, authenticated.DelegationId });
                }
            }
            finally { supported.DefaultRequestHeaders.Authorization = originalBearer; }
            var rejectionStatuses = new Dictionary<string, int>();
            foreach (var altered in new Dictionary<string, ListProjectsQuery>
            {
                ["tamper"] = new(Cursor: first.Cursor + "tampered", PageSize: 1, InspectDescriptiveMetadata: true),
                ["pageSize"] = new(Cursor: first.Cursor, PageSize: 2, InspectDescriptiveMetadata: true),
                ["lifecycle"] = new("Archived", first.Cursor, 1, true),
                ["inspection"] = new(Cursor: first.Cursor, PageSize: 1),
                ["quarantine"] = new(Cursor: first.Cursor, PageSize: 1, InspectDescriptiveMetadata: true, IncludeQuarantined: true),
            })
            {
                using var response = await supported.PostAsJsonAsync("/api/v1/queries", firstRequest with { Payload = JsonSerializer.SerializeToElement(altered.Value, JsonSerializerOptions.Web) }, TestContext.Current.CancellationToken).ConfigureAwait(false);
                rejectionStatuses[altered.Key] = (int)response.StatusCode;
            }
            clock.Now = expiry.AddSeconds(1); tenantClock.Now = clock.Now;
            IProjectTenantAccessProjectionStore accessStore = new DaprProjectTenantAccessProjectionStore(projects.Services.GetRequiredService<DaprReadModelStore>());
            var access = (await accessStore.GetAsync(tenant, TestContext.Current.CancellationToken).ConfigureAwait(false))!;
            DateTimeOffset? prior = access.LastEventTimestamp; access.LastEventTimestamp = clock.Now;
            await accessStore.SaveAsync(access, TestContext.Current.CancellationToken).ConfigureAwait(false);
            Dictionary<string, string> expiryBefore = await SnapshotAsync().ConfigureAwait(false);
            var grantBefore = await projects.Services.GetRequiredService<DaprReadModelStore>().GetAsync<System.Text.Json.Nodes.JsonObject>("statestore", "projects:tenant-access:" + tenant, TestContext.Current.CancellationToken).ConfigureAwait(false);
            using var expired = await supported.PostAsJsonAsync("/api/v1/queries", continued, TestContext.Current.CancellationToken).ConfigureAwait(false);
            var grantAfter = await projects.Services.GetRequiredService<DaprReadModelStore>().GetAsync<System.Text.Json.Nodes.JsonObject>("statestore", "projects:tenant-access:" + tenant, TestContext.Current.CancellationToken).ConfigureAwait(false);
            bool expiryReadOnly = Same(grantBefore.Value, grantAfter.Value) && grantBefore.ETag == grantAfter.ETag
                && (await SnapshotAsync().ConfigureAwait(false)).OrderBy(item => item.Key).SequenceEqual(expiryBefore.OrderBy(item => item.Key));
            access = (await accessStore.GetAsync(tenant, TestContext.Current.CancellationToken).ConfigureAwait(false))!;
            access.LastEventTimestamp = prior; await accessStore.SaveAsync(access, TestContext.Current.CancellationToken).ConfigureAwait(false);
            clock.Now = now.AddSeconds(1); tenantClock.Now = clock.Now;
            bool supportedProof = exactScope && first.Items.Select(row => row.ProjectId).SequenceEqual(expectedProjects.Take(1))
                && second.Items.Select(row => row.ProjectId).SequenceEqual(expectedProjects.Skip(1).Take(1)) && second.HasMore
                && continuedExpiry == expiry && third.Items.Select(row => row.ProjectId).SequenceEqual(expectedProjects.Skip(2))
                && !third.HasMore && third.Cursor is null && authorityReplays.Count == 4
                && rejectionStatuses.Values.All(status => status == 400) && expired.StatusCode == HttpStatusCode.BadRequest && expiryReadOnly;
            return (legacyProof && supportedProof,
                new { transport = "retained-v1", pagingSupported = false, expectedProjects, actualProjects = legacyProjects,
                    status = (int)legacyPage.StatusCode, rawResponse = legacyPageRaw, rejectedStatus = (int)legacyRejected.StatusCode, rejectedRawResponse = legacyRejectedRaw, passed = legacyProof },
                new { transport = "supported-Gateway", decoded, expectedScope, exactScope, expectedProjects, boundary,
                    first = first.Items.Select(row => row.ProjectId), second = second.Items.Select(row => row.ProjectId), third = third.Items.Select(row => row.ProjectId),
                    endOfEnumeration = !third.HasMore && third.Cursor is null, secondDecoded, continuedExpiry, authorityReplays,
                    expectedExpiry, expiry, rejectionStatuses, expiredStatus = (int)expired.StatusCode, expiryReadOnly,
                    controlledFixtureAuthorityRefresh = true, grantETagBefore = grantBefore.ETag, grantETagAfter = grantAfter.ETag, passed = supportedProof });
        }

        async Task<string> ExpectedMatrixCursorScopeAsync()
        {
            var index = await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index(tenant), TestContext.Current.CancellationToken).ConfigureAwait(false);
            QueryCursorScope visibility = QueryCursorScope.Create();
            foreach (string id in index.Value!.ProjectIds)
            {
                var source = (await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail(tenant, id), TestContext.Current.CancellationToken).ConfigureAwait(false)).Value!;
                bool included = source.Detail!.ProjectFolder is { FolderId: not null, ReferenceState: Hexalith.Projects.Contracts.Ui.ReferenceState.Included };
                visibility.Add("project", source.ProjectId).Add("allowed", "True").Add("authorization", "eventstore_validator_test_allow")
                    .Add("folder", included ? "Included" : "Pending").Add("quarantined", (!included).ToString());
            }
            string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            return Hash(QueryCursorScope.Create().Add("tenant", tenant).Add("actor", "actor-1").Add("workload", "operator-web")
                .Add("delegation", "delegation-1").Add("delegated", "True").Add("scopes", JsonSerializer.Serialize(new[] { "projects.read", "projects.list" }))
                .Add("audience", JsonSerializer.Serialize(new[] { Audience })).Add("action", ProjectAuthorizationGate.ListProjectsAction)
                .Add("lifecycle", (string?)null).Add("pageSize", "1").Add("inspect", "True").Add("quarantine", "False")
                .Add("policy", "project-list-v1-50-200-15m").Add("authority", tenant + ":1").Add("eventStoreAuthority", "eventstore_validator_test_allow")
                .Add("eventStoreFreshness", "fresh").Add("indexETag", index.ETag).Add("visibility", Hash(visibility.Build())).AddProjectionWatermark(index.Value.GlobalPosition).Build());
        }

        async Task<(bool Passed, object Evidence)> MissingTargetControlsAsync(string operation, SubmitQueryRequest original, string originalRoute)
        {
            var savedSupported = supported.DefaultRequestHeaders.Authorization; var savedLegacy = legacy.DefaultRequestHeaders.Authorization;
            supported.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: true, projectRole: "TenantProjectAdministrator"));
            legacy.DefaultRequestHeaders.Authorization = supported.DefaultRequestHeaders.Authorization;
            try
            {
                object payload = operation switch
                {
                    "list" => new ListProjectsQuery("Archived", InspectDescriptiveMetadata: true),
                    "open" => new GetProjectQuery(true),
                    "context" => new GetProjectContextQuery("missing-project") { ConversationId = "bound-conversation" },
                    "explain" => new ExplainContextSelectionQuery("missing-project") { ConversationId = "bound-conversation" },
                    "conversation-start" => new GetConversationStartSetupQuery("missing-project") { ConversationId = "bound-conversation" },
                    "resolve-conversation" => new ResolveProjectFromConversationQuery("missing-conversation"),
                    _ => new ResolveProjectFromAttachmentsQuery([new("folder", "missing-folder")]),
                };
                string target = operation is "list" or "resolve-conversation" or "resolve-attachments" ? "projects" : "missing-project";
                string route = operation switch
                {
                    "list" => "/api/v1/projects?lifecycle=archived&inspectDescriptiveMetadata=true",
                    "resolve-conversation" => "/api/v1/projects/resolution/from-conversation?conversationId=missing-conversation",
                    "resolve-attachments" => "/api/v1/projects/resolution/from-attachments?folderId=missing-folder",
                    _ => originalRoute.Replace("project-a", "missing-project", StringComparison.Ordinal),
                };
                Dictionary<string, string> before = await SnapshotAsync().ConfigureAwait(false);
                using var current = await supported.PostAsJsonAsync("/api/v1/queries", original with { Tenant = tenant, AggregateId = target,
                    Payload = JsonSerializer.SerializeToElement(payload, payload.GetType(), JsonSerializerOptions.Web) }, TestContext.Current.CancellationToken).ConfigureAwait(false);
                using var retained = await legacy.GetAsync(route, TestContext.Current.CancellationToken).ConfigureAwait(false);
                string currentRaw = await current.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                string retainedRaw = await retained.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                JsonElement body = MatrixPayload(currentRaw, current.IsSuccessStatusCode); JsonElement old = MatrixPayload(retainedRaw, false);
                bool behavior = operation == "list" ? current.StatusCode == HttpStatusCode.OK && retained.StatusCode == HttpStatusCode.OK
                    && body.GetProperty("items").GetArrayLength() == 0 && old.GetProperty("items").GetArrayLength() == 0
                    && body.GetProperty("snapshot").GetProperty("responseState").GetString() == "Complete"
                    : operation == "resolve-attachments" ? current.StatusCode == HttpStatusCode.OK && retained.StatusCode == HttpStatusCode.OK
                        && body.GetProperty("candidates").GetArrayLength() == 0 && old.GetProperty("candidates").GetArrayLength() == 0
                        && body.GetProperty("result").GetString() == "NoMatch" && !body.GetProperty("incomplete").GetBoolean()
                    : current.StatusCode == HttpStatusCode.NotFound && retained.StatusCode == HttpStatusCode.NotFound;
                object? absentSourceEvidence = null;
                bool absentSource;
                if (operation is "resolve-conversation" or "resolve-attachments")
                {
                    string Encode(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                    string key = operation == "resolve-conversation" ? "projection:conversations:" + Encode(tenant) + ":" + Encode("missing-conversation")
                        : "projection:folders:folder-permissions:" + tenant + ":missing-folder";
                    using var absent = await state.GetAsync("/v1.0/state/statestore/" + Uri.EscapeDataString(key), TestContext.Current.CancellationToken).ConfigureAwait(false);
                    absentSource = absent.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound;
                    absentSourceEvidence = new { store = "statestore", key, status = (int)absent.StatusCode };
                }
                else if (operation == "list")
                {
                    var index = await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index(tenant), TestContext.Current.CancellationToken).ConfigureAwait(false);
                    absentSource = index.Value is not null && !string.IsNullOrEmpty(index.ETag);
                    foreach (string id in index.Value!.ProjectIds)
                        absentSource &= (await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail(tenant, id), TestContext.Current.CancellationToken).ConfigureAwait(false)).Value?.Detail?.Lifecycle != Hexalith.Projects.Contracts.Ui.ProjectLifecycle.Archived;
                    absentSourceEvidence = new { indexETag = index.ETag, currentCollectionPresent = true, noArchivedRows = absentSource };
                }
                else
                {
                    var missing = await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail(tenant, "missing-project"), TestContext.Current.CancellationToken).ConfigureAwait(false);
                    absentSource = await journal.GetDetailAsync(tenant, "missing-project", TestContext.Current.CancellationToken).ConfigureAwait(false) is null && missing.Value is null;
                    absentSourceEvidence = new { legacyJournalFoldMissing = absentSource, supportedKey = ProjectReadModelKeys.Detail(tenant, "missing-project"), missing.ETag };
                }
                var authority = await new Hexalith.Projects.Authorization.TenantAccessAuthorizer(projects.Services.GetRequiredService<IProjectTenantAccessProjectionStore>(),
                    projects.Services.GetRequiredService<IUtcClock>(), new Hexalith.Projects.Authorization.TenantAccessOptions()).AuthorizeDiagnosticReadAsync(new(tenant, "actor-1", null), TestContext.Current.CancellationToken).ConfigureAwait(false);
                bool currentAuthority = authority.IsAllowed && authority.FreshnessStatus == Hexalith.Projects.Authorization.TenantProjectionFreshnessStatus.Fresh;
                string[] unexpected = new[] { projects, folders, conversations }.SelectMany(app => app.Services.GetRequiredService<ProjectMcpCliReadStore>().Writes.Keys
                    .Where(key => !ReferenceEquals(app, projects) || !key.StartsWith("projects:read-audit:v1:", StringComparison.Ordinal))).ToArray();
                bool readOnly = unexpected.Length == 0 && (await SnapshotAsync().ConfigureAwait(false)).OrderBy(item => item.Key).SequenceEqual(before.OrderBy(item => item.Key));
                string correlation = supported.DefaultRequestHeaders.GetValues("X-Correlation-Id").Single();
                bool safeContract = MatrixCanonical(operation, old, body, retained.StatusCode, current.StatusCode, correlation, histories,
                    ((ProjectReadTestClock)projects.Services.GetRequiredService<TimeProvider>()).Now, false, out _, out _)
                    && current.Headers.GetValues("X-Correlation-Id").Single() == correlation && retained.Headers.GetValues("X-Correlation-Id").Single() == correlation;
                bool supportedAssertions = MatrixSupportedAssertions(operation, body, current.StatusCode, histories,
                    ((ProjectReadTestClock)projects.Services.GetRequiredService<TimeProvider>()).Now, target)
                    && MatrixAdmissionAssertions(operation, "complete", body, current.StatusCode, histories, target,
                        ((ProjectReadTestClock)projects.Services.GetRequiredService<TimeProvider>()).Now);
                bool safe = !currentRaw.Contains("Project project-", StringComparison.Ordinal) && !retainedRaw.Contains("Project project-", StringComparison.Ordinal);
                return (behavior && absentSource && readOnly && safe && currentAuthority && safeContract && supportedAssertions,
                    new { executed = true, passed = behavior && absentSource && readOnly && safe && currentAuthority && safeContract && supportedAssertions,
                    canonicalAbsenceOrValidEmpty = safeContract, supportedOnlyAssertions = supportedAssertions, correlationId = correlation,
                    authenticatedActor = "actor-1", tenantAuthorityCurrent = currentAuthority, operation, target, route,
                    absenceKind = operation == "list" ? "existing-collection-valid-empty-Archived-filter" : "genuinely-missing-target",
                    supportedStatus = (int)current.StatusCode, legacyStatus = (int)retained.StatusCode, supportedRawResponse = currentRaw, legacyRawResponse = retainedRaw,
                    sourceAbsent = absentSource, sourceEvidence = absentSourceEvidence, noDomainWrites = readOnly,
                    unexpectedSdkWriteKeys = unexpected, domainBytesAndEtagsBefore = before });
            }
            finally { supported.DefaultRequestHeaders.Authorization = savedSupported; legacy.DefaultRequestHeaders.Authorization = savedLegacy; }
        }

    }

    private static async Task SeedCutoverFoldersAsync(WebApplication folders, GatewayFixtureOwnerHeads heads,
        string tenant, DateTimeOffset now, IReadOnlyList<string> projects)
    {
        var folderTenant = new FolderTenantAccessProjection { TenantId = tenant, Enabled = true, Watermark = 1,
            ProjectionWatermark = tenant + ":1", LastEventTimestamp = now };
        folderTenant.Principals["actor-1"] = new("actor-1", "TenantOwner");
        var folderAccess = folders.Services.GetRequiredService<IFolderTenantAccessProjectionStore>();
        if (await folderAccess.GetAsync(tenant, TestContext.Current.CancellationToken).ConfigureAwait(false) is null)
            await folderAccess.SaveAsync(folderTenant, TestContext.Current.CancellationToken).ConfigureAwait(false);
        foreach (string id in projects)
        {
            string folder = "folder-" + id;
            var owner = folders.Services.GetRequiredService<EventStoreFolderEffectivePermissionsReadModel>();
            await owner.ApplyAsync(tenant, folder, "created-" + id, 1, "created-" + id,
                new FolderCreated(tenant, tenant, folder, "Owner label", null, null, [], FolderLifecycleState.Active, FolderRepositoryBindingState.Unbound,
                    "actor-1", "corr", "task", "create", "digest", now), TestContext.Current.CancellationToken).ConfigureAwait(false);
            await owner.ApplyAsync(tenant, folder, "granted-" + id, 2, "granted-" + id,
                new FolderAccessGranted(tenant, tenant, folder, FolderAccessPrincipalKind.User,
                    "actor-1", "read_metadata", "actor-1", "corr", "task", "grant", "digest", 2, now), TestContext.Current.CancellationToken).ConfigureAwait(false);
            heads.Set(tenant, folder, 2);
            ((Hexalith.Folders.Queries.Folders.InMemoryFolderLifecycleStatusReadModel)folders.Services.GetRequiredService<Hexalith.Folders.Queries.Folders.IFolderLifecycleStatusReadModel>())
                .Save(new(tenant, folder, Hexalith.Folders.Queries.Folders.FolderLifecycleProjectionState.Active,
                    Hexalith.Folders.Queries.Folders.FolderRepositoryBindingStatus.Unbound, null, null,
                    new("eventually_consistent", now, "folder-source-2", false, null),
                    new(tenant, "actor-1", "read_metadata", null, null, null), []));
        }
    }

    private static JsonElement MatrixPayload(string raw, bool gatewayEnvelope)
    {
        using var document = JsonDocument.Parse(raw);
        return gatewayEnvelope && document.RootElement.TryGetProperty("payload", out var payload) ? payload.Clone() : document.RootElement.Clone();
    }

    private static bool MatrixSupportedAssertions(string operation, JsonElement payload, HttpStatusCode status, IReadOnlyDictionary<string, ProjectionRequest> histories, DateTimeOffset observedAt, string projectId)
    {
        if (status == HttpStatusCode.NotFound) return !payload.GetRawText().Contains("Project project-", StringComparison.Ordinal);
        if (status != HttpStatusCode.OK) return false;
        if (operation.StartsWith("resolve-", StringComparison.Ordinal))
            return payload.TryGetProperty("incomplete", out var incomplete) && incomplete.ValueKind is JsonValueKind.True or JsonValueKind.False
                && payload.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array
                && candidates.EnumerateArray().All(candidate => !candidate.TryGetProperty("displayName", out _) && !candidate.TryGetProperty("name", out _))
                && payload.TryGetProperty("unsupportedInputKinds", out var kinds) && kinds.ValueKind == JsonValueKind.Array;
        if (operation == "explain") payload = payload.GetProperty("context");
        if (!payload.TryGetProperty("snapshot", out var snapshot) || !snapshot.TryGetProperty("asOf", out var asOf)
            || !asOf.TryGetDateTimeOffset(out _) || !snapshot.TryGetProperty("projectVersion", out var version) || !version.TryGetInt64(out _)
            || !snapshot.TryGetProperty("components", out var components) || components.ValueKind != JsonValueKind.Array || components.GetArrayLength() == 0
            || !snapshot.TryGetProperty("recoveryActions", out var actions) || actions.ValueKind != JsonValueKind.Array) return false;
        if (operation == "list")
        {
            if (version.GetInt64() != 0) return false;
            foreach (var row in payload.GetProperty("items").EnumerateArray())
            {
                string id = row.GetProperty("projectId").GetString()!;
                if (!histories.TryGetValue(id, out var source) || row.GetProperty("snapshot").GetProperty("projectVersion").GetInt64() != source.Events.Max(item => item.SequenceNumber)) return false;
            }
        }
        else
        {
            string id = operation == "conversation-start" ? projectId : payload.GetProperty("projectId").GetString()!;
            if (!histories.TryGetValue(id, out var source) || version.GetInt64() != source.Events.Max(item => item.SequenceNumber)) return false;

        }
        if (asOf.GetDateTimeOffset() != observedAt) return false;
        return components.EnumerateArray().All(component => component.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String
            && component.TryGetProperty("freshness", out _) && component.TryGetProperty("included", out _) && component.TryGetProperty("reason", out _))
            && (!snapshot.TryGetProperty("responseState", out var state) || state.GetString() != "Unavailable"
                || operation is "list" or "open" || payload.GetProperty("setup").ValueKind == JsonValueKind.Null);
    }
}
#endif
