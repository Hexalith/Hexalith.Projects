// <copyright file="ProjectContextOperationKind.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

using System.Text.Json.Serialization;

/// <summary>
/// The Epic 3 operation the inclusion policy is evaluating for (AR-9, Story 3.1). Drives the
/// read-only / trust-bearing distinction in the (evidence-state × operation) fail-closed decision
/// matrix (AC 6/AC 7): a stale tenant-access projection is allowed for read-only operations and
/// downgrades the assembly <see cref="Hexalith.Projects.Contracts.Ui.ProjectContextFreshness"/> to
/// <see cref="Hexalith.Projects.Contracts.Ui.ProjectContextFreshness.Stale"/>, while a trust-bearing
/// operation collapses the assembly to
/// <see cref="Hexalith.Projects.Contracts.Ui.ProjectContextAssemblyOutcome.Unauthorized"/>.
/// </summary>
/// <remarks>
/// All Epic 3 endpoints are read-only by design (Story 3.2 Get, Story 3.3 Explain, Story 3.4
/// Refresh, Story 3.5 GetConversationStartSetup). The taxonomy still distinguishes them so a
/// future trust-bearing operation can be added without changing the policy shape.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectContextOperationKind>))]
public enum ProjectContextOperationKind
{
    /// <summary>Story 3.2: GetProjectContext (read-only, allows bounded-stale tenant projection).</summary>
    Get,

    /// <summary>Story 3.4: RefreshProjectContext (read-only with explicit freshness signalling).</summary>
    Refresh,

    /// <summary>Story 3.3: ExplainContextSelection (read-only, audit / operator surface).</summary>
    Explain,

    /// <summary>Story 3.5: GetConversationStartSetup (read-only).</summary>
    GetConversationStartSetup,
}
