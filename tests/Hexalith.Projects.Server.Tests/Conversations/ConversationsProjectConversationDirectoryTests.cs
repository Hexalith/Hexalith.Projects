// <copyright file="ConversationsProjectConversationDirectoryTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Conversations;

using System.Net;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
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

/// <summary>Pure ACL tests for the Projects-owned directory over the Conversations typed client.</summary>
public sealed class ConversationsProjectConversationDirectoryTests
{
    private static readonly ProjectId Project = new("project-001");
    private static readonly ConversationTenantId Tenant = new("tenant-a");
    private static readonly CallerPrincipalId Caller = new("caller-a");

    [Fact]
    public async Task ListForProjectAsyncShouldBuildProjectScopedQueryAndForwardPaging()
    {
        ConversationListResult result = new(
            SchemaVersion.Current,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            [],
            new ConversationPageMetadata(0),
            "No accessible matches.");
        CapturingConversationClient client = new(ConversationClientResult<ConversationListResult>.Success(result, HttpStatusCode.OK));
        ConversationsProjectConversationDirectory directory = new(client);

        await directory.ListForProjectAsync(
            Project,
            Tenant,
            Caller,
            new PageRequest(7, "cursor-001"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        client.CapturedQuery.ShouldNotBeNull();
        client.CapturedQuery!.TenantId.ShouldBe(Tenant);
        client.CapturedQuery.CallerPrincipalId.ShouldBe(Caller.Value);
        client.CapturedQuery.Filter.ProjectId.ShouldNotBeNull();
        client.CapturedQuery.Filter.ProjectId!.Value.ShouldBe(Project.Value);
        client.CapturedQuery.Page.PageSize.ShouldBe(7);
        client.CapturedQuery.Page.ContinuationCursor.ShouldBe("cursor-001");
    }

    [Fact]
    public async Task ListForProjectAsyncShouldCloseEntirePageWhenUpstreamRowsEscapeScope()
    {
        ConversationListResult result = new(
            SchemaVersion.Current,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            [
                ProjectConversationTranslatorTests.Summary(
                    "tenant-a",
                    "project-001",
                    "conversation-allowed",
                    ProjectionTrustState.Current,
                    ProjectionFreshnessReasonCode.Current),
                ProjectConversationTranslatorTests.Summary(
                    "tenant-b",
                    "project-001",
                    "conversation-tenant-b",
                    ProjectionTrustState.Current,
                    ProjectionFreshnessReasonCode.Current),
                ProjectConversationTranslatorTests.Summary(
                    "tenant-a",
                    "project-other",
                    "conversation-other-project",
                    ProjectionTrustState.Current,
                    ProjectionFreshnessReasonCode.Current),
            ],
            new ConversationPageMetadata(3, "cursor-after-poisoned-page"),
            "Accessible results are complete for the supplied filters.");
        ConversationsProjectConversationDirectory directory = new(new CapturingConversationClient(
            ConversationClientResult<ConversationListResult>.Success(result, HttpStatusCode.OK)));

        ProjectConversationsPage page = await directory
            .ListForProjectAsync(Project, Tenant, Caller, new PageRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        page.Items.ShouldBeEmpty();
        page.Page.ContinuationCursor.ShouldBeNull();
        page.TrustSignal.ShouldBe(ProjectConversationTrustSignal.Unavailable);
    }

    [Fact]
    public async Task ListForProjectAsyncShouldReturnHiddenPageForForbiddenUpstreamResult()
    {
        ConversationsProjectConversationDirectory directory = new(new CapturingConversationClient(
            ConversationClientResult<ConversationListResult>.Success(
                ConversationListResult.Hidden(SchemaVersion.Current),
                HttpStatusCode.OK)));

        ProjectConversationsPage page = await directory
            .ListForProjectAsync(Project, Tenant, Caller, new PageRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        page.Items.ShouldBeEmpty();
        page.TrustSignal.ShouldBe(ProjectConversationTrustSignal.Forbidden);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ProjectConversationTrustSignal.Forbidden)]
    [InlineData(HttpStatusCode.Forbidden, ProjectConversationTrustSignal.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, ProjectConversationTrustSignal.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError, ProjectConversationTrustSignal.Unavailable)]
    public async Task ListForProjectAsyncShouldFailClosedForUnsuccessfulUpstreamResponses(
        HttpStatusCode statusCode,
        ProjectConversationTrustSignal expectedSignal)
    {
        ConversationsProjectConversationDirectory directory = new(new CapturingConversationClient(
            ConversationClientResult<ConversationListResult>.Failure(SafeError(), statusCode)));

        ProjectConversationsPage page = await directory
            .ListForProjectAsync(Project, Tenant, Caller, new PageRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        page.Items.ShouldBeEmpty();
        page.TrustSignal.ShouldBe(expectedSignal);
    }

    [Fact]
    public async Task ListForProjectAsyncShouldReturnUnavailablePageWhenUpstreamThrows()
    {
        ConversationsProjectConversationDirectory directory = new(new CapturingConversationClient(
            new InvalidOperationException("Synthetic upstream outage.")));

        ProjectConversationsPage page = await directory
            .ListForProjectAsync(Project, Tenant, Caller, new PageRequest(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        page.Items.ShouldBeEmpty();
        page.TrustSignal.ShouldBe(ProjectConversationTrustSignal.Unavailable);
    }

    private static ConversationErrorResult SafeError()
        => new(
            [
                new ConversationError(
                    SchemaVersion.Current,
                    ConversationErrorCode.AggregateNotFound,
                    ConversationErrorCategory.Hidden,
                    IsRetryable: false,
                    CorrelationId: "corr-a"),
            ]);

    private sealed class CapturingConversationClient : IConversationClient
    {
        private readonly Exception? _exception;
        private readonly ConversationClientResult<ConversationListResult>? _listResult;

        public CapturingConversationClient(ConversationClientResult<ConversationListResult> listResult)
        {
            _listResult = listResult;
        }

        public CapturingConversationClient(Exception exception)
        {
            _exception = exception;
        }

        public ListConversationsQuery? CapturedQuery { get; private set; }

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
            => throw new NotSupportedException();

        public Task<ConversationClientResult<ConversationListResult>> ListConversationsAsync(
            ListConversationsQuery query,
            CancellationToken cancellationToken = default)
        {
            CapturedQuery = query;
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_listResult ?? throw new InvalidOperationException("No list result configured."));
        }
    }
}
