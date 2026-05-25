// <copyright file="ProjectContextSourceKind.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Closed v1 source-kind vocabulary for metadata-only Project setup preferences.
/// </summary>
[JsonConverter(typeof(ProjectContextSourceKindJsonConverter))]
public enum ProjectContextSourceKind
{
    /// <summary>Conversation metadata and references.</summary>
    Conversation,

    /// <summary>Project-owned folder metadata.</summary>
    ProjectFolder,

    /// <summary>File reference metadata.</summary>
    FileReference,

    /// <summary>Memory reference metadata.</summary>
    Memory,
}

/// <summary>
/// Writes the setup source-kind vocabulary in lower camel-case to match the OpenAPI spine.
/// </summary>
public sealed class ProjectContextSourceKindJsonConverter : JsonStringEnumConverter<ProjectContextSourceKind>
{
    /// <summary>Initializes a new instance of the <see cref="ProjectContextSourceKindJsonConverter"/> class.</summary>
    public ProjectContextSourceKindJsonConverter()
        : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    {
    }
}
