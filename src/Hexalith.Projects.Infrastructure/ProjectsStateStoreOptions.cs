// <copyright file="ProjectsStateStoreOptions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

/// <summary>
/// Dapr state-store adapter options.
/// </summary>
public sealed class ProjectsStateStoreOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Projects:StateStore";

    /// <summary>Gets the default options.</summary>
    public static ProjectsStateStoreOptions Default { get; } = new();

    /// <summary>Gets or sets the Dapr state-store component name.</summary>
    public string StateStoreName { get; set; } = "statestore";
}
