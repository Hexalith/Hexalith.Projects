// <copyright file="ExplainContextSelectionResponse.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System.Collections.Generic;

using Hexalith.Projects.Contracts.Models;

/// <summary>Supported request-scoped context-selection explanation.</summary>
/// <param name="Context">The assembled metadata-only context at the same evidence cutoff as Get.</param>
/// <param name="Evaluations">Deterministic per-candidate evaluations with no persisted identity.</param>
public sealed record ExplainContextSelectionResponse(
    ProjectContextReadResponse Context,
    IReadOnlyList<ProjectContextEvaluation> Evaluations);
