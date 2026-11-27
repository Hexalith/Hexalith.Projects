// <copyright file="ConfirmProjectResolution.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Commands;

/// <summary>SDK wire command matching the routed v1 <c>ConfirmProjectResolution</c> short name.</summary>
/// <param name="RequestSchemaVersion">The wire schema version.</param>
/// <param name="Operation">The operation discriminator.</param>
/// <param name="ProjectId">The Project identifier repeated on the wire payload.</param>
/// <param name="ConversationId">The Conversation identifier.</param>
/// <param name="SourceProjectId">The optional source Project identifier.</param>
/// <param name="ResolutionResult">The resolution discriminator.</param>
public sealed record ConfirmProjectResolution(
    string? RequestSchemaVersion,
    string? Operation,
    string? ProjectId,
    string? ConversationId,
    string? SourceProjectId,
    string? ResolutionResult);
