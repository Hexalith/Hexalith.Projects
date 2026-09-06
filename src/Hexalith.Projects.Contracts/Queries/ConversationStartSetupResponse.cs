// <copyright file="ConversationStartSetupResponse.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using Hexalith.Projects.Contracts.Models;

/// <summary>Supported Conversation-start setup response.</summary>
/// <param name="Setup">The bounded setup subset, or null when unavailable or denied.</param>
/// <param name="Snapshot">The shared AD-32 admission snapshot.</param>
public sealed record ConversationStartSetupResponse(ConversationStartSetup? Setup, AdmissionSnapshot Snapshot);
