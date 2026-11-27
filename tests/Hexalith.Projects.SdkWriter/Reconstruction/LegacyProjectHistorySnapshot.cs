// <copyright file="LegacyProjectHistorySnapshot.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Reconstruction;

using System.Collections.Generic;

using Hexalith.Projects.Contracts.Events;

/// <summary>Authoritative legacy Project stream snapshot supplied to AD-35 claim reconstruction.</summary>
/// <param name="TenantId">The owning tenant identifier.</param>
/// <param name="ProjectId">The Project identifier.</param>
/// <param name="Events">The persisted stream events in sequence order.</param>
/// <param name="IsIncompleteHistory">Whether the caller knows the stream history is truncated or incomplete.</param>
public sealed record LegacyProjectHistorySnapshot(
    string TenantId,
    string ProjectId,
    IReadOnlyList<IProjectEvent> Events,
    bool IsIncompleteHistory = false);
