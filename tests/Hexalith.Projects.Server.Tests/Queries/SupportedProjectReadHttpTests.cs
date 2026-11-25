namespace Hexalith.Projects.Server.Tests.Queries;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Dapr.Client;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Replay;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server.Authentication;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

/// <summary>Always-run authenticated routing plus opt-in isolated Dapr persistence; not production acceptance.</summary>
public sealed partial class SupportedProjectReadHttpTests
{
    private const string Issuer = "https://story61.example/identity";
    private const string Audience = "configured-read-audience";

    [Fact]
    public async Task AuthenticatedRegisteredReadsAlwaysExerciseHttpStatusAndSafeDenialContracts()
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "story61-always-http" };
        WebApplication app = await StartAsync(null, null, key, conversations: new AdvisoryTestConversationDirectory());
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
            QueryEnvelope open = new("tenant-a", "projects", "project-a", GetProjectQuery.QueryType, "{}"u8.ToArray(), "ordinary-http", "actor-1")
            { OriginalActorId = "actor-1", AuthenticatedWorkloadId = "operator-web", IsDelegated = true, DelegationId = "delegation-1", Scopes = ["projects.read", "projects.list"], Audience = [Audience] };
            using HttpResponseMessage anonymous = await client.PostAsJsonAsync("/query", open, TestContext.Current.CancellationToken);
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
            using HttpResponseMessage metadata = await client.PostAsJsonAsync("/admin/operational-index-metadata", new AdminOperationalIndexMetadata.Request(["projects"]) { AppId = "projects", ServiceVersion = "v1" }, TestContext.Current.CancellationToken);
            metadata.EnsureSuccessStatusCode();
            string fingerprint = (await metadata.Content.ReadFromJsonAsync<AdminOperationalIndexMetadata.Response>(TestContext.Current.CancellationToken))!.CatalogFingerprint!;
            foreach (string route in new[] { "/project/v2", "/project/rebuild/v1" })
            {
                using HttpResponseMessage mutation = await client.PostAsJsonAsync(route, new ProjectionDispatchRequest(SupportedProjectReadFixture.Request("project-a"), ["project-reads"], "ordinary-projection", fingerprint), TestContext.Current.CancellationToken);
                mutation.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            }
            (await DispatchAsync(app, new(SupportedProjectReadFixture.Request("project-a"), ["project-reads"], "ordinary-projection", fingerprint))).Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.Completed);
            QueryEnvelope list = open with { AggregateId = "projects", QueryType = ListProjectsQuery.QueryType };
            using HttpResponseMessage opened = await client.PostAsJsonAsync("/query", open, TestContext.Current.CancellationToken);
            using HttpResponseMessage listed = await client.PostAsJsonAsync("/query", list, TestContext.Current.CancellationToken);
            opened.StatusCode.ShouldBe(HttpStatusCode.OK); listed.StatusCode.ShouldBe(HttpStatusCode.OK);
            SupportedProjectReadFixture.Payload<GetProjectResponse>((await opened.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!).ProjectId.ShouldBe("project-a");
            SupportedProjectReadFixture.Payload<ProjectListPage>((await listed.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!).Items.Single().ProjectId.ShouldBe("project-a");
            foreach (QueryEnvelope query in new[] { open, list })
            {
                using HttpResponseMessage malformed = await client.PostAsJsonAsync("/query", query with { Payload = "[]"u8.ToArray() }, TestContext.Current.CancellationToken);
                malformed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            }
            using HttpResponseMessage absent = await client.PostAsJsonAsync("/query", open with { AggregateId = "absent" }, TestContext.Current.CancellationToken);
            using HttpResponseMessage denied = await client.PostAsJsonAsync("/query", open with { OriginalActorId = "other" }, TestContext.Current.CancellationToken);
            absent.StatusCode.ShouldBe(HttpStatusCode.NotFound); denied.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await absent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe(await denied.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            QueryEnvelope advisory = list with { QueryType = ResolveProjectFromAttachmentsQuery.QueryType,
                Payload = JsonSerializer.SerializeToUtf8Bytes(new ResolveProjectFromAttachmentsQuery([new("file", "file-id")]), JsonSerializerOptions.Web) };
            using HttpResponseMessage suggestions = await client.PostAsJsonAsync("/query", advisory, TestContext.Current.CancellationToken);
            suggestions.StatusCode.ShouldBe(HttpStatusCode.OK);
            var advisoryResult = SupportedProjectReadFixture.Payload<ProjectAdvisoryResolutionResponse>((await suggestions.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!);
            advisoryResult.Incomplete.ShouldBeTrue(); advisoryResult.Result.ShouldBeNull(); advisoryResult.UnsupportedInputKinds.ShouldBe(["file"]);
            using HttpResponseMessage oversize = await client.PostAsJsonAsync("/query", advisory with
            { Payload = JsonSerializer.SerializeToUtf8Bytes(new ResolveProjectFromAttachmentsQuery(Enumerable.Repeat(new ProjectAdvisoryAttachment("folder", "same"), 33).ToArray()), JsonSerializerOptions.Web) }, TestContext.Current.CancellationToken);
            oversize.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using HttpResponseMessage advisoryDenied = await client.PostAsJsonAsync("/query", advisory with { OriginalActorId = "other" }, TestContext.Current.CancellationToken);
            advisoryDenied.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            QueryEnvelope conversation = advisory with { QueryType = ResolveProjectFromConversationQuery.QueryType,
                Payload = "{\"conversationId\":\"conversation\"}"u8.ToArray() };
            using HttpResponseMessage conversationAllowed = await client.PostAsJsonAsync("/query", conversation, TestContext.Current.CancellationToken);
            conversationAllowed.StatusCode.ShouldBe(HttpStatusCode.OK);
            var conversationResult = SupportedProjectReadFixture.Payload<ProjectAdvisoryResolutionResponse>(
                (await conversationAllowed.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!);
            conversationResult.Incomplete.ShouldBeFalse(); conversationResult.Candidates.Single().ProjectId.ShouldBe("project-a");
            conversationResult.Candidates.Single().Score.ShouldBe(50);
            conversationResult.Candidates.Single().ReasonCodes.ShouldBe([Hexalith.Projects.Contracts.Ui.ProjectReasonCode.ConversationLinked]);
            using HttpResponseMessage conversationMalformed = await client.PostAsJsonAsync("/query", conversation with { Payload = "{}"u8.ToArray() }, TestContext.Current.CancellationToken);
            conversationMalformed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using HttpResponseMessage conversationDenied = await client.PostAsJsonAsync("/query", advisory with
            { QueryType = ResolveProjectFromConversationQuery.QueryType, OriginalActorId = "other", Payload = "{\"conversationId\":\"unknown\"}"u8.ToArray() }, TestContext.Current.CancellationToken);
            conversationDenied.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            await app.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart()
    {
        string? httpEndpoint = Environment.GetEnvironmentVariable("STORY61_DAPR_HTTP_ENDPOINT");
        string? grpcEndpoint = Environment.GetEnvironmentVariable("STORY61_DAPR_GRPC_ENDPOINT");
        Assert.SkipUnless(httpEndpoint is not null && grpcEndpoint is not null, "Requires the isolated Story 6.1 Dapr persistence lane.");
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "story61-http-fixture" };
        string project = "http-" + Guid.NewGuid().ToString("N");
        QueryEnvelope query = new("tenant-a", "projects", project, GetProjectQuery.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(new GetProjectQuery(true), SupportedProjectReadFixture.Json), "http-inspection-" + project, "actor-1")
        {
            OriginalActorId = "actor-1", AuthenticatedWorkloadId = "operator-web", IsDelegated = true, DelegationId = "delegation-1",
            Scopes = ["projects.read", "projects.list"], Audience = [Audience],
        };
        async Task<ProjectAuditFact> ReadInspectionAsync(HttpClient client)
        {
            QueryEnvelope auditQuery = query with
            {
                QueryType = GetProjectAuditQuery.QueryType,
                Payload = JsonSerializer.SerializeToUtf8Bytes(new GetProjectAuditQuery(), SupportedProjectReadFixture.Json),
            };
            using HttpResponseMessage response = await client.PostAsJsonAsync("/query", auditQuery, TestContext.Current.CancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            QueryResult result = (await response.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken).ConfigureAwait(false))!;
            ProjectAuditPage page = SupportedProjectReadFixture.Payload<ProjectAuditPage>(result);
            ProjectAuditFact inspection = page.Items.Single(item => item.Operation == "metadata.inspected");
            inspection.ProjectCount.ShouldBe(1);
            inspection.FieldClass.ShouldBe("ProjectNameDescriptionSetupReferenceLabels");
            return inspection;
        }

        ProjectAuditFact beforeRestart;
        ProjectionRequest projection = SupportedProjectReadFixture.Request(project, 20000);
        string? fingerprint;
        string? before;
        WebApplication app = await StartAsync(httpEndpoint!, grpcEndpoint!, key);
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
            using HttpResponseMessage anonymous = await client.PostAsJsonAsync("/project/v2", new ProjectionDispatchRequest(projection, ["project-reads"], "dispatch-" + project, "invalid"), TestContext.Current.CancellationToken);
            anonymous.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            using HttpResponseMessage anonymousRead = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            anonymousRead.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
            using HttpResponseMessage metadata = await client.PostAsJsonAsync("/admin/operational-index-metadata", new AdminOperationalIndexMetadata.Request(["projects"]) { AppId = "projects", ServiceVersion = "v1" }, TestContext.Current.CancellationToken);
            metadata.EnsureSuccessStatusCode();
            var catalog = await metadata.Content.ReadFromJsonAsync<AdminOperationalIndexMetadata.Response>(TestContext.Current.CancellationToken);
            fingerprint = catalog!.CatalogFingerprint;
            fingerprint.ShouldNotBeNullOrWhiteSpace();
            var outcome = await DispatchAsync(app, new(projection, ["project-reads"], "dispatch-" + project, fingerprint!));
            outcome!.Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.Completed, JsonSerializer.Serialize(outcome));
            IReadModelStore store = app.Services.GetRequiredService<IReadModelStore>();
            before = JsonSerializer.Serialize((await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", project), TestContext.Current.CancellationToken)).Value);
            using HttpResponseMessage opened = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            opened.EnsureSuccessStatusCode();
            QueryResult result = (await opened.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!;
            SupportedProjectReadFixture.Payload<GetProjectResponse>(result).Name.ShouldBe("Project " + project);
            beforeRestart = await ReadInspectionAsync(client);
            QueryEnvelope list = query with { AggregateId = "projects", QueryType = ListProjectsQuery.QueryType,
                Payload = JsonSerializer.SerializeToUtf8Bytes(new ListProjectsQuery(PageSize: 200), SupportedProjectReadFixture.Json) };
            using HttpResponseMessage listed = await client.PostAsJsonAsync("/query", list, TestContext.Current.CancellationToken);
            listed.EnsureSuccessStatusCode();
            ProjectListPage page = SupportedProjectReadFixture.Payload<ProjectListPage>((await listed.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!);
            page.Items.ShouldContain(item => item.ProjectId == project && item.Name == null);
            using HttpResponseMessage malformed = await client.PostAsJsonAsync("/query", query with { Payload = "{\"pageSize\":-1}"u8.ToArray() }, TestContext.Current.CancellationToken);
            malformed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using HttpResponseMessage denied = await client.PostAsJsonAsync("/query", query with { OriginalActorId = "other" }, TestContext.Current.CancellationToken);
            using HttpResponseMessage absent = await client.PostAsJsonAsync("/query", query with { AggregateId = "absent" }, TestContext.Current.CancellationToken);
            denied.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            absent.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await denied.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe(await absent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            JsonSerializer.Serialize((await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", project), TestContext.Current.CancellationToken)).Value).ShouldBe(before);
            await app.StopAsync(TestContext.Current.CancellationToken);
        }

        WebApplication restarted = await StartAsync(httpEndpoint!, grpcEndpoint!, key);
        await using (restarted.ConfigureAwait(true))
        {
            using HttpClient client = new() { BaseAddress = new Uri(restarted.Urls.Single()) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
            (await ReadInspectionAsync(client)).ShouldBe(beforeRestart);
            using HttpResponseMessage opened = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            opened.EnsureSuccessStatusCode();
            SupportedProjectReadFixture.Payload<GetProjectResponse>((await opened.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken))!).Name.ShouldBe("Project " + project);
            (await ReadInspectionAsync(client)).ShouldBe(beforeRestart);
            (await DispatchAsync(restarted, new(projection, ["project-reads"], "dispatch-" + project, fingerprint!))).Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.AlreadyCompleted);
            (await DispatchAsync(restarted, new(projection, ["project-reads"], "rebuild-" + project, fingerprint!), rebuild: true)).Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.Completed);
            IReadModelStore store = restarted.Services.GetRequiredService<IReadModelStore>();
            JsonSerializer.Serialize((await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", project), TestContext.Current.CancellationToken)).Value).ShouldBe(before);
            await restarted.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ActualWorkloadPolicyAdmitsSdkProjectionButFailsClosedWithoutActorAuthority()
    {

        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "story61-workload-fixture" };
        WebApplication app = await StartAsync(null, null, key, workload: true);
        await using (app.ConfigureAwait(true))
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
            IReadModelStore store = app.Services.GetRequiredService<IReadModelStore>();
            foreach (string route in new[] { "/project/v2", "/project/rebuild/v1" })
            {
                var mutation = new ProjectionDispatchRequest(SupportedProjectReadFixture.Request("blocked-project"), ["project-reads"], "blocked", "invalid");
                using HttpResponseMessage anonymous = await client.PostAsJsonAsync(route, mutation, TestContext.Current.CancellationToken);
                anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
                using HttpResponseMessage bearer = await client.PostAsJsonAsync(route, mutation, TestContext.Current.CancellationToken);
                bearer.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
                client.DefaultRequestHeaders.Authorization = null;
                (await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Detail("tenant-a", "blocked-project"), TestContext.Current.CancellationToken)).Value.ShouldBeNull();
                (await store.GetAsync<ProjectReadIndex>(ProjectReadModelKeys.StoreName, ProjectReadModelKeys.Index("tenant-a"), TestContext.Current.CancellationToken)).Value.ShouldBeNull();
            }
            client.DefaultRequestHeaders.Add(DaprAppChannelToken.HeaderName, "story61-isolated-channel-fixture");
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceMetadata));
            var metadataRequest = new AdminOperationalIndexMetadata.Request(["projects"]) { AppId = "projects", ServiceVersion = "v1" };
            using HttpResponseMessage unbound = await client.PostAsJsonAsync("/admin/operational-index-metadata", metadataRequest, TestContext.Current.CancellationToken);
            unbound.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            RouteCatalogGenerationReadback catalog = await app.Services.GetRequiredService<IRouteCatalogAuthority>()
                .ReadbackAsync(TestContext.Current.CancellationToken);
            client.DefaultRequestHeaders.Add(RouteCatalogInvocationHandler.RootHeader, catalog.RootDigest);
            client.DefaultRequestHeaders.Add(RouteCatalogInvocationHandler.EpochHeader, catalog.Epoch.ToString(System.Globalization.CultureInfo.InvariantCulture));
            using HttpResponseMessage metadata = await client.PostAsJsonAsync("/admin/operational-index-metadata", new AdminOperationalIndexMetadata.Request(["projects"]) { AppId = "projects", ServiceVersion = "v1" }, TestContext.Current.CancellationToken);
            metadata.EnsureSuccessStatusCode();
            string fingerprint = (await metadata.Content.ReadFromJsonAsync<AdminOperationalIndexMetadata.Response>(TestContext.Current.CancellationToken))!.CatalogFingerprint!;
            client.DefaultRequestHeaders.Remove(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName);
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceProject));
            string project = "workload-" + Guid.NewGuid().ToString("N");
            var delivery = new ProjectionDispatchRequest(SupportedProjectReadFixture.Request(project, 30000), ["project-reads"], "dispatch-" + project, fingerprint);
            using HttpResponseMessage dispatch = await client.PostAsJsonAsync("/project/v2", delivery, TestContext.Current.CancellationToken);
            dispatch.EnsureSuccessStatusCode();
            (await dispatch.Content.ReadFromJsonAsync<ProjectionDispatchResponse>(TestContext.Current.CancellationToken))!.Outcomes.Single().Status.ShouldBe(ProjectionDispatchStatus.Completed);
            string detailKey = ProjectReadModelKeys.Detail("tenant-a", project);
            var persisted = await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey, TestContext.Current.CancellationToken);
            persisted.Value.ShouldNotBeNull().GlobalPosition.ShouldBe(30001);
            using HttpResponseMessage duplicate = await client.PostAsJsonAsync("/project/v2", delivery, TestContext.Current.CancellationToken);
            duplicate.EnsureSuccessStatusCode();
            var after = await store.GetAsync<ProjectReadModel>(ProjectReadModelKeys.StoreName, detailKey, TestContext.Current.CancellationToken);
            after.ETag.ShouldBe(persisted.ETag);
            client.DefaultRequestHeaders.Remove(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName);
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceQuery));
            QueryEnvelope query = new("tenant-a", "projects", project, GetProjectQuery.QueryType, "{}"u8.ToArray(), "runtime-read", "actor-1")
            { OriginalActorId = "actor-1", AuthenticatedWorkloadId = "operator-web", IsDelegated = true, DelegationId = "delegation-1", Scopes = ["projects.read", "projects.list"], Audience = [Audience] };
            using HttpResponseMessage read = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            read.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await read.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain(project);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(key));
            using HttpResponseMessage ambiguous = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            ambiguous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            await app.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task CanonicalCommandReceiverPreservesLegacyWireAndRefusesUnadmittedCalls()
    {
        using RSA rsa = RSA.Create(2048);
        RsaSecurityKey key = new(rsa) { KeyId = "projects-sdk-command-fixture" };
        WebApplication app = await StartAsync(null, null, key, workload: true);
        await using (app.ConfigureAwait(true))
        {
            CancellationToken token = TestContext.Current.CancellationToken;
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
            var command = new CommandEnvelope("command", "tenant-a", "projects", "exact-project",
                ProjectsServerModule.CreateProjectCommandType, "{\"name\":\"Project\"}"u8.ToArray(), "correlation", null, "actor-1", null);
            var input = new DomainServiceRequest(command, null);
            using HttpResponseMessage anonymous = await client.PostAsJsonAsync("/process", input, token);
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            client.DefaultRequestHeaders.Add(DaprAppChannelToken.HeaderName, "incorrect-channel");
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceProcess));
            using HttpResponseMessage wrongSidecar = await client.PostAsJsonAsync("/process", input, token);
            wrongSidecar.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            client.DefaultRequestHeaders.Remove(DaprAppChannelToken.HeaderName);
            client.DefaultRequestHeaders.Add(DaprAppChannelToken.HeaderName, "story61-isolated-channel-fixture");
            using HttpResponseMessage missingCatalog = await client.PostAsJsonAsync("/process", input, token);
            missingCatalog.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            var readback = await app.Services.GetRequiredService<IRouteCatalogAuthority>().ReadbackAsync(token);
            client.DefaultRequestHeaders.Add(RouteCatalogInvocationHandler.RootHeader, readback.RootDigest);
            client.DefaultRequestHeaders.Add(RouteCatalogInvocationHandler.EpochHeader, readback.Epoch.ToString(System.Globalization.CultureInfo.InvariantCulture));
            client.DefaultRequestHeaders.Remove(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName);
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceProject));
            using HttpResponseMessage wrongOperation = await client.PostAsJsonAsync("/process", input, token);
            wrongOperation.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            client.DefaultRequestHeaders.Remove(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName);
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName,
                WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceProcess, audience: "tenants"));
            using HttpResponseMessage wrongAudience = await client.PostAsJsonAsync("/process", input, token);
            wrongAudience.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            client.DefaultRequestHeaders.Remove(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName);
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName,
                WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceProcess, caller: "tenants"));
            using HttpResponseMessage wrongCaller = await client.PostAsJsonAsync("/process", input, token);
            wrongCaller.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            client.DefaultRequestHeaders.Remove(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName);
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceProcess));
            using HttpResponseMessage foreign = await client.PostAsJsonAsync("/process", input with { Command = command with { TenantId = "foreign" } }, token);
            foreign.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);

            IDomainProcessor baseline = app.Services.GetRequiredKeyedService<IDomainProcessor>("projects");
            DomainServiceWireResult expected = DomainServiceWireResult.FromDomainResult(await baseline.ProcessAsync(command, null));
            expected.Events.Count.ShouldBe(2);
            await CompareWireAsync(input, expected);

            ProjectionEventDto[] history = expected.Events.Select((item, index) =>
                new ProjectionEventDto(item.EventTypeName, item.Payload, item.SerializationFormat,
                    index + 1, SupportedProjectReadFixture.EventTime, "state-parity")).ToArray();
            ProjectionResponse projection = ProjectProjectionHandler.Project(new("tenant-a", "projects", "exact-project", history));
            var snapshot = new DomainServiceCurrentState(new DomainServiceCurrentState(projection.State, [], 2, 2), [], 2, 2);
            DomainServiceWireResult noOp = DomainServiceWireResult.FromDomainResult(await baseline.ProcessAsync(command, snapshot));
            noOp.Events.ShouldBeEmpty();
            await CompareWireAsync(new(command, snapshot), noOp);
            var changedCommand = command with { MessageId = "different-command" };
            DomainServiceWireResult rejection = DomainServiceWireResult.FromDomainResult(await baseline.ProcessAsync(changedCommand, snapshot));
            rejection.IsRejection.ShouldBeTrue();
            await CompareWireAsync(new(changedCommand, snapshot), rejection);

            client.DefaultRequestHeaders.Remove(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName);
            client.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName,
                WorkloadToken(key, EventStoreWorkloadOperations.DomainServiceReplayState));
            ReplayEventEnvelope[] events = history.Select(item =>
                new ReplayEventEnvelope(item.SequenceNumber, item.EventTypeName, item.Payload, item.SerializationFormat,
                    1, "event-" + item.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), null, null)).ToArray();
            var replay = new AggregateReconstructionRequest("tenant-a", "projects", "projects", "exact-project",
                events.Length, events, true, "state-parity");
            using HttpResponseMessage replayed = await client.PostAsJsonAsync("/replay-state", replay, token);
            replayed.EnsureSuccessStatusCode();
            var replayedState = (await replayed.Content.ReadFromJsonAsync<AggregateReconstructionResult>(token)).ShouldNotBeNull();
            replayedState.Status.ShouldBe(AggregateReconstructionStatus.Succeeded);
            replayedState.LastAppliedSequenceNumber.ShouldBe(2);
            JsonElement.DeepEquals(JsonSerializer.Deserialize<JsonElement>(replayedState.StateJson!), projection.State).ShouldBeTrue();
            IReadOnlyList<AggregateReconstructionTimelineEntry> timeline = replayedState.Timeline.ShouldNotBeNull();
            timeline.Select(entry => entry.SequenceNumber).ShouldBe([1L, 2L]);
            ProjectionResponse created = ProjectProjectionHandler.Project(new("tenant-a", "projects", "exact-project", history[..1]));
            JsonElement.DeepEquals(JsonSerializer.Deserialize<JsonElement>(timeline[0].StateJson), created.State).ShouldBeTrue();
            JsonElement.DeepEquals(JsonSerializer.Deserialize<JsonElement>(timeline[1].StateJson), projection.State).ShouldBeTrue();
            app.Services.GetRequiredService<InMemoryReadModelStore>().Count.ShouldBe(0);
            await app.StopAsync(token);

            async Task CompareWireAsync(DomainServiceRequest request, DomainServiceWireResult wire)
            {
                using HttpResponseMessage admitted = await client.PostAsJsonAsync("/process", request, token).ConfigureAwait(false);
                admitted.EnsureSuccessStatusCode();
                string actual = await admitted.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                JsonElement.DeepEquals(JsonSerializer.Deserialize<JsonElement>(actual),
                    JsonSerializer.SerializeToElement(wire, JsonSerializerOptions.Web)).ShouldBeTrue();
            }
        }
    }

    private static Task<ProjectionDispatchResponse> DispatchAsync(WebApplication app, ProjectionDispatchRequest request, bool rebuild = false)
        => rebuild ? DomainProjectionDispatcher.RebuildAsync(app.Services, request,
            app.Services.GetRequiredService<IOptions<ProjectionDispatchOptions>>().Value,
            app.Services.GetRequiredService<IOptions<DomainProjectionIdentityOptions>>().Value, TestContext.Current.CancellationToken)
        : DomainProjectionDispatcher.DispatchAsync(app.Services, request,
            app.Services.GetRequiredService<IOptions<ProjectionDispatchOptions>>().Value,
            app.Services.GetRequiredService<IOptions<DomainProjectionIdentityOptions>>().Value, TestContext.Current.CancellationToken);

    /// <summary>Signs an EventStore workload assertion for one canonical callback operation.</summary>
    internal static string WorkloadToken(SecurityKey key, string operation, string audience = "projects", string caller = "eventstore")
    {
        JwtSecurityTokenHandler handler = new(); handler.OutboundClaimTypeMap.Clear();
        return handler.CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = Issuer, Audience = audience, IssuedAt = DateTime.UtcNow, NotBefore = DateTime.UtcNow.AddSeconds(-1), Expires = DateTime.UtcNow.AddSeconds(120),
            SigningCredentials = new(key, SecurityAlgorithms.RsaSha256),
            Subject = new ClaimsIdentity([new("azp", caller), new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, operation),
                new(RouteCatalogSurfacePolicy.ClaimType, "service")]),
        });
    }

    /// <summary>Starts a Projects receiver; <paramref name="workload"/> selects the SDK host with a committed fixture catalog.</summary>
    internal static async Task<WebApplication> StartAsync(string? httpEndpoint, string? grpcEndpoint, SecurityKey key, bool workload = false, IProjectAdvisoryConversationDirectory? conversations = null, Action<IServiceCollection>? configure = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Authority"] = Issuer, ["Authentication:JwtBearer:Issuer"] = Issuer,
            ["Authentication:JwtBearer:AllowedAlgorithms:0"] = "RS256",
            ["APP_API_TOKEN"] = "story61-isolated-channel-fixture",
            ["Authentication:JwtBearer:Audience"] = Audience, ["Authentication:JwtBearer:RequireHttpsMetadata"] = "true",
        });
        if (workload)
        {
            builder.AddProjectsDomainService();
            GatewayFixtureCatalogComposition.AddProjectsReceiver(builder, "tenant-a");
        }
        else
        {
            builder.Services.AddProjectsServer();
            builder.Services.AddProjectsAuthentication(builder.Configuration, builder.Environment);
        }
        builder.Services.RemoveAll<Hexalith.Projects.Server.Conversations.IProjectConversationMembershipDirectory>();
        builder.Services.AddSingleton<Hexalith.Projects.Server.Conversations.IProjectConversationMembershipDirectory, CurrentProjectConversationMembership>();
        if (conversations is not null)
        {
            builder.Services.RemoveAll<IProjectAdvisoryConversationDirectory>();
            builder.Services.AddSingleton(conversations);
        }
        builder.Services.RemoveAll<IProjectTenantAccessProjectionStore>();
        builder.Services.AddSingleton<IProjectTenantAccessProjectionStore, InMemoryProjectTenantAccessProjectionStore>();
        builder.Services.RemoveAll<IReadModelStore>();
        if (httpEndpoint is null || grpcEndpoint is null)
        {
            builder.Services.AddSingleton<InMemoryReadModelStore>();
            builder.Services.AddSingleton<IReadModelStore>(provider => provider.GetRequiredService<InMemoryReadModelStore>());
            builder.Services.AddSingleton<IReadModelBatchStore>(provider => provider.GetRequiredService<InMemoryReadModelStore>());
        }
        else
        {
            builder.Services.AddSingleton(_ => new DaprClientBuilder().UseHttpEndpoint(httpEndpoint).UseGrpcEndpoint(grpcEndpoint).Build());
            builder.Services.AddEventStoreReadModelStore();
        }
        if (!workload) { builder.Services.AddSingleton<IAsyncDomainProjectionHandler, ProjectReadProjectionHandler>(); }
        builder.Services.RemoveAll<IProjectFolderDirectory>();
        builder.Services.AddSingleton<IProjectFolderDirectory, ProjectReadTestFolders>();
        builder.Services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        builder.Services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        builder.Services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        builder.Services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        ProjectReadTestClock clock = new(SupportedProjectReadFixture.EventTime.AddSeconds(5));
        builder.Services.AddSingleton<TimeProvider>(clock);
        builder.Services.AddSingleton<IUtcClock>(clock);
        builder.Services.PostConfigureAll<JwtBearerOptions>(options =>
        {
            OpenIdConnectConfiguration oidc = new() { Issuer = Issuer }; oidc.SigningKeys.Add(key);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(oidc);
        });
        configure?.Invoke(builder.Services);
        WebApplication app = builder.Build();
        ProjectTenantAccessProjection tenant = new() { TenantId = "tenant-a", Enabled = true, Watermark = 1, ProjectionWatermark = "tenant-a:1", LastEventTimestamp = clock.Now };
        tenant.Principals["actor-1"] = new("actor-1", "TenantOwner");
        await app.Services.GetRequiredService<IProjectTenantAccessProjectionStore>().SaveAsync(tenant, TestContext.Current.CancellationToken).ConfigureAwait(false);
        if (workload)
        {
            app.UseEventStoreDomainService();
            app.MapProjectsHumanEndpoints();
        }
        else
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapProjectsServerEndpoints();
        }
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        return app;
    }

    private static string Token(SecurityKey key, bool inspection = true, string role = "TenantOperator", string roleClaimType = "projects:role", string workload = "operator-web")
    {
        JwtSecurityTokenHandler handler = new(); handler.OutboundClaimTypeMap.Clear();
        return handler.CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = Issuer, Audience = Audience, NotBefore = DateTime.UtcNow.AddMinutes(-1), Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new(key, SecurityAlgorithms.RsaSha256),
            Subject = new ClaimsIdentity([
                new("sub", "actor-1"), new("tenant_id", "tenant-a"), new("azp", workload), new("client_id", "issuing-client"),
                new("act", "{\"sub\":\"delegation-1\"}"), new("scope", "projects.read projects.list"), new(roleClaimType, role),
                new("eventstore:permission", ProjectAuthorizationGate.ReadProjectAction), new("eventstore:permission", ProjectAuthorizationGate.ListProjectsAction),
                .. (inspection ? new[] { new Claim("eventstore:permission", ProjectReadQueryExecutor.InspectionPermission) } : Array.Empty<Claim>()),
            ]),
        });
    }
}
