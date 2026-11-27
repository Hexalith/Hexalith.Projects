// <copyright file="AcquireProjectFolderClaim.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Claims;

/// <summary>Command to atomically acquire the exclusive AD-35 Folder claim for a Project.</summary>
/// <param name="ProjectId">The reserved Project identifier requesting the claim.</param>
/// <param name="TaskId">The Durable Task identifier.</param>
/// <param name="TaskStepKey">The deterministic task-step key.</param>
public sealed record AcquireProjectFolderClaim(
    string ProjectId,
    string TaskId,
    string TaskStepKey);
