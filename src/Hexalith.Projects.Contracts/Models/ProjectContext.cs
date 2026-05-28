// <copyright file="ProjectContext.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// The assembled, authorization-filtered, metadata-only Project Context emitted by
/// <c>ProjectContextInclusionPolicy</c> (AR-9, Story 3.1). It is the read-side output Stories 3.2
/// (Get), 3.3 (Explain), 3.4 (Refresh) and 3.5 (Conversation Start Setup) all consume.
/// </summary>
/// <remarks>
/// <para>
/// The shape is purely metadata — opaque ids, safe display labels, shared-vocabulary states,
/// timestamps, and the closed-vocabulary <see cref="ProjectContextExclusion.Diagnostic"/> strings.
/// It never carries transcripts, file contents, memory bodies, prompts, paths, tokens, secrets, or
/// any other content category from <see cref="PayloadClassification.ForbiddenContent"/>.
/// </para>
/// <para>
/// <see cref="ProjectFolder"/> is intentionally a single optional reference (Story 2.4 invariant —
/// never a list). <see cref="Conversations"/>, <see cref="FileReferences"/>, and
/// <see cref="MemoryReferences"/> remain per-kind disjoint, mirroring the
/// <c>ProjectReferenceIndexProjection</c> per-kind disjoint key contract.
/// <see cref="Excluded"/> aggregates the "what was left out and why" channel across all reference
/// kinds.
/// </para>
/// </remarks>
/// <param name="TenantId">The authoritative managed tenant the project belongs to.</param>
/// <param name="ProjectId">The opaque project identifier.</param>
/// <param name="Lifecycle">The owning project's lifecycle (<see cref="ProjectLifecycle.Active"/> or <see cref="ProjectLifecycle.Archived"/>).</param>
/// <param name="Setup">Optional metadata-only Project Setup carried forward from <see cref="Projections.ProjectDetail.ProjectDetailItem"/>.</param>
/// <param name="ProjectFolder">The optional single Project Folder reference, or <see langword="null"/>.</param>
/// <param name="Conversations">The included conversation references, ordered by (kind, id).</param>
/// <param name="FileReferences">The included file references, ordered by (kind, id).</param>
/// <param name="MemoryReferences">The included memory references, ordered by (kind, id).</param>
/// <param name="Excluded">The excluded candidates with their failed-check and diagnostic, ordered by (kind, id).</param>
/// <param name="AssemblyOutcome">The outer assembly outcome.</param>
/// <param name="ObservedAt">The assembly observation instant (sourced from the policy's typed Now input).</param>
/// <param name="Freshness">The assembly-level freshness derived from the tenant-access projection.</param>
public sealed record ProjectContext(
    string TenantId,
    string ProjectId,
    ProjectLifecycle Lifecycle,
    ProjectSetup? Setup,
    ProjectContextReference? ProjectFolder,
    IReadOnlyList<ProjectContextReference> Conversations,
    IReadOnlyList<ProjectContextReference> FileReferences,
    IReadOnlyList<ProjectContextReference> MemoryReferences,
    IReadOnlyList<ProjectContextExclusion> Excluded,
    ProjectContextAssemblyOutcome AssemblyOutcome,
    DateTimeOffset ObservedAt,
    ProjectContextFreshness Freshness)
{
    /// <summary>
    /// Gets the authoritative managed tenant the project belongs to. Marked
    /// <see cref="JsonIgnoreAttribute"/> so the wire body NEVER carries tenant authority
    /// (Story 3.2 / FS-8 / SM-3 — tenant authority is a server-derived claim, never a wire field).
    /// The policy continues to consume this field internally for inclusion / outcome logic.
    /// </summary>
    [JsonIgnore]
    public string TenantId { get; } = TenantId;

    /// <summary>
    /// Builds the canonical <see cref="ProjectContextAssemblyOutcome.Unauthorized"/> empty Project Context.
    /// </summary>
    /// <param name="requestedTenantId">The requested tenant identifier (echoed metadata-only; never used for trust).</param>
    /// <param name="projectId">The requested project identifier (echoed metadata-only; never used for trust).</param>
    /// <param name="observedAt">The assembly observation instant.</param>
    /// <param name="freshness">The freshness signal at the time of the unauthorized verdict.</param>
    /// <returns>An empty Project Context with <see cref="AssemblyOutcome"/> = <see cref="ProjectContextAssemblyOutcome.Unauthorized"/>.</returns>
    public static ProjectContext Unauthorized(
        string requestedTenantId,
        string projectId,
        DateTimeOffset observedAt,
        ProjectContextFreshness freshness)
        => new(
            requestedTenantId,
            projectId,
            ProjectLifecycle.Active,
            Setup: null,
            ProjectFolder: null,
            Conversations: Array.Empty<ProjectContextReference>(),
            FileReferences: Array.Empty<ProjectContextReference>(),
            MemoryReferences: Array.Empty<ProjectContextReference>(),
            Excluded: Array.Empty<ProjectContextExclusion>(),
            ProjectContextAssemblyOutcome.Unauthorized,
            observedAt,
            freshness);

    /// <summary>
    /// Builds the canonical <see cref="ProjectContextAssemblyOutcome.ProjectUnavailable"/> empty Project Context
    /// (the safe-denial 404 contract surface — never reveals cross-tenant existence).
    /// </summary>
    /// <param name="requestedTenantId">The requested tenant identifier (echoed metadata-only; safe-denial only).</param>
    /// <param name="projectId">The requested project identifier (echoed metadata-only; safe-denial only).</param>
    /// <param name="observedAt">The assembly observation instant.</param>
    /// <param name="freshness">The freshness signal at the time of the safe-denial verdict.</param>
    /// <returns>An empty Project Context with <see cref="AssemblyOutcome"/> = <see cref="ProjectContextAssemblyOutcome.ProjectUnavailable"/>.</returns>
    public static ProjectContext ProjectUnavailable(
        string requestedTenantId,
        string projectId,
        DateTimeOffset observedAt,
        ProjectContextFreshness freshness)
        => new(
            requestedTenantId,
            projectId,
            ProjectLifecycle.Active,
            Setup: null,
            ProjectFolder: null,
            Conversations: Array.Empty<ProjectContextReference>(),
            FileReferences: Array.Empty<ProjectContextReference>(),
            MemoryReferences: Array.Empty<ProjectContextReference>(),
            Excluded: Array.Empty<ProjectContextExclusion>(),
            ProjectContextAssemblyOutcome.ProjectUnavailable,
            observedAt,
            freshness);

    /// <summary>
    /// Builds an empty <see cref="ProjectContextAssemblyOutcome.Assembled"/> Project Context — the
    /// no-references happy-path shape Story 3.2's tests and Story 3.4's freshness probes consume.
    /// </summary>
    /// <param name="tenantId">The authoritative tenant identifier.</param>
    /// <param name="projectId">The opaque project identifier.</param>
    /// <param name="lifecycle">The owning project lifecycle.</param>
    /// <param name="observedAt">The assembly observation instant.</param>
    /// <param name="freshness">The assembly-level freshness signal.</param>
    /// <returns>An empty assembled Project Context (no folder, no references, no exclusions).</returns>
    public static ProjectContext Empty(
        string tenantId,
        string projectId,
        ProjectLifecycle lifecycle,
        DateTimeOffset observedAt,
        ProjectContextFreshness freshness)
        => new(
            tenantId,
            projectId,
            lifecycle,
            Setup: null,
            ProjectFolder: null,
            Conversations: Array.Empty<ProjectContextReference>(),
            FileReferences: Array.Empty<ProjectContextReference>(),
            MemoryReferences: Array.Empty<ProjectContextReference>(),
            Excluded: Array.Empty<ProjectContextExclusion>(),
            ProjectContextAssemblyOutcome.Assembled,
            observedAt,
            freshness);
}
