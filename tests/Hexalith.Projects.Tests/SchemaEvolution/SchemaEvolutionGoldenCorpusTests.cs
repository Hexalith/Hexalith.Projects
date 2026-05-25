// <copyright file="SchemaEvolutionGoldenCorpusTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.SchemaEvolution;

using System;
using System.IO;
using System.Text.Json;

using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// FS-5 schema-evolution golden-corpus tests (AC 4): the frozen serialized samples of
/// <c>ProjectCreated</c> + <c>ProjectCreationRejected</c> deserialize from the checked-in bytes via the
/// production <see cref="System.Text.Json"/> converters and round-trip — proving additive,
/// backward-compatible deserialization (no <c>V2</c> event types). Line endings are normalized before
/// comparison. Pure Tier-1.
/// </summary>
public sealed class SchemaEvolutionGoldenCorpusTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private static readonly string GoldenDirectory =
        Path.Combine(AppContext.BaseDirectory, "SchemaEvolution", "Golden");

    [Fact]
    public void ProjectCreated_DeserializesFromFrozenBytesAndRoundTrips()
    {
        string frozen = ReadGolden("ProjectCreated.v1.json");

        ProjectCreated deserialized = JsonSerializer.Deserialize<ProjectCreated>(frozen, Options).ShouldNotBeNull();

        deserialized.TenantId.ShouldBe("acme");
        deserialized.ProjectId.ShouldBe("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        deserialized.Name.ShouldBe("Tracer Bullet");
        deserialized.Lifecycle.ShouldBe(ProjectLifecycle.Active);
        deserialized.IdempotencyFingerprint.ShouldBe("sha256:deadbeef");
        deserialized.OccurredAt.ShouldBe(DateTimeOffset.UnixEpoch);

        // Round-trip: re-serializing then deserializing yields an equal event.
        string reserialized = JsonSerializer.Serialize(deserialized, Options);
        JsonSerializer.Deserialize<ProjectCreated>(reserialized, Options).ShouldBe(deserialized);
    }

    [Fact]
    public void ProjectCreationRejected_DeserializesFromFrozenBytesAndRoundTrips()
    {
        string frozen = ReadGolden("ProjectCreationRejected.v1.json");

        ProjectCreationRejected deserialized = JsonSerializer.Deserialize<ProjectCreationRejected>(frozen, Options).ShouldNotBeNull();

        deserialized.TenantId.ShouldBe("acme");
        deserialized.Reason.ShouldBe(ReferenceState.Unauthorized);
        deserialized.RejectedField.ShouldBe("SetupMetadata");
        deserialized.ProjectId.ShouldNotBeNull();
        deserialized.ProjectId!.Value.ShouldBe("01HZ9K8YQ3W6V2N4R7T5P0X1AB");

        string reserialized = JsonSerializer.Serialize(deserialized, Options);
        JsonSerializer.Deserialize<ProjectCreationRejected>(reserialized, Options).ShouldBe(deserialized);
    }

    [Fact]
    public void ProjectCreated_ToleratesAdditiveUnknownField()
    {
        // Additive/tolerant: an unknown field added by a future writer must not break deserialization.
        string frozen = ReadGolden("ProjectCreated.v1.json");
        string withFutureField = frozen.TrimEnd().TrimEnd('}') + ",\n  \"futureField\": \"ignored\"\n}";

        ProjectCreated deserialized = JsonSerializer.Deserialize<ProjectCreated>(withFutureField, Options).ShouldNotBeNull();
        deserialized.Name.ShouldBe("Tracer Bullet");
    }

    private static string ReadGolden(string fileName)
    {
        string path = Path.Combine(GoldenDirectory, fileName);
        File.Exists(path).ShouldBeTrue(path);

        // Normalize line endings before any comparison (golden files may be checked in CRLF or LF).
        return File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
