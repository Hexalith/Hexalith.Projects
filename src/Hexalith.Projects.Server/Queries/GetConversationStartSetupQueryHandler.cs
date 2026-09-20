// <copyright file="GetConversationStartSetupQueryHandler.cs" company="Hexalith">
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
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Handles the supported Conversation-start setup query over the persisted Conversation-start projection.</summary>
public sealed class GetConversationStartSetupQueryHandler(ProjectContextQueryExecutor executor) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ProjectContextQueryExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));

    /// <inheritdoc/>
    public string Domain => ProjectsServerModule.DomainName;

    /// <inheritdoc/>
    public string QueryType => ProjectsServerModule.GetConversationStartSetupQueryType;

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string projectId = query.EntityId ?? query.AggregateId;
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return QueryResult.Failure("safe-denial");
        }

        ProjectContextAdmission admission = await _executor
            .ExecuteAsync(query, projectId, ProjectContextOperationKind.Get, cancellationToken)
            .ConfigureAwait(false);
        if (admission.IsSafeDenial)
        {
            return QueryResult.Failure("safe-denial");
        }

        ProjectSetup? projectSetup = admission.Setup;
        ConversationStartSetup? setup = projectSetup is null
            ? null
            : new ConversationStartSetup(
                admission.ProjectId,
                admission.Lifecycle,
                projectSetup.Goals,
                projectSetup.UserInstructions,
                projectSetup.PreferredSourceKinds,
                projectSetup.ExcludedSourceKinds,
                projectSetup.ConversationStartDefaults?.LinkedSourcePolicy ?? LinkedSourcePolicy.None,
                admission.Snapshot.AsOf,
                ProjectContextFreshness.Fresh);
        var response = new ConversationStartSetupResponse(
            setup,
            ConversationStartAdmissionSnapshot.FromShared(admission.Snapshot));

        return QueryResult.FromPayload(JsonSerializer.SerializeToElement(response, JsonOptions), QueryType);
    }
}
