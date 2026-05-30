// <copyright file="ProjectAggregateResolutionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Aggregates.Project;

using System.Linq;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>Pure Tier-1 tests for confirm-resolution aggregate behavior.</summary>
public sealed class ProjectAggregateResolutionTests
{
    private const string Tenant = "acme";
    private const string ProjectIdValue = "project-target-001";

    [Fact]
    public void ConfirmResolution_Accepted_EmitsMetadataOnlyConfirmedEvent()
    {
        ProjectState state = CreatedState();

        ProjectResult result = ProjectAggregate.Handle(state, Command());

        result.IsAccepted.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.ProjectResolutionConfirmed);
        ProjectResolutionConfirmed confirmed = result.Events.Single().ShouldBeOfType<ProjectResolutionConfirmed>();
        confirmed.ProjectId.ShouldBe(ProjectIdValue);
        confirmed.ConversationId.ShouldBe("conversation-001");
        confirmed.SourceProjectId.ShouldBe("project-source-001");
        confirmed.IdempotencyFingerprint.ShouldBe(ProjectCommandValidator.Validate(Command()).IdempotencyFingerprint);
    }

    [Fact]
    public void ConfirmResolution_IdempotentReplay_ProducesNoSecondEvent()
    {
        ProjectState state = CreatedState();
        ProjectResult first = ProjectAggregate.Handle(state, Command());
        ProjectIdentity identity = new(Tenant, new ProjectId(ProjectIdValue));
        ProjectState afterFirst = state.Apply(first.Events.Cast<IProjectEvent>(), identity);

        ProjectResult replay = ProjectAggregate.Handle(afterFirst, Command());

        replay.IsIdempotentReplay.ShouldBeTrue();
        replay.Events.ShouldBeEmpty();
    }

    [Fact]
    public void ConfirmResolution_SameKeyDifferentConversation_IsConflict()
    {
        ProjectState state = CreatedState();
        ProjectResult first = ProjectAggregate.Handle(state, Command());
        ProjectIdentity identity = new(Tenant, new ProjectId(ProjectIdValue));
        ProjectState afterFirst = state.Apply(first.Events.Cast<IProjectEvent>(), identity);

        ProjectResult conflict = ProjectAggregate.Handle(afterFirst, Command() with { ConversationId = "conversation-002" });

        conflict.Code.ShouldBe(ProjectResultCode.IdempotencyConflict);
        conflict.ToRejectionReason().ShouldBe(ReferenceState.Conflict);
    }

    [Fact]
    public void ConfirmResolution_SourceEqualsTarget_IsValidationFailure()
    {
        ProjectResult result = ProjectAggregate.Handle(
            CreatedState(),
            Command() with { SourceProjectId = new ProjectId(ProjectIdValue) });

        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe(nameof(ConfirmProjectResolution.SourceProjectId));
        result.ToRejectionEvent().ShouldBeOfType<ProjectResolutionConfirmationRejected>();
    }

    [Fact]
    public void ConfirmResolution_ArchivedTarget_IsRejected()
    {
        ProjectState state = CreatedState() with { Lifecycle = ProjectLifecycle.Archived };

        ProjectResult result = ProjectAggregate.Handle(state, Command());

        result.Code.ShouldBe(ProjectResultCode.ProjectIsArchived);
        result.ToRejectionEvent().ShouldBeOfType<ProjectResolutionConfirmationRejected>();
    }

    [Fact]
    public void ApplyProjectResolutionConfirmed_RecordsOnlyIdempotency()
    {
        ProjectState state = CreatedState();
        ProjectResult accepted = ProjectAggregate.Handle(state, Command());
        ProjectIdentity identity = new(Tenant, new ProjectId(ProjectIdValue));

        ProjectState applied = state.Apply(accepted.Events.Cast<IProjectEvent>(), identity);

        applied.IdempotencyFingerprints.ShouldContainKey("idem-confirm-001");
        applied.ProjectId.ShouldBe(state.ProjectId);
        applied.FileReferences.ShouldBeEmpty();
        applied.MemoryReferences.ShouldBeEmpty();
    }

    private static ConfirmProjectResolution Command() => new(
        Tenant,
        new ProjectId(ProjectIdValue),
        "conversation-001",
        new ProjectId("project-source-001"),
        "actor-001",
        "corr-001",
        "task-001",
        "idem-confirm-001");

    private static ProjectState CreatedState()
    {
        CreateProject command = new(
            Tenant,
            new ProjectId(ProjectIdValue),
            "Target",
            null,
            null,
            "actor-001",
            "corr-create",
            "task-create",
            "idem-create-001");
        ProjectResult accepted = ProjectAggregate.Handle(ProjectState.Empty, command);
        return ProjectState.Empty.Apply(accepted.Events.Cast<IProjectEvent>(), new ProjectIdentity(Tenant, command.ProjectId));
    }
}
