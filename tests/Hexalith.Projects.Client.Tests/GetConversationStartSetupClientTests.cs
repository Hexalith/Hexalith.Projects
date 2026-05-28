// <copyright file="GetConversationStartSetupClientTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client.Tests;

using System;
using System.IO;

using Shouldly;

using Xunit;

/// <summary>
/// Story 3.5 inspection tests over the regenerated NSwag client surface for
/// <c>GET /api/v1/projects/{projectId}/setup/conversation-start</c>. Confirms the typed
/// <see cref="Generated.ConversationStartSetup"/> method, the
/// <see cref="Generated.ConversationStartSetup"/> wire DTO partial, the absence of an idempotency-
/// helper entry for the new query (queries have no idempotency surface), and the LF/NUL-free disk
/// layout (deterministic regeneration invariant).
/// </summary>
public sealed class GetConversationStartSetupClientTests
{
    private static readonly string GeneratedClientPath = Path.Combine(
        LocateRepositoryRoot(),
        "src",
        "Hexalith.Projects.Client",
        "Generated",
        "HexalithProjectsClient.g.cs");

    private static readonly string GeneratedIdempotencyHelpersPath = Path.Combine(
        LocateRepositoryRoot(),
        "src",
        "Hexalith.Projects.Client",
        "Generated",
        "HexalithProjectsIdempotencyHelpers.g.cs");

    [Fact]
    public void GeneratedClient_ExposesTypedGetConversationStartSetupAsync()
    {
        string generated = File.ReadAllText(GeneratedClientPath);

        generated.ShouldContain("GetConversationStartSetupAsync(string projectId");
        generated.ShouldContain("Task<ConversationStartSetup> GetConversationStartSetupAsync");
        generated.ShouldContain("class ConversationStartSetup");

        // NSwag deduplicates DTO partial declarations by name; the generated file must declare the
        // ConversationStartSetup partial class exactly once even though several operations may
        // reference the schema.
        int firstDeclaration = generated.IndexOf("partial class ConversationStartSetup", StringComparison.Ordinal);
        int secondDeclaration = generated.IndexOf("partial class ConversationStartSetup", firstDeclaration + 1, StringComparison.Ordinal);
        firstDeclaration.ShouldBeGreaterThan(0, "ConversationStartSetup partial class must be generated.");
        secondDeclaration.ShouldBeLessThan(0, "ConversationStartSetup partial class must be declared exactly once.");
    }

    [Fact]
    public void GeneratedClient_GetConversationStartSetupOperation_HasNoIdempotencyHelper()
    {
        // Queries have no idempotency surface — Story 3.5 is read-only (mirrors Story 3.2 / 3.3 /
        // 3.4). The idempotency-helpers file must not gain any entry for GetConversationStartSetup.
        string helpers = File.ReadAllText(GeneratedIdempotencyHelpersPath);

        helpers.ShouldNotContain("GetConversationStartSetup", Case.Sensitive);
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
        string current = AppContext.BaseDirectory;
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
