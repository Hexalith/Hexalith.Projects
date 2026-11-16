// <copyright file="FoldersV1ReadLifecycle.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using System.Text.Json.Serialization;

/// <summary>Exact retained Folders v1 read shape; this type never describes the non-routed v2 candidate.</summary>
/// <param name="FolderId">The exact requested native Folder identifier.</param>
/// <param name="LifecycleState">The retained v1 lifecycle state.</param>
/// <param name="Archived">Whether the Folder is archived.</param>
/// <param name="RepositoryBindingId">The optional repository binding identifier.</param>
/// <param name="ProviderBindingRef">The optional provider binding reference.</param>
/// <param name="Freshness">The owner lifecycle evidence freshness.</param>
internal sealed record FoldersV1ReadLifecycle([property: JsonRequired] string FolderId, [property: JsonRequired] string LifecycleState,
    [property: JsonRequired] bool Archived, string? RepositoryBindingId, string? ProviderBindingRef, [property: JsonRequired] FoldersV1ReadFreshness Freshness);
