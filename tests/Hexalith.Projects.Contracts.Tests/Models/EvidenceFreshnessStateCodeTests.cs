// <copyright file="EvidenceFreshnessStateCodeTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Models;

using Hexalith.Projects.Contracts.Models;

using Shouldly;

using Xunit;

/// <summary>Tests for the canonical reference-health freshness vocabulary.</summary>
public sealed class EvidenceFreshnessStateCodeTests
{
    [Theory]
    [InlineData("trusted", EvidenceFreshnessStateCode.Current)]
    [InlineData("fresh", EvidenceFreshnessStateCode.Current)]
    [InlineData("current", EvidenceFreshnessStateCode.Current)]
    [InlineData("  TrUsTeD  ", EvidenceFreshnessStateCode.Current)]
    [InlineData("stale", EvidenceFreshnessStateCode.Stale)]
    [InlineData("mixedGeneration", EvidenceFreshnessStateCode.Stale)]
    [InlineData(" MIXEDGENERATION ", EvidenceFreshnessStateCode.Stale)]
    [InlineData("rebuilding", EvidenceFreshnessStateCode.Rebuilding)]
    [InlineData(" ReBuIlDiNg ", EvidenceFreshnessStateCode.Rebuilding)]
    [InlineData("unavailable", EvidenceFreshnessStateCode.Unavailable)]
    [InlineData("unknown", EvidenceFreshnessStateCode.Unavailable)]
    [InlineData("forbidden", EvidenceFreshnessStateCode.Unavailable)]
    [InlineData("redacted", EvidenceFreshnessStateCode.Unavailable)]
    [InlineData(null, EvidenceFreshnessStateCode.Unavailable)]
    [InlineData("", EvidenceFreshnessStateCode.Unavailable)]
    [InlineData("   ", EvidenceFreshnessStateCode.Unavailable)]
    [InlineData("mixed-generation", EvidenceFreshnessStateCode.Unavailable)]
    [InlineData("future", EvidenceFreshnessStateCode.Unavailable)]
    public void NormalizeMapsApprovedInputsAndFailsClosed(string? input, string expected)
        => EvidenceFreshnessStateCode.Normalize(input).ShouldBe(expected);

    [Theory]
    [InlineData("trusted", "Current")]
    [InlineData("stale", "Stale")]
    [InlineData("rebuilding", "Rebuilding")]
    [InlineData("not-recognized", "Unavailable")]
    public void ToLabelUsesTheCanonicalTextVocabulary(string input, string expected)
        => EvidenceFreshnessStateCode.ToLabel(input).ShouldBe(expected);

    [Fact]
    public void CanonicalEnumContainsExactlyTheApprovedStatesAndDefaultFailsClosed()
    {
        Enum.GetValues<EvidenceFreshnessState>().ShouldBe(
            [
                EvidenceFreshnessState.Current,
                EvidenceFreshnessState.Stale,
                EvidenceFreshnessState.Rebuilding,
                EvidenceFreshnessState.Unavailable,
            ],
            ignoreOrder: false);

        EvidenceFreshnessState defaultState = default;
        Enum.IsDefined(defaultState).ShouldBeFalse();
        EvidenceFreshnessStateCode.Normalize(defaultState.ToString()).ShouldBe(EvidenceFreshnessStateCode.Unavailable);
    }
}
