// <copyright file="RecordingHttpMessageHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Authentication;

using System.Net;
using System.Net.Http.Headers;

/// <summary>Terminal handler that records the outbound authorization header and answers 204.</summary>
internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    /// <summary>Gets the authorization header observed on each outbound request.</summary>
    public List<AuthenticationHeaderValue?> Authorizations { get; } = [];

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Authorizations.Add(request.Headers.Authorization);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
    }
}
