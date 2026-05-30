// <copyright file="ProjectResolutionTraceProjectionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Ui;

using System.ComponentModel.DataAnnotations;
using System.Reflection;

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
            .PropertyType.ShouldBe(typeof(ResolutionResult));
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
    public void ExclusionDiagnosticRejectsFreeText()
    {
        var row = new ProjectResolutionTraceExclusionProjection();

        row.Diagnostic = ProjectContextInclusionDiagnostic.ReferenceUnavailable;
        row.Diagnostic.ShouldBe(ProjectContextInclusionDiagnostic.ReferenceUnavailable);
        Should.Throw<ArgumentException>(() => row.Diagnostic = "raw upstream denial body");
    }
}
