// <copyright file="ProjectContextInclusionPolicyNonAllowlistedKindTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Context;

using System.Linq;

using Hexalith.Projects.Context;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Testing.Context;

using Microsoft.Extensions.Logging;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Context.ProjectContextEvidenceBuilder;

/// <summary>
/// Tier-1 tests for the <see cref="ProjectContextInclusionCheck.ReferenceKindAllowlist"/> final
/// check (Story 3.1 AC 5, AC 12). The allowlist enforces <c>folder</c> / <c>file</c> /
/// <c>memory</c> / <c>conversation</c> only; everything else is excluded with
/// <see cref="ReferenceState.InvalidReference"/>,
/// <see cref="ProjectContextInclusionDiagnostic.ReferenceKindNotAllowlisted"/>, and a structured
/// log warning.
/// </summary>
public sealed class ProjectContextInclusionPolicyNonAllowlistedKindTests
{
    [Theory]
    [InlineData("workspace")]
    [InlineData("embedding")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a-very-long-string-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("‮‭")]
    public void IsAllowlisted_NonAllowlistedKind_ReturnsFalse(string referenceKind)
        => ProjectContextInclusionOrder.IsAllowlisted(referenceKind).ShouldBeFalse();

    [Theory]
    [InlineData("folder")]
    [InlineData("file")]
    [InlineData("memory")]
    [InlineData("conversation")]
    public void IsAllowlisted_AllowlistedKind_ReturnsTrue(string referenceKind)
        => ProjectContextInclusionOrder.IsAllowlisted(referenceKind).ShouldBeTrue();

    [Fact]
    public void IsAllowlisted_Null_ReturnsFalse()
        => ProjectContextInclusionOrder.IsAllowlisted(null).ShouldBeFalse();

    [Fact]
    public void Logger_HappyPathAssemble_EmitsNoWarning()
    {
        // The policy's RecordNonAllowlistedKind warning path is only entered when a candidate emits a
        // reference kind outside the four-value allowlist. The four built-in candidate evidence types
        // (folder/file/memory/conversation) all use hard-coded allowlisted kind strings, so the
        // warning is unreachable through the public Assemble surface today. This test asserts that
        // happy-path Assemble runs do NOT emit any warning entries — a regression guard for future
        // candidate-evidence types that might leak a non-allowlisted kind through.
        RecordingLogger<ProjectContextInclusionPolicy> recordingLogger = new();
        ProjectContextInclusionPolicy policy = new(recordingLogger);

        _ = policy.Assemble(Context(), Project(), TenantAccess(), WithAllKinds());

        recordingLogger.Entries.ShouldBeEmpty();
        recordingLogger.IsEnabled(LogLevel.Warning).ShouldBeTrue();
    }

    [Fact]
    public void AllowlistedReferenceKinds_AreExactlyFour()
    {
        ProjectContextInclusionOrder.AllowlistedReferenceKinds.Count.ShouldBe(4);
        ProjectContextInclusionOrder.AllowlistedReferenceKinds
            .OrderBy(static k => k, System.StringComparer.Ordinal)
            .ShouldBe(["conversation", "file", "folder", "memory"]);
    }
}
