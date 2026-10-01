// <copyright file="CapturingHttpMessageHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Authentication;

using System.Net;
using System.Net.Http.Headers;

/// <summary>Captures the authorization header forwarded by the Projects gateway fixture.</summary>
internal sealed class CapturingHttpMessageHandler : HttpMessageHandler
{
    /// <summary>Gets the authorization header received by the downstream handler.</summary>
    public AuthenticationHeaderValue? Authorization { get; private set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Authorization = request.Headers.Authorization;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
