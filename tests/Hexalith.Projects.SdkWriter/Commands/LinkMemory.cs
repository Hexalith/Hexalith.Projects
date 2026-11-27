// <copyright file="LinkMemory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Commands;

using Hexalith.Projects.Contracts.Models;

/// <summary>SDK wire command matching the routed v1 <c>LinkMemory</c> short name.</summary>
/// <param name="RequestSchemaVersion">The wire schema version.</param>
/// <param name="Operation">The operation discriminator.</param>
/// <param name="ProjectId">The Project identifier repeated on the wire payload.</param>
/// <param name="MemoryReferenceId">The Memory reference identifier.</param>
/// <param name="MemoryMetadata">The validated Memory metadata.</param>
public sealed record LinkMemory(
    string? RequestSchemaVersion,
    string? Operation,
    string? ProjectId,
    string? MemoryReferenceId,
    ProjectMemoryReferenceMetadata? MemoryMetadata);
