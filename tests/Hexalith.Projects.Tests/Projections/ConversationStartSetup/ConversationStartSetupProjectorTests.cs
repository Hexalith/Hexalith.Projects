// <copyright file="ConversationStartSetupProjectorTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Projections.ConversationStartSetup;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ConversationStartSetup;

using Shouldly;

using Xunit;

/// <summary>
/// Story 3.5 Tier-1 purity tests for the <see cref="ConversationStartSetupProjector"/> pure projector.
/// Asserts the AR-8 <c>ConversationStartSetupProjection</c> mapping invariants: subset mirroring,
/// closed-vocabulary default-of-default, lifecycle / freshness / observed-at / project-id /
/// source-kind-order preservation, empty-source-kind happy path, function purity, and no-mutation.
/// No infrastructure imports; no sibling client; no wall-clock; no <c>Thread.Sleep</c> /
/// <c>Task.Delay</c> / <c>SpinWait</c> / <c>await Task.Yield()</c>.
/// </summary>
public sealed class ConversationStartSetupProjectorTests
{
    private const string ProjectIdValue = "project-canonical-id";
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.Parse("2026-01-15T12:34:56Z", System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void Project_AssembledContextWithFullSetup_MirrorsSubset()
    {
        ProjectSetup setup = new(
            Goals: new[] { "g1", "g2" },
            UserInstructions: new[] { "ui1" },
            PreferredSourceKinds: new[] { ProjectContextSourceKind.Conversation, ProjectContextSourceKind.Memory },
            ExcludedSourceKinds: new[] { ProjectContextSourceKind.FileReference },
            ConversationStartDefaults: new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences));
        ProjectContext context = BuildContext(setup, ProjectLifecycle.Active, ProjectContextFreshness.Fresh);

        ConversationStartSetup projected = ConversationStartSetupProjector.Project(context);

        projected.ProjectId.ShouldBe(ProjectIdValue);
        projected.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        projected.Goals.ShouldBe(new[] { "g1", "g2" });
        projected.UserInstructions.ShouldBe(new[] { "ui1" });
        projected.PreferredSourceKinds.ShouldBe(new[] { ProjectContextSourceKind.Conversation, ProjectContextSourceKind.Memory });
        projected.ExcludedSourceKinds.ShouldBe(new[] { ProjectContextSourceKind.FileReference });
        projected.LinkedSourcePolicy.ShouldBe(LinkedSourcePolicy.AuthorizedReferences);
        projected.ObservedAt.ShouldBe(FixedNow);
        projected.Freshness.ShouldBe(ProjectContextFreshness.Fresh);
    }

    [Fact]
    public void Project_AssembledContextWithNullSetup_ReturnsEmptySubset()
    {
        ProjectContext context = BuildContext(setup: null, ProjectLifecycle.Active, ProjectContextFreshness.Fresh);

        ConversationStartSetup projected = ConversationStartSetupProjector.Project(context);

        projected.Goals.ShouldBeEmpty();
        projected.UserInstructions.ShouldBeEmpty();
        projected.PreferredSourceKinds.ShouldBeEmpty();
        projected.ExcludedSourceKinds.ShouldBeEmpty();
        projected.LinkedSourcePolicy.ShouldBe(LinkedSourcePolicy.None);
    }

    [Fact]
    public void Project_AssembledContextWithNullConversationStartDefaults_DefaultsToNone()
    {
        ProjectSetup setup = new(
            Goals: new[] { "g" },
            UserInstructions: new[] { "ui" },
            PreferredSourceKinds: Array.Empty<ProjectContextSourceKind>(),
            ExcludedSourceKinds: Array.Empty<ProjectContextSourceKind>(),
            ConversationStartDefaults: null);
        ProjectContext context = BuildContext(setup, ProjectLifecycle.Active, ProjectContextFreshness.Fresh);

        ConversationStartSetup projected = ConversationStartSetupProjector.Project(context);

        projected.LinkedSourcePolicy.ShouldBe(LinkedSourcePolicy.None);
        // Setup fields still mirrored even when ConversationStartDefaults is null.
        projected.Goals.ShouldBe(new[] { "g" });
        projected.UserInstructions.ShouldBe(new[] { "ui" });
    }

    [Theory]
    [InlineData(ProjectLifecycle.Active)]
    [InlineData(ProjectLifecycle.Archived)]
    public void Project_PreservesLifecycle(ProjectLifecycle lifecycle)
    {
        ProjectContext context = BuildContext(setup: null, lifecycle, ProjectContextFreshness.Fresh);

        ConversationStartSetup projected = ConversationStartSetupProjector.Project(context);

        projected.Lifecycle.ShouldBe(lifecycle);
    }

    [Theory]
    [InlineData(ProjectContextFreshness.Fresh)]
    [InlineData(ProjectContextFreshness.Stale)]
    [InlineData(ProjectContextFreshness.Unavailable)]
    [InlineData(ProjectContextFreshness.Unknown)]
    public void Project_PreservesFreshness(ProjectContextFreshness freshness)
    {
        ProjectContext context = BuildContext(setup: null, ProjectLifecycle.Active, freshness);

        ConversationStartSetup projected = ConversationStartSetupProjector.Project(context);

        projected.Freshness.ShouldBe(freshness);
    }

    [Fact]
    public void Project_PreservesObservedAt()
    {
        ProjectContext context = BuildContext(setup: null, ProjectLifecycle.Active, ProjectContextFreshness.Fresh);

        ConversationStartSetup projected = ConversationStartSetupProjector.Project(context);

        projected.ObservedAt.ShouldBe(FixedNow);
    }

    [Fact]
    public void Project_PreservesProjectId()
    {
        ProjectContext context = BuildContext(setup: null, ProjectLifecycle.Active, ProjectContextFreshness.Fresh);

        ConversationStartSetup projected = ConversationStartSetupProjector.Project(context);

        projected.ProjectId.ShouldBe(ProjectIdValue);
    }

    [Fact]
    public void Project_PreservesSourceKindOrder()
    {
        // Deliberately unsorted to surface any accidental ordering.
        ProjectContextSourceKind[] preferred = new[]
        {
            ProjectContextSourceKind.ProjectFolder,
            ProjectContextSourceKind.Memory,
            ProjectContextSourceKind.Conversation,
        };
        ProjectContextSourceKind[] excluded = new[]
        {
            ProjectContextSourceKind.FileReference,
            ProjectContextSourceKind.Memory,
        };
        ProjectSetup setup = new(
            Goals: Array.Empty<string>(),
            UserInstructions: Array.Empty<string>(),
            PreferredSourceKinds: preferred,
            ExcludedSourceKinds: excluded,
            ConversationStartDefaults: null);
        ProjectContext context = BuildContext(setup, ProjectLifecycle.Active, ProjectContextFreshness.Fresh);

        ConversationStartSetup projected = ConversationStartSetupProjector.Project(context);

        projected.PreferredSourceKinds.ShouldBe(preferred);
        projected.ExcludedSourceKinds.ShouldBe(excluded);
    }

    [Fact]
    public void Project_AssembledContextWithEmptySourceKinds_ReturnsEmptyArrays()
    {
        ProjectSetup setup = new(
            Goals: Array.Empty<string>(),
            UserInstructions: Array.Empty<string>(),
            PreferredSourceKinds: Array.Empty<ProjectContextSourceKind>(),
            ExcludedSourceKinds: Array.Empty<ProjectContextSourceKind>(),
            ConversationStartDefaults: null);
        ProjectContext context = BuildContext(setup, ProjectLifecycle.Active, ProjectContextFreshness.Fresh);

        ConversationStartSetup projected = ConversationStartSetupProjector.Project(context);

        projected.PreferredSourceKinds.ShouldNotBeNull();
        projected.PreferredSourceKinds.ShouldBeEmpty();
        projected.ExcludedSourceKinds.ShouldNotBeNull();
        projected.ExcludedSourceKinds.ShouldBeEmpty();
    }

    [Fact]
    public void Project_IsPureFunction_SameInputProducesSameOutput()
    {
        ProjectSetup setup = new(
            Goals: new[] { "g" },
            UserInstructions: new[] { "ui" },
            PreferredSourceKinds: new[] { ProjectContextSourceKind.Conversation },
            ExcludedSourceKinds: new[] { ProjectContextSourceKind.FileReference },
            ConversationStartDefaults: new ConversationStartDefaults(LinkedSourcePolicy.ProjectsOwnedMetadataOnly));
        ProjectContext context = BuildContext(setup, ProjectLifecycle.Active, ProjectContextFreshness.Stale);

        ConversationStartSetup first = ConversationStartSetupProjector.Project(context);
        ConversationStartSetup second = ConversationStartSetupProjector.Project(context);

        first.ShouldBe(second);
    }

    [Fact]
    public void Project_DoesNotMutateInput()
    {
        List<string> goals = new() { "g" };
        List<string> instructions = new() { "ui" };
        List<ProjectContextSourceKind> preferred = new() { ProjectContextSourceKind.Conversation };
        List<ProjectContextSourceKind> excluded = new() { ProjectContextSourceKind.Memory };
        ProjectSetup setup = new(
            Goals: goals,
            UserInstructions: instructions,
            PreferredSourceKinds: preferred,
            ExcludedSourceKinds: excluded,
            ConversationStartDefaults: new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences));
        ProjectContext context = BuildContext(setup, ProjectLifecycle.Active, ProjectContextFreshness.Fresh);

        _ = ConversationStartSetupProjector.Project(context);

        goals.ShouldBe(new[] { "g" });
        instructions.ShouldBe(new[] { "ui" });
        preferred.ShouldBe(new[] { ProjectContextSourceKind.Conversation });
        excluded.ShouldBe(new[] { ProjectContextSourceKind.Memory });
        context.Setup.ShouldBeSameAs(setup);
    }

    private static ProjectContext BuildContext(
        ProjectSetup? setup,
        ProjectLifecycle lifecycle,
        ProjectContextFreshness freshness)
        => new(
            TenantId: "tenant-canonical-id",
            ProjectId: ProjectIdValue,
            Lifecycle: lifecycle,
            Setup: setup,
            ProjectFolder: null,
            Conversations: Array.Empty<ProjectContextReference>(),
            FileReferences: Array.Empty<ProjectContextReference>(),
            MemoryReferences: Array.Empty<ProjectContextReference>(),
            Excluded: Array.Empty<ProjectContextExclusion>(),
            AssemblyOutcome: ProjectContextAssemblyOutcome.Assembled,
            ObservedAt: FixedNow,
            Freshness: freshness);
}
