// <copyright file="EventStoreAuthorizationValidationStatus.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System.Text.Json.Serialization;

/// <summary>EventStore write-layer authorization validation status.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EventStoreAuthorizationValidationStatus>))]
public enum EventStoreAuthorizationValidationStatus
{
    Allowed,
    Denied,
    Unavailable,
    Malformed,
}
