// <copyright file="ProjectProjectionEnvelope.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ProjectList;

using Hexalith.Projects.Contracts.Events;

/// <summary>
/// Read-side projection envelope carrying the projection sequence, the dispatch tenant, and the project
/// event (AR-8). Mirrors the Folders <c>FolderProjectionEnvelope</c>. The envelope tenant must agree
/// with the event tenant (tenant-guard) or the projection skips the event (FS-8, NFR-1).
/// </summary>
/// <param name="TenantId">The dispatch (envelope) tenant the event was delivered for.</param>
/// <param name="Sequence">The monotonic projection sequence used for deterministic ordering.</param>
/// <param name="Event">The project event to fold into the read model.</param>
public sealed record ProjectProjectionEnvelope(
    string TenantId,
    long Sequence,
    IProjectEvent Event);
