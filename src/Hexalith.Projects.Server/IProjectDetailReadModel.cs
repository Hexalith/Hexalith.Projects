// <copyright file="IProjectDetailReadModel.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Projections.ProjectDetail;

/// <summary>
/// Reads the tenant-scoped <c>ProjectDetailProjection</c> for the minimal <c>GetProject</c> query
/// (Story 1.4). The read is tenant-scoped (the authoritative tenant is supplied by the endpoint from
/// authenticated claims, never the caller) and eventually consistent. A null result means
/// absent-or-cross-tenant — the endpoint maps it to a safe-denial 404 so cross-tenant existence is not
/// inferable.
/// </summary>
public interface IProjectDetailReadModel
{
    /// <summary>Gets the projected detail for the authoritative tenant + project, or null when absent/cross-tenant.</summary>
    /// <param name="authoritativeTenantId">The authenticated authoritative tenant.</param>
    /// <param name="projectId">The requested project identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The detail item, or null.</returns>
    Task<ProjectDetailItem?> GetAsync(string authoritativeTenantId, string projectId, CancellationToken cancellationToken = default);
}
