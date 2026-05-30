// <copyright file="ProjectConversationAssignmentDirectoryTests.cs" company="Hexalith">
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
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Conversations;

using Shouldly;

using Xunit;

using ConversationProjectId = Hexalith.Conversations.Contracts.Identifiers.ProjectId;
using ProjectId = Hexalith.Projects.Contracts.Identifiers.ProjectId;

/// <summary>Pure write-ACL tests for Projects conversation assignment orchestration.</summary>
public sealed class ProjectConversationAssignmentDirectoryTests
{
    private static readonly TenantId Tenant = new("tenant-a");
    private static readonly CallerPrincipalId Caller = new("principal-a");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly ProjectId TargetProject = new("project-target");
    private static readonly ProjectId SourceProject = new("project-source");
    private static readonly PartyId ResolvedActor = new("party-resolved-from-server");
    private static readonly ProjectConversationCommandMetadata Metadata = new("corr-a", "task-a", "idem-a");

    [Fact]
    public async Task LinkAsync_UnassignedConversation_DispatchesAssignWithServerDerivedActor()
    {
        CapturingConversationClient client = new()
        {
            DetailResult = Detail(projectId: null),
            ReassignResult = Accepted(),
        };
        CapturingActorPartyResolver resolver = new(ResolvedActor);
        ConversationsProjectConversationAssignmentDirectory directory = new(client, resolver);

        ProjectConversationAssignmentResult result = await directory
            .LinkAsync(TargetProject, Conversation, Tenant, Caller, Metadata, expectedCurrentProjectId: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Accepted);
        client.CapturedReassign.ShouldNotBeNull();
        client.CapturedReassign!.Metadata.TenantId.ShouldBe(Tenant);
        client.CapturedReassign.Metadata.ActorPartyId.ShouldBe(ResolvedActor);
        client.CapturedReassign.Metadata.CorrelationId.ShouldBe("corr-a");
        client.CapturedReassign.Metadata.CausationId.ShouldBe("task-a");
        client.CapturedReassign.Metadata.IdempotencyKey.ShouldBe("idem-a");
        client.CapturedReassign.Target.Operation.ShouldBe(ConversationProjectAssignmentOperation.Assign);
        client.CapturedReassign.Target.ProjectId.ShouldBe(new ConversationProjectId(TargetProject.Value));
        client.CapturedReassign.ExpectedCurrentProjectId.ShouldBeNull();
        resolver.CapturedTenant.ShouldBe(Tenant);
        resolver.CapturedPrincipal.ShouldBe(Caller);
    }

    [Fact]
    public async Task LinkAsync_ConversationAlreadyAssignedElsewhere_RejectsWithoutDispatchingMove()
    {
        CapturingConversationClient client = new()
        {
            DetailResult = Detail(new ConversationProjectId(SourceProject.Value)),
            ReassignResult = Accepted(),
        };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new CapturingActorPartyResolver(ResolvedActor));

        ProjectConversationAssignmentResult result = await directory
            .LinkAsync(TargetProject, Conversation, Tenant, Caller, Metadata, expectedCurrentProjectId: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.ValidationFailed);
        client.CapturedReassign.ShouldBeNull();
    }

    [Fact]
    public async Task MoveAsync_DispatchesAssignWithExpectedSourceProjectGuard()
    {
        CapturingConversationClient client = new() { ReassignResult = Accepted() };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new CapturingActorPartyResolver(ResolvedActor));

        ProjectConversationAssignmentResult result = await directory
            .MoveAsync(TargetProject, Conversation, SourceProject, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Accepted);
        client.CapturedReassign.ShouldNotBeNull();
        client.CapturedReassign!.Target.Operation.ShouldBe(ConversationProjectAssignmentOperation.Assign);
        client.CapturedReassign.Target.ProjectId.ShouldBe(new ConversationProjectId(TargetProject.Value));
        client.CapturedReassign.ExpectedCurrentProjectId.ShouldBe(new ConversationProjectId(SourceProject.Value));
    }

    [Fact]
    public async Task UnlinkAsync_DispatchesClearWithExpectedTargetProjectGuard()
    {
        CapturingConversationClient client = new() { ReassignResult = Accepted() };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new CapturingActorPartyResolver(ResolvedActor));

        ProjectConversationAssignmentResult result = await directory
            .UnlinkAsync(TargetProject, Conversation, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Accepted);
        client.CapturedReassign.ShouldNotBeNull();
        client.CapturedReassign!.Target.Operation.ShouldBe(ConversationProjectAssignmentOperation.Clear);
        client.CapturedReassign.Target.ProjectId.ShouldBeNull();
        client.CapturedReassign.ExpectedCurrentProjectId.ShouldBe(new ConversationProjectId(TargetProject.Value));
    }

    [Fact]
    public async Task MoveAsync_ExpectedCurrentMismatchFromConversations_ReturnsConflict()
    {
        CapturingConversationClient client = new() { ReassignResult = Failure(HttpStatusCode.Conflict) };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new CapturingActorPartyResolver(ResolvedActor));

        ProjectConversationAssignmentResult result = await directory
            .MoveAsync(TargetProject, Conversation, SourceProject, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Conflict);
        client.CapturedReassign.ShouldNotBeNull();
        client.CapturedReassign!.ExpectedCurrentProjectId.ShouldBe(new ConversationProjectId(SourceProject.Value));
    }

    [Fact]
    public async Task ConfirmResolutionAssignmentAsync_AlreadyAtTarget_ReturnsAcceptedWithoutDuplicateDispatch()
    {
        CapturingConversationClient client = new()
        {
            DetailResult = Detail(new ConversationProjectId(TargetProject.Value)),
            ReassignResult = Accepted(),
        };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new CapturingActorPartyResolver(ResolvedActor));

        ProjectConversationAssignmentResult result = await directory
            .ConfirmResolutionAssignmentAsync(TargetProject, Conversation, SourceProject, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Accepted);
        client.CapturedReassign.ShouldBeNull();
    }

    [Fact]
    public async Task ConfirmResolutionAssignmentAsync_CurrentIsExpectedSource_DispatchesMoveGuard()
    {
        CapturingConversationClient client = new()
        {
            DetailResult = Detail(new ConversationProjectId(SourceProject.Value)),
            ReassignResult = Accepted(),
        };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new CapturingActorPartyResolver(ResolvedActor));

        ProjectConversationAssignmentResult result = await directory
            .ConfirmResolutionAssignmentAsync(TargetProject, Conversation, SourceProject, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Accepted);
        client.CapturedReassign.ShouldNotBeNull();
        client.CapturedReassign!.Target.ProjectId.ShouldBe(new ConversationProjectId(TargetProject.Value));
        client.CapturedReassign.ExpectedCurrentProjectId.ShouldBe(new ConversationProjectId(SourceProject.Value));
    }

    [Fact]
    public async Task ConfirmResolutionAssignmentAsync_UnassignedWithoutSource_DispatchesLink()
    {
        CapturingConversationClient client = new()
        {
            DetailResult = Detail(projectId: null),
            ReassignResult = Accepted(),
        };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new CapturingActorPartyResolver(ResolvedActor));

        ProjectConversationAssignmentResult result = await directory
            .ConfirmResolutionAssignmentAsync(TargetProject, Conversation, expectedSourceProjectId: null, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Accepted);
        client.CapturedReassign.ShouldNotBeNull();
        client.CapturedReassign!.ExpectedCurrentProjectId.ShouldBeNull();
        client.CapturedReassign.Target.ProjectId.ShouldBe(new ConversationProjectId(TargetProject.Value));
    }

    [Fact]
    public async Task ConfirmResolutionAssignmentAsync_UnexpectedThirdProject_ReturnsConflictWithoutDispatch()
    {
        CapturingConversationClient client = new()
        {
            DetailResult = Detail(new ConversationProjectId("project-third")),
            ReassignResult = Accepted(),
        };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new CapturingActorPartyResolver(ResolvedActor));

        ProjectConversationAssignmentResult result = await directory
            .ConfirmResolutionAssignmentAsync(TargetProject, Conversation, SourceProject, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Conflict);
        client.CapturedReassign.ShouldBeNull();
    }

    [Fact]
    public async Task MoveAsync_UpstreamAcceptedWithDifferentIdempotencyKey_ReturnsUnavailable()
    {
        CapturingConversationClient client = new() { ReassignResult = Accepted(idempotencyKey: "idem-other") };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new CapturingActorPartyResolver(ResolvedActor));

        ProjectConversationAssignmentResult result = await directory
            .MoveAsync(TargetProject, Conversation, SourceProject, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Unavailable);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ProjectConversationAssignmentOutcome.ValidationFailed)]
    [InlineData(HttpStatusCode.Unauthorized, ProjectConversationAssignmentOutcome.Denied)]
    [InlineData(HttpStatusCode.Forbidden, ProjectConversationAssignmentOutcome.Denied)]
    [InlineData(HttpStatusCode.NotFound, ProjectConversationAssignmentOutcome.Denied)]
    [InlineData(HttpStatusCode.InternalServerError, ProjectConversationAssignmentOutcome.Unavailable)]
    public async Task MoveAsync_UpstreamReassignmentFailures_MapFailClosed(
        HttpStatusCode statusCode,
        ProjectConversationAssignmentOutcome expectedOutcome)
    {
        CapturingConversationClient client = new() { ReassignResult = Failure(statusCode) };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new CapturingActorPartyResolver(ResolvedActor));

        ProjectConversationAssignmentResult result = await directory
            .MoveAsync(TargetProject, Conversation, SourceProject, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(expectedOutcome);
    }

    [Fact]
    public async Task MoveAsync_UpstreamReassignmentThrows_ReturnsUnavailable()
    {
        CapturingConversationClient client = new() { ReassignException = new HttpRequestException("upstream unavailable") };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new CapturingActorPartyResolver(ResolvedActor));

        ProjectConversationAssignmentResult result = await directory
            .MoveAsync(TargetProject, Conversation, SourceProject, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Unavailable);
    }

    [Fact]
    public async Task MoveAsync_ActorPartyResolutionFailure_ReturnsUnavailableWithoutDispatch()
    {
        CapturingConversationClient client = new() { ReassignResult = Accepted() };
        ConversationsProjectConversationAssignmentDirectory directory = new(client, new ThrowingActorPartyResolver());

        ProjectConversationAssignmentResult result = await directory
            .MoveAsync(TargetProject, Conversation, SourceProject, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Outcome.ShouldBe(ProjectConversationAssignmentOutcome.Unavailable);
        client.CapturedReassign.ShouldBeNull();
    }

    [Fact]
    public async Task DeterministicActorPartyResolver_DerivesStablePartyIdFromTenantAndPrincipal()
    {
        DeterministicActorPartyResolver resolver = new();

        PartyId first = await resolver.ResolveActorPartyAsync(Tenant, Caller, TestContext.Current.CancellationToken).ConfigureAwait(true);
        PartyId second = await resolver.ResolveActorPartyAsync(Tenant, Caller, TestContext.Current.CancellationToken).ConfigureAwait(true);
        PartyId otherTenant = await resolver
            .ResolveActorPartyAsync(new TenantId("tenant-b"), Caller, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.ShouldBe(second);
        first.ShouldNotBe(otherTenant);
        first.Value.ShouldStartWith("projects-actor-");
    }

    [Fact]
    public async Task ReassignmentAndClearKeepPatternAReadsOwnedByConversationsProjection()
    {
        MutableConversationClient client = new();
        ConversationsProjectConversationAssignmentDirectory assignmentDirectory = new(client, new CapturingActorPartyResolver(ResolvedActor));
        ConversationsProjectConversationDirectory readDirectory = new(client);

        await assignmentDirectory
            .LinkAsync(TargetProject, Conversation, Tenant, Caller, Metadata, expectedCurrentProjectId: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        ProjectConversationsPage targetAfterLink = await readDirectory
            .ListForProjectAsync(TargetProject, Tenant, Caller, new PageRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        ProjectConversationsPage sourceAfterLink = await readDirectory
            .ListForProjectAsync(SourceProject, Tenant, Caller, new PageRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        targetAfterLink.Items.Single().ConversationId.ShouldBe(Conversation);
        sourceAfterLink.Items.ShouldBeEmpty();

        await assignmentDirectory
            .MoveAsync(SourceProject, Conversation, TargetProject, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        ProjectConversationsPage targetAfterMove = await readDirectory
            .ListForProjectAsync(TargetProject, Tenant, Caller, new PageRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        ProjectConversationsPage sourceAfterMove = await readDirectory
            .ListForProjectAsync(SourceProject, Tenant, Caller, new PageRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        targetAfterMove.Items.ShouldBeEmpty();
        sourceAfterMove.Items.Single().ConversationId.ShouldBe(Conversation);

        await assignmentDirectory
            .UnlinkAsync(SourceProject, Conversation, Tenant, Caller, Metadata, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        ProjectConversationsPage sourceAfterClear = await readDirectory
            .ListForProjectAsync(SourceProject, Tenant, Caller, new PageRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        sourceAfterClear.Items.ShouldBeEmpty();
    }

    private static ConversationClientResult<ConversationCommandAcceptedResult> Accepted(string idempotencyKey = "idem-a")
        => ConversationClientResult<ConversationCommandAcceptedResult>.Success(
            new ConversationCommandAcceptedResult(
                SchemaVersion.Current,
                Tenant,
                Conversation,
                ConversationCommandType.ReassignConversationProjectCommand,
                "upstream-corr",
                idempotencyKey,
                new ReadModelVisibility(ProjectionTrustState.Current)),
            HttpStatusCode.Accepted);

    private static ConversationClientResult<ConversationCommandAcceptedResult> Failure(HttpStatusCode statusCode)
        => ConversationClientResult<ConversationCommandAcceptedResult>.Failure(
            new ConversationErrorResult(
                [
                    new ConversationError(
                        SchemaVersion.Current,
                        ConversationErrorCode.CommandValidationFailed,
                        ConversationErrorCategory.Validation,
                        IsRetryable: false,
                        CorrelationId: "corr-a"),
                ]),
            statusCode);

    private static ConversationClientResult<ConversationDetailResult> Detail(ConversationProjectId? projectId)
        => ConversationClientResult<ConversationDetailResult>.Success(
            ConversationDetailResult.Visible(
                SchemaVersion.Current,
                new ConversationDetailsV1(
                    SchemaVersion.Current,
                    Tenant,
                    Conversation,
                    Freshness(),
                    "Open",
                    ProjectId: projectId),
                "Visible."),
            HttpStatusCode.OK);

    private static ProjectionFreshnessV1 Freshness()
        => new(
            SchemaVersion.Current,
            "pos:1",
            1,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            TimeSpan.Zero,
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);

    private sealed class CapturingActorPartyResolver(PartyId result) : IActorPartyResolver
    {
        public TenantId? CapturedTenant { get; private set; }

        public CallerPrincipalId? CapturedPrincipal { get; private set; }

        public ValueTask<PartyId> ResolveActorPartyAsync(
            TenantId tenantId,
            CallerPrincipalId principalId,
            CancellationToken cancellationToken = default)
        {
            CapturedTenant = tenantId;
            CapturedPrincipal = principalId;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ThrowingActorPartyResolver : IActorPartyResolver
    {
        public ValueTask<PartyId> ResolveActorPartyAsync(
            TenantId tenantId,
            CallerPrincipalId principalId,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("resolver unavailable");
    }

    private sealed class CapturingConversationClient : IConversationClient
    {
        public ConversationClientResult<ConversationDetailResult>? DetailResult { get; init; }

        public ConversationClientResult<ConversationCommandAcceptedResult>? ReassignResult { get; init; }

        public Exception? ReassignException { get; init; }

        public ReassignConversationProjectCommand? CapturedReassign { get; private set; }

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
        {
            CapturedReassign = command;
            if (ReassignException is not null)
            {
                throw ReassignException;
            }

            return Task.FromResult(ReassignResult ?? throw new InvalidOperationException("No reassignment result configured."));
        }

        public Task<ConversationClientResult<ConversationDetailResult>> GetConversationAsync(
            GetConversationQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(DetailResult ?? throw new InvalidOperationException("No detail result configured."));

        public Task<ConversationClientResult<ConversationListResult>> ListConversationsAsync(
            ListConversationsQuery query,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class MutableConversationClient : IConversationClient
    {
        private ConversationProjectId? _currentProjectId;

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
        {
            if ((_currentProjectId is null) != (command.ExpectedCurrentProjectId is null)
                || (_currentProjectId is not null
                    && command.ExpectedCurrentProjectId is not null
                    && _currentProjectId.Value != command.ExpectedCurrentProjectId.Value))
            {
                return Task.FromResult(Failure(HttpStatusCode.Conflict));
            }

            _currentProjectId = command.Target.Operation == ConversationProjectAssignmentOperation.Assign
                ? command.Target.ProjectId
                : null;

            return Task.FromResult(ConversationClientResult<ConversationCommandAcceptedResult>.Success(
                new ConversationCommandAcceptedResult(
                    SchemaVersion.Current,
                    command.Metadata.TenantId,
                    command.ConversationId,
                    ConversationCommandType.ReassignConversationProjectCommand,
                    command.Metadata.CorrelationId ?? "corr-a",
                    command.Metadata.IdempotencyKey,
                    new ReadModelVisibility(ProjectionTrustState.Current)),
                HttpStatusCode.Accepted));
        }

        public Task<ConversationClientResult<ConversationDetailResult>> GetConversationAsync(
            GetConversationQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ConversationClientResult<ConversationDetailResult>.Success(
                ConversationDetailResult.Visible(
                    SchemaVersion.Current,
                    new ConversationDetailsV1(
                        SchemaVersion.Current,
                        query.TenantId,
                        query.ConversationId,
                        Freshness(),
                        "Open",
                        ProjectId: _currentProjectId),
                    "Visible."),
                HttpStatusCode.OK));

        public Task<ConversationClientResult<ConversationListResult>> ListConversationsAsync(
            ListConversationsQuery query,
            CancellationToken cancellationToken = default)
        {
            ConversationProjectId? requestedProjectId = query.Filter.ProjectId;
            IReadOnlyList<ConversationSummaryV1> summaries = _currentProjectId is not null
                && requestedProjectId is not null
                && _currentProjectId.Value == requestedProjectId.Value
                    ? [
                        ProjectConversationTranslatorTests.Summary(
                            query.TenantId.Value,
                            requestedProjectId.Value,
                            Conversation.Value,
                            ProjectionTrustState.Current,
                            ProjectionFreshnessReasonCode.Current),
                    ]
                    : [];

            return Task.FromResult(ConversationClientResult<ConversationListResult>.Success(
                new ConversationListResult(
                    SchemaVersion.Current,
                    ProjectionTrustState.Current,
                    ProjectionFreshnessReasonCode.Current,
                    summaries,
                    new ConversationPageMetadata(summaries.Count),
                    "Accessible results are complete for the supplied filters."),
                HttpStatusCode.OK));
        }
    }
}
