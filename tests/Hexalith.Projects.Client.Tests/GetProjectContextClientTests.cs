// <copyright file="GetProjectContextClientTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client.Tests;

using System.IO;
using System.Linq;

using Hexalith.Projects.Client.Generated;

using Shouldly;

using Xunit;

/// <summary>
/// Story 3.2 Tier-1 inspection tests for the generated NSwag client surface. Confirms the
/// <c>GetProjectContextAsync</c> typed method, the <c>ProjectContext</c> wire DTO, the
/// <c>ProjectContextReference</c> / <c>ProjectContextExclusion</c> shapes, and the assembly /
/// freshness / inclusion-check enums are emitted from the OpenAPI spine without surfacing tenant
/// authority (FS-8 / SM-3) and without embedding non-LF or NUL bytes.
/// </summary>
public sealed class GetProjectContextClientTests
{
    private static readonly string GeneratedClientPath = Path.Combine(
        LocateRepositoryRoot(),
        "src",
        "Hexalith.Projects.Client",
        "Generated",
        "HexalithProjectsClient.g.cs");

    [Fact]
    public void GeneratedClient_ExposesTypedGetProjectContextAsync()
    {
        string generated = File.ReadAllText(GeneratedClientPath);

        generated.ShouldContain("GetProjectContextAsync(string projectId");
        generated.ShouldContain("Task<ProjectContext> GetProjectContextAsync");
        generated.ShouldContain("class ProjectContext");
        generated.ShouldContain("class ProjectContextReference");
        generated.ShouldContain("class ProjectContextExclusion");
        generated.ShouldContain("enum ProjectContextAssemblyOutcome");
        generated.ShouldContain("enum ProjectContextFreshness");
        generated.ShouldContain("enum ProjectContextInclusionCheck");
    }

    [Fact]
    public void GeneratedClient_ProjectContextHasNoTenantAuthorityField()
    {
        // Per FS-8 / SM-3 the wire shape MUST NOT echo the authoritative tenant id back to the
        // caller. The C# DTO marks ProjectContext.TenantId [JsonIgnore]; the OpenAPI schema does not
        // declare it; the generated client must not expose a TenantId property.
        string generated = File.ReadAllText(GeneratedClientPath);
        int contextStart = generated.IndexOf("public partial class ProjectContext", System.StringComparison.Ordinal);
        contextStart.ShouldBeGreaterThan(0);
        int contextEnd = generated.IndexOf("public partial class ", contextStart + 1, System.StringComparison.Ordinal);
        string segment = contextEnd > contextStart
            ? generated[contextStart..contextEnd]
            : generated[contextStart..];

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
