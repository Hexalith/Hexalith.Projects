// <copyright file="ProjectAggregateMemoryTests.cs" company="Hexalith">
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
/// Pure Tier-1 tests for the optional Memory Reference handlers of <see cref="ProjectAggregate"/>
/// (Story 2.7, AC 2, 3, 4, 8). No Dapr, Aspire, network, containers, or browser. Convergence is
/// asserted purely; no Thread.Sleep / Task.Delay / SpinWait / wall-clock polling.
/// </summary>
public sealed class ProjectAggregateMemoryTests
{
    private const string Tenant = "acme";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string MemoryReferenceId = "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1";

    [Fact]
    public void LinkMemory_InitialReference_EmitsMetadataOnlyEvent()
    {
        ProjectResult result = ProjectAggregate.Handle(Created(), LinkCommand());

        result.IsAccepted.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.MemoryLinked);
        MemoryLinked linked = result.Events.Single().ShouldBeOfType<MemoryLinked>();
        linked.MemoryReferenceId.ShouldBe(MemoryReferenceId);
        linked.MemoryMetadata.DisplayName.ShouldBe("Q3 product strategy memory");
    }

    [Fact]
    public void LinkMemory_DoesNotTouchProjectFolderOrFileReferences()
    {
        ProjectState created = Created();
        ProjectState afterLink = ApplyLink(created, LinkCommand());

        afterLink.ProjectFolder.ShouldBe(created.ProjectFolder);
        afterLink.FileReferences.Count.ShouldBe(0);
        afterLink.MemoryReferences.Count.ShouldBe(1);
    }

    [Fact]
    public void LinkMemory_DuplicateEquivalentDifferentKey_IsIdempotentReplay()
    {
        ProjectState withMemory = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(withMemory, LinkCommand(idempotencyKey: "idem-memory-002"));

        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void LinkMemory_SameReferenceConflictingMetadata_IsConflict()
    {
        ProjectState withMemory = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(
            withMemory,
            LinkCommand(displayName: "Conflicting display", idempotencyKey: "idem-memory-002"));

        result.Code.ShouldBe(ProjectResultCode.MemoryReferenceConflict);
        result.ToRejectionReason().ShouldBe(ReferenceState.Conflict);
    }

    [Fact]
    public void LinkMemory_SameKeySamePayload_IsIdempotentReplay()
    {
        ProjectState withMemory = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(withMemory, LinkCommand());

        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
    }

    [Fact]
    public void LinkMemory_SameKeyDifferentPayload_IsIdempotencyConflict()
    {
        ProjectState withMemory = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(
            withMemory,
            LinkCommand(memoryReferenceId: "case_01HZ9K8YQ3W6V2N4R7T5P0X1M2"));

        result.Code.ShouldBe(ProjectResultCode.IdempotencyConflict);
    }

    [Fact]
    public void LinkMemory_MultipleDistinctReferences_AreAllStored()
    {
        ProjectState withFirst = ApplyLink(Created(), LinkCommand());
        ProjectState withSecond = ApplyLink(
            withFirst,
            LinkCommand(memoryReferenceId: "case_01HZ9K8YQ3W6V2N4R7T5P0X1M2", idempotencyKey: "idem-memory-002"));

        withSecond.MemoryReferences.Count.ShouldBe(2);
        withSecond.MemoryReferences.ShouldContainKey(MemoryReferenceId);
        withSecond.MemoryReferences.ShouldContainKey("case_01HZ9K8YQ3W6V2N4R7T5P0X1M2");
    }

    [Fact]
    public void LinkMemory_BoundedSetExceeded_IsRejected()
    {
        ProjectState state = Created();
        for (int i = 0; i < ProjectState.MaxMemoryReferences; i++)
        {
            state = ApplyLink(state, LinkCommand(memoryReferenceId: $"case_{i:D26}", idempotencyKey: $"idem-{i:D4}"));
        }

        ProjectResult result = ProjectAggregate.Handle(
            state,
            LinkCommand(memoryReferenceId: "case_01HZ9K8YQ3W6V2N4R7T5P0X1M9", idempotencyKey: "idem-over"));

        result.Code.ShouldBe(ProjectResultCode.MemoryReferenceLimitExceeded);
    }

    [Fact]
    public void LinkMemory_ProjectNotCreated_IsProjectNotFound()
    {
        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, LinkCommand());

        result.Code.ShouldBe(ProjectResultCode.ProjectNotFound);
    }

    [Fact]
    public void LinkMemory_TenantMismatch_IsRejected()
    {
        ProjectResult result = ProjectAggregate.Handle(Created(), LinkCommand() with { TenantId = "other-tenant" });

        result.Code.ShouldBe(ProjectResultCode.TenantMismatch);
    }

    [Fact]
    public void LinkMemory_ArchivedProject_IsRejected()
    {
        ProjectResult result = ProjectAggregate.Handle(Archived(), LinkCommand());

        result.Code.ShouldBe(ProjectResultCode.ProjectIsArchived);
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("bad id")]
    [InlineData("")]
    public void LinkMemory_MalformedMemoryReferenceId_IsValidationFailed(string memoryReferenceId)
    {
        ProjectResult result = ProjectAggregate.Handle(Created(), LinkCommand(memoryReferenceId: memoryReferenceId));

        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe(nameof(LinkMemory.MemoryReferenceId));
    }

    [Fact]
    public void UnlinkMemory_ExistingReference_EmitsMetadataOnlyEvent()
    {
        ProjectState withMemory = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(withMemory, UnlinkCommand());

        result.IsAccepted.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.MemoryUnlinked);
        result.Events.Single().ShouldBeOfType<MemoryUnlinked>().MemoryReferenceId.ShouldBe(MemoryReferenceId);
    }

    [Fact]
    public void UnlinkMemory_MissingReference_IsIdempotentNoOp()
    {
        ProjectResult result = ProjectAggregate.Handle(Created(), UnlinkCommand());

        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void UnlinkMemory_RemovesOnlyTargetedReference()
    {
        ProjectState withTwo = ApplyLink(
            ApplyLink(Created(), LinkCommand()),
            LinkCommand(memoryReferenceId: "case_01HZ9K8YQ3W6V2N4R7T5P0X1M2", idempotencyKey: "idem-memory-002"));

        ProjectState afterUnlink = ApplyUnlink(withTwo, UnlinkCommand(idempotencyKey: "idem-unlink-001"));

        afterUnlink.MemoryReferences.ShouldNotContainKey(MemoryReferenceId);
        afterUnlink.MemoryReferences.ShouldContainKey("case_01HZ9K8YQ3W6V2N4R7T5P0X1M2");
    }

    [Fact]
    public void UnlinkMemory_ProjectNotCreated_IsProjectNotFound()
    {
        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, UnlinkCommand());

        result.Code.ShouldBe(ProjectResultCode.ProjectNotFound);
    }

    [Fact]
    public void UnlinkMemory_TenantMismatch_IsRejected()
    {
        ProjectState withMemory = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(withMemory, UnlinkCommand() with { TenantId = "other-tenant" });

        result.Code.ShouldBe(ProjectResultCode.TenantMismatch);
    }

    [Fact]
    public void UnlinkMemory_ArchivedProject_IsRejected()
    {
        ProjectResult result = ProjectAggregate.Handle(Archived(), UnlinkCommand());

        result.Code.ShouldBe(ProjectResultCode.ProjectIsArchived);
    }

    [Fact]
    public void UnlinkMemory_MalformedMemoryReferenceId_IsValidationFailed()
    {
        ProjectResult result = ProjectAggregate.Handle(Created(), UnlinkCommand(memoryReferenceId: "../x"));

        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe(nameof(UnlinkMemory.MemoryReferenceId));
    }

    [Fact]
    public void LinkRejection_MapsToReferenceLinkRejectedWithMemoryKind()
    {
        ProjectResult result = ProjectAggregate.Handle(Archived(), LinkCommand());

        ProjectReferenceLinkRejected rejection = result.ToRejectionEvent().ShouldBeOfType<ProjectReferenceLinkRejected>();
        rejection.ReferenceKind.ShouldBe("memory");
        rejection.ReferenceId.ShouldBe(MemoryReferenceId);
        rejection.Reason.ShouldBe(ReferenceState.Archived);
    }

    [Fact]
    public void UnlinkRejection_MapsToReferenceUnlinkRejectedWithMemoryKind()
    {
        ProjectState withMemory = ApplyLink(Created(), LinkCommand());

        ProjectResult result = ProjectAggregate.Handle(withMemory, UnlinkCommand() with { TenantId = "other-tenant" });

        ProjectReferenceUnlinkRejected rejection = result.ToRejectionEvent().ShouldBeOfType<ProjectReferenceUnlinkRejected>();
        rejection.ReferenceKind.ShouldBe("memory");
        rejection.ReferenceId.ShouldBe(MemoryReferenceId);
        rejection.Reason.ShouldBe(ReferenceState.TenantMismatch);
    }

    private static LinkMemory LinkCommand(
        string memoryReferenceId = MemoryReferenceId,
        string? displayName = "Q3 product strategy memory",
        string idempotencyKey = "idem-memory-001") => new(
        Tenant,
        new ProjectId(ProjectIdValue),
        memoryReferenceId,
        new ProjectMemoryReferenceMetadata(displayName),
        "actor-001",
        "corr-001",
        "task-001",
        idempotencyKey);

    private static UnlinkMemory UnlinkCommand(
        string memoryReferenceId = MemoryReferenceId,
        string idempotencyKey = "idem-unlink-001") => new(
        Tenant,
        new ProjectId(ProjectIdValue),
        memoryReferenceId,
        "actor-001",
        "corr-001",
        "task-001",
        idempotencyKey);

    private static ProjectState Created()
    {
        CreateProject create = new(
            Tenant,
            new ProjectId(ProjectIdValue),
            "Tracer Bullet",
            null,
            null,
            "actor-001",
            "corr-001",
            "task-001",
            "idem-create-001");
        ProjectResult accepted = ProjectAggregate.Handle(ProjectState.Empty, create);
        ProjectIdentity identity = new(create.TenantId, create.ProjectId);
        return ProjectState.Empty.Apply(accepted.Events.Cast<IProjectEvent>(), identity);
    }

    private static ProjectState Archived()
    {
        ProjectState created = Created();
        ArchiveProject archive = new(
            Tenant,
            new ProjectId(ProjectIdValue),
            "actor-001",
            "corr-001",
            "task-001",
            "idem-archive-001");
        ProjectResult accepted = ProjectAggregate.Handle(created, archive);
        ProjectIdentity identity = new(archive.TenantId, archive.ProjectId);
        return created.Apply(accepted.Events.Cast<IProjectEvent>(), identity);
    }

    private static ProjectState ApplyLink(ProjectState state, LinkMemory command)
    {
        ProjectResult accepted = ProjectAggregate.Handle(state, command);
        ProjectIdentity identity = new(command.TenantId, command.ProjectId);
        return state.Apply(accepted.Events.Cast<IProjectEvent>(), identity);
    }

    private static ProjectState ApplyUnlink(ProjectState state, UnlinkMemory command)
    {
        ProjectResult accepted = ProjectAggregate.Handle(state, command);
        ProjectIdentity identity = new(command.TenantId, command.ProjectId);
        return state.Apply(accepted.Events.Cast<IProjectEvent>(), identity);
    }
}
