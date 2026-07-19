// <copyright file="ProposeNewProjectEndpointTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Generated = Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.ProjectReferenceIndex;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Proposals;
using Hexalith.Projects.Testing.Leakage;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

using Xunit;

/// <summary>Story 4.5 Tier-2 endpoint tests for proposal preview and composite confirm.</summary>
public sealed class ProposeNewProjectEndpointTests
{
    private const string TenantA = "tenant-a";
    private const string PrincipalA = "principal-a";
    private const string ConversationIdValue = "conversation-001";
    private const string ProjectIdValue = "project-001";
    private const string ExistingProjectId = "project-existing";
    private const string FolderIdValue = "folder-001";
    private const string FileIdValue = "file-001";
    private const string WorkspaceIdValue = "workspace-001";
    private const string CorrelationIdValue = "corr-001";
    private const string TaskIdValue = "task-001";
    private const string IdempotencyKeyValue = "proposal-001";

    [Fact]
    public async Task Preview_NoMatch_ReturnsMetadataOnlyProposal()
    {
        using ServiceProvider provider = await BuildProviderAsync().ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Header("X-Correlation-Id").ShouldBe(CorrelationIdValue);
        response.Header("X-Hexalith-Freshness").ShouldBe("eventually_consistent");

        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("resolutionResult").GetString().ShouldBe("NoMatch");
        document.RootElement.GetProperty("suggestedName").GetString().ShouldBe("Suggested Project");
        document.RootElement.GetProperty("conversationId").GetString().ShouldBe(ConversationIdValue);
        document.RootElement.GetProperty("fileReferenceIds")[0].GetString().ShouldBe(FileIdValue);
        document.RootElement.TryGetProperty("tenantId", out _).ShouldBeFalse();
        document.RootElement.TryGetProperty("filePath", out _).ShouldBeFalse();
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(response.Body));
    }

    [Fact]
    public async Task Preview_ExistingCandidate_ReturnsValidationProblemWithoutLeakingCandidate()
    {
        using ServiceProvider provider = await BuildProviderAsync(
            listRows: [ProjectRow(ExistingProjectId, "Existing Project")],
            conversation: new ConversationResolutionMetadata(ConversationIdValue, ExistingProjectId, "Existing Project", ReferenceState.Included)).ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain("resolutionResult");
        response.Body.ShouldNotContain(ExistingProjectId);
        response.Body.ShouldNotContain("Existing Project");
    }

    [Fact]
    public async Task Preview_MultipleCandidates_ReturnsValidationProblemWithoutLeakingCandidates()
    {
        using ServiceProvider provider = await BuildProviderAsync(
            referenceRows:
            [
                ReferenceRow("project-alpha", FolderIdValue, "folder"),
                ReferenceRow("project-beta", FileIdValue, "file"),
            ]).ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain("resolutionResult");
        response.Body.ShouldNotContain("project-alpha");
        response.Body.ShouldNotContain("project-beta");
    }

    [Fact]
    public async Task Preview_IdempotencyKeyPresent_ReturnsValidationProblem()
    {
        using ServiceProvider provider = await BuildProviderAsync().ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(
            provider,
            PreviewBody(),
            headers: new Dictionary<string, string>(StringComparer.Ordinal) { ["Idempotency-Key"] = IdempotencyKeyValue }).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
        document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("idempotency_key");
    }

    [Fact]
    public async Task Preview_UnauthorizedCaller_ReturnsSafeDenial()
    {
        using ServiceProvider provider = await BuildProviderAsync(tenantId: null, principalId: null).ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Body.ShouldNotContain(ConversationIdValue);
    }

    [Theory]
    [InlineData(ReferenceState.Unavailable, HttpStatusCode.ServiceUnavailable)]
    [InlineData(ReferenceState.Stale, HttpStatusCode.ServiceUnavailable)]
    [InlineData(ReferenceState.Unauthorized, HttpStatusCode.NotFound)]
    public async Task Preview_ConversationPreflightFailure_FailsClosed(ReferenceState state, HttpStatusCode expectedStatus)
    {
        using ServiceProvider provider = await BuildProviderAsync(
            conversation: ConversationResolutionMetadata.FailClosed(ConversationIdValue, state)).ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(expectedStatus);
        response.Body.ShouldNotContain("Conversation Project");
    }

    [Fact]
    public async Task Preview_ReferenceIndexUnavailable_Returns503()
    {
        using ServiceProvider provider = await BuildProviderAsync(referenceIndexThrows: true).ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(provider, PreviewBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Confirm_FullFlow_SubmitsCreateAssignmentFolderAndFilesWithDerivedKeys()
    {
        CapturingProjectCommandSubmitter submitter = new();
        CapturingAssignmentDirectory assignment = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter, assignmentDirectory: assignment).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        submitter.Created.Single().IdempotencyKey.ShouldBe(IdempotencyKeyValue + ":create");
        assignment.Confirmed.Single().Metadata.IdempotencyKey.ShouldBe(IdempotencyKeyValue + ":conversation");
        submitter.Folders.Single().IdempotencyKey.ShouldBe(IdempotencyKeyValue + ":folder");
        submitter.Files.Single().IdempotencyKey.ShouldBe(IdempotencyKeyValue + ":file:" + FileIdValue);
        submitter.Files.Single().FileMetadata.DisplayName.ShouldBe("Design brief");
    }

    [Fact]
    public async Task Confirm_CreateReplayStillResumesAssignmentFolderAndFiles()
    {
        CapturingProjectCommandSubmitter submitter = new();
        submitter.CreateResults.Enqueue(ProjectCommandSubmissionResult.Accepted("projects-corr", true));
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        submitter.Created.Single().IdempotencyKey.ShouldBe(IdempotencyKeyValue + ":create");
        submitter.Folders.Count.ShouldBe(1);
        submitter.Files.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Confirm_AssignmentConflict_Returns409AfterCreateForRetryRecovery()
    {
        CapturingProjectCommandSubmitter submitter = new();
        CapturingAssignmentDirectory assignment = new(new ProjectConversationAssignmentResult(ProjectConversationAssignmentOutcome.Conflict, "assignment-corr"));
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter, assignmentDirectory: assignment).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        submitter.Created.Count.ShouldBe(1);
        submitter.Folders.ShouldBeEmpty();
        submitter.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_FolderDenied_ReturnsSafeDenialBeforeCreate()
    {
        CapturingProjectCommandSubmitter submitter = new();
        RecordingFolderDirectory folderDirectory = new(ProjectFolderValidationOutcome.Denied);
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter, folderDirectory: folderDirectory).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        folderDirectory.CallCount.ShouldBe(1);
        submitter.Created.ShouldBeEmpty();
        submitter.Folders.ShouldBeEmpty();
        submitter.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_FileDenied_ReturnsSafeDenialBeforeCreate()
    {
        CapturingProjectCommandSubmitter submitter = new();
        RecordingFileReferenceDirectory fileDirectory = new(ProjectFileReferenceValidationOutcome.Denied);
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter, fileReferenceDirectory: fileDirectory).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        fileDirectory.CallCount.ShouldBe(1);
        submitter.Created.ShouldBeEmpty();
        submitter.Folders.ShouldBeEmpty();
        submitter.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_FileCommandUnavailableThenSameRootRetrySucceeds()
    {
        CapturingProjectCommandSubmitter submitter = new();
        submitter.FileResults.Enqueue(new ProjectCommandSubmissionResult(ProjectCommandSubmissionOutcome.Unavailable, "projects-corr"));
        submitter.CreateResults.Enqueue(ProjectCommandSubmissionResult.Accepted("projects-corr", true));
        submitter.FolderResults.Enqueue(ProjectCommandSubmissionResult.Accepted("projects-corr", true));
        submitter.FileResults.Enqueue(ProjectCommandSubmissionResult.Accepted("projects-corr", true));
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);

        EndpointResponse first = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);
        EndpointResponse second = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        second.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        submitter.Created.Count.ShouldBe(2);
        submitter.Files.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Confirm_SameRootKeyDifferentBody_Returns409BeforeDuplicateCreate()
    {
        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);

        EndpointResponse first = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);
        EndpointResponse second = await SendConfirmAsync(provider, ConfirmBody(displayName: "Different Project")).ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        submitter.Created.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("displayName", 0x2028, "leading")]
    [InlineData("displayName", 0x2028, "embedded")]
    [InlineData("displayName", 0x2028, "trailing")]
    [InlineData("displayName", 0x2029, "leading")]
    [InlineData("displayName", 0x2029, "embedded")]
    [InlineData("displayName", 0x2029, "trailing")]
    [InlineData("description", 0x2028, "leading")]
    [InlineData("description", 0x2028, "embedded")]
    [InlineData("description", 0x2028, "trailing")]
    [InlineData("description", 0x2028, "only")]
    [InlineData("description", 0x2029, "leading")]
    [InlineData("description", 0x2029, "embedded")]
    [InlineData("description", 0x2029, "trailing")]
    [InlineData("description", 0x2029, "only")]
    [InlineData("setupMetadata", 0x2028, "leading")]
    [InlineData("setupMetadata", 0x2028, "embedded")]
    [InlineData("setupMetadata", 0x2028, "trailing")]
    [InlineData("setupMetadata", 0x2028, "only")]
    [InlineData("setupMetadata", 0x2029, "leading")]
    [InlineData("setupMetadata", 0x2029, "embedded")]
    [InlineData("setupMetadata", 0x2029, "trailing")]
    [InlineData("setupMetadata", 0x2029, "only")]
    public async Task Confirm_SeparatorMetadataFingerprintMatchesGeneratedHelper(
        string field,
        int separatorCodePoint,
        string position)
    {
        (string DisplayName, string? Description, string? SetupMetadata) metadata = SeparatorProposalMetadata(
            field,
            (char)separatorCodePoint,
            position);
        CapturingProposalConfirmationIdempotencyLedger ledger = new();
        using ServiceProvider provider = await BuildProviderAsync(idempotencyLedger: ledger).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(
            provider,
            ConfirmBody(metadata.DisplayName, description: metadata.Description, setupMetadata: metadata.SetupMetadata)).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        ledger.Entries.Single().Fingerprint.ShouldBe(GeneratedConfirmRequest(metadata).ComputeIdempotencyHash());
    }

    [Theory]
    [InlineData(0x2028, "sha256:0e7b502ece508da2f0f19ae6391ffbed77c796f0e123e1113de78b95a7d224f4")]
    [InlineData(0x2029, "sha256:4544a4172efc147f5571e70527f8ec9dd55605b9b27ba76e3da64ebe8a60daa2")]
    public async Task Confirm_SeparatorFingerprintIsPinnedAgainstGeneratedHelper(
        int separatorCodePoint,
        string expectedFingerprint)
    {
        var metadata = (
            DisplayName: "Synthetic" + (char)separatorCodePoint + "Project",
            Description: (string?)"Safe project description",
            SetupMetadata: (string?)"Safe setup note");
        CapturingProposalConfirmationIdempotencyLedger ledger = new();
        using ServiceProvider provider = await BuildProviderAsync(idempotencyLedger: ledger).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(
            provider,
            ConfirmBody(metadata.DisplayName, description: metadata.Description, setupMetadata: metadata.SetupMetadata)).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        ledger.Entries.Single().Fingerprint.ShouldBe(expectedFingerprint);
        GeneratedConfirmRequest(metadata).ComputeIdempotencyHash().ShouldBe(expectedFingerprint);
    }

    [Theory]
    [InlineData("displayName", 0x2028)]
    [InlineData("displayName", 0x2029)]
    [InlineData("description", 0x2028)]
    [InlineData("description", 0x2029)]
    [InlineData("setupMetadata", 0x2028)]
    [InlineData("setupMetadata", 0x2029)]
    public async Task Confirm_RawModeRejectsOverlongMetadataBySafeFieldName(
        string overlongField,
        int separatorCodePoint)
    {
        char separator = (char)separatorCodePoint;
        string displayName = overlongField == "displayName"
            ? separator + new string('a', 160)
            : "Suggested Project";
        string description = overlongField == "description"
            ? separator + new string('b', 512)
            : "Safe description";
        string setupMetadata = overlongField == "setupMetadata"
            ? separator + new string('c', 512)
            : "Safe setup";
        string overlongValue = overlongField switch
        {
            "displayName" => displayName,
            "description" => description,
            "setupMetadata" => setupMetadata,
            _ => throw new ArgumentOutOfRangeException(nameof(overlongField)),
        };

        CapturingProposalConfirmationIdempotencyLedger ledger = new();
        using ServiceProvider provider = await BuildProviderAsync(idempotencyLedger: ledger).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(
            provider,
            ConfirmBody(displayName, description: description, setupMetadata: setupMetadata)).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain("projectMetadata");
        response.Body.ShouldNotContain(overlongValue);
        ledger.Entries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("displayName", 0x2028)]
    [InlineData("displayName", 0x2029)]
    [InlineData("description", 0x2028)]
    [InlineData("description", 0x2029)]
    [InlineData("setupMetadata", 0x2028)]
    [InlineData("setupMetadata", 0x2029)]
    public async Task Confirm_RawModeRejectsOverlongSeparatorFreeSibling(
        string overlongSibling,
        int separatorCodePoint)
    {
        char separator = (char)separatorCodePoint;
        string displayName = overlongSibling == "displayName"
            ? " " + new string('a', 159) + " "
            : "Suggested Project";
        string description = overlongSibling == "description"
            ? " " + new string('b', 512) + " "
            : separator + "Safe description";
        string setupMetadata = overlongSibling == "setupMetadata"
            ? " " + new string('c', 512) + " "
            : separator + "Safe setup";
        string overlongValue = overlongSibling switch
        {
            "displayName" => displayName,
            "description" => description,
            "setupMetadata" => setupMetadata,
            _ => throw new ArgumentOutOfRangeException(nameof(overlongSibling)),
        };

        CapturingProposalConfirmationIdempotencyLedger ledger = new();
        using ServiceProvider provider = await BuildProviderAsync(idempotencyLedger: ledger).ConfigureAwait(true);
        EndpointResponse response = await SendConfirmAsync(
            provider,
            ConfirmBody(displayName, description: description, setupMetadata: setupMetadata)).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain("projectMetadata");
        response.Body.ShouldNotContain(overlongValue);
        ledger.Entries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0x2028)]
    [InlineData(0x2029)]
    public async Task Confirm_RawModeAcceptsAllExactMetadataMaxima(int separatorCodePoint)
    {
        var metadata = (
            DisplayName: (char)separatorCodePoint + new string('a', 159),
            Description: (string?)new string('b', 512),
            SetupMetadata: (string?)new string('c', 512));
        CapturingProposalConfirmationIdempotencyLedger ledger = new();
        using ServiceProvider provider = await BuildProviderAsync(idempotencyLedger: ledger).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(
            provider,
            ConfirmBody(metadata.DisplayName, description: metadata.Description, setupMetadata: metadata.SetupMetadata)).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        ledger.Entries.Single().Fingerprint.ShouldBe(GeneratedConfirmRequest(metadata).ComputeIdempotencyHash());
    }

    [Fact]
    public async Task Confirm_SeparatorFreeMetadataRetainsTrimAndNullFingerprint()
    {
        CapturingProposalConfirmationIdempotencyLedger trimmedLedger = new();
        using (ServiceProvider provider = await BuildProviderAsync(idempotencyLedger: trimmedLedger).ConfigureAwait(true))
        {
            EndpointResponse response = await SendConfirmAsync(
                provider,
                ConfirmBody("  Suggested Project  ", description: "  Safe description  ", setupMetadata: "  Safe setup  ")).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            trimmedLedger.Entries.Single().Fingerprint.ShouldBe(GeneratedConfirmRequest(
                ("Suggested Project", "Safe description", "Safe setup")).ComputeIdempotencyHash());
        }

        CapturingProposalConfirmationIdempotencyLedger nullLedger = new();
        using ServiceProvider nullProvider = await BuildProviderAsync(idempotencyLedger: nullLedger).ConfigureAwait(true);
        EndpointResponse nullResponse = await SendConfirmAsync(
            nullProvider,
            ConfirmBody("Suggested Project", description: "   ", setupMetadata: null)).ConfigureAwait(true);

        nullResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        nullLedger.Entries.Single().Fingerprint.ShouldBe(GeneratedConfirmRequest(
            ("Suggested Project", null, null)).ComputeIdempotencyHash());
    }

    [Theory]
    [InlineData("displayName")]
    [InlineData("description")]
    [InlineData("setupMetadata")]
    public async Task Confirm_SeparatorFingerprintsDoNotCollideAndUnsafeVariantsAreRejected(string field)
    {
        string[] acceptedValues = ["Synthetic Project", "Synthetic\u2028Project", "Synthetic\u2029Project"];
        List<string> endpointFingerprints = [];
        for (int index = 0; index < acceptedValues.Length; index++)
        {
            (string DisplayName, string? Description, string? SetupMetadata) metadata = ProposalMetadataVariant(
                field,
                acceptedValues[index]);
            CapturingProposalConfirmationIdempotencyLedger ledger = new();
            using ServiceProvider provider = await BuildProviderAsync(idempotencyLedger: ledger).ConfigureAwait(true);
            EndpointResponse response = await SendConfirmAsync(
                provider,
                ConfirmBody(metadata.DisplayName, description: metadata.Description, setupMetadata: metadata.SetupMetadata),
                idempotencyKey: IdempotencyKeyValue + "-" + index).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            endpointFingerprints.Add(ledger.Entries.Single().Fingerprint);
        }

        string[] rejectedValues = ["Synthetic\nProject", "Synthetic\\u2028Project", "Synthetic\\u2029Project"];
        foreach (string rejectedValue in rejectedValues)
        {
            (string DisplayName, string? Description, string? SetupMetadata) metadata = ProposalMetadataVariant(
                field,
                rejectedValue);
            CapturingProposalConfirmationIdempotencyLedger ledger = new();
            using ServiceProvider provider = await BuildProviderAsync(idempotencyLedger: ledger).ConfigureAwait(true);
            EndpointResponse response = await SendConfirmAsync(
                provider,
                ConfirmBody(metadata.DisplayName, description: metadata.Description, setupMetadata: metadata.SetupMetadata)).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            response.Body.ShouldContain("projectMetadata");
            response.Body.ShouldNotContain(rejectedValue);
            ledger.Entries.ShouldBeEmpty();
        }

        string[] generatedFingerprints = acceptedValues.Concat(rejectedValues)
            .Select(value => GeneratedConfirmRequest(ProposalMetadataVariant(field, value)).ComputeIdempotencyHash())
            .ToArray();
        generatedFingerprints.Distinct(StringComparer.Ordinal).Count().ShouldBe(generatedFingerprints.Length);
        endpointFingerprints.ShouldBe(generatedFingerprints.Take(acceptedValues.Length));
    }

    [Theory]
    [InlineData("\"projectId\": \"bad/slash\"", "identity")]
    [InlineData("\"fileReferenceIds\": [\"file-999\"]", "fileReferenceIds")]
    public async Task Confirm_MalformedBody_ReturnsValidationProblemBeforeWrites(string replacement, string rejectedField)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        ArgumentNullException.ThrowIfNull(rejectedField);

        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);
        string body = replacement.StartsWith("\"projectId\"", StringComparison.Ordinal)
            ? ConfirmBody().Replace("\"projectId\": \"project-001\"", replacement, StringComparison.Ordinal)
            : ConfirmBody().Replace("\"fileReferenceIds\": [\"file-001\"]", replacement, StringComparison.Ordinal);

        EndpointResponse response = await SendConfirmAsync(provider, body).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain(rejectedField);
        submitter.Created.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_MissingIdempotencyKey_ReturnsValidationProblemBeforeWrites()
    {
        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody(), includeIdempotencyKey: false).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain("idempotency_key");
        submitter.Created.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_UnauthorizedCaller_ReturnsSafeDenialBeforeWrites()
    {
        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter, tenantId: null, principalId: null).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Body.ShouldNotContain(ConversationIdValue);
        response.Body.ShouldNotContain(ProjectIdValue);
        submitter.Created.ShouldBeEmpty();
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(response.Body));
    }

    [Theory]
    [InlineData(ReferenceState.Unavailable, HttpStatusCode.ServiceUnavailable)]
    [InlineData(ReferenceState.Stale, HttpStatusCode.ServiceUnavailable)]
    [InlineData(ReferenceState.Unauthorized, HttpStatusCode.NotFound)]
    public async Task Confirm_ConversationPreflightFailure_FailsClosedBeforeWrites(ReferenceState state, HttpStatusCode expectedStatus)
    {
        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(
            submitter: submitter,
            conversation: ConversationResolutionMetadata.FailClosed(ConversationIdValue, state)).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(expectedStatus);
        submitter.Created.ShouldBeEmpty();
        submitter.Folders.ShouldBeEmpty();
        submitter.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_ConversationReadThrows_FailsClosedBeforeWrites()
    {
        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter, conversationThrows: true).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        submitter.Created.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_ResponsesDoNotLeakPayload()
    {
        using ServiceProvider denyProvider = await BuildProviderAsync(folderDirectory: new RecordingFolderDirectory(ProjectFolderValidationOutcome.Denied)).ConfigureAwait(true);
        EndpointResponse denied = await SendConfirmAsync(denyProvider, ConfirmBody()).ConfigureAwait(true);
        denied.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(denied.Body));

        using ServiceProvider acceptProvider = await BuildProviderAsync(submitter: new CapturingProjectCommandSubmitter()).ConfigureAwait(true);
        EndpointResponse accepted = await SendConfirmAsync(acceptProvider, ConfirmBody()).ConfigureAwait(true);
        accepted.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(accepted.Body));
    }

    [Fact]
    public async Task Confirm_MissingMetadataClass_ReturnsValidationProblemBeforeWrites()
    {
        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBody(metadataClass: null)).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain("projectMetadata");
        submitter.Created.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_NoFolderNoFiles_SubmitsOnlyCreateAndAssignment()
    {
        CapturingProjectCommandSubmitter submitter = new();
        CapturingAssignmentDirectory assignment = new(ProjectConversationAssignmentResult.Accepted("assignment-corr"));
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter, assignmentDirectory: assignment).ConfigureAwait(true);

        EndpointResponse response = await SendConfirmAsync(provider, ConfirmBodyMinimal()).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        submitter.Created.Count.ShouldBe(1);
        submitter.Created.Single().IdempotencyKey.ShouldBe(IdempotencyKeyValue + ":create");
        assignment.Confirmed.Count.ShouldBe(1);
        submitter.Folders.ShouldBeEmpty();
        submitter.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_DuplicateFileReferenceIds_ReturnsValidationProblemBeforeWrites()
    {
        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);

        string body = $$"""
            {
              "requestSchemaVersion": "v1",
              "operation": "confirmNewProjectProposal",
              "resolutionResult": "NoMatch",
              "confirmed": true,
              "projectId": "{{ProjectIdValue}}",
              "conversationId": "{{ConversationIdValue}}",
              "projectMetadata": { "displayName": "Suggested Project", "metadataClass": "tenant_sensitive" },
              "fileReferences": [
                { "fileReferenceId": "{{FileIdValue}}", "folderId": "{{FolderIdValue}}", "workspaceId": "{{WorkspaceIdValue}}", "filePath": "docs/a.md", "fileMetadata": { "displayName": "A" } },
                { "fileReferenceId": "{{FileIdValue}}", "folderId": "{{FolderIdValue}}", "workspaceId": "{{WorkspaceIdValue}}", "filePath": "docs/b.md", "fileMetadata": { "displayName": "B" } }
              ],
              "fileReferenceIds": ["{{FileIdValue}}", "{{FileIdValue}}"]
            }
            """;

        EndpointResponse response = await SendConfirmAsync(provider, body).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain("fileReferences");
        submitter.Created.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_TooManyFileReferences_ReturnsValidationProblemBeforeWrites()
    {
        CapturingProjectCommandSubmitter submitter = new();
        using ServiceProvider provider = await BuildProviderAsync(submitter: submitter).ConfigureAwait(true);

        string[] ids = Enumerable.Range(0, 33).Select(static i => $"file-{i:D3}").ToArray();
        string fileReferences = string.Join(
            ",",
            ids.Select(static id => $$"""{ "fileReferenceId": "{{id}}", "folderId": "{{FolderIdValue}}", "workspaceId": "{{WorkspaceIdValue}}", "filePath": "docs/{{id}}.md", "fileMetadata": { "displayName": "{{id}}" } }"""));
        string idArray = string.Join(",", ids.Select(static id => $"\"{id}\""));
        string body = $$"""
            {
              "requestSchemaVersion": "v1",
              "operation": "confirmNewProjectProposal",
              "resolutionResult": "NoMatch",
              "confirmed": true,
              "projectId": "{{ProjectIdValue}}",
              "conversationId": "{{ConversationIdValue}}",
              "projectMetadata": { "displayName": "Suggested Project", "metadataClass": "tenant_sensitive" },
              "fileReferences": [{{fileReferences}}],
              "fileReferenceIds": [{{idArray}}]
            }
            """;

        EndpointResponse response = await SendConfirmAsync(provider, body).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Body.ShouldContain("fileReferences");
        submitter.Created.ShouldBeEmpty();
    }

    [Fact]
    public async Task Preview_InvalidFreshness_ReturnsValidationProblem()
    {
        using ServiceProvider provider = await BuildProviderAsync().ConfigureAwait(true);

        EndpointResponse response = await SendPreviewAsync(
            provider,
            PreviewBody(),
            headers: new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Hexalith-Freshness"] = "strong" }).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
        document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("freshness");
    }

    private static async Task<ServiceProvider> BuildProviderAsync(
        IReadOnlyList<ProjectListItem>? listRows = null,
        IReadOnlyList<ProjectReferenceIndexCandidateRow>? referenceRows = null,
        bool referenceIndexThrows = false,
        ConversationResolutionMetadata? conversation = null,
        bool conversationThrows = false,
        CapturingProjectCommandSubmitter? submitter = null,
        CapturingAssignmentDirectory? assignmentDirectory = null,
        RecordingFolderDirectory? folderDirectory = null,
        RecordingFileReferenceDirectory? fileReferenceDirectory = null,
        IProjectProposalConfirmationIdempotencyLedger? idempotencyLedger = null,
        string? tenantId = TenantA,
        string? principalId = PrincipalA)
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddProjectsServer();
        services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        services.RemoveAll<IProjectTenantContextAccessor>();
        services.AddSingleton<IProjectTenantContextAccessor>(new FixedProjectTenantContext(tenantId, principalId));
        services.RemoveAll<IProjectConversationResolutionDirectory>();
        services.AddSingleton<IProjectConversationResolutionDirectory>(new StubConversationResolutionDirectory(
            conversation ?? new ConversationResolutionMetadata(ConversationIdValue, null, "Conversation Project", ReferenceState.Included),
            conversationThrows));
        services.RemoveAll<IProjectListReadModel>();
        services.AddSingleton<IProjectListReadModel>(new StubProjectListReadModel(listRows ?? []));
        services.RemoveAll<IProjectReferenceIndexReadModel>();
        services.AddSingleton<IProjectReferenceIndexReadModel>(new StubProjectReferenceIndexReadModel(referenceRows ?? [], referenceIndexThrows));
        services.RemoveAll<IProjectCommandSubmitter>();
        services.AddSingleton<IProjectCommandSubmitter>(submitter ?? new CapturingProjectCommandSubmitter());
        services.RemoveAll<IProjectConversationAssignmentDirectory>();
        services.AddSingleton<IProjectConversationAssignmentDirectory>(assignmentDirectory ?? new CapturingAssignmentDirectory(ProjectConversationAssignmentResult.Accepted("assignment-corr")));
        services.RemoveAll<IProjectFolderDirectory>();
        services.AddSingleton<IProjectFolderDirectory>(folderDirectory ?? new RecordingFolderDirectory(ProjectFolderValidationOutcome.Accepted));
        services.RemoveAll<IProjectFileReferenceDirectory>();
        services.AddSingleton<IProjectFileReferenceDirectory>(fileReferenceDirectory ?? new RecordingFileReferenceDirectory(ProjectFileReferenceValidationOutcome.Accepted));
        if (idempotencyLedger is not null)
        {
            services.RemoveAll<IProjectProposalConfirmationIdempotencyLedger>();
            services.AddSingleton(idempotencyLedger);
        }

        ServiceProvider provider = services.BuildServiceProvider();
        if (!string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(principalId))
        {
            ProjectTenantAccessProjection projection = new()
            {
                TenantId = tenantId,
                Enabled = true,
                Watermark = 1,
                ProjectionWatermark = $"{tenantId}:1",
                LastEventTimestamp = DateTimeOffset.UtcNow,
            };
            projection.Principals[principalId] = new ProjectTenantPrincipalEvidence(principalId, "TenantOwner");
            await provider.GetRequiredService<IProjectTenantAccessProjectionStore>()
                .SaveAsync(projection, TestContext.Current.CancellationToken)
                .ConfigureAwait(false);
        }

        return provider;
    }

    private static Task<EndpointResponse> SendPreviewAsync(
        ServiceProvider provider,
        string body,
        IReadOnlyDictionary<string, string>? headers = null)
        => InvokeAsync(
            provider,
            "ProposeNewProjectAsync",
            body,
            headers,
            args =>
            [
                args.HttpContext,
                provider.GetRequiredService<IProjectTenantContextAccessor>(),
                provider.GetRequiredService<ProjectAuthorizationGate>(),
                provider.GetRequiredService<IProjectConversationResolutionDirectory>(),
                provider.GetRequiredService<IProjectListReadModel>(),
                provider.GetRequiredService<IProjectReferenceIndexReadModel>(),
                provider.GetRequiredService<ProjectResolutionEngine>(),
                provider.GetRequiredService<TimeProvider>(),
                args.CancellationToken,
            ]);

    private static Task<EndpointResponse> SendConfirmAsync(
        ServiceProvider provider,
        string body,
        bool includeIdempotencyKey = true,
        string idempotencyKey = IdempotencyKeyValue)
    {
        Dictionary<string, string> headers = new(StringComparer.Ordinal);
        if (includeIdempotencyKey)
        {
            headers["Idempotency-Key"] = idempotencyKey;
        }

        return InvokeAsync(
            provider,
            "ConfirmNewProjectProposalAsync",
            body,
            headers,
            args =>
            [
                args.HttpContext,
                provider.GetRequiredService<IProjectCommandSubmitter>(),
                provider.GetRequiredService<IProjectTenantContextAccessor>(),
                provider.GetRequiredService<ProjectAuthorizationGate>(),
                provider.GetRequiredService<IProjectConversationResolutionDirectory>(),
                provider.GetRequiredService<IProjectListReadModel>(),
                provider.GetRequiredService<IProjectReferenceIndexReadModel>(),
                provider.GetRequiredService<IProjectConversationAssignmentDirectory>(),
                provider.GetRequiredService<IProjectFolderDirectory>(),
                provider.GetRequiredService<IProjectFileReferenceDirectory>(),
                provider.GetRequiredService<ProjectResolutionEngine>(),
                provider.GetRequiredService<IProjectProposalConfirmationIdempotencyLedger>(),
                provider.GetRequiredService<TimeProvider>(),
                args.CancellationToken,
            ]);
    }

    private static async Task<EndpointResponse> InvokeAsync(
        ServiceProvider provider,
        string methodName,
        string body,
        IReadOnlyDictionary<string, string>? headers,
        Func<InvocationArgs, object?[]> buildArguments)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DefaultHttpContext httpContext = new()
        {
            RequestServices = provider,
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        httpContext.Request.Headers["X-Correlation-Id"] = CorrelationIdValue;
        httpContext.Request.Headers["X-Hexalith-Task-Id"] = TaskIdValue;
        if (headers is not null)
        {
            foreach ((string name, string value) in headers)
            {
                httpContext.Request.Headers[name] = value;
            }
        }

        MethodInfo method = typeof(ProjectsDomainServiceEndpoints)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            .ShouldNotBeNull();
        object? task = method.Invoke(null, buildArguments(new InvocationArgs(httpContext, cancellationToken)));
        IResult result = await ((Task<IResult>)task!).ConfigureAwait(false);
        await result.ExecuteAsync(httpContext).ConfigureAwait(false);

        httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(httpContext.Response.Body);
        return new EndpointResponse(
            (HttpStatusCode)httpContext.Response.StatusCode,
            await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false),
            httpContext.Response.Headers.ToDictionary(static header => header.Key, static header => header.Value.ToString(), StringComparer.OrdinalIgnoreCase));
    }

    private static string PreviewBody()
        => $$"""
            {
              "requestSchemaVersion": "v1",
              "conversationId": "{{ConversationIdValue}}",
              "folderId": "{{FolderIdValue}}",
              "fileReferenceIds": ["{{FileIdValue}}"],
              "suggestedName": "Suggested Project",
              "description": "Safe project description",
              "setupMetadata": "Safe setup note"
            }
            """;

    private static string ConfirmBody(
        string displayName = "Suggested Project",
        string? metadataClass = "tenant_sensitive",
        string? description = "Safe project description",
        string? setupMetadata = "Safe setup note")
    {
        string displayNameJson = JsonSerializer.Serialize(displayName);
        string descriptionJson = JsonSerializer.Serialize(description);
        string setupMetadataJson = JsonSerializer.Serialize(setupMetadata);
        string projectMetadata = metadataClass is null
            ? $$"""{ "displayName": {{displayNameJson}} }"""
            : $$"""{ "displayName": {{displayNameJson}}, "metadataClass": "{{metadataClass}}" }""";
        return $$"""
            {
              "requestSchemaVersion": "v1",
              "operation": "confirmNewProjectProposal",
              "resolutionResult": "NoMatch",
              "confirmed": true,
              "projectId": "{{ProjectIdValue}}",
              "conversationId": "{{ConversationIdValue}}",
              "projectMetadata": {{projectMetadata}},
              "description": {{descriptionJson}},
              "setupMetadata": {{setupMetadataJson}},
              "folder": {
                "folderId": "{{FolderIdValue}}",
                "folderMetadata": { "displayName": "Workspace folder" }
              },
              "fileReferences": [
                {
                  "fileReferenceId": "{{FileIdValue}}",
                  "folderId": "{{FolderIdValue}}",
                  "workspaceId": "{{WorkspaceIdValue}}",
                  "filePath": "docs/readme.md",
                  "fileMetadata": { "displayName": "Design brief" }
                }
              ],
              "fileReferenceIds": ["{{FileIdValue}}"]
            }
            """;
    }

    private static string ConfirmBodyMinimal()
        => $$"""
            {
              "requestSchemaVersion": "v1",
              "operation": "confirmNewProjectProposal",
              "resolutionResult": "NoMatch",
              "confirmed": true,
              "projectId": "{{ProjectIdValue}}",
              "conversationId": "{{ConversationIdValue}}",
              "projectMetadata": { "displayName": "Suggested Project", "metadataClass": "tenant_sensitive" }
            }
            """;

    private static Generated.ConfirmNewProjectProposalRequest GeneratedConfirmRequest(
        (string DisplayName, string? Description, string? SetupMetadata) metadata)
        => new()
        {
            RequestSchemaVersion = Generated.ConfirmNewProjectProposalRequestRequestSchemaVersion.V1,
            Operation = Generated.ConfirmNewProjectProposalRequestOperation.ConfirmNewProjectProposal,
            ResolutionResult = Generated.ConfirmNewProjectProposalRequestResolutionResult.NoMatch,
            Confirmed = true,
            ProjectId = ProjectIdValue,
            ConversationId = ConversationIdValue,
            ProjectMetadata = new Generated.ProjectMetadata
            {
                DisplayName = metadata.DisplayName,
                MetadataClass = Generated.SensitiveMetadataTier.Tenant_sensitive,
            },
            Description = metadata.Description,
            SetupMetadata = metadata.SetupMetadata,
            Folder = new Generated.ConfirmNewProjectProposalFolder
            {
                FolderId = FolderIdValue,
                FolderMetadata = new Generated.ProjectFolderMetadata { DisplayName = "Workspace folder" },
            },
            FileReferences =
            [
                new Generated.ConfirmNewProjectProposalFileReference
                {
                    FileReferenceId = FileIdValue,
                    FolderId = FolderIdValue,
                    WorkspaceId = WorkspaceIdValue,
                    FilePath = "docs/readme.md",
                    FileMetadata = new Generated.ProjectFileReferenceMetadata { DisplayName = "Design brief" },
                },
            ],
            FileReferenceIds = [FileIdValue],
        };

    private static (string DisplayName, string? Description, string? SetupMetadata) SeparatorProposalMetadata(
        string field,
        char separator,
        string position)
    {
        string displayName = "  Suggested Project  ";
        string? description = "  Safe project description  ";
        string? setupMetadata = "  Safe setup note  ";
        string value = position == "only"
            ? separator.ToString()
            : PositionedValue(field == "displayName" ? "Suggested Project" : field, separator, position);

        return field switch
        {
            "displayName" => (value, description, setupMetadata),
            "description" => (displayName, value, setupMetadata),
            "setupMetadata" => (displayName, description, value),
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };
    }

    private static (string DisplayName, string? Description, string? SetupMetadata) ProposalMetadataVariant(
        string field,
        string value)
        => field switch
        {
            "displayName" => (value, "Safe project description", "Safe setup note"),
            "description" => ("Synthetic Project", value, "Safe setup note"),
            "setupMetadata" => ("Synthetic Project", "Safe project description", value),
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };

    private static string PositionedValue(string value, char separator, string position)
        => position switch
        {
            "leading" => separator + value,
            "embedded" => value.Insert(value.Length / 2, separator.ToString()),
            "trailing" => value + separator,
            _ => throw new ArgumentOutOfRangeException(nameof(position)),
        };

    private static ProjectListItem ProjectRow(string projectId, string name)
        => new(TenantA, projectId, name, ProjectLifecycle.Active, 1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

    private static ProjectReferenceIndexCandidateRow ReferenceRow(string projectId, string referenceId, string referenceKind)
        => new(
            TenantA,
            projectId,
            "Project " + projectId,
            ProjectLifecycle.Active,
            [
                new ProjectReferenceIndexItem(
                    TenantA,
                    projectId,
                    referenceKind,
                    referenceId,
                    ReferenceState.Included,
                    DisplayName: null,
                    ReasonCode: null,
                    UpdatedAt: DateTimeOffset.UnixEpoch,
                    Sequence: 1),
            ]);

    private sealed record InvocationArgs(HttpContext HttpContext, CancellationToken CancellationToken);

    private sealed record EndpointResponse(
        HttpStatusCode StatusCode,
        string Body,
        IReadOnlyDictionary<string, string> Headers)
    {
        public string? Header(string name)
            => Headers.TryGetValue(name, out string? value) ? value : null;
    }

    private sealed record ProposalConfirmationLedgerEntry(string IdempotencyKey, string Fingerprint);

    private sealed class CapturingProposalConfirmationIdempotencyLedger : IProjectProposalConfirmationIdempotencyLedger
    {
        private readonly Dictionary<string, string> _fingerprints = new(StringComparer.Ordinal);

        public List<ProposalConfirmationLedgerEntry> Entries { get; } = [];

        public bool TryRecord(string idempotencyKey, string fingerprint)
        {
            Entries.Add(new ProposalConfirmationLedgerEntry(idempotencyKey, fingerprint));
            if (_fingerprints.TryGetValue(idempotencyKey, out string? existing))
            {
                return string.Equals(existing, fingerprint, StringComparison.Ordinal);
            }

            _fingerprints[idempotencyKey] = fingerprint;
            return true;
        }
    }

    private sealed class FixedProjectTenantContext(string? tenantId, string? principalId) : IProjectTenantContextAccessor
    {
        public string? AuthoritativeTenantId { get; } = tenantId;

        public string? PrincipalId { get; } = principalId;

        public EventStoreClaimTransformEvidence GetClaimTransformEvidence(string actionToken)
            => string.IsNullOrWhiteSpace(AuthoritativeTenantId) || string.IsNullOrWhiteSpace(PrincipalId)
                ? EventStoreClaimTransformEvidence.Missing()
                : EventStoreClaimTransformEvidence.Allowed(
                    AuthoritativeTenantId,
                    PrincipalId,
                    [
                        ProjectAuthorizationGate.CreateProjectAction,
                        ProjectAuthorizationGate.ListProjectsAction,
                        ProjectAuthorizationGate.ReadProjectAction,
                    ]);
    }

    private sealed class StubConversationResolutionDirectory(ConversationResolutionMetadata metadata, bool throws = false) : IProjectConversationResolutionDirectory
    {
        public Task<ConversationResolutionMetadata> ReadConversationMetadataAsync(
            ConversationId conversationId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            string correlationId,
            CancellationToken cancellationToken = default)
            => throws
                ? Task.FromException<ConversationResolutionMetadata>(new InvalidOperationException("conversation metadata unavailable"))
                : Task.FromResult(metadata);
    }

    private sealed class StubProjectListReadModel(IReadOnlyList<ProjectListItem> rows) : IProjectListReadModel
    {
        public Task<IReadOnlyList<ProjectListItem>> ListAsync(
            string authoritativeTenantId,
            ProjectLifecycle? lifecycleFilter,
            CancellationToken cancellationToken = default)
            => Task.FromResult(rows);
    }

    private sealed class StubProjectReferenceIndexReadModel(IReadOnlyList<ProjectReferenceIndexCandidateRow> rows, bool throws) : IProjectReferenceIndexReadModel
    {
        public Task<IReadOnlyList<ProjectReferenceIndexCandidateRow>> ListByReferenceAsync(
            string authoritativeTenantId,
            IReadOnlyCollection<string> folderIds,
            IReadOnlyCollection<string> fileReferenceIds,
            CancellationToken cancellationToken = default)
            => throws
                ? Task.FromException<IReadOnlyList<ProjectReferenceIndexCandidateRow>>(new InvalidOperationException("reference index unavailable"))
                : Task.FromResult(rows);
    }

    private sealed class CapturingAssignmentDirectory(ProjectConversationAssignmentResult result) : IProjectConversationAssignmentDirectory
    {
        public List<AssignmentCall> Confirmed { get; } = [];

        public Task<ProjectConversationAssignmentResult> LinkAsync(
            ProjectId projectId,
            ConversationId conversationId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            ProjectId? expectedCurrentProjectId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectConversationAssignmentResult> MoveAsync(
            ProjectId targetProjectId,
            ConversationId conversationId,
            ProjectId sourceProjectId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectConversationAssignmentResult> UnlinkAsync(
            ProjectId projectId,
            ConversationId conversationId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public Task<ProjectConversationAssignmentResult> ConfirmResolutionAssignmentAsync(
            ProjectId targetProjectId,
            ConversationId conversationId,
            ProjectId? expectedSourceProjectId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            ProjectConversationCommandMetadata metadata,
            CancellationToken cancellationToken = default)
        {
            Confirmed.Add(new AssignmentCall(targetProjectId, conversationId, expectedSourceProjectId, metadata));
            return Task.FromResult(result);
        }
    }

    private sealed record AssignmentCall(
        ProjectId TargetProjectId,
        ConversationId ConversationId,
        ProjectId? ExpectedSourceProjectId,
        ProjectConversationCommandMetadata Metadata);

    private sealed class RecordingFolderDirectory(ProjectFolderValidationOutcome outcome) : IProjectFolderDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(
            ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectFolderValidationResult(outcome, correlationId));
        }

        public Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(
            ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectFolderValidationResult(ProjectFolderValidationOutcome.Unavailable, correlationId));
    }

    private sealed class RecordingFileReferenceDirectory(ProjectFileReferenceValidationOutcome outcome) : IProjectFileReferenceDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectFileReferenceValidationResult> ValidateLinkFileReferenceAsync(
            ProjectId projectId,
            string folderId,
            string workspaceId,
            string filePath,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectFileReferenceValidationResult(outcome, correlationId));
        }

        public Task<ProjectFileReferenceValidationResult> RefreshFileReferenceAsync(
            ProjectId projectId,
            string fileReferenceId,
            string folderId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectFileReferenceValidationResult(ProjectFileReferenceValidationOutcome.Unavailable, correlationId));
    }

    private sealed class CapturingProjectCommandSubmitter : IProjectCommandSubmitter
    {
        private static readonly ProjectCommandSubmissionResult DefaultAccepted = ProjectCommandSubmissionResult.Accepted("projects-corr", false);

        public Queue<ProjectCommandSubmissionResult> CreateResults { get; } = [];

        public Queue<ProjectCommandSubmissionResult> FolderResults { get; } = [];

        public Queue<ProjectCommandSubmissionResult> FileResults { get; } = [];

        public List<CreateProject> Created { get; } = [];

        public List<SetProjectFolder> Folders { get; } = [];

        public List<LinkFileReference> Files { get; } = [];

        public Task<ProjectCommandSubmissionResult> SubmitCreateProjectAsync(CreateProject command, CancellationToken cancellationToken = default)
        {
            Created.Add(command);
            return Task.FromResult(CreateResults.TryDequeue(out ProjectCommandSubmissionResult? result) ? result : DefaultAccepted);
        }

        public Task<ProjectCommandSubmissionResult> SubmitUpdateProjectSetupAsync(UpdateProjectSetup command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitArchiveProjectAsync(ArchiveProject command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitSetProjectFolderAsync(SetProjectFolder command, CancellationToken cancellationToken = default)
        {
            Folders.Add(command);
            return Task.FromResult(FolderResults.TryDequeue(out ProjectCommandSubmissionResult? result) ? result : DefaultAccepted);
        }

        public Task<ProjectCommandSubmissionResult> SubmitLinkFileReferenceAsync(LinkFileReference command, CancellationToken cancellationToken = default)
        {
            Files.Add(command);
            return Task.FromResult(FileResults.TryDequeue(out ProjectCommandSubmissionResult? result) ? result : DefaultAccepted);
        }

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkFileReferenceAsync(UnlinkFileReference command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitLinkMemoryAsync(LinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkMemoryAsync(UnlinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitConfirmProjectResolutionAsync(
            ConfirmProjectResolution command,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("projects-corr", false));
    }
}
