// <copyright file="ConversationStartSetupResponseTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Queries;

using System.Collections.Generic;
using System.Text.Json;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>Verifies value semantics at the supported response boundary while preserving the legacy DTO.</summary>
public sealed class ConversationStartSetupResponseTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 10, 24, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Response_OwnsEveryCollectionAndKeepsHashStable()
    {
        List<string> goals = ["goal"];
        List<string> instructions = ["instruction"];
        List<ProjectContextSourceKind> preferred = [ProjectContextSourceKind.Memory];
        List<ProjectContextSourceKind> excluded = [ProjectContextSourceKind.FileReference];
        List<ConversationStartComponent> components = [Component()];
        List<string> recovery = ["RefreshContext"];
        ConversationStartSetup legacy = Setup() with
        {
            Goals = goals,
            UserInstructions = instructions,
            PreferredSourceKinds = preferred,
            ExcludedSourceKinds = excluded,
        };
        ConversationStartSetupResponse response = new(legacy, Snapshot() with
        {
            Components = components,
            RecoveryActions = recovery,
        });
        string before = JsonSerializer.Serialize(response, JsonOptions);
        HashSet<ConversationStartSetupResponse> values = [response];
        int hash = response.GetHashCode();

        goals.Clear();
        instructions.Add("changed");
        preferred.Clear();
        excluded.Clear();
        components.Clear();
        recovery.Clear();

        JsonSerializer.Serialize(response, JsonOptions).ShouldBe(before);
        response.GetHashCode().ShouldBe(hash);
        values.Contains(response).ShouldBeTrue();
        // The legacy DTO still has its historical aliasing semantics.
        legacy.Goals.ShouldBeEmpty();
        Should.Throw<NotSupportedException>(() => ((IList<string>)response.Setup!.Goals).Clear());
        Should.Throw<NotSupportedException>(() => ((IList<string>)response.Setup!.UserInstructions).Clear());
        Should.Throw<NotSupportedException>(() => ((IList<ProjectContextSourceKind>)response.Setup!.PreferredSourceKinds).Clear());
        Should.Throw<NotSupportedException>(() => ((IList<ProjectContextSourceKind>)response.Setup!.ExcludedSourceKinds).Clear());
        Should.Throw<NotSupportedException>(() => ((IList<ConversationStartComponent>)response.Snapshot.Components).Clear());
        Should.Throw<NotSupportedException>(() => ((IList<string>)response.Snapshot.RecoveryActions).Clear());
    }

    [Fact]
    public void Response_IndependentValuesAndJsonRoundTripHaveEqualHashes()
    {
        ConversationStartSetupResponse first = new(Setup(), Snapshot());
        ConversationStartSetupResponse second = new(Setup(), Snapshot());
        ConversationStartSetupResponse roundTrip = JsonSerializer.Deserialize<ConversationStartSetupResponse>(
            JsonSerializer.Serialize(first, JsonOptions), JsonOptions)!;

        first.ShouldBe(second);
        first.ShouldBe(roundTrip);
        first.GetHashCode().ShouldBe(second.GetHashCode());
        first.GetHashCode().ShouldBe(roundTrip.GetHashCode());
        new HashSet<ConversationStartSetupResponse> { first }.Contains(roundTrip).ShouldBeTrue();
        first.Equals(null).ShouldBeFalse();
        ((object)first).Equals(second).ShouldBeTrue();
        (first == second).ShouldBeTrue();
    }

    [Fact]
    public void Response_WithExpressionsCopyReplacementCollections()
    {
        List<string> goals = ["replacement"];
        List<string> recovery = ["Retry"];
        ConversationStartSetupResponse original = new(Setup(), Snapshot());
        ConversationStartSetupResponse changed = original with
        {
            Setup = original.Setup! with { Goals = goals },
            Snapshot = original.Snapshot with { RecoveryActions = recovery },
        };
        goals.Clear();
        recovery.Clear();

        changed.Setup!.Goals.ShouldBe(["replacement"]);
        changed.Snapshot.RecoveryActions.ShouldBe(["Retry"]);
        original.Setup!.Goals.ShouldBe(["goal", "second"]);
        original.Snapshot.RecoveryActions.ShouldBe(["RefreshContext"]);
    }

    [Theory]
    [InlineData("project")]
    [InlineData("lifecycle")]
    [InlineData("goals")]
    [InlineData("instructions")]
    [InlineData("preferred")]
    [InlineData("excluded")]
    [InlineData("policy")]
    [InlineData("observed")]
    [InlineData("freshness")]
    [InlineData("state")]
    [InlineData("asOf")]
    [InlineData("version")]
    [InlineData("components")]
    [InlineData("recovery")]
    [InlineData("null")]
    public void Response_ChangedScalarOrOrderedCollectionIsUnequal(string field)
    {
        ConversationStartSetupResponse original = new(Setup(), Snapshot());
        ConversationStartSetup setup = original.Setup!;
        ConversationStartAdmissionSnapshot snapshot = original.Snapshot;
        ConversationStartSetupResponse changed = field switch
        {
            "project" => original with { Setup = setup with { ProjectId = "other" } },
            "lifecycle" => original with { Setup = setup with { Lifecycle = ProjectLifecycle.Archived } },
            "goals" => original with { Setup = setup with { Goals = ["second", "goal"] } },
            "instructions" => original with { Setup = setup with { UserInstructions = ["other"] } },
            "preferred" => original with { Setup = setup with { PreferredSourceKinds = [] } },
            "excluded" => original with { Setup = setup with { ExcludedSourceKinds = [] } },
            "policy" => original with { Setup = setup with { LinkedSourcePolicy = LinkedSourcePolicy.None } },
            "observed" => original with { Setup = setup with { ObservedAt = ObservedAt.AddSeconds(1) } },
            "freshness" => original with { Setup = setup with { Freshness = ProjectContextFreshness.Stale } },
            "state" => original with { Snapshot = snapshot with { ResponseState = ConversationStartResponseState.Complete } },
            "asOf" => original with { Snapshot = snapshot with { AsOf = ObservedAt.AddSeconds(1) } },
            "version" => original with { Snapshot = snapshot with { ProjectVersion = 2 } },
            "components" => original with { Snapshot = snapshot with { Components = [Component() with { Included = false }] } },
            "recovery" => original with { Snapshot = snapshot with { RecoveryActions = ["Retry"] } },
            _ => original with { Setup = null },
        };

        original.Equals(changed).ShouldBeFalse();
        changed.Equals(original).ShouldBeFalse();
        (original == changed).ShouldBeFalse();
    }

    [Fact]
    public void Response_NullSetupRoundTripsAsAValue()
    {
        ConversationStartSetupResponse first = new(null, Snapshot());
        ConversationStartSetupResponse second = JsonSerializer.Deserialize<ConversationStartSetupResponse>(
            JsonSerializer.Serialize(first, JsonOptions), JsonOptions)!;

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
        second.Setup.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Response_JsonRetainsTheCompletePublishedShape(bool nullSetup)
    {
        ConversationStartSetupResponse response = new(nullSetup ? null : Setup(), Snapshot());
        string expectedSetup = nullSetup ? "null" : """
            {"projectId":"project","lifecycle":"Active","goals":["goal","second"],"userInstructions":["instruction"],"preferredSourceKinds":["memory"],"excludedSourceKinds":["fileReference"],"linkedSourcePolicy":"authorizedReferences","observedAt":"2026-10-24T00:00:00+00:00","freshness":"Fresh"}
            """;
        string expected = "{\"setup\":" + expectedSetup + """
            ,"snapshot":{"responseState":"Unavailable","asOf":"2026-10-24T00:00:00+00:00","projectVersion":1,"components":[{"name":"Project","included":true,"freshness":"Current","reason":"current"}],"recoveryActions":["RefreshContext"]}}
            """;

        JsonSerializer.Serialize(response, JsonOptions).ShouldBe(expected);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("freshness")]
    [InlineData("reason")]
    [InlineData("component-order")]
    [InlineData("recovery-order")]
    [InlineData("preferred-order")]
    [InlineData("excluded-order")]
    public void Response_ComponentFieldsAndCollectionOrderParticipateInEquality(string field)
    {
        ConversationStartSetupResponse original = new(Setup() with
        {
            PreferredSourceKinds = [ProjectContextSourceKind.Memory, ProjectContextSourceKind.Conversation],
            ExcludedSourceKinds = [ProjectContextSourceKind.FileReference, ProjectContextSourceKind.ProjectFolder],
        }, Snapshot() with
        {
            Components = [Component(), Component() with { Name = "Folder" }],
            RecoveryActions = ["RefreshContext", "Retry"],
        });
        ConversationStartSetupResponse changed = field switch
        {
            "name" => original with { Snapshot = original.Snapshot with { Components = [Component() with { Name = "Other" }, original.Snapshot.Components[1]] } },
            "freshness" => original with { Snapshot = original.Snapshot with { Components = [Component() with { Freshness = EvidenceFreshnessState.Stale }, original.Snapshot.Components[1]] } },
            "reason" => original with { Snapshot = original.Snapshot with { Components = [Component() with { Reason = "changed" }, original.Snapshot.Components[1]] } },
            "component-order" => original with { Snapshot = original.Snapshot with { Components = original.Snapshot.Components.Reverse().ToArray() } },
            "recovery-order" => original with { Snapshot = original.Snapshot with { RecoveryActions = original.Snapshot.RecoveryActions.Reverse().ToArray() } },
            "preferred-order" => original with { Setup = original.Setup! with { PreferredSourceKinds = original.Setup!.PreferredSourceKinds.Reverse().ToArray() } },
            _ => original with { Setup = original.Setup! with { ExcludedSourceKinds = original.Setup!.ExcludedSourceKinds.Reverse().ToArray() } },
        };

        original.Equals(changed).ShouldBeFalse();
        changed.Equals(original).ShouldBeFalse();
    }

    private static ConversationStartSetup Setup()
        => new("project", ProjectLifecycle.Active, ["goal", "second"], ["instruction"],
            [ProjectContextSourceKind.Memory], [ProjectContextSourceKind.FileReference],
            LinkedSourcePolicy.AuthorizedReferences, ObservedAt, ProjectContextFreshness.Fresh);

    private static ConversationStartAdmissionSnapshot Snapshot()
        => new(ConversationStartResponseState.Unavailable, ObservedAt, 1, [Component()], ["RefreshContext"]);

    private static ConversationStartComponent Component()
        => new("Project", true, EvidenceFreshnessState.Current, "current");
}
