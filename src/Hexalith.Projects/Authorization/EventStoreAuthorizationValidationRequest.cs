// <copyright file="EventStoreAuthorizationValidationRequest.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System.Collections.Generic;

/// <summary>Metadata-only request for EventStore write-layer authorization validation.</summary>
public sealed record EventStoreAuthorizationValidationRequest(
    string TenantId,
    string PrincipalId,
    string ActionToken,
    string? ProjectId,
    string? CorrelationId,
    string? TaskId,
    IReadOnlyList<AuthorizationLayer> EvaluatedLayers);
