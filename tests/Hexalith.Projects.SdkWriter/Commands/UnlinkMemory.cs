// <copyright file="UnlinkMemory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Commands;

/// <summary>SDK wire command matching the routed v1 <c>UnlinkMemory</c> short name.</summary>
/// <param name="RequestSchemaVersion">The wire schema version.</param>
/// <param name="Operation">The operation discriminator.</param>
/// <param name="UnlinkIntent">The explicit unlink intent.</param>
/// <param name="ProjectId">The Project identifier repeated on the wire payload.</param>
/// <param name="MemoryReferenceId">The Memory reference identifier.</param>
public sealed record UnlinkMemory(
    string? RequestSchemaVersion,
    string? Operation,
    string? UnlinkIntent,
    string? ProjectId,
    string? MemoryReferenceId);
