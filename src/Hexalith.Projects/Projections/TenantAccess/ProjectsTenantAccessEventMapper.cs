// <copyright file="ProjectsTenantAccessEventMapper.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;

/// <summary>Maps Tenants event metadata into normalized Projects tenant-access projection events.</summary>
public sealed class ProjectsTenantAccessEventMapper(ILogger<ProjectsTenantAccessEventMapper>? logger = null)
{
    private const char FieldSeparator = '\u001F';
    private const string NullMarker = "\u0001null\u0001";

    /// <summary>Maps the event metadata to an internal projection event.</summary>
    public ProjectTenantAccessEvent Map(
        ProjectTenantAccessEventKind kind,
        string eventTenantId,
        string envelopeTenantId,
        string messageId,
        long sequenceNumber,
        DateTimeOffset timestamp,
        string correlationId,
        string? principalId = null,
        string? role = null,
        string? previousRole = null,
        string? configurationKey = null,
        string?[]? fingerprintParts = null)
    {
        string projectionTenantId = eventTenantId;

        // The Tenants registry aggregate lives in the reserved 'system' platform tenant, so its
        // lifecycle/membership events legitimately carry envelope TenantId='system' while the payload
        // names the managed tenant (e.g. 'tenant-a'). That administering case must NOT be treated as a
        // cross-tenant mismatch — only a non-system envelope that disagrees with the payload is dropped.
        if (!string.Equals(envelopeTenantId, eventTenantId, StringComparison.Ordinal)
            && !string.Equals(envelopeTenantId, "system", StringComparison.Ordinal))
        {
            logger?.LogWarning(
                "Tenant envelope mismatch: envelope TenantId={EnvelopeTenantId} differs from payload TenantId={PayloadTenantId} for event {EventKind} (MessageId={MessageId}); event will be dropped.",
                envelopeTenantId,
                eventTenantId,
                kind,
                messageId);
            projectionTenantId = string.Empty;
        }

        return new ProjectTenantAccessEvent(
            kind,
            projectionTenantId,
            messageId,
            sequenceNumber,
            timestamp,
            correlationId,
            principalId,
            role,
            previousRole,
            configurationKey,
            FingerprintHash(fingerprintParts));
    }

    private static string FingerprintHash(string?[]? parts)
    {
        if (parts is null || parts.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder canonical = new();
        for (int i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                _ = canonical.Append(FieldSeparator);
            }

            _ = canonical.Append(parts[i] is null ? NullMarker : parts[i]);
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
