// <copyright file="ProjectAggregateHandleTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Aggregates.Project;

using System;
using System.Linq;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Pure Tier-1 tests for <see cref="ProjectAggregate"/> <c>Handle</c> (AC 1, 2, 3, 6, 7). No Dapr,
/// Aspire, network, containers, or browser. Mirrors the Folders aggregate test patterns.
/// </summary>
public sealed class ProjectAggregateHandleTests
{
    private const string Tenant = "acme";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    [Fact]
    public void HappyPath_EmitsExactlyOneProjectCreatedActive()
    {
        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, Command());

        result.IsAccepted.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.Created);
        result.Events.Count.ShouldBe(1);

        ProjectCreated created = result.Events[0].ShouldBeOfType<ProjectCreated>();
        created.TenantId.ShouldBe(Tenant);
        created.ProjectId.ShouldBe(ProjectIdValue);
        created.Name.ShouldBe("Tracer Bullet");
        created.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        created.IdempotencyFingerprint.ShouldStartWith("sha256:");
    }

    [Fact]
    public void HandleIsPure_DoesNotMutateInputState()
    {
        ProjectState state = ProjectState.Empty;

        _ = ProjectAggregate.Handle(state, Command());

        // The pure Handle must not mutate state — only Apply does. State is an immutable record so this
        // documents the invariant: Empty stays IsCreated == false.
        state.IsCreated.ShouldBeFalse();
        ProjectState.Empty.IsCreated.ShouldBeFalse();
    }

    [Fact]
    public void MissingTenant_FailsClosedWithRejectionNotException()
    {
        CreateProject command = Command() with { TenantId = "  " };

        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, command);

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.Unauthorized);
        result.Events.ShouldBeEmpty();
        result.ToRejectionReason().ShouldBe(ReferenceState.Unauthorized);

        // Rejection is an event, not an exception.
        ProjectCreationRejected rejection = result.ToRejectionEvent();
        rejection.ShouldBeAssignableTo<IRejectionEvent>();
        rejection.Reason.ShouldBe(ReferenceState.Unauthorized);
    }

    [Fact]
    public void ResultIsEitherAcceptedOrRejected_NeverBoth()
    {
        ProjectResult accepted = ProjectAggregate.Handle(ProjectState.Empty, Command());
        accepted.Events.All(e => e is not IRejectionEvent).ShouldBeTrue();

        ProjectResult rejected = ProjectAggregate.Handle(ProjectState.Empty, Command() with { Name = "  " });
        rejected.Events.ShouldBeEmpty();
        rejected.IsAccepted.ShouldBeFalse();
    }

    [Theory]
    [InlineData("  ", nameof(CreateProject.Name))]
    [InlineData("contains a secret value", nameof(CreateProject.Name))]
    public void BlankOrUnsafeName_RejectedWithFieldNameOnly(string name, string expectedField)
    {
        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, Command() with { Name = name });

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe(expectedField);
    }

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\secrets\\token.txt")]
    [InlineData("https://evil.example/raw")]
    [InlineData("api-key=sk_live_abc")]
    public void UnsafeSetupMetadata_RejectedWithFieldNameOnlyNeverEchoesValue(string setup)
    {
        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, Command() with { SetupMetadata = setup });

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe(nameof(CreateProject.SetupMetadata));

        // The rejection event carries the field NAME only; the rejected value never appears.
        ProjectCreationRejected rejection = result.ToRejectionEvent();
        rejection.RejectedField.ShouldBe(nameof(CreateProject.SetupMetadata));
        (rejection.RejectedField ?? string.Empty).ShouldNotContain(setup);
    }

    [Fact]
    public void NoFolderSupplied_SucceedsWithoutOne()
    {
        // The only required input is the name; absent setup metadata succeeds (no auto-folder).
        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, Command() with { SetupMetadata = null, Description = null });

        result.IsAccepted.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<ProjectCreated>().SetupMetadata.ShouldBeNull();
    }

    [Fact]
    public void DuplicateCreate_Rejected()
    {
        ProjectState created = ApplyCreated(Command());

        ProjectResult result = ProjectAggregate.Handle(created, Command() with { IdempotencyKey = "different-key-002" });

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.DuplicateProject);
        result.ToRejectionReason().ShouldBe(ReferenceState.Conflict);
    }

    [Fact]
    public void IdempotentReplay_SameKeySamePayload_NoSecondEvent()
    {
        ProjectState created = ApplyCreated(Command());

        ProjectResult result = ProjectAggregate.Handle(created, Command());

        result.IsAccepted.ShouldBeFalse();
        result.IsIdempotentReplay.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void IdempotencyConflict_SameKeyDifferentPayload_Rejected()
    {
        ProjectState created = ApplyCreated(Command());

        ProjectResult result = ProjectAggregate.Handle(created, Command() with { Name = "A Different Name" });

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.IdempotencyConflict);
        result.ToRejectionReason().ShouldBe(ReferenceState.Conflict);
    }

    [Fact]
    public void DeterministicTimestampOverload_UsesMinValue()
    {
        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, Command());
        result.Events[0].ShouldBeOfType<ProjectCreated>().OccurredAt.ShouldBe(DateTimeOffset.MinValue);
    }

    private static CreateProject Command() => new(
        Tenant,
        new ProjectId(ProjectIdValue),
        "Tracer Bullet",
        "A safe description",
        null,
        "actor-001",
        "corr-001",
        "task-001",
        "idem-key-001");

    private static ProjectState ApplyCreated(CreateProject command)
    {
        ProjectResult accepted = ProjectAggregate.Handle(ProjectState.Empty, command);
        ProjectIdentity identity = new(command.TenantId, command.ProjectId);
        return ProjectState.Empty.Apply(accepted.Events.Cast<IProjectEvent>(), identity);
    }
}
