// <copyright file="IProjectCreationAuthorityChecker.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.SdkWriter.Admission;

/// <summary>Checks live same-Tenant original-actor authority at admission and before each pre-commit step.</summary>
public interface IProjectCreationAuthorityChecker
{
    /// <summary>Evaluates whether the original actor currently holds create authority in the target Tenant.</summary>
    /// <param name="tenantId">The target Tenant identifier.</param>
    /// <param name="actorPrincipalId">The original actor principal identifier.</param>
    /// <param name="callerClass">The authenticated caller class.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The live authority status (<see cref="ActorAuthorityStatus.Allowed"/>, <see cref="ActorAuthorityStatus.Denied"/>, or <see cref="ActorAuthorityStatus.Unavailable"/>).</returns>
    Task<ActorAuthorityStatus> CheckAsync(
        string tenantId,
        string actorPrincipalId,
        ProjectCreationCallerClass callerClass,
        CancellationToken cancellationToken = default);
}
