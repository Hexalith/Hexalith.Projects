// <copyright file="EventStoreAuthorizationValidationResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

/// <summary>Metadata-only EventStore write-layer authorization validation result.</summary>
public sealed record EventStoreAuthorizationValidationResult(
    EventStoreAuthorizationValidationStatus Status,
    string OutcomeCode,
    string? FreshnessWatermark,
    string FreshnessClass,
    bool Retryable)
{
    /// <summary>Creates an allowed validation result.</summary>
    public static EventStoreAuthorizationValidationResult Allowed(string? freshnessWatermark)
        => new(EventStoreAuthorizationValidationStatus.Allowed, "allowed", freshnessWatermark, "fresh", Retryable: false);

    /// <summary>Creates a denied validation result.</summary>
    public static EventStoreAuthorizationValidationResult Denied()
        => new(EventStoreAuthorizationValidationStatus.Denied, "eventstore_validator_denied", null, "fresh", Retryable: false);

    /// <summary>Creates an unavailable validation result.</summary>
    public static EventStoreAuthorizationValidationResult Unavailable()
        => new(EventStoreAuthorizationValidationStatus.Unavailable, "eventstore_validator_unavailable", null, "unavailable", Retryable: true);

    /// <summary>Creates a malformed validation result.</summary>
    public static EventStoreAuthorizationValidationResult Malformed()
        => new(EventStoreAuthorizationValidationStatus.Malformed, "authorization_evidence_malformed", null, "malformed", Retryable: false);
}
