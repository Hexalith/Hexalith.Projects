// <copyright file="FoldersV1ProjectReadDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using System.Net;
using System.Text.Json;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Reads the owner's retained v1 lifecycle/permissions contracts explicitly. The default
/// generated Folders IClient describes a non-routed v2 candidate and is not a v1 read transport.
/// </summary>
/// <param name="client">The existing bearer-forwarding HTTP client for retained owner reads.</param>
/// <param name="mutations">The unchanged adapter for write-time Folder validation.</param>
internal sealed class FoldersV1ProjectReadDirectory(HttpClient client, IProjectFolderDirectory mutations) : IProjectFolderDirectory
{
    /// <summary>The named HTTP client carrying the authenticated actor to retained owner reads.</summary>
    internal const string ClientName = "projects-retained-folder-v1-reads";
    private const int MaximumResponseBytes = 16 * 1024;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    // Existing write-time validation remains on its original adapter and contract.
    public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(ProjectId projectId, string folderId,
        string correlationId, CancellationToken cancellationToken = default)
        => mutations.ValidateSetProjectFolderAsync(projectId, folderId, correlationId, cancellationToken);

    /// <inheritdoc/>
    public async Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(ProjectId projectId, string folderId,
        string correlationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);
        if (string.IsNullOrWhiteSpace(folderId) || string.IsNullOrWhiteSpace(correlationId))
            return new(ProjectFolderValidationOutcome.ValidationFailed, correlationId);
        // Streaming response bodies remain inside the client's existing timeout budget.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(client.Timeout);
        CancellationToken readToken = deadline.Token;
        try
        {
            string resource = "api/v1/folders/" + Uri.EscapeDataString(folderId);
            using HttpResponseMessage lifecycleResponse = await SendAsync(resource + "/lifecycle-status", correlationId,
                "eventually_consistent", readToken).ConfigureAwait(false);
            if (!lifecycleResponse.IsSuccessStatusCode) return new(Status(lifecycleResponse.StatusCode), correlationId);
            FoldersV1ReadLifecycle? lifecycle = await ReadAsync<FoldersV1ReadLifecycle>(lifecycleResponse.Content, readToken).ConfigureAwait(false);
            if (lifecycle?.FolderId != folderId || !Current(lifecycle.Freshness, "eventually_consistent"))
                return new(lifecycle?.Freshness.Stale == true ? ProjectFolderValidationOutcome.Stale : ProjectFolderValidationOutcome.Unavailable, correlationId);
            if (lifecycle.Archived || lifecycle.LifecycleState is "failed" or "inaccessible" or "unknown_provider_outcome")
                return new(ProjectFolderValidationOutcome.Archived, correlationId);
            if (lifecycle.LifecycleState is not ("requested" or "ready" or "reconciliation_required"))
                return new(ProjectFolderValidationOutcome.Unavailable, correlationId);
            using HttpResponseMessage permissionsResponse = await SendAsync(resource + "/effective-permissions", correlationId,
                "read_your_writes", readToken).ConfigureAwait(false);
            if (!permissionsResponse.IsSuccessStatusCode) return new(Status(permissionsResponse.StatusCode), correlationId);
            FoldersV1ReadPermissions? permissions = await ReadAsync<FoldersV1ReadPermissions>(permissionsResponse.Content, readToken).ConfigureAwait(false);
            if (permissions?.FolderId != folderId || !Current(permissions.Freshness, "read_your_writes")
                || permissions.Permissions is null || permissions.Permissions.Any(value => value is not ("read" or "write" or "administer"))
                || permissions.Permissions.Distinct(StringComparer.Ordinal).Count() != permissions.Permissions.Count)
                return new(permissions?.Freshness.Stale == true ? ProjectFolderValidationOutcome.Stale : ProjectFolderValidationOutcome.Unavailable, correlationId);
            return new(permissions.AuthorizationOutcome == "allowed" && permissions.Permissions.Count > 0
                ? ProjectFolderValidationOutcome.Accepted : permissions.AuthorizationOutcome == "denied_safe"
                    ? ProjectFolderValidationOutcome.Denied : ProjectFolderValidationOutcome.Unavailable, correlationId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return new(ProjectFolderValidationOutcome.Unavailable, correlationId); }
    }

    private async Task<HttpResponseMessage> SendAsync(string route, string correlationId, string freshness, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        request.Headers.TryAddWithoutValidation("X-Hexalith-Freshness", freshness);
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
    }

    private static async Task<T?> ReadAsync<T>(HttpContent content, CancellationToken token)
    {
        if (content.Headers.ContentLength is > MaximumResponseBytes) return default;
        using Stream body = await content.ReadAsStreamAsync(token).ConfigureAwait(false);
        byte[] bytes = new byte[MaximumResponseBytes + 1];
        int length = 0;
        while (length < bytes.Length)
        {
            int read = await body.ReadAsync(bytes.AsMemory(length), token).ConfigureAwait(false);
            if (read == 0) break;
            length += read;
        }
        if (length > MaximumResponseBytes) return default;
        using JsonDocument document = JsonDocument.Parse(bytes.AsMemory(0, length));
        return UniqueMembers(document.RootElement) ? document.RootElement.Deserialize<T>(Json) : default;
    }

    private static bool UniqueMembers(JsonElement value)
        => value.ValueKind == JsonValueKind.Object
            ? value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == value.EnumerateObject().Count()
                && value.EnumerateObject().All(property => UniqueMembers(property.Value))
            : value.ValueKind != JsonValueKind.Array || value.EnumerateArray().All(UniqueMembers);

    private static bool Current(FoldersV1ReadFreshness? value, string expected)
        => value is { Stale: false } && value.ObservedAt != default && !string.IsNullOrWhiteSpace(value.ProjectionWatermark)
            && value.ReadConsistency == expected;
    private static ProjectFolderValidationOutcome Status(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => ProjectFolderValidationOutcome.ValidationFailed,
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound => ProjectFolderValidationOutcome.Denied,
        HttpStatusCode.Conflict => ProjectFolderValidationOutcome.Archived,
        _ => ProjectFolderValidationOutcome.Unavailable,
    };
}
