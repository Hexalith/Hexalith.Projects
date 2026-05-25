// <copyright file="ProjectsDomainProcessorTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Server;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-2 tests for the <c>/process</c> aggregate-callback <see cref="ProjectsDomainProcessor"/>
/// (AC 1, 2): a valid create yields a success <c>DomainResult</c> carrying exactly one
/// <c>ProjectCreated</c>; a duplicate create against existing state yields a rejection
/// <c>DomainResult</c> (a rejection event, never an exception). Tenant authority comes from the
/// verified envelope.
/// </summary>
public sealed class ProjectsDomainProcessorTests
{
    private const string Tenant = "tenant-a";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    [Fact]
    public async Task ProcessCreate_NewAggregate_YieldsSuccessDomainResultWithProjectCreated()
    {
        ProjectsDomainProcessor processor = CreateProcessor();

        DomainResult result = await processor.ProcessAsync(Envelope(), currentState: null).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        ProjectCreated created = result.Events[0].ShouldBeOfType<ProjectCreated>();
        created.TenantId.ShouldBe(Tenant);
        created.ProjectId.ShouldBe(ProjectIdValue);
        created.OccurredAt.ShouldBe(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task ProcessCreate_DuplicateAgainstExistingState_YieldsRejectionNotException()
    {
        ProjectsDomainProcessor processor = CreateProcessor();

        ProjectState existing = ProjectState.Empty.Apply(
            [ExistingCreatedEvent()],
            new ProjectIdentity(Tenant, new ProjectId(ProjectIdValue)));

        DomainResult result = await processor.ProcessAsync(Envelope(idempotencyKey: "different-key"), existing).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        result.Events[0].ShouldBeOfType<ProjectCreationRejected>().Reason.ShouldBe(Contracts.Ui.ReferenceState.Conflict);
    }

    [Fact]
    public async Task ProcessCreate_MalformedPayload_FailsClosedToRejection()
    {
        ProjectsDomainProcessor processor = CreateProcessor();

        CommandEnvelope envelope = new(
            MessageId: "idem-key-a",
            TenantId: Tenant,
            Domain: ProjectsServerModule.DomainName,
            AggregateId: ProjectIdValue,
            CommandType: ProjectsServerModule.CreateProjectCommandType,
            Payload: Encoding.UTF8.GetBytes("{ not valid json"),
            CorrelationId: "corr-a",
            CausationId: null,
            UserId: "principal-a",
            Extensions: null);

        DomainResult result = await processor.ProcessAsync(envelope, currentState: null).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessCreate_DenyByDefaultEventStoreValidator_FailsClosedToUnauthorizedRejection()
    {
        ProjectsDomainProcessor processor = new(new FixedTimeProvider(DateTimeOffset.UnixEpoch), new DenyAllProjectEventStoreAuthorizationValidator());

        DomainResult result = await processor.ProcessAsync(Envelope(), currentState: null).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        ProjectCreationRejected rejected = result.Events.Single().ShouldBeOfType<ProjectCreationRejected>();
        rejected.Reason.ShouldBe(Contracts.Ui.ReferenceState.Unauthorized);
    }

    [Fact]
    public async Task ProcessUpdateSetup_ExistingState_YieldsProjectSetupUpdated()
    {
        ProjectsDomainProcessor processor = CreateProcessor();
        ProjectState existing = ProjectState.Empty.Apply([ExistingCreatedEvent()], new ProjectIdentity(Tenant, new ProjectId(ProjectIdValue)));

        DomainResult result = await processor.ProcessAsync(UpdateEnvelope(), existing).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        ProjectSetupUpdated updated = result.Events.Single().ShouldBeOfType<ProjectSetupUpdated>();
        updated.Setup.Goals.ShouldBe(["keep continuity current"]);
        updated.OccurredAt.ShouldBe(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task ProcessArchive_ExistingState_YieldsProjectArchived()
    {
        ProjectsDomainProcessor processor = CreateProcessor();
        ProjectState existing = ProjectState.Empty.Apply([ExistingCreatedEvent()], new ProjectIdentity(Tenant, new ProjectId(ProjectIdValue)));

        DomainResult result = await processor.ProcessAsync(ArchiveEnvelope(), existing).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ProjectArchived>().Lifecycle.ShouldBe(Contracts.Ui.ProjectLifecycle.Archived);
    }

    [Fact]
    public async Task ProcessUpdateSetup_InvalidSetup_YieldsSetupRejected()
    {
        ProjectsDomainProcessor processor = CreateProcessor();
        ProjectState existing = ProjectState.Empty.Apply([ExistingCreatedEvent()], new ProjectIdentity(Tenant, new ProjectId(ProjectIdValue)));

        DomainResult result = await processor.ProcessAsync(UpdateEnvelope(rawGoal: "token=abc"), existing).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ProjectSetupUpdateRejected>().RejectedField.ShouldBe("setup.goals");
    }

    [Fact]
    public async Task ProcessUpdateSetup_MissingSchemaVersion_YieldsSetupRejected()
    {
        ProjectsDomainProcessor processor = CreateProcessor();
        ProjectState existing = ProjectState.Empty.Apply([ExistingCreatedEvent()], new ProjectIdentity(Tenant, new ProjectId(ProjectIdValue)));

        DomainResult result = await processor.ProcessAsync(UpdateEnvelope(includeSchemaVersion: false), existing).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ProjectSetupUpdateRejected>().RejectedField.ShouldBe("requestSchemaVersion");
    }

    private static ProjectsDomainProcessor CreateProcessor()
        => new(new FixedTimeProvider(DateTimeOffset.UnixEpoch), new AllowingProjectEventStoreAuthorizationValidator());

    private static ProjectCreated ExistingCreatedEvent() => new(
        Tenant,
        ProjectIdValue,
        "Existing",
        null,
        null,
        Contracts.Ui.ProjectLifecycle.Active,
        "principal-a",
        "corr-existing",
        "task-existing",
        "key-existing",
        "sha256:existing",
        DateTimeOffset.UnixEpoch);

    private static CommandEnvelope Envelope(string idempotencyKey = "idem-key-a")
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            name = "Tracer Bullet",
            description = "A safe description",
            setupMetadata = (string?)null,
        });

        return new CommandEnvelope(
            MessageId: idempotencyKey,
            TenantId: Tenant,
            Domain: ProjectsServerModule.DomainName,
            AggregateId: ProjectIdValue,
            CommandType: ProjectsServerModule.CreateProjectCommandType,
            Payload: payload,
            CorrelationId: "corr-a",
            CausationId: null,
            UserId: "principal-a",
            Extensions: new Dictionary<string, string> { ["taskId"] = "task-a" });
    }

    private static CommandEnvelope UpdateEnvelope(string rawGoal = "keep continuity current", bool includeSchemaVersion = true)
    {
        byte[] payload = includeSchemaVersion
            ? JsonSerializer.SerializeToUtf8Bytes(new
            {
                requestSchemaVersion = "v1",
                setup = new
                {
                    goals = new[] { rawGoal },
                    userInstructions = new[] { "use safe metadata" },
                    preferredSourceKinds = new[] { "conversation" },
                    excludedSourceKinds = new[] { "fileReference" },
                    conversationStartDefaults = new
                    {
                        linkedSourcePolicy = "authorizedReferences",
                    },
                },
            })
            : JsonSerializer.SerializeToUtf8Bytes(new
            {
                setup = new
                {
                    goals = new[] { rawGoal },
                    userInstructions = new[] { "use safe metadata" },
                    preferredSourceKinds = new[] { "conversation" },
                    excludedSourceKinds = new[] { "fileReference" },
                    conversationStartDefaults = new
                    {
                        linkedSourcePolicy = "authorizedReferences",
                    },
                },
            });

        return new CommandEnvelope(
            MessageId: "idem-key-update",
            TenantId: Tenant,
            Domain: ProjectsServerModule.DomainName,
            AggregateId: ProjectIdValue,
            CommandType: ProjectsServerModule.UpdateProjectSetupCommandType,
            Payload: payload,
            CorrelationId: "corr-update",
            CausationId: null,
            UserId: "principal-a",
            Extensions: new Dictionary<string, string> { ["taskId"] = "task-update" });
    }

    private static CommandEnvelope ArchiveEnvelope()
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            archiveIntent = "archive",
            requestSchemaVersion = "v1",
        });

        return new CommandEnvelope(
            MessageId: "idem-key-archive",
            TenantId: Tenant,
            Domain: ProjectsServerModule.DomainName,
            AggregateId: ProjectIdValue,
            CommandType: ProjectsServerModule.ArchiveProjectCommandType,
            Payload: payload,
            CorrelationId: "corr-archive",
            CausationId: null,
            UserId: "principal-a",
            Extensions: new Dictionary<string, string> { ["taskId"] = "task-archive" });
    }

    // Minimal deterministic TimeProvider so the /process callback stamps a fixed OccurredAt.
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
