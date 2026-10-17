namespace Hexalith.Projects.Server.Queries;

using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Server.Projections.ProjectReads;

/// <summary>Thin Projects audit adapter over platform persistence; an audit failure prevents disclosure.</summary>
public sealed class ProjectReadAudit(IReadModelStore store, TimeProvider clock)
{
    /// <summary>Persists one stable, metadata-only record before protected fields are returned.</summary>
    public Task<bool> RecordAsync(QueryEnvelope query, bool inspection, int count, CancellationToken cancellationToken)
        => RecordAsync(query, inspection, count, null, cancellationToken);

    internal async Task<bool> RecordAsync(QueryEnvelope query, bool inspection, int count, string? observedRevision, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        string outcome = inspection ? "Inspection" : "SafeDenial";
        string fieldClass = inspection ? query.QueryType == ListProjectsQueryType ? "ProjectName" : "ProjectNameDescriptionSetupReferenceLabels" : "None";
        string surrogate = CreateRequestSurrogate(query, inspection, observedRevision);
        ProjectReadAuditRecord proposed = new(surrogate, outcome, count, fieldClass, clock.GetUtcNow());
        try
        {
            ProjectReadAuditRecord persisted = await ReadModelWritePolicy.UpdateAsync<ProjectReadAuditRecord>(
                store, ProjectReadModelKeys.StoreName, $"projects:read-audit:v1:{surrogate}",
                current => current ?? proposed, cancellationToken: cancellationToken).ConfigureAwait(false);
            return persisted.RequestSurrogate == surrogate && persisted.Outcome == outcome
                && persisted.ProjectCount == count && persisted.FieldClass == fieldClass;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>Computes the non-reversible identity used to correlate one durable audit request.</summary>
    internal static string CreateRequestSurrogate(QueryEnvelope query, bool inspection, string? observedRevision = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            query.TenantId, query.OriginalActorId, query.AuthenticatedWorkloadId, query.DelegationId,
            query.IsDelegated, query.Scopes, query.Audience, query.CorrelationId, query.QueryType,
            ObservedRevision = observedRevision, query.AggregateId, query.EntityId, PayloadHash = Convert.ToHexString(SHA256.HashData(query.Payload)), Outcome = inspection ? "Inspection" : "SafeDenial",
        })));
    }

    private const string ListProjectsQueryType = Hexalith.Projects.Contracts.Queries.ListProjectsQuery.QueryType;
}
