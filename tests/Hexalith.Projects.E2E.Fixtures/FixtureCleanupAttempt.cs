// <copyright file="FixtureCleanupAttempt.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

/// <summary>Reports one metadata-only sibling cleanup attempt.</summary>
/// <param name="Role">The sibling role that was attempted.</param>
/// <param name="StatusCode">The HTTP status, or <see langword="null"/> when no response was received.</param>
public sealed record FixtureCleanupAttempt(
    string Role,
    int? StatusCode)
{
    /// <summary>Gets a value indicating whether the role reached an idempotent successful outcome.</summary>
    public bool Succeeded => StatusCode is >= 200 and < 300 or 404;
}
