// <copyright file="ProjectsMcpStory511ParityTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp.Tests;

using System.Reflection;

using Hexalith.Projects.Mcp;

using Shouldly;

using Xunit;

/// <summary>
/// Story 5.11 cross-surface parity checks for the MCP descriptors and the final parity matrix.
/// </summary>
public sealed class ProjectsMcpStory511ParityTests
{
    [Fact]
    public void Parity_Matrix_Lists_Every_Mcp_Resource_And_Tool()
    {
        string parityMatrix = ReadRepositoryFile("docs/parity-matrix.md");

        foreach (string resource in ProjectsMcpDescriptors.ResourceNames)
        {
            parityMatrix.ShouldContain($"`{resource}`");
        }

        foreach (string action in ProjectsMcpDescriptors.MaintenanceActionNames)
        {
            parityMatrix.ShouldContain($"`projects.{action}`");
        }
    }

    [Fact]
    public void Every_Mcp_Resource_Row_Exposes_Tenant_Explanation_And_Payload_Exclusion()
    {
        foreach (Type projectionType in ProjectsMcpDescriptors.Manifest.Resources.Select(static r => Type.GetType(r.ProjectionTypeName, throwOnError: true)!))
        {
            projectionType.GetProperty("TenantScope", BindingFlags.Instance | BindingFlags.Public).ShouldNotBeNull();
            projectionType.GetProperty("ShortExplanation", BindingFlags.Instance | BindingFlags.Public).ShouldNotBeNull();
            projectionType.GetProperty("PayloadExcluded", BindingFlags.Instance | BindingFlags.Public).ShouldNotBeNull();
        }
    }

    [Fact]
    public void Warning_And_Dashboard_Resources_Expose_Diagnostic_Unavailable_Parity_Field()
    {
        typeof(ProjectsMcpOperationalDashboardItem).GetProperty(nameof(ProjectsMcpOperationalDashboardItem.DiagnosticUnavailable)).ShouldNotBeNull();
        typeof(ProjectsMcpWarningQueueItem).GetProperty(nameof(ProjectsMcpWarningQueueItem.DiagnosticUnavailable)).ShouldNotBeNull();

        string parityMatrix = ReadRepositoryFile("docs/parity-matrix.md");
        parityMatrix.ShouldContain("diagnosticUnavailable");
        parityMatrix.ShouldContain("PayloadExcluded");
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
