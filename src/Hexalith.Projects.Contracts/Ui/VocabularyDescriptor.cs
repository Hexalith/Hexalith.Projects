// <copyright file="VocabularyDescriptor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Presentation metadata for a single shared-vocabulary enum member (UX-DR5): a stable wire
/// <see cref="Code"/>, a human <see cref="DisplayLabel"/>, an <see cref="AccessibleName"/> for assistive
/// technologies, and a <see cref="Severity"/> badge slot.
/// </summary>
/// <param name="Code">The stable wire code — the enum member name (never the integer ordinal).</param>
/// <param name="DisplayLabel">The human-readable display label.</param>
/// <param name="AccessibleName">The accessible name announced by assistive technology.</param>
/// <param name="Severity">The badge severity slot mapped from the member's <see cref="ProjectionBadgeAttribute"/>.</param>
public sealed record VocabularyDescriptor(
    string Code,
    string DisplayLabel,
    string AccessibleName,
    BadgeSlot Severity);
