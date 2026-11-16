// <copyright file="GetConversationStartSetupQuery.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

/// <summary>Identifies the supported Conversation-start setup query.</summary>
/// <param name="ProjectId">The opaque Project identifier targeted by the query.</param>
[Hexalith.EventStore.Contracts.Queries.QueryResultContract(typeof(ConversationStartSetupResponse), "projects", "Hexalith.Projects.Queries.GetConversationStartSetup.v1")]
public sealed record GetConversationStartSetupQuery(string ProjectId)
{
    /// <summary>Gets the named Conversation whose current native membership authorizes context use.</summary>
    public string? ConversationId { get; init; }
}
