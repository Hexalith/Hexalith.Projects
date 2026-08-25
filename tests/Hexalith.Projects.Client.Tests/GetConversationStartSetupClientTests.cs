// <copyright file="GetConversationStartSetupClientTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client.Tests;

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Client.Generated;

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
    private static string GeneratedClientPath => Path.Combine(
        LocateRepositoryRoot(),
        "src",
        "Hexalith.Projects.Client",
        "Generated",
        "HexalithProjectsClient.g.cs");

    private static string GeneratedIdempotencyHelpersPath => Path.Combine(
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
        firstDeclaration.ShouldBeGreaterThanOrEqualTo(0, "ConversationStartSetup partial class must be generated.");
        int secondDeclaration = generated.IndexOf("partial class ConversationStartSetup", firstDeclaration + 1, StringComparison.Ordinal);
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

    [Theory]
    [InlineData("HexalithProjectsClient.g.cs")]
    [InlineData("HexalithProjectsIdempotencyHelpers.g.cs")]
    public void GeneratedArtifact_IsLfOnDiskAndNulFree(string fileName)
    {
        // Both artifacts are regenerated together, so both carry the deterministic-regeneration invariant.
        string path = Path.Combine(LocateRepositoryRoot(), "src", "Hexalith.Projects.Client", "Generated", fileName);
        byte[] bytes = File.ReadAllBytes(path);

        bytes.ShouldNotContain((byte)'\r', $"{fileName} must be LF-only.");
        bytes.ShouldNotContain((byte)0, $"{fileName} must contain no NUL bytes.");
    }

    [Fact]
    public async Task GeneratedClient_DeserializesServerShapedConversationStartSetupBody()
    {
        // The source-text assertions above cannot catch naming drift: server-side System.Text.Json
        // output and client-side EnumMember/JsonProperty names were pinned only by two independently
        // written strings. This body is the shape the server tests assert on the wire, including the
        // mixed PascalCase lifecycle/freshness and camelCase policy/source-kind casing.
        const string body = @"{
  ""projectId"": ""01HZ9K8YQ3W6V2N4R7T5P0X1AB"",
  ""lifecycle"": ""Active"",
  ""goals"": [""keep continuity current""],
  ""userInstructions"": [""use safe project references""],
  ""preferredSourceKinds"": [""conversation"", ""memory""],
  ""excludedSourceKinds"": [""fileReference""],
  ""linkedSourcePolicy"": ""authorizedReferences"",
  ""observedAt"": ""2026-08-26T09:30:15+00:00"",
  ""freshness"": ""Fresh""
}";

        using StubHandler handler = new(body);
        using HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost/") };
        Client client = new(httpClient);

        ConversationStartSetup result = await client
            .GetConversationStartSetupAsync("01HZ9K8YQ3W6V2N4R7T5P0X1AB", null, null, CancellationToken.None)
            .ConfigureAwait(true);

        handler.RequestUri!.AbsolutePath.ShouldBe("/api/v1/projects/01HZ9K8YQ3W6V2N4R7T5P0X1AB/setup/conversation-start");
        result.ProjectId.ShouldBe("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        result.Lifecycle.ShouldBe(ConversationStartSetupLifecycle.Active);
        result.LinkedSourcePolicy.ShouldBe(LinkedSourcePolicy.AuthorizedReferences);
        result.Freshness.ShouldBe(ProjectContextFreshness.Fresh);
        result.ObservedAt.ShouldBe(new DateTimeOffset(2026, 8, 26, 9, 30, 15, TimeSpan.Zero));
    }

    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
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
