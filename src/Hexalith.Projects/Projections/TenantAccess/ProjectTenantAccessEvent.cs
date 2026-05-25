// <copyright file="ProjectTenantAccessEvent.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;

/// <summary>Normalized, metadata-only Tenants event consumed by the local projection handler.</summary>
public sealed record ProjectTenantAccessEvent(
    ProjectTenantAccessEventKind Kind,
    string TenantId,
    string MessageId,
    long SequenceNumber,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string? PrincipalId = null,
    string? Role = null,
    string? PreviousRole = null,
    string? ConfigurationKey = null,
    string PayloadFingerprint = "");
