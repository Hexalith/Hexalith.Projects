// <copyright file="PayloadClassificationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Models;

using System;
using System.Linq;

using Hexalith.Projects.Contracts.Models;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 tests for the machine-usable payload-classification allowlist (FS-1, NFR-2) that the FS-2
/// <c>NoPayloadLeakage</c> harness (Story 1.4) is built against. Pure: no infrastructure.
/// </summary>
public sealed class PayloadClassificationTests
{
    [Fact]
    public void SafeFieldsAndForbiddenContentAreNonEmpty()
    {
        PayloadClassification.SafeFields.ShouldNotBeEmpty();
        PayloadClassification.ForbiddenContent.ShouldNotBeEmpty();
    }

    [Fact]
    public void SafeAndForbiddenSetsDoNotOverlap()
    {
        PayloadClassification.SafeFields
            .Intersect(PayloadClassification.ForbiddenContent, StringComparer.Ordinal)
            .ShouldBeEmpty();
    }

    [Theory]
    [InlineData("OpaqueId")]
    [InlineData("TenantId")]
    [InlineData("ReasonCode")]
    [InlineData("SetupPreference")]
    [InlineData("CorrelationId")]
    [InlineData("TransientTraceMetadata")]
    public void SafeFieldsContainsExpectedCategories(string category)
    {
        PayloadClassification.IsSafe(category).ShouldBeTrue();
        PayloadClassification.IsForbidden(category).ShouldBeFalse();
    }

    [Theory]
    [InlineData("ConversationTranscriptText")]
    [InlineData("FileContents")]
    [InlineData("MemoryBody")]
    [InlineData("RawPrompt")]
    [InlineData("Secret")]
    [InlineData("RawToken")]
    [InlineData("FullCommandBody")]
    [InlineData("SensitiveFolderName")]
    public void ForbiddenContentContainsExpectedCategories(string category)
    {
        PayloadClassification.IsForbidden(category).ShouldBeTrue();
        PayloadClassification.IsSafe(category).ShouldBeFalse();
    }

    [Fact]
    public void ExposesTaxonomyDocumentPathAndSourceOfTruthStatement()
    {
        PayloadClassification.TaxonomyDocumentPath.ShouldBe("docs/payload-taxonomy.md");
        PayloadClassification.SourceOfTruthStatement.ShouldContain("NoPayloadLeakage");
    }

    [Fact]
    public void IsSafeRejectsNull()
        => Should.Throw<ArgumentNullException>(() => PayloadClassification.IsSafe(null!));
}
