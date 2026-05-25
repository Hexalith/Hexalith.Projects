// <copyright file="ProjectCommandValidatorTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Aggregates.Project;

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
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
}
