// <copyright file="ProjectsMcpNoPayloadLeakageTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp.Tests;

using System.Reflection;
using System.Text.Json;

using Hexalith.Projects.Mcp;

using Shouldly;

using Xunit;

public sealed class ProjectsMcpNoPayloadLeakageTests
{
    // Property names that would carry an actual payload value (not a safe boolean requirement flag).
    private static readonly string[] ForbiddenExactPropertyNames =
    [
        "IdempotencyKey",
        "CommandBody",
        "ProposalBody",
        "MemoryBody",
        "ProblemBody",
        "Score",
        "Rank",
    ];

    private static readonly string[] ForbiddenPropertyNameTokens =
    [
        "Transcript",
        "Secret",
        "Token",
        "Prompt",
        "RawProblem",
        "ProblemDetail",
        "FilePath",
        "RejectedCandidate",
    ];

    /// <summary>
    /// Story 5.11 AC4: every MCP resource DTO (not just the audit row) must stay metadata-only.
    /// Iterating the manifest guarantees newly added Story 5.3-5.10 surfaces are all covered, while the
    /// exact-name guard avoids false positives on safe requirement flags such as
    /// <c>RequiresIdempotencyKey</c>.
    /// </summary>
    [Fact]
    public void Every_Mcp_Resource_Dto_Declares_No_Payload_Bearing_Property_And_Keeps_Safe_Fields()
    {
        foreach (Type dto in ProjectsMcpDescriptors.Manifest.Resources
            .Select(static r => Type.GetType(r.ProjectionTypeName, throwOnError: true)!))
        {
            string[] propertyNames = dto
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(static p => p.Name)
                .ToArray();

            foreach (string propertyName in propertyNames)
            {
                ForbiddenExactPropertyNames.ShouldNotContain(
                    propertyName,
                    $"{dto.Name}.{propertyName} would expose a payload-bearing value.");

                foreach (string token in ForbiddenPropertyNameTokens)
                {
                    propertyName.Contains(token, StringComparison.OrdinalIgnoreCase).ShouldBeFalse(
                        $"{dto.Name}.{propertyName} matches forbidden payload token '{token}'.");
                }
            }

            propertyNames.ShouldContain("TenantScope", $"{dto.Name} must expose server-derived TenantScope.");
            propertyNames.ShouldContain("ShortExplanation", $"{dto.Name} must expose a safe ShortExplanation.");
            propertyNames.ShouldContain("PayloadExcluded", $"{dto.Name} must assert PayloadExcluded.");
        }
    }

    [Fact]
    public void Mcp_Result_Dtos_Do_Not_Serialize_Payload_Bearing_Fields()
    {
        string json = JsonSerializer.Serialize(new ProjectsMcpAuditTimelineItem(
            "project-1",
            "audit-1",
            "archive",
            DateTimeOffset.UnixEpoch,
            "corr-1",
            "task-1",
            "file",
            "file-1",
            "archived",
            "server-derived tenant",
            "Safe audit evidence.",
            PayloadExcluded: true));

        json.ShouldNotContain("idempotency", Case.Insensitive);
        json.ShouldNotContain("commandBody", Case.Insensitive);
        json.ShouldNotContain("problemDetails", Case.Insensitive);
        json.ShouldNotContain("transcript", Case.Insensitive);
        json.ShouldNotContain("filePath", Case.Insensitive);
        json.ShouldNotContain("memoryBody", Case.Insensitive);
    }
}
