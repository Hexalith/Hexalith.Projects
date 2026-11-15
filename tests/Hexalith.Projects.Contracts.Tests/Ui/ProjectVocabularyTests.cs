// <copyright file="ProjectVocabularyTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Ui;

using System;
using System.Collections.Generic;
using System.Text.Json;

using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 tests for the single shared vocabulary wire shape: name-based JSON for every member. Pure: no
/// infrastructure. Presentation descriptors, badge slots and FrontComposer metadata are tested with the
/// UI-only contracts in <c>Hexalith.Projects.UI.Tests</c>.
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
}
