// <copyright file="ProjectCreationWorkflowStatus.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

/// <summary>Public outcome status of the Story 7.1 Folder-first creation workflow.</summary>
public enum ProjectCreationWorkflowStatus
{
    /// <summary>Project is Active, bound to one authorized Folder, and confirmed by the supported read model.</summary>
    Succeeded,

    /// <summary>Task is admitted and in progress (paused, running, or awaiting projection confirmation).</summary>
    InProgress,

    /// <summary>Task is waiting for a recoverable dependency failure and will resume via authoritative lookup.</summary>
    WaitingForDependency,

    /// <summary>Task is fenced pending reconciliation with receipts and any Folders-owned resource retained.</summary>
    NeedsAttention,

    /// <summary>Task reached terminal rejection/cancellation.</summary>
    Rejected,

    /// <summary>Request failed 7.1-P1 validation before task admission (<c>400 ValidationFailure</c>).</summary>
    ValidationFailure,

    /// <summary>Caller or resource access was denied (<c>404</c>/<c>403</c>).</summary>
    Denied,

    /// <summary>Supplied Folder requires FR-25 Preview/Selection Evidence (<c>RequestPreview</c>).</summary>
    RequestPreview,

    /// <summary>Idempotency conflict (changed request for same key) or irreversible cancellation conflict.</summary>
    Conflict,

    /// <summary>Live authority source is unavailable (<c>503 Unavailable</c>, distinct from <c>404 Denied</c>).</summary>
    Unavailable,
}
