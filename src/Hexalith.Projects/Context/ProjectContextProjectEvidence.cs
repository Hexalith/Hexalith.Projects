// <copyright file="ProjectContextProjectEvidence.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

using Hexalith.Projects.Projections.ProjectDetail;

/// <summary>
/// Typed wrapper around the <see cref="ProjectDetailItem"/> projection row that
/// <c>ProjectContextInclusionPolicy</c> evaluates (Story 3.1). A <see langword="null"/>
/// <see cref="Detail"/> means the project is not visible to the authoritative tenant — the policy
/// collapses such requests to the safe-denial
/// <see cref="Hexalith.Projects.Contracts.Ui.ProjectContextAssemblyOutcome.ProjectUnavailable"/>
/// outcome (never reveals cross-tenant existence).
/// </summary>
/// <param name="Detail">The projection row, or <see langword="null"/> when not visible.</param>
public sealed record ProjectContextProjectEvidence(ProjectDetailItem? Detail);
