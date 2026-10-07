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
        result.Events.Count.ShouldBe(2);
        ProjectCreated created = result.Events[0].ShouldBeOfType<ProjectCreated>();
        created.TenantId.ShouldBe(Tenant);
        created.ProjectId.ShouldBe(ProjectIdValue);
        created.OccurredAt.ShouldBe(DateTimeOffset.UnixEpoch);
        result.Events[1].ShouldBeOfType<ProjectFolderCreationPending>();
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
    public async Task ProcessArchive_DaprJsonRoundTripState_RehydratesEventStoreHistory()
    {
        ProjectsDomainProcessor processor = CreateProcessor();
        ProjectCreated created = ExistingCreatedEvent();
        var persisted = new Hexalith.EventStore.Contracts.Events.EventEnvelope(
            new Hexalith.EventStore.Contracts.Events.EventMetadata(
                "message-created",
                ProjectIdValue,
                "Project",
                Tenant,
                ProjectsServerModule.DomainName,
                1,
                1,
                created.OccurredAt,
                created.CorrelationId,
                created.IdempotencyKey,
                created.ActorPrincipalId,
                "1.0.0",
                typeof(ProjectCreated).FullName!,
                1,
                "json"),
            JsonSerializer.SerializeToUtf8Bytes(created),
            null);
        var currentState = new DomainServiceCurrentState(null, [persisted], 0, 1);
        JsonElement daprRoundTrip = JsonSerializer.Deserialize<JsonElement>(
            JsonSerializer.SerializeToUtf8Bytes(currentState, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        DomainResult result = await processor.ProcessAsync(ArchiveEnvelope(), daprRoundTrip).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ProjectArchived>().Lifecycle.ShouldBe(Contracts.Ui.ProjectLifecycle.Archived);
    }

    [Fact]
    public async Task ProcessArchive_HistoryWithPersistedRejection_SkipsRejectionAndRehydrates()
    {
        ProjectsDomainProcessor processor = CreateProcessor();
        DomainServiceCurrentState currentState = new(
            null,
            [
                Persisted(ExistingCreatedEvent(), 1),
                Persisted(new ProjectRestoreRejected(new ProjectId(ProjectIdValue), Tenant, Contracts.Ui.ReferenceState.Conflict), 2),
            ],
            0,
            2);

        DomainResult result = await processor.ProcessAsync(ArchiveEnvelope(), DaprRoundTrip(currentState)).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ProjectArchived>();
    }

    [Fact]
    public async Task ProcessArchive_FlatProjectStateSnapshotPlusTail_AppliesTailAfterSnapshot()
    {
        ProjectsDomainProcessor processor = CreateProcessor();
        ProjectState snapshot = ProjectState.Empty.Apply([ExistingCreatedEvent()], new ProjectIdentity(Tenant, new ProjectId(ProjectIdValue)));
        DomainServiceCurrentState currentState = new(
            JsonSerializer.SerializeToElement(snapshot, WebJson),
            [Persisted(ArchivedEvent(), 2)],
            1,
            2);

        DomainResult result = await processor.ProcessAsync(ArchiveEnvelope(), DaprRoundTrip(currentState)).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ProjectArchiveRejected>().Reason.ShouldBe(Contracts.Ui.ReferenceState.Archived);
    }

    [Fact]
    public async Task ProcessArchive_NestedSnapshotAwareSnapshotPlusTail_RehydratesSnapshotHistoryRecursively()
    {
        ProjectsDomainProcessor processor = CreateProcessor();

        // EventStore snapshots the whole snapshot-aware state: the snapshot carries the pre-snapshot history.
        DomainServiceCurrentState snapshot = new(null, [Persisted(ExistingCreatedEvent(), 1)], 0, 1);
        DomainServiceCurrentState currentState = new(snapshot, [Persisted(ArchivedEvent(), 2)], 1, 2);

        DomainResult result = await processor.ProcessAsync(ArchiveEnvelope(), DaprRoundTrip(currentState)).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ProjectArchiveRejected>().Reason.ShouldBe(Contracts.Ui.ReferenceState.Archived);
    }

    [Theory]
    [InlineData("tenant-b", ProjectIdValue)]
    [InlineData(Tenant, "01HZ9K8YQ3W6V2N4R7T5P0X1ZZ")]
    public async Task ProcessArchive_HistoryEventFromAnotherStream_Throws(string tenant, string aggregateId)
    {
        ProjectsDomainProcessor processor = CreateProcessor();
        DomainServiceCurrentState currentState = new(
            null,
            [
                Persisted(ExistingCreatedEvent(), 1),
                Persisted(new ProjectRestoreRejected(new ProjectId(ProjectIdValue), Tenant, Contracts.Ui.ReferenceState.Conflict), 2, tenant, aggregateId),
            ],
            0,
            2);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => processor.ProcessAsync(ArchiveEnvelope(), DaprRoundTrip(currentState))).ConfigureAwait(true);
    }

    [Fact]
    public async Task ProcessSetProjectFolder_ExistingState_YieldsProjectFolderSet()
    {
        ProjectsDomainProcessor processor = CreateProcessor();
        ProjectState existing = ProjectState.Empty.Apply([ExistingCreatedEvent()], new ProjectIdentity(Tenant, new ProjectId(ProjectIdValue)));

        DomainResult result = await processor.ProcessAsync(SetFolderEnvelope(), existing).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        ProjectFolderSet folderSet = result.Events.Single().ShouldBeOfType<ProjectFolderSet>();
        folderSet.FolderId.ShouldBe("folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC");
        folderSet.FolderMetadata.DisplayName.ShouldBe("Tracer Folder");
    }

    [Fact]
    public async Task ProcessConfirmProjectResolution_ExistingState_YieldsProjectResolutionConfirmed()
    {
        ProjectsDomainProcessor processor = CreateProcessor();
        ProjectState existing = ProjectState.Empty.Apply([ExistingCreatedEvent()], new ProjectIdentity(Tenant, new ProjectId(ProjectIdValue)));

        DomainResult result = await processor.ProcessAsync(ConfirmEnvelope(), existing).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        ProjectResolutionConfirmed confirmed = result.Events.Single().ShouldBeOfType<ProjectResolutionConfirmed>();
        confirmed.ProjectId.ShouldBe(ProjectIdValue);
        confirmed.ConversationId.ShouldBe("conversation-001");
        confirmed.SourceProjectId.ShouldBe("project-source-001");
        confirmed.OccurredAt.ShouldBe(DateTimeOffset.UnixEpoch);
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

    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static JsonElement DaprRoundTrip(DomainServiceCurrentState currentState)
        => JsonSerializer.Deserialize<JsonElement>(JsonSerializer.SerializeToUtf8Bytes(currentState, WebJson), WebJson);

    private static Hexalith.EventStore.Contracts.Events.EventEnvelope Persisted(
        object payload,
        long sequence,
        string tenant = Tenant,
        string aggregateId = ProjectIdValue)
        => new(
            new Hexalith.EventStore.Contracts.Events.EventMetadata(
                $"message-{sequence}",
                aggregateId,
                "Project",
                tenant,
                ProjectsServerModule.DomainName,
                sequence,
                sequence,
                DateTimeOffset.UnixEpoch,
                "corr-history",
                "key-history",
                "principal-a",
                "1.0.0",
                payload.GetType().FullName!,
                1,
                "json"),
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()),
            null);

    private static ProjectArchived ArchivedEvent() => new(
        Tenant,
        ProjectIdValue,
        Contracts.Ui.ProjectLifecycle.Archived,
        "principal-a",
        "corr-archived",
        "task-archived",
        "key-archived",
        "sha256:archived",
        DateTimeOffset.UnixEpoch);

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

    private static CommandEnvelope SetFolderEnvelope()
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            requestSchemaVersion = "v1",
            operation = "set",
            projectId = ProjectIdValue,
            folderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
            folderMetadata = new
            {
                displayName = "Tracer Folder",
            },
            replacementConfirmed = false,
        });

        return new CommandEnvelope(
            MessageId: "idem-key-folder",
            TenantId: Tenant,
            Domain: ProjectsServerModule.DomainName,
            AggregateId: ProjectIdValue,
            CommandType: ProjectsServerModule.SetProjectFolderCommandType,
            Payload: payload,
            CorrelationId: "corr-folder",
            CausationId: null,
            UserId: "principal-a",
            Extensions: new Dictionary<string, string> { ["taskId"] = "task-folder" });
    }

    private static CommandEnvelope ConfirmEnvelope()
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            requestSchemaVersion = "v1",
            operation = "confirm",
            projectId = ProjectIdValue,
            conversationId = "conversation-001",
            sourceProjectId = "project-source-001",
            resolutionResult = "MultipleCandidates",
        });

        return new CommandEnvelope(
            MessageId: "idem-key-confirm",
            TenantId: Tenant,
            Domain: ProjectsServerModule.DomainName,
            AggregateId: ProjectIdValue,
            CommandType: ProjectsServerModule.ConfirmProjectResolutionCommandType,
            Payload: payload,
            CorrelationId: "corr-confirm",
            CausationId: null,
            UserId: "principal-a",
            Extensions: new Dictionary<string, string> { ["taskId"] = "task-confirm" });
    }

    // Minimal deterministic TimeProvider so the /process callback stamps a fixed OccurredAt.
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
