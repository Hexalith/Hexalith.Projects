// <copyright file="InMemoryProjectProposalConfirmationIdempotencyLedger.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Proposals;

using System.Collections.Concurrent;

/// <summary>Process-local implementation of the proposal confirmation root idempotency guard.</summary>
public sealed class InMemoryProjectProposalConfirmationIdempotencyLedger : IProjectProposalConfirmationIdempotencyLedger
{
    private readonly ConcurrentDictionary<string, string> _fingerprints = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public bool TryRecord(string idempotencyKey, string fingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);

        string recorded = _fingerprints.GetOrAdd(idempotencyKey, fingerprint);
        return string.Equals(recorded, fingerprint, StringComparison.Ordinal);
    }
}
