// <copyright file="ProjectConversationAssignmentEndpointTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Conversations;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Xunit;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ProjectId = Hexalith.Projects.Contracts.Identifiers.ProjectId;

/// <summary>Tier-2 endpoint tests for Projects conversation link/move/unlink mutations.</summary>
public sealed class ProjectConversationAssignmentEndpointTests
{
    private const string TargetProjectId = "project-target-001";
    private const string SourceProjectId = "project-source-001";
    private const string ConversationIdValue = "conversation-001";

    [Fact]
    public async Task LinkConversation_Authorized_Returns202AndCallsWriteAcl()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("upstream-corr"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client
                .SendAsync(LinkRequest(), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("correlationId").GetString().ShouldBe("upstream-corr");
            directory.Calls.Single().Operation.ShouldBe("link");
            directory.Calls.Single().TenantId.ShouldBe(new TenantId("tenant-a"));
            directory.Calls.Single().Caller.ShouldBe(new CallerPrincipalId("principal-a"));
            directory.Calls.Single().Metadata.IdempotencyKey.ShouldBe("idem-link");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkConversation_BodyActorPartyId_IsRejectedAndNeverReachesWriteAcl()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("upstream-corr"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = LinkRequest(new
            {
                requestSchemaVersion = "v1",
                operation = "link",
                projectId = TargetProjectId,
                conversationId = ConversationIdValue,
                actorPartyId = "party:client-supplied",
            });

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkConversation_RouteBodyMismatch_Returns400BeforeWriteAcl()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("upstream-corr"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = LinkRequest(new
            {
                requestSchemaVersion = "v1",
                operation = "link",
                projectId = TargetProjectId,
                conversationId = "conversation-other",
            });

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkConversation_MissingIdempotencyKey_Returns400BeforeWriteAcl()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("upstream-corr"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = LinkRequest();
            request.Headers.Remove("Idempotency-Key");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData("X-Tenant-Id", "tenant-b")]
    [InlineData("X-Principal-Id", "principal-b")]
    public async Task LinkConversation_ClientControlledAuthorityMismatch_FailsClosedBeforeWriteAcl(
        string headerName,
        string headerValue)
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("upstream-corr"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = LinkRequest();
            request.Headers.Add(headerName, headerValue);

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            directory.Calls.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task MoveConversation_SourceProjectHidden_FailsClosedBeforeWriteAcl()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("upstream-corr"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client
                .SendAsync(MoveRequest(), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            directory.Calls.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task MoveConversation_RouteBodyMismatch_Returns400BeforeWriteAcl()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("upstream-corr"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel detail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            detail.Project("tenant-a", Created(TargetProjectId));
            detail.Project("tenant-a", Created(SourceProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = MoveRequest(new
            {
                requestSchemaVersion = "v1",
                operation = "move",
                projectId = "project-other",
                conversationId = ConversationIdValue,
                sourceProjectId = SourceProjectId,
                confirmed = true,
            });

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task MoveConversation_MissingConfirmation_Returns400BeforeWriteAcl()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("upstream-corr"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = MoveRequest(new
            {
                requestSchemaVersion = "v1",
                operation = "move",
                projectId = TargetProjectId,
                conversationId = ConversationIdValue,
                sourceProjectId = SourceProjectId,
            });

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task MoveConversation_AuthorizedSourceAndTarget_CallsWriteAcl()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("upstream-corr"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel detail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            detail.Project("tenant-a", Created(TargetProjectId));
            detail.Project("tenant-a", Created(SourceProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client
                .SendAsync(MoveRequest(), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            AssignmentCall call = directory.Calls.Single();
            call.Operation.ShouldBe("move");
            call.ProjectId.ShouldBe(new ProjectId(TargetProjectId));
            call.SourceProjectId.ShouldBe(new ProjectId(SourceProjectId));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task UnlinkConversation_Authorized_CallsWriteAcl()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("upstream-corr"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client
                .SendAsync(UnlinkRequest(), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            AssignmentCall call = directory.Calls.Single();
            call.Operation.ShouldBe("unlink");
            call.ProjectId.ShouldBe(new ProjectId(TargetProjectId));
            call.ConversationId.ShouldBe(new ConversationId(ConversationIdValue));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_Authorized_AssignsThenSubmitsCommand()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel detail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            detail.Project("tenant-a", Created(TargetProjectId));
            detail.Project("tenant-a", Created(SourceProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client
                .SendAsync(ConfirmRequest(), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            AssignmentCall assignment = directory.Calls.Single();
            assignment.Operation.ShouldBe("confirm");
            assignment.SourceProjectId.ShouldBe(new ProjectId(SourceProjectId));
            ConfirmProjectResolution command = submitter.ResolutionConfirmed.Single();
            command.ProjectId.ShouldBe(new ProjectId(TargetProjectId));
            command.ConversationId.ShouldBe(ConversationIdValue);
            command.SourceProjectId.ShouldBe(new ProjectId(SourceProjectId));
            command.ActorPrincipalId.ShouldBe("principal-a");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_AuthorizedWithoutSource_AssignsThenSubmitsCommand()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ConfirmRequest(ConfirmBody(
                candidateProjectIds: [TargetProjectId, "project-other-001"],
                sourceProjectId: null));

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            AssignmentCall assignment = directory.Calls.Single();
            assignment.Operation.ShouldBe("confirm");
            assignment.SourceProjectId.ShouldBeNull();
            submitter.ResolutionConfirmed.Single().SourceProjectId.ShouldBeNull();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_MissingIdempotencyKey_Returns400BeforeAssignment()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ConfirmRequest();
            request.Headers.Remove("Idempotency-Key");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
            submitter.ResolutionConfirmed.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_RouteBodyMismatch_Returns400BeforeAssignment()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ConfirmRequest(ConfirmBody(projectId: "project-other-001"));

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
            submitter.ResolutionConfirmed.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_UnknownBodyMember_Returns400BeforeAssignment()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            Dictionary<string, object?> body = ConfirmBody();
            body["actorPrincipalId"] = "client-controlled";
            using HttpRequestMessage request = ConfirmRequest(body);

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
            submitter.ResolutionConfirmed.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_MalformedJson_Returns400BeforeAssignment()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/projects/{TargetProjectId}/conversations/{ConversationIdValue}/resolution/confirm")
            {
                Content = new StringContent("{\"requestSchemaVersion\":\"v1\",", System.Text.Encoding.UTF8, "application/json"),
            };
            AddMutationHeaders(request, "idem-confirm");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
            submitter.ResolutionConfirmed.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_DuplicateCandidates_Returns400BeforeAssignment()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ConfirmRequest(new
            {
                requestSchemaVersion = "v1",
                operation = "confirm",
                projectId = TargetProjectId,
                conversationId = ConversationIdValue,
                resolutionResult = "MultipleCandidates",
                confirmed = true,
                candidateProjectIds = new[] { TargetProjectId, TargetProjectId },
            });

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
            submitter.ResolutionConfirmed.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData("SingleCandidate", true, new[] { TargetProjectId, SourceProjectId })]
    [InlineData("MultipleCandidates", false, new[] { TargetProjectId, SourceProjectId })]
    [InlineData("MultipleCandidates", true, new[] { SourceProjectId, "project-other-001" })]
    [InlineData("MultipleCandidates", true, new[] { TargetProjectId })]
    public async Task ConfirmProjectResolution_InvalidConfirmationEvidence_Returns400BeforeAssignment(
        string resolutionResult,
        bool confirmed,
        string[] candidateProjectIds)
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ConfirmRequest(ConfirmBody(
                resolutionResult: resolutionResult,
                confirmed: confirmed,
                candidateProjectIds: candidateProjectIds));

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
            submitter.ResolutionConfirmed.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_SourceEqualsTarget_Returns400BeforeAssignment()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ConfirmRequest(ConfirmBody(sourceProjectId: TargetProjectId));

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
            submitter.ResolutionConfirmed.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_SourceProjectHidden_Returns404BeforeAssignment()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client.SendAsync(ConfirmRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            directory.Calls.ShouldBeEmpty();
            submitter.ResolutionConfirmed.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_ArchivedTarget_Returns404BeforeAssignment()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel detail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            detail.Project("tenant-a", Archived(TargetProjectId));
            detail.Project("tenant-a", Created(SourceProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client.SendAsync(ConfirmRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            directory.Calls.ShouldBeEmpty();
            submitter.ResolutionConfirmed.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_ArchivedSource_Returns404BeforeAssignment()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel detail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            detail.Project("tenant-a", Created(TargetProjectId));
            detail.Project("tenant-a", Archived(SourceProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client.SendAsync(ConfirmRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            directory.Calls.ShouldBeEmpty();
            submitter.ResolutionConfirmed.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData(ProjectConversationAssignmentOutcome.Conflict, HttpStatusCode.Conflict)]
    [InlineData(ProjectConversationAssignmentOutcome.Unavailable, HttpStatusCode.ServiceUnavailable)]
    [InlineData(ProjectConversationAssignmentOutcome.Denied, HttpStatusCode.NotFound)]
    [InlineData(ProjectConversationAssignmentOutcome.ValidationFailed, HttpStatusCode.BadRequest)]
    public async Task ConfirmProjectResolution_AssignmentFailure_DoesNotSubmitProjectsCommand(
        ProjectConversationAssignmentOutcome outcome,
        HttpStatusCode expectedStatusCode)
    {
        CapturingAssignmentDirectory directory = new(new ProjectConversationAssignmentResult(outcome, "assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel detail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            detail.Project("tenant-a", Created(TargetProjectId));
            detail.Project("tenant-a", Created(SourceProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client.SendAsync(ConfirmRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(expectedStatusCode);
            directory.Calls.Single().Operation.ShouldBe("confirm");
            submitter.ResolutionConfirmed.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData(ProjectCommandSubmissionOutcome.IdempotentReplay, HttpStatusCode.Accepted)]
    [InlineData(ProjectCommandSubmissionOutcome.IdempotencyConflict, HttpStatusCode.Conflict)]
    [InlineData(ProjectCommandSubmissionOutcome.Unavailable, HttpStatusCode.ServiceUnavailable)]
    [InlineData(ProjectCommandSubmissionOutcome.ValidationFailed, HttpStatusCode.BadRequest)]
    [InlineData(ProjectCommandSubmissionOutcome.Denied, HttpStatusCode.NotFound)]
    public async Task ConfirmProjectResolution_CommandSubmissionOutcome_MapsAfterAssignmentAccepted(
        ProjectCommandSubmissionOutcome outcome,
        HttpStatusCode expectedStatusCode)
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        CapturingProjectCommandSubmitter submitter = new(new ProjectCommandSubmissionResult(outcome, "projects-corr"));
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel detail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            detail.Project("tenant-a", Created(TargetProjectId));
            detail.Project("tenant-a", Created(SourceProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client.SendAsync(ConfirmRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(expectedStatusCode);
            directory.Calls.Single().Operation.ShouldBe("confirm");
            submitter.ResolutionConfirmed.Single().IdempotencyKey.ShouldBe("idem-confirm");
            if (outcome == ProjectCommandSubmissionOutcome.IdempotentReplay)
            {
                using JsonDocument document = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
                document.RootElement.GetProperty("idempotentReplay").GetBoolean().ShouldBeTrue();
            }
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ConfirmProjectResolution_SameIdempotencyKeyDifferentBody_Returns409Conflict()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        IdempotencyTrackingProjectCommandSubmitter submitter = new();
        WebApplication app = await StartAppAsync(directory, submitter).ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel detail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            detail.Project("tenant-a", Created(TargetProjectId));
            detail.Project("tenant-a", Created(SourceProjectId));
            detail.Project("tenant-a", Created("project-other-001"));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            // First request with idempotency key "idem-confirm" and source = SourceProjectId.
            HttpResponseMessage first = await client
                .SendAsync(ConfirmRequest(), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            first.StatusCode.ShouldBe(HttpStatusCode.Accepted);

            // Second request reuses the same idempotency key but a different body (no source project).
            using HttpRequestMessage secondRequest = ConfirmRequest(ConfirmBody(
                candidateProjectIds: [TargetProjectId, "project-other-001"],
                sourceProjectId: null));
            HttpResponseMessage second = await client
                .SendAsync(secondRequest, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            submitter.ResolutionConfirmed.Count.ShouldBe(2);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task UnlinkConversation_RouteBodyMismatch_Returns400BeforeWriteAcl()
    {
        CapturingAssignmentDirectory directory = new(ProjectConversationAssignmentResult.Accepted("upstream-corr"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = UnlinkRequest(new
            {
                requestSchemaVersion = "v1",
                operation = "unlink",
                unlinkIntent = "clear",
                projectId = TargetProjectId,
                conversationId = "conversation-other",
            });

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            directory.Calls.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData(ProjectConversationAssignmentOutcome.Conflict, HttpStatusCode.Conflict)]
    [InlineData(ProjectConversationAssignmentOutcome.Unavailable, HttpStatusCode.ServiceUnavailable)]
    [InlineData(ProjectConversationAssignmentOutcome.Denied, HttpStatusCode.NotFound)]
    public async Task LinkConversation_WriteAclOutcomes_MapToSafeProblemResponses(
        ProjectConversationAssignmentOutcome outcome,
        HttpStatusCode expectedStatusCode)
    {
        CapturingAssignmentDirectory directory = new(new ProjectConversationAssignmentResult(outcome, "corr-a"));
        WebApplication app = await StartAppAsync(directory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", Created(TargetProjectId));
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client.SendAsync(LinkRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(expectedStatusCode);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    private static HttpRequestMessage LinkRequest(object? body = null)
    {
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/projects/{TargetProjectId}/conversations/{ConversationIdValue}/link")
        {
            Content = JsonContent.Create(body ?? new
            {
                requestSchemaVersion = "v1",
                operation = "link",
                projectId = TargetProjectId,
                conversationId = ConversationIdValue,
            }),
        };
        AddMutationHeaders(request, "idem-link");
        return request;
    }

    private static HttpRequestMessage MoveRequest(object? body = null)
    {
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/projects/{TargetProjectId}/conversations/{ConversationIdValue}/move")
        {
            Content = JsonContent.Create(body ?? new
            {
                requestSchemaVersion = "v1",
                operation = "move",
                projectId = TargetProjectId,
                conversationId = ConversationIdValue,
                sourceProjectId = SourceProjectId,
                confirmed = true,
            }),
        };
        AddMutationHeaders(request, "idem-move");
        return request;
    }

    private static HttpRequestMessage UnlinkRequest(object? body = null)
    {
        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/v1/projects/{TargetProjectId}/conversations/{ConversationIdValue}")
        {
            Content = JsonContent.Create(body ?? new
            {
                requestSchemaVersion = "v1",
                operation = "unlink",
                unlinkIntent = "clear",
                projectId = TargetProjectId,
                conversationId = ConversationIdValue,
            }),
        };
        AddMutationHeaders(request, "idem-unlink");
        return request;
    }

    private static HttpRequestMessage ConfirmRequest(object? body = null)
    {
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/projects/{TargetProjectId}/conversations/{ConversationIdValue}/resolution/confirm")
        {
            Content = JsonContent.Create(body ?? ConfirmBody()),
        };
        AddMutationHeaders(request, "idem-confirm");
        return request;
    }

    private static Dictionary<string, object?> ConfirmBody(
        string requestSchemaVersion = "v1",
        string operation = "confirm",
        string projectId = TargetProjectId,
        string conversationId = ConversationIdValue,
        string resolutionResult = "MultipleCandidates",
        bool confirmed = true,
        string[]? candidateProjectIds = null,
        string? sourceProjectId = SourceProjectId)
        => new(StringComparer.Ordinal)
        {
            ["requestSchemaVersion"] = requestSchemaVersion,
            ["operation"] = operation,
            ["projectId"] = projectId,
            ["conversationId"] = conversationId,
            ["resolutionResult"] = resolutionResult,
            ["confirmed"] = confirmed,
            ["candidateProjectIds"] = candidateProjectIds ?? [TargetProjectId, SourceProjectId],
            ["sourceProjectId"] = sourceProjectId,
        };

    private static void AddMutationHeaders(HttpRequestMessage request, string idempotencyKey)
    {
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
    }

    private static ProjectCreated Created(string projectId) => new(
        "tenant-a",
        projectId,
        "Project " + projectId,
        null,
        null,
        ProjectLifecycle.Active,
        "principal-a",
        "corr-a",
        "task-a",
        "idem-a",
        "sha256:project",
        DateTimeOffset.UnixEpoch);

    private static ProjectCreated Archived(string projectId)
        => Created(projectId) with { Lifecycle = ProjectLifecycle.Archived };

    private static async Task<WebApplication> StartAppAsync(
        CapturingAssignmentDirectory directory,
        IProjectCommandSubmitter? submitter = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.Services.AddProjectsServer();
        builder.Services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        builder.Services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        builder.Services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        builder.Services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        builder.Services.RemoveAll<IProjectTenantContextAccessor>();
        builder.Services.AddSingleton<IProjectTenantContextAccessor>(new FixedProjectTenantContextAccessor());
        builder.Services.RemoveAll<IProjectConversationAssignmentDirectory>();
        builder.Services.AddSingleton<IProjectConversationAssignmentDirectory>(directory);
        if (submitter is not null)
        {
            builder.Services.RemoveAll<IProjectCommandSubmitter>();
            builder.Services.AddSingleton<IProjectCommandSubmitter>(submitter);
        }

        WebApplication app = builder.Build();
        await SeedTenantAccessAsync(app.Services).ConfigureAwait(true);
        app.MapProjectsServerEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        return app;
    }

    private static async Task SeedTenantAccessAsync(IServiceProvider services)
    {
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = "tenant-a",
            Enabled = true,
            Watermark = 1,
            ProjectionWatermark = "tenant-a:1",
            LastEventTimestamp = DateTimeOffset.UtcNow,
        };
        projection.Principals["principal-a"] = new ProjectTenantPrincipalEvidence("principal-a", "TenantOwner");

        await services
            .GetRequiredService<IProjectTenantAccessProjectionStore>()
            .SaveAsync(projection, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    private static async Task StopAsync(WebApplication app)
    {
        await app.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await app.DisposeAsync().ConfigureAwait(true);
    }

    private sealed class FixedProjectTenantContextAccessor : IProjectTenantContextAccessor
    {
        public string? AuthoritativeTenantId => "tenant-a";

        public string? PrincipalId => "principal-a";

        public EventStoreClaimTransformEvidence GetClaimTransformEvidence(string actionToken)
            => EventStoreClaimTransformEvidence.Allowed(
                "tenant-a",
                "principal-a",
                    [
                        ProjectAuthorizationGate.LinkConversationAction,
                        ProjectAuthorizationGate.MoveConversationAction,
                        ProjectAuthorizationGate.UnlinkConversationAction,
                        ProjectAuthorizationGate.ConfirmProjectResolutionAction,
                    ]);
    }

    private sealed class CapturingAssignmentDirectory(ProjectConversationAssignmentResult result) : IProjectConversationAssignmentDirectory
    {
        public List<AssignmentCall> Calls { get; } = [];

        public Task<ProjectConversationAssignmentResult> LinkAsync(
            ProjectId projectId,
            ConversationId conversationId,
            TenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            ProjectId? expectedCurrentProjectId = null,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new("link", projectId, conversationId, tenantId, caller, metadata, SourceProjectId: null));
            return Task.FromResult(result);
        }

        public Task<ProjectConversationAssignmentResult> MoveAsync(
            ProjectId targetProjectId,
            ConversationId conversationId,
            ProjectId sourceProjectId,
            TenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new("move", targetProjectId, conversationId, tenantId, caller, metadata, sourceProjectId));
            return Task.FromResult(result);
        }

        public Task<ProjectConversationAssignmentResult> UnlinkAsync(
            ProjectId projectId,
            ConversationId conversationId,
            TenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new("unlink", projectId, conversationId, tenantId, caller, metadata, SourceProjectId: null));
            return Task.FromResult(result);
        }

        public Task<ProjectConversationAssignmentResult> ConfirmResolutionAssignmentAsync(
            ProjectId targetProjectId,
            ConversationId conversationId,
            ProjectId? expectedSourceProjectId,
            TenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new("confirm", targetProjectId, conversationId, tenantId, caller, metadata, expectedSourceProjectId));
            return Task.FromResult(result);
        }
    }

    private sealed record AssignmentCall(
        string Operation,
        ProjectId ProjectId,
        ConversationId ConversationId,
        TenantId TenantId,
        CallerPrincipalId Caller,
        ProjectConversationCommandMetadata Metadata,
        ProjectId? SourceProjectId);

    private sealed class CapturingProjectCommandSubmitter(ProjectCommandSubmissionResult result) : IProjectCommandSubmitter
    {
        public List<ConfirmProjectResolution> ResolutionConfirmed { get; } = [];

        public Task<ProjectCommandSubmissionResult> SubmitCreateProjectAsync(CreateProject command, CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectCommandSubmissionResult> SubmitUpdateProjectSetupAsync(UpdateProjectSetup command, CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectCommandSubmissionResult> SubmitArchiveProjectAsync(ArchiveProject command, CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectCommandSubmissionResult> SubmitSetProjectFolderAsync(SetProjectFolder command, CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectCommandSubmissionResult> SubmitLinkFileReferenceAsync(LinkFileReference command, CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkFileReferenceAsync(UnlinkFileReference command, CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectCommandSubmissionResult> SubmitLinkMemoryAsync(LinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkMemoryAsync(UnlinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectCommandSubmissionResult> SubmitConfirmProjectResolutionAsync(
            ConfirmProjectResolution command,
            CancellationToken cancellationToken = default)
        {
            ResolutionConfirmed.Add(command);
            return Task.FromResult(result);
        }
    }

    // Stateful submitter that records the first confirm-resolution fingerprint per idempotency key and
    // returns IdempotencyConflict when the same key is reused with a divergent body. This exercises the
    // real same-key/different-body conflict at the HTTP mutation surface (negative-test checklist row 8).
    private sealed class IdempotencyTrackingProjectCommandSubmitter : IProjectCommandSubmitter
    {
        private readonly Dictionary<string, string> _seen = new(StringComparer.Ordinal);

        public List<ConfirmProjectResolution> ResolutionConfirmed { get; } = [];

        public Task<ProjectCommandSubmissionResult> SubmitCreateProjectAsync(CreateProject command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitUpdateProjectSetupAsync(UpdateProjectSetup command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitArchiveProjectAsync(ArchiveProject command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitSetProjectFolderAsync(SetProjectFolder command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitLinkFileReferenceAsync(LinkFileReference command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkFileReferenceAsync(UnlinkFileReference command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitLinkMemoryAsync(LinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkMemoryAsync(UnlinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitConfirmProjectResolutionAsync(
            ConfirmProjectResolution command,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            ResolutionConfirmed.Add(command);
            string fingerprint = $"{command.ConversationId}|{command.SourceProjectId?.Value ?? "<none>"}";
            if (_seen.TryGetValue(command.IdempotencyKey, out string? prior))
            {
                return Task.FromResult(string.Equals(prior, fingerprint, StringComparison.Ordinal)
                    ? ProjectCommandSubmissionResult.Accepted("projects-corr", true)
                    : new ProjectCommandSubmissionResult(ProjectCommandSubmissionOutcome.IdempotencyConflict, "projects-corr"));
            }

            _seen[command.IdempotencyKey] = fingerprint;
            return Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
        }
    }
}
