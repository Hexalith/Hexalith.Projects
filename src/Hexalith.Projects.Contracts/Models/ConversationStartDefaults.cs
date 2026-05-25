// <copyright file="ConversationStartDefaults.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

/// <summary>
/// Metadata-only defaults that influence how a conversation may start from a Project.
/// </summary>
/// <param name="LinkedSourcePolicy">The closed v1 linked-source policy.</param>
public sealed record ConversationStartDefaults(LinkedSourcePolicy LinkedSourcePolicy);
