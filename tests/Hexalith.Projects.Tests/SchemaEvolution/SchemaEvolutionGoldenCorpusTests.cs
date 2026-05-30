// <copyright file="SchemaEvolutionGoldenCorpusTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.SchemaEvolution;

using System;
using System.IO;
using System.Text.Json;

using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// FS-5 schema-evolution golden-corpus tests (AC 4): the frozen serialized samples of
/// Projects v1 events deserialize from the checked-in bytes via the production
/// <see cref="System.Text.Json"/> converters and round-trip — proving additive,
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
    public void ProjectSetupUpdated_DeserializesFromFrozenBytesAndRoundTrips()
    {
        string frozen = ReadGolden("ProjectSetupUpdated.v1.json");

        ProjectSetupUpdated deserialized = JsonSerializer.Deserialize<ProjectSetupUpdated>(frozen, Options).ShouldNotBeNull();

        deserialized.TenantId.ShouldBe("acme");
        deserialized.ProjectId.ShouldBe("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        deserialized.Setup.Goals.ShouldBe(["keep continuity current"]);
        deserialized.Setup.PreferredSourceKinds.ShouldBe([ProjectContextSourceKind.Conversation, ProjectContextSourceKind.Memory]);
        deserialized.Setup.ExcludedSourceKinds.ShouldBe([ProjectContextSourceKind.FileReference]);
        deserialized.Setup.ConversationStartDefaults!.LinkedSourcePolicy.ShouldBe(LinkedSourcePolicy.AuthorizedReferences);
        deserialized.IdempotencyFingerprint.ShouldBe("sha256:setup");
        deserialized.OccurredAt.ShouldBe(DateTimeOffset.UnixEpoch);

        string reserialized = JsonSerializer.Serialize(deserialized, Options);
        ProjectSetupUpdated roundTrip = JsonSerializer.Deserialize<ProjectSetupUpdated>(reserialized, Options).ShouldNotBeNull();
        roundTrip.TenantId.ShouldBe(deserialized.TenantId);
        roundTrip.ProjectId.ShouldBe(deserialized.ProjectId);
        roundTrip.Setup.Goals.ShouldBe(deserialized.Setup.Goals);
        roundTrip.Setup.UserInstructions.ShouldBe(deserialized.Setup.UserInstructions);
        roundTrip.Setup.PreferredSourceKinds.ShouldBe(deserialized.Setup.PreferredSourceKinds);
        roundTrip.Setup.ExcludedSourceKinds.ShouldBe(deserialized.Setup.ExcludedSourceKinds);
        roundTrip.Setup.ConversationStartDefaults.ShouldBe(deserialized.Setup.ConversationStartDefaults);
        roundTrip.IdempotencyFingerprint.ShouldBe(deserialized.IdempotencyFingerprint);
    }

    [Fact]
    public void ProjectArchived_DeserializesFromFrozenBytesAndRoundTrips()
    {
        string frozen = ReadGolden("ProjectArchived.v1.json");

        ProjectArchived deserialized = JsonSerializer.Deserialize<ProjectArchived>(frozen, Options).ShouldNotBeNull();

        deserialized.TenantId.ShouldBe("acme");
        deserialized.ProjectId.ShouldBe("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        deserialized.Lifecycle.ShouldBe(ProjectLifecycle.Archived);
        deserialized.IdempotencyFingerprint.ShouldBe("sha256:archive");
        deserialized.OccurredAt.ShouldBe(DateTimeOffset.UnixEpoch);

        string reserialized = JsonSerializer.Serialize(deserialized, Options);
        JsonSerializer.Deserialize<ProjectArchived>(reserialized, Options).ShouldBe(deserialized);
    }

    [Fact]
    public void ProjectSetupUpdateRejected_DeserializesFromFrozenBytesAndRoundTrips()
    {
        string frozen = ReadGolden("ProjectSetupUpdateRejected.v1.json");

        ProjectSetupUpdateRejected deserialized = JsonSerializer.Deserialize<ProjectSetupUpdateRejected>(frozen, Options).ShouldNotBeNull();

        deserialized.ProjectId.Value.ShouldBe("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        deserialized.TenantId.ShouldBe("acme");
        deserialized.Reason.ShouldBe(ReferenceState.InvalidReference);
        deserialized.RejectedField.ShouldBe("setup.goals");
        deserialized.CorrelationId.ShouldBe("corr-setup");

        string reserialized = JsonSerializer.Serialize(deserialized, Options);
        JsonSerializer.Deserialize<ProjectSetupUpdateRejected>(reserialized, Options).ShouldBe(deserialized);
    }

    [Fact]
    public void ProjectArchiveRejected_DeserializesFromFrozenBytesAndRoundTrips()
    {
        string frozen = ReadGolden("ProjectArchiveRejected.v1.json");

        ProjectArchiveRejected deserialized = JsonSerializer.Deserialize<ProjectArchiveRejected>(frozen, Options).ShouldNotBeNull();

        deserialized.ProjectId.Value.ShouldBe("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        deserialized.TenantId.ShouldBe("acme");
        deserialized.Reason.ShouldBe(ReferenceState.Archived);
        deserialized.RejectedField.ShouldBe("lifecycle");
        deserialized.CorrelationId.ShouldBe("corr-archive");

        string reserialized = JsonSerializer.Serialize(deserialized, Options);
        JsonSerializer.Deserialize<ProjectArchiveRejected>(reserialized, Options).ShouldBe(deserialized);
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

    // === Story 4.4 (AC 4) additive/serialization-tolerant coverage for the confirm-resolution
    // command and event. Frozen inline samples (no golden file needed) prove backward-compatible
    // deserialization and round-trip equality for the net-new types. ===

    [Fact]
    public void ProjectResolutionConfirmed_DeserializesFromFrozenBytesAndRoundTrips()
    {
        const string frozen = """
            {"tenantId":"acme","projectId":"project-target-001","conversationId":"conversation-001","sourceProjectId":"project-source-001","actorPrincipalId":"actor-001","correlationId":"corr-001","taskId":"task-001","idempotencyKey":"idem-confirm","idempotencyFingerprint":"sha256:confirm","occurredAt":"1970-01-01T00:00:00+00:00"}
            """;

        ProjectResolutionConfirmed deserialized = JsonSerializer.Deserialize<ProjectResolutionConfirmed>(frozen, Options).ShouldNotBeNull();
        deserialized.TenantId.ShouldBe("acme");
        deserialized.ProjectId.ShouldBe("project-target-001");
        deserialized.ConversationId.ShouldBe("conversation-001");
        deserialized.SourceProjectId.ShouldBe("project-source-001");
        deserialized.IdempotencyFingerprint.ShouldBe("sha256:confirm");
        deserialized.OccurredAt.ShouldBe(DateTimeOffset.UnixEpoch);

        string reserialized = JsonSerializer.Serialize(deserialized, Options);
        JsonSerializer.Deserialize<ProjectResolutionConfirmed>(reserialized, Options).ShouldBe(deserialized);
    }

    [Fact]
    public void ProjectResolutionConfirmed_OmittedOptionalSourceProjectId_DeserializesAndRoundTrips()
    {
        const string frozen = """
            {"tenantId":"acme","projectId":"project-target-001","conversationId":"conversation-001","sourceProjectId":null,"actorPrincipalId":"actor-001","correlationId":"corr-001","taskId":"task-001","idempotencyKey":"idem-confirm","idempotencyFingerprint":"sha256:confirm","occurredAt":"1970-01-01T00:00:00+00:00"}
            """;

        ProjectResolutionConfirmed deserialized = JsonSerializer.Deserialize<ProjectResolutionConfirmed>(frozen, Options).ShouldNotBeNull();
        deserialized.SourceProjectId.ShouldBeNull();

        string reserialized = JsonSerializer.Serialize(deserialized, Options);
        JsonSerializer.Deserialize<ProjectResolutionConfirmed>(reserialized, Options).ShouldBe(deserialized);
    }

    [Fact]
    public void ProjectResolutionConfirmed_ToleratesAdditiveUnknownField()
    {
        const string frozen = """
            {"tenantId":"acme","projectId":"project-target-001","conversationId":"conversation-001","sourceProjectId":"project-source-001","actorPrincipalId":"actor-001","correlationId":"corr-001","taskId":"task-001","idempotencyKey":"idem-confirm","idempotencyFingerprint":"sha256:confirm","occurredAt":"1970-01-01T00:00:00+00:00","futureField":"ignored"}
            """;

        ProjectResolutionConfirmed deserialized = JsonSerializer.Deserialize<ProjectResolutionConfirmed>(frozen, Options).ShouldNotBeNull();
        deserialized.TenantId.ShouldBe("acme");
    }

    [Fact]
    public void ProjectResolutionConfirmationRejected_DeserializesFromFrozenBytesAndRoundTrips()
    {
        const string frozen = """
            {"projectId":"project-target-001","tenantId":"acme","reason":"Conflict","rejectedField":"sourceProjectId","correlationId":"corr-001"}
            """;

        ProjectResolutionConfirmationRejected deserialized = JsonSerializer.Deserialize<ProjectResolutionConfirmationRejected>(frozen, Options).ShouldNotBeNull();
        deserialized.ProjectId.Value.ShouldBe("project-target-001");
        deserialized.TenantId.ShouldBe("acme");
        deserialized.Reason.ShouldBe(ReferenceState.Conflict);
        deserialized.RejectedField.ShouldBe("sourceProjectId");

        string reserialized = JsonSerializer.Serialize(deserialized, Options);
        JsonSerializer.Deserialize<ProjectResolutionConfirmationRejected>(reserialized, Options).ShouldBe(deserialized);
    }

    [Fact]
    public void ConfirmProjectResolutionCommand_DeserializesFromFrozenBytesAndRoundTrips()
    {
        const string frozen = """
            {"tenantId":"acme","projectId":"project-target-001","conversationId":"conversation-001","sourceProjectId":"project-source-001","actorPrincipalId":"actor-001","correlationId":"corr-001","taskId":"task-001","idempotencyKey":"idem-confirm"}
            """;

        ConfirmProjectResolution deserialized = JsonSerializer.Deserialize<ConfirmProjectResolution>(frozen, Options).ShouldNotBeNull();
        deserialized.TenantId.ShouldBe("acme");
        deserialized.ProjectId.ShouldBe(new ProjectId("project-target-001"));
        deserialized.ConversationId.ShouldBe("conversation-001");
        deserialized.SourceProjectId.ShouldBe(new ProjectId("project-source-001"));
        deserialized.IdempotencyKey.ShouldBe("idem-confirm");

        string reserialized = JsonSerializer.Serialize(deserialized, Options);
        JsonSerializer.Deserialize<ConfirmProjectResolution>(reserialized, Options).ShouldBe(deserialized);
    }

    [Fact]
    public void ConfirmProjectResolutionCommand_OmittedOptionalSourceProjectId_DeserializesAndRoundTrips()
    {
        const string frozen = """
            {"tenantId":"acme","projectId":"project-target-001","conversationId":"conversation-001","sourceProjectId":null,"actorPrincipalId":"actor-001","correlationId":"corr-001","taskId":"task-001","idempotencyKey":"idem-confirm"}
            """;

        ConfirmProjectResolution deserialized = JsonSerializer.Deserialize<ConfirmProjectResolution>(frozen, Options).ShouldNotBeNull();
        deserialized.SourceProjectId.ShouldBeNull();

        string reserialized = JsonSerializer.Serialize(deserialized, Options);
        JsonSerializer.Deserialize<ConfirmProjectResolution>(reserialized, Options).ShouldBe(deserialized);
    }

    private static string ReadGolden(string fileName)
    {
        string path = Path.Combine(GoldenDirectory, fileName);
        File.Exists(path).ShouldBeTrue(path);

        // Normalize line endings before any comparison (golden files may be checked in CRLF or LF).
        return File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
