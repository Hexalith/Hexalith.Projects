// <copyright file="ConversationStartSetupTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Models;

using System;
using System.Collections.Generic;
using System.Text.Json;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Story 3.5 Tier-0 Contracts tests for the <see cref="ConversationStartSetup"/> wire DTO.
/// Validates serialization round-trip, the FS-2 NoPayloadLeakage invariant (no forbidden content
/// substrings), the FS-8 / SM-3 wire-shape invariant (no <c>tenantId</c>), the no-audit-metadata
/// invariant, the <see cref="LinkedSourcePolicy.None"/> default-of-default, additive
/// schema-evolution tolerance, and stable wire property order.
/// </summary>
public sealed class ConversationStartSetupTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ConversationStartSetup_RoundTripsSerialization()
    {
        ConversationStartSetup original = BuildFullyPopulated();

        string json = JsonSerializer.Serialize(original, WebOptions);
        ConversationStartSetup roundTripped = JsonSerializer.Deserialize<ConversationStartSetup>(json, WebOptions)!;

        // Records compare list members by reference, so assert element-wise across every field.
        roundTripped.ProjectId.ShouldBe(original.ProjectId);
        roundTripped.Lifecycle.ShouldBe(original.Lifecycle);
        roundTripped.Goals.ShouldBe(original.Goals);
        roundTripped.UserInstructions.ShouldBe(original.UserInstructions);
        roundTripped.PreferredSourceKinds.ShouldBe(original.PreferredSourceKinds);
        roundTripped.ExcludedSourceKinds.ShouldBe(original.ExcludedSourceKinds);
        roundTripped.LinkedSourcePolicy.ShouldBe(original.LinkedSourcePolicy);
        roundTripped.ObservedAt.ShouldBe(original.ObservedAt);
        roundTripped.Freshness.ShouldBe(original.Freshness);
    }

    [Fact]
    public void ConversationStartSetup_SerializesMetadataOnly()
    {
        ConversationStartSetup populated = BuildFullyPopulated();
        ConversationStartSetup empty = ConversationStartSetup.Empty(
            projectId: "project_test",
            lifecycle: ProjectLifecycle.Active,
            observedAt: DateTimeOffset.UnixEpoch,
            freshness: ProjectContextFreshness.Fresh);

        AssertNoLeakage(JsonSerializer.Serialize(populated, WebOptions));
        AssertNoLeakage(JsonSerializer.Serialize(empty, WebOptions));
    }

    [Fact]
    public void ConversationStartSetup_DoesNotEmitTenantIdField()
    {
        // FS-8 / SM-3: tenant authority is server-derived and NEVER on the wire body. The DTO does
        // not declare a TenantId field at all (cleaner than [JsonIgnore]-on-required-field).
        string json = JsonSerializer.Serialize(BuildFullyPopulated(), WebOptions);

        json.ShouldNotContain("tenantId");
        json.ShouldNotContain("TenantId");
    }

    [Fact]
    public void ConversationStartSetup_DoesNotEmitAuditFields()
    {
        // FR-20 fast path elides internal audit metadata. The DTO does not declare any of these.
        string json = JsonSerializer.Serialize(BuildFullyPopulated(), WebOptions);

        json.ShouldNotContain("createdAt");
        json.ShouldNotContain("updatedAt");
        json.ShouldNotContain("sequence");
        json.ShouldNotContain("setupMetadata");
    }

    [Fact]
    public void ConversationStartSetup_LinkedSourcePolicyDefault_IsNone()
    {
        ConversationStartSetup empty = ConversationStartSetup.Empty(
            projectId: "project_test",
            lifecycle: ProjectLifecycle.Active,
            observedAt: DateTimeOffset.UnixEpoch,
            freshness: ProjectContextFreshness.Fresh);

        empty.LinkedSourcePolicy.ShouldBe(LinkedSourcePolicy.None);
    }

    [Fact]
    public void ConversationStartSetup_AdditiveDeserialization_TolerantToUnknownFields()
    {
        // NFR-6 / additive-contracts rule: unknown wire fields must not break deserialization.
        const string Json =
            "{" +
            "\"projectId\":\"project_test\"," +
            "\"lifecycle\":\"Active\"," +
            "\"goals\":[\"g\"]," +
            "\"userInstructions\":[]," +
            "\"preferredSourceKinds\":[]," +
            "\"excludedSourceKinds\":[]," +
            "\"linkedSourcePolicy\":\"none\"," +
            "\"observedAt\":\"2026-05-12T12:34:56Z\"," +
            "\"freshness\":\"Fresh\"," +
            "\"extraField\":\"ignored\"," +
            "\"futureExtension\":{\"nested\":true}" +
            "}";

        ConversationStartSetup? deserialized = JsonSerializer.Deserialize<ConversationStartSetup>(Json, WebOptions);

        deserialized.ShouldNotBeNull();
        deserialized!.ProjectId.ShouldBe("project_test");
        deserialized.Goals.ShouldBe(new[] { "g" });
        deserialized.LinkedSourcePolicy.ShouldBe(LinkedSourcePolicy.None);
        deserialized.Freshness.ShouldBe(ProjectContextFreshness.Fresh);
    }

    [Fact]
    public void ConversationStartSetup_PropertyOrder_StableOnWire()
    {
        string json = JsonSerializer.Serialize(BuildFullyPopulated(), WebOptions);
        string[] expectedOrder = new[]
        {
            "projectId",
            "lifecycle",
            "goals",
            "userInstructions",
            "preferredSourceKinds",
            "excludedSourceKinds",
            "linkedSourcePolicy",
            "observedAt",
            "freshness",
        };

        int lastIndex = -1;
        foreach (string property in expectedOrder)
        {
            int index = json.IndexOf("\"" + property + "\"", StringComparison.Ordinal);
            index.ShouldBeGreaterThan(lastIndex, $"property '{property}' must appear after preceding declared properties in the serialized JSON.");
            lastIndex = index;
        }
    }

    private static ConversationStartSetup BuildFullyPopulated()
        => new(
            ProjectId: "project_01HZY7Z6N7J4Q2X8Y9V0A1B2C3",
            Lifecycle: ProjectLifecycle.Active,
            Goals: new[] { "keep continuity current", "summarize key risks" },
            UserInstructions: new[] { "use safe project references" },
            PreferredSourceKinds: new[] { ProjectContextSourceKind.Conversation, ProjectContextSourceKind.Memory },
            ExcludedSourceKinds: new[] { ProjectContextSourceKind.FileReference },
            LinkedSourcePolicy: LinkedSourcePolicy.AuthorizedReferences,
            ObservedAt: DateTimeOffset.Parse("2026-05-12T12:34:56Z", System.Globalization.CultureInfo.InvariantCulture),
            Freshness: ProjectContextFreshness.Fresh);

    private static void AssertNoLeakage(string serialized)
    {
        // Local Tier-0 mirror of NoPayloadLeakageAssertions.AssertNoLeakageInText — Contracts.Tests
        // does NOT reference Hexalith.Projects.Testing (which depends on Hexalith.Projects domain core)
        // so this lane re-uses PayloadClassification.ForbiddenContent directly to keep the boundary tight.
        List<string> violations = new();
        string haystack = serialized.ToLowerInvariant();
        foreach (string forbidden in PayloadClassification.ForbiddenContent)
        {
            if (haystack.Contains(forbidden.ToLowerInvariant(), StringComparison.Ordinal))
            {
                violations.Add($"forbidden content category '{forbidden}'");
            }
        }

        if (serialized.Contains("-----BEGIN", StringComparison.OrdinalIgnoreCase))
        {
            violations.Add("PEM key block");
        }

        violations.ShouldBeEmpty($"DTO serialization leaked forbidden content: {string.Join("; ", violations)}");
    }
}
