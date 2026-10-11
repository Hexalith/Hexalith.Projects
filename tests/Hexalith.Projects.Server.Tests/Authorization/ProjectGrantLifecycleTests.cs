using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Authorization;

using Microsoft.Extensions.Configuration;

using Shouldly;

using Xunit;

namespace Hexalith.Projects.Server.Tests.Authorization;

public sealed class ProjectGrantLifecycleTests
{
    private const string Tenant = "tenant-alpha";
    private const string Project = "project-alpha";
    private const string Principal = "actor-alpha";
    private const string Operation = "project-conversation";

    [Fact]
    public async Task ProcessorReplacesExpiredGrantButRejectsActiveGrantAndReusedIssuanceId()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using OwnerEvidenceHandler owner = new(now);
        using HttpClient http = new(owner) { BaseAddress = new Uri("http://eventstore.local/") };
        ProjectOwnerBootstrapVerifier bootstrap = Verifier(http, owner);
        var processor = new ProjectsDomainProcessor(TimeProvider.System,
            new DenyAllProjectEventStoreAuthorizationValidator(), bootstrap);
        ProjectState expired = State(now.AddMinutes(-1));

        var replacement = new GrantProjectChatBotAuthority(Principal, Operation, "human", "Ui",
            "grant-next", now.AddMinutes(10));
        ProjectChatBotAuthorityGranted issued = (await processor.ProcessAsync(Envelope(replacement), expired))
            .Events.Single().ShouldBeOfType<ProjectChatBotAuthorityGranted>();
        issued.GrantId.ShouldBe("grant-next");
        ProjectState replaced = expired.Apply([issued], new ProjectIdentity(Tenant, new ProjectId(Project)));
        replaced.ChatBotGrants.Values.Single().GrantId.ShouldBe("grant-next");
        (await processor.ProcessAsync(Envelope(replacement), State(now.AddMinutes(1)))).Events.Single()
            .ShouldBeOfType<ProjectChatBotAuthorityRejected>().ReasonCode.ShouldBe("grant_active");
        (await processor.ProcessAsync(Envelope(replacement with { GrantId = "grant-prior" }), expired)).Events.Single()
            .ShouldBeOfType<ProjectChatBotAuthorityRejected>().ReasonCode.ShouldBe("invalid_grant");
        owner.Reads.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task OwnerHttpTimeoutFailsClosed()
    {
        using OwnerEvidenceHandler owner = new(DateTimeOffset.UtcNow) { Timeout = true };
        using HttpClient http = new(owner) { BaseAddress = new Uri("http://eventstore.local/") };

        (await Verifier(http, owner).IsCurrentOwnerAsync(Tenant, Principal, "GrantProjectChatBotAuthority",
            TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task OwnerObservationCreatedDuringTenantQueryIsCurrent()
    {
        DateTimeOffset started = DateTimeOffset.UtcNow;
        var clock = new AdjustableClock(started);
        using OwnerEvidenceHandler owner = new(started)
        {
            ObservationTime = clock.GetUtcNow,
            BeforeTenantResponse = () => clock.UtcNow = started.AddSeconds(1),
        };
        using HttpClient http = new(owner) { BaseAddress = new Uri("http://eventstore.local/") };

        (await Verifier(http, owner, clock).IsCurrentOwnerAsync(Tenant, Principal,
            "GrantProjectChatBotAuthority", TestContext.Current.CancellationToken)).ShouldBeTrue();
        owner.Reads.ShouldBe(2);
    }

    private static ProjectOwnerBootstrapVerifier Verifier(HttpClient http, OwnerEvidenceHandler owner,
        TimeProvider? clock = null)
    {
        using RSA key = RSA.Create(2048);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
        }).Build();
        owner.PublicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
        TimeProvider effectiveClock = clock ?? TimeProvider.System;
        return new(new ProjectChatBotAuthorityGateway(http, new Issuer(), configuration, effectiveClock),
            effectiveClock);
    }

    private static ProjectState State(DateTimeOffset expiry)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var identity = new ProjectIdentity(Tenant, new ProjectId(Project));
        return ProjectState.Empty.Apply(
        [
            new ProjectCreated(Tenant, Project, "Project", null, null, ProjectLifecycle.Active,
                Principal, "corr-create", "task-create", "message-create", "fingerprint-create", now),
            new ProjectChatBotAuthorityGranted(Tenant, Project, Principal, Operation, "human", "Ui",
                "grant-prior", expiry, Principal, "corr-prior", "task-prior", "message-prior",
                "fingerprint-prior", now),
        ], identity);
    }

    private static CommandEnvelope Envelope(object payload)
        => new("message-next", Tenant, "projects", Project,
            ProjectsServerModule.GrantProjectChatBotAuthorityCommandType,
            JsonSerializer.SerializeToUtf8Bytes(payload), "corr-next", null, Principal, null);

    private sealed class Issuer : IWorkloadAssertionIssuer
    {
        public bool CanBindResources => true;

        public ValueTask<string?> IssueAsync(WorkloadAssertionRequest request, CancellationToken cancellationToken = default)
        {
            request.Audience.ShouldBe("eventstore");
            request.Operation.ShouldBe("eventstore:owner-authority-query");
            return ValueTask.FromResult<string?>("test-workload-assertion");
        }
    }

    private sealed class OwnerEvidenceHandler(DateTimeOffset now) : HttpMessageHandler
    {
        public Action? BeforeTenantResponse { get; set; }

        public Func<DateTimeOffset>? ObservationTime { get; set; }

        public bool Timeout { get; set; }

        public int Reads { get; private set; }

        public string? PublicKey { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Timeout) { throw new TaskCanceledException("owner read timed out"); }
            request.RequestUri!.AbsolutePath.ShouldBe("/api/v1/queries/owner-authority/workload");
            request.Headers.Contains("X-Hexalith-Bound-Owner-Query-Proof").ShouldBeTrue();
            request.Headers.Contains(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).ShouldBeTrue();
            BoundOwnerQueryRequest bound = (await request.Content!.ReadFromJsonAsync<BoundOwnerQueryRequest>(cancellationToken)
                .ConfigureAwait(false)).ShouldNotBeNull();
            BoundOwnerQueryProof.Verify(bound,
                request.Headers.GetValues(BoundOwnerQueryProof.HeaderName).Single(), "projects", PublicKey,
                ObservationTime?.Invoke() ?? now)
                .ShouldBeTrue();
            string operation = bound.Query.Payload!.Value.GetProperty("operation").GetString()!;
            string domain = bound.Query.Domain;
            if (domain == "tenants") { BeforeTenantResponse?.Invoke(); }
            DateTimeOffset observedAt = ObservationTime?.Invoke() ?? now;
            object evidence = domain == "tenants"
                ? new { tenantId = Tenant, principalId = Principal, operation, actorClass = "human",
                    origin = "Api", allowed = true, sourceVersion = 1, observationId = "observation-owner", observedAt }
                : new { tenantId = Tenant, principalId = Principal, partyId = Principal, operation,
                    actorClass = "human", origin = "Api", allowed = true, sourceVersion = 1,
                    observationId = "observation-party", observedAt };
            Reads++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SubmitQueryResponse("corr", JsonSerializer.SerializeToElement(evidence))),
            };
        }
    }

    private sealed class AdjustableClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
