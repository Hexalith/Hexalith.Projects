// <copyright file="MatrixLegacyActivation.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Declared retained v1 wire shape used for strict cutover comparison; unknown members are rejected.</summary>
/// <param name="Enabled">The retained activation indicator; native membership is checked separately.</param>
/// <param name="BlockedReasonCode">The optional retained activation reason.</param>
internal sealed record MatrixLegacyActivation(bool Enabled, string? BlockedReasonCode);
#endif
