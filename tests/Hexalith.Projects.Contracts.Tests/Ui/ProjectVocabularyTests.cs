// <copyright file="ProjectVocabularyTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Ui;

using System;
using System.Collections.Generic;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 tests for the single shared <c>[ProjectionBadge]</c> vocabulary: name-based JSON,
/// total descriptor coverage, and per-enum code uniqueness. Pure: no infrastructure.
/// </summary>
public sealed class ProjectVocabularyTests
{
    public static IEnumerable<object[]> LifecycleMembers() => Members<ProjectLifecycle>();

    public static IEnumerable<object[]> ReferenceStateMembers() => Members<ReferenceState>();

    public static IEnumerable<object[]> ResolutionResultMembers() => Members<ResolutionResult>();

    public static IEnumerable<object[]> ReasonCodeMembers() => Members<ProjectReasonCode>();

    [Theory]
    [MemberData(nameof(LifecycleMembers))]
    public void LifecycleSerializesByName(ProjectLifecycle value)
        => AssertNameBasedJson(value);

    [Theory]
    [MemberData(nameof(ReferenceStateMembers))]
    public void ReferenceStateSerializesByName(ReferenceState value)
        => AssertNameBasedJson(value);

    [Theory]
    [MemberData(nameof(ResolutionResultMembers))]
    public void ResolutionResultSerializesByName(ResolutionResult value)
        => AssertNameBasedJson(value);

    [Theory]
    [MemberData(nameof(ReasonCodeMembers))]
    public void ReasonCodeSerializesByName(ProjectReasonCode value)
        => AssertNameBasedJson(value);

    [Fact]
    public void DescriptorLookupCoversEveryLifecycleMember()
        => AssertTotalDescriptorCoverage(ProjectVocabularyDescriptors.Lifecycle, ProjectVocabularyDescriptors.Describe);

    [Fact]
    public void DescriptorLookupCoversEveryReferenceStateMember()
        => AssertTotalDescriptorCoverage(ProjectVocabularyDescriptors.ReferenceStates, ProjectVocabularyDescriptors.Describe);

    [Fact]
    public void DescriptorLookupCoversEveryResolutionResultMember()
        => AssertTotalDescriptorCoverage(ProjectVocabularyDescriptors.ResolutionResults, ProjectVocabularyDescriptors.Describe);

    [Fact]
    public void DescriptorLookupCoversEveryReasonCodeMember()
        => AssertTotalDescriptorCoverage(ProjectVocabularyDescriptors.ReasonCodes, ProjectVocabularyDescriptors.Describe);

    [Fact]
    public void SeverityIsReadFromProjectionBadgeAttribute()
    {
        ProjectVocabularyDescriptors.Describe(ProjectLifecycle.Active).Severity.ShouldBe(BadgeSlot.Success);
        ProjectVocabularyDescriptors.Describe(ProjectLifecycle.Archived).Severity.ShouldBe(BadgeSlot.Warning);
        ProjectVocabularyDescriptors.Describe(ReferenceState.Unauthorized).Severity.ShouldBe(BadgeSlot.Danger);
        ProjectVocabularyDescriptors.Describe(ResolutionResult.SingleCandidate).Severity.ShouldBe(BadgeSlot.Success);
        ProjectVocabularyDescriptors.Describe(ProjectReasonCode.ConversationLinked).Severity.ShouldBe(BadgeSlot.Info);
    }

    [Fact]
    public void DescriptorCodesAreUniqueWithinEachEnum()
    {
        AssertUniqueCodes(ProjectVocabularyDescriptors.Lifecycle.Values);
        AssertUniqueCodes(ProjectVocabularyDescriptors.ReferenceStates.Values);
        AssertUniqueCodes(ProjectVocabularyDescriptors.ResolutionResults.Values);
        AssertUniqueCodes(ProjectVocabularyDescriptors.ReasonCodes.Values);
    }

    [Fact]
    public void GetSeverityThrowsWhenMemberLacksBadgeAttribute()
        => Should.Throw<InvalidOperationException>(() => ProjectVocabularyDescriptors.GetSeverity(UnbadgedEnum.Member));

    private static IEnumerable<object[]> Members<TEnum>()
        where TEnum : struct, Enum
    {
        foreach (TEnum value in (TEnum[])Enum.GetValues(typeof(TEnum)))
        {
            yield return [value];
        }
    }

    private static void AssertNameBasedJson<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        // The wire/serialization shape MUST be the member NAME, never the integer ordinal — this is the
        // schema-tolerance guarantee (members can be inserted/renamed/renumbered without breaking the wire).
        string json = JsonSerializer.Serialize(value);
        json.ShouldBe($"\"{value}\"");
        json.ShouldNotBe(Convert.ToInt32(value).ToString(System.Globalization.CultureInfo.InvariantCulture));

        // Name round-trips back to the same member.
        JsonSerializer.Deserialize<TEnum>(json).ShouldBe(value);

        // An unknown name is rejected (the converter does not silently accept arbitrary strings).
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<TEnum>("\"__not_a_member__\""));
    }

    private static void AssertTotalDescriptorCoverage<TEnum>(
        IReadOnlyDictionary<TEnum, VocabularyDescriptor> lookup,
        Func<TEnum, VocabularyDescriptor> describe)
        where TEnum : struct, Enum
    {
        TEnum[] members = (TEnum[])Enum.GetValues(typeof(TEnum));
        lookup.Count.ShouldBe(members.Length);
        foreach (TEnum member in members)
        {
            lookup.ShouldContainKey(member);
            VocabularyDescriptor descriptor = describe(member);
            descriptor.Code.ShouldBe(member.ToString());
            descriptor.DisplayLabel.ShouldNotBeNullOrWhiteSpace();
            descriptor.AccessibleName.ShouldNotBeNullOrWhiteSpace();
            descriptor.Severity.ShouldBe(ProjectVocabularyDescriptors.GetSeverity(member));
        }
    }

    private static void AssertUniqueCodes(IEnumerable<VocabularyDescriptor> descriptors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (VocabularyDescriptor descriptor in descriptors)
        {
            seen.Add(descriptor.Code).ShouldBeTrue($"Duplicate vocabulary code '{descriptor.Code}'.");
        }
    }

    private enum UnbadgedEnum
    {
        Member,
    }
}
