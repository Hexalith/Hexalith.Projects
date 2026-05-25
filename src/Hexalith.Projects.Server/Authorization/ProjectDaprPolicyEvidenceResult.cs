// <copyright file="ProjectDaprPolicyEvidenceResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

/// <summary>Metadata-only Dapr deny-by-default policy evidence result.</summary>
public sealed record ProjectDaprPolicyEvidenceResult(
    ProjectDaprPolicyEvidenceStatus Status,
    string OutcomeCode,
    bool Retryable)
{
    /// <summary>Creates allowed policy evidence.</summary>
    public static ProjectDaprPolicyEvidenceResult Allowed()
        => new(ProjectDaprPolicyEvidenceStatus.Allowed, "allowed", Retryable: false);

    /// <summary>Creates denied policy evidence.</summary>
    public static ProjectDaprPolicyEvidenceResult Denied()
        => new(ProjectDaprPolicyEvidenceStatus.Denied, "dapr_policy_denied", Retryable: false);

    /// <summary>Creates unavailable policy evidence.</summary>
    public static ProjectDaprPolicyEvidenceResult Unavailable()
        => new(ProjectDaprPolicyEvidenceStatus.Unavailable, "dapr_policy_unavailable", Retryable: true);
}
