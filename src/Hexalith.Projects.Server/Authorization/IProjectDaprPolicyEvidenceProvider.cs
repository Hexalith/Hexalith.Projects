// <copyright file="IProjectDaprPolicyEvidenceProvider.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

/// <summary>Provides Dapr deny-by-default policy evidence for Projects host authorization.</summary>
public interface IProjectDaprPolicyEvidenceProvider
{
    /// <summary>Gets policy evidence for the operation.</summary>
    Task<ProjectDaprPolicyEvidenceResult> GetEvidenceAsync(
        string actionToken,
        string? correlationId,
        string? taskId,
        CancellationToken cancellationToken = default);
}
