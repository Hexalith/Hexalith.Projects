// <copyright file="ProjectMaintenancePanelStates.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

/// <summary>Shared maintenance panel states.</summary>
public static class ProjectMaintenancePanelStates
{
    public const string Preview = "Preview";
    public const string DryRunRequired = "DryRunRequired";
    public const string DryRunPassed = "DryRunPassed";
    public const string DryRunBlocked = "DryRunBlocked";
    public const string ConfirmationRequired = "ConfirmationRequired";
    public const string Executing = "Executing";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}
