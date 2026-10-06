// <copyright file="LiveFixtureGraph.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

/// <summary>
/// Metadata-only state used to exercise supported sibling HTTP contracts. The wire shape is symmetric:
/// the runner posts exactly these members and every fixture response returns exactly these members.
/// </summary>
/// <param name="GraphId">The run-scoped graph identity.</param>
/// <param name="RunId">The managed run identity.</param>
/// <param name="WorkerIndex">The Playwright worker index that owns the graph.</param>
/// <param name="Retry">The Playwright retry index that owns the graph.</param>
/// <param name="RepeatEachIndex">The Playwright repeat index that owns the graph.</param>
/// <param name="Scenario">The bounded scenario label.</param>
/// <param name="TenantId">The token-derived tenant identity.</param>
/// <param name="PrincipalId">The token-derived principal identity.</param>
/// <param name="ProjectId">The caller-owned primary Project identity.</param>
/// <param name="SecondaryProjectId">The caller-owned secondary Project identity.</param>
/// <param name="ProposalProjectId">The caller-owned proposal Project identity.</param>
/// <param name="ProposalRetryProjectId">The caller-owned proposal retry Project identity.</param>
/// <param name="ConversationId">The unlinked conversation identity.</param>
/// <param name="AmbiguousConversationId">The ambiguous conversation identity.</param>
/// <param name="ExistingConversationId">The conversation already assigned to the primary Project.</param>
/// <param name="FolderId">The primary folder identity.</param>
/// <param name="SecondaryFolderId">The secondary folder identity.</param>
/// <param name="ProposalFolderId">The proposal folder identity.</param>
/// <param name="WorkspaceId">The folder workspace identity.</param>
/// <param name="FileReferenceId">The primary file reference identity.</param>
/// <param name="SecondaryFileReferenceId">The secondary file reference identity.</param>
/// <param name="ProposalFileReferenceId">The proposal file reference identity.</param>
/// <param name="DeniedFileReferenceId">The file reference identity that must fail closed.</param>
/// <param name="FilePath">The normalized workspace-relative file path.</param>
/// <param name="MemoryReferenceId">The memory reference identity.</param>
/// <param name="CorrelationId">The graph correlation identity.</param>
/// <param name="TaskId">The graph task identity.</param>
/// <param name="IdempotencyKey">The graph idempotency root.</param>
public sealed record LiveFixtureGraph(
    string GraphId,
    string RunId,
    int WorkerIndex,
    int Retry,
    int RepeatEachIndex,
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
    string MemoryReferenceId,
    string CorrelationId,
    string TaskId,
    string IdempotencyKey)
{
    /// <summary>Gets the maximum length of one metadata value accepted at the fixture ingress.</summary>
    public const int MaxValueLength = 128;

    /// <summary>Validates the bounded graph envelope at the fixture ingress without echoing values.</summary>
    /// <returns><see langword="true"/> when every metadata value is present, bounded, and normalized.</returns>
    public bool IsValid()
    {
        if (WorkerIndex < 0 || Retry < 0 || RepeatEachIndex < 0)
        {
            return false;
        }

        foreach (string? value in Values())
        {
            if (string.IsNullOrWhiteSpace(value)
                || value.Length > MaxValueLength
                || value.Any(char.IsControl))
            {
                return false;
            }
        }

        return !FilePath.StartsWith('/')
            && !FilePath.Contains('\\', StringComparison.Ordinal)
            && !FilePath.Contains("..", StringComparison.Ordinal);
    }

    private IEnumerable<string?> Values()
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
        yield return CorrelationId;
        yield return TaskId;
        yield return IdempotencyKey;
    }
}
