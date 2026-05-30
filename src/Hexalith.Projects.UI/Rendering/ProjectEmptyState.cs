// <copyright file="ProjectEmptyState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Rendering;

/// <summary>
/// Data-only Projects empty-state description for generated and composed views.
/// </summary>
/// <param name="Category">The stable empty-state category.</param>
/// <param name="EntityPlural">The entity plural rendered by FrontComposer's empty placeholder.</param>
/// <param name="SecondaryText">The visible secondary explanation.</param>
/// <param name="AriaLabel">The accessible category label.</param>
public sealed record ProjectEmptyState(
    string Category,
    string EntityPlural,
    string SecondaryText,
    string AriaLabel)
{
    /// <summary>True absence category.</summary>
    public const string None = "none";

    /// <summary>Access denied category.</summary>
    public const string Denied = "denied";

    /// <summary>Data unavailable category.</summary>
    public const string Unavailable = "unavailable";

    /// <summary>Filter-empty category.</summary>
    public const string Filtered = "filtered";

    /// <summary>No projects empty state.</summary>
    public static ProjectEmptyState NoProjects()
        => new(None, "projects", "No projects are available for the current tenant scope.", "No projects");

    /// <summary>No references empty state.</summary>
    public static ProjectEmptyState NoReferences()
        => new(None, "references", "No references are linked to this project.", "No project references");

    /// <summary>No audit events empty state.</summary>
    public static ProjectEmptyState NoAudit()
        => new(None, "audit events", "No audit events are available for this project.", "No project audit events");

    /// <summary>Access denied empty state.</summary>
    public static ProjectEmptyState AccessDenied()
        => new(Denied, "authorized rows", "Access denied for this view.", "Access denied");

    /// <summary>Unavailable data empty state.</summary>
    public static ProjectEmptyState DataUnavailable()
        => new(Unavailable, "rows", "Data is temporarily unavailable.", "Data unavailable");

    /// <summary>Filter-empty state.</summary>
    public static ProjectEmptyState FilterReturnedNoResults()
        => new(Filtered, "filtered results", "The current filter returned no results.", "Filter returned no results");
}

