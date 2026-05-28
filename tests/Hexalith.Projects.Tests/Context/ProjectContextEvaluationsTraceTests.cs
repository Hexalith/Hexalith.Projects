// <copyright file="ProjectContextEvaluationsTraceTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Context;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Testing.Context;
using Hexalith.Projects.Testing.Leakage;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>
/// Story 3.3 Tier-1 trace integrity tests for the per-candidate
/// <see cref="ProjectContextEvaluation"/> rows the Story 3.1 policy emits and Story 3.3 surfaces on
/// the wire via <see cref="ProjectContextExplanation"/>. Verifies the policy-to-wire contract is
/// stable end-to-end (one row per candidate, deterministic Ordinal sort, outer-collapse empty
/// trace, closed-vocabulary diagnostics, FS-2 no-leakage).
/// </summary>
public sealed class ProjectContextEvaluationsTraceTests
{
    [Fact]
    public void Trace_OneRowPerCandidate_WhenAllKindsPresent()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(operationKind: ProjectContextOperationKind.Explain),
            Project(),
            TenantAccess(),
            WithAllKinds());

        // WithAllKinds() seeds: 1 folder + 1 file + 1 memory + 1 conversation = 4 candidates.
        result.Evaluations.Count.ShouldBe(4);
        string[] kinds = result.Evaluations.Select(static e => e.ReferenceKind).ToArray();
        kinds.ShouldContain("folder");
        kinds.ShouldContain("file");
        kinds.ShouldContain("memory");
        kinds.ShouldContain("conversation");
    }

    [Fact]
    public void Trace_DeterministicSort_KindThenIdOrdinal()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextReferenceEvidence evidence = new(
            ProjectFolder: new ProjectFolderReference(
                FolderId: "folder_x",
                DisplayName: "Sort fixture",
                ReferenceState: ReferenceState.Included,
                ReasonCode: null,
                ObservedAt: DefaultNow),
            FileReferences:
            [
                new ProjectFileReference(
                    FileReferenceId: "file_a",
                    FolderId: "folder_x",
                    DisplayName: "a",
                    ReferenceState: ReferenceState.Included,
                    ReasonCode: null,
                    ObservedAt: DefaultNow),
                new ProjectFileReference(
                    FileReferenceId: "file_Z",
                    FolderId: "folder_x",
                    DisplayName: "Z",
                    ReferenceState: ReferenceState.Included,
                    ReasonCode: null,
                    ObservedAt: DefaultNow),
                new ProjectFileReference(
                    FileReferenceId: "file_b",
                    FolderId: "folder_x",
                    DisplayName: "b",
                    ReferenceState: ReferenceState.Included,
                    ReasonCode: null,
                    ObservedAt: DefaultNow),
            ],
            MemoryReferences:
            [
                new ProjectMemoryReference(
                    MemoryReferenceId: "m_1",
                    DisplayName: "memory",
                    ReferenceState: ReferenceState.Included,
                    ReasonCode: null,
                    ObservedAt: DefaultNow),
            ],
            Conversations:
            [
                new ProjectContextConversationEvidence(
                    ConversationId: "c_2",
                    DisplayLabel: "two",
                    TrustSignal: ProjectConversationTrustSignal.Current,
                    LastCheckedAt: DefaultNow),
                new ProjectContextConversationEvidence(
                    ConversationId: "c_1",
                    DisplayLabel: "one",
                    TrustSignal: ProjectConversationTrustSignal.Current,
                    LastCheckedAt: DefaultNow),
            ]);

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(operationKind: ProjectContextOperationKind.Explain),
            Project(),
            TenantAccess(),
            evidence);

        // Deterministic sort: (ReferenceKind, ReferenceId) Ordinal.
        (string Kind, string Id)[] expected =
        [
            ("conversation", "c_1"),
            ("conversation", "c_2"),
            ("file", "file_Z"),
            ("file", "file_a"),
            ("file", "file_b"),
            ("folder", "folder_x"),
            ("memory", "m_1"),
        ];

        result.Evaluations
            .Select(static e => (e.ReferenceKind, e.ReferenceId))
            .ToArray()
            .ShouldBe(expected);
    }

    [Fact]
    public void Trace_OuterCollapse_TenantAuthorityMissing_HasEmptyEvaluations()
    {
        ProjectContextInclusionPolicy policy = new();

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(authoritativeTenantId: null, operationKind: ProjectContextOperationKind.Explain),
            Project(),
            TenantAccess(),
            WithAllKinds());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Unauthorized);
        result.Evaluations.ShouldBeEmpty();
    }

    [Fact]
    public void Trace_OuterCollapse_ProjectVisibilityFails_HasEmptyEvaluations()
    {
        ProjectContextInclusionPolicy policy = new();

        // Project visible to a different tenant — policy returns ProjectUnavailable with empty
        // evaluations (existence-non-inference contract; the cross-tenant branch never leaks a
        // per-candidate trace).
        ProjectContextAssemblyResult result = policy.Assemble(
            Context(authoritativeTenantId: "tenant-a", requestedTenantId: "tenant-a", operationKind: ProjectContextOperationKind.Explain),
            Project(tenantId: "tenant-b"),
            TenantAccess(tenantId: "tenant-a"),
            WithAllKinds());

        result.Context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.ProjectUnavailable);
        result.Evaluations.ShouldBeEmpty();
    }

    public static IEnumerable<object?[]> ClosedDiagnosticInputs()
    {
        yield return [ProjectContextInclusionDiagnostic.TenantMismatch];
        yield return [ProjectContextInclusionDiagnostic.ProjectUnknown];
        yield return [ProjectContextInclusionDiagnostic.ProjectArchived];
        yield return [ProjectContextInclusionDiagnostic.ReferenceUnauthorized];
        yield return [ProjectContextInclusionDiagnostic.ReferenceUnavailable];
        yield return [ProjectContextInclusionDiagnostic.ReferenceStale];
        yield return [ProjectContextInclusionDiagnostic.ReferenceArchived];
        yield return [ProjectContextInclusionDiagnostic.ReferenceConflict];
        yield return [ProjectContextInclusionDiagnostic.ReferenceInvalidIdentifier];
        yield return [ProjectContextInclusionDiagnostic.ReferenceKindNotAllowlisted];
        yield return [ProjectContextInclusionDiagnostic.ProjectFolderPending];
        yield return [ProjectContextInclusionDiagnostic.ReferenceAmbiguous];
        yield return [ProjectContextInclusionDiagnostic.ReferenceRedacted];
        yield return new object?[] { null };
    }

    [Theory]
    [MemberData(nameof(ClosedDiagnosticInputs))]
    public void Trace_AllEvaluations_HaveDiagnostic_InClosedVocabularyOrNull(string? diagnostic)
        => ProjectContextInclusionDiagnostic.IsKnown(diagnostic).ShouldBeTrue();

    [Fact]
    public void Trace_NoLeakage_OverEvaluationsArray()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextReferenceEvidence evidence = new(
            ProjectFolder: new ProjectFolderReference(
                FolderId: "folder_x",
                DisplayName: "fixture",
                ReferenceState: ReferenceState.Included,
                ReasonCode: null,
                ObservedAt: DefaultNow),
            FileReferences:
            [
                new ProjectFileReference(
                    FileReferenceId: "file_included",
                    FolderId: "folder_x",
                    DisplayName: "in",
                    ReferenceState: ReferenceState.Included,
                    ReasonCode: null,
                    ObservedAt: DefaultNow),
                new ProjectFileReference(
                    FileReferenceId: "file_stale",
                    FolderId: "folder_x",
                    DisplayName: "out",
                    ReferenceState: ReferenceState.Stale,
                    ReasonCode: null,
                    ObservedAt: DefaultNow),
            ],
            MemoryReferences:
            [
                new ProjectMemoryReference(
                    MemoryReferenceId: "case_archived",
                    DisplayName: "archived case",
                    ReferenceState: ReferenceState.Archived,
                    ReasonCode: null,
                    ObservedAt: DefaultNow),
            ],
            Conversations:
            [
                new ProjectContextConversationEvidence(
                    ConversationId: "conv_current",
                    DisplayLabel: "ok",
                    TrustSignal: ProjectConversationTrustSignal.Current,
                    LastCheckedAt: DefaultNow),
            ]);

        ProjectContextAssemblyResult result = policy.Assemble(
            Context(operationKind: ProjectContextOperationKind.Explain),
            Project(),
            TenantAccess(),
            evidence);

        // 1 folder + 2 files + 1 memory + 1 conversation = 5 candidate evaluation rows.
        result.Evaluations.Count.ShouldBe(5);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakage(
            new ProjectContextExplanation(result.Context, result.Evaluations)));
    }
}
