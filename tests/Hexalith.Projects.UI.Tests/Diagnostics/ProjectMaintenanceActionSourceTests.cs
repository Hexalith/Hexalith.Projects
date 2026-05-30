// <copyright file="ProjectMaintenanceActionSourceTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Diagnostics;

using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Leakage;
using Hexalith.Projects.UI.Diagnostics;

using NSubstitute;

using Shouldly;

using Xunit;

using Generated = Hexalith.Projects.Client.Generated;

/// <summary>
/// Tests for the generated-client backed maintenance action source.
/// </summary>
public sealed class ProjectMaintenanceActionSourceTests
{
    [Fact]
    public async Task ExecuteAsync_UnlinksFileReferenceThroughExistingEndpoint()
    {
        Generated.IClient client = Substitute.For<Generated.IClient>();
        client.UnlinkFileReferenceAsync(
                "project-001",
                "file-001",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Is<Generated.UnlinkFileReferenceRequest>(body =>
                    body.ProjectId == "project-001"
                    && body.FileReferenceId == "file-001"
                    && body.Operation == Generated.UnlinkFileReferenceRequestOperation.Unlink
                    && body.UnlinkIntent == Generated.UnlinkFileReferenceRequestUnlinkIntent.RemoveReference),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Accepted()));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                Generated.ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Diagnostic("file_reference.unlinked", call.ArgAt<string>(2))));

        ProjectMaintenanceActionSource source = new(client);

        ProjectMaintenanceActionExecutionResult result = await source.ExecuteAsync(
            new ProjectMaintenanceActionExecutionRequest(
                "project-001",
                ProjectMaintenanceActions.Unlink,
                "file",
                "file-001",
                "File",
                true,
                null,
                null,
                null,
                "file_reference.unlinked"),
            null,
            TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeTrue();
        result.AuditEventId.ShouldBe("audit-confirmed");
        await client.Received(1)
            .UnlinkFileReferenceAsync(
                "project-001",
                "file-001",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Generated.UnlinkFileReferenceRequest>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task ExecuteAsync_RelinksMemoryThroughExistingEndpoint()
    {
        Generated.IClient client = Substitute.For<Generated.IClient>();
        client.LinkMemoryAsync(
                "project-001",
                "memory-001",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Is<Generated.LinkMemoryRequest>(body =>
                    body.ProjectId == "project-001"
                    && body.MemoryReferenceId == "memory-001"
                    && body.Operation == Generated.LinkMemoryRequestOperation.Link
                    && body.MemoryMetadata.DisplayName == "Memory"),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Accepted()));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                Generated.ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Diagnostic("memory.linked", call.ArgAt<string>(2))));

        ProjectMaintenanceActionSource source = new(client);

        ProjectMaintenanceActionExecutionResult result = await source.ExecuteAsync(
            new ProjectMaintenanceActionExecutionRequest(
                "project-001",
                ProjectMaintenanceActions.Relink,
                "memory",
                "memory-001",
                "Memory",
                true,
                null,
                null,
                null,
                "memory.linked"),
            null,
            TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeTrue();
        await client.Received(1)
            .LinkMemoryAsync(
                "project-001",
                "memory-001",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Generated.LinkMemoryRequest>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task ExecuteAsync_BlocksFileRelinkWithoutTransientValidationEvidence()
    {
        Generated.IClient client = Substitute.For<Generated.IClient>();
        ProjectMaintenanceActionSource source = new(client);

        ProjectMaintenanceActionExecutionResult result = await source.ExecuteAsync(
            new ProjectMaintenanceActionExecutionRequest(
                "project-001",
                ProjectMaintenanceActions.Relink,
                "file",
                "file-001",
                "File",
                true,
                null,
                null,
                null,
                "file_reference.linked"),
            null,
            TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeFalse();
        result.FeedbackCode.ShouldBe("transient_validation_required");
        await client.DidNotReceiveWithAnyArgs()
            .LinkFileReferenceAsync(default!, default!, default!, default!, default!, default!, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task ExecuteAsync_RelinksFileThroughExistingEndpointWhenTransientEvidenceIsProvided()
    {
        Generated.IClient client = Substitute.For<Generated.IClient>();
        client.LinkFileReferenceAsync(
                "project-001",
                "file-001",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Is<Generated.LinkFileReferenceRequest>(body =>
                    body.ProjectId == "project-001"
                    && body.FileReferenceId == "file-001"
                    && body.FolderId == "folder-001"
                    && body.WorkspaceId == "workspace-001"
                    && body.FilePath == "docs/contract.pdf"
                    && body.Operation == Generated.LinkFileReferenceRequestOperation.Link
                    && body.FileMetadata.DisplayName == "File"),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Accepted()));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                Generated.ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Diagnostic("file_reference.linked", call.ArgAt<string>(2))));

        ProjectMaintenanceActionSource source = new(client);

        ProjectMaintenanceActionExecutionResult result = await source.ExecuteAsync(
            new ProjectMaintenanceActionExecutionRequest(
                "project-001",
                ProjectMaintenanceActions.Relink,
                "file",
                "file-001",
                "File",
                true,
                "folder-001",
                "workspace-001",
                "docs/contract.pdf",
                "file_reference.linked"),
            null,
            TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeTrue();
        await client.Received(1)
            .LinkFileReferenceAsync(
                "project-001",
                "file-001",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Generated.LinkFileReferenceRequest>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task ExecuteAsync_ReportsAcknowledgedThenSyncingLifecycleBeforeConfirmation()
    {
        Generated.IClient client = Substitute.For<Generated.IClient>();
        client.UnlinkMemoryAsync(
                "project-001",
                "memory-001",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Generated.UnlinkMemoryRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Accepted()));
        client.GetProjectOperatorDiagnosticsAsync(
                "project-001",
                25,
                Arg.Any<string>(),
                Generated.ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Diagnostic("memory.unlinked", call.ArgAt<string>(2))));

        ProjectMaintenanceActionSource source = new(client);
        RecordingProgress lifecycle = new();

        ProjectMaintenanceActionExecutionResult result = await source.ExecuteAsync(
            new ProjectMaintenanceActionExecutionRequest(
                "project-001",
                ProjectMaintenanceActions.Unlink,
                "memory",
                "memory-001",
                "Memory",
                true,
                null,
                null,
                null,
                "memory.unlinked"),
            lifecycle,
            TestContext.Current.CancellationToken);

        // The 202 acknowledgement is observably distinct from the final audit confirmation.
        result.Succeeded.ShouldBeTrue();
        lifecycle.Reports.ShouldBe(
        [
            ProjectMaintenanceCommandLifecycleStates.Acknowledged,
            ProjectMaintenanceCommandLifecycleStates.Syncing,
        ]);
    }

    [Fact]
    public async Task ExecuteAsync_ReevaluateUsesReadOnlyRefreshAndEmitsNoAuditEvent()
    {
        Generated.IClient client = Substitute.For<Generated.IClient>();
        ProjectMaintenanceActionSource source = new(client);

        ProjectMaintenanceActionExecutionResult result = await source.ExecuteAsync(
            new ProjectMaintenanceActionExecutionRequest(
                "project-001",
                ProjectMaintenanceActions.Reevaluate,
                null,
                null,
                null,
                true,
                null,
                null,
                null,
                "none (read-only recompute)"),
            null,
            TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeTrue();
        result.AuditEventId.ShouldBeNull();
        result.FeedbackCode.ShouldBe("diagnostics_reloaded");

        // Re-evaluate is a read-only recompute: it calls only the refresh query and never a mutation
        // or an audit-confirmation poll.
        await client.Received(1)
            .RefreshProjectContextAsync(
                "project-001",
                Arg.Any<string>(),
                Generated.ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await client.DidNotReceiveWithAnyArgs()
            .GetProjectOperatorDiagnosticsAsync(default!, default, default!, default, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await client.DidNotReceiveWithAnyArgs()
            .ArchiveProjectAsync(default!, default!, default!, default!, default!, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task ExecuteAsync_ConversationUnlinkConfirmsOnAcceptanceWithoutPollingProjectsAudit()
    {
        Generated.IClient client = Substitute.For<Generated.IClient>();
        client.UnlinkProjectConversationAsync(
                "project-001",
                "conversation-001",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Is<Generated.UnlinkProjectConversationRequest>(body =>
                    body.ProjectId == "project-001"
                    && body.ConversationId == "conversation-001"
                    && body.Operation == Generated.UnlinkProjectConversationRequestOperation.Unlink),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Accepted()));

        ProjectMaintenanceActionSource source = new(client);

        ProjectMaintenanceActionExecutionResult result = await source.ExecuteAsync(
            new ProjectMaintenanceActionExecutionRequest(
                "project-001",
                ProjectMaintenanceActions.Unlink,
                "conversation",
                "conversation-001",
                "Conversation",
                true,
                null,
                null,
                null,
                "conversation assignment (Conversations-owned)"),
            null,
            TestContext.Current.CancellationToken);

        // Conversation unlink is Conversations-owned and emits no Projects audit row, so confirmation is
        // keyed on the 202 acceptance and the Projects audit timeline is never polled.
        result.Succeeded.ShouldBeTrue();
        result.AuditEventId.ShouldBeNull();
        result.FeedbackCode.ShouldBe("conversation_unlink_accepted");
        await client.Received(1)
            .UnlinkProjectConversationAsync(
                "project-001",
                "conversation-001",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Generated.UnlinkProjectConversationRequest>(),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await client.DidNotReceiveWithAnyArgs()
            .GetProjectOperatorDiagnosticsAsync(default!, default, default!, default, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    [Fact]
    public void MaintenanceExecutionDtos_SerializeMetadataOnly()
    {
        // Story 5.9 (AC 13): the maintenance request/result DTOs must serialize metadata-only. The request
        // carries transient Folders-ACL inputs (workspace id + workspace-relative path) that are validation
        // inputs only and must never carry machine-local paths or sibling-owned content; the result carries
        // only correlation/task/audit ids and a safe feedback code.
        var request = new ProjectMaintenanceActionExecutionRequest(
            "project-001",
            ProjectMaintenanceActions.Relink,
            "file",
            "file-001",
            "Contract file",
            true,
            "folder-001",
            "workspace-001",
            "docs/contract.pdf",
            "file_reference.linked");
        ProjectMaintenanceActionExecutionResult result =
            ProjectMaintenanceActionExecutionResult.Confirmed("corr-001", "task-001", "audit-001");

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(request));
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(result));
    }

    private static Generated.AcceptedCommand Accepted()
        => new()
        {
            AcceptedAt = DateTimeOffset.UnixEpoch,
            CorrelationId = "corr-accepted",
            TaskId = "task-accepted",
            Status = Generated.AcceptedCommandStatus.Accepted,
            IdempotentReplay = false,
        };

    private static Generated.ProjectOperatorDiagnostic Diagnostic(string operationType, string correlationId)
        => new()
        {
            ProjectId = "project-001",
            Name = "Project",
            Description = "Safe metadata",
            LifecycleState = Generated.ProjectLifecycleState.Active,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            SetupMetadata = "safe-setup",
            ContextActivation = new Generated.ContextActivation
            {
                Enabled = true,
            },
            AuditTimeline =
            [
                new Generated.ProjectOperatorAuditTimelineItem
                {
                    AuditEventId = "audit-confirmed",
                    OperationType = operationType,
                    OccurredAt = DateTimeOffset.UnixEpoch,
                    ActorPrincipalId = "actor-001",
                    CorrelationId = correlationId,
                    TaskId = "task-001",
                    ProjectionSequence = 1,
                },
            ],
            Freshness = new Generated.FreshnessMetadata
            {
                ReadConsistency = Generated.ReadConsistencyClass.Eventually_consistent,
                ObservedAt = DateTimeOffset.UnixEpoch,
                ProjectionWatermark = "watermark-001",
                TrustState = Generated.ProjectionTrustState.Trusted,
            },
        };

    /// <summary>Records lifecycle progress reports synchronously for deterministic assertions.</summary>
    private sealed class RecordingProgress : IProgress<string>
    {
        public List<string> Reports { get; } = [];

        public void Report(string value) => Reports.Add(value);
    }
}
