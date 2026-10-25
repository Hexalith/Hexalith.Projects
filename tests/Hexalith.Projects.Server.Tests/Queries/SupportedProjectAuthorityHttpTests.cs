#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Queries;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;
using Xunit;

/// <summary>Local asymmetric software proof only; no deployed identity, enrollment or acceptance is established.</summary>
public sealed class SupportedProjectAuthorityHttpTests
{
    /// <summary>Proves the authorized two-hop path and read-only persisted end-state.</summary>
    [Fact]
    public async Task RealBrokerAndBothRs256ReceiversAuthorizeListOpenAndContinuationWithoutDomainWrites()
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        var before = await fixture.SnapshotAsync(); before.ShouldAllBe(item => item.Bytes.Length > 0 && item.ETag.Length > 0);
        fixture.Projects.Services.GetRequiredService<IDataProtectionProvider>().ShouldBeOfType<EphemeralDataProtectionProvider>();
        var opened = await fixture.ReadAsync<GetProjectResponse>(fixture.Open());
        fixture.Routes.Attempts.ShouldBe(OpenSteps());
        opened.ProjectId.ShouldBe("project-a"); opened.Name.ShouldBeNull(); opened.Description.ShouldBeNull();
        var first = await fixture.ReadAsync<ProjectListPage>(fixture.List(new(PageSize: 1)));
        first.Items.Single().ProjectId.ShouldBe("project-a"); first.Cursor.ShouldNotBeNull();
        var second = await fixture.ReadAsync<ProjectListPage>(fixture.List(new(Cursor: first.Cursor, PageSize: 1)));
        second.Items.Single().ProjectId.ShouldBe("project-b"); second.Cursor.ShouldBeNull();
        first.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        foreach (var hop in fixture.Routes.Hops)
        {
            hop.Status.ShouldBe(200); await fixture.VerifyHopAsync(hop);
            hop.Query.UserId.ShouldBe("actor-1"); hop.Query.OriginalActorId.ShouldBe("actor-1");
            if (hop.Target == "folders")
            {
                hop.Query.AuthenticatedWorkloadId.ShouldBe("projects");
                var origin = hop.Query.CallerAuthority!.OriginatingContext.ShouldNotBeNull();
                origin.OriginalActorId.ShouldBe("actor-1"); origin.AuthenticatedWorkloadId.ShouldBe("operator-web");
                origin.IsDelegated.ShouldBeTrue(); origin.DelegationId.ShouldBe("delegation-1");
                origin.Scopes.ShouldBe(["projects.read", "projects.list"]); origin.Audience.ShouldBe([ProjectReadAuthorityFixture.UserAudience]);
                hop.Query.CallerAuthority.Roles.ShouldBeEmpty();
            }
        }
        fixture.Routes.Hops.ShouldContain(hop => hop.Target == "projects"); fixture.Routes.Hops.ShouldContain(hop => hop.Target == "folders");
        AssertProfileRequests(fixture);
        fixture.Routes.HumanBearerForwarded.ShouldBeFalse(); await fixture.AssertUnchangedAsync(before);
    }

    /// <summary>Proves delegated Chatbot descriptions and continuation depend on real Folder authorization.</summary>
    [Fact]
    public async Task DelegatedChatbotListsAndOpensOnlyCurrentFolderAuthorizedProjects()
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.HumanToken(projectUser: true));
        var before = await fixture.SnapshotAsync();
        (await fixture.ReadAsync<GetProjectResponse>(fixture.Open())).Name.ShouldBe("Project project-a");
        var first = await fixture.ReadAsync<ProjectListPage>(fixture.List(new(PageSize: 1)));
        first.Items.Single().Name.ShouldBe("Project project-a"); first.Cursor.ShouldNotBeNull();
        var second = await fixture.ReadAsync<ProjectListPage>(fixture.List(new(Cursor: first.Cursor, PageSize: 1)));
        second.Items.Single().Name.ShouldBe("Project project-b"); second.Cursor.ShouldBeNull();
        foreach (var hop in fixture.Routes.Hops)
        {
            hop.Status.ShouldBe(200); await fixture.VerifyHopAsync(hop);
            if (hop.Target == "folders") hop.Query.CallerAuthority!.OriginatingContext!.AuthenticatedWorkloadId.ShouldBe("chatbot");
        }
        fixture.Audit.Keys.ShouldNotBeEmpty(); fixture.Routes.HumanBearerForwarded.ShouldBeFalse();
        await fixture.AssertUnchangedAsync(before);
    }

    /// <summary>Rejects exact-envelope substitutions after issuance at either receiver.</summary>
    /// <param name="target">The receiving workload.</param>
    /// <param name="fault">The immutable field altered after issuance.</param>
    [Theory]
    [InlineData("projects", "actor")]
    [InlineData("projects", "tenant")]
    [InlineData("projects", "target")]
    [InlineData("projects", "payload")]
    [InlineData("projects", "audience")]
    [InlineData("folders", "actor")]
    [InlineData("folders", "tenant")]
    [InlineData("folders", "target")]
    [InlineData("folders", "payload")]
    [InlineData("folders", "audience")]
    public async Task AlteringAnAlreadyIssuedQueryAtEitherHopRefusesProtectedMetadata(string target, string fault)
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.HumanToken(projectUser: true));
        (await fixture.ReadAsync<GetProjectResponse>(fixture.Open())).ProjectId.ShouldBe("project-a");
        var before = await fixture.SnapshotAsync();
        if (fault == "tenant")
        {
            var foreign = before.Single(item => item.Key == Hexalith.Projects.Server.Projections.ProjectReads.ProjectReadModelKeys.Detail(fixture.ForeignTenant, "project-a"));
            System.Text.Encoding.UTF8.GetString(foreign.Bytes).ShouldContain("Foreign project project-a");
            System.Text.Encoding.UTF8.GetString(foreign.Bytes).ShouldContain("Foreign secret description");
        }
        fixture.Routes.ChangeTarget = target;
        fixture.Routes.ChangeQuery = query => fault switch
        {
            "actor" => query with { OriginalActorId = "other", UserId = "other" },
            "tenant" => query with { TenantId = fixture.ForeignTenant },
            "target" => query with { AggregateId = target == "projects" ? "project-b" : "folder-project-b" },
            "payload" => query with { Payload = "{\"inspectDescriptiveMetadata\":true}"u8.ToArray() },
            "audience" => query with { Audience = ["other-audience"] },
            _ => throw new InvalidOperationException(),
        };
        await fixture.RefusedAsync(fixture.Open(), HttpStatusCode.NotFound); await fixture.RefusedAsync(fixture.Open(), HttpStatusCode.NotFound);
        var rejected = fixture.Routes.Hops.Last(hop => hop.Target == target);
        rejected.Status.ShouldBe(404); await fixture.VerifyHopAsync(rejected, exact: false);
        var token = new JsonWebToken(rejected.Assertion);
        token.GetClaim(QueryCallerAuthorityBinding.DigestClaim).Value.ShouldNotBe(QueryCallerAuthorityBinding.Bind(rejected.Query)[QueryCallerAuthorityBinding.DigestClaim]);
        AssertProfileRequests(fixture); await fixture.AssertUnchangedAsync(before);
    }

    /// <summary>Proves distinct permission, durable audit and outage refusal on both inspection branches.</summary>
    /// <param name="branch">The list or singleton inspection branch.</param>
    [Theory]
    [InlineData("open")]
    [InlineData("list")]
    public async Task InspectionNeedsDistinctPermissionAndDurableMetadataOnlyAuditIncludingUnchangedRetries(string branch)
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        var before = await fixture.SnapshotAsync();
        SubmitQueryRequest query = branch == "open" ? fixture.Open(inspection: true) : fixture.List(new(InspectDescriptiveMetadata: true));
        await fixture.RefusedAsync(query, HttpStatusCode.NotFound);
        (await InspectionKeysAsync(fixture)).ShouldBeEmpty();
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.HumanToken(inspection: true));
        fixture.Client.DefaultRequestHeaders.Add("X-Correlation-Id", "inspection-read");
        async Task InspectAsync()
        {
            if (branch == "open") (await fixture.ReadAsync<GetProjectResponse>(query).ConfigureAwait(true)).Name.ShouldBe("Project project-a");
            else (await fixture.ReadAsync<ProjectListPage>(query).ConfigureAwait(true)).Items.ShouldAllBe(item => item.Name != null);
        }
        await InspectAsync();
        string[] keys = await InspectionKeysAsync(fixture); keys.Length.ShouldBe(1);
        var first = (await fixture.SnapshotAsync(keys)).Last();
        var record = JsonSerializer.Deserialize<ProjectReadAuditRecord>(first.Bytes, JsonSerializerOptions.Web)!;
        record.ProjectCount.ShouldBe(branch == "open" ? 1 : 2);
        record.FieldClass.ShouldBe(branch == "open" ? "ProjectNameDescriptionSetupReferenceLabels" : "ProjectName");
        await InspectAsync();
        (await InspectionKeysAsync(fixture)).ShouldBe(keys);
        (await fixture.SnapshotAsync(keys)).Last().Bytes.ShouldBe(first.Bytes);
        foreach (string key in fixture.Audit.Keys.Keys)
        {
            var snapshot = (await fixture.SnapshotAsync(key).ConfigureAwait(true)).Last();
            using var document = JsonDocument.Parse(snapshot.Bytes);
            document.RootElement.EnumerateObject().Select(property => property.Name.ToLowerInvariant()).Order().ShouldBe(
                new[] { "requestsurrogate", "outcome", "projectcount", "fieldclass", "recordedat" }.Order());
            string serialized = System.Text.Encoding.UTF8.GetString(snapshot.Bytes);
            foreach (string secret in new[] { fixture.Tenant, fixture.ForeignTenant, "actor-1", "project-a", "Project project", "Safe description", "Owner label", "delegation-1" }) serialized.ShouldNotContain(secret);
        }
        fixture.Audit.Unavailable = true;
        fixture.Client.DefaultRequestHeaders.Remove("X-Correlation-Id");
        fixture.Client.DefaultRequestHeaders.Add("X-Correlation-Id", "inspection-outage");
        await fixture.RefusedAsync(query, HttpStatusCode.NotFound);
        (await InspectionKeysAsync(fixture)).ShouldBe(keys);
        (await fixture.SnapshotAsync(keys)).Last().Bytes.ShouldBe(first.Bytes);
        await fixture.AssertUnchangedAsync(before);
    }

    /// <summary>Preserves current owner filtering, safe denial and honest dependency degradation.</summary>
    /// <param name="fault">The current owner failure.</param>
    [Theory]
    [InlineData("denied")]
    [InlineData("absent")]
    [InlineData("stale")]
    [InlineData("unavailable")]
    public async Task RealFolderAuthorityPreservesFilteringSafeDenialAndHonestDegradation(string fault)
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.HumanToken(projectUser: true));
        (await fixture.ReadAsync<GetProjectResponse>(fixture.Open())).ProjectId.ShouldBe("project-a");
        switch (fault)
        {
            case "denied": await fixture.RevokeFolderAsync(); break;
            case "absent": await fixture.RemoveFolderAsync(); break;
            case "stale": fixture.Heads.Set(fixture.Tenant, "folder-project-a", 3); break;
            case "unavailable": fixture.Heads.Outage = true; break;
        }
        var before = await fixture.SnapshotAsync();
        string denied = await fixture.RefusedAsync(fixture.Open(), HttpStatusCode.NotFound);
        string absent = await fixture.RefusedAsync(fixture.Open(id: "absent"), HttpStatusCode.NotFound);
        denied.ShouldBe(absent);
        var page = await fixture.ReadAsync<ProjectListPage>(fixture.List());
        if (fault is "denied" or "absent")
        {
            page.Items.Select(item => item.ProjectId).ShouldBe(["project-b"]); page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        }
        else { page.Items.ShouldBeEmpty(); page.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable); }
        await fixture.RefusedAsync(fixture.Open(), HttpStatusCode.NotFound);
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.HumanToken());
        var operatorView = await fixture.ReadAsync<GetProjectResponse>(fixture.Open());
        operatorView.Name.ShouldBeNull(); operatorView.Description.ShouldBeNull();
        operatorView.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        await fixture.AssertUnchangedAsync(before);
    }

    /// <summary>Refuses missing enrollment at all three acquisitions and checks every attempted operation in order.</summary>
    /// <param name="hop">The acquisition whose current client or grant fails.</param>
    /// <param name="fault">The enrollment fault.</param>
    [Theory]
    [InlineData("projects", "binding-disabled")]
    [InlineData("projects", "wrong-client")]
    [InlineData("projects", "wrong-grant")]
    [InlineData("projects", "wrong-issuer")]
    [InlineData("owner-gateway", "wrong-client")]
    [InlineData("owner-gateway", "revoked")]
    [InlineData("folders", "wrong-grant")]
    [InlineData("folders", "revoked")]
    public async Task MissingEnrollmentFailsClosedWithoutAnUnboundRetry(string hop, string fault)
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.HumanToken(projectUser: true));
        (await fixture.ReadAsync<GetProjectResponse>(fixture.Open())).Name.ShouldBe("Project project-a");
        var before = await fixture.SnapshotAsync(); fixture.Routes.ClearAttempts();
        string client = hop == "owner-gateway" ? "projects" : "eventstore";
        string grantAudience = hop == "owner-gateway" ? "eventstore" : hop;
        switch (fault)
        {
            case "binding-disabled": fixture.ClientOptions(client).EnableResourceBindingRequests = false; break;
            case "wrong-client": fixture.ClientOptions(client).ClientId = "not-enrolled"; break;
            case "wrong-grant": fixture.AuthorityOptions.Grants.Single(grant => grant.Audience == grantAudience).Bindings[EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType] = ["other"]; break;
            case "wrong-issuer": fixture.ReplaceTrustIssuer(client, "https://wrong-issuer.example.test"); break;
            case "revoked": fixture.AuthorityOptions.Grants.Single(grant => grant.Audience == grantAudience).Revoked = true; break;
        }
        HttpStatusCode expected = hop == "projects" ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound;
        await fixture.RefusedAsync(fixture.Open(), expected); await fixture.RefusedAsync(fixture.Open(), expected);
        string[] once = hop switch
        {
            "projects" => fault == "binding-disabled" ? [ServiceStep("projects", "missing")] : [BrokerStep("projects"), ServiceStep("projects", "missing")],
            "owner-gateway" => [BrokerStep("projects"), ServiceStep("projects"), BrokerStep("eventstore")],
            _ => [BrokerStep("projects"), ServiceStep("projects"), BrokerStep("eventstore"), ServiceStep("eventstore"), BrokerStep("folders"), ServiceStep("folders", "missing")],
        };
        fixture.Routes.Attempts.ShouldBe(once.Concat(once));
        if (hop == "owner-gateway")
        {
            fixture.Routes.Invocations.ShouldAllBe(attempt => attempt.Target == "projects");
            fixture.Routes.Hops.ShouldAllBe(attempt => attempt.Target == "projects" && attempt.Status == 404);
        }
        else
        {
            var refusedDispatches = fixture.Routes.Hops.Where(attempt => attempt.Target == hop).ToArray();
            refusedDispatches.Length.ShouldBe(2); refusedDispatches.ShouldAllBe(attempt => attempt.Status == 401);
            fixture.Routes.Invocations.Where(attempt => attempt.Target == hop).ShouldAllBe(attempt => attempt.AssertionCount == 0 && attempt.AssertionState == "missing");
        }
        fixture.Routes.HumanBearerForwarded.ShouldBeFalse();
        foreach (var attempt in fixture.Routes.TokenRequests) AssertProfileAttempt(attempt);
        fixture.Routes.Attempts.Count.ShouldBe(fixture.Routes.TokenRequests.Count + fixture.Routes.Invocations.Count);
        await fixture.AssertUnchangedAsync(before);
    }

    /// <summary>Separates current grant revocation from already issued JWT lifetime.</summary>
    [Fact]
    public async Task RevokingLoadedGrantStopsNewIssuanceWhileAnExistingJwtRetainsItsBoundedLifetime()
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        (await fixture.ReadAsync<GetProjectResponse>(fixture.Open())).ProjectId.ShouldBe("project-a");
        var previous = fixture.Routes.Hops.Last(hop => hop.Target == "projects");
        fixture.AuthorityOptions.Grants.Single(grant => grant.Audience == "projects").Revoked = true;
        var before = await fixture.SnapshotAsync();
        fixture.Routes.ClearAttempts();
        await fixture.RefusedAsync(fixture.Open(), HttpStatusCode.InternalServerError);
        fixture.Routes.Attempts.ShouldBe([BrokerStep("projects"), ServiceStep("projects", "missing")]);
        fixture.Routes.Hops.Single().Status.ShouldBe(401);
        fixture.Routes.HumanBearerForwarded.ShouldBeFalse();
        await fixture.VerifyHopAsync(previous);
        using HttpClient receiver = new() { BaseAddress = new(fixture.Projects.Urls.Single()) };
        receiver.DefaultRequestHeaders.Add(DaprAppChannelToken.HeaderName, fixture.Channel);
        receiver.DefaultRequestHeaders.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, previous.Assertion);
        using HttpResponseMessage response = await receiver.PostAsJsonAsync("/query", previous.Query, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<QueryResult>(TestContext.Current.CancellationToken);
        payload!.Success.ShouldBeTrue(); payload.GetPayload().Deserialize<GetProjectResponse>(JsonSerializerOptions.Web)!.ProjectId.ShouldBe("project-a");
        await fixture.AssertUnchangedAsync(before);
    }

    /// <summary>Rejects user-issuer assertions despite otherwise valid workload shape and bindings.</summary>
    /// <param name="target">The receiving workload.</param>
    [Theory]
    [InlineData("projects")]
    [InlineData("folders")]
    public async Task PublicUserIssuerCannotAuthenticateAtDedicatedWorkloadReceivers(string target)
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.HumanToken(projectUser: true));
        (await fixture.ReadAsync<GetProjectResponse>(fixture.Open())).ProjectId.ShouldBe("project-a");
        var before = await fixture.SnapshotAsync();
        fixture.Routes.ChangeTarget = target; fixture.Routes.ReplaceAssertion = fixture.UserIssuedWorkloadAssertion;
        await fixture.RefusedAsync(fixture.Open(), target == "projects" ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound);
        fixture.Routes.Hops.Last(hop => hop.Target == target).Status.ShouldBe(401);
        fixture.Routes.HumanBearerForwarded.ShouldBeFalse(); await fixture.AssertUnchangedAsync(before);
    }

    /// <summary>Shows that ordinary/malformed token POSTs reach the real broker and remain in the complete observation sequence.</summary>
    /// <param name="fault">The stripped or malformed authorization-details field.</param>
    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    public async Task BrokerObserverRecordsMissingAndMalformedProfileBeforeRealRejection(string fault)
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        fixture.Routes.ClearAttempts();
        (await fixture.SendTokenControlAsync(fault)).ShouldBe(HttpStatusCode.BadRequest);
        fixture.Routes.Attempts.ShouldBe([$"broker:{fault}:-:-:0"]);
        var attempt = fixture.Routes.TokenRequests.Single();
        attempt.State.ShouldBe(fault); attempt.DetailsCount.ShouldBe(fault == "missing" ? 0 : 1);
        attempt.FieldCount.ShouldBe(fault == "missing" ? 4 : 5); fixture.Routes.Invocations.ShouldBeEmpty();
    }

    /// <summary>Shows unsigned and malformed attempts reach each real receiver instead of disappearing inside the observer.</summary>
    /// <param name="target">The actual receiving workload.</param>
    /// <param name="fault">The assertion header fault.</param>
    [Theory]
    [InlineData("projects", "missing")]
    [InlineData("projects", "malformed")]
    [InlineData("folders", "missing")]
    [InlineData("folders", "malformed")]
    public async Task ReceiverObserverRecordsStrippedAndMalformedAssertionsBeforeRealRejection(string target, string fault)
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.HumanToken(projectUser: true));
        (await fixture.ReadAsync<GetProjectResponse>(fixture.Open())).Name.ShouldBe("Project project-a");
        var before = await fixture.SnapshotAsync(); fixture.Routes.ClearAttempts();
        fixture.Routes.ChangeTarget = target; fixture.Routes.ReplaceAssertion = _ => fault == "missing" ? null : "not-a-jwt";
        await fixture.RefusedAsync(fixture.Open(), target == "projects" ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound);
        fixture.Routes.Attempts.ShouldBe(target == "projects"
            ? [BrokerStep("projects"), ServiceStep(target, fault)]
            : [BrokerStep("projects"), ServiceStep("projects"), BrokerStep("eventstore"), ServiceStep("eventstore"), BrokerStep("folders"), ServiceStep(target, fault)]);
        var attempt = fixture.Routes.Invocations.Last(); attempt.AssertionCount.ShouldBe(fault == "missing" ? 0 : 1);
        fixture.Routes.Hops.Last(hop => hop.Target == target).Status.ShouldBe(401);
        await fixture.AssertUnchangedAsync(before);
    }

    /// <summary>Proves actual host stop and disposal exceptions cannot strand later owned hosts or private material.</summary>
    [Fact]
    public async Task ExceptionalTeardownStillAttemptsEveryHostAndDisposesPrivateMaterial()
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync(failCleanup: true).ConfigureAwait(true);
        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(async () => await fixture.DisposeAsync().ConfigureAwait(true));
        failure.InnerExceptions.Count.ShouldBeGreaterThanOrEqualTo(2);
        fixture.CleanupAttempts.ShouldBe(["client", "host-3-stop", "host-3-dispose", "host-2-stop", "host-2-dispose", "host-1-stop", "host-1-dispose",
            "host-0-stop", "host-0-dispose", "certificate", "user-key", "owner-key", "private-directory"]);
        fixture.PrivateKeysDisposed.ShouldBeTrue(); fixture.PrivateDirectoryExists.ShouldBeFalse();
        await fixture.DisposeAsync();
    }

    /// <summary>Rejects an otherwise correctly signed, scoped assertion for the wrong workload recipient.</summary>
    /// <param name="target">The actual receiver.</param>
    [Theory]
    [InlineData("projects")]
    [InlineData("folders")]
    public async Task CorrectlySignedWrongRecipientAssertionsAreRejectedByBothReceivers(string target)
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.HumanToken(projectUser: true));
        (await fixture.ReadAsync<GetProjectResponse>(fixture.Open())).Name.ShouldBe("Project project-a");
        var before = await fixture.SnapshotAsync(); fixture.Routes.ClearAttempts();
        string? wrong = null;
        fixture.Routes.ChangeTarget = target;
        fixture.Routes.ReplaceAssertion = query => wrong = fixture.BoundaryAssertion(query, "different-recipient", DateTimeOffset.UtcNow);
        await fixture.RefusedAsync(fixture.Open(), target == "projects" ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound);
        await fixture.VerifyBoundarySignatureAsync(wrong!, "different-recipient");
        fixture.Routes.Hops.Last(hop => hop.Target == target).Status.ShouldBe(401);
        fixture.Routes.Attempts.ShouldBe(target == "projects" ? OpenSteps().Take(2) : OpenSteps());
        await fixture.AssertUnchangedAsync(before);
    }

    /// <summary>Distinguishes the broker issuance interval from the real receiver expiry plus configured clock-skew window.</summary>
    /// <param name="target">The actual receiver.</param>
    /// <param name="secondsPastExpiry">How far the synthetic signed token expiry lies in the past.</param>
    [Theory]
    [InlineData("projects", 30)]
    [InlineData("folders", 30)]
    [InlineData("projects", 65)]
    [InlineData("folders", 65)]
    public async Task ReceiverAcceptsOnlyTheConfiguredExpiryAndClockSkewWindow(string target, int secondsPastExpiry)
    {
        var fixture = await ProjectReadAuthorityFixture.CreateAsync().ConfigureAwait(true);
        await using var fixtureLifetime = fixture.ConfigureAwait(true);
        fixture.Client.DefaultRequestHeaders.Authorization = new("Bearer", fixture.HumanToken(projectUser: true));
        (await fixture.ReadAsync<GetProjectResponse>(fixture.Open())).Name.ShouldBe("Project project-a");
        var before = await fixture.SnapshotAsync(); fixture.Routes.ClearAttempts();
        var options = fixture.Host(target).Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(EventStoreWorkloadAuthenticationDefaults.WorkloadScheme);
        options.TokenValidationParameters.ClockSkew.ShouldBe(TimeSpan.FromSeconds(60));
        string? boundary = null;
        fixture.Routes.ChangeTarget = target;
        fixture.Routes.ReplaceAssertion = query => boundary = fixture.BoundaryAssertion(query, target, DateTimeOffset.UtcNow.AddSeconds(-120 - secondsPastExpiry));
        if (secondsPastExpiry < 60) (await fixture.ReadAsync<GetProjectResponse>(fixture.Open())).Name.ShouldBe("Project project-a");
        else await fixture.RefusedAsync(fixture.Open(), target == "projects" ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound);
        await fixture.VerifyBoundarySignatureAsync(boundary!, target);
        var token = new JsonWebToken(boundary!); (token.ValidTo - token.IssuedAt).ShouldBe(TimeSpan.FromSeconds(120));
        fixture.Routes.Hops.Last(hop => hop.Target == target).Status.ShouldBe(secondsPastExpiry < 60 ? 200 : 401);
        fixture.Routes.Attempts.ShouldBe(target == "projects" && secondsPastExpiry > 60 ? OpenSteps().Take(2) : OpenSteps());
        await fixture.AssertUnchangedAsync(before);
    }

    private static string BrokerStep(string audience) => $"broker:profile:{audience}:{(audience == "eventstore" ? EventStoreWorkloadOperations.GatewayOwnerAuthorityQuery : EventStoreWorkloadOperations.DomainServiceQuery)}:{(audience == "eventstore" ? 0 : 3)}";
    private static string ServiceStep(string target, string state = "jwt") => $"service:{target}:{(target == "eventstore" ? "api/v1/queries/owner-authority/workload" : "query")}:{state}";
    private static string[] OpenSteps() => [BrokerStep("projects"), ServiceStep("projects"), BrokerStep("eventstore"), ServiceStep("eventstore"), BrokerStep("folders"), ServiceStep("folders")];
    private static void AssertProfileAttempt(ProjectAuthorityTokenAttempt request)
    {
        request.State.ShouldBe("profile"); request.FieldCount.ShouldBe(5); request.DetailsCount.ShouldBe(1); request.ValidScope.ShouldBeTrue();
        if (request.Operation == EventStoreWorkloadOperations.DomainServiceQuery)
        {
            request.BindingKeys.ShouldBe(new[] { EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType,
                EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType, QueryCallerAuthorityBinding.DigestClaim }.Order(StringComparer.Ordinal));
        }
        else
        {
            request.Operation.ShouldBe(EventStoreWorkloadOperations.GatewayOwnerAuthorityQuery); request.Audience.ShouldBe("eventstore"); request.BindingKeys.ShouldBeEmpty();
        }
    }
    private static void AssertProfileRequests(ProjectReadAuthorityFixture fixture)
    {
        fixture.Routes.TokenRequests.ShouldNotBeEmpty();
        foreach (var request in fixture.Routes.TokenRequests) AssertProfileAttempt(request);
        fixture.Routes.Invocations.ShouldAllBe(attempt => attempt.AssertionCount == 1 && attempt.AssertionState == "jwt");
        fixture.Routes.Attempts.Count.ShouldBe(fixture.Routes.TokenRequests.Count + fixture.Routes.Invocations.Count);
    }
    private static async Task<string[]> InspectionKeysAsync(ProjectReadAuthorityFixture fixture)
    {
        List<string> keys = [];
        foreach (string key in fixture.Audit.Keys.Keys)
        {
            var snapshot = (await fixture.SnapshotAsync(key).ConfigureAwait(true)).Last();
            var record = JsonSerializer.Deserialize<ProjectReadAuditRecord>(snapshot.Bytes, JsonSerializerOptions.Web)!;
            if (record.Outcome == "Inspection") keys.Add(key);
        }
        return [.. keys.Order(StringComparer.Ordinal)];
    }
}
#endif
