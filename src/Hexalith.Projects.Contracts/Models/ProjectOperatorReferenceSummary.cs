// <copyright file="ProjectOperatorReferenceSummary.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

/// <summary>Metadata-only reference summary exposed through operator diagnostics.</summary>
/// <param name="ReferenceKind">The reference kind.</param>
/// <param name="ReferenceState">The safe reference state.</param>
/// <param name="ReferenceId">The reference identifier when safe and available.</param>
/// <param name="DisplayName">The bounded safe display name when available.</param>
/// <param name="ReasonCode">The safe reason code when available.</param>
/// <param name="Freshness">The reference freshness evidence.</param>
public sealed record ProjectOperatorReferenceSummary(
    string ReferenceKind,
    string ReferenceState,
    string? ReferenceId,
    string? DisplayName,
    string? ReasonCode,
    ProjectOperatorFreshnessMetadata Freshness);
