// <copyright file="ConversationStartSetup.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// The bounded subset of <see cref="ProjectSetup"/> Hexalith.Chatbot retrieves to start or resume a
/// conversation without re-querying every bounded context first (FR-20, Story 3.5). Realizes AR-8's
/// <c>ConversationStartSetupProjection</c> as the wire shape returned by
/// <c>GET /api/v1/projects/{projectId}/setup/conversation-start</c>.
/// </summary>
/// <remarks>
/// <para>
/// The wire shape is purely metadata — bounded text fields, closed-vocabulary policy/lifecycle/freshness
/// enums, and a typed observation instant. It never carries transcripts, file contents, memory bodies,
/// prompts, paths, tokens, secrets, tenant authority, audit metadata, or any per-reference inventory.
/// Consumers that need per-reference inventories or exclusion diagnostics call
/// <c>GetProjectContext</c> / <c>ExplainContextSelection</c> / <c>RefreshProjectContext</c>.
/// </para>
/// <para>
/// Field provenance: <see cref="ProjectId"/> echoes the request path parameter;
/// <see cref="Lifecycle"/> / <see cref="ObservedAt"/> / <see cref="Freshness"/> come from the policy-
/// assembled <see cref="ProjectContext"/>; <see cref="Goals"/> / <see cref="UserInstructions"/> /
/// <see cref="PreferredSourceKinds"/> / <see cref="ExcludedSourceKinds"/> come from
/// <c>ProjectContext.Setup</c> when present (empty arrays when null);
/// <see cref="LinkedSourcePolicy"/> comes from
/// <c>ProjectContext.Setup.ConversationStartDefaults.LinkedSourcePolicy</c> when present
/// (<see cref="Models.LinkedSourcePolicy.None"/> as the closed default-of-default).
/// </para>
/// <para>
/// Wire-shape invariants:
/// (i) NO <c>TenantId</c> field on the body (FS-8 / SM-3 — tenant authority is a server-derived claim,
/// never a wire field; cleaner than the <c>[JsonIgnore]</c>-on-required-field pattern Story 3.2 used
/// for <see cref="ProjectContext.TenantId"/>);
/// (ii) NO internal audit metadata (no <c>Sequence</c> / <c>CreatedAt</c> / <c>UpdatedAt</c> /
/// <c>SetupMetadata</c> — those belong on <c>ProjectDetailItem</c> for audit/operator surfaces, not on
/// the FR-20 fast-path response);
/// (iii) NO per-reference inventory (no <c>ProjectFolder</c> / <c>FileReferences</c> /
/// <c>MemoryReferences</c> / <c>Conversations</c> / <c>Excluded</c> / <c>AssemblyOutcome</c> — FR-20
/// is explicit "without re-querying every bounded context first").
/// </para>
/// </remarks>
/// <param name="ProjectId">The opaque project identifier (metadata-only echo of the request path).</param>
/// <param name="Lifecycle">The owning project's lifecycle.</param>
/// <param name="Goals">Bounded safe project goals (empty when setup is absent).</param>
/// <param name="UserInstructions">Bounded user-facing instructions (empty when setup is absent).</param>
/// <param name="PreferredSourceKinds">Preferred source kinds for future context selection (empty when setup is absent).</param>
/// <param name="ExcludedSourceKinds">Excluded source kinds for future context selection (empty when setup is absent).</param>
/// <param name="LinkedSourcePolicy">The closed v1 linked-source policy (defaults to <see cref="Models.LinkedSourcePolicy.None"/> when conversation-start defaults are absent).</param>
/// <param name="ObservedAt">The assembly observation instant (sourced from the policy's typed Now input).</param>
/// <param name="Freshness">The assembly-level freshness derived from the tenant-access projection.</param>
public sealed record ConversationStartSetup(
    string ProjectId,
    ProjectLifecycle Lifecycle,
    IReadOnlyList<string> Goals,
    IReadOnlyList<string> UserInstructions,
    IReadOnlyList<ProjectContextSourceKind> PreferredSourceKinds,
    IReadOnlyList<ProjectContextSourceKind> ExcludedSourceKinds,
    LinkedSourcePolicy LinkedSourcePolicy,
    DateTimeOffset ObservedAt,
    ProjectContextFreshness Freshness)
{
    /// <summary>
    /// Builds the canonical empty-setup record for a project with no <c>UpdateProjectSetup</c> yet —
    /// matches the <see cref="ProjectSetup.Empty"/> semantic (empty arrays + <see cref="Models.LinkedSourcePolicy.None"/>).
    /// </summary>
    /// <param name="projectId">The opaque project identifier.</param>
    /// <param name="lifecycle">The owning project's lifecycle.</param>
    /// <param name="observedAt">The assembly observation instant.</param>
    /// <param name="freshness">The assembly-level freshness signal.</param>
    /// <returns>An empty <see cref="ConversationStartSetup"/> with no goals, instructions, or source-kind preferences.</returns>
    public static ConversationStartSetup Empty(
        string projectId,
        ProjectLifecycle lifecycle,
        DateTimeOffset observedAt,
        ProjectContextFreshness freshness)
        => new(
            projectId,
            lifecycle,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<ProjectContextSourceKind>(),
            Array.Empty<ProjectContextSourceKind>(),
            LinkedSourcePolicy.None,
            observedAt,
            freshness);

    /// <summary>
    /// Projects a policy-assembled <see cref="ProjectContext"/> onto the bounded conversation-start subset
    /// (the canonical mapper the Story 3.5 projector consumes).
    /// </summary>
    /// <param name="context">The policy-assembled context (single source of truth — never raw <c>ProjectDetailItem.Setup</c>).</param>
    /// <returns>The bounded conversation-start subset of the input context.</returns>
    public static ConversationStartSetup FromContext(ProjectContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ProjectSetup? setup = context.Setup;
        LinkedSourcePolicy linkedSource = setup?.ConversationStartDefaults?.LinkedSourcePolicy ?? LinkedSourcePolicy.None;
        return new ConversationStartSetup(
            ProjectId: context.ProjectId,
            Lifecycle: context.Lifecycle,
            Goals: setup?.Goals ?? Array.Empty<string>(),
            UserInstructions: setup?.UserInstructions ?? Array.Empty<string>(),
            PreferredSourceKinds: setup?.PreferredSourceKinds ?? Array.Empty<ProjectContextSourceKind>(),
            ExcludedSourceKinds: setup?.ExcludedSourceKinds ?? Array.Empty<ProjectContextSourceKind>(),
            LinkedSourcePolicy: linkedSource,
            ObservedAt: context.ObservedAt,
            Freshness: context.Freshness);
    }
}
