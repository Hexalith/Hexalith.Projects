namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text;
using System.Text.Json;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Queries;
using Shouldly;
using Xunit;

/// <summary>Verifies retained operator evidence, scoped pagination and task disclosure.</summary>
[Trait("Story", "8.1")]
public sealed class ProjectOperatorQueryTests
{
    [Fact]
    public async Task ReplayedDomainAndSharedInspectionFactsPageOnceUnderCurrentAuthority()
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync(inspection: true);
        await fixture.SeedAsync("alpha");
        await fixture.SeedAsync("beta", 200);
        var journal = new InMemoryProjectAuditTimelineReadModel();
        ProjectCreated committed = new("tenant-a", "alpha", "Private name", "Private description", null, ProjectLifecycle.Active,
            "Private actor", "Private correlation", "Private task", "Private idempotency", "fingerprint", SupportedProjectReadFixture.EventTime);
        journal.Project("tenant-a", committed);
        journal.Project("tenant-a", committed with { OccurredAt = committed.OccurredAt.AddMilliseconds(20) });
        var token = TestContext.Current.CancellationToken;
        var inspected = fixture.Query(new ListProjectsQuery(InspectDescriptiveMetadata: true));
        (await fixture.Executor.ListAsync(inspected, token)).Success.ShouldBeTrue();
        (await fixture.Executor.ListAsync(inspected, token)).Success.ShouldBeTrue();

        var executor = Executor(fixture, journal);
        QueryEnvelope Query(string project, string? cursor = null) => fixture.Query(new GetProjectAuditQuery(cursor, 1), project)
            with { QueryType = GetProjectAuditQuery.QueryType };
        var first = SupportedProjectReadFixture.Payload<ProjectAuditPage>(await executor.AuditAsync(Query("alpha"), token));
        first.State.ShouldBe("Partial");
        var row = first.Items.Single();
        row.Operation.ShouldBe("metadata.inspected");
        row.ProjectCount.ShouldBe(2);
        row.FieldClass.ShouldBe("ProjectName");
        first.Cursor.ShouldNotBeNull();
        var beta = SupportedProjectReadFixture.Payload<ProjectAuditPage>(await executor.AuditAsync(Query("beta"), token));
        beta.Items.Single().ShouldBe(row);
        (await executor.AuditAsync(Query("beta", first.Cursor), token)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);

        var second = SupportedProjectReadFixture.Payload<ProjectAuditPage>(await executor.AuditAsync(Query("alpha", first.Cursor), token));
        second.Cursor.ShouldBeNull();
        second.Items.Single().Outcome.ShouldBe("Active");
        second.Items.Single().AuditEventId.ShouldStartWith("audit_");
        JsonSerializer.Serialize(second).ShouldNotContain("Private");
        fixture.Authorization.Decision = req => req.ProjectId == "alpha" ? EventStoreAuthorizationValidationResult.Denied()
            : EventStoreAuthorizationValidationResult.Allowed("eventstore_validator_test_allow");
        (await executor.AuditAsync(Query("alpha", first.Cursor), token)).ErrorMessage.ShouldBe("safe-denial");
        (await executor.AuditAsync(Query("missing"), token)).ErrorMessage.ShouldBe("safe-denial");
        (await executor.AuditAsync(Query("alpha") with { TenantId = "other" }, token)).PayloadBytes.ShouldBeNull();
    }

    [Fact]
    [Trait("Trait", "retention")]
    public async Task SecurityIndexSurvivesWriterRestartAndLongerHistory()
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        var query = fixture.Query(new GetProjectQuery(true), "alpha");
        var writer = new ProjectReadAudit(fixture.Store, fixture.Clock);
        (await writer.RecordAsync(query, true, 1, "revision", false, token, ["alpha"])).ShouldBeTrue();
        fixture.Clock.Now += TimeSpan.FromDays(730);
        var restarted = new ProjectReadAudit(fixture.Store, fixture.Clock);
        (await restarted.RecordAsync(query, true, 1, "revision", false, token, ["alpha"])).ShouldBeTrue();
        List<ProjectReadAuditRecord> facts = [];
        await foreach (var fact in restarted.ReadAsync("tenant-a", "alpha", token)) facts.Add(fact);
        facts.Single().RecordedAt.ShouldBe(SupportedProjectReadFixture.EventTime.AddSeconds(1));
        await foreach (var _ in restarted.ReadAsync("tenant-b", "alpha", token)) Assert.Fail("Cross-Tenant fact");
    }

    [Fact]
    public async Task IndexRepairAfterInterruptedWriteAndConcurrentRedeliveryPreservesEveryUniqueFact()
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        var audited = fixture.Query(new GetProjectQuery(true), "alpha");
        var original = new ProjectReadAudit(fixture.AuditStore, fixture.Clock);
        int writes = 0;
        fixture.AuditStore.BeforeWrite = () => ++writes == 2 ? Task.FromException(new IOException("index unavailable")) : Task.CompletedTask;
        (await original.RecordAsync(audited, true, 1, "revision", false, token, ["alpha"])).ShouldBeFalse();

        var restarted = new ProjectReadAudit(fixture.Store, fixture.Clock);
        (await restarted.RecordAsync(audited, true, 1, "revision", false, token, ["alpha"])).ShouldBeTrue();
        for (int i = 0; i < 128; i++)
        {
            var other = audited with { CorrelationId = "other-" + i };
            (await restarted.RecordAsync(other, true, 1, "revision", false, token, ["alpha"])).ShouldBeTrue();
        }
        bool[] delivered = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(
            () => restarted.RecordAsync(audited, true, 1, "revision", false, token, ["alpha"]), token)));
        delivered.ShouldAllBe(success => success);
        List<string> ids = [];
        await foreach (var fact in restarted.ReadAsync("tenant-a", "alpha", token)) ids.Add(fact.RequestSurrogate);
        ids.Count.ShouldBe(129);
        ids.Distinct().Count().ShouldBe(129);
    }

    [Fact]
    public async Task TaskPollRequiresAnExactOwnerDecisionAndNeverDisclosesReservedIdentities()
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync(chatbot: true);
        var token = TestContext.Current.CancellationToken;
        var query = fixture.Query(new GetProjectTaskStatusQuery(), "opaque-task") with { QueryType = GetProjectTaskStatusQuery.QueryType };
        ProjectTaskStatusResponse observation = new("Available", query.AggregateId, "NeedsAttention", "NeedsAttention",
            ["ResolveNeedsAttention"], fixture.Clock.Now, fixture.Clock.Now, null) { ReconciliationOutcome = "Resumed" };
        ProjectAuthorizedTaskObservation bound = new("tenant-a", "actor-1", "actor-1", true, observation);
        var owner = new ProjectTaskStatusTestAuthority(caller => caller.TenantId == "tenant-a" && caller.OriginalActorId == "actor-1"
            && caller.AggregateId == "opaque-task" ? bound : null);
        var executor = Executor(fixture, new(), owner);
        var result = await executor.TaskStatusAsync(query, token);
        result.Success.ShouldBeTrue();
        using var json = JsonDocument.Parse(result.PayloadBytes!);
        json.RootElement.EnumerateObject().Select(p => p.Name).Except(
            ["state", "taskId", "status", "reason", "recoveryActions", "createdAt", "updatedAt", "expiresAt"]).ShouldBeEmpty();
        json.RootElement.GetProperty("status").GetString().ShouldBe("NeedsAttention");
        (await executor.TaskStatusAsync(query with { AggregateId = "foreign-task" }, token)).ErrorMessage.ShouldBe("safe-denial");
        int calls = owner.Calls;
        var wrongSurface = await SupportedProjectReadFixture.CreateAsync();
        wrongSurface.Identity.RemoveClaim(wrongSurface.Identity.FindFirst("projects:role")!);
        wrongSurface.Identity.AddClaim(new System.Security.Claims.Claim("projects:role", "ProjectUser"));
        var direct = wrongSurface.Query(new GetProjectTaskStatusQuery(), "opaque-task") with { QueryType = GetProjectTaskStatusQuery.QueryType };
        (await Executor(wrongSurface, new(), owner).TaskStatusAsync(direct, token)).ErrorMessage.ShouldBe("safe-denial");
        (await executor.TaskStatusAsync(query with { Payload = """{"projectId":"new-target"}"""u8.ToArray() }, token)).Success.ShouldBeFalse();
        (await executor.TaskStatusAsync(query with { OriginalActorId = "impostor" }, token)).PayloadBytes.ShouldBeNull();
        owner.Calls.ShouldBe(calls);
        var unavailable = SupportedProjectReadFixture.Payload<ProjectTaskStatusResponse>(
            await Executor(fixture, new()).TaskStatusAsync(query, token));
        unavailable.State.ShouldBe("Unavailable");
        unavailable.TaskId.ShouldBeNull();

        bound = bound with { PreActivation = false };
        var activated = SupportedProjectReadFixture.Payload<ProjectTaskStatusResponse>(await executor.TaskStatusAsync(query, token));
        activated.ReconciliationOutcome.ShouldBe("Resumed");
        bound = bound with { AdmittedActorId = "another-owner" };
        (await executor.TaskStatusAsync(query, token)).ErrorMessage.ShouldBe("safe-denial");
        bound = bound with { TenantId = "tenant-b", AdmittedActorId = "actor-1" };
        (await executor.TaskStatusAsync(query, token)).ErrorMessage.ShouldBe("safe-denial");
        var bad = new ProjectTaskStatusTestAuthority(_ => new("tenant-a", "actor-1", "actor-1", false, observation with { Reason = "Private Folder name" }));
        var malformed = await Executor(fixture, new(), bad).TaskStatusAsync(query, token);
        Encoding.UTF8.GetString(malformed.PayloadBytes!).ShouldNotContain("Private");
        SupportedProjectReadFixture.Payload<ProjectTaskStatusResponse>(malformed).State.ShouldBe("Unavailable");
    }

    private static ProjectOperatorQueryExecutor Executor(SupportedProjectReadFixture fixture,
        InMemoryProjectAuditTimelineReadModel journal, IProjectTaskStatusAuthority? tasks = null)
        => new(fixture.Store, fixture.Gate, fixture.Binding, journal, new ProjectReadAudit(fixture.Store, fixture.Clock),
            new QueryCursorCodec(fixture.DataProtection, "operator-tests"), fixture.Clock, tasks);
}
