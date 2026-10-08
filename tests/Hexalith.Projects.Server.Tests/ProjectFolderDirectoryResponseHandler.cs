// <copyright file="ProjectFolderDirectoryResponseHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System.Net.Http;

/// <summary>Returns queued Folders responses and records routes and task identity headers.</summary>
/// <param name="responses">The responses returned in request order.</param>
internal sealed class ProjectFolderDirectoryResponseHandler(IReadOnlyList<HttpResponseMessage> responses) : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new(responses);

    /// <summary>Gets the task identities observed on outgoing requests.</summary>
    internal List<string?> TaskIds { get; } = [];

    /// <summary>Gets the public routes observed on outgoing requests.</summary>
    internal List<string> RequestPaths { get; } = [];

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestPaths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
        TaskIds.Add(request.Headers.TryGetValues("X-Hexalith-Task-Id", out IEnumerable<string>? values)
            ? values.FirstOrDefault()
            : null);
        return Task.FromResult(_responses.Dequeue());
    }
}
