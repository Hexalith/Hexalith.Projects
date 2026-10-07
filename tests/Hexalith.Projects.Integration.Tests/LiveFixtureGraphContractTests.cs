// <copyright file="LiveFixtureGraphContractTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;
using System.Text.RegularExpressions;

using Hexalith.Projects.E2E.Fixtures;

using Shouldly;

using Xunit;

/// <summary>Verifies the metadata-only, symmetric live-fixture graph contract shared with the Playwright runner.</summary>
public sealed partial class LiveFixtureGraphContractTests
{
    /// <summary>Verifies the C# wire members match the runner's TypeScript graph field list exactly and in order.</summary>
    [Fact]
    public void WireMembersShouldMatchTheRunnerGraphFieldsExactly()
    {
        string client = File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "tests",
            "e2e",
            "support",
            "helpers",
            "live-fixtures-api-client.ts"));
        Match fieldList = GraphFieldListRegex().Match(client);
        fieldList.Success.ShouldBeTrue("The runner must declare LIVE_FIXTURE_GRAPH_FIELDS.");
        string[] runnerFields = [.. QuotedFieldRegex().Matches(fieldList.Groups["fields"].Value).Select(static match => match.Groups["name"].Value)];

        using JsonDocument wire = JsonDocument.Parse(JsonSerializer.Serialize(ValidGraph(), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        string[] wireFields = [.. wire.RootElement.EnumerateObject().Select(static property => property.Name)];

        wireFields.ShouldBe(runnerFields);
    }

    /// <summary>Verifies a graph round-trips through the wire unchanged, so request and response are symmetric.</summary>
    [Fact]
    public void GraphShouldRoundTripUnchanged()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        LiveFixtureGraph graph = ValidGraph();

        LiveFixtureGraph? roundTripped = JsonSerializer.Deserialize<LiveFixtureGraph>(JsonSerializer.Serialize(graph, options), options);

        roundTripped.ShouldBe(graph);
        graph.IsValid().ShouldBeTrue();
    }

    /// <summary>Verifies the ingress rejects unbounded, control-character, escaping, or negative metadata.</summary>
    /// <param name="mutation">The invalid mutation to apply.</param>
    [Theory]
    [InlineData("long-folder")]
    [InlineData("blank-tenant")]
    [InlineData("control-character")]
    [InlineData("absolute-path")]
    [InlineData("parent-path")]
    [InlineData("backslash-path")]
    [InlineData("negative-retry")]
    [InlineData("null-scenario")]
    public void IsValidShouldRejectInvalidMetadata(string mutation)
    {
        LiveFixtureGraph graph = ValidGraph();
        LiveFixtureGraph invalid = mutation switch
        {
            "long-folder" => graph with { FolderId = new string('f', LiveFixtureGraph.MaxValueLength + 1) },
            "blank-tenant" => graph with { TenantId = " " },
            "control-character" => graph with { Scenario = "scenario\nsecond-line" },
            "absolute-path" => graph with { FilePath = "/etc/contract.pdf" },
            "parent-path" => graph with { FilePath = "docs/../contract.pdf" },
            "backslash-path" => graph with { FilePath = "docs\\contract.pdf" },
            "negative-retry" => graph with { Retry = -1 },
            "null-scenario" => graph with { Scenario = null! },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        invalid.IsValid().ShouldBeFalse();
        _ = Should.Throw<ArgumentException>(() => new LiveFixtureState().TryAdd(invalid));
    }

    /// <summary>Verifies state is idempotent per graph, rejects conflicting reuse, and isolates graphs.</summary>
    [Fact]
    public void StateShouldBeIdempotentRejectConflictingReuseAndIsolateGraphs()
    {
        LiveFixtureState state = new();
        LiveFixtureGraph graph = ValidGraph();
        LiveFixtureGraph sibling = graph with { GraphId = "graph-sibling", WorkerIndex = 1, ProjectId = "project-sibling" };

        state.TryAdd(graph).ShouldBeTrue();
        state.TryAdd(graph with { }).ShouldBeTrue();
        state.TryAdd(graph with { ProjectId = "project-other" }).ShouldBeFalse();
        state.TryAdd(sibling).ShouldBeTrue();

        state.Remove(graph.GraphId).ShouldBeTrue();
        state.Remove(graph.GraphId).ShouldBeFalse();
        state.Graphs.ShouldHaveSingleItem().ShouldBe(sibling);
    }

    /// <summary>Creates a valid, metadata-only graph.</summary>
    /// <returns>The graph.</returns>
    internal static LiveFixtureGraph ValidGraph()
        => new(
            GraphId: "graph-1",
            RunId: "run-contract-full",
            WorkerIndex: 0,
            Retry: 0,
            RepeatEachIndex: 0,
            Scenario: "specs/contract.spec.ts:scenario",
            TenantId: "tenant-a",
            PrincipalId: "principal-1",
            ProjectId: "project-1",
            SecondaryProjectId: "project-secondary-1",
            ProposalProjectId: "project-proposal-1",
            ProposalRetryProjectId: "project-proposal-retry-1",
            DegradedProjectId: "project-degraded-1",
            ConversationId: "conversation-1",
            AmbiguousConversationId: "conversation-ambiguous-1",
            ExistingConversationId: "conversation-existing-1",
            StaleConversationId: "conversation-stale-1",
            ForbiddenConversationId: "conversation-forbidden-1",
            UnavailableConversationId: "conversation-unavailable-1",
            FolderId: "folder-1",
            SecondaryFolderId: "folder-secondary-1",
            ProposalFolderId: "folder-proposal-1",
            WorkspaceId: "workspace-1",
            FileReferenceId: "file-1",
            SecondaryFileReferenceId: "file-secondary-1",
            ProposalFileReferenceId: "file-proposal-1",
            DeniedFileReferenceId: "file-denied-1",
            FilePath: "docs/contract.pdf",
            MemoryReferenceId: "memory-1",
            CorrelationId: "correlation-1",
            TaskId: "task-1",
            IdempotencyKey: "idempotency-1");

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [GeneratedRegex(@"LIVE_FIXTURE_GRAPH_FIELDS\s*=\s*\[(?<fields>[^\]]*)\]", RegexOptions.CultureInvariant)]
    private static partial Regex GraphFieldListRegex();

    [GeneratedRegex(@"'(?<name>[A-Za-z]+)'", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedFieldRegex();
}
