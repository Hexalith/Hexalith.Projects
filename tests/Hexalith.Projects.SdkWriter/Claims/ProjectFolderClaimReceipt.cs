// <copyright file="ProjectFolderClaimReceipt.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Claims;

using System;

/// <summary>Durable receipt produced when an AD-35 Folder claim is acquired.</summary>
/// <param name="TenantId">The owning tenant identifier.</param>
/// <param name="FolderId">The claimed Folder identifier.</param>
/// <param name="ProjectId">The holding Project identifier.</param>
/// <param name="Generation">The claim stream version at acquisition (fencing token).</param>
/// <param name="TaskId">The Durable Task identifier that acquired the claim.</param>
/// <param name="TaskStepKey">The deterministic task-step key for claim acquisition.</param>
/// <param name="ReceiptId">The opaque receipt identifier.</param>
/// <param name="AcquiredAt">The UTC timestamp of acquisition.</param>
public sealed record ProjectFolderClaimReceipt(
    string TenantId,
    string FolderId,
    string ProjectId,
    long Generation,
    string TaskId,
    string TaskStepKey,
    string ReceiptId,
    DateTimeOffset AcquiredAt);
