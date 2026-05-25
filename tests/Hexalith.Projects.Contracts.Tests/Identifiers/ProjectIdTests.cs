// <copyright file="ProjectIdTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Identifiers;

using System;
using System.Text.Json;

using Hexalith.Projects.Contracts.Identifiers;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 tests for <see cref="ProjectId"/> eager validation, record semantics, and JSON round-trip.
/// Pure: no Dapr, Aspire, network, browser, or containers.
/// </summary>
public sealed class ProjectIdTests
{
    [Fact]
    public void ConstructorAcceptsValidUlidShapedValue()
    {
        const string value = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
        var id = new ProjectId(value);
        id.Value.ShouldBe(value);
        id.ToString().ShouldBe(value);
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionOnNull()
        => Should.Throw<ArgumentNullException>(() => new ProjectId(null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("   ")]
    public void ConstructorThrowsArgumentExceptionOnEmptyOrWhitespace(string value)
        => Should.Throw<ArgumentException>(() => new ProjectId(value));

    [Fact]
    public void RecordEqualityIsValueBased()
    {
        var a = new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        var b = new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        var c = new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1CD");

        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
        a.ShouldNotBe(c);
    }

    [Fact]
    public void SerializesAsOpaqueStringValue()
    {
        var id = new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        string json = JsonSerializer.Serialize(id);
        json.ShouldBe("\"01HZ9K8YQ3W6V2N4R7T5P0X1AB\"");
    }

    [Fact]
    public void RoundTripsThroughJson()
    {
        var id = new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        string json = JsonSerializer.Serialize(id);
        ProjectId? deserialized = JsonSerializer.Deserialize<ProjectId>(json);
        deserialized.ShouldBe(id);
    }

    [Fact]
    public void DeserializeRejectsEmptyStringValue()
        => Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ProjectId>("\"\""));

    [Fact]
    public void DeserializeRejectsNonStringToken()
        => Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ProjectId>("123"));

    [Fact]
    public void DeserializeRejectsObjectToken()
        => Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ProjectId>("{}"));
}
