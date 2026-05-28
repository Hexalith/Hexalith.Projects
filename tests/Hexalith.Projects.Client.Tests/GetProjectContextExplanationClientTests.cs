// <copyright file="GetProjectContextExplanationClientTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client.Tests;

using System.IO;

using Shouldly;

using Xunit;

/// <summary>
/// Story 3.3 Tier-1 inspection tests for the regenerated NSwag client. Confirms the
/// <c>GetProjectContextExplanationAsync</c> typed method, the new
/// <c>ProjectContextExplanation</c> wrapper, and the per-candidate
/// <c>ProjectContextEvaluation</c> shape are emitted from the OpenAPI spine. Carries forward the
/// LF / NUL-free invariants Story 3.2 established.
/// </summary>
public sealed class GetProjectContextExplanationClientTests
{
    private static readonly string GeneratedClientPath = Path.Combine(
        LocateRepositoryRoot(),
        "src",
        "Hexalith.Projects.Client",
        "Generated",
        "HexalithProjectsClient.g.cs");

    [Fact]
    public void GeneratedClient_ExposesTypedGetProjectContextExplanationAsync()
    {
        string generated = File.ReadAllText(GeneratedClientPath);

        generated.ShouldContain("GetProjectContextExplanationAsync(string projectId");
        generated.ShouldContain("Task<ProjectContextExplanation> GetProjectContextExplanationAsync");
        generated.ShouldContain("class ProjectContextExplanation");
        generated.ShouldContain("class ProjectContextEvaluation");
        // The Story 3.2 types are reused unchanged (NSwag de-dupes by name).
        generated.ShouldContain("class ProjectContext");
        generated.ShouldContain("class ProjectContextReference");
        generated.ShouldContain("class ProjectContextExclusion");
        generated.ShouldContain("enum ProjectContextInclusionCheck");
    }

    [Fact]
    public void GeneratedClient_ProjectContextExplanationHasEvaluationsArrayAndNoTenantId()
    {
        string generated = File.ReadAllText(GeneratedClientPath);
        int wrapperStart = generated.IndexOf("public partial class ProjectContextExplanation", System.StringComparison.Ordinal);
        wrapperStart.ShouldBeGreaterThan(0);
        int wrapperEnd = generated.IndexOf("public partial class ", wrapperStart + 1, System.StringComparison.Ordinal);
        string segment = wrapperEnd > wrapperStart
            ? generated[wrapperStart..wrapperEnd]
            : generated[wrapperStart..];

        segment.ShouldContain("Evaluations");
        segment.ShouldContain("ProjectContextEvaluation");
        // FS-8 / SM-3 carryforward: tenant authority must not surface on the new wrapper either.
        segment.ShouldNotContain("TenantId", Case.Sensitive);
        segment.ShouldNotContain("tenantId", Case.Sensitive);
    }

    [Fact]
    public void GeneratedClient_IsLfOnDiskAndNulFree()
    {
        byte[] bytes = File.ReadAllBytes(GeneratedClientPath);

        bytes.ShouldNotContain((byte)'\r', "generated client must be LF-only.");
        bytes.ShouldNotContain((byte)0, "generated client must contain no NUL bytes.");
    }

    private static string LocateRepositoryRoot()
    {
        string current = System.AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "global.json")))
            {
                return current;
            }

            string? parent = Directory.GetParent(current)?.FullName;
            if (parent == current)
            {
                break;
            }

            current = parent ?? string.Empty;
        }

        throw new FileNotFoundException("global.json not found while locating repository root.");
    }
}
