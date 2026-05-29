// <copyright file="ConversationsProjectConversationResolutionDirectoryTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Conversations;

using System.Net;
using System.Net.Http;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Server.Conversations;

using Shouldly;

using Xunit;

using CallerPrincipalId = Hexalith.Projects.Contracts.Identifiers.CallerPrincipalId;
using ConversationProjectId = Hexalith.Conversations.Contracts.Identifiers.ProjectId;

/// <summary>
/// Story 4.2 Tier-2 fail-closed tests for the single-conversation metadata ACL
/// (<see cref="ConversationsProjectConversationResolutionDirectory"/>) and its
/// <see cref="UnavailableProjectConversationResolutionDirectory"/> default. Proves the upstream
/// trust/HTTP posture is mapped onto the shared <see cref="ReferenceState"/> vocabulary, the per-row
/// tenant/conversation scope re-check collapses any escape to a fail-closed record, and the linked
/// project precedence (explicit ProjectId before response-scoped hydration) is honoured — all without
/// leaking an out-of-scope identity.
/// </summary>
public sealed class ConversationsProjectConversationResolutionDirectoryTests
{
    private static readonly TenantId Tenant = new("tenant-a");
    private static readonly CallerPrincipalId Caller = new("principal-a");
    private static readonly ConversationId Conversation = new("conversation-001");
    private const string CorrelationId = "corr-a";

    [Fact]
    public async Task ReadConversationMetadata_VisibleCurrentWithExplicitProject_SurfacesLinkedProjectLabelAndIncluded()
    {
        StubConversationClient client = new()
        {
            DetailResult = VisibleSuccess(
                Details(projectId: new ConversationProjectId("project-a"), label: "Project Alpha")),
        };
        ConversationsProjectConversationResolutionDirectory directory = new(client);

        ConversationResolutionMetadata metadata = await directory
            .ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metadata.ConversationId.ShouldBe(Conversation.Value);
        metadata.LinkedProjectId.ShouldBe("project-a");
        metadata.SafeLabel.ShouldBe("Project Alpha");
        metadata.ReferenceState.ShouldBe(ReferenceState.Included);
    }

    [Fact]
    public async Task ReadConversationMetadata_ProjectlessConversation_YieldsNoLinkedProjectButRemainsIncluded()
    {
        StubConversationClient client = new() { DetailResult = VisibleSuccess(Details(projectId: null, label: null)) };
        ConversationsProjectConversationResolutionDirectory directory = new(client);

        ConversationResolutionMetadata metadata = await directory
            .ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metadata.LinkedProjectId.ShouldBeNull();
        metadata.SafeLabel.ShouldBeNull();
        metadata.ReferenceState.ShouldBe(ReferenceState.Included);
    }

    [Fact]
    public async Task ReadConversationMetadata_NoExplicitProjectButHydrationPointer_UsesHydrationAsSoftLink()
    {
        StubConversationClient client = new()
        {
            DetailResult = VisibleSuccess(Details(
                projectId: null,
                label: "Hydrated",
                hydration: new ProjectReferenceHydrationV1(
                    new ConversationProjectId("project-hydrated"),
                    ProjectionTrustState.Current,
                    Resolved: true,
                    SafeLabel: "Hydrated",
                    SafeToken: "tok",
                    SafeStatus: "Active"))),
        };
        ConversationsProjectConversationResolutionDirectory directory = new(client);

        ConversationResolutionMetadata metadata = await directory
            .ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metadata.LinkedProjectId.ShouldBe("project-hydrated");
    }

    [Fact]
    public async Task ReadConversationMetadata_ExplicitProjectWins_OverHydrationPointer()
    {
        StubConversationClient client = new()
        {
            DetailResult = VisibleSuccess(Details(
                projectId: new ConversationProjectId("project-explicit"),
                label: null,
                hydration: new ProjectReferenceHydrationV1(
                    new ConversationProjectId("project-hydrated"),
                    ProjectionTrustState.Current,
                    Resolved: true,
                    SafeLabel: "Hydrated",
                    SafeToken: "tok",
                    SafeStatus: "Active"))),
        };
        ConversationsProjectConversationResolutionDirectory directory = new(client);

        ConversationResolutionMetadata metadata = await directory
            .ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metadata.LinkedProjectId.ShouldBe("project-explicit");
    }

    [Theory]
    [InlineData("Current", ReferenceState.Included)]
    [InlineData("Stale", ReferenceState.Stale)]
    [InlineData("Forbidden", ReferenceState.Unauthorized)]
    [InlineData("Redacted", ReferenceState.Excluded)]
    [InlineData("Rebuilding", ReferenceState.Unavailable)]
    [InlineData("Unavailable", ReferenceState.Unavailable)]
    public async Task ReadConversationMetadata_MapsUpstreamTrustPostureOntoReferenceState(
        string upstreamTrust,
        ReferenceState expectedReferenceState)
    {
        StubConversationClient client = new()
        {
            DetailResult = VisibleSuccess(Details(
                projectId: new ConversationProjectId("project-a"),
                label: "Project Alpha",
                trust: TrustState(upstreamTrust))),
        };
        ConversationsProjectConversationResolutionDirectory directory = new(client);

        ConversationResolutionMetadata metadata = await directory
            .ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metadata.ReferenceState.ShouldBe(expectedReferenceState);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ReferenceState.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ReferenceState.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ReferenceState.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError, ReferenceState.Unavailable)]
    [InlineData(HttpStatusCode.BadGateway, ReferenceState.Unavailable)]
    public async Task ReadConversationMetadata_UpstreamFailure_FailsClosedWithoutLinkOrLabel(
        HttpStatusCode statusCode,
        ReferenceState expectedReferenceState)
    {
        StubConversationClient client = new() { DetailResult = Failure(statusCode) };
        ConversationsProjectConversationResolutionDirectory directory = new(client);

        ConversationResolutionMetadata metadata = await directory
            .ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metadata.ReferenceState.ShouldBe(expectedReferenceState);
        metadata.LinkedProjectId.ShouldBeNull();
        metadata.SafeLabel.ShouldBeNull();
    }

    [Fact]
    public async Task ReadConversationMetadata_TenantScopeEscape_CollapsesToUnavailableAndLeaksNothing()
    {
        StubConversationClient client = new()
        {
            DetailResult = VisibleSuccess(Details(
                projectId: new ConversationProjectId("project-other-tenant"),
                label: "Other Tenant Label",
                tenant: new TenantId("tenant-b"))),
        };
        ConversationsProjectConversationResolutionDirectory directory = new(client);

        ConversationResolutionMetadata metadata = await directory
            .ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metadata.ReferenceState.ShouldBe(ReferenceState.Unavailable);
        metadata.LinkedProjectId.ShouldBeNull();
        metadata.SafeLabel.ShouldBeNull();
    }

    [Fact]
    public async Task ReadConversationMetadata_ConversationScopeEscape_CollapsesToUnavailable()
    {
        StubConversationClient client = new()
        {
            DetailResult = VisibleSuccess(Details(
                projectId: new ConversationProjectId("project-a"),
                label: "Project Alpha",
                conversation: new ConversationId("conversation-999"))),
        };
        ConversationsProjectConversationResolutionDirectory directory = new(client);

        ConversationResolutionMetadata metadata = await directory
            .ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metadata.ReferenceState.ShouldBe(ReferenceState.Unavailable);
        metadata.LinkedProjectId.ShouldBeNull();
    }

    [Fact]
    public async Task ReadConversationMetadata_SuccessfulButHiddenBody_FailsClosedFromResultFreshness()
    {
        StubConversationClient client = new()
        {
            DetailResult = ConversationClientResult<ConversationDetailResult>.Success(
                ConversationDetailResult.Hidden(SchemaVersion.Current),
                HttpStatusCode.OK),
        };
        ConversationsProjectConversationResolutionDirectory directory = new(client);

        ConversationResolutionMetadata metadata = await directory
            .ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        // Hidden carries ProjectionTrustState.Forbidden → Unauthorized; no body to surface.
        metadata.ReferenceState.ShouldBe(ReferenceState.Unauthorized);
        metadata.LinkedProjectId.ShouldBeNull();
        metadata.SafeLabel.ShouldBeNull();
    }

    [Fact]
    public async Task ReadConversationMetadata_UpstreamThrows_FailsClosedToUnavailable()
    {
        StubConversationClient client = new() { Exception = new HttpRequestException("upstream unavailable") };
        ConversationsProjectConversationResolutionDirectory directory = new(client);

        ConversationResolutionMetadata metadata = await directory
            .ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metadata.ReferenceState.ShouldBe(ReferenceState.Unavailable);
        metadata.LinkedProjectId.ShouldBeNull();
    }

    [Fact]
    public async Task ReadConversationMetadata_CancellationPropagates()
    {
        StubConversationClient client = new() { Exception = new OperationCanceledException() };
        ConversationsProjectConversationResolutionDirectory directory = new(client);

        await Should.ThrowAsync<OperationCanceledException>(
            () => directory.ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken))
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task UnavailableDirectory_AlwaysFailsClosedToUnavailable()
    {
        UnavailableProjectConversationResolutionDirectory directory = new();

        ConversationResolutionMetadata metadata = await directory
            .ReadConversationMetadataAsync(Conversation, Tenant, Caller, CorrelationId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        metadata.ConversationId.ShouldBe(Conversation.Value);
        metadata.ReferenceState.ShouldBe(ReferenceState.Unavailable);
        metadata.LinkedProjectId.ShouldBeNull();
        metadata.SafeLabel.ShouldBeNull();
    }

    private static ConversationClientResult<ConversationDetailResult> VisibleSuccess(ConversationDetailsV1 details)
        => ConversationClientResult<ConversationDetailResult>.Success(
            ConversationDetailResult.Visible(SchemaVersion.Current, details, "Visible."),
            HttpStatusCode.OK);

    private static ConversationClientResult<ConversationDetailResult> Failure(HttpStatusCode statusCode)
        => ConversationClientResult<ConversationDetailResult>.Failure(
            new ConversationErrorResult(
                [
                    new ConversationError(
                        SchemaVersion.Current,
                        ConversationErrorCode.CommandValidationFailed,
                        ConversationErrorCategory.Validation,
                        IsRetryable: false,
                        CorrelationId: CorrelationId),
                ]),
            statusCode);

    private static ConversationDetailsV1 Details(
        ConversationProjectId? projectId,
        string? label,
        ProjectionTrustState? trust = null,
        ProjectReferenceHydrationV1? hydration = null,
        TenantId? tenant = null,
        ConversationId? conversation = null)
        => new(
            SchemaVersion.Current,
            tenant ?? Tenant,
            conversation ?? Conversation,
            Freshness(trust ?? ProjectionTrustState.Current),
            "Open",
            Label: label,
            ProjectId: projectId,
            ProjectHydration: hydration);

    private static ProjectionFreshnessV1 Freshness(ProjectionTrustState trust)
        => new(
            SchemaVersion.Current,
            "pos:1",
            1,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            TimeSpan.Zero,
            IsStale: trust != ProjectionTrustState.Current,
            trust,
            ReasonFor(trust));

    private static ProjectionFreshnessReasonCode ReasonFor(ProjectionTrustState trust)
    {
        if (trust == ProjectionTrustState.Stale)
        {
            return ProjectionFreshnessReasonCode.StaleThresholdExceeded;
        }

        if (trust == ProjectionTrustState.Rebuilding)
        {
            return ProjectionFreshnessReasonCode.Rebuilding;
        }

        if (trust == ProjectionTrustState.Unavailable)
        {
            return ProjectionFreshnessReasonCode.Unavailable;
        }

        if (trust == ProjectionTrustState.Forbidden)
        {
            return ProjectionFreshnessReasonCode.Forbidden;
        }

        return trust == ProjectionTrustState.Redacted
            ? ProjectionFreshnessReasonCode.Redacted
            : ProjectionFreshnessReasonCode.Current;
    }

    private static ProjectionTrustState TrustState(string name)
    {
        if (name == "Stale")
        {
            return ProjectionTrustState.Stale;
        }

        if (name == "Forbidden")
        {
            return ProjectionTrustState.Forbidden;
        }

        if (name == "Redacted")
        {
            return ProjectionTrustState.Redacted;
        }

        if (name == "Rebuilding")
        {
            return ProjectionTrustState.Rebuilding;
        }

        return name == "Unavailable" ? ProjectionTrustState.Unavailable : ProjectionTrustState.Current;
    }

    private sealed class StubConversationClient : IConversationClient
    {
        public ConversationClientResult<ConversationDetailResult>? DetailResult { get; init; }

        public Exception? Exception { get; init; }

        public Task<ConversationClientResult<ConversationCreatedResult>> CreateConversationAsync(
            CreateConversationCommand command,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ConversationClientResult<ConversationCommandAcceptedResult>> AppendMessageAsync(
            AppendMessageCommand command,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ConversationClientResult<ConversationCommandAcceptedResult>> ReassignConversationProjectAsync(
            ReassignConversationProjectCommand command,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ConversationClientResult<ConversationDetailResult>> GetConversationAsync(
            GetConversationQuery query,
            CancellationToken cancellationToken = default)
        {
            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(DetailResult ?? throw new InvalidOperationException("No detail result configured."));
        }

        public Task<ConversationClientResult<ConversationListResult>> ListConversationsAsync(
            ListConversationsQuery query,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
