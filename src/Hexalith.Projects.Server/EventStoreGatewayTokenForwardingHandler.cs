// <copyright file="EventStoreGatewayTokenForwardingHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

/// <summary>
/// Forwards the authenticated caller's bearer token from the inbound request onto the outbound
/// EventStore command-gateway call. The EventStore command API is <c>[Authorize]</c> and derives the
/// command UserId from the JWT <c>sub</c> claim; without this the persist-then-publish hop would be
/// rejected as unauthenticated (401) and collapse to a safe-denial at the edge.
/// </summary>
internal sealed class EventStoreGatewayTokenForwardingHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    private const string AuthorizationHeader = "Authorization";
    private const string BearerPrefix = "Bearer ";

    private readonly IHttpContextAccessor _httpContextAccessor =
        httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.Authorization is null
            && _httpContextAccessor.HttpContext is { } context
            && context.Request.Headers.TryGetValue(AuthorizationHeader, out StringValues header))
        {
            string raw = header.ToString();
            if (raw.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string token = raw[BearerPrefix.Length..].Trim();
                if (token.Length > 0)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
