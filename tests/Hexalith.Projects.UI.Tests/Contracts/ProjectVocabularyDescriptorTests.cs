// <copyright file="ProjectVocabularyDescriptorTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 tests for the UI-only shared-vocabulary descriptors: total descriptor coverage, per-enum code
/// uniqueness, and the golden badge slots that keep rendered badges identical after the seven Contracts
/// enums stopped carrying <c>[ProjectionBadge]</c>. Pure: no infrastructure.
/// </summary>
public sealed class ProjectVocabularyDescriptorTests
{
    private static readonly Type[] _strippedEnums =
    [
        typeof(ProjectLifecycle),
        typeof(ReferenceState),
        typeof(ResolutionResult),
        typeof(ProjectReasonCode),
        typeof(ProjectContextAssemblyOutcome),
        typeof(ProjectContextFreshness),
        typeof(ProjectContextInclusionCheck),
    ];

    /// <summary>
    /// Gets every member of the seven stripped enums with the slot its <c>[ProjectionBadge]</c> attribute
    /// declared before the split (Projects baseline 6bea95a3). Change a row only with an intentional,
    /// reviewed badge change.
    /// </summary>
    /// <returns>The pre-split golden slots.</returns>
    public static TheoryData<Enum, BadgeSlot> PreSplitBadgeSlots()
        => new()
        {
            { ProjectLifecycle.Active, BadgeSlot.Success },
            { ProjectLifecycle.Archived, BadgeSlot.Warning },
            { ReferenceState.Pending, BadgeSlot.Warning },
            { ReferenceState.Included, BadgeSlot.Success },
            { ReferenceState.Excluded, BadgeSlot.Neutral },
            { ReferenceState.Unauthorized, BadgeSlot.Danger },
            { ReferenceState.Unavailable, BadgeSlot.Danger },
            { ReferenceState.Stale, BadgeSlot.Warning },
            { ReferenceState.Archived, BadgeSlot.Warning },
            { ReferenceState.Ambiguous, BadgeSlot.Warning },
            { ReferenceState.TenantMismatch, BadgeSlot.Danger },
            { ReferenceState.Conflict, BadgeSlot.Danger },
            { ReferenceState.InvalidReference, BadgeSlot.Danger },
            { ResolutionResult.NoMatch, BadgeSlot.Neutral },
            { ResolutionResult.SingleCandidate, BadgeSlot.Success },
            { ResolutionResult.MultipleCandidates, BadgeSlot.Warning },
            { ProjectReasonCode.ConversationLinked, BadgeSlot.Info },
            { ProjectReasonCode.ProjectFolderMatched, BadgeSlot.Info },
            { ProjectReasonCode.FileReferenceMatched, BadgeSlot.Info },
            { ProjectReasonCode.MemoryMatched, BadgeSlot.Info },
            { ProjectReasonCode.MetadataMatched, BadgeSlot.Info },
            { ProjectContextAssemblyOutcome.Assembled, BadgeSlot.Success },
            { ProjectContextAssemblyOutcome.ProjectUnavailable, BadgeSlot.Neutral },
            { ProjectContextAssemblyOutcome.Unauthorized, BadgeSlot.Danger },
            { ProjectContextFreshness.Fresh, BadgeSlot.Success },
            { ProjectContextFreshness.Stale, BadgeSlot.Warning },
            { ProjectContextFreshness.Unavailable, BadgeSlot.Danger },
            { ProjectContextFreshness.Unknown, BadgeSlot.Neutral },
            { ProjectContextInclusionCheck.TenantAuthority, BadgeSlot.Info },
            { ProjectContextInclusionCheck.ProjectVisibility, BadgeSlot.Info },
            { ProjectContextInclusionCheck.ProjectLifecycle, BadgeSlot.Info },
            { ProjectContextInclusionCheck.ReferenceAuthorization, BadgeSlot.Info },
            { ProjectContextInclusionCheck.ReferenceLifecycle, BadgeSlot.Info },
            { ProjectContextInclusionCheck.ReferenceFreshness, BadgeSlot.Info },
            { ProjectContextInclusionCheck.ReferenceKindAllowlist, BadgeSlot.Info },
        };

    [Theory]
    [MemberData(nameof(PreSplitBadgeSlots))]
    public void SeverityMatchesPreSplitProjectionBadge(Enum member, BadgeSlot expected)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (Describe(member) is VocabularyDescriptor descriptor)
        {
            descriptor.Severity.ShouldBe(expected);
        }

        GetSeverity(member).ShouldBe(expected);
    }

    [Fact]
    public void GoldenSlotsCoverEveryMemberOfTheSevenStrippedEnums()
    {
        Enum[] golden = [.. PreSplitBadgeSlots().Select(row => (Enum)row.Data.Item1)];
        Enum[] members = [.. _strippedEnums.SelectMany(type => Enum.GetValues(type).Cast<Enum>())];

        golden.Distinct().Count().ShouldBe(golden.Length);
        golden.ShouldBe(members, ignoreOrder: true);
    }

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
    public void DescriptorCodesAreUniqueWithinEachEnum()
    {
        AssertUniqueCodes(ProjectVocabularyDescriptors.Lifecycle.Values);
        AssertUniqueCodes(ProjectVocabularyDescriptors.ReferenceStates.Values);
        AssertUniqueCodes(ProjectVocabularyDescriptors.ResolutionResults.Values);
        AssertUniqueCodes(ProjectVocabularyDescriptors.ReasonCodes.Values);
    }

    [Fact]
    public void GetSeverityThrowsWhenMemberHasNoDeclaredSlot()
    {
        Should.Throw<InvalidOperationException>(() => ProjectVocabularyDescriptors.GetSeverity(UnbadgedEnum.Member));
        Should.Throw<InvalidOperationException>(() => ProjectVocabularyDescriptors.GetSeverity((ProjectLifecycle)99));
    }

    private static VocabularyDescriptor? Describe(Enum member)
        => member switch
        {
            ProjectLifecycle value => ProjectVocabularyDescriptors.Describe(value),
            ReferenceState value => ProjectVocabularyDescriptors.Describe(value),
            ResolutionResult value => ProjectVocabularyDescriptors.Describe(value),
            ProjectReasonCode value => ProjectVocabularyDescriptors.Describe(value),
            _ => null,
        };

    private static BadgeSlot GetSeverity(Enum member)
        => member switch
        {
            ProjectLifecycle value => ProjectVocabularyDescriptors.GetSeverity(value),
            ReferenceState value => ProjectVocabularyDescriptors.GetSeverity(value),
            ResolutionResult value => ProjectVocabularyDescriptors.GetSeverity(value),
            ProjectReasonCode value => ProjectVocabularyDescriptors.GetSeverity(value),
            ProjectContextAssemblyOutcome value => ProjectVocabularyDescriptors.GetSeverity(value),
            ProjectContextFreshness value => ProjectVocabularyDescriptors.GetSeverity(value),
            ProjectContextInclusionCheck value => ProjectVocabularyDescriptors.GetSeverity(value),
            _ => throw new ArgumentOutOfRangeException(nameof(member), member, "Not a stripped vocabulary enum."),
        };

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
