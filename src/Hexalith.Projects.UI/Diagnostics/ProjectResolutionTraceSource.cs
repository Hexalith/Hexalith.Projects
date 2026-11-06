namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.UI.Rendering;

/// <summary>Loads unselected advisory observations through the supported SDK query.</summary>
public sealed class ProjectResolutionTraceSource(ProjectReadQueryClient queries) : IProjectResolutionTraceSource
{
    /// <inheritdoc/>
    public async Task<ProjectResolutionTraceLoadResult> LoadTraceAsync(ProjectResolutionTraceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string? conversation = string.IsNullOrWhiteSpace(request.ConversationId) ? null : request.ConversationId.Trim();
        string[] folders = RawIds(request.FolderIds);
        string[] files = RawIds(request.FileIds);
        int count = folders.Length + files.Length;
        string? error = request.Mode switch
        {
            ProjectResolutionTraceRequest.ConversationMode when conversation is null => "conversation_id_required",
            ProjectResolutionTraceRequest.ConversationMode when count != 0 => "mixed_trace_input",
            ProjectResolutionTraceRequest.AttachmentsMode when conversation is not null => "mixed_trace_input",
            ProjectResolutionTraceRequest.AttachmentsMode when count == 0 => "attachment_ids_required",
            ProjectResolutionTraceRequest.AttachmentsMode when count > 32 => "attachment_limit_exceeded",
            ProjectResolutionTraceRequest.ConversationMode or ProjectResolutionTraceRequest.AttachmentsMode => null,
            _ => "trace_mode_required",
        };
        if (error is not null)
            return ProjectResolutionTraceLoadResult.FromFeedback(ProjectConsoleFeedback.Error(error));
        // Preserve raw entries so duplicate inputs cannot bypass the server's 32-entry limit.
        object payload = request.Mode == ProjectResolutionTraceRequest.ConversationMode
            ? new ResolveProjectFromConversationQuery(conversation!, request.IncludeArchived)
            : new ResolveProjectFromAttachmentsQuery(folders.Select(id => new ProjectAdvisoryAttachment("folder", id))
                .Concat(files.Select(id => new ProjectAdvisoryAttachment("file", id))).ToArray(), request.IncludeArchived);
        string discriminator = request.Mode == ProjectResolutionTraceRequest.ConversationMode
            ? ResolveProjectFromConversationQuery.QueryType : ResolveProjectFromAttachmentsQuery.QueryType;
        ProjectReadResult<ProjectAdvisoryResolutionResponse> response = await queries.QueryAsync<ProjectAdvisoryResolutionResponse>(
            "projects", discriminator, payload, cancellationToken).ConfigureAwait(false);
        return response.Value is null ? ProjectResolutionTraceLoadResult.FromFeedback(response.Feedback!)
            : ProjectResolutionTraceMapper.ToLoadResult(request, folders, files, response.Value);
    }

    private static string[] RawIds(IEnumerable<string> ids)
        => ids.SelectMany(value => value.Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToArray();
}
