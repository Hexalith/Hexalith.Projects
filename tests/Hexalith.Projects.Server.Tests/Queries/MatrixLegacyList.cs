// <copyright file="MatrixLegacyList.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Declared retained v1 wire shape used for strict cutover comparison; unknown members are rejected.</summary>
/// <param name="Items">The ordered retained Project rows.</param>
/// <param name="Freshness">The retained freshness evidence.</param>
internal sealed record MatrixLegacyList(IReadOnlyList<MatrixLegacyRow> Items, MatrixLegacyFreshness Freshness);
#endif
