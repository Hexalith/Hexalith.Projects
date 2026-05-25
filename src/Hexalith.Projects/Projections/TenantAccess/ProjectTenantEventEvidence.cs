// <copyright file="ProjectTenantEventEvidence.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;

/// <summary>
/// Deduplication evidence for a consumed Tenants event. Correlation id is deliberately excluded so
/// normal at-least-once redelivery does not become a false replay conflict.
/// </summary>
public sealed record ProjectTenantEventEvidence(
    string MessageId,
    string TenantId,
    string EventTypeName,
    long SequenceNumber,
    DateTimeOffset Timestamp,
    string PayloadFingerprint);
