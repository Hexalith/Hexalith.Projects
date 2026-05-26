// <copyright file="CommandDeliveryIdempotencyTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Replay;

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
/// Tier-1 FS-6 / NFR-7 duplicate-COMMAND-delivery dedup proofs (AC-2). Proves the <b>command</b> axis of
/// at-least-once delivery end-to-end on the trivial Epic-1 event set: a redelivered logical
/// <see cref="CreateProject"/> never produces a second <c>ProjectCreated</c>, equivalence is field-scoped
/// via the canonical idempotency fingerprint (not raw object equality), and a same-key/non-equivalent
/// redelivery is a conflict — never a silent overwrite.
/// </summary>
/// <remarks>
/// <b>This file is the command-delivery axis only.</b> It is kept physically separate from the
/// projection-delivery idempotency proofs (<c>ProjectionDeliveryIdempotencyTests</c>, AC-3) because the
/// epic / FS-6 require the two properties to fail independently. The field-scoped equivalence set is the
/// spine's <c>x-hexalith-idempotency-equivalence</c> list — <c>project_metadata.display_name</c> (the
/// <see cref="CreateProject.Name"/>) and <c>request_schema_version</c> (a pinned constant). Fields NOT in
/// that set (<see cref="CreateProject.Description"/>, <see cref="CreateProject.SetupMetadata"/>) do not
/// change the fingerprint, so a redelivery that differs only in those still <em>replays</em>; a
/// redelivery that differs in the in-set <see cref="CreateProject.Name"/> <em>conflicts</em>.
/// </remarks>
public sealed class CommandDeliveryIdempotencyTests
{
    private const string Tenant = "acme";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    [Fact]
    public void RedeliveredSameCommand_AgainstCreatedState_IsIdempotentReplay_NoSecondEvent()
    {
        ProjectState created = ApplyCreated(Command());

        // Second delivery of the SAME logical command (same key, equivalent payload).
        ProjectResult result = ProjectAggregate.Handle(created, Command());

        result.IsAccepted.ShouldBeFalse();
        result.IsIdempotentReplay.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void RedeliveredReplayResult_AppliedToState_IsANoOp()
    {
        ProjectState created = ApplyCreated(Command());

        // The redelivery yields an empty-event replay result; applying it to state must not change state
        // (no second event ever lands). This proves the end-to-end "redelivered command never produces a
        // second event" property through the state-apply fold, not just at the Handle boundary.
        ProjectResult replay = ProjectAggregate.Handle(created, Command());
        ProjectIdentity identity = new(Tenant, new ProjectId(ProjectIdValue));
        ProjectState after = created.Apply(replay.Events.Cast<IProjectEvent>(), identity);

        replay.Events.ShouldBeEmpty();
        after.ShouldBeSameAs(created); // empty fold returns the same instance.
        after.IsCreated.ShouldBeTrue();
    }

    [Fact]
    public void RedeliveredSameKeyDifferentName_IsConflict_MappedToConflict_NoSecondEvent()
    {
        ProjectState created = ApplyCreated(Command());

        // Same idempotency key, but the IN-equivalence-set display name differs -> the fingerprint
        // differs -> conflict (never a silent overwrite, never a second event).
        ProjectResult result = ProjectAggregate.Handle(created, Command() with { Name = "A Different Name" });

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.IdempotencyConflict);
        result.ToRejectionReason().ShouldBe(ReferenceState.Conflict);
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void EquivalenceIsFieldScoped_DescriptionNotInEquivalenceSet_StillReplays()
    {
        ProjectState created = ApplyCreated(Command());

        // Description is NOT in the spine equivalence set (project_metadata.display_name,
        // request_schema_version), so the canonical fingerprint is unchanged -> this is a replay, not a
        // conflict. This proves equivalence is field-scoped via the fingerprint, not raw object equality.
        ProjectResult result = ProjectAggregate.Handle(
            created,
            Command() with { Description = "A completely different but still safe description" });

        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
        result.IsIdempotentReplay.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void EquivalenceIsFieldScoped_SetupMetadataNotInEquivalenceSet_StillReplays()
    {
        ProjectState created = ApplyCreated(Command());

        // SetupMetadata is also out of the equivalence set -> fingerprint unchanged -> replay.
        ProjectResult result = ProjectAggregate.Handle(
            created,
            Command() with { SetupMetadata = "safe-reference-002" });

        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void EquivalenceIsFingerprintBased_NotRawEquality_ConfirmedAgainstValidator()
    {
        // Cross-check the field-scoped semantics directly against the canonical fingerprint source of
        // truth: only the in-set display name changes the fingerprint; out-of-set fields do not.
        ProjectCommandValidationResult baseline = ProjectCommandValidator.Validate(Command());
        ProjectCommandValidationResult sameNameDifferentDescription =
            ProjectCommandValidator.Validate(Command() with { Description = "different-but-safe" });
        ProjectCommandValidationResult differentName =
            ProjectCommandValidator.Validate(Command() with { Name = "Renamed Project" });

        baseline.IsAccepted.ShouldBeTrue();
        sameNameDifferentDescription.IdempotencyFingerprint.ShouldBe(baseline.IdempotencyFingerprint);
        differentName.IdempotencyFingerprint.ShouldNotBe(baseline.IdempotencyFingerprint);
    }

    /// <summary>
    /// Separation proof: the second (redelivered) command produces no second <c>ProjectCreated</c> when
    /// folded through <see cref="ProjectStateApply"/> — the recorded idempotency fingerprint deduplicates
    /// — independently of the projection-fold idempotency property (which AC-3 proves separately).
    /// </summary>
    [Fact]
    public void SecondDelivery_ProducesNoSecondProjectCreated_ThroughStateApply()
    {
        CreateProject command = Command();
        ProjectIdentity identity = new(Tenant, new ProjectId(ProjectIdValue));

        // First delivery: ProjectCreated plus the degraded ProjectFolderCreationPending event land.
        ProjectResult first = ProjectAggregate.Handle(ProjectState.Empty, command);
        first.Events.Count.ShouldBe(2);
        ProjectState afterFirst = ProjectState.Empty.Apply(first.Events.Cast<IProjectEvent>(), identity);

        // Second delivery of the same command: Handle dedupes to an empty replay, so nothing new folds.
        ProjectResult second = ProjectAggregate.Handle(afterFirst, command);
        second.Events.ShouldBeEmpty();
        ProjectState afterSecond = afterFirst.Apply(second.Events.Cast<IProjectEvent>(), identity);

        // Exactly one create result was ever produced across both deliveries; state is unchanged.
        afterSecond.IsCreated.ShouldBeTrue();
        afterSecond.IdempotencyFingerprints.Count.ShouldBe(2);
        afterSecond.ShouldBeSameAs(afterFirst);
    }

    [Fact]
    public void RedeliveredSameSetProjectFolder_AgainstFolderState_IsIdempotentReplay_NoSecondEvent()
    {
        ProjectState withFolder = ApplySetFolder(ApplyCreated(Command()), SetFolderCommand());

        ProjectResult result = ProjectAggregate.Handle(withFolder, SetFolderCommand());

        result.IsAccepted.ShouldBeFalse();
        result.IsIdempotentReplay.ShouldBeTrue();
        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void RedeliveredSetProjectFolder_SameKeyDifferentFolder_IsConflict_NoSecondEvent()
    {
        ProjectState withFolder = ApplySetFolder(ApplyCreated(Command()), SetFolderCommand());

        ProjectResult result = ProjectAggregate.Handle(
            withFolder,
            SetFolderCommand(folderId: "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AD"));

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.IdempotencyConflict);
        result.ToRejectionReason().ShouldBe(ReferenceState.Conflict);
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void SetProjectFolder_SameFolderDifferentKey_IsSafeNoOpReplay()
    {
        ProjectState withFolder = ApplySetFolder(ApplyCreated(Command()), SetFolderCommand());

        ProjectResult result = ProjectAggregate.Handle(
            withFolder,
            SetFolderCommand(idempotencyKey: "idem-folder-002"));

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.IdempotentReplay);
        result.Events.ShouldBeEmpty();
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

    private static SetProjectFolder SetFolderCommand(
        string folderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
        string idempotencyKey = "idem-folder-001") => new(
        Tenant,
        new ProjectId(ProjectIdValue),
        folderId,
        new ProjectFolderMetadata("Tracer Folder"),
        false,
        "actor-001",
        "corr-001",
        "task-001",
        idempotencyKey);

    private static ProjectState ApplyCreated(CreateProject command)
    {
        ProjectResult accepted = ProjectAggregate.Handle(ProjectState.Empty, command);
        ProjectIdentity identity = new(command.TenantId, command.ProjectId);
        return ProjectState.Empty.Apply(accepted.Events.Cast<IProjectEvent>(), identity);
    }

    private static ProjectState ApplySetFolder(ProjectState state, SetProjectFolder command)
    {
        ProjectResult accepted = ProjectAggregate.Handle(state, command);
        ProjectIdentity identity = new(command.TenantId, command.ProjectId);
        return state.Apply(accepted.Events.Cast<IProjectEvent>(), identity);
    }
}
