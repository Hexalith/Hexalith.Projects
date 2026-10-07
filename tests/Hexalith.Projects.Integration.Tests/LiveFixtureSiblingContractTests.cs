// <copyright file="LiveFixtureSiblingContractTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Conversations.Client;
using Hexalith.Folders.Client;
using Hexalith.Memories.Client.Rest;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.E2E.Fixtures;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Memories;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using FoldersClient = Hexalith.Folders.Client.Generated.IClient;

/// <summary>
/// Pins every live fixture role to the sibling client contract the Projects server consumes. Each test
/// runs the real role host on loopback and calls it through the Projects ACL adapter composed with the
/// referenced sibling client package (the published package in CI package mode), so a route, header,
/// or wire-shape drift fails here instead of as a live safe denial.
/// </summary>
public sealed class LiveFixtureSiblingContractTests
{
    private const string CorrelationId = "correlation-sibling-contract-0001";

    /// <summary>Verifies every graph folder is a ready, readable folder through the Folders client.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FoldersRoleShouldAcceptEveryGraphFolderAndSafelyDenyUnknownFolders()
    {
        LiveFixtureGraph graph = Graph();
        LiveFixtureRoleHost host = await LiveFixtureRoleHost
            .StartAsync("folders", graph, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await using ConfiguredAsyncDisposable hostScope = host.ConfigureAwait(true);
        using ServiceProvider provider = Services(services => services.AddFoldersClient(options => options.BaseAddress = host.BaseAddress));
        FoldersProjectFolderDirectory folders = new(provider.GetRequiredService<FoldersClient>());

        foreach (string folderId in new[] { graph.FolderId, graph.SecondaryFolderId, graph.ProposalFolderId })
        {
            ProjectFolderValidationResult accepted = await folders
                .ValidateSetProjectFolderAsync(ProjectId(graph), folderId, CorrelationId, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            accepted.Outcome.ShouldBe(ProjectFolderValidationOutcome.Accepted, folderId);
        }

        ProjectFolderValidationResult unknown = await folders
            .ValidateSetProjectFolderAsync(ProjectId(graph), Id("folder-unknown"), CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        unknown.Outcome.ShouldBe(ProjectFolderValidationOutcome.Denied);
    }

    /// <summary>Verifies only the graph's metadata-only file is visible through the Folders client.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task FoldersRoleShouldExposeOnlyTheGraphFileThroughContextMetadata()
    {
        LiveFixtureGraph graph = Graph();
        LiveFixtureRoleHost host = await LiveFixtureRoleHost
            .StartAsync("folders", graph, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await using ConfiguredAsyncDisposable hostScope = host.ConfigureAwait(true);
        using ServiceProvider provider = Services(services => services.AddFoldersClient(options => options.BaseAddress = host.BaseAddress));
        FoldersProjectFileReferenceDirectory files = new(provider.GetRequiredService<FoldersClient>());

        foreach (string folderId in new[] { graph.FolderId, graph.ProposalFolderId })
        {
            ProjectFileReferenceValidationResult accepted = await files
                .ValidateLinkFileReferenceAsync(
                    ProjectId(graph),
                    folderId,
                    graph.WorkspaceId,
                    graph.FilePath,
                    CorrelationId,
                    graph.TaskId,
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            accepted.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Accepted, folderId);
        }

        ProjectFileReferenceValidationResult hiddenPath = await files
            .ValidateLinkFileReferenceAsync(
                ProjectId(graph),
                graph.FolderId,
                graph.WorkspaceId,
                "secret/redacted-note.md",
                CorrelationId,
                graph.TaskId,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        hiddenPath.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Denied);

        ProjectFileReferenceValidationResult otherWorkspace = await files
            .ValidateLinkFileReferenceAsync(
                ProjectId(graph),
                graph.FolderId,
                Id("workspace-other"),
                graph.FilePath,
                CorrelationId,
                graph.TaskId,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        otherWorkspace.Outcome.ShouldBe(ProjectFileReferenceValidationOutcome.Denied);
    }

    /// <summary>Verifies the Conversations role lists, reads, and accepts assignments through the Conversations client.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ConversationsRoleShouldListReadAndAssignThroughTheConversationsClient()
    {
        LiveFixtureGraph graph = Graph();
        LiveFixtureRoleHost host = await LiveFixtureRoleHost
            .StartAsync("conversations", graph, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await using ConfiguredAsyncDisposable hostScope = host.ConfigureAwait(true);
        using ServiceProvider provider = Services(services =>
            services.AddHexalithConversationsClient(options => options.Endpoint = host.BaseAddress));
        IConversationClient client = provider.GetRequiredService<IConversationClient>();
        ConversationTenantId tenantId = new(graph.TenantId);
        CallerPrincipalId caller = new(graph.PrincipalId);

        ProjectConversationsPage page = await new ConversationsProjectConversationDirectory(client)
            .ListForProjectAsync(ProjectId(graph), tenantId, caller, new PageRequest(25), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        page.TrustSignal.ShouldBe(ProjectConversationTrustSignal.Current);
        page.Items.ShouldHaveSingleItem().ConversationId.Value.ShouldBe(graph.ExistingConversationId);

        ConversationsProjectConversationResolutionDirectory resolution = new(client);
        ConversationResolutionMetadata existing = await resolution
            .ReadConversationMetadataAsync(new ConversationId(graph.ExistingConversationId), tenantId, caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        existing.LinkedProjectId.ShouldBe(graph.ProjectId);
        existing.ReferenceState.ShouldBe(ReferenceState.Included);

        ConversationResolutionMetadata unlinked = await resolution
            .ReadConversationMetadataAsync(new ConversationId(graph.ConversationId), tenantId, caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        unlinked.LinkedProjectId.ShouldBeNull();
        unlinked.SafeLabel.ShouldBe($"Fixture conversation {graph.Scenario}");
        unlinked.ReferenceState.ShouldBe(ReferenceState.Included);

        ConversationResolutionMetadata unknown = await resolution
            .ReadConversationMetadataAsync(new ConversationId(Id("conversation-unknown")), tenantId, caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        unknown.ReferenceState.ShouldBe(ReferenceState.Unauthorized);

        ProjectConversationAssignmentResult assigned = await new ConversationsProjectConversationAssignmentDirectory(client, new DeterministicActorPartyResolver())
            .LinkAsync(
                ProjectId(graph),
                new ConversationId(graph.ConversationId),
                tenantId,
                caller,
                new ProjectConversationCommandMetadata(CorrelationId, graph.TaskId, graph.IdempotencyKey),
                cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        assigned.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Accepted);
    }

    /// <summary>Verifies the degraded Project's conversations report deterministic Stale, Forbidden, and Unavailable trust.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ConversationsRoleShouldServeDeterministicDegradedTrustForTheDegradedProject()
    {
        LiveFixtureGraph graph = Graph();
        LiveFixtureRoleHost host = await LiveFixtureRoleHost
            .StartAsync("conversations", graph, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await using ConfiguredAsyncDisposable hostScope = host.ConfigureAwait(true);
        using ServiceProvider provider = Services(services =>
            services.AddHexalithConversationsClient(options => options.Endpoint = host.BaseAddress));
        IConversationClient client = provider.GetRequiredService<IConversationClient>();
        ConversationTenantId tenantId = new(graph.TenantId);
        CallerPrincipalId caller = new(graph.PrincipalId);

        ProjectConversationsPage page = await new ConversationsProjectConversationDirectory(client)
            .ListForProjectAsync(new ProjectId(graph.DegradedProjectId), tenantId, caller, new PageRequest(25), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        page.Items
            .Select(static item => (item.ConversationId.Value, item.TrustSignal))
            .ShouldBe(
                new[]
                {
                    (graph.StaleConversationId, ProjectConversationTrustSignal.Stale),
                    (graph.ForbiddenConversationId, ProjectConversationTrustSignal.Forbidden),
                    (graph.UnavailableConversationId, ProjectConversationTrustSignal.Unavailable),
                },
                ignoreOrder: true);

        ConversationsProjectConversationResolutionDirectory resolution = new(client);
        (await resolution
            .ReadConversationMetadataAsync(new ConversationId(graph.StaleConversationId), tenantId, caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ReferenceState.ShouldBe(ReferenceState.Stale);
        (await resolution
            .ReadConversationMetadataAsync(new ConversationId(graph.ForbiddenConversationId), tenantId, caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ReferenceState.ShouldBe(ReferenceState.Unauthorized);

        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        ConversationResolutionMetadata unavailable = await resolution
            .ReadConversationMetadataAsync(new ConversationId(graph.UnavailableConversationId), tenantId, caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        unavailable.ReferenceState.ShouldBe(ReferenceState.Unavailable);
        System.Diagnostics.Stopwatch.GetElapsedTime(started).ShouldBeGreaterThanOrEqualTo(ConversationsFixtureEndpoints.UnavailableReadDelay - TimeSpan.FromMilliseconds(100));
    }

    /// <summary>Verifies the Memories role serves the graph case and safely denies other tenants and cases.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task MemoriesRoleShouldServeTheGraphCaseThroughTheMemoriesClient()
    {
        LiveFixtureGraph graph = Graph();
        LiveFixtureRoleHost host = await LiveFixtureRoleHost
            .StartAsync("memories", graph, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await using ConfiguredAsyncDisposable hostScope = host.ConfigureAwait(true);
        using ServiceProvider provider = Services(services => services.AddMemoriesClient(options => options.Endpoint = host.BaseAddress));
        MemoriesProjectMemoryDirectory memories = new(provider.GetRequiredService<MemoriesClient>());

        ProjectMemoryValidationResult accepted = await memories
            .ValidateLinkMemoryReferenceAsync(ProjectId(graph), graph.MemoryReferenceId, graph.TenantId, CorrelationId, graph.TaskId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        accepted.Outcome.ShouldBe(ProjectMemoryValidationOutcome.Accepted);

        ProjectMemoryValidationResult unknown = await memories
            .ValidateLinkMemoryReferenceAsync(ProjectId(graph), Id("memory-unknown"), graph.TenantId, CorrelationId, graph.TaskId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        unknown.Outcome.ShouldNotBe(ProjectMemoryValidationOutcome.Accepted);

        ProjectMemoryValidationResult otherTenant = await memories
            .ValidateLinkMemoryReferenceAsync(ProjectId(graph), graph.MemoryReferenceId, "tenant-other", CorrelationId, graph.TaskId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        otherTenant.Outcome.ShouldNotBe(ProjectMemoryValidationOutcome.Accepted);
    }

    private static ServiceProvider Services(Action<IServiceCollection> configure)
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        configure(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static ProjectId ProjectId(LiveFixtureGraph graph) => new(graph.ProjectId);

    // Mirrors the runner's identity shape: kind prefix plus a 40-character lowercase hex digest.
    private static string Id(string kind)
        => $"{kind}-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"sibling-contract|{kind}")))[..40]}";

    private static LiveFixtureGraph Graph()
        => new(
            GraphId: Id("graph"),
            RunId: "run-sibling-contract-full",
            WorkerIndex: 0,
            Retry: 0,
            RepeatEachIndex: 0,
            Scenario: "projects-resolution-0123456789abcdef-sibling-contract",
            TenantId: "tenant-a",
            PrincipalId: "principal-sibling-contract",
            ProjectId: Id("project"),
            SecondaryProjectId: Id("project-secondary"),
            ProposalProjectId: Id("project-proposal"),
            ProposalRetryProjectId: Id("project-proposal-retry"),
            DegradedProjectId: Id("project-degraded"),
            ConversationId: Id("conversation"),
            AmbiguousConversationId: Id("conversation-ambiguous"),
            ExistingConversationId: Id("conversation-existing"),
            StaleConversationId: Id("conversation-stale"),
            ForbiddenConversationId: Id("conversation-forbidden"),
            UnavailableConversationId: Id("conversation-unavailable"),
            FolderId: Id("folder"),
            SecondaryFolderId: Id("folder-secondary"),
            ProposalFolderId: Id("folder-proposal"),
            WorkspaceId: Id("workspace"),
            FileReferenceId: Id("file"),
            SecondaryFileReferenceId: Id("file-secondary"),
            ProposalFileReferenceId: Id("file-proposal"),
            DeniedFileReferenceId: Id("file-denied"),
            FilePath: "docs/contract.pdf",
            MemoryReferenceId: Id("memory"),
            CorrelationId: Id("correlation"),
            TaskId: Id("task"),
            IdempotencyKey: Id("idempotency"));
}
