// <copyright file="FixtureCleanupResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

/// <summary>Reports the observable reverse-order cleanup attempts and metadata-only failures.</summary>
/// <param name="AttemptedRoles">The sibling roles in the exact order cleanup was attempted.</param>
/// <param name="Failures">Metadata-only role and HTTP status diagnostics.</param>
public sealed record FixtureCleanupResult(
    IReadOnlyList<string> AttemptedRoles,
    IReadOnlyList<string> Failures);
