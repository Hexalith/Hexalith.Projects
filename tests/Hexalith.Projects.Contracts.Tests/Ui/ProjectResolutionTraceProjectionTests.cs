// <copyright file="ProjectResolutionTraceProjectionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Ui;

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 tests for Story 5.6 transient resolution trace UI descriptors.
/// </summary>
public sealed class ProjectResolutionTraceProjectionTests
{
    [Theory]
    [InlineData(typeof(ProjectResolutionTraceProjection))]
    [InlineData(typeof(ProjectResolutionTraceCandidateProjection))]
    [InlineData(typeof(ProjectResolutionTraceExclusionProjection))]
    public void TraceDescriptorCarriesFrontComposerMetadata(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        type.GetCustomAttributes(typeof(ProjectionAttribute), inherit: false).ShouldHaveSingleItem();
        ProjectionRoleAttribute role = type.GetCustomAttributes(typeof(ProjectionRoleAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ProjectionRoleAttribute>();
        role.Role.ShouldBe(ProjectionRole.DetailRecord);

        BoundedContextAttribute context = type.GetCustomAttributes(typeof(BoundedContextAttribute), inherit: false)
            .ShouldHaveSingleItem()
            .ShouldBeOfType<BoundedContextAttribute>();
        context.Name.ShouldBe("Projects");

        type.GetCustomAttributes(typeof(DisplayAttribute), inherit: false).ShouldHaveSingleItem();
    }

    [Fact]
    public void TraceDescriptorUsesStableContractVersionAndSharedVocabulary()
    {
        ProjectResolutionTraceProjection.ContractVersionValue.ShouldBe("projects.resolution-trace.ui.v1");
        typeof(ProjectResolutionTraceProjection)
            .GetProperty(nameof(ProjectResolutionTraceProjection.Result))!
            .PropertyType.ShouldBe(typeof(ResolutionResult?));
        typeof(ProjectResolutionTraceExclusionProjection)
            .GetProperty(nameof(ProjectResolutionTraceExclusionProjection.ReferenceState))!
            .PropertyType.ShouldBe(typeof(ReferenceState));
        typeof(ProjectResolutionTraceExclusionProjection)
            .GetProperty(nameof(ProjectResolutionTraceExclusionProjection.ReasonCode))!
            .PropertyType.ShouldBe(typeof(ProjectReasonCode?));
    }

    [Fact]
    public void TraceDescriptorsDoNotDeclareTenantPayloadCorrelationOrTraceHistoryFields()
    {
        string[] forbiddenFragments =
        [
            "Tenant",
            "Payload",
            "Transcript",
            "Prompt",
            "FilePath",
            "FileContent",
            "Workspace",
            "Secret",
            "Token",
            "Command",
            "Proposal",
            "Correlation",
            "Task",
            "TraceId",
            "History",
        ];

        foreach (Type type in new[]
        {
            typeof(ProjectResolutionTraceProjection),
            typeof(ProjectResolutionTraceCandidateProjection),
            typeof(ProjectResolutionTraceExclusionProjection),
        })
        {
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                forbiddenFragments.Any(fragment => property.Name.Contains(fragment, StringComparison.Ordinal))
                    .ShouldBeFalse($"{type.Name}.{property.Name} must stay metadata-only and trace-history-free.");
            }
        }
    }

    [Fact]
    public void IncompleteAdvisorySerializesNullResultAndCoverageWithoutInventingSourceTime()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        DateTimeOffset evaluated = DateTimeOffset.Parse("2026-11-06T01:00:00Z");
        ProjectResolutionTraceProjection incomplete = new()
        {
            Result = null,
            Incomplete = true,
            UnsupportedInputKinds = "file",
            EvaluatedAt = evaluated,
        };
        string serialized = JsonSerializer.Serialize(incomplete, options);
        using JsonDocument wire = JsonDocument.Parse(serialized);
        wire.RootElement.GetProperty("result").ValueKind.ShouldBe(JsonValueKind.Null);
        wire.RootElement.GetProperty("incomplete").GetBoolean().ShouldBeTrue();
        wire.RootElement.GetProperty("unsupportedInputKinds").GetString().ShouldBe("file");
        wire.RootElement.GetProperty("evaluatedAt").GetDateTimeOffset().ShouldBe(evaluated);
        wire.RootElement.TryGetProperty("observedAt", out _).ShouldBeFalse();
        serialized.ShouldNotContain("0001-01-01");
        ProjectResolutionTraceProjection roundTrip = JsonSerializer.Deserialize<ProjectResolutionTraceProjection>(serialized, options)!;
        roundTrip.Result.ShouldBeNull();
        roundTrip.Incomplete.ShouldBeTrue();
        roundTrip.UnsupportedInputKinds.ShouldBe("file");
        ProjectResolutionTraceProjection complete = new()
        {
            Result = ResolutionResult.NoMatch,
            EvaluatedAt = evaluated
        };
        using JsonDocument completed = JsonDocument.Parse(JsonSerializer.Serialize(complete, options));
        completed.RootElement.GetProperty("result").GetString().ShouldBe("NoMatch");
        completed.RootElement.GetProperty("incomplete").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void ExclusionDiagnosticRejectsFreeText()
    {
        var row = new ProjectResolutionTraceExclusionProjection();

        row.Diagnostic = ProjectContextInclusionDiagnostic.ReferenceUnavailable;
        row.Diagnostic.ShouldBe(ProjectContextInclusionDiagnostic.ReferenceUnavailable);
        Should.Throw<ArgumentException>(() => row.Diagnostic = "raw upstream denial body");
    }
}
