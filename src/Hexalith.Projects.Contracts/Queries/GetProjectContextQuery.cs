// <copyright file="GetProjectContextQuery.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

/// <summary>Identifies the supported Get Project Context query.</summary>
/// <param name="ProjectId">The opaque Project identifier targeted by the query.</param>
[Hexalith.EventStore.Contracts.Queries.QueryResultContract(typeof(ProjectContextReadResponse), "projects", "Hexalith.Projects.Queries.GetProjectContext.v1")]
public sealed record GetProjectContextQuery(string ProjectId)
{
    /// <summary>Gets the named Conversation whose current native membership authorizes context use.</summary>
    public string? ConversationId { get; init; }
}
