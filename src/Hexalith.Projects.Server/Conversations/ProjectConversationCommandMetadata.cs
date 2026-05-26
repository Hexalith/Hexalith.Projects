// <copyright file="ProjectConversationCommandMetadata.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Conversations;

/// <summary>Server-derived command metadata for Projects conversation assignment requests.</summary>
/// <param name="CorrelationId">The safe correlation identifier.</param>
/// <param name="TaskId">The task or causation identifier.</param>
/// <param name="IdempotencyKey">The caller idempotency key accepted by Projects.</param>
public sealed record ProjectConversationCommandMetadata(
    string CorrelationId,
    string TaskId,
    string IdempotencyKey)
{
    /// <summary>Gets the safe correlation identifier.</summary>
    public string CorrelationId { get; } = Require(CorrelationId, nameof(CorrelationId));

    /// <summary>Gets the task or causation identifier.</summary>
    public string TaskId { get; } = Require(TaskId, nameof(TaskId));

    /// <summary>Gets the caller idempotency key accepted by Projects.</summary>
    public string IdempotencyKey { get; } = Require(IdempotencyKey, nameof(IdempotencyKey));

    private static string Require(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
