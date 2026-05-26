// <copyright file="ProjectReferenceIndexItem.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectReferenceIndex;

using System;

using Hexalith.Projects.Contracts.Ui;

/// <summary>Metadata-only index row for a Project sibling reference.</summary>
/// <param name="TenantId">The managed tenant identifier.</param>
/// <param name="ProjectId">The project identifier.</param>
/// <param name="ReferenceKind">The sibling reference kind.</param>
/// <param name="ReferenceId">The sibling reference identifier, or null while pending.</param>
/// <param name="ReferenceState">The shared reference state.</param>
/// <param name="DisplayName">Safe display metadata, or null.</param>
/// <param name="ReasonCode">Optional stable metadata-only reason code.</param>
/// <param name="UpdatedAt">The event-carried update time.</param>
/// <param name="Sequence">The projection sequence watermark.</param>
public sealed record ProjectReferenceIndexItem(
    string TenantId,
    string ProjectId,
    string ReferenceKind,
    string? ReferenceId,
    ReferenceState ReferenceState,
    string? DisplayName,
    string? ReasonCode,
    DateTimeOffset UpdatedAt,
    long Sequence);
