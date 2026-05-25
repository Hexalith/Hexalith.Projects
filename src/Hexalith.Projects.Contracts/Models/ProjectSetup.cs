// <copyright file="ProjectSetup.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System.Collections.Generic;

/// <summary>
/// Concrete v1 Project setup shape. It carries only bounded, metadata-safe conversation guidance and
/// source-kind preferences. It must never contain raw prompts, transcript text, file contents, memory
/// bodies, paths, tokens, secrets, or provider internals.
/// </summary>
/// <param name="Goals">Bounded safe project goals.</param>
/// <param name="UserInstructions">Bounded user-facing instructions, not model/provider prompts.</param>
/// <param name="PreferredSourceKinds">Preferred source kinds for future context selection.</param>
/// <param name="ExcludedSourceKinds">Excluded source kinds for future context selection.</param>
/// <param name="ConversationStartDefaults">Optional conversation-start defaults.</param>
public sealed record ProjectSetup(
    IReadOnlyList<string> Goals,
    IReadOnlyList<string> UserInstructions,
    IReadOnlyList<ProjectContextSourceKind> PreferredSourceKinds,
    IReadOnlyList<ProjectContextSourceKind> ExcludedSourceKinds,
    ConversationStartDefaults? ConversationStartDefaults)
{
    /// <summary>Gets an empty metadata-safe setup instance.</summary>
    public static ProjectSetup Empty { get; } = new([], [], [], [], null);
}
