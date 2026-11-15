// <copyright file="ProjectMaintenancePanelState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>Shared maintenance panel states for FrontComposer status filtering.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectMaintenancePanelState>))]
public enum ProjectMaintenancePanelState
{
    [ProjectionBadge(BadgeSlot.Info)]
    Preview,

    [ProjectionBadge(BadgeSlot.Warning)]
    DryRunRequired,

    [ProjectionBadge(BadgeSlot.Success)]
    DryRunPassed,

    [ProjectionBadge(BadgeSlot.Danger)]
    DryRunBlocked,

    [ProjectionBadge(BadgeSlot.Warning)]
    ConfirmationRequired,

    [ProjectionBadge(BadgeSlot.Info)]
    Executing,

    [ProjectionBadge(BadgeSlot.Success)]
    Succeeded,

    [ProjectionBadge(BadgeSlot.Danger)]
    Failed,
}
