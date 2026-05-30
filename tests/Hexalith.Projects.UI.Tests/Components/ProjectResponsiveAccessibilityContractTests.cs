// <copyright file="ProjectResponsiveAccessibilityContractTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Components;

using Shouldly;

using Xunit;

/// <summary>
/// Story 5.11 static contract checks for responsive and accessibility hardening.
/// </summary>
public sealed class ProjectResponsiveAccessibilityContractTests
{
    [Fact]
    public void Epic5_Css_Declares_Required_Responsive_Bands_And_Focus_Contracts()
    {
        string css = string.Join(
            Environment.NewLine,
            ReadRepositoryFile("src/Hexalith.Projects.UI/Components/Pages/Home.razor.css"),
            ReadRepositoryFile("src/Hexalith.Projects.UI/Components/Pages/ProjectDiagnostics.razor.css"),
            ReadRepositoryFile("src/Hexalith.Projects.UI/Components/Shared/ProjectDiagnosticHeader.razor.css"),
            ReadRepositoryFile("src/Hexalith.Projects.UI/Components/Shared/ProjectResolutionTraceWorkbench.razor.css"));

        css.ShouldContain("@media (max-width: 1023px)");
        css.ShouldContain("@media (max-width: 767px)");
        css.ShouldContain("@media (min-width: 1440px)");
        css.ShouldContain(":focus-visible");
        css.ShouldContain("overflow-wrap: anywhere");
        css.ShouldNotContain("text-overflow: ellipsis");
    }

    [Fact]
    public void Parity_Matrix_Records_Story511_Web_Mcp_Cli_Quality_Gates()
    {
        string parityMatrix = ReadRepositoryFile("docs/parity-matrix.md");

        parityMatrix.ShouldContain("Story 5.11 Final Cross-Surface Quality Contract");
        parityMatrix.ShouldContain("project-reference-health-matrix");
        parityMatrix.ShouldContain("projects.operationalDashboard");
        parityMatrix.ShouldContain("projects dashboard");
        parityMatrix.ShouldContain("320-767");
        parityMatrix.ShouldContain("768-1023");
        parityMatrix.ShouldContain("1440+");
        parityMatrix.ShouldContain("diagnosticUnavailable");
        parityMatrix.ShouldContain("raw ProblemDetails");
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.Projects.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return File.ReadAllText(Path.Combine(directory!.FullName, relativePath));
    }
}
