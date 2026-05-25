// <copyright file="LinkedSourcePolicy.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Closed v1 policy for linked-source behavior at conversation start.
/// </summary>
[JsonConverter(typeof(LinkedSourcePolicyJsonConverter))]
public enum LinkedSourcePolicy
{
    /// <summary>No linked sources are selected by default.</summary>
    None,

    /// <summary>Only Projects-owned metadata may be selected by default.</summary>
    ProjectsOwnedMetadataOnly,

    /// <summary>Authorized linked references may be selected by default.</summary>
    AuthorizedReferences,
}

/// <summary>
/// Writes the linked-source policy vocabulary in lower camel-case to match the OpenAPI spine.
/// </summary>
public sealed class LinkedSourcePolicyJsonConverter : JsonStringEnumConverter<LinkedSourcePolicy>
{
    /// <summary>Initializes a new instance of the <see cref="LinkedSourcePolicyJsonConverter"/> class.</summary>
    public LinkedSourcePolicyJsonConverter()
        : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    {
    }
}
