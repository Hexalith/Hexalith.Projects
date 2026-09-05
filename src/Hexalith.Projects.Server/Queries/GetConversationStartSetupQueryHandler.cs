// <copyright file="GetConversationStartSetupQueryHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Queries;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;

/// <summary>Handles the supported Conversation-start setup query over the tenant-scoped detail model.</summary>
public sealed class GetConversationStartSetupQueryHandler(IProjectDetailReadModel detailReadModel) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IProjectDetailReadModel _detailReadModel = detailReadModel ?? throw new ArgumentNullException(nameof(detailReadModel));

    /// <inheritdoc/>
    public string Domain => ProjectsServerModule.DomainName;

    /// <inheritdoc/>
    public string QueryType => ProjectsServerModule.GetConversationStartSetupQueryType;

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string projectId = query.EntityId ?? query.AggregateId;
        if (string.IsNullOrWhiteSpace(query.TenantId)
            || string.IsNullOrWhiteSpace(query.UserId)
            || string.IsNullOrWhiteSpace(projectId))
        {
            return QueryResult.Failure("safe-denial");
        }

        ProjectDetailItem? detail = await _detailReadModel
            .GetAsync(query.TenantId, projectId, cancellationToken)
            .ConfigureAwait(false);
        if (detail is null || detail.Lifecycle != ProjectLifecycle.Active)
        {
            return QueryResult.Failure("safe-denial");
        }

        DateTimeOffset asOf = detail.UpdatedAt;
        bool hasFolder = detail.ProjectFolder?.ReferenceState == ReferenceState.Included;
        EvidenceFreshnessState evidenceFreshness = hasFolder
            ? EvidenceFreshnessState.Current
            : EvidenceFreshnessState.Unavailable;
        ConversationStartResponseState responseState = hasFolder
            ? ConversationStartResponseState.Complete
            : ConversationStartResponseState.Unavailable;
        IReadOnlyList<string> recoveryActions = hasFolder
            ? Array.Empty<string>()
            : new[] { "RefreshContext", "ContactAdministrator" };

        ConversationStartSetup setup = ConversationStartSetup.Empty(
            detail.ProjectId,
            detail.Lifecycle,
            asOf,
            hasFolder ? ProjectContextFreshness.Fresh : ProjectContextFreshness.Unavailable);
        if (detail.Setup is not null)
        {
            setup = new ConversationStartSetup(
                detail.ProjectId,
                detail.Lifecycle,
                detail.Setup.Goals,
                detail.Setup.UserInstructions,
                detail.Setup.PreferredSourceKinds,
                detail.Setup.ExcludedSourceKinds,
                detail.Setup.ConversationStartDefaults?.LinkedSourcePolicy ?? LinkedSourcePolicy.None,
                asOf,
                hasFolder ? ProjectContextFreshness.Fresh : ProjectContextFreshness.Unavailable);
        }

        var snapshot = new ConversationStartAdmissionSnapshot(
            responseState,
            asOf,
            detail.Sequence,
            new[]
            {
                new ConversationStartComponent("Project", true, EvidenceFreshnessState.Current, "current"),
                new ConversationStartComponent("Folder", hasFolder, evidenceFreshness, hasFolder ? "current" : "missing"),
                new ConversationStartComponent("Setup", true, EvidenceFreshnessState.Current, "current"),
                new ConversationStartComponent("FirstResponseAuthorization", true, EvidenceFreshnessState.Current, "envelope-authorized"),
            },
            recoveryActions);
        var response = new ConversationStartSetupResponse(
            responseState == ConversationStartResponseState.Unavailable ? null : setup,
            snapshot);

        return QueryResult.FromPayload(JsonSerializer.SerializeToElement(response, JsonOptions), QueryType);
    }
}
