// <copyright file="LiveFixtureGraph.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

/// <summary>Metadata-only state used to exercise supported sibling HTTP contracts.</summary>
public sealed record LiveFixtureGraph(
    string GraphId,
    string RunId,
    string Scenario,
    string TenantId,
    string PrincipalId,
    string ProjectId,
    string SecondaryProjectId,
    string ProposalProjectId,
    string ProposalRetryProjectId,
    string ConversationId,
    string AmbiguousConversationId,
    string ExistingConversationId,
    string FolderId,
    string SecondaryFolderId,
    string ProposalFolderId,
    string WorkspaceId,
    string FileReferenceId,
    string SecondaryFileReferenceId,
    string ProposalFileReferenceId,
    string DeniedFileReferenceId,
    string FilePath,
    string MemoryReferenceId)
{
    /// <summary>Validates the bounded graph envelope at the fixture ingress.</summary>
    /// <exception cref="ArgumentException">Thrown when required metadata is empty or unbounded.</exception>
    public void Validate()
    {
        foreach (string value in Values())
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (value.Length > 128)
            {
                throw new ArgumentException("Fixture metadata values must not exceed 128 characters.", nameof(value));
            }
        }

        if (FilePath.StartsWith('/') || FilePath.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("The fixture file path must be workspace-relative and normalized.", nameof(FilePath));
        }
    }

    private IEnumerable<string> Values()
    {
        yield return GraphId;
        yield return RunId;
        yield return Scenario;
        yield return TenantId;
        yield return PrincipalId;
        yield return ProjectId;
        yield return SecondaryProjectId;
        yield return ProposalProjectId;
        yield return ProposalRetryProjectId;
        yield return ConversationId;
        yield return AmbiguousConversationId;
        yield return ExistingConversationId;
        yield return FolderId;
        yield return SecondaryFolderId;
        yield return ProposalFolderId;
        yield return WorkspaceId;
        yield return FileReferenceId;
        yield return SecondaryFileReferenceId;
        yield return ProposalFileReferenceId;
        yield return DeniedFileReferenceId;
        yield return FilePath;
        yield return MemoryReferenceId;
    }
}
