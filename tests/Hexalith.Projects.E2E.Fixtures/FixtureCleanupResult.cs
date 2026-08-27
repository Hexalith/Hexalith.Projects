// <copyright file="FixtureCleanupResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

/// <summary>Reports the observable reverse-order cleanup attempts.</summary>
/// <param name="Attempts">Metadata-only role and HTTP status results in attempt order.</param>
public sealed record FixtureCleanupResult(
    IReadOnlyList<FixtureCleanupAttempt> Attempts)
{
    /// <summary>Gets a value indicating whether every role reached an idempotent successful outcome.</summary>
    public bool Succeeded => Attempts.All(static attempt => attempt.Succeeded);
}
