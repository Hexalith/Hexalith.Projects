// <copyright file="InMemoryProjectListReadModelTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Server;

using Shouldly;

using Xunit;

/// <summary>Tier-1-style tests for the in-memory ProjectList read model used by server query tests.</summary>
public sealed class InMemoryProjectListReadModelTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 12, 12, 34, 56, TimeSpan.Zero);

    [Fact]
    public async Task ListAsync_FiltersByAuthoritativeTenantBeforeReturningRows()
    {
        InMemoryProjectListReadModel readModel = new();
        readModel.Project("tenant-a", Created("tenant-a", "01HZ9K8YQ3W6V2N4R7T5P0X1AB", ProjectLifecycle.Active));
        readModel.Project("tenant-b", Created("tenant-b", "01HZ9K8YQ3W6V2N4R7T5P0X1AC", ProjectLifecycle.Active));

        IReadOnlyList<ProjectListItem> rows = await readModel
            .ListAsync("tenant-a", lifecycleFilter: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        rows.Count.ShouldBe(1);
        rows.Single().TenantId.ShouldBe("tenant-a");
        rows.Single().ProjectId.ShouldBe("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
    }

    [Fact]
    public async Task ListAsync_FiltersByLifecycleWhenRequested()
    {
        InMemoryProjectListReadModel readModel = new();
        readModel.Project("tenant-a", Created("tenant-a", "01HZ9K8YQ3W6V2N4R7T5P0X1AB", ProjectLifecycle.Active));
        readModel.Project("tenant-a", Created("tenant-a", "01HZ9K8YQ3W6V2N4R7T5P0X1AC", ProjectLifecycle.Archived));

        IReadOnlyList<ProjectListItem> active = await readModel
            .ListAsync("tenant-a", ProjectLifecycle.Active, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        IReadOnlyList<ProjectListItem> archived = await readModel
            .ListAsync("tenant-a", ProjectLifecycle.Archived, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        IReadOnlyList<ProjectListItem> all = await readModel
            .ListAsync("tenant-a", lifecycleFilter: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        active.Select(row => row.ProjectId).ShouldBe(["01HZ9K8YQ3W6V2N4R7T5P0X1AB"]);
        archived.Select(row => row.ProjectId).ShouldBe(["01HZ9K8YQ3W6V2N4R7T5P0X1AC"]);
        all.Select(row => row.ProjectId).OrderBy(id => id, StringComparer.Ordinal).ToArray()
            .ShouldBe(["01HZ9K8YQ3W6V2N4R7T5P0X1AB", "01HZ9K8YQ3W6V2N4R7T5P0X1AC"]);
    }

    [Fact]
    public async Task ListAsync_CreateOnlyProjectionSetsUpdatedAtToCreatedAt()
    {
        InMemoryProjectListReadModel readModel = new();
        readModel.Project("tenant-a", Created("tenant-a", "01HZ9K8YQ3W6V2N4R7T5P0X1AB", ProjectLifecycle.Active));

        ProjectListItem row = (await readModel
            .ListAsync("tenant-a", lifecycleFilter: null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).Single();

        row.CreatedAt.ShouldBe(CreatedAt);
        row.UpdatedAt.ShouldBe(CreatedAt);
    }

    private static ProjectCreated Created(string tenantId, string projectId, ProjectLifecycle lifecycle)
        => new(
            tenantId,
            projectId,
            lifecycle == ProjectLifecycle.Active ? "Active Project" : "Archived Project",
            "Synthetic description",
            "setup-reference",
            lifecycle,
            "principal-a",
            "corr-a",
            "task-a",
            "idem-" + projectId[^4..],
            "sha256:" + projectId[^8..],
            CreatedAt);
}
