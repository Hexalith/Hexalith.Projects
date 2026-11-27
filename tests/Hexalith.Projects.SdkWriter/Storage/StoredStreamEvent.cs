// <copyright file="StoredStreamEvent.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Storage;

using System;

/// <summary>A single persisted event envelope inside <see cref="StoredEventStream"/>.</summary>
/// <param name="SequenceNumber">The 1-based per-stream sequence number.</param>
/// <param name="GlobalPosition">The monotonically increasing shared-store global position.</param>
/// <param name="EventTypeName">The full CLR type name of the event payload.</param>
/// <param name="Payload">The UTF-8 JSON-serialized event bytes.</param>
/// <param name="Timestamp">The UTC persistence timestamp.</param>
/// <param name="ActorId">The authenticated actor principal identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="MessageId">The idempotency/message identifier.</param>
public sealed record StoredStreamEvent(
    long SequenceNumber,
    long GlobalPosition,
    string EventTypeName,
    byte[] Payload,
    DateTimeOffset Timestamp,
    string ActorId,
    string CorrelationId,
    string MessageId);
