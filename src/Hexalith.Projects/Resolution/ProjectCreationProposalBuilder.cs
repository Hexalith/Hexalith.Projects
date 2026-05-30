// <copyright file="ProjectCreationProposalBuilder.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Resolution;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Pure metadata-only proposal derivation helpers for Story 4.5.</summary>
public static class ProjectCreationProposalBuilder
{
    private const string FallbackName = "New project";

    /// <summary>Builds a safe proposal when the current resolution result is NoMatch.</summary>
    public static ProjectCreationProposal? TryBuild(
        ResolutionResult resolutionResult,
        string conversationId,
        string? callerSuggestedName,
        string? conversationLabel,
        string? attachmentLabel,
        string? description,
        string? setupMetadata,
        string? folderId,
        IReadOnlyList<string> fileReferenceIds,
        DateTimeOffset observedAt,
        string freshness)
    {
        if (resolutionResult != ResolutionResult.NoMatch)
        {
            return null;
        }

        string selectedName = FirstSafeName(callerSuggestedName, conversationLabel, attachmentLabel) ?? FallbackName;
        ProjectCommandValidationResult validation = ValidateProjectMetadata(selectedName, description, setupMetadata);
        if (!validation.IsAccepted || string.IsNullOrWhiteSpace(validation.CanonicalName))
        {
            return null;
        }

        return new ProjectCreationProposal(
            ResolutionResult.NoMatch,
            validation.CanonicalName,
            validation.CanonicalDescription,
            validation.CanonicalSetupMetadata,
            conversationId,
            Normalize(folderId),
            fileReferenceIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray(),
            observedAt,
            freshness,
            Array.Empty<string>());
    }

    /// <summary>Returns whether create metadata is safe under the existing Project command validator.</summary>
    public static bool IsSafeCreateMetadata(string displayName, string? description, string? setupMetadata)
        => ValidateProjectMetadata(displayName, description, setupMetadata).IsAccepted;

    private static string? FirstSafeName(params string?[] candidates)
    {
        foreach (string? candidate in candidates)
        {
            string? normalized = Normalize(candidate);
            if (normalized is null)
            {
                continue;
            }

            if (ValidateProjectMetadata(normalized, description: null, setupMetadata: null).IsAccepted)
            {
                return normalized;
            }
        }

        return null;
    }

    private static ProjectCommandValidationResult ValidateProjectMetadata(string displayName, string? description, string? setupMetadata)
        => ProjectCommandValidator.Validate(new CreateProject(
            TenantId: "tenant-proposal-validation",
            ProjectId: new ProjectId("project-proposal-validation"),
            Name: displayName,
            Description: description,
            SetupMetadata: setupMetadata,
            ActorPrincipalId: "principal-proposal-validation",
            CorrelationId: "correlation-proposal-validation",
            TaskId: "task-proposal-validation",
            IdempotencyKey: "idempotency-proposal-validation"));

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
