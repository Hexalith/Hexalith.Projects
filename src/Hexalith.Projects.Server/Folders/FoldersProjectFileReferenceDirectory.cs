// <copyright file="FoldersProjectFileReferenceDirectory.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Folders;

using System;
using System.Linq;

using FoldersClient = Hexalith.Folders.Client.Generated.IClient;
using Hexalith.Folders.Client.Generated;
using Hexalith.Projects.Contracts.Identifiers;

/// <summary>
/// Projects ACL adapter over the Folders generated metadata-only file-context client (Story 2.5). It
/// validates an optional File Reference through the Folders <c>GetFolderFileMetadata</c> route, which
/// returns bounded file metadata (path/kind/byte-length/sensitivity/redaction) only after Folders runs
/// tenant access, folder ACL, path policy, sensitivity classification, and C4 bounds. It never requests
/// file content bytes (the content-bearing <c>ReadFileRange</c> route is deliberately not used) and
/// translates accepted metadata into a safe Projects outcome, failing closed on any denial, missing,
/// redacted/excluded, stale, archived, or unavailable evidence.
/// </summary>
public sealed class FoldersProjectFileReferenceDirectory(FoldersClient foldersClient) : IProjectFileReferenceDirectory
{
    private const string DefaultPathPolicyClass = "tenant_sensitive_document";

    private readonly FoldersClient _foldersClient = foldersClient ?? throw new ArgumentNullException(nameof(foldersClient));

    /// <inheritdoc />
    public async Task<ProjectFileReferenceValidationResult> ValidateLinkFileReferenceAsync(
        ProjectId projectId,
        string folderId,
        string workspaceId,
        string filePath,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);

        if (string.IsNullOrWhiteSpace(folderId)
            || string.IsNullOrWhiteSpace(workspaceId)
            || string.IsNullOrWhiteSpace(filePath)
            || string.IsNullOrWhiteSpace(correlationId))
        {
            return new(ProjectFileReferenceValidationOutcome.ValidationFailed, correlationId);
        }

        string normalizedPath = filePath.Trim();

        try
        {
            FileMetadataRequest request = new()
            {
                RequestSchemaVersion = "v1",
                Paths =
                [
                    new PathMetadata
                    {
                        NormalizedPath = normalizedPath,
                        DisplayName = DeriveDisplayName(normalizedPath),
                        PathPolicyClass = DefaultPathPolicyClass,
                        UnicodeNormalization = PathMetadataUnicodeNormalization.NFC,
                    },
                ],
            };

            FileMetadataResult result = await _foldersClient
                .GetFolderFileMetadataAsync(
                    folderId,
                    workspaceId,
                    correlationId,
                    string.IsNullOrWhiteSpace(taskId) ? correlationId : taskId,
                    ReadConsistencyClass.Eventually_consistent,
                    request,
                    cancellationToken)
                .ConfigureAwait(false);

            return Evaluate(result, normalizedPath, correlationId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithFoldersApiException ex)
        {
            return new(MapFoldersStatus(ex.StatusCode), correlationId);
        }
        catch (Exception)
        {
            return new(ProjectFileReferenceValidationOutcome.Unavailable, correlationId);
        }
    }

    private static ProjectFileReferenceValidationResult Evaluate(FileMetadataResult? result, string normalizedPath, string correlationId)
    {
        // No evidence at all is untrusted: fail closed as unavailable (retryable) rather than accept.
        if (result is null || result.Items is null)
        {
            return new(ProjectFileReferenceValidationOutcome.Unavailable, correlationId);
        }

        // Stale snapshot evidence must not be trusted to authorize a mutation.
        if (result.Freshness?.Stale == true)
        {
            return new(ProjectFileReferenceValidationOutcome.Stale, correlationId);
        }

        FileMetadataItem? item = result.Items.FirstOrDefault(candidate =>
            candidate?.Path is not null
            && string.Equals(candidate.Path.NormalizedPath, normalizedPath, StringComparison.Ordinal));

        // A missing path (no matching metadata row) is a safe denial — existence is never disclosed.
        if (item is null)
        {
            return new(ProjectFileReferenceValidationOutcome.Denied, correlationId);
        }

        // A directory is not a usable file reference.
        if (item.Kind != FileMetadataItemKind.File)
        {
            return new(ProjectFileReferenceValidationOutcome.Denied, correlationId);
        }

        // Redacted / excluded / binary-disallowed metadata fails closed without leaking why.
        if (item.Redaction != FileMetadataItemRedaction.Not_redacted)
        {
            return new(ProjectFileReferenceValidationOutcome.Redacted, correlationId);
        }

        return ProjectFileReferenceValidationResult.Accepted(correlationId);
    }

    // Derives a safe Folders-request display name from the workspace-relative path's final segment.
    // The Folders PathMetadata.displayName contract rejects slashes and control characters, so only the
    // last path segment is used. Folders-returned metadata remains the authority for the stored label.
    private static string DeriveDisplayName(string normalizedPath)
    {
        string trimmed = normalizedPath.TrimEnd('/');
        int lastSlash = trimmed.LastIndexOf('/');
        string segment = lastSlash >= 0 ? trimmed[(lastSlash + 1)..] : trimmed;
        return string.IsNullOrWhiteSpace(segment) ? "file" : segment;
    }

    /// <inheritdoc />
    public Task<ProjectFileReferenceValidationResult> RefreshFileReferenceAsync(
        ProjectId projectId,
        string fileReferenceId,
        string folderId,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);

        // Story 3.4 capability-gate HALT (option (a)): the Folders typed client does not expose a stable
        // read route that validates a file reference by opaque (folderId, fileReferenceId) without
        // workspaceId / filePath inputs. Projects MUST NOT store workspaceId / filePath per
        // docs/payload-taxonomy.md, so the recheck cannot be performed in v1. Fail closed by returning
        // Unavailable; the outcome mapper translates that to ReferenceState.Unavailable when the handler
        // chooses to invoke this method. The default Story 3.4 handler does NOT invoke this method —
        // file references retain their projection-stored state until a Folders submodule story adds an
        // opaque-id-only read route.
        _ = fileReferenceId;
        _ = folderId;
        _ = taskId;
        _ = cancellationToken;
        return Task.FromResult(new ProjectFileReferenceValidationResult(
            ProjectFileReferenceValidationOutcome.Unavailable,
            correlationId));
    }

    private static ProjectFileReferenceValidationOutcome MapFoldersStatus(int statusCode)
        => statusCode switch
        {
            400 or 413 or 422 => ProjectFileReferenceValidationOutcome.ValidationFailed,
            401 or 403 or 404 => ProjectFileReferenceValidationOutcome.Denied,
            409 => ProjectFileReferenceValidationOutcome.Archived,
            408 or 503 => ProjectFileReferenceValidationOutcome.Unavailable,
            >= 500 and < 600 => ProjectFileReferenceValidationOutcome.Unavailable,
            _ => ProjectFileReferenceValidationOutcome.Denied,
        };
}
