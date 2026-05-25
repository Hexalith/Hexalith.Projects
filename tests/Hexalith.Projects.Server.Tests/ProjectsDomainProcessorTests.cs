// <copyright file="ProjectsDomainProcessorTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-2 tests for the <c>/process</c> aggregate-callback <see cref="ProjectsDomainProcessor"/>
/// (AC 1, 2): a valid create yields a success <c>DomainResult</c> carrying exactly one
/// <c>ProjectCreated</c>; a duplicate create against existing state yields a rejection
/// <c>DomainResult</c> (a rejection event, never an exception). Tenant authority comes from the
/// verified envelope.
/// </summary>
public sealed class ProjectsDomainProcessorTests
{
    private const string Tenant = "tenant-a";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    [Fact]
    public async Task ProcessCreate_NewAggregate_YieldsSuccessDomainResultWithProjectCreated()
    {
        ProjectsDomainProcessor processor = new(new FixedTimeProvider(DateTimeOffset.UnixEpoch));

        DomainResult result = await processor.ProcessAsync(Envelope(), currentState: null).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        ProjectCreated created = result.Events[0].ShouldBeOfType<ProjectCreated>();
        created.TenantId.ShouldBe(Tenant);
        created.ProjectId.ShouldBe(ProjectIdValue);
        created.OccurredAt.ShouldBe(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task ProcessCreate_DuplicateAgainstExistingState_YieldsRejectionNotException()
    {
        ProjectsDomainProcessor processor = new(new FixedTimeProvider(DateTimeOffset.UnixEpoch));

        ProjectState existing = ProjectState.Empty.Apply(
            [ExistingCreatedEvent()],
            new ProjectIdentity(Tenant, new ProjectId(ProjectIdValue)));

        DomainResult result = await processor.ProcessAsync(Envelope(idempotencyKey: "different-key"), existing).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        result.Events[0].ShouldBeOfType<ProjectCreationRejected>().Reason.ShouldBe(Contracts.Ui.ReferenceState.Conflict);
    }

    [Fact]
    public async Task ProcessCreate_MalformedPayload_FailsClosedToRejection()
    {
        ProjectsDomainProcessor processor = new(new FixedTimeProvider(DateTimeOffset.UnixEpoch));

        CommandEnvelope envelope = new(
            MessageId: "idem-key-a",
            TenantId: Tenant,
            Domain: ProjectsServerModule.DomainName,
            AggregateId: ProjectIdValue,
            CommandType: ProjectsServerModule.CreateProjectCommandType,
            Payload: Encoding.UTF8.GetBytes("{ not valid json"),
            CorrelationId: "corr-a",
            CausationId: null,
            UserId: "principal-a",
            Extensions: null);

        DomainResult result = await processor.ProcessAsync(envelope, currentState: null).ConfigureAwait(true);

        result.IsRejection.ShouldBeTrue();
    }

    private static ProjectCreated ExistingCreatedEvent() => new(
        Tenant,
        ProjectIdValue,
        "Existing",
        null,
        null,
        Contracts.Ui.ProjectLifecycle.Active,
        "principal-a",
        "corr-existing",
        "task-existing",
        "key-existing",
        "sha256:existing",
        DateTimeOffset.UnixEpoch);

    private static CommandEnvelope Envelope(string idempotencyKey = "idem-key-a")
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            name = "Tracer Bullet",
            description = "A safe description",
            setupMetadata = (string?)null,
        });

        return new CommandEnvelope(
            MessageId: idempotencyKey,
            TenantId: Tenant,
            Domain: ProjectsServerModule.DomainName,
            AggregateId: ProjectIdValue,
            CommandType: ProjectsServerModule.CreateProjectCommandType,
            Payload: payload,
            CorrelationId: "corr-a",
            CausationId: null,
            UserId: "principal-a",
            Extensions: new Dictionary<string, string> { ["taskId"] = "task-a" });
    }

    // Minimal deterministic TimeProvider so the /process callback stamps a fixed OccurredAt.
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
