// <copyright file="ConversationStartSetupResponse.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Hexalith.Projects.Contracts.Models;

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

/// <summary>Metadata-only evidence for one Conversation-start response component.</summary>
/// <param name="Name">The stable component name.</param>
/// <param name="Included">Whether the component is present.</param>
/// <param name="Freshness">The component freshness.</param>
/// <param name="Reason">A bounded safe reason code.</param>
public sealed record ConversationStartComponent(string Name, bool Included, EvidenceFreshnessState Freshness, string Reason);

/// <summary>AD-32 response snapshot governing Conversation-start setup usability.</summary>
/// <param name="ResponseState">The aggregate response state.</param>
/// <param name="AsOf">The persisted read-model observation instant.</param>
/// <param name="ProjectVersion">The authorized Project read-model version.</param>
/// <param name="Components">Metadata-only component evidence.</param>
/// <param name="RecoveryActions">Bounded recovery actions.</param>
public sealed record ConversationStartAdmissionSnapshot(
    ConversationStartResponseState ResponseState,
    DateTimeOffset AsOf,
    long ProjectVersion,
    IReadOnlyList<ConversationStartComponent> Components,
    IReadOnlyList<string> RecoveryActions);

/// <summary>Supported Conversation-start setup response.</summary>
/// <param name="Setup">The bounded setup subset, or null when unavailable or denied.</param>
/// <param name="Snapshot">The AD-32 admission snapshot.</param>
public sealed record ConversationStartSetupResponse(ConversationStartSetup? Setup, ConversationStartAdmissionSnapshot Snapshot);
