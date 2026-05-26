// <copyright file="ProjectConversationAssignmentResult.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

/// <summary>Outcome category for Projects-owned conversation assignment orchestration.</summary>
public enum ProjectConversationAssignmentOutcome
{
    /// <summary>The upstream assignment command was accepted.</summary>
    Accepted,

    /// <summary>The request failed metadata-only validation.</summary>
    ValidationFailed,

    /// <summary>The request conflicted with a prior idempotency key or optimistic guard.</summary>
    Conflict,

    /// <summary>The caller or resource was hidden by a fail-closed authorization result.</summary>
    Denied,

    /// <summary>The upstream assignment boundary was unavailable or untrusted.</summary>
    Unavailable,
}

/// <summary>Result of a Projects conversation assignment request.</summary>
/// <param name="Outcome">The outcome category.</param>
/// <param name="CorrelationId">The safe upstream correlation identifier when available.</param>
public sealed record ProjectConversationAssignmentResult(
    ProjectConversationAssignmentOutcome Outcome,
    string? CorrelationId)
{
    /// <summary>Creates an accepted result.</summary>
    public static ProjectConversationAssignmentResult Accepted(string? correlationId) =>
        new(ProjectConversationAssignmentOutcome.Accepted, correlationId);
}
