namespace Hexalith.Projects.Server.Queries;

using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectAuditTimeline;
using Hexalith.Projects.Projections.ProjectList;

/// <summary>Projects confirmed facts directly from the bound Project stream, including retained command refusals.</summary>
internal static class ProjectCommittedAuditHistory
{
    private static readonly IReadOnlyDictionary<string, Type> EventTypes = new[]
    {
        typeof(ProjectCreated), typeof(ProjectSetupUpdated), typeof(ProjectArchived), typeof(ProjectRestored),
        typeof(ProjectFolderSet), typeof(FileReferenceLinked), typeof(FileReferenceUnlinked),
        typeof(MemoryLinked), typeof(MemoryUnlinked), typeof(ProjectResolutionConfirmed),
        typeof(ProjectChatBotAuthorityGranted), typeof(ProjectChatBotAuthorityRevoked),
        typeof(ProjectResolutionConfirmationRejected), typeof(ProjectArchiveRejected), typeof(ProjectRestoreRejected),
        typeof(ProjectSetupUpdateRejected), typeof(ProjectCreationRejected), typeof(ProjectReferenceLinkRejected),
        typeof(ProjectReferenceUnlinkRejected),
    }.ToDictionary(type => type.FullName!, StringComparer.Ordinal);

    /// <summary>Reads the verified prefix. An unavailable stream permits the retained journal to supply its historical subset.</summary>
    internal static async Task<IReadOnlyList<ProjectAuditFact>?> ReadAsync(IAuthoritativeEventStreamReader reader,
        string tenant, string project, long authorizedSequence, TimeProvider clock, CancellationToken token)
    {
        var identity = new AggregateIdentity(tenant, ProjectsServerModule.DomainName, project);
        if (identity.TenantId != tenant) throw new InvalidDataException("Invalid stream scope.");
        AuthoritativeStreamReadResult read = await reader.ReadAsync(identity, token).ConfigureAwait(false);
        if (!read.IsAuthoritative || read.Stream is not { } stream) return null;
        if (stream.Identity != identity || stream.ObservedAt == default || stream.ObservedAt > clock.GetUtcNow()
            || stream.Head != authorizedSequence || stream.Head != stream.Events.Count)
            throw new InvalidDataException("Invalid audit stream.");

        List<ProjectAuditFact> facts = [];
        long sequence = 0;
        foreach (StreamReadEvent item in stream.Events)
        {
            token.ThrowIfCancellationRequested();
            if (item.SequenceNumber != ++sequence) throw new InvalidDataException("Invalid stream position.");
            string eventType = item.EventTypeName.Split(',', 2)[0].Trim();
            if (!EventTypes.TryGetValue(eventType, out Type? type)) continue;
            if (!string.Equals(item.SerializationFormat, "json", StringComparison.OrdinalIgnoreCase)
                || item.Timestamp == default || string.IsNullOrWhiteSpace(item.MessageId))
                throw new InvalidDataException("Invalid audit event.");

            object? value = JsonSerializer.Deserialize(item.Payload, type, JsonSerializerOptions.Web);
            ProjectAuditFact? fact;
            if (value is IProjectEvent mutation)
            {
                if (mutation.TenantId != tenant || mutation.ProjectId != project || mutation.OccurredAt == default)
                    throw new InvalidDataException("Invalid audit mutation.");
                fact = ProjectAuditTimelineProjection.ToSupportedFact(new ProjectProjectionEnvelope(tenant, item.GlobalPosition, mutation));
            }
            else
            {
                fact = FromRejection(value, item, tenant, project);
            }
            if (fact is not null) facts.Add(fact);
        }
        return facts;
    }

    private static ProjectAuditFact? FromRejection(object? value, StreamReadEvent item, string tenant, string project)
    {
        var (owner, target, reason) = value switch
        {
            ProjectResolutionConfirmationRejected e => (e.TenantId, e.ProjectId?.Value, e.Reason),
            ProjectCreationRejected e => (e.TenantId, e.ProjectId?.Value, e.Reason),
            ProjectArchiveRejected e => (e.TenantId, e.ProjectId?.Value, e.Reason),
            ProjectRestoreRejected e => (e.TenantId, e.ProjectId?.Value, e.Reason),
            ProjectSetupUpdateRejected e => (e.TenantId, e.ProjectId?.Value, e.Reason),
            ProjectReferenceLinkRejected e => (e.TenantId, e.ProjectId?.Value, e.Reason),
            ProjectReferenceUnlinkRejected e => (e.TenantId, e.ProjectId?.Value, e.Reason),
            _ => throw new InvalidDataException("Invalid audit rejection."),
        };
        if (owner != tenant || (target != project && !(target is null && value is ProjectCreationRejected)) || !Enum.IsDefined(reason))
            throw new InvalidDataException("Invalid rejection scope.");
        if (target is null) return null;

        bool confirmation = value is ProjectResolutionConfirmationRejected;
        if (!confirmation && reason != ReferenceState.Unauthorized) return null;
        string operation = confirmation ? "project.resolution_confirmation_rejected" : "authorization.denied";
        string id = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { tenant, project, operation, item.MessageId })));
        return new("audit_" + id, operation, confirmation ? "Rejected" : "SafeDenial", item.Timestamp);
    }
}
