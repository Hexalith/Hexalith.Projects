namespace Hexalith.Projects.Server.Queries;

using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Projections.ProjectReads;

/// <summary>Thin Projects audit adapter over platform persistence; an audit failure prevents disclosure.</summary>
public sealed class ProjectReadAudit(IReadModelStore store, TimeProvider clock)
{
    private const int IndexPageSize = 128;
    private const string FactPrefix = "projects:read-audit:v1:";

    /// <summary>Persists one stable, metadata-only record before protected fields are returned.</summary>
    public Task<bool> RecordAsync(QueryEnvelope query, bool inspection, int count, CancellationToken cancellationToken)
        => RecordAsync(query, inspection, count, null, cancellationToken);

    /// <summary>Persists an observation-bound metadata audit for ordinary inspection.</summary>
    internal async Task<bool> RecordAsync(QueryEnvelope query, bool inspection, int count, string? observedRevision, CancellationToken cancellationToken)
        => await RecordAsync(query, inspection, count, observedRevision, false, cancellationToken).ConfigureAwait(false);

    /// <summary>Persists the narrower name-only field class for quarantine inspection.</summary>
    internal async Task<bool> RecordAsync(QueryEnvelope query, bool inspection, int count, string? observedRevision, bool quarantined, CancellationToken cancellationToken, IReadOnlyCollection<string>? projectIds = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        string outcome = inspection ? "Inspection" : "SafeDenial";
        bool list = query.QueryType is ListProjectsQuery.QueryType or ListProjectsV2Query.QueryType;
        string fieldClass = inspection ? quarantined || list ? "ProjectName" : "ProjectNameDescriptionSetupReferenceLabels" : "None";
        string surrogate = CreateRequestSurrogate(query, inspection, observedRevision);
        ProjectReadAuditRecord proposed = new(surrogate, outcome, count, fieldClass, clock.GetUtcNow());
        try
        {
            ProjectReadAuditRecord persisted = await ReadModelWritePolicy.UpdateAsync<ProjectReadAuditRecord>(
                store, ProjectReadModelKeys.StoreName, FactPrefix + surrogate,
                current => current ?? proposed, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (persisted.RequestSurrogate != surrogate || persisted.Outcome != outcome
                || persisted.ProjectCount != count || persisted.FieldClass != fieldClass)
            {
                return false;
            }

            foreach (string projectId in projectIds ?? [])
            {
                await IndexAsync(query.TenantId, projectId, surrogate, cancellationToken).ConfigureAwait(false);
            }
            return true;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task IndexAsync(string tenant, string project, string surrogate, CancellationToken token)
    {
        string prefix = IndexPrefix(tenant, project);
        for (int page = 0; ; page++)
        {
            string key = prefix + page.ToString(CultureInfo.InvariantCulture);
            ReadModelEntry<ProjectReadAuditIndexPage> entry = await store.GetAsync<ProjectReadAuditIndexPage>(
                ProjectReadModelKeys.StoreName, key, token).ConfigureAwait(false);
            if (entry.Value is { } existing) ValidatePage(existing);
            if (entry.Value?.Surrogates.Contains(surrogate, StringComparer.Ordinal) == true) return;
            if (entry.Value?.Surrogates.Length == IndexPageSize) continue;
            // A full page is immutable. On conflict the platform reloads before appending or moving on.
            ProjectReadAuditIndexPage saved = await ReadModelWritePolicy.UpdateAsync<ProjectReadAuditIndexPage>(
                store, ProjectReadModelKeys.StoreName, key, current => current switch
                {
                    null => new([surrogate]),
                    _ when current.Surrogates.Length >= IndexPageSize || current.Surrogates.Contains(surrogate, StringComparer.Ordinal) => current,
                    _ => new([.. current.Surrogates, surrogate]),
                }, cancellationToken: token).ConfigureAwait(false);
            if (saved.Surrogates.Contains(surrogate, StringComparer.Ordinal)) return;
        }
    }

    /// <summary>Reads only facts indexed under the authenticated Project in the current Tenant.</summary>
    internal async IAsyncEnumerable<ProjectReadAuditRecord> ReadAsync(string tenant, string project,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        string prefix = IndexPrefix(tenant, project);
        for (int page = 0; ; page++)
        {
            ProjectReadAuditIndexPage? index = (await store.GetAsync<ProjectReadAuditIndexPage>(ProjectReadModelKeys.StoreName,
                prefix + page.ToString(CultureInfo.InvariantCulture), token).ConfigureAwait(false)).Value;
            if (index is null) yield break;
            ValidatePage(index);
            string[] keys = index.Surrogates.Select(id => FactPrefix + id).ToArray();
            IReadOnlyList<ReadModelBulkEntry<ProjectReadAuditRecord>>? entries = null;
            if (store is IReadModelBulkStore bulk)
            {
                entries = await bulk.GetManyAsync<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, keys, 8, token).ConfigureAwait(false);
                if (!entries.Select(entry => entry.Key).SequenceEqual(keys, StringComparer.Ordinal))
                    throw new InvalidDataException("Invalid audit batch.");
            }

            for (int i = 0; i < keys.Length; i++)
            {
                string id = index.Surrogates[i];
                ProjectReadAuditRecord? fact = entries is not null ? entries[i].Value :
                    (await store.GetAsync<ProjectReadAuditRecord>(ProjectReadModelKeys.StoreName, keys[i], token).ConfigureAwait(false)).Value;
                if (fact is null || fact.RequestSurrogate != id || fact.RecordedAt == default || fact.ProjectCount < 0
                    || !(fact.Outcome == "SafeDenial" && fact.FieldClass == "None" && fact.ProjectCount == 0
                        || fact.Outcome == "Inspection" && fact.FieldClass is "ProjectName" or "ProjectNameDescriptionSetupReferenceLabels"))
                    throw new InvalidDataException("Invalid audit fact.");
                yield return fact;
            }
            if (index.Surrogates.Length < IndexPageSize) yield break;
        }
    }

    private static void ValidatePage(ProjectReadAuditIndexPage page)
    {
        if (page.Surrogates is not { Length: > 0 and <= IndexPageSize } ids
            || ids.Any(id => id is not { Length: 64 } || !id.All(char.IsAsciiHexDigitUpper))
            || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new InvalidDataException("Invalid audit index.");
    }

    private static string IndexPrefix(string tenant, string project)
        => "projects:audit-index:v1:" + Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { tenant, project }))) + ":";

    /// <summary>Computes the non-reversible identity used to correlate one durable audit request.</summary>
    internal static string CreateRequestSurrogate(QueryEnvelope query, bool inspection, string? observedRevision = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        string payloadHash = Convert.ToHexString(SHA256.HashData(query.Payload));
        if (query.QueryType == ListProjectsV2Query.QueryType)
        {
            payloadHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { payloadHash, query.Paging })));
        }

        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            query.TenantId, query.OriginalActorId, query.AuthenticatedWorkloadId, query.DelegationId,
            query.IsDelegated, query.Scopes, query.Audience, query.CorrelationId, query.QueryType,
            ObservedRevision = observedRevision, query.AggregateId, query.EntityId, PayloadHash = payloadHash, Outcome = inspection ? "Inspection" : "SafeDenial",
        })));
    }
}
