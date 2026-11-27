// <copyright file="ClaimStepReceiptPayload.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

/// <summary>Durable receipt persisted at Step 2 (<c>Receipts[1]</c>) binding Project, task, step, and AD-35 generation.</summary>
/// <param name="FolderId">The claimed Folder identifier.</param>
/// <param name="ProjectId">The holding Project identifier.</param>
/// <param name="Generation">The winning AD-35 claim stream generation.</param>
/// <param name="ReceiptId">The opaque AD-35 claim receipt identifier.</param>
public sealed record ClaimStepReceiptPayload(
    string FolderId,
    string ProjectId,
    long Generation,
    string ReceiptId);
