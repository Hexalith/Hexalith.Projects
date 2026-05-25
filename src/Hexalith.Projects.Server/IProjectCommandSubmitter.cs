// <copyright file="IProjectCommandSubmitter.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Contracts.Commands;

/// <summary>
/// Submits a validated <see cref="CreateProject"/> command through the EventStore command pipeline
/// (persist-then-publish). This thin abstraction keeps the endpoint's tenant-guard + 202/safe-denial
/// mapping testable at Tier-2 with an in-memory fake, while the production binding wraps the
/// EventStore gateway client (the only Dapr-backed infrastructure boundary).
/// </summary>
public interface IProjectCommandSubmitter
{
    /// <summary>Submits the create command and returns the accept/replay/denial outcome.</summary>
    /// <param name="command">The validated create command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The submission outcome.</returns>
    Task<ProjectCommandSubmissionResult> SubmitCreateProjectAsync(CreateProject command, CancellationToken cancellationToken = default);
}

/// <summary>The outcome category of a command submission through the EventStore command pipeline.</summary>
public enum ProjectCommandSubmissionOutcome
{
    /// <summary>The command was accepted for asynchronous processing (202).</summary>
    Accepted,

    /// <summary>The command was a logical idempotent replay (202, idempotentReplay=true).</summary>
    IdempotentReplay,

    /// <summary>The same idempotency key was reused with a non-equivalent payload (409).</summary>
    IdempotencyConflict,

    /// <summary>Input validation failed (400).</summary>
    ValidationFailed,

    /// <summary>The caller is not authorized / the resource is not visible — surface as safe-denial 404.</summary>
    Denied,

    /// <summary>An infrastructure boundary was unavailable — surface as retryable 503.</summary>
    Unavailable,
}

/// <summary>Result of submitting a command through the EventStore command pipeline.</summary>
/// <param name="Outcome">The submission outcome category.</param>
/// <param name="CorrelationId">The correlation identifier echoed by the pipeline (safe, sanitized).</param>
public sealed record ProjectCommandSubmissionResult(
    ProjectCommandSubmissionOutcome Outcome,
    string? CorrelationId)
{
    /// <summary>Creates an accepted result (202).</summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="idempotentReplay">Whether this was a logical replay.</param>
    /// <returns>An accepted submission result.</returns>
    public static ProjectCommandSubmissionResult Accepted(string? correlationId, bool idempotentReplay)
        => new(idempotentReplay ? ProjectCommandSubmissionOutcome.IdempotentReplay : ProjectCommandSubmissionOutcome.Accepted, correlationId);

    /// <summary>Creates a denial result (safe-denial 404).</summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <returns>A denied submission result.</returns>
    public static ProjectCommandSubmissionResult Denied(string? correlationId)
        => new(ProjectCommandSubmissionOutcome.Denied, correlationId);
}
