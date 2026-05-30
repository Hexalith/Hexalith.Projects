// <copyright file="CreateProjectEndpointTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.ProjectList;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Memories;
using Hexalith.Projects.Testing.Leakage;
using Hexalith.Projects.Testing.TenantIsolation;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-2 tests for the Story 1.4 Server slice (AC 1, 2, 5, 7): the <c>POST /api/v1/projects</c>
/// endpoint returns <c>202 AcceptedCommand</c> on a valid create, maps a fail-closed denial to
/// <c>404</c> (not 500, not 200), and the minimal <c>GetProject</c> read returns the projected detail
/// with freshness after the projection updates. Uses an in-memory fake submitter / in-memory read
/// model — a real boundary stand-in, not real Dapr/infra.
/// </summary>
public sealed class CreateProjectEndpointTests
{
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string FileRefId = "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1";
    private const string MemoryRefId = "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1";

    [Fact]
    public async Task PostProject_ValidCreate_Returns202AcceptedCommand()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("status").GetString().ShouldBe("accepted");
            document.RootElement.GetProperty("idempotentReplay").GetBoolean().ShouldBeFalse();

            submitter.Submitted.Count.ShouldBe(1);
            CreateProject submitted = submitter.Submitted.Single();
            submitted.TenantId.ShouldBe("tenant-a");
            submitted.ProjectId.Value.ShouldBe(ProjectIdValue);
            submitted.Name.ShouldBe("Tracer Bullet");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_GeneratedClientCreateShape_Returns202AcceptedCommand()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GeneratedClientCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            CreateProject submitted = submitter.Submitted.Single();
            submitted.TenantId.ShouldBe("tenant-a");
            submitted.ProjectId.Value.ShouldNotBeNullOrWhiteSpace();
            submitted.Name.ShouldBe("Tracer Bullet");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PatchProjectSetup_Authorized_Returns202AndSubmitsUpdate()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidUpdateSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            UpdateProjectSetup submitted = submitter.Updated.Single();
            submitted.TenantId.ShouldBe("tenant-a");
            submitted.ProjectId.Value.ShouldBe(ProjectIdValue);
            submitted.Setup.Goals.ShouldBe(["keep continuity current"]);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProjectArchive_Authorized_Returns202AndSubmitsArchive()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidArchiveRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            submitter.Archived.Single().ProjectId.Value.ShouldBe(ProjectIdValue);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PutProjectFolder_AuthorizedAndFolderValidated_Returns202AndSubmitsSetFolder()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            folderDirectory: new FixedProjectFolderDirectory(ProjectFolderValidationOutcome.Accepted)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidSetFolderRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            SetProjectFolder submitted = submitter.FolderSet.Single();
            submitted.ProjectId.Value.ShouldBe(ProjectIdValue);
            submitted.FolderId.ShouldBe("folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC");
            submitted.FolderMetadata.DisplayName.ShouldBe("Tracer Folder");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PutProjectFolder_UnavailableFolderEvidence_Returns503AndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            folderDirectory: new FixedProjectFolderDirectory(ProjectFolderValidationOutcome.Unavailable)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidSetFolderRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            submitter.FolderSet.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PutProjectFolder_DeniedFolderEvidence_ReturnsSafe404MetadataOnlyAndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            folderDirectory: new FixedProjectFolderDirectory(ProjectFolderValidationOutcome.Denied)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidSetFolderRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("category").GetString().ShouldBe("tenant_access_denied");
            document.RootElement.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("redacted");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
            submitter.FolderSet.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PutProjectFolder_ReplacingExistingFolderWithoutConfirmation_Returns400BeforeFoldersAcl()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        TrackingProjectFolderDirectory folderDirectory = new(ProjectFolderValidationOutcome.Accepted);
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            folderDirectory: folderDirectory).ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));
            readModel.Project("tenant-a", FolderSetEvent("tenant-a", "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                ValidSetFolderRequest(folderId: "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AD"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            folderDirectory.CallCount.ShouldBe(0);
            submitter.FolderSet.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PutProjectFolder_ReplacingExistingFolderWithConfirmation_SubmitsReplacement()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            folderDirectory: new FixedProjectFolderDirectory(ProjectFolderValidationOutcome.Accepted)).ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));
            readModel.Project("tenant-a", FolderSetEvent("tenant-a", "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                ValidSetFolderRequest(
                    folderId: "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AD",
                    replacementConfirmed: true),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            SetProjectFolder submitted = submitter.FolderSet.Single();
            submitted.FolderId.ShouldBe("folder_01HZ9K8YQ3W6V2N4R7T5P0X1AD");
            submitted.ReplacementConfirmed.ShouldBeTrue();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PutProjectFolder_RouteBodyMismatch_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            folderDirectory: new FixedProjectFolderDirectory(ProjectFolderValidationOutcome.Accepted)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                ValidSetFolderRequest(projectId: "01HZ9K8YQ3W6V2N4R7T5P0X1ZZ"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.FolderSet.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkFile_AuthorizedAndFolderValidated_Returns202AndSubmitsLink()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            fileReferenceDirectory: new TrackingProjectFileReferenceDirectory(ProjectFileReferenceValidationOutcome.Accepted)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkFileRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            LinkFileReference submitted = submitter.FileLinked.Single();
            submitted.FileReferenceId.ShouldBe(FileRefId);
            submitted.FolderId.ShouldBe("folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC");
            submitted.FileMetadata.DisplayName.ShouldBe("contract.pdf");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkFile_DeniedFoldersEvidence_ReturnsSafe404AndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            fileReferenceDirectory: new TrackingProjectFileReferenceDirectory(ProjectFileReferenceValidationOutcome.Denied)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkFileRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
            submitter.FileLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkFile_StaleFoldersEvidence_Returns503AndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            fileReferenceDirectory: new TrackingProjectFileReferenceDirectory(ProjectFileReferenceValidationOutcome.Stale)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkFileRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            submitter.FileLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkFile_MissingIdempotencyKey_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            fileReferenceDirectory: new TrackingProjectFileReferenceDirectory(ProjectFileReferenceValidationOutcome.Accepted)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = ValidLinkFileRequest();
            request.Headers.Remove("Idempotency-Key");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.FileLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkFile_RouteBodyFileIdMismatch_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            fileReferenceDirectory: new TrackingProjectFileReferenceDirectory(ProjectFileReferenceValidationOutcome.Accepted)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                ValidLinkFileRequest(bodyFileReferenceId: "file_01HZ9K8YQ3W6V2N4R7T5P0X1ZZ"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.FileLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkFile_ArchivedProject_ReturnsSafe404BeforeFoldersAcl()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        TrackingProjectFileReferenceDirectory fileDirectory = new(ProjectFileReferenceValidationOutcome.Accepted);
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            fileReferenceDirectory: fileDirectory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a", ProjectLifecycle.Archived));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkFileRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            fileDirectory.CallCount.ShouldBe(0);
            submitter.FileLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkFile_UnknownTenantProjection_ReturnsSafe404BeforeFoldersAcl()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        TrackingProjectFileReferenceDirectory fileDirectory = new(ProjectFileReferenceValidationOutcome.Accepted);
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            seedTenantAccess: false,
            fileReferenceDirectory: fileDirectory).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkFileRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            fileDirectory.CallCount.ShouldBe(0);
            submitter.FileLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task DeleteFile_Authorized_Returns202AndSubmitsUnlinkWithoutFoldersCall()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));

        // No file-reference directory is registered: unlink must never call Folders. The default
        // UnavailableProjectFileReferenceDirectory would fail closed if it were ever invoked.
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidUnlinkFileRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            UnlinkFileReference submitted = submitter.FileUnlinked.Single();
            submitted.FileReferenceId.ShouldBe(FileRefId);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task DeleteFile_RouteBodyMismatch_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                ValidUnlinkFileRequest(bodyFileReferenceId: "file_01HZ9K8YQ3W6V2N4R7T5P0X1ZZ"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.FileUnlinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task DeleteFile_MissingIdempotencyKey_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = ValidUnlinkFileRequest();
            request.Headers.Remove("Idempotency-Key");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.FileUnlinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkFile_RedactedFoldersEvidence_ReturnsSafe404AndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            fileReferenceDirectory: new TrackingProjectFileReferenceDirectory(ProjectFileReferenceValidationOutcome.Redacted)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkFileRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Redacted/excluded/sensitivity evidence must collapse to an externally-indistinguishable safe
            // denial (404) so file sensitivity is never disclosed through Projects, and must not submit.
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
            submitter.FileLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkFile_UnavailableFoldersEvidence_Returns503AndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            fileReferenceDirectory: new TrackingProjectFileReferenceDirectory(ProjectFileReferenceValidationOutcome.Unavailable)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkFileRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Unavailable Folders evidence is retryable: 503, never a false accept.
            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            submitter.FileLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkFile_UnknownBodyField_Returns400AndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            fileReferenceDirectory: new TrackingProjectFileReferenceDirectory(ProjectFileReferenceValidationOutcome.Accepted)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(LinkFileRequestWithUnknownField(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Closed request schema (JsonUnmappedMemberHandling.Disallow): unknown fields are rejected.
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.FileLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task DeleteFile_UnknownBodyField_Returns400AndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(UnlinkFileRequestWithUnknownField(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.FileUnlinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkMemory_Authorized_Returns202AndSubmitsLink()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        TrackingProjectMemoryDirectory memoryDirectory = new(ProjectMemoryValidationOutcome.Accepted);
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            memoryDirectory: memoryDirectory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkMemoryRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            memoryDirectory.CallCount.ShouldBe(1);
            LinkMemory submitted = submitter.MemoryLinked.Single();
            submitted.MemoryReferenceId.ShouldBe(MemoryRefId);
            submitted.MemoryMetadata.DisplayName.ShouldBe("Q3 product strategy memory");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkMemory_RouteBodyMismatch_Returns400BeforeMemoriesCall()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        TrackingProjectMemoryDirectory memoryDirectory = new(ProjectMemoryValidationOutcome.Accepted);
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            memoryDirectory: memoryDirectory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                ValidLinkMemoryRequest(bodyMemoryReferenceId: "case_01HZ9K8YQ3W6V2N4R7T5P0X1ZZ"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.MemoryLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkMemory_MissingIdempotencyKey_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            memoryDirectory: new TrackingProjectMemoryDirectory(ProjectMemoryValidationOutcome.Accepted)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = ValidLinkMemoryRequest();
            request.Headers.Remove("Idempotency-Key");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.MemoryLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkMemory_UnknownBodyField_Returns400AndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            memoryDirectory: new TrackingProjectMemoryDirectory(ProjectMemoryValidationOutcome.Accepted)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(LinkMemoryRequestWithUnknownField(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Closed request schema (JsonUnmappedMemberHandling.Disallow): unknown fields are rejected.
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.MemoryLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkMemory_ArchivedProject_ReturnsSafe404BeforeMemoriesCall()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        TrackingProjectMemoryDirectory memoryDirectory = new(ProjectMemoryValidationOutcome.Accepted);
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            memoryDirectory: memoryDirectory).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a", ProjectLifecycle.Archived));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkMemoryRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Archived/unauthorized Project must short-circuit BEFORE any Memories ACL call (AC 11).
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            memoryDirectory.CallCount.ShouldBe(0);
            submitter.MemoryLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkMemory_DeniedMemoriesEvidence_ReturnsSafe404AndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            memoryDirectory: new TrackingProjectMemoryDirectory(ProjectMemoryValidationOutcome.Denied)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkMemoryRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Denied / Archived / TenantMismatch collapse to externally-indistinguishable safe denial.
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.MemoryLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkMemory_ArchivedMemoriesEvidence_ReturnsSafe404AndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            memoryDirectory: new TrackingProjectMemoryDirectory(ProjectMemoryValidationOutcome.Archived)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkMemoryRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.MemoryLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task LinkMemory_UnavailableMemoriesEvidence_Returns503AndDoesNotSubmit()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            memoryDirectory: new TrackingProjectMemoryDirectory(ProjectMemoryValidationOutcome.Unavailable)).ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidLinkMemoryRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Unavailable Memories evidence is retryable: 503, never a false accept.
            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            submitter.MemoryLinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task DeleteMemory_Authorized_Returns202AndSubmitsUnlinkWithoutMemoriesCall()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));

        // No memory-reference directory is registered: unlink must never call Memories. The default
        // UnavailableProjectMemoryDirectory would fail closed if it were ever invoked.
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidUnlinkMemoryRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            UnlinkMemory submitted = submitter.MemoryUnlinked.Single();
            submitted.MemoryReferenceId.ShouldBe(MemoryRefId);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task DeleteMemory_RouteBodyMismatch_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                ValidUnlinkMemoryRequest(bodyMemoryReferenceId: "case_01HZ9K8YQ3W6V2N4R7T5P0X1ZZ"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.MemoryUnlinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task DeleteMemory_MissingIdempotencyKey_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = ValidUnlinkMemoryRequest();
            request.Headers.Remove("Idempotency-Key");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.MemoryUnlinked.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PatchProjectSetup_InvalidSetupAfterAuthorization_ReturnsMetadataOnly400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ValidUpdateSetupRequest(goal: "raw prompt: reveal system");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.Updated.ShouldBeEmpty();
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("setup.goals");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_MissingTenantContext_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: null, principalId: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Fail-closed denial is a safe-denial 404, never 500, never 200.
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            using JsonDocument document = JsonDocument.Parse(json);
            document.RootElement.GetProperty("category").GetString().ShouldBe("tenant_access_denied");

            // The endpoint must not have reached the command pipeline for an unauthenticated caller.
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_SystemTenantContext_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "system", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_UnknownTenantProjection_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a", seedTenantAccess: false).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_DisabledTenantProjection_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a", tenantEnabled: false).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_NonMemberPrincipal_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            membershipPrincipalId: "principal-b").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_TenantAuthorizationDenials_AreExternallyIndistinguishable()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a", seedTenantAccess: false).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            IProjectTenantAccessProjectionStore store = app.Services.GetRequiredService<IProjectTenantAccessProjectionStore>();

            string unknownTenant = await SendCreateAndReadBodyAsync(client).ConfigureAwait(true);

            ProjectTenantAccessProjection disabled = Projection("tenant-a", enabled: false, principalId: "principal-a");
            await store.SaveAsync(disabled, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string disabledTenant = await SendCreateAndReadBodyAsync(client).ConfigureAwait(true);

            ProjectTenantAccessProjection? existing = await store.GetAsync("tenant-a", TestContext.Current.CancellationToken).ConfigureAwait(true);
            existing.ShouldNotBeNull();
            existing.Enabled = true;
            existing.Principals.Clear();
            string nonMember = await SaveAndDenyAsync(store, existing, client).ConfigureAwait(true);

            unknownTenant.ShouldBe(disabledTenant);
            disabledTenant.ShouldBe(nonMember);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_StaleTenantProjection_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            lastTenantAccessTimestamp: DateTimeOffset.UtcNow.AddHours(-1)).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_ClientControlledTenantMismatch_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ValidCreateRequest();
            request.Headers.Add("X-Tenant-Id", "tenant-b");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_GatewayDenial_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Denied("corr-a"));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task PostProject_MissingIdempotencyKey_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = ValidCreateRequest();
            request.Headers.Remove("Idempotency-Key");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            submitter.Submitted.ShouldBeEmpty();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_AfterProjectionUpdates_ReturnsProjectedDetailWithFreshness()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            // Drive the projection as the Workers/projection subscriber would after the 202'd create.
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("projectId").GetString().ShouldBe(ProjectIdValue);
            document.RootElement.GetProperty("lifecycleState").GetString().ShouldBe("active");
            document.RootElement.GetProperty("setupMetadata").GetString().ShouldBe("setup-reference");
            document.RootElement.GetProperty("contextActivation").GetProperty("enabled").GetBoolean().ShouldBeTrue();
            document.RootElement.GetProperty("contextActivation").TryGetProperty("blockedReasonCode", out JsonElement blockedReason).ShouldBeTrue();
            blockedReason.ValueKind.ShouldBe(JsonValueKind.Null);
            document.RootElement.GetProperty("references").GetArrayLength().ShouldBe(0);
            document.RootElement.GetProperty("freshness").GetProperty("readConsistency").GetString().ShouldBe("eventually_consistent");
            document.RootElement.GetProperty("freshness").GetProperty("trustState").GetString().ShouldBe("trusted");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_CrossTenant_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-b", principalId: "principal-b").ConfigureAwait(true);
        try
        {
            // Project created in tenant A; the authenticated caller is tenant B → safe-denial 404.
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_ArchivedProject_ReturnsMetadataWithContextActivationBlocked()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a", ProjectLifecycle.Archived));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("lifecycleState").GetString().ShouldBe("archived");
            document.RootElement.GetProperty("contextActivation").GetProperty("enabled").GetBoolean().ShouldBeFalse();
            document.RootElement.GetProperty("contextActivation").GetProperty("blockedReasonCode").GetString().ShouldBe("archived");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetAndList_AfterSetupUpdateAndArchive_ReflectProjectionEvents()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel detail = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            InMemoryProjectListReadModel list = app.Services.GetRequiredService<InMemoryProjectListReadModel>();
            detail.Project("tenant-a", CreatedEvent("tenant-a"));
            list.Project("tenant-a", CreatedEvent("tenant-a"));
            detail.Project("tenant-a", SetupUpdatedEvent("tenant-a"));
            list.Project("tenant-a", SetupUpdatedEvent("tenant-a"));
            detail.Project("tenant-a", ArchivedEvent("tenant-a"));
            list.Project("tenant-a", ArchivedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage open = await client.GetAsync($"/api/v1/projects/{ProjectIdValue}", TestContext.Current.CancellationToken).ConfigureAwait(true);
            HttpResponseMessage active = await client.GetAsync("/api/v1/projects?lifecycle=active", TestContext.Current.CancellationToken).ConfigureAwait(true);
            HttpResponseMessage archived = await client.GetAsync("/api/v1/projects?lifecycle=archived", TestContext.Current.CancellationToken).ConfigureAwait(true);

            open.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(await open.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("projectSetup").GetProperty("goals")[0].GetString().ShouldBe("keep continuity current");
            document.RootElement.GetProperty("contextActivation").GetProperty("enabled").GetBoolean().ShouldBeFalse();
            active.StatusCode.ShouldBe(HttpStatusCode.OK);
            archived.StatusCode.ShouldBe(HttpStatusCode.OK);
            JsonDocument.Parse(await active.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
                .RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
            JsonDocument.Parse(await archived.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
                .RootElement.GetProperty("items").GetArrayLength().ShouldBe(1);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ListProjects_Authorized_ReturnsOnlyTenantScopedFilteredRows()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectListReadModel readModel = app.Services.GetRequiredService<InMemoryProjectListReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a", ProjectLifecycle.Active, ProjectIdValue));
            readModel.Project("tenant-a", CreatedEvent("tenant-a", ProjectLifecycle.Archived, "01HZ9K8YQ3W6V2N4R7T5P0X1AC"));
            readModel.Project("tenant-b", CreatedEvent("tenant-b", ProjectLifecycle.Active, "01HZ9K8YQ3W6V2N4R7T5P0X1AD"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage activeResponse = await client
                .GetAsync("/api/v1/projects?lifecycle=active", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            HttpResponseMessage allResponse = await client
                .GetAsync("/api/v1/projects", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            activeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            activeResponse.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");
            using JsonDocument activeDocument = JsonDocument.Parse(
                await activeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            JsonElement activeItems = activeDocument.RootElement.GetProperty("items");
            activeItems.GetArrayLength().ShouldBe(1);
            activeItems[0].GetProperty("projectId").GetString().ShouldBe(ProjectIdValue);
            activeItems[0].TryGetProperty("tenantId", out _).ShouldBeFalse();

            allResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument allDocument = JsonDocument.Parse(
                await allResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            allDocument.RootElement.GetProperty("items").GetArrayLength().ShouldBe(2);
            allDocument.RootElement.GetProperty("freshness").GetProperty("trustState").GetString().ShouldBe("trusted");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_AuthorizedQueryWithIdempotencyKey_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            request.Headers.Add("Idempotency-Key", "idem-key-a");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ListProjects_AuthorizedQueryWithIdempotencyKey_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/projects");
            request.Headers.Add("Idempotency-Key", "idem-key-a");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_CrossTenantIdempotencyKey_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-b", principalId: "principal-b").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            request.Headers.Add("Idempotency-Key", "idem-key-a");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ListProjects_MissingTenantIdempotencyKey_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: null, principalId: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/projects");
            request.Headers.Add("Idempotency-Key", "idem-key-a");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ListProjects_AuthorizedInvalidLifecycle_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };

            HttpResponseMessage response = await client
                .GetAsync("/api/v1/projects?lifecycle=deleted", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_CrossTenantInvalidFreshness_MapsToSafeDenial404()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-b", principalId: "principal-b").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            request.Headers.Add("X-Hexalith-Freshness", "strict");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_AuthorizedInvalidFreshness_Returns400()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            request.Headers.Add("X-Hexalith-Freshness", "strict");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_CrossTenantExistingProjectAndMissingProject_AreIndistinguishable()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-b", CreatedEvent("tenant-b"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage foreignProject = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
            using HttpRequestMessage missingProject = new(HttpMethod.Get, "/api/v1/projects/01HZ9K8YQ3W6V2N4R7T5P0X1AC");
            foreignProject.Headers.Add("X-Correlation-Id", "corr-same");
            missingProject.Headers.Add("X-Correlation-Id", "corr-same");

            HttpResponseMessage foreignResponse = await client.SendAsync(foreignProject, TestContext.Current.CancellationToken).ConfigureAwait(true);
            HttpResponseMessage missingResponse = await client.SendAsync(missingProject, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string foreignBody = await foreignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            string missingBody = await missingResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            foreignResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            missingResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            foreignBody.ShouldBe(missingBody);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task TenantIsolationConformance_CoversEndpointReadAndQueryFilter()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-b", principalId: "principal-b").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));
            InMemoryProjectListReadModel listReadModel = app.Services.GetRequiredService<InMemoryProjectListReadModel>();
            listReadModel.Project("tenant-a", CreatedEvent("tenant-a"));

            await ProjectTenantIsolationConformance.AssertNoLeakageAsync(
                [
                    new ProjectTenantIsolationSurface(
                        "query-filter",
                        _ =>
                        {
                            ProjectDetailItem? leaked = ProjectQueryTenantFilter.Filter(
                                "tenant-b",
                                new ProjectDetailItem(
                                    "tenant-a",
                                    ProjectIdValue,
                                    "Foreign",
                                    null,
                                    null,
                                    null,
                                    null,
                                    [],
                                    [],
                                    ProjectLifecycle.Active,
                                    DateTimeOffset.UnixEpoch,
                                    DateTimeOffset.UnixEpoch,
                                    1));

                            return Task.FromResult(leaked is null
                                ? ProjectTenantIsolationResult.NoLeak("ProjectQueryTenantFilter")
                                : ProjectTenantIsolationResult.Leak("ProjectQueryTenantFilter", leaked.TenantId, leaked.ProjectId));
                        }),
                    new ProjectTenantIsolationSurface(
                        "read-endpoint",
                        async cancellationToken =>
                        {
                            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
                            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}");
                            HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(true);
                            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
                            bool leaked = response.StatusCode != HttpStatusCode.NotFound
                                || body.Contains(ProjectIdValue, StringComparison.Ordinal)
                                || body.Contains("tenant-a", StringComparison.Ordinal);

                            return leaked
                                ? ProjectTenantIsolationResult.Leak("GET /api/v1/projects/{projectId}", "tenant-a", ProjectIdValue)
                                : ProjectTenantIsolationResult.NoLeak("GET /api/v1/projects/{projectId}");
                        }),
                    new ProjectTenantIsolationSurface(
                        "list-query-filter",
                        _ =>
                        {
                            IReadOnlyList<ProjectListItem> leaked = ProjectQueryTenantFilter.FilterList(
                                "tenant-b",
                                [
                                    new ProjectListItem(
                                        "tenant-a",
                                        ProjectIdValue,
                                        "Foreign",
                                        ProjectLifecycle.Active,
                                        1,
                                        DateTimeOffset.UnixEpoch,
                                        DateTimeOffset.UnixEpoch),
                                ]);

                            return Task.FromResult(leaked.Count == 0
                                ? ProjectTenantIsolationResult.NoLeak("ProjectQueryTenantFilter.FilterList")
                                : ProjectTenantIsolationResult.Leak("ProjectQueryTenantFilter.FilterList", leaked[0].TenantId, leaked[0].ProjectId));
                        }),
                    new ProjectTenantIsolationSurface(
                        "list-endpoint",
                        async cancellationToken =>
                        {
                            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
                            HttpResponseMessage response = await client.GetAsync("/api/v1/projects", cancellationToken).ConfigureAwait(true);
                            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
                            bool leaked = response.StatusCode != HttpStatusCode.OK
                                || body.Contains(ProjectIdValue, StringComparison.Ordinal)
                                || body.Contains("tenant-a", StringComparison.Ordinal);

                            return leaked
                                ? ProjectTenantIsolationResult.Leak("GET /api/v1/projects", "tenant-a", ProjectIdValue)
                                : ProjectTenantIsolationResult.NoLeak("GET /api/v1/projects");
                        }),
                ],
                TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task SafeDenialProblemDetails_IsMetadataOnly()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a", seedTenantAccess: false).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(ValidCreateRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task OpenAndListProjectResponses_AreMetadataOnly()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<InMemoryProjectDetailReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));
            app.Services.GetRequiredService<InMemoryProjectListReadModel>().Project("tenant-a", CreatedEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage open = await client.GetAsync($"/api/v1/projects/{ProjectIdValue}", TestContext.Current.CancellationToken).ConfigureAwait(true);
            HttpResponseMessage list = await client.GetAsync("/api/v1/projects", TestContext.Current.CancellationToken).ConfigureAwait(true);

            open.StatusCode.ShouldBe(HttpStatusCode.OK);
            list.StatusCode.ShouldBe(HttpStatusCode.OK);
            string openBody = await open.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            string listBody = await list.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(openBody));
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(listBody));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_WithPendingAutoFolderReference_ExposesPendingReferenceMetadataOnly()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            InMemoryProjectDetailReadModel readModel = app.Services.GetRequiredService<InMemoryProjectDetailReadModel>();
            readModel.Project("tenant-a", CreatedEvent("tenant-a"));
            readModel.Project("tenant-a", FolderPendingEvent("tenant-a"));

            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.GetAsync(
                $"/api/v1/projects/{ProjectIdValue}",
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement reference = document.RootElement.GetProperty("references").EnumerateArray().Single();
            reference.GetProperty("referenceKind").GetString().ShouldBe("folder");
            reference.GetProperty("referenceState").GetString().ShouldBe("pending");
            reference.TryGetProperty("referenceId", out _).ShouldBeFalse();
            reference.GetProperty("displayName").GetString().ShouldBe("Tracer Bullet");
            reference.GetProperty("reasonCode").GetString().ShouldBe("folder_create_external_unavailable");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProject_ReadModelUnavailable_ReturnsMetadataOnly503()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            detailReadUnavailable: true).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client
                .GetAsync($"/api/v1/projects/{ProjectIdValue}", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ListProjects_ReadModelUnavailable_ReturnsMetadataOnly503()
    {
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", false));
        WebApplication app = await StartAppAsync(
            submitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            listReadUnavailable: true).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client
                .GetAsync("/api/v1/projects", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    private static ProjectCreated CreatedEvent(
        string tenant,
        ProjectLifecycle lifecycle = ProjectLifecycle.Active,
        string projectId = ProjectIdValue) => new(
        tenant,
        projectId,
        "Tracer Bullet",
        "A safe description",
        "setup-reference",
        lifecycle,
        "principal-a",
        "corr-a",
        "task-a",
        "idem-key-a",
        "sha256:deadbeef",
        DateTimeOffset.UnixEpoch);

    private static ProjectSetupUpdated SetupUpdatedEvent(string tenant) => new(
        tenant,
        ProjectIdValue,
        Setup(),
        "principal-a",
        "corr-setup",
        "task-setup",
        "idem-key-setup",
        "sha256:setup",
        DateTimeOffset.UnixEpoch.AddMinutes(1));

    private static ProjectArchived ArchivedEvent(string tenant) => new(
        tenant,
        ProjectIdValue,
        ProjectLifecycle.Archived,
        "principal-a",
        "corr-archive",
        "task-archive",
        "idem-key-archive",
        "sha256:archive",
        DateTimeOffset.UnixEpoch.AddMinutes(2));

    private static ProjectFolderCreationPending FolderPendingEvent(string tenant) => new(
        tenant,
        ProjectIdValue,
        "Tracer Bullet",
        "folder_create_external_unavailable",
        true,
        "principal-a",
        "corr-folder-pending",
        "task-folder-pending",
        "idem-key-folder-pending",
        "sha256:folder-pending",
        DateTimeOffset.UnixEpoch.AddMinutes(3));

    private static ProjectFolderSet FolderSetEvent(string tenant, string folderId) => new(
        tenant,
        ProjectIdValue,
        folderId,
        new ProjectFolderMetadata("Tracer Folder"),
        "principal-a",
        "corr-folder",
        "task-folder",
        "idem-key-folder",
        "sha256:folder-set",
        DateTimeOffset.UnixEpoch.AddMinutes(4));

    private static HttpRequestMessage ValidCreateRequest()
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/projects")
        {
            Content = JsonContent.Create(new
            {
                projectId = ProjectIdValue,
                name = "Tracer Bullet",
                description = "A safe description",
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-a");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage GeneratedClientCreateRequest()
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/projects")
        {
            Content = JsonContent.Create(new
            {
                requestSchemaVersion = "v1",
                projectMetadata = new
                {
                    displayName = "Tracer Bullet",
                    metadataClass = "tenant_sensitive",
                },
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-generated");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage ValidUpdateSetupRequest(string goal = "keep continuity current")
    {
        HttpRequestMessage request = new(HttpMethod.Patch, $"/api/v1/projects/{ProjectIdValue}/setup")
        {
            Content = JsonContent.Create(new
            {
                requestSchemaVersion = "v1",
                projectSetup = new
                {
                    goals = new[] { goal },
                    userInstructions = new[] { "use safe metadata" },
                    preferredSourceKinds = new[] { "conversation" },
                    excludedSourceKinds = new[] { "fileReference" },
                    conversationStartDefaults = new
                    {
                        linkedSourcePolicy = "authorizedReferences",
                    },
                },
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-update");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage ValidArchiveRequest()
    {
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/projects/{ProjectIdValue}/archive")
        {
            Content = JsonContent.Create(new
            {
                archiveIntent = "archive",
                requestSchemaVersion = "v1",
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-archive");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage ValidSetFolderRequest(
        string projectId = ProjectIdValue,
        string folderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
        bool replacementConfirmed = false)
    {
        HttpRequestMessage request = new(HttpMethod.Put, $"/api/v1/projects/{ProjectIdValue}/folder")
        {
            Content = JsonContent.Create(new
            {
                requestSchemaVersion = "v1",
                operation = "set",
                projectId,
                folderId,
                folderMetadata = new
                {
                    displayName = "Tracer Folder",
                },
                replacementConfirmed,
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-folder");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage ValidLinkFileRequest(
        string projectId = ProjectIdValue,
        string? bodyFileReferenceId = null,
        string folderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
        string workspaceId = "workspace_01HZ9K8YQ3W6V2N4R7T5P0X1AD",
        string filePath = "docs/contract.pdf")
    {
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/projects/{ProjectIdValue}/files/{FileRefId}/link")
        {
            Content = JsonContent.Create(new
            {
                requestSchemaVersion = "v1",
                operation = "link",
                projectId,
                fileReferenceId = bodyFileReferenceId ?? FileRefId,
                folderId,
                workspaceId,
                filePath,
                fileMetadata = new
                {
                    displayName = "contract.pdf",
                },
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-file-link");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage ValidUnlinkFileRequest(
        string projectId = ProjectIdValue,
        string? bodyFileReferenceId = null)
    {
        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/v1/projects/{ProjectIdValue}/files/{FileRefId}")
        {
            Content = JsonContent.Create(new
            {
                requestSchemaVersion = "v1",
                operation = "unlink",
                unlinkIntent = "removeReference",
                projectId,
                fileReferenceId = bodyFileReferenceId ?? FileRefId,
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-file-unlink");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage LinkFileRequestWithUnknownField()
    {
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/projects/{ProjectIdValue}/files/{FileRefId}/link")
        {
            Content = JsonContent.Create(new
            {
                requestSchemaVersion = "v1",
                operation = "link",
                projectId = ProjectIdValue,
                fileReferenceId = FileRefId,
                folderId = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC",
                workspaceId = "workspace_01HZ9K8YQ3W6V2N4R7T5P0X1AD",
                filePath = "docs/contract.pdf",
                fileMetadata = new
                {
                    displayName = "contract.pdf",
                },

                // Unexpected field rejected by the closed request schema.
                rawContent = "should-be-rejected",
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-file-link");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage ValidLinkMemoryRequest(
        string projectId = ProjectIdValue,
        string? bodyMemoryReferenceId = null)
    {
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/projects/{ProjectIdValue}/memories/{MemoryRefId}/link")
        {
            Content = JsonContent.Create(new
            {
                requestSchemaVersion = "v1",
                operation = "link",
                projectId,
                memoryReferenceId = bodyMemoryReferenceId ?? MemoryRefId,
                memoryMetadata = new
                {
                    displayName = "Q3 product strategy memory",
                },
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-memory-link");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage ValidUnlinkMemoryRequest(
        string projectId = ProjectIdValue,
        string? bodyMemoryReferenceId = null)
    {
        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/v1/projects/{ProjectIdValue}/memories/{MemoryRefId}")
        {
            Content = JsonContent.Create(new
            {
                requestSchemaVersion = "v1",
                operation = "unlink",
                unlinkIntent = "removeReference",
                projectId,
                memoryReferenceId = bodyMemoryReferenceId ?? MemoryRefId,
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-memory-unlink");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage LinkMemoryRequestWithUnknownField()
    {
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/projects/{ProjectIdValue}/memories/{MemoryRefId}/link")
        {
            Content = JsonContent.Create(new
            {
                requestSchemaVersion = "v1",
                operation = "link",
                projectId = ProjectIdValue,
                memoryReferenceId = MemoryRefId,
                memoryMetadata = new
                {
                    displayName = "Q3 product strategy memory",
                },

                // Unexpected field rejected by the closed request schema.
                memoryUnitContent = "should-be-rejected",
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-memory-link");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static HttpRequestMessage UnlinkFileRequestWithUnknownField()
    {
        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/v1/projects/{ProjectIdValue}/files/{FileRefId}")
        {
            Content = JsonContent.Create(new
            {
                requestSchemaVersion = "v1",
                operation = "unlink",
                unlinkIntent = "removeReference",
                projectId = ProjectIdValue,
                fileReferenceId = FileRefId,

                // Unexpected field rejected by the closed request schema.
                deleteUnderlyingFile = true,
            }),
        };
        request.Headers.Add("Idempotency-Key", "idem-key-file-unlink");
        request.Headers.Add("X-Correlation-Id", "corr-a");
        request.Headers.Add("X-Hexalith-Task-Id", "task-a");
        return request;
    }

    private static ProjectSetup Setup() => new(
        ["keep continuity current"],
        ["use safe metadata"],
        [ProjectContextSourceKind.Conversation],
        [ProjectContextSourceKind.FileReference],
        new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences));

    private static async Task<string> SendCreateAndReadBodyAsync(HttpClient client)
    {
        using HttpRequestMessage request = ValidCreateRequest();
        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using JsonDocument document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("tenant_access_denied");
        document.RootElement.GetProperty("code").GetString().ShouldBe("resource_unavailable");
        document.RootElement.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("redacted");
        return body;
    }

    private static async Task<string> SaveAndDenyAsync(
        IProjectTenantAccessProjectionStore store,
        ProjectTenantAccessProjection projection,
        HttpClient client)
    {
        await store.SaveAsync(projection, TestContext.Current.CancellationToken).ConfigureAwait(true);
        return await SendCreateAndReadBodyAsync(client).ConfigureAwait(true);
    }

    private static async Task<WebApplication> StartAppAsync(
        FakeProjectCommandSubmitter submitter,
        string? tenantId,
        string? principalId,
        bool seedTenantAccess = true,
        bool tenantEnabled = true,
        string? membershipPrincipalId = null,
        DateTimeOffset? lastTenantAccessTimestamp = null,
        bool detailReadUnavailable = false,
        bool listReadUnavailable = false,
        IProjectFolderDirectory? folderDirectory = null,
        IProjectFileReferenceDirectory? fileReferenceDirectory = null,
        IProjectMemoryDirectory? memoryDirectory = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.Services.AddProjectsServer();
        builder.Services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        builder.Services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        builder.Services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        builder.Services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        builder.Services.RemoveAll<IProjectTenantContextAccessor>();
        builder.Services.AddSingleton<IProjectTenantContextAccessor>(new FixedProjectTenantContextAccessor(tenantId, principalId));
        builder.Services.AddSingleton<IProjectCommandSubmitter>(submitter);
        if (folderDirectory is not null)
        {
            builder.Services.RemoveAll<IProjectFolderDirectory>();
            builder.Services.AddSingleton(folderDirectory);
        }

        if (fileReferenceDirectory is not null)
        {
            builder.Services.RemoveAll<IProjectFileReferenceDirectory>();
            builder.Services.AddSingleton(fileReferenceDirectory);
        }

        if (memoryDirectory is not null)
        {
            builder.Services.RemoveAll<IProjectMemoryDirectory>();
            builder.Services.AddSingleton(memoryDirectory);
        }

        if (detailReadUnavailable)
        {
            builder.Services.RemoveAll<IProjectDetailReadModel>();
            builder.Services.AddSingleton<IProjectDetailReadModel, ThrowingProjectDetailReadModel>();
        }

        if (listReadUnavailable)
        {
            builder.Services.RemoveAll<IProjectListReadModel>();
            builder.Services.AddSingleton<IProjectListReadModel, ThrowingProjectListReadModel>();
        }

        WebApplication app = builder.Build();
        if (seedTenantAccess && !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(principalId))
        {
            await SeedTenantAccessAsync(
                app.Services,
                tenantId,
                membershipPrincipalId ?? principalId,
                tenantEnabled,
                lastTenantAccessTimestamp ?? DateTimeOffset.UtcNow).ConfigureAwait(true);
        }

        app.MapProjectsServerEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        return app;
    }

    private static async Task SeedTenantAccessAsync(
        IServiceProvider services,
        string tenantId,
        string principalId,
        bool enabled,
        DateTimeOffset lastEventTimestamp)
    {
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = tenantId,
            Enabled = enabled,
            Watermark = 1,
            ProjectionWatermark = $"{tenantId}:1",
            LastEventTimestamp = lastEventTimestamp,
        };
        projection.Principals[principalId] = new ProjectTenantPrincipalEvidence(principalId, "TenantOwner");

        await services
            .GetRequiredService<IProjectTenantAccessProjectionStore>()
            .SaveAsync(projection, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    private static ProjectTenantAccessProjection Projection(string tenantId, bool enabled, string principalId)
    {
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = tenantId,
            Enabled = enabled,
            Watermark = 1,
            ProjectionWatermark = $"{tenantId}:1",
            LastEventTimestamp = DateTimeOffset.UtcNow,
        };
        projection.Principals[principalId] = new ProjectTenantPrincipalEvidence(principalId, "TenantOwner");
        return projection;
    }

    private static async Task StopAsync(WebApplication app)
    {
        await app.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await app.DisposeAsync().ConfigureAwait(true);
    }

    private sealed class FakeProjectCommandSubmitter(ProjectCommandSubmissionResult result) : IProjectCommandSubmitter
    {
        public List<CreateProject> Submitted { get; } = [];

        public List<UpdateProjectSetup> Updated { get; } = [];

        public List<ArchiveProject> Archived { get; } = [];

        public List<SetProjectFolder> FolderSet { get; } = [];

        public Task<ProjectCommandSubmissionResult> SubmitCreateProjectAsync(CreateProject command, CancellationToken cancellationToken = default)
        {
            Submitted.Add(command);
            return Task.FromResult(result);
        }

        public Task<ProjectCommandSubmissionResult> SubmitUpdateProjectSetupAsync(UpdateProjectSetup command, CancellationToken cancellationToken = default)
        {
            Updated.Add(command);
            return Task.FromResult(result);
        }

        public Task<ProjectCommandSubmissionResult> SubmitArchiveProjectAsync(ArchiveProject command, CancellationToken cancellationToken = default)
        {
            Archived.Add(command);
            return Task.FromResult(result);
        }

        public Task<ProjectCommandSubmissionResult> SubmitSetProjectFolderAsync(SetProjectFolder command, CancellationToken cancellationToken = default)
        {
            FolderSet.Add(command);
            return Task.FromResult(result);
        }

        public List<LinkFileReference> FileLinked { get; } = [];

        public List<UnlinkFileReference> FileUnlinked { get; } = [];

        public Task<ProjectCommandSubmissionResult> SubmitLinkFileReferenceAsync(LinkFileReference command, CancellationToken cancellationToken = default)
        {
            FileLinked.Add(command);
            return Task.FromResult(result);
        }

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkFileReferenceAsync(UnlinkFileReference command, CancellationToken cancellationToken = default)
        {
            FileUnlinked.Add(command);
            return Task.FromResult(result);
        }

        public List<LinkMemory> MemoryLinked { get; } = [];

        public List<UnlinkMemory> MemoryUnlinked { get; } = [];

        public List<ConfirmProjectResolution> ResolutionConfirmed { get; } = [];

        public Task<ProjectCommandSubmissionResult> SubmitLinkMemoryAsync(LinkMemory command, CancellationToken cancellationToken = default)
        {
            MemoryLinked.Add(command);
            return Task.FromResult(result);
        }

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkMemoryAsync(UnlinkMemory command, CancellationToken cancellationToken = default)
        {
            MemoryUnlinked.Add(command);
            return Task.FromResult(result);
        }

        public Task<ProjectCommandSubmissionResult> SubmitConfirmProjectResolutionAsync(
            ConfirmProjectResolution command,
            CancellationToken cancellationToken = default)
        {
            ResolutionConfirmed.Add(command);
            return Task.FromResult(result);
        }
    }

    private sealed class FixedProjectTenantContextAccessor(string? tenantId, string? principalId) : IProjectTenantContextAccessor
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
                        ProjectAuthorizationGate.ReadProjectAction,
                        ProjectAuthorizationGate.ListProjectsAction,
                        ProjectAuthorizationGate.UpdateProjectSetupAction,
                        ProjectAuthorizationGate.ArchiveProjectAction,
                        ProjectAuthorizationGate.SetProjectFolderAction,
                        ProjectAuthorizationGate.LinkFileReferenceAction,
                        ProjectAuthorizationGate.UnlinkFileReferenceAction,
                        ProjectAuthorizationGate.LinkMemoryAction,
                        ProjectAuthorizationGate.UnlinkMemoryAction,
                    ]);
    }

    private sealed class FixedProjectFolderDirectory(ProjectFolderValidationOutcome outcome) : IProjectFolderDirectory
    {
        public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(
            Hexalith.Projects.Contracts.Identifiers.ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectFolderValidationResult(outcome, correlationId));

        public Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(
            Hexalith.Projects.Contracts.Identifiers.ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectFolderValidationResult(ProjectFolderValidationOutcome.Unavailable, correlationId));
    }

    private sealed class TrackingProjectFolderDirectory(ProjectFolderValidationOutcome outcome) : IProjectFolderDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(
            Hexalith.Projects.Contracts.Identifiers.ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectFolderValidationResult(outcome, correlationId));
        }

        public Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(
            Hexalith.Projects.Contracts.Identifiers.ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectFolderValidationResult(ProjectFolderValidationOutcome.Unavailable, correlationId));
    }

    private sealed class TrackingProjectFileReferenceDirectory(ProjectFileReferenceValidationOutcome outcome) : IProjectFileReferenceDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectFileReferenceValidationResult> ValidateLinkFileReferenceAsync(
            Hexalith.Projects.Contracts.Identifiers.ProjectId projectId,
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
            Hexalith.Projects.Contracts.Identifiers.ProjectId projectId,
            string fileReferenceId,
            string folderId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectFileReferenceValidationResult(ProjectFileReferenceValidationOutcome.Unavailable, correlationId));
    }

    private sealed class TrackingProjectMemoryDirectory(ProjectMemoryValidationOutcome outcome) : IProjectMemoryDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectMemoryValidationResult> ValidateLinkMemoryReferenceAsync(
            Hexalith.Projects.Contracts.Identifiers.ProjectId projectId,
            string memoryReferenceId,
            string tenantId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectMemoryValidationResult(outcome, correlationId));
        }

        public Task<ProjectMemoryValidationResult> RefreshMemoryReferenceAsync(
            Hexalith.Projects.Contracts.Identifiers.ProjectId projectId,
            string memoryReferenceId,
            string tenantId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectMemoryValidationResult(ProjectMemoryValidationOutcome.Unavailable, correlationId));
    }

    private sealed class ThrowingProjectDetailReadModel : IProjectDetailReadModel
    {
        public Task<ProjectDetailItem?> GetAsync(string authoritativeTenantId, string projectId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Synthetic projection unavailable.");
    }

    private sealed class ThrowingProjectListReadModel : IProjectListReadModel
    {
        public Task<IReadOnlyList<ProjectListItem>> ListAsync(
            string authoritativeTenantId,
            ProjectLifecycle? lifecycleFilter,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Synthetic projection unavailable.");
    }
}
