// <copyright file="ProjectContextEvidenceBuilder.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Testing.Context;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Authorization;
using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;

/// <summary>
/// Reusable, deterministic builders for the inputs <c>ProjectContextInclusionPolicy.Assemble(...)</c>
/// consumes. Used by Story 3.1 Tier-1 tests to keep fixture wiring short and readable.
/// </summary>
public static class ProjectContextEvidenceBuilder
{
    /// <summary>The default tenant identifier used by tests.</summary>
    public const string DefaultTenant = "acme";

    /// <summary>The default project identifier used by tests.</summary>
    public const string DefaultProjectId = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    /// <summary>The default deterministic <c>Now</c> used by tests.</summary>
    public static readonly DateTimeOffset DefaultNow = new(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Builds a deterministic <see cref="ProjectContextAssemblyContext"/>.</summary>
    /// <param name="authoritativeTenantId">The authoritative tenant identifier, or <see langword="null"/>.</param>
    /// <param name="requestedTenantId">The requested tenant identifier, or <see langword="null"/>.</param>
    /// <param name="projectId">The requested project identifier, or <see langword="null"/>.</param>
    /// <param name="operationKind">The operation kind (defaults to <see cref="ProjectContextOperationKind.Get"/>).</param>
    /// <param name="now">The deterministic <c>Now</c>; falls back to <see cref="DefaultNow"/>.</param>
    /// <returns>A populated assembly context.</returns>
    public static ProjectContextAssemblyContext Context(
        string? authoritativeTenantId = DefaultTenant,
        string? requestedTenantId = DefaultTenant,
        string? projectId = DefaultProjectId,
        ProjectContextOperationKind operationKind = ProjectContextOperationKind.Get,
        DateTimeOffset? now = null)
        => new(
            authoritativeTenantId,
            requestedTenantId,
            projectId,
            operationKind,
            CorrelationId: "corr-001",
            TaskId: "task-001",
            Now: now ?? DefaultNow);

    /// <summary>Builds a <see cref="ProjectDetailItem"/> wrapped as <see cref="ProjectContextProjectEvidence"/>.</summary>
    /// <param name="tenantId">The owning tenant identifier (defaults to <see cref="DefaultTenant"/>).</param>
    /// <param name="projectId">The project identifier (defaults to <see cref="DefaultProjectId"/>).</param>
    /// <param name="lifecycle">The project lifecycle (defaults to <see cref="ProjectLifecycle.Active"/>).</param>
    /// <param name="includeFolderInDetail">Whether to inline an included Project Folder reference on the detail.</param>
    /// <returns>The wrapped projection evidence.</returns>
    public static ProjectContextProjectEvidence Project(
        string tenantId = DefaultTenant,
        string projectId = DefaultProjectId,
        ProjectLifecycle lifecycle = ProjectLifecycle.Active,
        bool includeFolderInDetail = false)
    {
        ProjectFolderReference? folder = includeFolderInDetail
            ? new ProjectFolderReference(
                FolderId: "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
                DisplayName: "Tracer Folder",
                ReferenceState: ReferenceState.Included,
                ReasonCode: null,
                ObservedAt: DefaultNow)
            : null;

        ProjectDetailItem detail = new(
            tenantId,
            projectId,
            Name: "Tracer Bullet",
            Description: "A safe description",
            SetupMetadata: null,
            Setup: null,
            ProjectFolder: folder,
            FileReferences: Array.Empty<ProjectFileReference>(),
            MemoryReferences: Array.Empty<ProjectMemoryReference>(),
            Lifecycle: lifecycle,
            CreatedAt: DefaultNow,
            UpdatedAt: DefaultNow,
            Sequence: 1);

        return new ProjectContextProjectEvidence(detail);
    }

    /// <summary>Builds the typed-null projection evidence (project not visible).</summary>
    /// <returns>The null-projection evidence (safe-denial 404 driver).</returns>
    public static ProjectContextProjectEvidence ProjectMissing()
        => new(Detail: null);

    /// <summary>Builds a tenant-access wrapper around the supplied authorization outcome.</summary>
    /// <param name="outcome">The Story 1.6 tenant-access outcome.</param>
    /// <param name="freshness">The Story 1.6 projection freshness status.</param>
    /// <param name="tenantId">The tenant identifier echoed onto the result (defaults to <see cref="DefaultTenant"/>).</param>
    /// <returns>The wrapped tenant-access evidence.</returns>
    public static ProjectContextTenantAccess TenantAccess(
        TenantAccessOutcome outcome = TenantAccessOutcome.Allowed,
        TenantProjectionFreshnessStatus freshness = TenantProjectionFreshnessStatus.Fresh,
        string tenantId = DefaultTenant)
        => new(new TenantAccessAuthorizationResult(
            outcome,
            Code: outcome.ToString(),
            TenantId: tenantId,
            ProjectionWatermark: $"{tenantId}:1",
            LastEventTimestamp: DefaultNow,
            ProjectionAge: TimeSpan.Zero,
            FreshnessStatus: freshness,
            Source: "test"));

    /// <summary>Builds a fully empty reference evidence input.</summary>
    /// <returns>Empty reference evidence.</returns>
    public static ProjectContextReferenceEvidence NoReferences() => ProjectContextReferenceEvidence.Empty;

    /// <summary>Builds a reference evidence with a single Project Folder candidate.</summary>
    /// <param name="state">The reference state to inject.</param>
    /// <param name="folderId">The folder identifier, or <see langword="null"/> for pending.</param>
    /// <returns>The reference evidence with a single folder candidate.</returns>
    public static ProjectContextReferenceEvidence WithFolder(
        ReferenceState state = ReferenceState.Included,
        string? folderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC")
        => new(
            ProjectFolder: new ProjectFolderReference(
                FolderId: folderId,
                DisplayName: "Tracer Folder",
                ReferenceState: state,
                ReasonCode: null,
                ObservedAt: DefaultNow),
            FileReferences: Array.Empty<ProjectFileReference>(),
            MemoryReferences: Array.Empty<ProjectMemoryReference>(),
            Conversations: Array.Empty<ProjectContextConversationEvidence>());

    /// <summary>Builds a reference evidence with a single file candidate in the supplied state.</summary>
    /// <param name="state">The reference state to inject.</param>
    /// <param name="fileId">The file-reference identifier.</param>
    /// <returns>The reference evidence with a single file candidate.</returns>
    public static ProjectContextReferenceEvidence WithFile(
        ReferenceState state = ReferenceState.Included,
        string fileId = "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1")
        => new(
            ProjectFolder: null,
            FileReferences: [
                new ProjectFileReference(
                    FileReferenceId: fileId,
                    FolderId: "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
                    DisplayName: "contract.pdf",
                    ReferenceState: state,
                    ReasonCode: null,
                    ObservedAt: DefaultNow),
            ],
            MemoryReferences: Array.Empty<ProjectMemoryReference>(),
            Conversations: Array.Empty<ProjectContextConversationEvidence>());

    /// <summary>Builds a reference evidence with a single memory candidate in the supplied state.</summary>
    /// <param name="state">The reference state to inject.</param>
    /// <param name="memoryId">The memory-reference identifier.</param>
    /// <returns>The reference evidence with a single memory candidate.</returns>
    public static ProjectContextReferenceEvidence WithMemory(
        ReferenceState state = ReferenceState.Included,
        string memoryId = "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1")
        => new(
            ProjectFolder: null,
            FileReferences: Array.Empty<ProjectFileReference>(),
            MemoryReferences: [
                new ProjectMemoryReference(
                    MemoryReferenceId: memoryId,
                    DisplayName: "Q3 product strategy memory",
                    ReferenceState: state,
                    ReasonCode: null,
                    ObservedAt: DefaultNow),
            ],
            Conversations: Array.Empty<ProjectContextConversationEvidence>());

    /// <summary>Builds a reference evidence with a single conversation candidate.</summary>
    /// <param name="trustSignal">The conversation trust signal to inject.</param>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <returns>The reference evidence with a single conversation candidate.</returns>
    public static ProjectContextReferenceEvidence WithConversation(
        ProjectConversationTrustSignal trustSignal = ProjectConversationTrustSignal.Current,
        string conversationId = "01HZ9K8YQ3W6V2N4R7T5P0X1AC")
        => new(
            ProjectFolder: null,
            FileReferences: Array.Empty<ProjectFileReference>(),
            MemoryReferences: Array.Empty<ProjectMemoryReference>(),
            Conversations: [
                new ProjectContextConversationEvidence(
                    ConversationId: conversationId,
                    DisplayLabel: "Synthetic conversation reference",
                    TrustSignal: trustSignal,
                    LastCheckedAt: DefaultNow),
            ]);

    /// <summary>Builds a reference evidence with one candidate of each allowlisted kind.</summary>
    /// <returns>The reference evidence with one folder/file/memory/conversation each.</returns>
    public static ProjectContextReferenceEvidence WithAllKinds()
        => new(
            ProjectFolder: new ProjectFolderReference(
                FolderId: "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
                DisplayName: "Tracer Folder",
                ReferenceState: ReferenceState.Included,
                ReasonCode: null,
                ObservedAt: DefaultNow),
            FileReferences: [
                new ProjectFileReference(
                    FileReferenceId: "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1",
                    FolderId: "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
                    DisplayName: "contract.pdf",
                    ReferenceState: ReferenceState.Included,
                    ReasonCode: null,
                    ObservedAt: DefaultNow),
            ],
            MemoryReferences: [
                new ProjectMemoryReference(
                    MemoryReferenceId: "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1",
                    DisplayName: "Q3 product strategy memory",
                    ReferenceState: ReferenceState.Included,
                    ReasonCode: null,
                    ObservedAt: DefaultNow),
            ],
            Conversations: [
                new ProjectContextConversationEvidence(
                    ConversationId: "01HZ9K8YQ3W6V2N4R7T5P0X1AC",
                    DisplayLabel: "Synthetic conversation reference",
                    TrustSignal: ProjectConversationTrustSignal.Current,
                    LastCheckedAt: DefaultNow),
            ]);
}
