// <copyright file="ProjectsContractMetadata.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts;

/// <summary>
/// Placeholder metadata anchor for the Hexalith.Projects contract surface.
/// Establishes the contract namespace and assembly identity so later stories
/// (identifiers, commands, events, queries, models, OpenAPI spine) can fill it.
/// Kept netstandard2.0-safe because contract types feed FrontComposer source generators.
/// </summary>
public static class ProjectsContractMetadata
{
    /// <summary>
    /// Gets the logical domain name for Projects aggregates, used to derive the canonical
    /// EventStore identity <c>{tenant}:{domain}:{aggregateId}</c> in later stories.
    /// </summary>
    public static string DomainName => "projects";
}
