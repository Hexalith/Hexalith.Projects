// <copyright file="MatrixGatewayError.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Declared retained v1 wire shape used for strict cutover comparison; unknown members are rejected.</summary>
/// <param name="Type">The declared problem type.</param>
/// <param name="Title">The declared safe problem title.</param>
/// <param name="Status">The HTTP status represented by the problem.</param>
/// <param name="Detail">The declared safe problem detail.</param>
/// <param name="Instance">The actual request problem instance.</param>
/// <param name="CorrelationId">The exact validated caller correlation identifier.</param>
/// <param name="ReasonCode">The declared safe reason code.</param>
internal sealed record MatrixGatewayError(string Type, string Title, int Status, string Detail, string Instance, string CorrelationId, string ReasonCode);
#endif
