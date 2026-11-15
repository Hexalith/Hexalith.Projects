#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dapr.Client;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.McpCli.Abstractions;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectGatewayHttpTests
{
    private readonly List<string> _mcpCliSafeDenialBodies = [];
    private async Task VerifyMcpCliReadMatrixAsync(HttpClient client, WebApplication projects,
        ProjectMcpCliReadStore[] owners, GatewayFixtureLocalRoutes transport, GatewayFixtureOwnerHeads heads, string tenant,
        Func<Task<byte[][]>> storeBytes, Func<object, string, SubmitQueryRequest> webRequest)
    {
        ProjectMcpCliReadStore store = projects.Services.GetRequiredService<ProjectMcpCliReadStore>();
        async Task<JsonObject> ObservedSubmitAsync(SubmitQueryRequest request, HttpStatusCode status)
        {
            byte[][] persistedBefore = await storeBytes().ConfigureAwait(false);
            JsonObject result = await SubmitMcpCliMatrixAsync(client, request, status).ConfigureAwait(false);
            byte[][] persistedAfter = await storeBytes().ConfigureAwait(false);
            for (int i = 0; i < persistedBefore.Length; i++) persistedAfter[i].ShouldBe(persistedBefore[i]);
            foreach (ProjectMcpCliReadStore owner in owners) owner.Writes.ShouldBeEmpty();
            store.Writes.Keys.ShouldAllBe(key => key.StartsWith("projects:read-audit:v1:", StringComparison.Ordinal));
            return result;
        }
        async Task<JsonObject> EqualReadAsync(object input, HttpStatusCode status, string project = "project-a")
        {
            JsonObject cli = await ObservedSubmitAsync(McpCliRequest(tenant, input, project), status).ConfigureAwait(false);
            JsonObject web = await ObservedSubmitAsync(webRequest(input, project), status).ConfigureAwait(false);
            cli.ToJsonString().ShouldBe(web.ToJsonString());
            return cli;
        }
        async Task<JsonObject> InspectedReadAsync(object input, int count, string fieldClass)
        {
            HashSet<string> previous = store.Writes.Keys.ToHashSet(StringComparer.Ordinal);
            JsonObject result = await EqualReadAsync(input, HttpStatusCode.OK).ConfigureAwait(false);
            string[] added = store.Writes.Keys.Except(previous, StringComparer.Ordinal).ToArray();
            added.Length.ShouldBe(2); // One durable record per independently submitted CLI/Web read.
            foreach (string key in added)
            {
                ProjectReadAuditRecord audit = (await store.GetAsync<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName,
                    key, TestContext.Current.CancellationToken).ConfigureAwait(false)).Value!;
                audit.ShouldNotBeNull();
                audit.Outcome.ShouldBe("Inspection");
                audit.ProjectCount.ShouldBe(count);
                audit.FieldClass.ShouldBe(fieldClass);
                key.ShouldBe("projects:read-audit:v1:" + audit.RequestSurrogate);
            }
            return result;
        }
        byte[][] before = await storeBytes().ConfigureAwait(false);
        store.Writes.Clear();
        foreach (ProjectMcpCliReadStore owner in owners) { owner.Writes.Clear(); owner.Reads.Clear(); }
        _mcpCliSafeDenialBodies.Clear();
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false));
        foreach (object input in new object[]
        {
            new ListProjectsQuery(PageSize: 200), new GetProjectQuery(),
            new ResolveProjectFromConversationQuery("conversation-a"),
            new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-b")]),
            new ResolveProjectFromAttachmentsQuery([new("file", "unsupported-file")]),
        })
        {
            await EqualReadAsync(input, HttpStatusCode.OK).ConfigureAwait(false);
        }
        GetProjectResponse safe = (await EqualReadAsync(new GetProjectQuery(), HttpStatusCode.OK).ConfigureAwait(false))["payload"]!.Deserialize<GetProjectResponse>(JsonSerializerOptions.Web)!;
        safe.Name.ShouldBeNull(); safe.Setup.ShouldBeNull();
        JsonObject denied = await EqualReadAsync(new GetProjectQuery(true), HttpStatusCode.NotFound).ConfigureAwait(false);
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: true));
        JsonObject inspected = await InspectedReadAsync(new GetProjectQuery(true), 1, "ProjectNameDescriptionSetupReferenceLabels").ConfigureAwait(false);
        inspected["payload"]!["name"]!.GetValue<string>().ShouldBe("Project project-a");
        await InspectedReadAsync(new ListProjectsQuery(InspectDescriptiveMetadata: true), 2, "ProjectName").ConfigureAwait(false);
        store.FailAuditWrites = true;
        try
        {
            await EqualReadAsync(new GetProjectQuery(true), HttpStatusCode.NotFound).ConfigureAwait(false);
        }
        finally { store.FailAuditWrites = false; }
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false));
        JsonObject absent = await EqualReadAsync(new GetProjectQuery(), HttpStatusCode.NotFound, "missing").ConfigureAwait(false);
        JsonObject foreign = await EqualReadAsync(new GetProjectQuery(), HttpStatusCode.NotFound, "foreign-only-project").ConfigureAwait(false);
        absent.ToJsonString().ShouldBe(foreign.ToJsonString());
        absent.ToJsonString().ShouldBe(denied.ToJsonString());
        absent["detail"]!.GetValue<string>().ShouldBe("The requested resource was not found.");
        absent.ContainsKey("payload").ShouldBeFalse();
        await EqualReadAsync(new ResolveProjectFromConversationQuery("missing"), HttpStatusCode.NotFound).ConfigureAwait(false);
        // This caller has valid Project admission but no membership in the Conversation owner's tenant projection.
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false, actorId: "actor-without-conversation-access"));
        int conversationCalls = transport.TransportResponses.Count(item => item.StartsWith("conversations:query:", StringComparison.Ordinal));
        await EqualReadAsync(new ResolveProjectFromConversationQuery("conversation-a"), HttpStatusCode.NotFound).ConfigureAwait(false);
        transport.TransportResponses.Count(item => item.StartsWith("conversations:query:", StringComparison.Ordinal)).ShouldBe(conversationCalls + 2);
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false, projectRole: "Reader"));
        foreach (object input in new object[] { new ListProjectsQuery(), new ResolveProjectFromConversationQuery("conversation-a"),
            new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")]) })
        {
            await EqualReadAsync(input, HttpStatusCode.NotFound).ConfigureAwait(false);
        }
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false));
        foreach (object input in new object[] { new ListProjectsQuery(), new GetProjectQuery(),
            new ResolveProjectFromConversationQuery("conversation-a"), new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")]) })
        {
            JsonObject cliForbidden = await ObservedSubmitAsync(McpCliRequest(tenant + "-foreign", input), HttpStatusCode.Forbidden).ConfigureAwait(false);
            JsonObject webForbidden = await ObservedSubmitAsync(webRequest(input, "project-a") with { Tenant = tenant + "-foreign" }, HttpStatusCode.Forbidden).ConfigureAwait(false);
            cliForbidden.ContainsKey("payload").ShouldBeFalse();
            webForbidden.ContainsKey("payload").ShouldBeFalse();
            cliForbidden.ToJsonString().ShouldBe(webForbidden.ToJsonString());
        }
        _mcpCliSafeDenialBodies.Count.ShouldBeGreaterThanOrEqualTo(6);
        _mcpCliSafeDenialBodies.ShouldAllBe(body => body == _mcpCliSafeDenialBodies[0]);
        // Cursor bytes are opaque and independently randomized; prove equivalent continuations instead of equating tokens.
        ListProjectsQuery firstInput = new(PageSize: 1);
        JsonObject cliFirst = await ObservedSubmitAsync(McpCliRequest(tenant, firstInput), HttpStatusCode.OK).ConfigureAwait(false);
        JsonObject webFirst = await ObservedSubmitAsync(webRequest(firstInput, "projects"), HttpStatusCode.OK).ConfigureAwait(false);
        string cliCursor = cliFirst["payload"]!["cursor"].ShouldNotBeNull().GetValue<string>();
        string webCursor = webFirst["payload"]!["cursor"].ShouldNotBeNull().GetValue<string>();
        cliCursor.ShouldNotBeNullOrWhiteSpace(); webCursor.ShouldNotBeNullOrWhiteSpace();
        cliFirst["payload"]!["items"]![0]!["projectId"]!.GetValue<string>().ShouldBe("project-a");
        cliFirst["payload"]!.AsObject().Remove("cursor"); webFirst["payload"]!.AsObject().Remove("cursor");
        cliFirst.ToJsonString().ShouldBe(webFirst.ToJsonString());
        JsonObject cliNext = await ObservedSubmitAsync(McpCliRequest(tenant, firstInput with { Cursor = cliCursor }), HttpStatusCode.OK).ConfigureAwait(false);
        JsonObject webNext = await ObservedSubmitAsync(webRequest(firstInput with { Cursor = webCursor }, "projects"), HttpStatusCode.OK).ConfigureAwait(false);
        cliNext.ToJsonString().ShouldBe(webNext.ToJsonString());
        cliNext["payload"]!["items"]![0]!["projectId"]!.GetValue<string>().ShouldBe("project-b");
        cliNext["payload"]!["cursor"].ShouldBeNull();
        await ObservedSubmitAsync(McpCliRequest(tenant, firstInput with { Cursor = cliCursor, PageSize = 2 }), HttpStatusCode.BadRequest).ConfigureAwait(false);
        await ObservedSubmitAsync(webRequest(firstInput with { Cursor = webCursor, PageSize = 2 }, "projects"), HttpStatusCode.BadRequest).ConfigureAwait(false);
        client.DefaultRequestHeaders.Authorization = null;
        int ownerQueries = transport.OwnerQueries;
        int receiverQueries = transport.TransportResponses.Count;
        foreach (object input in new object[] { new ListProjectsQuery(), new GetProjectQuery(),
            new ResolveProjectFromConversationQuery("conversation-a"), new ResolveProjectFromAttachmentsQuery([new("folder", "folder-project-a")]) })
        {
            await EqualReadAsync(input, HttpStatusCode.Unauthorized).ConfigureAwait(false);
        }
        transport.OwnerQueries.ShouldBe(ownerQueries);
        transport.TransportResponses.Count.ShouldBe(receiverQueries);
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false, projectUser: true));
        heads.Outage = true;
        try
        {
            JsonObject unavailable = await EqualReadAsync(new ListProjectsQuery(), HttpStatusCode.OK).ConfigureAwait(false);
            unavailable["payload"]!.Deserialize<ProjectListPage>(JsonSerializerOptions.Web)!.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        }
        finally
        {
            heads.Outage = false;
            client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false));
        }
        byte[][] after = await storeBytes().ConfigureAwait(false);
        for (int i = 0; i < before.Length; i++) after[i].ShouldBe(before[i]);
        owners[0].Reads.Keys.ShouldContain(item => item.Key.StartsWith("projection:folders:folder-permissions:", StringComparison.Ordinal));
        owners[1].Reads.Keys.ShouldContain(item => item.Key.StartsWith("projection:conversations-resolution:", StringComparison.Ordinal));
        foreach (ProjectMcpCliReadStore owner in owners) owner.Writes.ShouldBeEmpty();
        store.Writes.Keys.ShouldNotBeEmpty();
        store.Writes.Keys.ShouldAllBe(key => key.StartsWith("projects:read-audit:v1:", StringComparison.Ordinal));
        foreach (string key in store.Writes.Keys)
        {
            ProjectReadAuditRecord record = (await store.GetAsync<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, key,
                TestContext.Current.CancellationToken).ConfigureAwait(false)).Value!;
            record.ShouldNotBeNull();
            new[] { "Inspection", "SafeDenial" }.ShouldContain(record.Outcome);
        }
        // Seed a separate optional-reference fixture before observing its persisted read delta.
        const string partialProject = "partial-project";
        string detailKey = ProjectReadModelKeys.Detail(tenant, partialProject);
        ProjectReadModel original = (await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName,
            ProjectReadModelKeys.Detail(tenant, "project-a"), TestContext.Current.CancellationToken).ConfigureAwait(false)).Value!;
        ProjectReadModel partialModel = original with
        {
            ProjectId = partialProject,
            Detail = original.Detail! with
            {
                ProjectId = partialProject,
                FileReferences = [new("file-a", "folder-project-a", null, ReferenceState.Unavailable,
                    "OptionalReferencesOmitted", original.LastVerifiedAt)],
            },
        };
        await ReadModelWritePolicy.UpdateAsync<ProjectReadModel>(store, ProjectReadModelKeys.StoreName, detailKey, _ => partialModel,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);
        DaprClient dapr = projects.Services.GetRequiredService<DaprClient>();
        string partialBytesBefore = (await dapr.GetStateAsync<JsonElement>(ProjectReadModelKeys.StoreName, detailKey,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false)).GetRawText();
        string? partialETagBefore = (await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey,
            TestContext.Current.CancellationToken).ConfigureAwait(false)).ETag;
        store.Writes.Clear();
        JsonObject partial = await EqualReadAsync(new GetProjectQuery(), HttpStatusCode.OK, partialProject).ConfigureAwait(false);
        partial["payload"]!.Deserialize<GetProjectResponse>(JsonSerializerOptions.Web)!.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Partial);
        store.Writes.ShouldBeEmpty();
        string partialBytesAfter = (await dapr.GetStateAsync<JsonElement>(ProjectReadModelKeys.StoreName, detailKey,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false)).GetRawText();
        partialBytesAfter.ShouldBe(partialBytesBefore);
        (await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey,
            TestContext.Current.CancellationToken).ConfigureAwait(false)).ETag.ShouldBe(partialETagBefore);
        output.WriteLine("Story 6.6 real Gateway matrix: four attribute-derived routes equal Web; safe/descriptive/audit failure/404/401/Partial/Unavailable and persisted audit-only delta verified.");
    }

    private static SubmitQueryRequest McpCliRequest(string tenant, object input, string project = "project-a")
    {
        HexalithQueryAttribute query = input.GetType().GetCustomAttribute<HexalithQueryAttribute>()!;
        return new(tenant, query.Domain!, query.AggregateId ?? project, query.WireType!, query.ProjectionType,
            JsonSerializer.SerializeToElement(input, input.GetType()), ProjectionActorType: query.ProjectionActorType);
    }

    private async Task<JsonObject> SubmitMcpCliMatrixAsync(HttpClient client, SubmitQueryRequest request, HttpStatusCode expected)
    {
        DateTimeOffset requestStartedAt = DateTimeOffset.UtcNow;
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/queries", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.StatusCode.ShouldBe(expected, body);
        JsonObject json = string.IsNullOrEmpty(body) ? new() : JsonNode.Parse(body)!.AsObject();
        if (expected == HttpStatusCode.NotFound)
        {
            using JsonDocument raw = JsonDocument.Parse(body);
            string correlation = raw.RootElement.GetProperty("correlationId").GetRawText();
            // Preserve every original byte except the single per-call correlation value.
            _mcpCliSafeDenialBodies.Add(body.Replace(correlation, "\"<correlation-id>\"", StringComparison.Ordinal));
        }
        json.Remove("correlationId");
        // Gateway delivery time changes per call; Project asOf/lastVerifiedAt remain part of exact parity.
        if (expected == HttpStatusCode.OK)
        {
            JsonObject metadata = json["metadata"].ShouldNotBeNull().AsObject();
            DateTimeOffset served = metadata["servedAt"].ShouldNotBeNull().GetValue<DateTimeOffset>();
            served.ShouldBeGreaterThanOrEqualTo(requestStartedAt);
            served.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
            metadata.Remove("servedAt");
        }
        return json;
    }
}
#endif
