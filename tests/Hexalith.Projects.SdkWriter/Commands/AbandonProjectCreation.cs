// <copyright file="AbandonProjectCreation.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Commands;

/// <summary>
/// Story 7.1 terminal abandonment fence command committed to a reserved <c>ProjectId</c> stream
/// before releasing an AD-35 Folder claim so no late creation can commit.
/// </summary>
/// <param name="FolderId">The optional Folder identifier whose claim is being released.</param>
/// <param name="FolderClaimGeneration">The optional AD-35 claim generation held when abandoning.</param>
/// <param name="ReasonCode">The Safe Metadata abandonment reason code.</param>
/// <param name="TaskStepKey">The deterministic Durable Task step key.</param>
public sealed record AbandonProjectCreation(
    string? FolderId,
    long? FolderClaimGeneration,
    string ReasonCode,
    string TaskStepKey);
