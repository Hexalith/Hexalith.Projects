// <copyright file="ProjectCreationWorkflowResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

/// <summary>Safe Metadata result returned by <see cref="FolderFirstProjectCreationWorkflow"/>.</summary>
/// <param name="Status">The workflow status.</param>
/// <param name="HttpStatusCode">The mapped HTTP status code.</param>
/// <param name="TaskId">The Durable Task identifier when admitted, or null.</param>
/// <param name="ProjectId">The Project identifier when visible/confirmed, or null while hidden/denied.</param>
/// <param name="FolderId">The bound Folder identifier when established, or null.</param>
/// <param name="FolderClaimGeneration">The winning AD-35 claim generation when established, or null.</param>
/// <param name="FolderProvisioningReceiptId">The durable Folders provisioning receipt identifier, or null.</param>
/// <param name="RejectedField">The rejected field NAME only (never its value), or null.</param>
/// <param name="ReasonCode">The Safe Metadata reason code, or null.</param>
public sealed record ProjectCreationWorkflowResult(
    ProjectCreationWorkflowStatus Status,
    int HttpStatusCode,
    string? TaskId = null,
    string? ProjectId = null,
    string? FolderId = null,
    long? FolderClaimGeneration = null,
    string? FolderProvisioningReceiptId = null,
    string? RejectedField = null,
    string? ReasonCode = null);
