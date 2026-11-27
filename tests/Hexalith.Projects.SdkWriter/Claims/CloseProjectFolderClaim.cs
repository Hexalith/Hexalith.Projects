// <copyright file="CloseProjectFolderClaim.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Claims;

/// <summary>Command to permanently close/quarantine an AD-35 Folder claim key with uncertain or conflicting history.</summary>
/// <param name="ReasonCode">The Safe Metadata reason code.</param>
/// <param name="TaskId">Optional task identifier.</param>
/// <param name="TaskStepKey">Optional task-step key.</param>
public sealed record CloseProjectFolderClaim(
    string ReasonCode,
    string? TaskId = null,
    string? TaskStepKey = null);
