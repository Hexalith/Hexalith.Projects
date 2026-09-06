// <copyright file="ExplainContextSelectionQuery.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

/// <summary>Identifies the supported Explain Context Selection query.</summary>
/// <param name="ProjectId">The opaque Project identifier targeted by the query.</param>
public sealed record ExplainContextSelectionQuery(string ProjectId);
