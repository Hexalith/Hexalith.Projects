// <copyright file="MatrixLegacyFreshness.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Declared retained v1 wire shape used for strict cutover comparison; unknown members are rejected.</summary>
/// <param name="ReadConsistency">The owner-served consistency mode.</param>
/// <param name="ObservedAt">The exact persisted source observation timestamp.</param>
/// <param name="ProjectionWatermark">The exact original source global-position watermark.</param>
/// <param name="Stale">Whether the source evidence is stale.</param>
/// <param name="TrustState">The retained projection trust state.</param>
internal sealed record MatrixLegacyFreshness(string ReadConsistency, DateTimeOffset ObservedAt, string? ProjectionWatermark, bool Stale, string TrustState);
#endif
