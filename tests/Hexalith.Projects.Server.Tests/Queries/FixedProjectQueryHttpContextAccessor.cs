// <copyright file="FixedProjectQueryHttpContextAccessor.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using Microsoft.AspNetCore.Http;

/// <summary>Provides a stable test HTTP context across asynchronous factory boundaries.</summary>
internal sealed class FixedProjectQueryHttpContextAccessor(HttpContext httpContext) : IHttpContextAccessor
{
    /// <inheritdoc/>
    public HttpContext? HttpContext { get; set; } = httpContext;
}
