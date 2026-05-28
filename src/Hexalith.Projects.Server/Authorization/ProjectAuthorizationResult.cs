// <copyright file="ProjectAuthorizationResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;

/// <summary>Host-side layered authorization result for Projects endpoints.</summary>
/// <remarks>
/// <para>
/// Story 3.2 additively extended this record with <see cref="TenantAccessResult"/> so the
/// GetProjectContext handler can hand the Story 1.6 tenant-access decision to
/// <c>ProjectContextInclusionPolicy.Assemble(...)</c> without re-running tenant authorization. The
/// property is optional and defaults to <see langword="null"/> on every existing call site —
/// callers that do not need the typed result are unaffected (binary-compatible additive change).
/// </para>
/// </remarks>
public sealed record ProjectAuthorizationResult(
    bool IsAllowed,
    AuthorizationLayer TerminalLayer,
    ReferenceState Reason,
    string Code,
    bool Retryable,
    IReadOnlyList<AuthorizationLayer> EvaluatedLayers,
    ProjectDetailItem? ProjectDetail,
    TenantAccessAuthorizationResult? TenantAccessResult = null)
{
    /// <summary>Creates an allowed result.</summary>
    public static ProjectAuthorizationResult Allowed(
        IReadOnlyList<AuthorizationLayer> evaluatedLayers,
        ProjectDetailItem? projectDetail,
        TenantAccessAuthorizationResult? tenantAccessResult = null)
        => new(
            IsAllowed: true,
            AuthorizationLayer.DaprDenyByDefaultPolicy,
            ReferenceState.Included,
            "allowed",
            Retryable: false,
            evaluatedLayers,
            projectDetail,
            tenantAccessResult);

    /// <summary>Creates a denied result.</summary>
    public static ProjectAuthorizationResult Denied(
        AuthorizationLayer terminalLayer,
        ReferenceState reason,
        string code,
        bool retryable,
        IReadOnlyList<AuthorizationLayer> evaluatedLayers,
        TenantAccessAuthorizationResult? tenantAccessResult = null)
        => new(
            IsAllowed: false,
            terminalLayer,
            reason,
            code,
            retryable,
            evaluatedLayers,
            ProjectDetail: null,
            tenantAccessResult);
}
