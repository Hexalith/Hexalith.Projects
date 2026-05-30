// <copyright file="ProjectOperatorContextActivation.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System.Text.Json.Serialization;

/// <summary>Safe context activation metadata for operator diagnostics.</summary>
/// <param name="Enabled">
/// A lifecycle-derived value indicating whether the Project may become active conversation context by default
/// (true when the Project is Active). It is not a live context-assembly probe.
/// </param>
/// <param name="BlockedReasonCode">The safe reason code when activation is blocked (for example, <c>archived</c>); <c>null</c> when enabled.</param>
public sealed record ProjectOperatorContextActivation(
    bool Enabled,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    string? BlockedReasonCode);
