// <copyright file="ProjectQueryTenantFilterTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Server;

using Shouldly;

using Xunit;

/// <summary>Pure query-filter tests for tenant-scoped Project detail and list rows.</summary>
public sealed class ProjectQueryTenantFilterTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 5, 12, 12, 34, 56, TimeSpan.Zero);

    [Fact]
    public void Filter_DetailReturnsNullForEmptyTenantOrForeignItem()
    {
        ProjectDetailItem foreign = Detail("tenant-a", "project-a");

        ProjectQueryTenantFilter.Filter(null, foreign).ShouldBeNull();
        ProjectQueryTenantFilter.Filter(" ", foreign).ShouldBeNull();
        ProjectQueryTenantFilter.Filter("tenant-b", foreign).ShouldBeNull();
        ProjectQueryTenantFilter.Filter("tenant-a", foreign).ShouldBe(foreign);
    }

    [Fact]
    public void FilterDetails_HandlesEmptyAndMixedTenantRows()
    {
        IReadOnlyList<ProjectDetailItem> filtered = ProjectQueryTenantFilter.FilterDetails(
            "tenant-a",
            [Detail("tenant-a", "project-a"), Detail("tenant-b", "project-b")]);

        filtered.Select(row => row.ProjectId).ShouldBe(["project-a"]);
        ProjectQueryTenantFilter.FilterDetails("tenant-a", []).ShouldBeEmpty();
        ProjectQueryTenantFilter.FilterDetails("", [Detail("tenant-a", "project-a")]).ShouldBeEmpty();
    }

    [Fact]
    public void FilterList_HandlesEmptyAndMixedTenantRows()
    {
        IReadOnlyList<ProjectListItem> filtered = ProjectQueryTenantFilter.FilterList(
            "tenant-a",
            [List("tenant-a", "project-a"), List("tenant-b", "project-b")]);

        filtered.Select(row => row.ProjectId).ShouldBe(["project-a"]);
        ProjectQueryTenantFilter.FilterList("tenant-a", []).ShouldBeEmpty();
        ProjectQueryTenantFilter.FilterList("", [List("tenant-a", "project-a")]).ShouldBeEmpty();
    }

    private static ProjectDetailItem Detail(string tenantId, string projectId)
        => new(
            tenantId,
            projectId,
            "Synthetic",
            "Synthetic description",
            "setup-reference",
            null,
            null,
            [],
            ProjectLifecycle.Active,
            Timestamp,
            Timestamp,
            1);

    private static ProjectListItem List(string tenantId, string projectId)
        => new(
            tenantId,
            projectId,
            "Synthetic",
            ProjectLifecycle.Active,
            1,
            Timestamp,
            Timestamp);
}
