// <copyright file="FixtureSeedFailure.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

/// <summary>Reports a metadata-only seed failure and the reverse-order compensation it triggered.</summary>
/// <param name="FailedRole">The sibling role whose seed did not succeed.</param>
/// <param name="StatusCode">The HTTP status, or <see langword="null"/> when no response was received.</param>
/// <param name="Compensation">The reverse-order removal attempts for every role that may hold the graph.</param>
public sealed record FixtureSeedFailure(
    string FailedRole,
    int? StatusCode,
    FixtureCleanupResult Compensation);
