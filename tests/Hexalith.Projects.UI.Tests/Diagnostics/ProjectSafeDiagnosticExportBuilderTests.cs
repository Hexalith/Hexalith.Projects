// <copyright file="ProjectSafeDiagnosticExportBuilderTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Diagnostics;

using System.Text.Json;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Diagnostics;
using Hexalith.Projects.UI.Rendering;

using Shouldly;

using Xunit;

/// <summary>
/// Tests for the Story 5.7 metadata-only safe diagnostic export builder.
/// </summary>
public sealed class ProjectSafeDiagnosticExportBuilderTests
{
    [Fact]
    public void BuilderProducesDeterministicMetadataOnlyJson()
    {
        ProjectDetailLoadResult result = ProjectDetailLoadResult.FromDetail(
            Detail(),
            ProjectConsoleFeedback.Warning("data_unavailable", "corr-feedback"),
            ReferenceRows(),
            tenantScope: "server-derived tenant");

        string first = ProjectSafeDiagnosticExportBuilder.BuildJson(result, DateTimeOffset.UnixEpoch);
        string second = ProjectSafeDiagnosticExportBuilder.BuildJson(result, DateTimeOffset.UnixEpoch);

        first.ShouldBe(second);
        first.ShouldContain("\"schemaVersion\":\"projects.safe-diagnostic-export.v1\"");
        first.ShouldContain("\"generatedAt\":\"1970-01-01T00:00:00+00:00\"");
        first.ShouldContain("\"projectId\":\"project-001\"");
        first.ShouldContain("\"projectName\":\"Detail Project\"");
        first.ShouldContain("\"tenantScopeLabel\":\"server-derived tenant\"");
        first.ShouldContain("\"referenceHealthRows\"");
        first.ShouldContain("\"auditRows\"");
        first.ShouldContain("\"safeFeedbackCodes\":[\"data_unavailable\"]");
        first.ShouldContain("\"payloadExclusionGuarantee\":\"Payload-bearing data is excluded; this export contains metadata only.\"");
        first.ShouldNotContain("transcript", Case.Insensitive);
        first.ShouldNotContain("prompt", Case.Insensitive);
        first.ShouldNotContain("token", Case.Insensitive);
        first.ShouldNotContain("secret", Case.Insensitive);
        first.ShouldNotContain("score", Case.Insensitive);
        first.ShouldNotContain("rank", Case.Insensitive);
        first.ShouldNotContain("idempotency", Case.Insensitive);
        first.ShouldNotContain("command body", Case.Insensitive);
    }

    [Fact]
    public void BuilderProjectionSummarizesIncludedFieldsAndCounts()
    {
        ProjectSafeDiagnosticExportProjection projection = ProjectSafeDiagnosticExportBuilder.BuildProjection(
            ProjectDetailLoadResult.FromDetail(Detail(), referenceHealthRows: ReferenceRows()),
            DateTimeOffset.UnixEpoch);

        projection.ContractVersion.ShouldBe(ProjectSafeDiagnosticExportProjection.ContractVersionValue);
        projection.ProjectId.ShouldBe("project-001");
        projection.ProjectName.ShouldBe("Detail Project");
        projection.AuditRowCount.ShouldBe(1);
        projection.ReferenceHealthRowCount.ShouldBe(1);
        projection.IncludedFieldNames.ShouldContain("auditRows.auditEventId");
        projection.ExcludedPayloadCategories.ShouldContain("resolution-metrics");
        projection.PayloadExclusionGuarantee.ShouldBe(ProjectSafeDiagnosticExportProjection.PayloadExclusionGuaranteeText);
    }

    [Fact]
    public void IncludedFieldNamesEnumerateEveryEmittedLeafExactly()
    {
        ProjectDetailLoadResult result = ProjectDetailLoadResult.FromDetail(Detail(), referenceHealthRows: ReferenceRows());

        ProjectSafeDiagnosticExportDocument document = ProjectSafeDiagnosticExportBuilder.BuildDocument(result, DateTimeOffset.UnixEpoch);
        string json = ProjectSafeDiagnosticExportBuilder.BuildJson(result, DateTimeOffset.UnixEpoch);

        using JsonDocument parsed = JsonDocument.Parse(json);
        HashSet<string> emitted = [.. CollectLeafFieldPaths(string.Empty, parsed.RootElement)];
        HashSet<string> declared = [.. document.IncludedFieldNames];

        // The export's own field list is the canonical CLI/MCP parity contract; it must be a faithful,
        // complete accounting of what BuildJson actually serializes (AC6/AC8).
        emitted.Except(declared).ShouldBeEmpty("includedFieldNames is missing serialized export fields");
        declared.Except(emitted).ShouldBeEmpty("includedFieldNames declares fields the export does not serialize");
        document.IncludedFieldNames.Count.ShouldBe(document.IncludedFieldNames.Distinct().Count());
    }

    private static IEnumerable<string> CollectLeafFieldPaths(string prefix, JsonElement element)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string path = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (string nested in CollectLeafFieldPaths(path, property.Value))
                {
                    yield return nested;
                }
            }
            else if (property.Value.ValueKind == JsonValueKind.Array
                && property.Value.GetArrayLength() > 0
                && property.Value[0].ValueKind == JsonValueKind.Object)
            {
                // Object arrays (referenceHealthRows/auditRows) contribute one leaf per element field.
                foreach (string nested in CollectLeafFieldPaths(path, property.Value[0]))
                {
                    yield return nested;
                }
            }
            else
            {
                // Scalars and string arrays (safeFeedbackCodes/includedFieldNames/...) are leaves themselves.
                yield return path;
            }
        }
    }

    private static ProjectOperatorDiagnostic Detail()
        => new(
            "project-001",
            "Detail Project",
            "Safe metadata",
            "active",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            "safe-setup",
            null,
            new ProjectOperatorContextActivation(true, null),
            [
                new ProjectOperatorReferenceSummary(
                    "folder",
                    "included",
                    "folder-001",
                    "Folder",
                    "ProjectFolderMatched",
                    Freshness()),
            ],
            [
                new ProjectOperatorAuditTimelineItem(
                    "audit-001",
                    "project.created",
                    DateTimeOffset.UnixEpoch,
                    "actor-001",
                    "corr-001",
                    "task-001",
                    "folder",
                    "folder-001",
                    null,
                    "active",
                    "project_created",
                    null,
                    null,
                    1),
            ],
            Freshness());

    private static IReadOnlyList<ProjectReferenceHealthRowProjection> ReferenceRows()
        =>
        [
            ProjectReferenceHealthRowProjection.FromReferenceSummary(
                "project-001",
                new ProjectOperatorReferenceSummary(
                    "folder",
                    "included",
                    "folder-001",
                    "Folder",
                    "ProjectFolderMatched",
                    Freshness())),
        ];

    private static ProjectOperatorFreshnessMetadata Freshness()
        => new("eventually_consistent", DateTimeOffset.UnixEpoch, "watermark-001", false, "trusted");
}
