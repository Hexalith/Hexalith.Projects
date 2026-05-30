// <copyright file="ProjectResolutionTraceSourceTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Diagnostics;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Leakage;
using Hexalith.Projects.UI.Diagnostics;
using Hexalith.Projects.UI.Rendering;

using NSubstitute;

using Shouldly;

using Xunit;

using ContractResolutionResult = Hexalith.Projects.Contracts.Ui.ResolutionResult;
using GeneratedReasonCode = Hexalith.Projects.Client.Generated.ReasonCodes;
using GeneratedResolutionResult = Hexalith.Projects.Client.Generated.ResolutionResult;

/// <summary>
/// Tests for the generated-client backed resolution trace source and mapper.
/// </summary>
public sealed class ProjectResolutionTraceSourceTests
{
    [Fact]
    public async Task ConversationModeCallsGeneratedQueryWithEventuallyConsistentFreshness()
    {
        IClient client = Substitute.For<IClient>();
        client.ResolveProjectFromConversationAsync(
                "conversation-001",
                true,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(SingleCandidateResolution()));

        using var cts = new CancellationTokenSource();
        var source = new ProjectResolutionTraceSource(client);
        ProjectResolutionTraceLoadResult result = await source
            .LoadTraceAsync(ProjectResolutionTraceRequest.ForConversation(" conversation-001 ", includeArchived: true), cts.Token)
            .ConfigureAwait(true);

        result.Trace.ShouldNotBeNull();
        result.Trace.InputMode.ShouldBe(ProjectResolutionTraceRequest.ConversationMode);
        result.Trace.PresentedConversationId.ShouldBe("conversation-001");
        result.Trace.Result.ShouldBe(ContractResolutionResult.SingleCandidate);
        result.Candidates.ShouldHaveSingleItem().ProjectId.ShouldBe("project-001");
        await client.Received(1).ResolveProjectFromConversationAsync(
            "conversation-001",
            true,
            Arg.Is<string>(value => !string.IsNullOrWhiteSpace(value)),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Is<CancellationToken>(token => token == cts.Token)).ConfigureAwait(true);
        await client.DidNotReceive().ResolveProjectFromAttachmentsAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<bool?>(),
            Arg.Any<string>(),
            Arg.Any<ReadConsistencyClass?>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task AttachmentModeDedupesAndOrdersInputsBeforeCallingGeneratedQuery()
    {
        IClient client = Substitute.For<IClient>();
        client.ResolveProjectFromAttachmentsAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IEnumerable<string>>(),
                false,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(MultipleCandidatesResolution()));

        var source = new ProjectResolutionTraceSource(client);
        ProjectResolutionTraceLoadResult result = await source
            .LoadTraceAsync(
                ProjectResolutionTraceRequest.ForAttachments(
                    ["folder-b", "folder-a", "folder-b"],
                    ["file-2 file-1", "file-2"],
                    includeArchived: false),
                CancellationToken.None)
            .ConfigureAwait(true);

        result.Trace.ShouldNotBeNull();
        result.Trace.PresentedFolderIds.ShouldBe("folder-a, folder-b");
        result.Trace.PresentedFileIds.ShouldBe("file-1, file-2");
        result.Trace.Result.ShouldBe(ContractResolutionResult.MultipleCandidates);
        await client.Received(1).ResolveProjectFromAttachmentsAsync(
            Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "folder-a", "folder-b" })),
            Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "file-1", "file-2" })),
            false,
            Arg.Is<string>(value => !string.IsNullOrWhiteSpace(value)),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Theory]
    [InlineData(400, ProjectConsoleFeedback.ErrorCategory, "validation_error")]
    [InlineData(404, ProjectConsoleFeedback.FailClosedCategory, "safe_denial")]
    [InlineData(503, ProjectConsoleFeedback.WarningCategory, "data_unavailable")]
    [InlineData(500, ProjectConsoleFeedback.ErrorCategory, "resolution_trace_query_failed")]
    public async Task SourceMapsApiFailuresToSafeFeedbackWithoutEchoingProblemDetails(
        int statusCode,
        string category,
        string reasonCode)
    {
        IClient client = Substitute.For<IClient>();
        client.ResolveProjectFromConversationAsync(
                "conversation-001",
                false,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<ProjectResolution>>(_ => throw new HexalithProjectsApiException(
                "unsafe response hidden",
                statusCode,
                "secret token transcript raw problem body",
                new Dictionary<string, IEnumerable<string>>(),
                null!));

        var source = new ProjectResolutionTraceSource(client);
        ProjectResolutionTraceLoadResult result = await source
            .LoadTraceAsync(ProjectResolutionTraceRequest.ForConversation("conversation-001", includeArchived: false), CancellationToken.None)
            .ConfigureAwait(true);

        result.Trace.ShouldBeNull();
        result.Feedback.ShouldNotBeNull();
        result.Feedback.Category.ShouldBe(category);
        result.Feedback.SafeReasonCode.ShouldBe(reasonCode);
        result.Feedback.Message.ShouldNotContain("secret");
        result.Feedback.Message.ShouldNotContain("token");
        result.Feedback.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public async Task SourceValidatesEmptyAndMixedInputsBeforeCallingClient()
    {
        IClient client = Substitute.For<IClient>();
        var source = new ProjectResolutionTraceSource(client);

        ProjectResolutionTraceLoadResult empty = await source
            .LoadTraceAsync(ProjectResolutionTraceRequest.ForConversation(" ", includeArchived: false), CancellationToken.None)
            .ConfigureAwait(true);
        ProjectResolutionTraceLoadResult mixed = await source
            .LoadTraceAsync(
                new ProjectResolutionTraceRequest(ProjectResolutionTraceRequest.ConversationMode, "conversation-001", ["folder-001"], [], false),
                CancellationToken.None)
            .ConfigureAwait(true);
        ProjectResolutionTraceLoadResult emptyAttachments = await source
            .LoadTraceAsync(ProjectResolutionTraceRequest.ForAttachments([], [], includeArchived: false), CancellationToken.None)
            .ConfigureAwait(true);

        empty.Feedback.ShouldNotBeNull();
        empty.Feedback.SafeReasonCode.ShouldBe("conversation_id_required");
        mixed.Feedback.ShouldNotBeNull();
        mixed.Feedback.SafeReasonCode.ShouldBe("mixed_trace_input");
        emptyAttachments.Feedback.ShouldNotBeNull();
        emptyAttachments.Feedback.SafeReasonCode.ShouldBe("attachment_ids_required");
        await client.DidNotReceiveWithAnyArgs()
            .ResolveProjectFromConversationAsync(default!, default, default!, default, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await client.DidNotReceiveWithAnyArgs()
            .ResolveProjectFromAttachmentsAsync(default!, default!, default, default!, default, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task SourceMapsTransportAndDeserializationFailuresToSafeFeedback()
    {
        IClient client = Substitute.For<IClient>();
        client.ResolveProjectFromConversationAsync(
                "conversation-001",
                false,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<ProjectResolution>>(_ => throw new InvalidOperationException("transcript secret token raw ProblemDetails"));

        var source = new ProjectResolutionTraceSource(client);
        ProjectResolutionTraceLoadResult result = await source
            .LoadTraceAsync(ProjectResolutionTraceRequest.ForConversation("conversation-001", includeArchived: false), CancellationToken.None)
            .ConfigureAwait(true);

        result.Trace.ShouldBeNull();
        result.Feedback.ShouldNotBeNull();
        result.Feedback.Category.ShouldBe(ProjectConsoleFeedback.ErrorCategory);
        result.Feedback.SafeReasonCode.ShouldBe("resolution_trace_query_failed");
        result.Feedback.Message.ShouldNotContain("transcript");
        result.Feedback.Message.ShouldNotContain("secret");
        result.Feedback.Message.ShouldNotContain("token");
        result.Feedback.Message.ShouldNotContain("ProblemDetails");
    }

    [Fact]
    public async Task SourcePropagatesCancellation()
    {
        IClient client = Substitute.For<IClient>();
        client.ResolveProjectFromConversationAsync(
                "conversation-001",
                false,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns<Task<ProjectResolution>>(_ => throw new OperationCanceledException());

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var source = new ProjectResolutionTraceSource(client);

        await Should.ThrowAsync<OperationCanceledException>(() => source.LoadTraceAsync(
            ProjectResolutionTraceRequest.ForConversation("conversation-001", includeArchived: false),
            cts.Token));
    }

    [Fact]
    public void MapperDerivesResolvedNoMatchMultipleExcludedAndFailedClosedOutcomeLabels()
    {
        ProjectResolutionTraceMapper.DeriveOutcomeLabel(
            SingleCandidateTrace().Trace!,
            []).ShouldBe("Resolved");
        ProjectResolutionTraceMapper.DeriveOutcomeLabel(
            NoMatchTrace().Trace!,
            []).ShouldBe("NoMatch");
        ProjectResolutionTraceMapper.DeriveOutcomeLabel(
            MultipleCandidatesTrace().Trace!,
            []).ShouldBe("MultipleCandidates");

        ProjectResolutionTraceLoadResult excluded = ProjectResolutionTraceMapper.ToLoadResult(
            ProjectResolutionTraceRequest.ForConversation("conversation-001", includeArchived: false),
            [],
            [],
            new ProjectResolution
            {
                Result = GeneratedResolutionResult.NoMatch,
                ObservedAt = DateTimeOffset.UnixEpoch,
                Excluded =
                {
                    new ResolutionExclusion
                    {
                        ProjectId = "project-archived",
                        ReferenceState = ResolutionExclusionReferenceState.Archived,
                        Diagnostic = ProjectContextInclusionDiagnostic.ReferenceArchived,
                    },
                },
            });
        ProjectResolutionTraceMapper.DeriveOutcomeLabel(excluded.Trace!, excluded.Exclusions).ShouldBe("Excluded");

        ProjectResolutionTraceLoadResult failedClosed = ProjectResolutionTraceMapper.ToLoadResult(
            ProjectResolutionTraceRequest.ForConversation("conversation-001", includeArchived: false),
            [],
            [],
            new ProjectResolution
            {
                Result = GeneratedResolutionResult.NoMatch,
                ObservedAt = DateTimeOffset.UnixEpoch,
                Excluded =
                {
                    new ResolutionExclusion
                    {
                        ProjectId = "project-denied",
                        ReferenceState = ResolutionExclusionReferenceState.Unauthorized,
                        Diagnostic = ProjectContextInclusionDiagnostic.ReferenceUnauthorized,
                    },
                },
            });
        ProjectResolutionTraceMapper.DeriveOutcomeLabel(failedClosed.Trace!, failedClosed.Exclusions).ShouldBe("FailedClosed");
    }

    [Fact]
    public void MapperDerivesExcludedForPolicyRedactedExclusion()
    {
        // Policy-redacted is a deliberate policy exclusion (like archived), not unverifiable evidence,
        // so it must map to "Excluded" and never to "FailedClosed" per the documented Trace Mapping.
        ProjectResolutionTraceLoadResult redacted = ProjectResolutionTraceMapper.ToLoadResult(
            ProjectResolutionTraceRequest.ForConversation("conversation-001", includeArchived: false),
            [],
            [],
            new ProjectResolution
            {
                Result = GeneratedResolutionResult.NoMatch,
                ObservedAt = DateTimeOffset.UnixEpoch,
                Excluded =
                {
                    new ResolutionExclusion
                    {
                        ProjectId = "project-redacted",
                        ReferenceState = ResolutionExclusionReferenceState.Excluded,
                        Diagnostic = ProjectContextInclusionDiagnostic.ReferenceRedacted,
                    },
                },
            });

        ProjectResolutionTraceMapper.DeriveOutcomeLabel(redacted.Trace!, redacted.Exclusions).ShouldBe("Excluded");
    }

    [Theory]
    [InlineData("file-2,file-1")]
    [InlineData("file-2;file-1")]
    [InlineData("file-2\nfile-1")]
    [InlineData("file-2\r\nfile-1")]
    [InlineData("file-2\tfile-1")]
    [InlineData("file-2 file-1")]
    public async Task AttachmentModeSplitsAllSupportedDelimiters(string fileInput)
    {
        IClient client = Substitute.For<IClient>();
        client.ResolveProjectFromAttachmentsAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IEnumerable<string>>(),
                false,
                Arg.Any<string>(),
                ReadConsistencyClass.Eventually_consistent,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(MultipleCandidatesResolution()));

        var source = new ProjectResolutionTraceSource(client);
        ProjectResolutionTraceLoadResult result = await source
            .LoadTraceAsync(
                ProjectResolutionTraceRequest.ForAttachments([], [fileInput], includeArchived: false),
                CancellationToken.None)
            .ConfigureAwait(true);

        result.Trace.ShouldNotBeNull();
        result.Trace.PresentedFileIds.ShouldBe("file-1, file-2");
        await client.Received(1).ResolveProjectFromAttachmentsAsync(
            Arg.Is<IEnumerable<string>>(ids => !ids.Any()),
            Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "file-1", "file-2" })),
            false,
            Arg.Is<string>(value => !string.IsNullOrWhiteSpace(value)),
            ReadConsistencyClass.Eventually_consistent,
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public void TraceLoadResultSerializesMetadataOnly()
    {
        ProjectResolutionTraceLoadResult result = SingleCandidateTrace();

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(result));
        string serialized = System.Text.Json.JsonSerializer.Serialize(result);
        serialized.ShouldNotContain("tenantId", Case.Insensitive);
        serialized.ShouldNotContain("correlation", Case.Insensitive);
        serialized.ShouldNotContain("task", Case.Insensitive);
        serialized.ShouldNotContain("transcript", Case.Insensitive);
        serialized.ShouldNotContain("token", Case.Insensitive);
    }

    private static ProjectResolutionTraceLoadResult SingleCandidateTrace()
        => ProjectResolutionTraceMapper.ToLoadResult(
            ProjectResolutionTraceRequest.ForConversation("conversation-001", includeArchived: true),
            [],
            [],
            SingleCandidateResolution());

    private static ProjectResolutionTraceLoadResult NoMatchTrace()
        => ProjectResolutionTraceMapper.ToLoadResult(
            ProjectResolutionTraceRequest.ForConversation("conversation-001", includeArchived: false),
            [],
            [],
            new ProjectResolution
            {
                Result = GeneratedResolutionResult.NoMatch,
                ObservedAt = DateTimeOffset.UnixEpoch,
            });

    private static ProjectResolutionTraceLoadResult MultipleCandidatesTrace()
        => ProjectResolutionTraceMapper.ToLoadResult(
            ProjectResolutionTraceRequest.ForAttachments(["folder-001"], ["file-001"], includeArchived: false),
            ["folder-001"],
            ["file-001"],
            MultipleCandidatesResolution());

    private static ProjectResolution SingleCandidateResolution()
        => new()
        {
            Result = GeneratedResolutionResult.SingleCandidate,
            ObservedAt = DateTimeOffset.UnixEpoch,
            Candidates =
            {
                new ResolutionCandidate
                {
                    ProjectId = "project-001",
                    DisplayName = "Console Project",
                    Rank = 1,
                    Score = 70,
                    ReasonCodes = { GeneratedReasonCode.ConversationLinked, GeneratedReasonCode.MetadataMatched },
                },
            },
        };

    private static ProjectResolution MultipleCandidatesResolution()
        => new()
        {
            Result = GeneratedResolutionResult.MultipleCandidates,
            ObservedAt = DateTimeOffset.UnixEpoch,
            Candidates =
            {
                new ResolutionCandidate
                {
                    ProjectId = "project-001",
                    Rank = 1,
                    Score = 45,
                    ReasonCodes = { GeneratedReasonCode.ProjectFolderMatched },
                },
                new ResolutionCandidate
                {
                    ProjectId = "project-002",
                    Rank = 2,
                    Score = 35,
                    ReasonCodes = { GeneratedReasonCode.FileReferenceMatched },
                },
            },
        };
}
