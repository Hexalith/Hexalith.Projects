// <copyright file="ProjectStateApplyTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Aggregates.Project;

using System;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Pure Tier-1 tests for <see cref="ProjectStateApply"/> (AC 1, 6, 7): applies <c>ProjectCreated</c>,
/// rejects a foreign-tenant event, dedupes an idempotent replay, and throws on an unknown event type.
/// </summary>
public sealed class ProjectStateApplyTests
{
    private const string Tenant = "acme";
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    [Fact]
    public void ApplyProjectCreated_SetsCreatedAndActive()
    {
        ProjectIdentity identity = Identity(Tenant);

        ProjectState state = ProjectStateApply.Apply(ProjectState.Empty, Created(Tenant), identity);

        state.IsCreated.ShouldBeTrue();
        state.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        state.TenantId.ShouldBe(Tenant);
        state.ProjectId.ShouldBe(ProjectIdValue);
        state.IdempotencyFingerprints.ShouldContainKey("idem-key-001");
    }

    [Fact]
    public void ForeignTenantEvent_Throws_TenantMismatch()
    {
        // The expected identity is tenant A; the event carries tenant B → foreign event guard fires.
        ProjectIdentity expected = Identity("tenant-a");

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => ProjectStateApply.Apply(ProjectState.Empty, Created("tenant-b"), expected));

        ex.Message.ShouldContain(nameof(ProjectResultCode.TenantMismatch));
        // The exception message must not echo the event payload (only the stable result code).
        ex.Message.ShouldNotContain("Tracer Bullet");
    }

    [Fact]
    public void IdenticalReplay_IsDeduped_StateUnchanged()
    {
        ProjectIdentity identity = Identity(Tenant);
        ProjectState once = ProjectStateApply.Apply(ProjectState.Empty, Created(Tenant), identity);

        ProjectState twice = ProjectStateApply.Apply(once, Created(Tenant), identity);

        twice.ShouldBe(once);
    }

    [Fact]
    public void UnknownEventType_ThrowsStateTransitionInvalid()
    {
        ProjectIdentity identity = Identity(Tenant);

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => ProjectStateApply.Apply(ProjectState.Empty, new UnknownProjectEvent(), identity));

        ex.Message.ShouldContain(nameof(ProjectResultCode.StateTransitionInvalid));
    }

    private static ProjectIdentity Identity(string tenant) => new(tenant, new ProjectId(ProjectIdValue));

    private static ProjectCreated Created(string tenant) => new(
        tenant,
        ProjectIdValue,
        "Tracer Bullet",
        null,
        null,
        ProjectLifecycle.Active,
        "actor-001",
        "corr-001",
        "task-001",
        "idem-key-001",
        "sha256:deadbeef",
        DateTimeOffset.UnixEpoch);

    // A foreign event type that targets the expected identity but is not handled by Apply.
    private sealed record UnknownProjectEvent : IProjectEvent
    {
        public string TenantId => Tenant;

        public string ProjectId => ProjectIdValue;

        public string CorrelationId => "corr-001";

        public string TaskId => "task-001";

        public string IdempotencyKey => "idem-key-zzz";

        public string IdempotencyFingerprint => "sha256:feedface";

        public DateTimeOffset OccurredAt => DateTimeOffset.UnixEpoch;
    }
}
