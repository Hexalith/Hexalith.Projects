// <copyright file="ProjectResolutionTraceSource.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.UI.Rendering;

/// <summary>
/// Generated-client backed source for explicit Project resolution trace queries.
/// </summary>
public sealed class ProjectResolutionTraceSource(IClient client) : IProjectResolutionTraceSource
{
    /// <inheritdoc />
    public async Task<ProjectResolutionTraceLoadResult> LoadTraceAsync(
        ProjectResolutionTraceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? conversationId = Normalize(request.ConversationId);
        string[] folderIds = NormalizeIds(request.FolderIds);
        string[] fileIds = NormalizeIds(request.FileIds);
        ProjectConsoleFeedback? validation = Validate(request, conversationId, folderIds, fileIds);
        if (validation is not null)
        {
            return ProjectResolutionTraceLoadResult.FromFeedback(validation);
        }

        string correlationId = Guid.NewGuid().ToString("N");
        try
        {
            ProjectResolution resolution = request.Mode == ProjectResolutionTraceRequest.ConversationMode
                ? await client.ResolveProjectFromConversationAsync(
                    conversationId!,
                    request.IncludeArchived,
                    correlationId,
                    ReadConsistencyClass.Eventually_consistent,
                    cancellationToken).ConfigureAwait(false)
                : await client.ResolveProjectFromAttachmentsAsync(
                    folderIds,
                    fileIds,
                    request.IncludeArchived,
                    correlationId,
                    ReadConsistencyClass.Eventually_consistent,
                    cancellationToken).ConfigureAwait(false);

            return ProjectResolutionTraceMapper.ToLoadResult(request, folderIds, fileIds, resolution);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 400)
        {
            return ProjectResolutionTraceLoadResult.FromFeedback(ProjectConsoleFeedback.Error("validation_error"));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode is 401 or 403 or 404)
        {
            return ProjectResolutionTraceLoadResult.FromFeedback(ProjectConsoleFeedback.FailClosed("safe_denial"));
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 503)
        {
            return ProjectResolutionTraceLoadResult.FromFeedback(ProjectConsoleFeedback.Warning("data_unavailable"));
        }
        catch (HexalithProjectsApiException)
        {
            return ProjectResolutionTraceLoadResult.FromFeedback(ProjectConsoleFeedback.Error("resolution_trace_query_failed"));
        }
        catch (Exception)
        {
            return ProjectResolutionTraceLoadResult.FromFeedback(ProjectConsoleFeedback.Error("resolution_trace_query_failed"));
        }
    }

    private static ProjectConsoleFeedback? Validate(
        ProjectResolutionTraceRequest request,
        string? conversationId,
        IReadOnlyList<string> folderIds,
        IReadOnlyList<string> fileIds)
    {
        bool hasConversation = !string.IsNullOrWhiteSpace(conversationId);
        bool hasAttachments = folderIds.Count > 0 || fileIds.Count > 0;

        return request.Mode switch
        {
            ProjectResolutionTraceRequest.ConversationMode when !hasConversation => ProjectConsoleFeedback.Error("conversation_id_required"),
            ProjectResolutionTraceRequest.ConversationMode when hasAttachments => ProjectConsoleFeedback.Error("mixed_trace_input"),
            ProjectResolutionTraceRequest.AttachmentsMode when !hasAttachments => ProjectConsoleFeedback.Error("attachment_ids_required"),
            ProjectResolutionTraceRequest.AttachmentsMode when hasConversation => ProjectConsoleFeedback.Error("mixed_trace_input"),
            ProjectResolutionTraceRequest.ConversationMode or ProjectResolutionTraceRequest.AttachmentsMode => null,
            _ => ProjectConsoleFeedback.Error("trace_mode_required"),
        };
    }

    private static string[] NormalizeIds(IEnumerable<string>? ids)
        => ids?.SelectMany(SplitInput)
            .Select(Normalize)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray()
            ?? [];

    private static IEnumerable<string> SplitInput(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
