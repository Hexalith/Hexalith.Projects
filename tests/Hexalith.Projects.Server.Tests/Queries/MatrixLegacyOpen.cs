// <copyright file="MatrixLegacyOpen.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using Hexalith.Projects.Contracts.Models;

/// <summary>Declared retained v1 wire shape used for strict cutover comparison; unknown members are rejected.</summary>
/// <param name="ProjectId">The exact opaque Project identifier.</param>
/// <param name="Name">The authorized audited descriptive Project name.</param>
/// <param name="Description">The optional authorized descriptive Project text.</param>
/// <param name="LifecycleState">The original retained lifecycle spelling.</param>
/// <param name="CreatedAt">The exact original creation timestamp.</param>
/// <param name="UpdatedAt">The exact original last-update timestamp.</param>
/// <param name="SetupMetadata">The optional retained Setup metadata.</param>
/// <param name="ProjectSetup">The original typed Setup, when materialized.</param>
/// <param name="ContextActivation">The retained activation observation, independent of Conversation authority.</param>
/// <param name="References">The ordered authorized retained references.</param>
/// <param name="Freshness">The retained freshness evidence.</param>
internal sealed record MatrixLegacyOpen(string ProjectId, string Name, string? Description, string LifecycleState, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    string? SetupMetadata, ProjectSetup? ProjectSetup, MatrixLegacyActivation ContextActivation, IReadOnlyList<MatrixLegacyReference> References, MatrixLegacyFreshness Freshness);
#endif
