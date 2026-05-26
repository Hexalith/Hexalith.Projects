// <copyright file="ProjectConversationTrustSignal.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System.Text.Json.Serialization;

/// <summary>
/// Projects-owned trust/freshness signal for conversation reference reads.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectConversationTrustSignal>))]
public enum ProjectConversationTrustSignal
{
    /// <summary>The upstream projection and hydration evidence are current.</summary>
    Current,

    /// <summary>The read may be incomplete because upstream projection evidence is stale.</summary>
    Stale,

    /// <summary>The upstream read model is rebuilding.</summary>
    Rebuilding,

    /// <summary>The upstream read model is unavailable.</summary>
    Unavailable,

    /// <summary>The caller is not authorized to see upstream data.</summary>
    Forbidden,

    /// <summary>Some upstream metadata was policy-redacted.</summary>
    Redacted,

    /// <summary>The upstream list detected mixed projection generations.</summary>
    MixedGeneration,
}
