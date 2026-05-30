// <copyright file="IProjectProposalConfirmationIdempotencyLedger.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Proposals;

/// <summary>Root idempotency guard for the composite confirm-new-project-proposal endpoint.</summary>
public interface IProjectProposalConfirmationIdempotencyLedger
{
    /// <summary>Records the root idempotency fingerprint or reports a same-key/different-body conflict.</summary>
    bool TryRecord(string idempotencyKey, string fingerprint);
}
