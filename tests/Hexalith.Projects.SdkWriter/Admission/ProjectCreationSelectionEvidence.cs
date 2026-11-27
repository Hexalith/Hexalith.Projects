// <copyright file="ProjectCreationSelectionEvidence.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Admission;

using System;

/// <summary>
/// Five-minute FR-25 Selection Evidence bound to actor, Tenant, surface, and supplied Folder
/// for interactive supplied-Folder creation admission.
/// </summary>
/// <param name="EvidenceId">The opaque selection evidence identifier.</param>
/// <param name="TenantId">The bound Tenant identifier.</param>
/// <param name="ActorPrincipalId">The bound original actor principal identifier.</param>
/// <param name="Surface">The bound interactive surface.</param>
/// <param name="FolderId">The bound target Folder identifier.</param>
/// <param name="IssuedAt">The UTC timestamp when the evidence was issued.</param>
/// <param name="ExpiresAt">The UTC timestamp when the evidence expires (at most 5 minutes after issuance).</param>
public sealed record ProjectCreationSelectionEvidence(
    string EvidenceId,
    string TenantId,
    string ActorPrincipalId,
    string Surface,
    string FolderId,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
