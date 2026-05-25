// <copyright file="ProjectProjectionReadiness.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

/// <summary>
/// Metadata-only readiness evidence for a tenant project projection journal.
/// </summary>
/// <param name="TenantId">The tenant id.</param>
/// <param name="Watermark">The highest applied EventStore global position.</param>
/// <param name="ReplayConflict">Whether a replay conflict was observed.</param>
/// <param name="MalformedEvidence">Whether malformed evidence was observed.</param>
public sealed record ProjectProjectionReadiness(
    string TenantId,
    long Watermark,
    bool ReplayConflict,
    bool MalformedEvidence);
