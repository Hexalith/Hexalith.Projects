// <copyright file="ConversationFixtureTrust.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

/// <summary>One deterministic Conversations projection trust posture served by the fixture role.</summary>
/// <param name="State">The published <c>ProjectionTrustState</c> wire value.</param>
/// <param name="ReasonCode">The published <c>ProjectionFreshnessReasonCode</c> wire value.</param>
/// <param name="SafeNextAction">The metadata-only next action text.</param>
internal sealed record ConversationFixtureTrust(string State, string ReasonCode, string SafeNextAction);
