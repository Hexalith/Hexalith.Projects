// <copyright file="MatrixLegacyReference.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Declared retained v1 wire shape used for strict cutover comparison; unknown members are rejected.</summary>
/// <param name="ReferenceKind">The retained reference kind.</param>
/// <param name="ReferenceState">The retained inclusion state.</param>
/// <param name="ReferenceId">The optional opaque owner reference identifier.</param>
/// <param name="DisplayName">The optional authorized owner display name.</param>
/// <param name="ReasonCode">The declared safe reason code.</param>
/// <param name="Freshness">The retained freshness evidence.</param>
internal sealed record MatrixLegacyReference(string ReferenceKind, string ReferenceState, string? ReferenceId, string? DisplayName, string? ReasonCode, MatrixLegacyFreshness Freshness);
#endif
