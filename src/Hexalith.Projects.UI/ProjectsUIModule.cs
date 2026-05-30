// <copyright file="ProjectsUIModule.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI;

/// <summary>
/// Assembly anchor for the Hexalith.Projects read-only operational console. Used as the Blazor
/// router app-assembly for the FrontComposer/Fluent UI shell delivered in Story 5.3; later Epic 5
/// stories add the specific inventory, detail, reference, trace, audit, warning, and mutation views.
/// </summary>
public static class ProjectsUIModule
{
    /// <summary>
    /// Gets the module name used in UI diagnostics.
    /// </summary>
    public static string Name => "Hexalith.Projects.UI";
}
