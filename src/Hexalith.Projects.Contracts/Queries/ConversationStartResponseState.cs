// <copyright file="ConversationStartResponseState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System.Text.Json.Serialization;

/// <summary>Admission state for a Conversation-start setup response.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ConversationStartResponseState>))]
public enum ConversationStartResponseState
{
    /// <summary>All required evidence is current and the setup may be used.</summary>
    Complete,

    /// <summary>Required evidence is current and optional omissions are explicit.</summary>
    Partial,

    /// <summary>Required evidence is not current or is unavailable.</summary>
    Unavailable,

    /// <summary>The target is denied and no protected response is disclosed.</summary>
    Denied,
}
