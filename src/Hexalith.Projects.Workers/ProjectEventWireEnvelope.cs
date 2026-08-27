// <copyright file="ProjectEventWireEnvelope.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Workers;

using Hexalith.EventStore.Contracts.Events;

/// <summary>
/// Flat wire-format envelope published by the EventStore Dapr publisher.
/// </summary>
/// <remarks>
/// The publisher emits metadata fields at the root of the payload. The projection layer uses the
/// contracts representation, which groups those fields under <see cref="EventEnvelope.Metadata"/>.
/// This boundary DTO keeps the transport shape explicit and converts it before processing.
/// </remarks>
internal sealed record ProjectEventWireEnvelope(
    string MessageId,
    string AggregateId,
    string AggregateType,
    string TenantId,
    string Domain,
    long SequenceNumber,
    long GlobalPosition,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string CausationId,
    string UserId,
    string DomainServiceVersion,
    string EventTypeName,
    int MetadataVersion,
    string SerializationFormat,
    byte[] Payload,
    IReadOnlyDictionary<string, string>? Extensions)
{
    /// <summary>Converts the transport DTO to the canonical contracts envelope.</summary>
    /// <returns>The canonical contracts envelope.</returns>
    public EventEnvelope ToEventEnvelope()
        => new(
            new EventMetadata(
                MessageId,
                AggregateId,
                AggregateType,
                TenantId,
                Domain,
                SequenceNumber,
                GlobalPosition,
                Timestamp,
                CorrelationId,
                CausationId,
                UserId,
                DomainServiceVersion,
                EventTypeName,
                MetadataVersion,
                SerializationFormat),
            Payload,
            Extensions);
}
