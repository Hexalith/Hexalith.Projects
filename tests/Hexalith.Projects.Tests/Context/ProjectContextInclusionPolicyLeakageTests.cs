// <copyright file="ProjectContextInclusionPolicyLeakageTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Context;

using System;

using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Context;
using Hexalith.Projects.Testing.Leakage;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>
/// Tier-1 NoPayloadLeakage harness tests for <see cref="ProjectContextInclusionPolicy"/> outputs
/// (Story 3.1 AC 10, AC 12). Every <see cref="ProjectContext"/> produced by the policy across the
/// matrix is asserted leakage-free; the closed-vocabulary diagnostic boundary is also asserted.
/// </summary>
public sealed class ProjectContextInclusionPolicyLeakageTests
{
    [Fact]
    public void HappyPath_AssembledContext_IsLeakageFree()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithAllKinds());

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(result.Context));
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(result));
    }

    [Fact]
    public void Unauthorized_EmptyContext_IsLeakageFree()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(authoritativeTenantId: null),
            Project(),
            TenantAccess(),
            NoReferences());

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(result.Context));
    }

    [Fact]
    public void ProjectUnavailable_EmptyContext_IsLeakageFree()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            ProjectMissing(),
            TenantAccess(),
            NoReferences());

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(result.Context));
    }

    [Fact]
    public void ArchivedProject_AssembledContext_IsLeakageFree()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(lifecycle: ProjectLifecycle.Archived),
            TenantAccess(),
            WithAllKinds());

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(result.Context));
    }

    [Theory]
    [InlineData(ProjectConversationTrustSignal.Stale)]
    [InlineData(ProjectConversationTrustSignal.Rebuilding)]
    [InlineData(ProjectConversationTrustSignal.Forbidden)]
    [InlineData(ProjectConversationTrustSignal.Redacted)]
    public void EachConversationExclusion_ProducesLeakageFreeOutput(ProjectConversationTrustSignal signal)
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(),
            Project(),
            TenantAccess(),
            WithConversation(signal));

        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(result.Context));
    }

    [Fact]
    public void Diagnostic_OutOfVocabulary_OnExclusion_ThrowsArgumentException()
        => Should.Throw<ArgumentException>(() =>
            new ProjectContextExclusion(
                ReferenceKind: "memory",
                ReferenceId: "case_a",
                ReferenceState: ReferenceState.Unauthorized,
                ReasonCode: null,
                FailedCheck: ProjectContextInclusionCheck.ReferenceAuthorization,
                Diagnostic: "this is free-form upstream Suggestion text"));

    [Fact]
    public void Diagnostic_OutOfVocabulary_OnEvaluation_ThrowsArgumentException()
        => Should.Throw<ArgumentException>(() =>
            new ProjectContextEvaluation(
                ReferenceKind: "memory",
                ReferenceId: "case_a",
                ResultState: ReferenceState.Unauthorized,
                FailedCheck: ProjectContextInclusionCheck.ReferenceAuthorization,
                ReasonCode: null,
                Diagnostic: "this is free-form text",
                ObservedAt: DefaultNow));

    [Fact]
    public void Diagnostic_NullIsAllowed_OnExclusionAndEvaluation()
    {
        ProjectContextExclusion ex = new(
            "memory",
            "case_a",
            ReferenceState.Included,
            null,
            ProjectContextInclusionCheck.ReferenceFreshness,
            null);
        ProjectContextEvaluation ev = new(
            "memory",
            "case_a",
            ReferenceState.Included,
            null,
            null,
            null,
            DefaultNow);

        ex.Diagnostic.ShouldBeNull();
        ev.Diagnostic.ShouldBeNull();
    }

    [Fact]
    public void AllPolicyDiagnostics_AreMembersOfClosedVocabulary()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult[] results =
        [
            policy.Assemble(Context(), Project(), TenantAccess(), WithFolder(ReferenceState.Pending, folderId: null)),
            policy.Assemble(Context(), Project(), TenantAccess(), WithFile(ReferenceState.Stale)),
            policy.Assemble(Context(), Project(), TenantAccess(), WithMemory(ReferenceState.TenantMismatch)),
            policy.Assemble(Context(), Project(), TenantAccess(), WithConversation(ProjectConversationTrustSignal.Forbidden)),
            policy.Assemble(Context(), Project(lifecycle: ProjectLifecycle.Archived), TenantAccess(), WithAllKinds()),
        ];

        foreach (ProjectContextAssemblyResult result in results)
        {
            foreach (ProjectContextExclusion row in result.Context.Excluded)
            {
                ProjectContextInclusionDiagnostic.IsKnown(row.Diagnostic).ShouldBeTrue();
            }

            foreach (ProjectContextEvaluation eval in result.Evaluations)
            {
                ProjectContextInclusionDiagnostic.IsKnown(eval.Diagnostic).ShouldBeTrue();
            }
        }
    }
}
