// <copyright file="ProjectProjectionAppendStatus.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Infrastructure;

/// <summary>
/// Outcome of appending a project event to the durable projection journal.
/// </summary>
public enum ProjectProjectionAppendStatus
{
    /// <summary>The event was applied.</summary>
    Applied,

    /// <summary>The event message was already processed with identical metadata.</summary>
    Duplicate,

    /// <summary>The same message id carried incompatible evidence.</summary>
    ReplayConflict,

    /// <summary>The event is for a foreign domain.</summary>
    SkippedForeignDomain,

    /// <summary>The event type is not a known Projects success event.</summary>
    SkippedUnknownEventType,

    /// <summary>The event payload could not be safely deserialized.</summary>
    InvalidPayload,

    /// <summary>The event is older than the stored projection watermark.</summary>
    OutOfOrder,
}
