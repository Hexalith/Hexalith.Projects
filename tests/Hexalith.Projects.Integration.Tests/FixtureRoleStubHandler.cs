// <copyright file="FixtureRoleStubHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using System.Net;

/// <summary>Stub sibling role hosts keyed by host name; records every request as <c>METHOD role</c>.</summary>
/// <param name="respond">Returns the status for a method and role, or <see langword="null"/> to fail transport.</param>
internal sealed class FixtureRoleStubHandler(Func<HttpMethod, string, HttpStatusCode?> respond) : HttpMessageHandler
{
    /// <summary>Gets the observed requests in send order.</summary>
    public List<string> Requests { get; } = [];

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string role = request.RequestUri!.Host;
        Requests.Add($"{request.Method.Method} {role}");
        HttpStatusCode? status = respond(request.Method, role);
        return status is null
            ? Task.FromException<HttpResponseMessage>(new HttpRequestException("transport failure for http://unreachable.invalid"))
            : Task.FromResult(new HttpResponseMessage(status.Value));
    }
}
