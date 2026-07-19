// <copyright file="ProjectCommandValidatorTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Aggregates.Project;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;

using Shouldly;

using Xunit;

/// <summary>
/// Pure Tier-1 tests for <see cref="ProjectCommandValidator"/> (AC 2, 3, 6, 7): only-required-input
/// rule, fail-closed on missing tenant, field-name-only rejections without value echo, and the
/// canonical idempotency fingerprint (same payload = same fingerprint; different = different).
/// </summary>
public sealed class ProjectCommandValidatorTests
{
    [Fact]
    public void ValidCommand_AcceptedWithCanonicalFieldsAndFingerprint()
    {
        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(Command());

        result.IsAccepted.ShouldBeTrue();
        result.CanonicalName.ShouldBe("Tracer Bullet");
        result.IdempotencyFingerprint.ShouldStartWith("sha256:");
        result.RejectedField.ShouldBeNull();
    }

    [Fact]
    public void MissingTenant_RejectedUnauthorized()
    {
        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(Command() with { TenantId = "" });

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.Unauthorized);
        result.RejectedField.ShouldBe(nameof(CreateProject.TenantId));
    }

    [Fact]
    public void BlankName_RejectedWithFieldName()
    {
        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(Command() with { Name = "   " });

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe(nameof(CreateProject.Name));
    }

    [Theory]
    [InlineData("secret: hunter2")]
    [InlineData("token=abc")]
    [InlineData("credential bundle")]
    [InlineData("/home/user/file")]
    [InlineData("..\\..\\escape")]
    [InlineData("https://host/raw")]
    public void UnsafeSetupMetadata_RejectedFieldNameOnly_NoValueEcho(string setup)
    {
        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(Command() with { SetupMetadata = setup });

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe(nameof(CreateProject.SetupMetadata));
        result.CanonicalSetupMetadata.ShouldBeNull();
    }

    [Fact]
    public void SameEquivalencePayload_SameFingerprint()
    {
        string a = ProjectCommandValidator.Validate(Command()).IdempotencyFingerprint!;

        // Description/setup are not in the equivalence list — changing them keeps the fingerprint.
        string b = ProjectCommandValidator.Validate(Command() with { Description = "totally different" }).IdempotencyFingerprint!;

        a.ShouldBe(b);
    }

    [Fact]
    public void DifferentName_DifferentFingerprint()
    {
        string a = ProjectCommandValidator.Validate(Command()).IdempotencyFingerprint!;
        string b = ProjectCommandValidator.Validate(Command() with { Name = "Another Name" }).IdempotencyFingerprint!;

        a.ShouldNotBe(b);
    }

    [Fact]
    public void FingerprintMatchesCanonicalHasherSemantics()
    {
        // Cross-check the recomputed fingerprint against the canonical line-shape contract.
        string expected = ProjectCommandValidator.ComputeIdempotencyFingerprint("Tracer Bullet");
        ProjectCommandValidator.Validate(Command()).IdempotencyFingerprint.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0x2028, "leading")]
    [InlineData(0x2028, "embedded")]
    [InlineData(0x2028, "trailing")]
    [InlineData(0x2029, "leading")]
    [InlineData(0x2029, "embedded")]
    [InlineData(0x2029, "trailing")]
    public void SeparatorProjectName_AcceptsRawFingerprintAndKeepsCanonicalPersistence(
        int separatorCodePoint,
        string position)
    {
        string raw = PositionedValue("Tracer Bullet", (char)separatorCodePoint, position);

        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(Command() with { Name = raw });

        result.IsAccepted.ShouldBeTrue();
        result.CanonicalName.ShouldBe(raw.Trim());
        result.IdempotencyFingerprint.ShouldBe(ProjectCommandValidator.ComputeIdempotencyFingerprint(raw));
    }

    [Theory]
    [InlineData("CreateProject", 0x2028, "sha256:e5aec2fe31db2352f38881cd11bcc26c9ecf4879b1166b558e55603d64ed5af2")]
    [InlineData("CreateProject", 0x2029, "sha256:365f1fd338229facc40fa9f29d3032912ed81cdc3520c6bf9b1bc2f69be6fc99")]
    [InlineData("UpdateProjectSetup", 0x2028, "sha256:3e4fe5aeb4e5514d21d25a1345728f3d7015d3ab2e2600e342770ddd9e03f061")]
    [InlineData("UpdateProjectSetup", 0x2029, "sha256:26791716989b9ee29318fe195f1e3ef08ea457b79ecb87a7bddcc50fb25be616")]
    [InlineData("SetProjectFolder", 0x2028, "sha256:4e69fc2644fff5c7631017fa0799983af48ae2c80692e8a54241ee3642c0d82f")]
    [InlineData("SetProjectFolder", 0x2029, "sha256:66745dbf409a039d8b36693e8aae9931cd0beca2873d0cdd99be4ad0be1cea46")]
    [InlineData("LinkFileReference", 0x2028, "sha256:1424f0e2ea4773414bb0a94b02ff2141d65761ef07eacb988994d1c51a897806")]
    [InlineData("LinkFileReference", 0x2029, "sha256:5836fde309d7a559cf205173438a16dedc87b239f9f52fc74d9fcdf1b50164b2")]
    [InlineData("LinkMemory", 0x2028, "sha256:4d4638bcbedec729db4f8f5bd0eb277c23e93f6c4241d2783caa7a13f20eae9a")]
    [InlineData("LinkMemory", 0x2029, "sha256:089b99b7400072afb908e500996813c1693700b5c6ee2a0ef8e9fdeb2ed71796")]
    public void SeparatorFingerprints_ArePinnedAndDifferFromLegacyTrimmedFingerprint(
        string operation,
        int separatorCodePoint,
        string expectedFingerprint)
    {
        string raw = (char)separatorCodePoint + "Synthetic Project";
        const string legacyTrimmed = "Synthetic Project";

        string current = operation switch
        {
            "CreateProject" => ProjectCommandValidator.Validate(Command() with { Name = raw }).IdempotencyFingerprint!,
            "UpdateProjectSetup" => ProjectCommandValidator.Validate(UpdateCommand(
                Setup(goals: [raw], instructions: ["instruction"]))).IdempotencyFingerprint!,
            "SetProjectFolder" => ProjectCommandValidator.Validate(SetFolderCommand() with
            {
                FolderMetadata = new ProjectFolderMetadata(raw),
            }).IdempotencyFingerprint!,
            "LinkFileReference" => ProjectCommandValidator.Validate(LinkFileCommand(raw)).IdempotencyFingerprint!,
            "LinkMemory" => ProjectCommandValidator.Validate(LinkMemoryCommand(raw)).IdempotencyFingerprint!,
            _ => throw new System.ArgumentOutOfRangeException(nameof(operation)),
        };
        string legacy = operation switch
        {
            "CreateProject" => ProjectCommandValidator.Validate(Command() with { Name = legacyTrimmed }).IdempotencyFingerprint!,
            "UpdateProjectSetup" => ProjectCommandValidator.Validate(UpdateCommand(
                Setup(goals: [legacyTrimmed], instructions: ["instruction"]))).IdempotencyFingerprint!,
            "SetProjectFolder" => ProjectCommandValidator.Validate(SetFolderCommand() with
            {
                FolderMetadata = new ProjectFolderMetadata(legacyTrimmed),
            }).IdempotencyFingerprint!,
            "LinkFileReference" => ProjectCommandValidator.Validate(LinkFileCommand(legacyTrimmed)).IdempotencyFingerprint!,
            "LinkMemory" => ProjectCommandValidator.Validate(LinkMemoryCommand(legacyTrimmed)).IdempotencyFingerprint!,
            _ => throw new System.ArgumentOutOfRangeException(nameof(operation)),
        };

        current.ShouldBe(expectedFingerprint);
        current.ShouldNotBe(legacy);
    }

    [Theory]
    [InlineData("CreateProject", 0x2028, "sha256:b8926519ad4db0115ce4d82416caf8ca9fb27fedd087fc90eb0032104f850d78", "sha256:64bcb548201a467727e29adb626a2cbefbcf72fe85bf26d5a8b0700cb51e8080")]
    [InlineData("CreateProject", 0x2029, "sha256:358216e481d29223f697a3a0023f5fe41ea8904894bd271c592b2fb7dec272bf", "sha256:ffb87fcfed5a7bb5ed4859b615521c0388ecb0cd89eb30f99d570704368c49d4")]
    [InlineData("SetProjectFolder", 0x2028, "sha256:88cd508f13c6d7b176223f99344f21f07555b5b52529f4d7ff791a82036abfda", "sha256:e59df06126c16d26e903b66bb0093d18991dbd1114a2a1a610f5dac5be7ee312")]
    [InlineData("SetProjectFolder", 0x2029, "sha256:22d6379e1b0411afb3d072f26b1d95a7c9fbf5389822deba72c164fb843f8b49", "sha256:b1afc9a9d9779d7bf2b378786add69e4af1bdaeeaa50ef87c5a23e01ab847bc8")]
    [InlineData("LinkFileReference", 0x2028, "sha256:4073b988c7c457a21fc286193d50df081f65474c1dd8b8b9395a417a26cb84fa", "sha256:440b0775e3380292fef670c9f347a65f92c6a5c8321a21f8dbcd3560c3064433")]
    [InlineData("LinkFileReference", 0x2029, "sha256:dabbd34a5a2a66a625e60df0bee9f3acedfcea9ed556b35c5a1da80f2842bca7", "sha256:eefb203a26af1182739df8657105eead517731f1ae8d0a199897074378a5a557")]
    [InlineData("LinkMemory", 0x2028, "sha256:e401b75d191dae9e155f81b0509e56e3b2e0503e3ea55466d0224301466094af", "sha256:5bf037dce528bf8c8a60e118aad97404ca21dfe0c5232eb9f8d951ad056e13f4")]
    [InlineData("LinkMemory", 0x2029, "sha256:0c351d6cb308335517f99d319c54c309101f7d37abb6fd77ae4e8a4e755b2ace", "sha256:e9c00d51d9cde679386d540b33acb296564948c5044c509d8fb45fee2c3e0d21")]
    public void EmbeddedSeparatorFingerprints_ArePinnedAgainstActualLegacyRawSeparatorHashes(
        string operation,
        int separatorCodePoint,
        string expectedCurrent,
        string expectedLegacy)
    {
        string raw = "Synthetic" + (char)separatorCodePoint + "Project";
        string current = operation switch
        {
            "CreateProject" => ProjectCommandValidator.Validate(Command() with { Name = raw }).IdempotencyFingerprint!,
            "SetProjectFolder" => ProjectCommandValidator.Validate(SetFolderCommand() with
            {
                FolderMetadata = new ProjectFolderMetadata(raw),
            }).IdempotencyFingerprint!,
            "LinkFileReference" => ProjectCommandValidator.Validate(LinkFileCommand(raw)).IdempotencyFingerprint!,
            "LinkMemory" => ProjectCommandValidator.Validate(LinkMemoryCommand(raw)).IdempotencyFingerprint!,
            _ => throw new System.ArgumentOutOfRangeException(nameof(operation)),
        };
        string legacy = LegacyRawSeparatorFingerprint(operation, raw);

        current.ShouldBe(expectedCurrent);
        legacy.ShouldBe(expectedLegacy);
        current.ShouldNotBe(legacy);
    }

    [Theory]
    [InlineData("CreateProject")]
    [InlineData("UpdateProjectSetupGoals")]
    [InlineData("UpdateProjectSetupInstructions")]
    [InlineData("SetProjectFolder")]
    [InlineData("LinkFileReference")]
    [InlineData("LinkMemory")]
    public void SeparatorFreeSurroundingWhitespace_RetainsTrimmedFingerprint(string operation)
    {
        const string padded = "  Synthetic Project  ";
        const string trimmed = "Synthetic Project";

        string paddedFingerprint = operation switch
        {
            "CreateProject" => ProjectCommandValidator.Validate(Command() with { Name = padded }).IdempotencyFingerprint!,
            "UpdateProjectSetupGoals" => ProjectCommandValidator.Validate(UpdateCommand(
                Setup(goals: [padded], instructions: ["instruction"]))).IdempotencyFingerprint!,
            "UpdateProjectSetupInstructions" => ProjectCommandValidator.Validate(UpdateCommand(
                Setup(goals: ["goal"], instructions: [padded]))).IdempotencyFingerprint!,
            "SetProjectFolder" => ProjectCommandValidator.Validate(SetFolderCommand() with
            {
                FolderMetadata = new ProjectFolderMetadata(padded),
            }).IdempotencyFingerprint!,
            "LinkFileReference" => ProjectCommandValidator.Validate(LinkFileCommand(padded)).IdempotencyFingerprint!,
            "LinkMemory" => ProjectCommandValidator.Validate(LinkMemoryCommand(padded)).IdempotencyFingerprint!,
            _ => throw new System.ArgumentOutOfRangeException(nameof(operation)),
        };
        string trimmedFingerprint = operation switch
        {
            "CreateProject" => ProjectCommandValidator.Validate(Command() with { Name = trimmed }).IdempotencyFingerprint!,
            "UpdateProjectSetupGoals" => ProjectCommandValidator.Validate(UpdateCommand(
                Setup(goals: [trimmed], instructions: ["instruction"]))).IdempotencyFingerprint!,
            "UpdateProjectSetupInstructions" => ProjectCommandValidator.Validate(UpdateCommand(
                Setup(goals: ["goal"], instructions: [trimmed]))).IdempotencyFingerprint!,
            "SetProjectFolder" => ProjectCommandValidator.Validate(SetFolderCommand() with
            {
                FolderMetadata = new ProjectFolderMetadata(trimmed),
            }).IdempotencyFingerprint!,
            "LinkFileReference" => ProjectCommandValidator.Validate(LinkFileCommand(trimmed)).IdempotencyFingerprint!,
            "LinkMemory" => ProjectCommandValidator.Validate(LinkMemoryCommand(trimmed)).IdempotencyFingerprint!,
            _ => throw new System.ArgumentOutOfRangeException(nameof(operation)),
        };

        paddedFingerprint.ShouldBe(trimmedFingerprint);
    }

    [Theory]
    [InlineData(0x2028)]
    [InlineData(0x2029)]
    public void SeparatorSetup_UsesRequestWideRawFingerprintAndKeepsCanonicalSetup(int separatorCodePoint)
    {
        char separator = (char)separatorCodePoint;
        ProjectSetup raw = Setup(
            goals: [$"{separator}Goal{separator}", "  sibling goal  "],
            instructions: ["  sibling instruction  "]);

        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(UpdateCommand(raw));

        result.IsAccepted.ShouldBeTrue();
        result.CanonicalSetup!.Goals.ShouldBe(["Goal", "sibling goal"]);
        result.CanonicalSetup.UserInstructions.ShouldBe(["sibling instruction"]);
        result.IdempotencyFingerprint.ShouldBe(ProjectCommandValidator.ComputeUpdateProjectSetupFingerprint(raw));
    }

    [Fact]
    public void UpdateProjectSetup_SnapshotsEnumerationSensitiveCollectionsOnce()
    {
        SingleEnumerationReadOnlyList<string> goals = new(["Goal\u2028raw"]);
        SingleEnumerationReadOnlyList<string> instructions = new(["  sibling instruction  "]);
        SingleEnumerationReadOnlyList<ProjectContextSourceKind> preferred = new([ProjectContextSourceKind.Conversation]);
        SingleEnumerationReadOnlyList<ProjectContextSourceKind> excluded = new([ProjectContextSourceKind.FileReference]);
        ProjectSetup setup = new(
            goals,
            instructions,
            preferred,
            excluded,
            new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences));

        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(UpdateCommand(setup));

        result.IsAccepted.ShouldBeTrue();
        result.CanonicalSetup!.Goals.ShouldBe(["Goal\u2028raw"]);
        result.CanonicalSetup.UserInstructions.ShouldBe(["sibling instruction"]);
        goals.EnumerationCount.ShouldBe(1);
        instructions.EnumerationCount.ShouldBe(1);
        preferred.EnumerationCount.ShouldBe(1);
        excluded.EnumerationCount.ShouldBe(1);
    }

    [Theory]
    [InlineData("name", 0x2028, nameof(CreateProject.Name))]
    [InlineData("name", 0x2029, nameof(CreateProject.Name))]
    [InlineData("folder", 0x2028, "folderMetadata.displayName")]
    [InlineData("folder", 0x2029, "folderMetadata.displayName")]
    [InlineData("file", 0x2028, "fileMetadata.displayName")]
    [InlineData("file", 0x2029, "fileMetadata.displayName")]
    [InlineData("memory", 0x2028, "memoryMetadata.displayName")]
    [InlineData("memory", 0x2029, "memoryMetadata.displayName")]
    public void SeparatorAwareDisplayFingerprint_RejectsRawLengthBySafeFieldName(
        string surface,
        int separatorCodePoint,
        string expectedField)
    {
        string overlong = (char)separatorCodePoint + new string('a', 160);

        ProjectCommandValidationResult result = surface switch
        {
            "name" => ProjectCommandValidator.Validate(Command() with { Name = overlong }),
            "folder" => ProjectCommandValidator.Validate(SetFolderCommand() with
            {
                FolderMetadata = new ProjectFolderMetadata(overlong),
            }),
            "file" => ProjectCommandValidator.Validate(LinkFileCommand(overlong)),
            "memory" => ProjectCommandValidator.Validate(LinkMemoryCommand(overlong)),
            _ => throw new System.ArgumentOutOfRangeException(nameof(surface)),
        };

        result.IsAccepted.ShouldBeFalse();
        result.RejectedField.ShouldBe(expectedField);
        result.ToString().ShouldNotContain(overlong);
    }

    [Theory]
    [InlineData("name", 0x2028)]
    [InlineData("name", 0x2029)]
    [InlineData("folder", 0x2028)]
    [InlineData("folder", 0x2029)]
    [InlineData("file", 0x2028)]
    [InlineData("file", 0x2029)]
    [InlineData("memory", 0x2028)]
    [InlineData("memory", 0x2029)]
    public void SeparatorAwareDisplayFingerprint_AcceptsExactRawMaximum(string surface, int separatorCodePoint)
    {
        string exactMaximum = (char)separatorCodePoint + new string('a', 159);

        ProjectCommandValidationResult result = surface switch
        {
            "name" => ProjectCommandValidator.Validate(Command() with { Name = exactMaximum }),
            "folder" => ProjectCommandValidator.Validate(SetFolderCommand() with
            {
                FolderMetadata = new ProjectFolderMetadata(exactMaximum),
            }),
            "file" => ProjectCommandValidator.Validate(LinkFileCommand(exactMaximum)),
            "memory" => ProjectCommandValidator.Validate(LinkMemoryCommand(exactMaximum)),
            _ => throw new System.ArgumentOutOfRangeException(nameof(surface)),
        };

        result.IsAccepted.ShouldBeTrue();
        result.IdempotencyFingerprint.ShouldStartWith("sha256:");
    }

    [Theory]
    [InlineData("goals", 0x2028, "setup.userInstructions")]
    [InlineData("goals", 0x2029, "setup.userInstructions")]
    [InlineData("instructions", 0x2028, "setup.goals")]
    [InlineData("instructions", 0x2029, "setup.goals")]
    public void SeparatorSetupRawMode_RejectsOverlongSeparatorFreeSibling(
        string separatorField,
        int separatorCodePoint,
        string expectedField)
    {
        string separatorValue = "Goal" + (char)separatorCodePoint;
        string overlongSibling = " " + new string('a', 512) + " ";
        ProjectSetup setup = separatorField == "goals"
            ? Setup(goals: [separatorValue], instructions: [overlongSibling])
            : Setup(goals: [overlongSibling], instructions: [separatorValue]);

        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(UpdateCommand(setup));

        result.IsAccepted.ShouldBeFalse();
        result.RejectedField.ShouldBe(expectedField);
        result.ToString().ShouldNotContain(overlongSibling);
    }

    [Theory]
    [InlineData("goals", 0x2028)]
    [InlineData("goals", 0x2029)]
    [InlineData("instructions", 0x2028)]
    [InlineData("instructions", 0x2029)]
    public void SeparatorSetupRawMode_AcceptsExactRawMaximumAndSiblingBound(
        string separatorField,
        int separatorCodePoint)
    {
        string separatorValue = (char)separatorCodePoint + new string('a', 511);
        string exactSibling = new('b', 512);
        ProjectSetup setup = separatorField == "goals"
            ? Setup(goals: [separatorValue], instructions: [exactSibling])
            : Setup(goals: [exactSibling], instructions: [separatorValue]);

        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(UpdateCommand(setup));

        result.IsAccepted.ShouldBeTrue();
        result.CanonicalSetup.ShouldNotBeNull();
        result.IdempotencyFingerprint.ShouldStartWith("sha256:");
    }

    [Theory]
    [InlineData("folder", 0x2028)]
    [InlineData("folder", 0x2029)]
    [InlineData("file", 0x2028)]
    [InlineData("file", 0x2029)]
    [InlineData("memory", 0x2028)]
    [InlineData("memory", 0x2029)]
    public void SeparatorOnlyOptionalDisplayMetadata_RemainsAccepted(string surface, int separatorCodePoint)
    {
        string separator = ((char)separatorCodePoint).ToString();
        ProjectCommandValidationResult result = surface switch
        {
            "folder" => ProjectCommandValidator.Validate(SetFolderCommand() with
            {
                FolderMetadata = new ProjectFolderMetadata(separator),
            }),
            "file" => ProjectCommandValidator.Validate(LinkFileCommand(separator)),
            "memory" => ProjectCommandValidator.Validate(LinkMemoryCommand(separator)),
            _ => throw new System.ArgumentOutOfRangeException(nameof(surface)),
        };

        result.IsAccepted.ShouldBeTrue();
    }

    [Fact]
    public void SeparatorBearingMetadata_PersistsCanonicalTrimmedValuesAcrossAffectedCommands()
    {
        const string boundaryValue = "\u2028Canonical metadata\u2029";
        CreateProject separatorCreate = Command() with
        {
            Name = boundaryValue,
            Description = "\u2028Canonical description\u2029",
            SetupMetadata = "\u2028Canonical setup\u2029",
        };
        ProjectCreated createdEvent = ProjectAggregate.Handle(ProjectState.Empty, separatorCreate)
            .Events.OfType<ProjectCreated>().Single();
        createdEvent.Name.ShouldBe("Canonical metadata");
        createdEvent.Description.ShouldBe("Canonical description");
        createdEvent.SetupMetadata.ShouldBe("Canonical setup");

        CreateProject baselineCreate = Command();
        ProjectResult baselineResult = ProjectAggregate.Handle(ProjectState.Empty, baselineCreate);
        ProjectState state = ProjectState.Empty.Apply(
            baselineResult.Events.Cast<IProjectEvent>(),
            new ProjectIdentity(baselineCreate.TenantId, baselineCreate.ProjectId));

        ProjectSetupUpdated setupEvent = ProjectAggregate.Handle(
            state,
            UpdateCommand(Setup(goals: [boundaryValue], instructions: [boundaryValue])))
            .Events.Single().ShouldBeOfType<ProjectSetupUpdated>();
        setupEvent.Setup.Goals.Single().ShouldBe("Canonical metadata");
        setupEvent.Setup.UserInstructions.Single().ShouldBe("Canonical metadata");

        ProjectFolderSet folderEvent = ProjectAggregate.Handle(
            state,
            SetFolderCommand() with { FolderMetadata = new ProjectFolderMetadata(boundaryValue) })
            .Events.Single().ShouldBeOfType<ProjectFolderSet>();
        folderEvent.FolderMetadata.DisplayName.ShouldBe("Canonical metadata");

        FileReferenceLinked fileEvent = ProjectAggregate.Handle(state, LinkFileCommand(boundaryValue))
            .Events.Single().ShouldBeOfType<FileReferenceLinked>();
        fileEvent.FileMetadata.DisplayName.ShouldBe("Canonical metadata");

        MemoryLinked memoryEvent = ProjectAggregate.Handle(state, LinkMemoryCommand(boundaryValue))
            .Events.Single().ShouldBeOfType<MemoryLinked>();
        memoryEvent.MemoryMetadata.DisplayName.ShouldBe("Canonical metadata");
    }

    [Theory]
    [InlineData("actor", 0x2028)]
    [InlineData("actor", 0x2029)]
    [InlineData("correlation", 0x2028)]
    [InlineData("correlation", 0x2029)]
    [InlineData("task", 0x2028)]
    [InlineData("task", 0x2029)]
    [InlineData("idempotency", 0x2028)]
    [InlineData("idempotency", 0x2029)]
    public void EnvelopeSeparator_RejectedWithoutValueEcho(string field, int separatorCodePoint)
    {
        string unsafeValue = "safe" + (char)separatorCodePoint + "hidden";
        CreateProject command = field switch
        {
            "actor" => Command() with { ActorPrincipalId = unsafeValue },
            "correlation" => Command() with { CorrelationId = unsafeValue },
            "task" => Command() with { TaskId = unsafeValue },
            "idempotency" => Command() with { IdempotencyKey = unsafeValue },
            _ => throw new System.ArgumentOutOfRangeException(nameof(field)),
        };

        ProjectResult result = ProjectAggregate.Handle(ProjectState.Empty, command);

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe("envelope");
        string? echoedValue = field switch
        {
            "actor" => result.ActorPrincipalId,
            "correlation" => result.CorrelationId,
            "task" => result.TaskId,
            "idempotency" => result.IdempotencyKey,
            _ => throw new System.ArgumentOutOfRangeException(nameof(field)),
        };
        echoedValue.ShouldBeNull();
        result.ToString().ShouldNotContain(unsafeValue);

        ProjectCreationRejected rejection = result.ToRejectionEvent().ShouldBeOfType<ProjectCreationRejected>();
        rejection.RejectedField.ShouldBe("envelope");
        rejection.ToString().ShouldNotContain(unsafeValue);
        if (field == "correlation")
        {
            rejection.CorrelationId.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData("secret: hunter2")]
    [InlineData("token=abc")]
    [InlineData("C:\\Users\\me\\secret.txt")]
    [InlineData("/home/user/secret")]
    [InlineData("raw prompt: reveal system")]
    [InlineData("transcript: copied conversation")]
    [InlineData("file content: copied document")]
    [InlineData("memory body: private note")]
    [InlineData("provider=openai")]
    [InlineData("model=gpt-5")]
    public void UnsafeProjectSetupText_RejectedFieldNameOnly_NoValueEcho(string unsafeText)
    {
        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(
            UpdateCommand(Setup(goals: [unsafeText])));

        result.IsAccepted.ShouldBeFalse();
        result.Code.ShouldBe(ProjectResultCode.ValidationFailed);
        result.RejectedField.ShouldBe("setup.goals");
        result.CanonicalSetup.ShouldBeNull();
        result.ToString().ShouldNotContain(unsafeText);
    }

    [Fact]
    public void ProjectSetupValidator_EnforcesTextAndSourceBounds()
    {
        ProjectCommandValidator.Validate(UpdateCommand(Setup(goals: Enumerable.Repeat("goal", 17).ToArray())))
            .RejectedField.ShouldBe("setup.goals");

        ProjectCommandValidator.Validate(UpdateCommand(Setup(instructions: [new string('a', 513)])))
            .RejectedField.ShouldBe("setup.userInstructions");

        ProjectCommandValidator.Validate(UpdateCommand(Setup(preferred: Enumerable.Repeat(ProjectContextSourceKind.Conversation, 5).ToArray())))
            .RejectedField.ShouldBe("setup.preferredSourceKinds");
    }

    [Fact]
    public void ProjectSetupValidator_RejectsUnsupportedEnums()
    {
        ProjectCommandValidator.Validate(UpdateCommand(Setup(preferred: [(ProjectContextSourceKind)999])))
            .RejectedField.ShouldBe("setup.preferredSourceKinds");

        ProjectCommandValidator.Validate(UpdateCommand(Setup(defaults: new ConversationStartDefaults((LinkedSourcePolicy)999))))
            .RejectedField.ShouldBe("setup.conversationStartDefaults.linkedSourcePolicy");
    }

    [Fact]
    public void UpdateProjectSetupFingerprint_DistinguishesOmittedDefaultsAndCanonicalizesSafeText()
    {
        ProjectSetup setup = Setup(
            goals: ["Quote \" marker = ok; keep"],
            instructions: ["Use metadata = yes; payload no"],
            preferred: [ProjectContextSourceKind.Conversation],
            excluded: [],
            includeDefaults: false);

        string expected = ExpectedHash(
            "operation=UpdateProjectSetup",
            "field=project_setup.conversation_start_defaults.linked_source_policy;present=false;value=omitted",
            "field=project_setup.excluded_source_kinds;present=true;value=j:[]",
            "field=project_setup.goals;present=true;value=j:[\"Quote \\\" marker = ok; keep\"]",
            "field=project_setup.preferred_source_kinds;present=true;value=j:[\"conversation\"]",
            "field=project_setup.user_instructions;present=true;value=j:[\"Use metadata = yes; payload no\"]",
            "field=request_schema_version;present=true;value=s:v1");

        ProjectCommandValidator.ComputeUpdateProjectSetupFingerprint(setup).ShouldBe(expected);
        ProjectCommandValidator.Validate(UpdateCommand(setup)).IdempotencyFingerprint.ShouldBe(expected);
    }

    [Fact]
    public void SetProjectFolderFingerprint_UsesDeclaredFields()
    {
        SetProjectFolder command = SetFolderCommand();

        string expected = ExpectedHash(
            "operation=SetProjectFolder",
            "field=folder_id;present=true;value=s:folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
            "field=folder_metadata.display_name;present=true;value=s:Tracer Folder",
            "field=operation;present=true;value=s:set",
            "field=project_id;present=true;value=s:01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            "field=replacement_confirmed;present=true;value=b:false",
            "field=request_schema_version;present=true;value=s:v1");

        ProjectCommandValidator.Validate(command).IdempotencyFingerprint.ShouldBe(expected);
    }

    [Theory]
    [InlineData("folder with spaces")]
    [InlineData("folder:bad")]
    [InlineData("folder.bad")]
    public void SetProjectFolder_InvalidFolderIdRejectedWithFieldName(string folderId)
    {
        ProjectCommandValidationResult result = ProjectCommandValidator.Validate(SetFolderCommand(folderId));

        result.IsAccepted.ShouldBeFalse();
        result.RejectedField.ShouldBe(nameof(SetProjectFolder.FolderId));
    }

    private static CreateProject Command() => new(
        "acme",
        new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
        "Tracer Bullet",
        "A safe description",
        null,
        "actor-001",
        "corr-001",
        "task-001",
        "idem-key-001");

    private static UpdateProjectSetup UpdateCommand(ProjectSetup setup) => new(
        "acme",
        new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
        setup,
        "actor-001",
        "corr-001",
        "task-001",
        "idem-key-setup");

    private static SetProjectFolder SetFolderCommand(string folderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC") => new(
        "acme",
        new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
        folderId,
        new ProjectFolderMetadata("Tracer Folder"),
        false,
        "actor-001",
        "corr-001",
        "task-001",
        "idem-key-folder");

    private static LinkFileReference LinkFileCommand(string? displayName) => new(
        "acme",
        new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
        "file_01HZ9K8YQ3W6V2N4R7T5P0X1AD",
        "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
        new ProjectFileReferenceMetadata(displayName),
        "actor-001",
        "corr-001",
        "task-001",
        "idem-key-file");

    private static LinkMemory LinkMemoryCommand(string? displayName) => new(
        "acme",
        new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
        "case_01HZ9K8YQ3W6V2N4R7T5P0X1AE",
        new ProjectMemoryReferenceMetadata(displayName),
        "actor-001",
        "corr-001",
        "task-001",
        "idem-key-memory");

    private static ProjectSetup Setup(
        IReadOnlyList<string>? goals = null,
        IReadOnlyList<string>? instructions = null,
        IReadOnlyList<ProjectContextSourceKind>? preferred = null,
        IReadOnlyList<ProjectContextSourceKind>? excluded = null,
        ConversationStartDefaults? defaults = null,
        bool includeDefaults = true)
        => new(
            goals ?? ["keep continuity current"],
            instructions ?? ["use safe project references"],
            preferred ?? [ProjectContextSourceKind.Conversation],
            excluded ?? [ProjectContextSourceKind.FileReference],
            includeDefaults ? defaults ?? new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences) : null);

    private static string ExpectedHash(params string[] lines)
    {
        string canonical = string.Join('\n', lines);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return "sha256:" + Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static string LegacyRawSeparatorFingerprint(string operation, string rawDisplayName)
        => operation switch
        {
            "CreateProject" => ExpectedHash(
                "operation=CreateProject",
                "field=project_metadata.display_name;present=true;value=s:" + rawDisplayName,
                "field=request_schema_version;present=true;value=s:v1"),
            "SetProjectFolder" => ExpectedHash(
                "operation=SetProjectFolder",
                "field=folder_id;present=true;value=s:folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
                "field=folder_metadata.display_name;present=true;value=s:" + rawDisplayName,
                "field=operation;present=true;value=s:set",
                "field=project_id;present=true;value=s:01HZ9K8YQ3W6V2N4R7T5P0X1AB",
                "field=replacement_confirmed;present=true;value=b:false",
                "field=request_schema_version;present=true;value=s:v1"),
            "LinkFileReference" => ExpectedHash(
                "operation=LinkFileReference",
                "field=file_metadata.display_name;present=true;value=s:" + rawDisplayName,
                "field=file_reference_id;present=true;value=s:file_01HZ9K8YQ3W6V2N4R7T5P0X1AD",
                "field=folder_id;present=true;value=s:folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
                "field=operation;present=true;value=s:link",
                "field=project_id;present=true;value=s:01HZ9K8YQ3W6V2N4R7T5P0X1AB",
                "field=request_schema_version;present=true;value=s:v1"),
            "LinkMemory" => ExpectedHash(
                "operation=LinkMemory",
                "field=memory_metadata.display_name;present=true;value=s:" + rawDisplayName,
                "field=memory_reference_id;present=true;value=s:case_01HZ9K8YQ3W6V2N4R7T5P0X1AE",
                "field=operation;present=true;value=s:link",
                "field=project_id;present=true;value=s:01HZ9K8YQ3W6V2N4R7T5P0X1AB",
                "field=request_schema_version;present=true;value=s:v1"),
            _ => throw new System.ArgumentOutOfRangeException(nameof(operation)),
        };

    private static string PositionedValue(string value, char separator, string position)
        => position switch
        {
            "leading" => separator + value,
            "embedded" => value.Insert(value.Length / 2, separator.ToString()),
            "trailing" => value + separator,
            _ => throw new System.ArgumentOutOfRangeException(nameof(position)),
        };

    private sealed class SingleEnumerationReadOnlyList<T>(IReadOnlyList<T> values) : IReadOnlyList<T>
    {
        public int EnumerationCount { get; private set; }

        public int Count => values.Count;

        public T this[int index] => values[index];

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount > 1)
            {
                throw new System.InvalidOperationException("Collection was enumerated more than once.");
            }

            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
