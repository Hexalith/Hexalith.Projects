// <copyright file="ProjectResolutionEvidenceBuilder.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Testing.Resolution;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;

/// <summary>Reusable deterministic builders for Story 4.1 Project resolution tests.</summary>
public static class ProjectResolutionEvidenceBuilder
{
    /// <summary>The default tenant identifier.</summary>
    public const string DefaultTenant = "acme";

    /// <summary>The default deterministic evaluation instant.</summary>
    public static readonly DateTimeOffset DefaultNow = new(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Builds a deterministic resolution context.</summary>
    /// <param name="authoritativeTenantId">The server-derived tenant authority.</param>
    /// <param name="requestedTenantId">The requested tenant id.</param>
    /// <param name="includeArchived">Whether archived Projects are eligible.</param>
    /// <param name="now">The deterministic evaluation instant.</param>
    /// <returns>A populated resolution context.</returns>
    public static ProjectResolutionContext Context(
        string? authoritativeTenantId = DefaultTenant,
        string? requestedTenantId = DefaultTenant,
        bool includeArchived = false,
        DateTimeOffset? now = null)
        => new(
            authoritativeTenantId,
            requestedTenantId,
            includeArchived,
            now ?? DefaultNow,
            CorrelationId: "corr-001",
            TaskId: "task-001",
            PresentedInputIds: ["conversation-001"]);

    /// <summary>Builds one candidate Project.</summary>
    /// <param name="projectId">The opaque project id.</param>
    /// <param name="lifecycle">The candidate lifecycle.</param>
    /// <param name="signals">Match signals.</param>
    /// <returns>A candidate evidence record.</returns>
    public static ProjectResolutionCandidateEvidence Candidate(
        string projectId = "project-a",
        ProjectLifecycle lifecycle = ProjectLifecycle.Active,
        IReadOnlyList<ProjectResolutionMatchSignal>? signals = null)
        => new(
            projectId,
            DisplayName: $"Project {projectId}",
            lifecycle,
            signals ?? [Signal()]);

    /// <summary>Builds one match signal.</summary>
    /// <param name="reasonCode">The shared reason code.</param>
    /// <param name="state">The reference state.</param>
    /// <param name="referenceId">The opaque reference id.</param>
    /// <param name="kind">The reference kind.</param>
    /// <returns>A match signal.</returns>
    public static ProjectResolutionMatchSignal Signal(
        ProjectReasonCode reasonCode = ProjectReasonCode.ConversationLinked,
        ReferenceState state = ReferenceState.Included,
        string referenceId = "conversation-001",
        string kind = "conversation")
        => new(kind, referenceId, reasonCode, state, DefaultNow);
}
