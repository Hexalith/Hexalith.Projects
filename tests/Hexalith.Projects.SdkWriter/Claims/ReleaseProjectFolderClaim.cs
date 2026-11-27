// <copyright file="ReleaseProjectFolderClaim.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Claims;

/// <summary>Command to release an AD-35 Folder claim fenced by exact Project and generation.</summary>
/// <param name="ProjectId">The Project holding the claim.</param>
/// <param name="ExpectedGeneration">The exact claim generation to release.</param>
/// <param name="TaskId">The Durable Task identifier.</param>
/// <param name="TaskStepKey">The deterministic task-step key for the release.</param>
/// <param name="ReasonCode">The Safe Metadata reason code.</param>
public sealed record ReleaseProjectFolderClaim(
    string ProjectId,
    long ExpectedGeneration,
    string TaskId,
    string TaskStepKey,
    string ReasonCode);
