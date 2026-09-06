// <copyright file="ExplainContextSelectionQueryHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Queries;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Queries;

/// <summary>Handles the supported Explain Context Selection query over persisted Project detail.</summary>
public sealed class ExplainContextSelectionQueryHandler(ProjectContextQueryExecutor executor) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ProjectContextQueryExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));

    /// <inheritdoc/>
    public string Domain => ProjectsServerModule.DomainName;

    /// <inheritdoc/>
    public string QueryType => ProjectsServerModule.ExplainContextSelectionQueryType;

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!TryReadProjectId(query, out string projectId))
        {
            return QueryResult.Failure("safe-denial");
        }

        ProjectContextAdmission admission = await _executor
            .ExecuteAsync(query, projectId, ProjectContextOperationKind.Explain, cancellationToken)
            .ConfigureAwait(false);
        if (admission.IsSafeDenial)
        {
            return QueryResult.Failure("safe-denial");
        }

        return QueryResult.FromPayload(
            JsonSerializer.SerializeToElement(admission.ToExplanation(), JsonOptions),
            QueryType);
    }

    private static bool TryReadProjectId(QueryEnvelope query, out string projectId)
    {
        projectId = string.Empty;
        if (query.Payload is not { Length: > 0 })
        {
            return false;
        }

        try
        {
            ExplainContextSelectionQuery? parsed = JsonSerializer.Deserialize<ExplainContextSelectionQuery>(query.Payload, JsonOptions);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.ProjectId))
            {
                return false;
            }

            projectId = parsed.ProjectId;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
