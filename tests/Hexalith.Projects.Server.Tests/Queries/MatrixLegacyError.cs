// <copyright file="MatrixLegacyError.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Declared retained v1 wire shape used for strict cutover comparison; unknown members are rejected.</summary>
/// <param name="Type">The declared problem type.</param>
/// <param name="Title">The declared safe problem title.</param>
/// <param name="Status">The HTTP status represented by the problem.</param>
/// <param name="Category">The retained problem category.</param>
/// <param name="Code">The retained safe problem code.</param>
/// <param name="Message">The retained safe problem message.</param>
/// <param name="CorrelationId">The exact validated caller correlation identifier.</param>
/// <param name="Retryable">Whether the retained problem permits retry.</param>
/// <param name="ClientAction">The declared safe recovery action.</param>
/// <param name="Details">The retained safe visibility details.</param>
internal sealed record MatrixLegacyError(string Type, string Title, int Status, string Category, string Code, string Message, string CorrelationId, bool Retryable, string ClientAction, MatrixVisibility Details);
#endif
