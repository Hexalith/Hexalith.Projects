// <copyright file="ProjectResultCode.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aggregates.Project;

using System.Text.Json.Serialization;

/// <summary>
/// Aggregate-internal control-flow result code for <see cref="ProjectAggregate"/> (AR-3). Mirrors the
/// Folders <c>FolderResultCode</c> role: a minimal, name-based enum that drives <c>Handle</c>'s
/// accept/reject branching. Externally-surfaced rejection reasons use the shared
/// <see cref="Hexalith.Projects.Contracts.Ui.ReferenceState"/> vocabulary — never a parallel error
/// enum — via <see cref="ProjectResult.ToRejectionReason"/>.
/// </summary>
/// <remarks>
/// Name-based JSON is mandatory so the integer ordinal stays an internal implementation detail and the
/// wire shape (any diagnostic surface) serializes the member NAME, keeping the contract stable when
/// members are inserted, renamed, or renumbered (NFR-6).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectResultCode>))]
public enum ProjectResultCode
{
    /// <summary>The create command was accepted and a <c>ProjectCreated</c> event was emitted.</summary>
    Created,

    /// <summary>The setup update command was accepted and a <c>ProjectSetupUpdated</c> event was emitted.</summary>
    SetupUpdated,

    /// <summary>The archive command was accepted and a <c>ProjectArchived</c> event was emitted.</summary>
    Archived,

    /// <summary>The set-folder command was accepted and a <c>ProjectFolderSet</c> event was emitted.</summary>
    FolderSet,

    /// <summary>The link-file-reference command was accepted and a <c>FileReferenceLinked</c> event was emitted.</summary>
    FileReferenceLinked,

    /// <summary>The unlink-file-reference command was accepted and a <c>FileReferenceUnlinked</c> event was emitted.</summary>
    FileReferenceUnlinked,

    /// <summary>The link-memory command was accepted and a <c>MemoryLinked</c> event was emitted.</summary>
    MemoryLinked,

    /// <summary>The unlink-memory command was accepted and a <c>MemoryUnlinked</c> event was emitted.</summary>
    MemoryUnlinked,

    /// <summary>The confirm-resolution command was accepted and a <c>ProjectResolutionConfirmed</c> event was emitted.</summary>
    ProjectResolutionConfirmed,

    /// <summary>The command is a logical replay of an already-recorded idempotency key with an equivalent payload.</summary>
    IdempotentReplay,

    /// <summary>The same idempotency key was reused with a non-equivalent payload.</summary>
    IdempotencyConflict,

    /// <summary>A project already exists on this stream; a second create is rejected.</summary>
    DuplicateProject,

    /// <summary>The command requires an existing project but none has been created on this stream.</summary>
    ProjectNotFound,

    /// <summary>The project is already archived and cannot be archived again with a different idempotency key.</summary>
    ProjectAlreadyArchived,

    /// <summary>The project is archived and cannot accept setup updates.</summary>
    ProjectIsArchived,

    /// <summary>A different Project Folder is already set and replacement was not explicitly confirmed.</summary>
    ProjectFolderReplacementRequiresConfirmation,

    /// <summary>The same file reference is already linked with conflicting safe metadata.</summary>
    FileReferenceConflict,

    /// <summary>The project already holds the maximum number of bounded file references.</summary>
    FileReferenceLimitExceeded,

    /// <summary>The same memory reference is already linked with conflicting safe metadata.</summary>
    MemoryReferenceConflict,

    /// <summary>The project already holds the maximum number of bounded memory references.</summary>
    MemoryReferenceLimitExceeded,

    /// <summary>The command failed boundary validation (blank name, unsafe setup metadata, malformed identifier).</summary>
    ValidationFailed,

    /// <summary>The tenant context is missing or unauthorized — the create fails closed.</summary>
    Unauthorized,

    /// <summary>The event/command targets a tenant or project that does not match the canonical stream identity.</summary>
    TenantMismatch,

    /// <summary>An unknown event type reached the aggregate <c>Apply</c> path (a code/contract bug, not user input).</summary>
    StateTransitionInvalid,
}
