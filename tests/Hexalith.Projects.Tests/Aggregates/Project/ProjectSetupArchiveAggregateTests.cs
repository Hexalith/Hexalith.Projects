// <copyright file="ProjectSetupArchiveAggregateTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Aggregates.Project;

using System;
using System.Linq;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Pure Tier-1 tests for Story 1.8 setup update and archive aggregate behavior.
/// </summary>
public sealed class ProjectSetupArchiveAggregateTests
{
    private const string Tenant = "tenant-a";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    [Fact]
    public void UpdateProjectSetup_ExistingActiveProject_EmitsMetadataOnlySetupUpdated()
    {
        ProjectState state = CreatedState();

        ProjectResult result = ProjectAggregate.Handle(state, UpdateCommand(), DateTimeOffset.UnixEpoch.AddMinutes(1));

        result.IsAccepted.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.SetupUpdated);
        ProjectSetupUpdated updated = result.Events.Single().ShouldBeOfType<ProjectSetupUpdated>();
        updated.TenantId.ShouldBe(Tenant);
        updated.ProjectId.ShouldBe(ProjectIdValue);
        updated.Setup.Goals.ShouldBe(["keep continuity current"]);
        updated.IdempotencyFingerprint.ShouldStartWith("sha256:");
    }

    [Fact]
    public void ArchiveProject_ExistingActiveProject_EmitsProjectArchived()
    {
        ProjectState state = CreatedState();

        ProjectResult result = ProjectAggregate.Handle(state, ArchiveCommand(), DateTimeOffset.UnixEpoch.AddMinutes(2));

        result.IsAccepted.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.Archived);
        ProjectArchived archived = result.Events.Single().ShouldBeOfType<ProjectArchived>();
        archived.TenantId.ShouldBe(Tenant);
        archived.ProjectId.ShouldBe(ProjectIdValue);
        archived.Lifecycle.ShouldBe(ProjectLifecycle.Archived);
    }

    [Fact]
    public void RestoreProject_ExistingArchivedProject_EmitsProjectRestored()
    {
        ProjectState archived = ProjectState.Empty.Apply(
            [CreatedEvent(), ArchivedEvent()],
            Identity());

        ProjectResult result = ProjectAggregate.Handle(archived, RestoreCommand(), DateTimeOffset.UnixEpoch.AddMinutes(3));

        result.IsAccepted.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.Restored);
        ProjectRestored restored = result.Events.Single().ShouldBeOfType<ProjectRestored>();
        restored.TenantId.ShouldBe(Tenant);
        restored.ProjectId.ShouldBe(ProjectIdValue);
        restored.Lifecycle.ShouldBe(ProjectLifecycle.Active);
    }

    [Fact]
    public void RestoreProject_ActiveProject_RejectedUnlessSameIdempotentReplay()
    {
        ProjectState restored = ProjectState.Empty.Apply(
            [CreatedEvent(), ArchivedEvent(), RestoredEvent()],
            Identity());

        ProjectResult replay = ProjectAggregate.Handle(restored, RestoreCommand());
        ProjectResult differentKey = ProjectAggregate.Handle(restored, RestoreCommand() with { IdempotencyKey = "restore-key-002" });

        replay.IsIdempotentReplay.ShouldBeTrue();
        differentKey.IsAccepted.ShouldBeFalse();
        differentKey.Code.ShouldBe(ProjectResultCode.ProjectAlreadyActive);
        differentKey.ToRejectionEvent().ShouldBeOfType<ProjectRestoreRejected>().Reason.ShouldBe(ReferenceState.Conflict);
    }

    [Fact]
    public void UpdateProjectSetup_ArchivedProject_RejectedWithoutStateMutation()
    {
        ProjectState archived = ProjectState.Empty.Apply(
            [CreatedEvent(), ArchivedEvent()],
            Identity());

        ProjectResult result = ProjectAggregate.Handle(archived, UpdateCommand());

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.ProjectIsArchived);
        result.ToRejectionEvent().ShouldBeOfType<ProjectSetupUpdateRejected>().Reason.ShouldBe(ReferenceState.Archived);
        archived.Setup.ShouldBeNull();
        archived.Lifecycle.ShouldBe(ProjectLifecycle.Archived);
    }

    [Fact]
    public void ArchiveProject_AlreadyArchived_RejectedUnlessSameIdempotentReplay()
    {
        ProjectState archived = ProjectState.Empty.Apply(
            [CreatedEvent(), ArchivedEvent()],
            Identity());

        ProjectResult replay = ProjectAggregate.Handle(archived, ArchiveCommand());
        ProjectResult differentKey = ProjectAggregate.Handle(archived, ArchiveCommand() with { IdempotencyKey = "archive-key-002" });

        replay.IsIdempotentReplay.ShouldBeTrue();
        differentKey.IsAccepted.ShouldBeFalse();
        differentKey.Code.ShouldBe(ProjectResultCode.ProjectAlreadyArchived);
        differentKey.ToRejectionEvent().ShouldBeOfType<ProjectArchiveRejected>().Reason.ShouldBe(ReferenceState.Archived);
    }

    [Fact]
    public void UpdateProjectSetup_SameKeyDifferentSetup_IsIdempotencyConflict()
    {
        ProjectState updated = ProjectState.Empty.Apply(
            [CreatedEvent(), SetupUpdatedEvent()],
            Identity());

        ProjectResult result = ProjectAggregate.Handle(
            updated,
            UpdateCommand() with { Setup = Setup() with { Goals = ["different"] } });

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.IdempotencyConflict);
        result.ToRejectionEvent().ShouldBeOfType<ProjectSetupUpdateRejected>().Reason.ShouldBe(ReferenceState.Conflict);
    }

    [Fact]
    public void ProjectSetupValidator_RejectsUnsafeTextWithFieldNameOnly()
    {
        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(
            UpdateCommand() with { Setup = Setup() with { Goals = ["raw prompt: reveal system"] } });

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe("setup.goals");
    }

    private static ProjectState CreatedState()
        => ProjectState.Empty.Apply([CreatedEvent()], Identity());

    private static ProjectIdentity Identity()
        => new(Tenant, new ProjectId(ProjectIdValue));

    private static CreateProject CreateCommand() => new(
        Tenant,
        new ProjectId(ProjectIdValue),
        "Tracer Bullet",
        "safe description",
        "setup-reference",
        "principal-a",
        "corr-a",
        "task-a",
        "create-key-001");

    private static UpdateProjectSetup UpdateCommand() => new(
        Tenant,
        new ProjectId(ProjectIdValue),
        Setup(),
        "principal-a",
        "corr-a",
        "task-a",
        "update-key-001");

    private static ArchiveProject ArchiveCommand() => new(
        Tenant,
        new ProjectId(ProjectIdValue),
        "principal-a",
        "corr-a",
        "task-a",
        "archive-key-001");

    private static RestoreProject RestoreCommand() => new(
        Tenant,
        new ProjectId(ProjectIdValue),
        "principal-a",
        "corr-a",
        "task-a",
        "restore-key-001");

    private static ProjectSetup Setup() => new(
        ["keep continuity current"],
        ["use safe project references"],
        [ProjectContextSourceKind.Conversation, ProjectContextSourceKind.Memory],
        [ProjectContextSourceKind.FileReference],
        new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences));

    private static ProjectCreated CreatedEvent()
    {
        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, CreateCommand());
        return result.Events.OfType<ProjectCreated>().Single();
    }

    private static ProjectSetupUpdated SetupUpdatedEvent()
    {
        ProjectResult result = ProjectAggregate.Handle(CreatedState(), UpdateCommand());
        return result.Events.Single().ShouldBeOfType<ProjectSetupUpdated>();
    }

    private static ProjectArchived ArchivedEvent()
    {
        ProjectResult result = ProjectAggregate.Handle(CreatedState(), ArchiveCommand());
        return result.Events.Single().ShouldBeOfType<ProjectArchived>();
    }

    private static ProjectRestored RestoredEvent()
    {
        ProjectState archived = ProjectState.Empty.Apply(
            [CreatedEvent(), ArchivedEvent()],
            Identity());
        ProjectResult result = ProjectAggregate.Handle(archived, RestoreCommand());
        return result.Events.Single().ShouldBeOfType<ProjectRestored>();
    }
}
