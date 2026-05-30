// <copyright file="ProjectOperatorFreshnessMetadata.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

/// <summary>Freshness evidence carried by operator diagnostics.</summary>
/// <param name="ReadConsistency">The read-consistency class.</param>
/// <param name="ObservedAt">The newest projection observation timestamp used for the response.</param>
/// <param name="ProjectionWatermark">The projection watermark when available.</param>
/// <param name="Stale">A value indicating whether the response is stale.</param>
/// <param name="TrustState">The safe trust state.</param>
public sealed record ProjectOperatorFreshnessMetadata(
    string ReadConsistency,
    DateTimeOffset ObservedAt,
    string? ProjectionWatermark,
    bool Stale,
    string TrustState);
