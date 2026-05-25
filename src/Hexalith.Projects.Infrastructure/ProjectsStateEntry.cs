// <copyright file="ProjectsStateEntry.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

/// <summary>
/// State-store entry carrying a value and optimistic-concurrency token.
/// </summary>
/// <typeparam name="T">The state value type.</typeparam>
/// <param name="Value">The stored value, or null when no state exists.</param>
/// <param name="ETag">The state-store ETag.</param>
public sealed record ProjectsStateEntry<T>(T? Value, string? ETag);
