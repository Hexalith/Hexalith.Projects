// <copyright file="MatrixVisibility.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Declared retained v1 wire shape used for strict cutover comparison; unknown members are rejected.</summary>
/// <param name="Visibility">The declared safe resource visibility.</param>
internal sealed record MatrixVisibility(string Visibility);
#endif
