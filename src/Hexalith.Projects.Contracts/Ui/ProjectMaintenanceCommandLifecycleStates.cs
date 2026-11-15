// <copyright file="ProjectMaintenanceCommandLifecycleStates.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

/// <summary>Shared command lifecycle states for command-async maintenance actions.</summary>
public static class ProjectMaintenanceCommandLifecycleStates
{
    public const string Idle = "Idle";
    public const string Submitting = "Submitting";
    public const string Acknowledged = "Acknowledged(202)";
    public const string Syncing = "Syncing";
    public const string Confirmed = "Confirmed";
    public const string Rejected = "Rejected";
}
