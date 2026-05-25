// <copyright file="ProjectCommandValidatorTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Aggregates.Project;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;

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
}
