// <copyright file="DenyAllProjectDaprPolicyEvidenceProvider.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

/// <summary>Fail-closed default Dapr policy evidence provider for unconfigured hosts.</summary>
public sealed class DenyAllProjectDaprPolicyEvidenceProvider : IProjectDaprPolicyEvidenceProvider
{
    /// <inheritdoc/>
    public Task<ProjectDaprPolicyEvidenceResult> GetEvidenceAsync(
        string actionToken,
        string? correlationId,
        string? taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionToken);
        return Task.FromResult(ProjectDaprPolicyEvidenceResult.Denied());
    }
}
