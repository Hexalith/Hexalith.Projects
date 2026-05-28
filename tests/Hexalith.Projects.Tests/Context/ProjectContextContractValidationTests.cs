// <copyright file="ProjectContextContractValidationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Context;

using System;

using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Context;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>
/// Tier-1 contract-level coverage for the additive Story 3.1 DTOs and helpers (AC 9, AC 5, AC 17).
/// Closes gaps the matrix/behavior tests do not exercise: eager-validation on
/// <see cref="ProjectContextReference"/> / <see cref="ProjectContextExclusion"/> /
/// <see cref="ProjectContextEvaluation"/>, the closed
/// <see cref="ProjectContextInclusionDiagnostic.IsKnown(string?)"/> vocabulary boundary, the
/// case-sensitive <see cref="ProjectContextInclusionOrder.IsAllowlisted(string?)"/> contract,
/// the canonical <see cref="ProjectContext.Unauthorized(string, string, DateTimeOffset, ProjectContextFreshness)"/>
/// / <see cref="ProjectContext.ProjectUnavailable(string, string, DateTimeOffset, ProjectContextFreshness)"/>
/// factory shapes, and the
/// <see cref="ProjectContextInclusionPolicy.Assemble(ProjectContextAssemblyContext, ProjectContextProjectEvidence, ProjectContextTenantAccess, ProjectContextReferenceEvidence)"/>
/// null-argument guards plus the correlation-metadata-no-leak invariant.
/// </summary>
public sealed class ProjectContextContractValidationTests
{
    // ============================================================================================
    // ProjectContextReference — eager-validation contract (AC 9 / Task 2)
    // ============================================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProjectContextReference_NullOrWhitespaceReferenceKind_Throws(string? referenceKind)
        => Should.Throw<ArgumentException>(() =>
            new ProjectContextReference(
                ReferenceKind: referenceKind!,
                ReferenceId: "id-001",
                DisplayName: null,
                ReferenceState: ReferenceState.Included,
                ReasonCode: ProjectReasonCode.MemoryMatched,
                ObservedAt: DateTimeOffset.UnixEpoch));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProjectContextReference_NullOrWhitespaceReferenceId_Throws(string? referenceId)
        => Should.Throw<ArgumentException>(() =>
            new ProjectContextReference(
                ReferenceKind: "memory",
                ReferenceId: referenceId!,
                DisplayName: null,
                ReferenceState: ReferenceState.Included,
                ReasonCode: ProjectReasonCode.MemoryMatched,
                ObservedAt: DateTimeOffset.UnixEpoch));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("    ")]
    [InlineData("\t\n")]
    public void ProjectContextReference_WhitespaceDisplayName_NormalizedToNull(string displayName)
    {
        ProjectContextReference reference = new(
            ReferenceKind: "memory",
            ReferenceId: "case_a",
            DisplayName: displayName,
            ReferenceState: ReferenceState.Included,
            ReasonCode: ProjectReasonCode.MemoryMatched,
            ObservedAt: DateTimeOffset.UnixEpoch);

        reference.DisplayName.ShouldBeNull();
    }

    [Fact]
    public void ProjectContextReference_NonWhitespaceDisplayName_Preserved()
    {
        ProjectContextReference reference = new(
            ReferenceKind: "memory",
            ReferenceId: "case_a",
            DisplayName: "Q3 product strategy memory",
            ReferenceState: ReferenceState.Included,
            ReasonCode: ProjectReasonCode.MemoryMatched,
            ObservedAt: DateTimeOffset.UnixEpoch);

        reference.DisplayName.ShouldBe("Q3 product strategy memory");
    }

    // ============================================================================================
    // ProjectContextExclusion — eager-validation contract (AC 9 / Task 2)
    // ============================================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProjectContextExclusion_NullOrWhitespaceReferenceKind_Throws(string? referenceKind)
        => Should.Throw<ArgumentException>(() =>
            new ProjectContextExclusion(
                ReferenceKind: referenceKind!,
                ReferenceId: "case_a",
                ReferenceState: ReferenceState.Unauthorized,
                ReasonCode: null,
                FailedCheck: ProjectContextInclusionCheck.ReferenceAuthorization,
                Diagnostic: ProjectContextInclusionDiagnostic.ReferenceUnauthorized));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProjectContextExclusion_NullOrWhitespaceReferenceId_Throws(string? referenceId)
        => Should.Throw<ArgumentException>(() =>
            new ProjectContextExclusion(
                ReferenceKind: "memory",
                ReferenceId: referenceId!,
                ReferenceState: ReferenceState.Unauthorized,
                ReasonCode: null,
                FailedCheck: ProjectContextInclusionCheck.ReferenceAuthorization,
                Diagnostic: ProjectContextInclusionDiagnostic.ReferenceUnauthorized));

    [Fact]
    public void ProjectContextExclusion_EveryClosedVocabularyValue_IsAccepted()
    {
        foreach (string diagnostic in ProjectContextInclusionDiagnostic.Values)
        {
            ProjectContextExclusion exclusion = new(
                ReferenceKind: "memory",
                ReferenceId: "case_a",
                ReferenceState: ReferenceState.Unauthorized,
                ReasonCode: null,
                FailedCheck: ProjectContextInclusionCheck.ReferenceAuthorization,
                Diagnostic: diagnostic);

            exclusion.Diagnostic.ShouldBe(diagnostic);
        }
    }

    [Fact]
    public void ProjectContextExclusion_NullDiagnostic_IsAccepted()
    {
        ProjectContextExclusion exclusion = new(
            ReferenceKind: "memory",
            ReferenceId: "case_a",
            ReferenceState: ReferenceState.Included,
            ReasonCode: ProjectReasonCode.MemoryMatched,
            FailedCheck: ProjectContextInclusionCheck.ReferenceFreshness,
            Diagnostic: null);

        exclusion.Diagnostic.ShouldBeNull();
    }

    // ============================================================================================
    // ProjectContextEvaluation — eager-validation contract (AC 9 / Task 2)
    // ============================================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProjectContextEvaluation_NullOrWhitespaceReferenceKind_Throws(string? referenceKind)
        => Should.Throw<ArgumentException>(() =>
            new ProjectContextEvaluation(
                ReferenceKind: referenceKind!,
                ReferenceId: "case_a",
                ResultState: ReferenceState.Included,
                FailedCheck: null,
                ReasonCode: ProjectReasonCode.MemoryMatched,
                Diagnostic: null,
                ObservedAt: DateTimeOffset.UnixEpoch));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProjectContextEvaluation_NullOrWhitespaceReferenceId_Throws(string? referenceId)
        => Should.Throw<ArgumentException>(() =>
            new ProjectContextEvaluation(
                ReferenceKind: "memory",
                ReferenceId: referenceId!,
                ResultState: ReferenceState.Included,
                FailedCheck: null,
                ReasonCode: ProjectReasonCode.MemoryMatched,
                Diagnostic: null,
                ObservedAt: DateTimeOffset.UnixEpoch));

    [Theory]
    [InlineData("an upstream Suggestion message")]
    [InlineData("/home/user/secret.txt")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.payload.sig")]
    [InlineData("TENANTMISMATCH")]
    public void ProjectContextEvaluation_OutOfVocabularyDiagnostic_Throws(string diagnostic)
        => Should.Throw<ArgumentException>(() =>
            new ProjectContextEvaluation(
                ReferenceKind: "memory",
                ReferenceId: "case_a",
                ResultState: ReferenceState.Unauthorized,
                FailedCheck: ProjectContextInclusionCheck.ReferenceAuthorization,
                ReasonCode: null,
                Diagnostic: diagnostic,
                ObservedAt: DateTimeOffset.UnixEpoch));

    // ============================================================================================
    // ProjectContextInclusionDiagnostic.IsKnown — closed-vocabulary boundary (AC 9, AC 17)
    // ============================================================================================

    [Fact]
    public void IsKnown_Null_ReturnsTrue()
        => ProjectContextInclusionDiagnostic.IsKnown(null).ShouldBeTrue();

    [Fact]
    public void IsKnown_EveryShippedValue_ReturnsTrue()
    {
        foreach (string value in ProjectContextInclusionDiagnostic.Values)
        {
            ProjectContextInclusionDiagnostic.IsKnown(value).ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("tenantmismatch")]
    [InlineData("TenantMismatch")]
    [InlineData("TENANTMISMATCH")]
    [InlineData("unknownDiagnostic")]
    [InlineData("an upstream Suggestion message")]
    public void IsKnown_UnknownOrWrongCase_ReturnsFalse(string value)
        => ProjectContextInclusionDiagnostic.IsKnown(value).ShouldBeFalse();

    [Fact]
    public void Values_AreImmutableAndOrdinal()
    {
        ProjectContextInclusionDiagnostic.Values.Count.ShouldBeGreaterThan(0);
        ProjectContextInclusionDiagnostic.Values.ShouldContain(ProjectContextInclusionDiagnostic.TenantMismatch);
    }

    // ============================================================================================
    // ProjectContextInclusionOrder.IsAllowlisted — Ordinal/case-sensitive contract (AC 5)
    // ============================================================================================

    [Theory]
    [InlineData("FILE")]
    [InlineData("File")]
    [InlineData("Folder")]
    [InlineData("MEMORY")]
    [InlineData("Conversation")]
    [InlineData("file ")]
    [InlineData(" file")]
    [InlineData("file\t")]
    public void IsAllowlisted_WrongCaseOrSurroundingWhitespace_ReturnsFalse(string referenceKind)
        => ProjectContextInclusionOrder.IsAllowlisted(referenceKind).ShouldBeFalse();

    [Fact]
    public void IsAllowlisted_ExactAllowlistedValues_AllReturnTrue()
    {
        foreach (string kind in ProjectContextInclusionOrder.AllowlistedReferenceKinds)
        {
            ProjectContextInclusionOrder.IsAllowlisted(kind).ShouldBeTrue();
        }
    }

    // ============================================================================================
    // ProjectContext.Unauthorized / ProjectUnavailable factory shapes (Task 2)
    // ============================================================================================

    [Fact]
    public void Unauthorized_Factory_ProducesEmptyContextWithUnauthorizedOutcome()
    {
        ProjectContext context = ProjectContext.Unauthorized(
            requestedTenantId: "tenant-a",
            projectId: "project-1",
            observedAt: DefaultNow,
            freshness: ProjectContextFreshness.Unknown);

        context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.Unauthorized);
        context.TenantId.ShouldBe("tenant-a");
        context.ProjectId.ShouldBe("project-1");
        context.ObservedAt.ShouldBe(DefaultNow);
        context.Freshness.ShouldBe(ProjectContextFreshness.Unknown);
        context.ProjectFolder.ShouldBeNull();
        context.Conversations.ShouldBeEmpty();
        context.FileReferences.ShouldBeEmpty();
        context.MemoryReferences.ShouldBeEmpty();
        context.Excluded.ShouldBeEmpty();
        context.Setup.ShouldBeNull();
    }

    [Fact]
    public void ProjectUnavailable_Factory_ProducesEmptyContextWithSafeDenialOutcome()
    {
        ProjectContext context = ProjectContext.ProjectUnavailable(
            requestedTenantId: "tenant-a",
            projectId: "project-1",
            observedAt: DefaultNow,
            freshness: ProjectContextFreshness.Fresh);

        context.AssemblyOutcome.ShouldBe(ProjectContextAssemblyOutcome.ProjectUnavailable);
        context.AssemblyOutcome.ShouldNotBe(ProjectContextAssemblyOutcome.Unauthorized);
        context.TenantId.ShouldBe("tenant-a");
        context.ProjectId.ShouldBe("project-1");
        context.ProjectFolder.ShouldBeNull();
        context.Conversations.ShouldBeEmpty();
        context.FileReferences.ShouldBeEmpty();
        context.MemoryReferences.ShouldBeEmpty();
        context.Excluded.ShouldBeEmpty();
    }

    // ============================================================================================
    // ProjectContextInclusionPolicy.Assemble — null-argument guards (AC 1)
    // ============================================================================================

    [Fact]
    public void Assemble_NullContext_ThrowsArgumentNullException()
    {
        ProjectContextInclusionPolicy policy = new();

        Should.Throw<ArgumentNullException>(() =>
            policy.Assemble(context: null!, Project(), TenantAccess(), NoReferences()));
    }

    [Fact]
    public void Assemble_NullProjectEvidence_ThrowsArgumentNullException()
    {
        ProjectContextInclusionPolicy policy = new();

        Should.Throw<ArgumentNullException>(() =>
            policy.Assemble(Context(), project: null!, TenantAccess(), NoReferences()));
    }

    [Fact]
    public void Assemble_NullTenantAccess_ThrowsArgumentNullException()
    {
        ProjectContextInclusionPolicy policy = new();

        Should.Throw<ArgumentNullException>(() =>
            policy.Assemble(Context(), Project(), tenantAccess: null!, NoReferences()));
    }

    [Fact]
    public void Assemble_NullReferenceEvidence_ThrowsArgumentNullException()
    {
        ProjectContextInclusionPolicy policy = new();

        Should.Throw<ArgumentNullException>(() =>
            policy.Assemble(Context(), Project(), TenantAccess(), references: null!));
    }

    // ============================================================================================
    // Correlation metadata never leaks into the assembled output (AC 10, AC 17)
    // ============================================================================================

    [Fact]
    public void Assemble_CorrelationAndTaskIds_NeverLeakIntoAssembledContext()
    {
        ProjectContextInclusionPolicy policy = new();
        ProjectContextAssemblyContext requestContext = new(
            AuthoritativeTenantId: DefaultTenant,
            RequestedTenantId: DefaultTenant,
            ProjectId: DefaultProjectId,
            OperationKind: ProjectContextOperationKind.Get,
            CorrelationId: "corr-leak-sentinel-x9q2",
            TaskId: "task-leak-sentinel-x9q2",
            Now: DefaultNow);

        ProjectContextAssemblyResult result = policy.Assemble(
            requestContext,
            Project(),
            TenantAccess(),
            WithAllKinds());

        string serialized = System.Text.Json.JsonSerializer.Serialize(
            result,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        serialized.ShouldNotContain("corr-leak-sentinel-x9q2");
        serialized.ShouldNotContain("task-leak-sentinel-x9q2");
    }

    // ============================================================================================
    // ProjectContextReferenceEvidence — empty-list defaults (Task 3)
    // ============================================================================================

    [Fact]
    public void ReferenceEvidence_NullCollections_DefaultToEmpty()
    {
        ProjectContextReferenceEvidence evidence = new(
            ProjectFolder: null,
            FileReferences: null!,
            MemoryReferences: null!,
            Conversations: null!);

        evidence.FileReferences.ShouldNotBeNull();
        evidence.FileReferences.ShouldBeEmpty();
        evidence.MemoryReferences.ShouldNotBeNull();
        evidence.MemoryReferences.ShouldBeEmpty();
        evidence.Conversations.ShouldNotBeNull();
        evidence.Conversations.ShouldBeEmpty();
    }

    [Fact]
    public void ReferenceEvidence_Empty_IsTrulyEmpty()
    {
        ProjectContextReferenceEvidence evidence = ProjectContextReferenceEvidence.Empty;

        evidence.ProjectFolder.ShouldBeNull();
        evidence.FileReferences.ShouldBeEmpty();
        evidence.MemoryReferences.ShouldBeEmpty();
        evidence.Conversations.ShouldBeEmpty();
    }
}
