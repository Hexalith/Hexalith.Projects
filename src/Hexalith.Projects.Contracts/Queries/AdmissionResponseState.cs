// <copyright file="AdmissionResponseState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System.Text.Json.Serialization;

/// <summary>Shared AD-32 admission state for supported Project read snapshots.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AdmissionResponseState>))]
public enum AdmissionResponseState
{
    /// <summary>All required evidence is current and optional omissions are absent.</summary>
    Complete,

    /// <summary>Required evidence is current and every optional omission is explicit.</summary>
    Partial,

    /// <summary>Required evidence is not current or is unavailable.</summary>
    Unavailable,

    /// <summary>The target is denied and no protected response is disclosed.</summary>
    Denied,
}
