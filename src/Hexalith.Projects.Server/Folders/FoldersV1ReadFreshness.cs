// <copyright file="FoldersV1ReadFreshness.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using System.Text.Json.Serialization;

/// <summary>Exact retained Folders v1 read shape; this type never describes the non-routed v2 candidate.</summary>
/// <param name="ReadConsistency">The consistency mode actually served by the owner.</param>
/// <param name="ObservedAt">The exact owner evidence observation timestamp.</param>
/// <param name="ProjectionWatermark">The owner projection watermark, when available.</param>
/// <param name="Stale">Whether the owner marks its evidence stale.</param>
internal sealed record FoldersV1ReadFreshness([property: JsonRequired] string ReadConsistency, [property: JsonRequired] DateTimeOffset ObservedAt,
    string? ProjectionWatermark, [property: JsonRequired] bool Stale);
