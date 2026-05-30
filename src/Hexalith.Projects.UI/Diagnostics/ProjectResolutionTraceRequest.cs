// <copyright file="ProjectResolutionTraceRequest.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

/// <summary>
/// Safe operator input for one compute-on-demand resolution trace query.
/// </summary>
/// <param name="Mode">The trace mode: <see cref="ConversationMode"/> or <see cref="AttachmentsMode"/>.</param>
/// <param name="ConversationId">The conversation id for conversation mode.</param>
/// <param name="FolderIds">Folder ids for attachment mode.</param>
/// <param name="FileIds">File ids for attachment mode.</param>
/// <param name="IncludeArchived">Whether archived Projects should be included by the query.</param>
public sealed record ProjectResolutionTraceRequest(
    string Mode,
    string? ConversationId,
    IReadOnlyList<string> FolderIds,
    IReadOnlyList<string> FileIds,
    bool IncludeArchived)
{
    /// <summary>Conversation trace mode.</summary>
    public const string ConversationMode = "conversation";

    /// <summary>Attachment trace mode.</summary>
    public const string AttachmentsMode = "attachments";

    /// <summary>Creates a conversation trace request.</summary>
    public static ProjectResolutionTraceRequest ForConversation(string? conversationId, bool includeArchived)
        => new(ConversationMode, conversationId, [], [], includeArchived);

    /// <summary>Creates an attachment trace request.</summary>
    public static ProjectResolutionTraceRequest ForAttachments(
        IEnumerable<string>? folderIds,
        IEnumerable<string>? fileIds,
        bool includeArchived)
        => new(
            AttachmentsMode,
            null,
            folderIds?.ToArray() ?? [],
            fileIds?.ToArray() ?? [],
            includeArchived);
}
