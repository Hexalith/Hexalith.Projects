// <copyright file="AllowingProjectDaprPolicyEvidenceProvider.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

/// <summary>Explicit dev/test opt-in Dapr policy evidence provider.</summary>
public sealed class AllowingProjectDaprPolicyEvidenceProvider : IProjectDaprPolicyEvidenceProvider
{
    /// <inheritdoc/>
    public Task<ProjectDaprPolicyEvidenceResult> GetEvidenceAsync(
        string actionToken,
        string? correlationId,
        string? taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionToken);
        return Task.FromResult(ProjectDaprPolicyEvidenceResult.Allowed());
    }
}
