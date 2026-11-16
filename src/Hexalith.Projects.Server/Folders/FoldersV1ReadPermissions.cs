// <copyright file="FoldersV1ReadPermissions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using System.Text.Json.Serialization;

/// <summary>Exact retained Folders v1 read shape; this type never describes the non-routed v2 candidate.</summary>
/// <param name="FolderId">The exact requested native Folder identifier.</param>
/// <param name="Permissions">The effective retained v1 permissions.</param>
/// <param name="AuthorizationOutcome">The owner authorization outcome for the current actor.</param>
/// <param name="Freshness">The owner permissions evidence freshness.</param>
internal sealed record FoldersV1ReadPermissions([property: JsonRequired] string FolderId, [property: JsonRequired] IReadOnlyList<string> Permissions,
    [property: JsonRequired] string AuthorizationOutcome, [property: JsonRequired] FoldersV1ReadFreshness Freshness);
