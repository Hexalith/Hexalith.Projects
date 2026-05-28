// <copyright file="RefreshProjectContextClientTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client.Tests;

using System.IO;

using Shouldly;

using Xunit;

/// <summary>
/// Story 3.4 Tier-1 inspection tests for the regenerated NSwag client surface. Confirms the new
/// <c>RefreshProjectContextAsync</c> typed method is emitted from the OpenAPI spine, that no
/// idempotency-helper entry is added for the query (queries are idempotency-free), and that the
/// regenerated client stays LF-only and NUL-free.
/// </summary>
public sealed class RefreshProjectContextClientTests
{
    private static readonly string GeneratedClientPath = Path.Combine(
        LocateRepositoryRoot(),
        "src",
        "Hexalith.Projects.Client",
        "Generated",
        "HexalithProjectsClient.g.cs");

    private static readonly string IdempotencyHelperPath = Path.Combine(
        LocateRepositoryRoot(),
        "src",
        "Hexalith.Projects.Client",
        "Generated",
        "HexalithProjectsIdempotencyHelpers.g.cs");

    [Fact]
    public void GeneratedClient_ExposesTypedRefreshProjectContextAsync()
    {
        string generated = File.ReadAllText(GeneratedClientPath);

        generated.ShouldContain("RefreshProjectContextAsync(string projectId");
        generated.ShouldContain("Task<ProjectContext> RefreshProjectContextAsync");
    }

    [Fact]
    public void GeneratedClient_RefreshOperation_HasNoIdempotencyHelper()
    {
        // Queries have no idempotency surface; the regenerated helper must not contain a
        // RefreshProjectContext entry (mirrors Story 3.2 / 3.3 GET-context queries).
        string helper = File.ReadAllText(IdempotencyHelperPath);
        helper.ShouldNotContain("RefreshProjectContext");
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
