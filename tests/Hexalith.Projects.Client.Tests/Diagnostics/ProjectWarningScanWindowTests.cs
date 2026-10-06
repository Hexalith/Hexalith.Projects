// <copyright file="ProjectWarningScanWindowTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client.Tests.Diagnostics;

using System.Globalization;

using Hexalith.Projects.Client.Diagnostics;
using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Contracts.Identifiers;

using Shouldly;

using Xunit;

/// <summary>
/// Tests ordinal project selection independently of the ambient culture.
/// </summary>
public sealed class ProjectWarningScanWindowTests
{
    /// <summary>
    /// Keeps the ordinal-first project at the window boundary when cultural ordering would replace it.
    /// </summary>
    [Fact]
    public void SelectUsesOrdinalOrderingAcrossTheWindowBoundaryUnderAnotherCulture()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            StringComparer.CurrentCulture.Compare("a-project-026", "Z-project-025").ShouldBeLessThan(0);
            StringComparer.Ordinal.Compare("a-project-026", "Z-project-025").ShouldBeGreaterThan(0);
            string[] expectedProjectIds = Enumerable.Range(1, 24)
                .Select(static index => $"A-project-{index:000}")
                .Append("Z-project-025")
                .ToArray();
            ProjectListItem[] inventory = expectedProjectIds
                .Append("a-project-026")
                .Reverse()
                .Select(static projectId => new ProjectListItem { ProjectId = new ProjectId(projectId).Value })
                .ToArray();

            IReadOnlyList<ProjectListItem> selectedProjects = ProjectWarningScanWindow.Select(inventory);

            selectedProjects.Count.ShouldBe(25);
            selectedProjects.Select(static project => project.ProjectId)
                .ShouldBe(expectedProjectIds, ignoreOrder: false);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUICulture;
        }
    }
}
