// <copyright file="RefreshProjectContextTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ConversationId = Hexalith.Conversations.Contracts.Identifiers.ConversationId;
using ConversationTenantId = Hexalith.Conversations.Contracts.Identifiers.TenantId;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Conversations;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Memories;
using Hexalith.Projects.Testing.Leakage;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Xunit;

/// <summary>
/// Story 3.4 Tier-2 endpoint tests for <c>GET /api/v1/projects/{projectId}/context/refresh</c>.
/// Exercises the host composition end-to-end: authorize, fan out the folder + memory ACL rechecks +
/// the conversation page in parallel, map outcomes to fresh <c>ReferenceState</c> values, invoke
/// the Story 3.1 inclusion policy unchanged, return the assembled <c>ProjectContext</c> wire body.
/// </summary>
/// <remarks>
/// Per the Story 3.4 capability-gate HALT, file references retain projection-stored state (the
/// Folders typed client has no opaque-id-only file-reference read route); file-recovery contract
/// tests are deferred to a follow-up story. Folder + memory recheck recovery / regression contract
/// is fully covered (AC 9 minus the file rows).
/// </remarks>
public sealed class RefreshProjectContextTests
{
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string FolderIdValue = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC";
    private const string FileRefId = "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1";
    private const string MemoryRefId = "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1";
    private const string ConversationIdValue = "conv_01HZ9K8YQ3W6V2N4R7T5P0X1C1";
    private const string CorrelationIdValue = "corr_01HZ9K8YQ3W6V2N4R7T5P0X1XX";

    [Fact]
    public async Task RefreshProjectContext_HappyPath_Returns200WithAssembledContext()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(CorrelationIdValue);
            response.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");

            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("projectId").GetString().ShouldBe(ProjectIdValue);
            document.RootElement.GetProperty("lifecycle").GetString().ShouldBe("Active");
            document.RootElement.GetProperty("assemblyOutcome").GetString().ShouldBe("Assembled");
            document.RootElement.GetProperty("freshness").GetString().ShouldBe("Fresh");
            document.RootElement.GetProperty("projectFolder").GetProperty("referenceId").GetString().ShouldBe(FolderIdValue);
            document.RootElement.TryGetProperty("tenantId", out _).ShouldBeFalse("FS-8/SM-3: tenant authority must never appear on the wire.");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_IdempotencyKeyPresent_ReturnsValidationProblem()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = RefreshContextRequest();
            request.Headers.Add("Idempotency-Key", "idem-should-be-rejected");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
            document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("idempotency_key");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            seedTenantAccess: false,
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = RefreshContextRequest();
            request.Headers.Add("Idempotency-Key", "idem-key-leaked-through-rejection?");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_StricterFreshnessRequested_ReturnsValidationProblem()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = RefreshContextRequest();
            request.Headers.Add("X-Hexalith-Freshness", "strong");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("freshness");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("..%2F..")]
    [InlineData("path/with/slash")]
    [InlineData("bad‎char")]
    public async Task RefreshProjectContext_MalformedProjectId_ReturnsSafeDenial404(string malformedId)
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            string encoded = Uri.EscapeDataString(malformedId);
            HttpResponseMessage response = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, $"/api/v1/projects/{encoded}/context/refresh"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_ExtraQueryParameters_AreIgnoredNotFailed()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                RefreshContextRequest($"/api/v1/projects/{ProjectIdValue}/context/refresh?expand=full&unknown=ignored"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_CrossTenant_ReturnsSafeDenial404_AndNoAclCallWasMade()
    {
        // AC 10 — cross-tenant safe-denial 404 must collapse BEFORE the ACL fan-out runs. The
        // recording directories assert the call counts remain 0 so we never leak tenant-existence
        // evidence to sibling ACLs (FS-2 / FS-8).
        RecordingFolderDirectory folder = new(ProjectFolderValidationOutcome.Accepted);
        RecordingMemoryDirectory memory = new(ProjectMemoryValidationOutcome.Accepted);
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            projectTenant: "tenant-b",
            folderDirectoryOverride: folder,
            memoryDirectoryOverride: memory).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            body.ShouldNotContain("tenant-b");
            folder.CallCount.ShouldBe(0);
            memory.CallCount.ShouldBe(0);
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_ProjectArchived_Returns200WithAllReferencesExcluded()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted,
            lifecycle: ProjectLifecycle.Archived).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("assemblyOutcome").GetString().ShouldBe("Assembled");
            document.RootElement.GetProperty("lifecycle").GetString().ShouldBe("Archived");
            document.RootElement.TryGetProperty("projectFolder", out _).ShouldBeFalse("archived project has no included folder reference (collapsed to excluded).");
            document.RootElement.GetProperty("excluded").GetArrayLength().ShouldBeGreaterThanOrEqualTo(1);
            foreach (JsonElement excluded in document.RootElement.GetProperty("excluded").EnumerateArray())
            {
                excluded.GetProperty("failedCheck").GetString().ShouldBe("ProjectLifecycle");
            }

            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_ConversationsPageUnavailable_AssemblesWithExclusions()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted,
            conversationDirectoryOverride: new UnavailablePageConversationDirectory()).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("assemblyOutcome").GetString().ShouldBe("Assembled");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_TenantAccessUnavailable_ReturnsReadModelUnavailable503()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted,
            tenantAccessStoreOverride: new ThrowingTenantAccessProjectionStore()).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
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
    public async Task RefreshProjectContext_AuthoritativeTenantIdMissing_ReturnsSafeDenial404()
    {
        WebApplication app = await StartAppAsync(
            tenantId: null,
            principalId: null,
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            body.ShouldNotContain("tenant-a");
            body.ShouldNotContain("principal-a");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_ResponseHeaders_HaveCorrelationAndFreshness()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(CorrelationIdValue);
            response.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    // ---- AC 9 recovery / regression contract (folder + memory only; file recheck deferred) ----

    [Fact]
    public async Task RefreshProjectContext_AclReportsAccepted_OverridesProjectionStoredStale_ForMemory()
    {
        // Projection stores ProjectMemoryReference.ReferenceState=Stale; the ACL recheck returns
        // Accepted; the response MemoryReferences list contains the memory as Included; Excluded does
        // NOT contain the memory.
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted,
            includeMemoryReference: true,
            memoryProjectionState: ReferenceState.Stale,
            memoryRecheckOutcome: ProjectMemoryValidationOutcome.Accepted).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("memoryReferences").GetArrayLength().ShouldBe(1);
            document.RootElement.GetProperty("memoryReferences")[0].GetProperty("referenceId").GetString().ShouldBe(MemoryRefId);
            document.RootElement.GetProperty("memoryReferences")[0].GetProperty("referenceState").GetString().ShouldBe("Included");
            foreach (JsonElement excluded in document.RootElement.GetProperty("excluded").EnumerateArray())
            {
                excluded.GetProperty("referenceId").GetString().ShouldNotBe(MemoryRefId);
            }
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_AclReportsArchived_OverridesProjectionStoredIncluded_ForMemory()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted,
            includeMemoryReference: true,
            memoryProjectionState: ReferenceState.Included,
            memoryRecheckOutcome: ProjectMemoryValidationOutcome.Archived).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("memoryReferences").GetArrayLength().ShouldBe(0);
            JsonElement excludedArray = document.RootElement.GetProperty("excluded");
            JsonElement memoryRow = excludedArray.EnumerateArray()
                .First(e => string.Equals(e.GetProperty("referenceKind").GetString(), "memory", StringComparison.Ordinal));
            memoryRow.GetProperty("referenceState").GetString().ShouldBe("Archived");
            memoryRow.GetProperty("failedCheck").GetString().ShouldBe("ReferenceLifecycle");
            memoryRow.GetProperty("diagnostic").GetString().ShouldBe(ProjectContextInclusionDiagnostic.ReferenceArchived);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_AclReportsUnavailable_OverridesProjectionStoredIncluded_ForMemory()
    {
        // Projection stores ProjectMemoryReference.ReferenceState=Included; the ACL recheck returns
        // Unavailable; the memory must collapse to an Excluded row with state=Unavailable, surfacing the
        // referenceUnavailable diagnostic — proving the recheck overrides projection-stored Included.
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted,
            includeMemoryReference: true,
            memoryProjectionState: ReferenceState.Included,
            memoryRecheckOutcome: ProjectMemoryValidationOutcome.Unavailable).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("memoryReferences").GetArrayLength().ShouldBe(0);
            JsonElement memoryRow = document.RootElement.GetProperty("excluded").EnumerateArray()
                .First(e => string.Equals(e.GetProperty("referenceKind").GetString(), "memory", StringComparison.Ordinal));
            memoryRow.GetProperty("referenceState").GetString().ShouldBe("Unavailable");
            memoryRow.GetProperty("diagnostic").GetString().ShouldBe(ProjectContextInclusionDiagnostic.ReferenceUnavailable);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_AclReportsTenantMismatch_OverridesToUnauthorized_ForMemory()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted,
            includeMemoryReference: true,
            memoryProjectionState: ReferenceState.Included,
            memoryRecheckOutcome: ProjectMemoryValidationOutcome.TenantMismatch).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement memoryRow = document.RootElement.GetProperty("excluded").EnumerateArray()
                .First(e => string.Equals(e.GetProperty("referenceKind").GetString(), "memory", StringComparison.Ordinal));
            memoryRow.GetProperty("referenceState").GetString().ShouldBe("Unauthorized");
            memoryRow.GetProperty("failedCheck").GetString().ShouldBe("ReferenceAuthorization");
            memoryRow.GetProperty("diagnostic").GetString().ShouldBe(ProjectContextInclusionDiagnostic.TenantMismatch);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_AclReportsDenied_SurfacesAsUnauthorized_ForFolder()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Denied).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement folderRow = document.RootElement.GetProperty("excluded").EnumerateArray()
                .First(e => string.Equals(e.GetProperty("referenceKind").GetString(), "folder", StringComparison.Ordinal));
            folderRow.GetProperty("referenceState").GetString().ShouldBe("Unauthorized");
            folderRow.GetProperty("failedCheck").GetString().ShouldBe("ReferenceAuthorization");
            folderRow.GetProperty("diagnostic").GetString().ShouldBe(ProjectContextInclusionDiagnostic.ReferenceUnauthorized);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_FolderUnavailableAndProjectionStoredPending_PreservesPendingDiagnostic()
    {
        // AC 3 Folder mapping rule: when outcome=Unavailable AND projection-stored=Pending, preserve
        // Pending so the inclusion policy continues to emit the projectFolderPending diagnostic rather
        // than referenceUnavailable.
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Pending,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Unavailable).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement folderRow = document.RootElement.GetProperty("excluded").EnumerateArray()
                .First(e => string.Equals(e.GetProperty("referenceKind").GetString(), "folder", StringComparison.Ordinal));
            folderRow.GetProperty("referenceState").GetString().ShouldBe("Pending");
            folderRow.GetProperty("diagnostic").GetString().ShouldBe(ProjectContextInclusionDiagnostic.ProjectFolderPending);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_AllAclsReturnAccepted_AndProjectionStoredIncluded_AssembledMatchesGetShape()
    {
        // Equivalence-on-no-drift contract: when the ACL recheck confirms every projection-stored
        // state, the resulting projectFolder + memoryReferences sets should match what GetProjectContext
        // produces for the same projection. We compare structurally (not byte-byte) because Refresh
        // stamps observedAt with the current TimeProvider on state changes — there are none here.
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted,
            includeMemoryReference: true,
            memoryProjectionState: ReferenceState.Included,
            memoryRecheckOutcome: ProjectMemoryValidationOutcome.Accepted).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage refresh = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            HttpResponseMessage get = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}/context"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
            get.StatusCode.ShouldBe(HttpStatusCode.OK);

            string refreshBody = await refresh.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            string getBody = await get.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            using JsonDocument refreshDoc = JsonDocument.Parse(refreshBody);
            using JsonDocument getDoc = JsonDocument.Parse(getBody);

            refreshDoc.RootElement.GetProperty("assemblyOutcome").GetString().ShouldBe(
                getDoc.RootElement.GetProperty("assemblyOutcome").GetString());
            refreshDoc.RootElement.GetProperty("projectFolder").GetProperty("referenceId").GetString().ShouldBe(
                getDoc.RootElement.GetProperty("projectFolder").GetProperty("referenceId").GetString());
            refreshDoc.RootElement.GetProperty("memoryReferences").GetArrayLength().ShouldBe(
                getDoc.RootElement.GetProperty("memoryReferences").GetArrayLength());
            refreshDoc.RootElement.GetProperty("excluded").GetArrayLength().ShouldBe(
                getDoc.RootElement.GetProperty("excluded").GetArrayLength());
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_DeterministicFanOut_PreservesSortByReferenceId()
    {
        // Multiple memories stored in non-sorted order; ACL recheck accepts all; the response is
        // sorted by (ReferenceKind, ReferenceId) Ordinal (the policy enforces this).
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted,
            extraMemoryReferences: new[]
            {
                ("case_zzz_01HZ", ProjectMemoryValidationOutcome.Accepted),
                ("case_aaa_01HZ", ProjectMemoryValidationOutcome.Accepted),
                ("case_mmm_01HZ", ProjectMemoryValidationOutcome.Accepted),
            }).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement memories = document.RootElement.GetProperty("memoryReferences");
            memories.GetArrayLength().ShouldBe(3);
            string[] ids = memories.EnumerateArray()
                .Select(m => m.GetProperty("referenceId").GetString() ?? string.Empty)
                .ToArray();
            ids.ShouldBe(new[] { "case_aaa_01HZ", "case_mmm_01HZ", "case_zzz_01HZ" });
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RefreshProjectContext_ResponseBody_HasNoLeakageAcrossOutcomes()
    {
        foreach ((string label, Func<HttpClient, Task<HttpResponseMessage>> send) in new (string, Func<HttpClient, Task<HttpResponseMessage>>)[]
            {
                ("happy", c => c.SendAsync(RefreshContextRequest(), TestContext.Current.CancellationToken)),
                ("idempotencyRejected", c => c.SendAsync(WithHeader(RefreshContextRequest(), "Idempotency-Key", "idem-x"), TestContext.Current.CancellationToken)),
                ("freshnessRejected", c => c.SendAsync(WithHeader(RefreshContextRequest(), "X-Hexalith-Freshness", "strong"), TestContext.Current.CancellationToken)),
                ("malformedRoute", c => c.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/v1/projects/%20/context/refresh"), TestContext.Current.CancellationToken)),
            })
        {
            WebApplication app = await StartAppAsync(
                tenantId: "tenant-a",
                principalId: "principal-a",
                projectFolderState: ReferenceState.Included,
                folderRecheckOutcome: ProjectFolderValidationOutcome.Accepted).ConfigureAwait(true);
            try
            {
                using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
                HttpResponseMessage response = await send(client).ConfigureAwait(true);
                string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
                Should.NotThrow(
                    () => NoPayloadLeakageAssertions.AssertNoLeakageInText(body),
                    $"label={label} status={response.StatusCode}");
            }
            finally
            {
                await StopAsync(app).ConfigureAwait(true);
            }
        }
    }

    // ---- helpers ----

    private static HttpRequestMessage RefreshContextRequest(string? url = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url ?? $"/api/v1/projects/{ProjectIdValue}/context/refresh");
        request.Headers.Add("X-Correlation-Id", CorrelationIdValue);
        return request;
    }

    private static HttpRequestMessage WithHeader(HttpRequestMessage request, string name, string value)
    {
        request.Headers.Add(name, value);
        return request;
    }

    private static async Task<WebApplication> StartAppAsync(
        string? tenantId,
        string? principalId,
        bool seedTenantAccess = true,
        ProjectLifecycle lifecycle = ProjectLifecycle.Active,
        ReferenceState projectFolderState = ReferenceState.Included,
        ProjectFolderValidationOutcome folderRecheckOutcome = ProjectFolderValidationOutcome.Accepted,
        bool includeMemoryReference = false,
        ReferenceState memoryProjectionState = ReferenceState.Included,
        ProjectMemoryValidationOutcome memoryRecheckOutcome = ProjectMemoryValidationOutcome.Accepted,
        IEnumerable<(string ReferenceId, ProjectMemoryValidationOutcome Outcome)>? extraMemoryReferences = null,
        string? projectTenant = null,
        IProjectConversationDirectory? conversationDirectoryOverride = null,
        IProjectTenantAccessProjectionStore? tenantAccessStoreOverride = null,
        IProjectFolderDirectory? folderDirectoryOverride = null,
        IProjectMemoryDirectory? memoryDirectoryOverride = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.Services.AddProjectsServer();
        builder.Services.RemoveAll<IProjectEventStoreAuthorizationValidator>();
        builder.Services.AddSingleton<IProjectEventStoreAuthorizationValidator, AllowingProjectEventStoreAuthorizationValidator>();
        builder.Services.RemoveAll<IProjectDaprPolicyEvidenceProvider>();
        builder.Services.AddSingleton<IProjectDaprPolicyEvidenceProvider, AllowingProjectDaprPolicyEvidenceProvider>();
        builder.Services.RemoveAll<IProjectTenantContextAccessor>();
        builder.Services.AddSingleton<IProjectTenantContextAccessor>(new FixedProjectTenantContext(tenantId, principalId));
        builder.Services.AddSingleton<IProjectCommandSubmitter>(new NoopProjectCommandSubmitter());
        builder.Services.RemoveAll<IProjectConversationDirectory>();
        builder.Services.AddSingleton(
            conversationDirectoryOverride ?? new StubConversationDirectory(includeOneConversation: true));

        builder.Services.RemoveAll<IProjectFolderDirectory>();
        builder.Services.AddSingleton(folderDirectoryOverride ?? new StubFolderDirectory(folderRecheckOutcome));
        builder.Services.RemoveAll<IProjectMemoryDirectory>();
        Dictionary<string, ProjectMemoryValidationOutcome> memoryOutcomes = [];
        if (includeMemoryReference)
        {
            memoryOutcomes[MemoryRefId] = memoryRecheckOutcome;
        }

        if (extraMemoryReferences is not null)
        {
            foreach ((string id, ProjectMemoryValidationOutcome outcome) in extraMemoryReferences)
            {
                memoryOutcomes[id] = outcome;
            }
        }

        builder.Services.AddSingleton(memoryDirectoryOverride ?? new StubMemoryDirectory(memoryOutcomes, defaultOutcome: ProjectMemoryValidationOutcome.Accepted));

        if (tenantAccessStoreOverride is not null)
        {
            builder.Services.RemoveAll<IProjectTenantAccessProjectionStore>();
            builder.Services.AddSingleton(tenantAccessStoreOverride);
        }

        List<ProjectMemoryReference> memories = [];
        if (includeMemoryReference)
        {
            memories.Add(new ProjectMemoryReference(
                MemoryReferenceId: MemoryRefId,
                DisplayName: "Memory",
                ReferenceState: memoryProjectionState,
                ReasonCode: null,
                ObservedAt: DateTimeOffset.UnixEpoch));
        }

        if (extraMemoryReferences is not null)
        {
            foreach ((string id, _) in extraMemoryReferences)
            {
                memories.Add(new ProjectMemoryReference(
                    MemoryReferenceId: id,
                    DisplayName: "Memory",
                    ReferenceState: ReferenceState.Included,
                    ReasonCode: null,
                    ObservedAt: DateTimeOffset.UnixEpoch));
            }
        }

        ProjectDetailItem detail = new(
            TenantId: projectTenant ?? tenantId ?? "tenant-a",
            ProjectId: ProjectIdValue,
            Name: "Refresh Project",
            Description: "metadata-only context refresh",
            SetupMetadata: "metadata",
            Setup: null,
            ProjectFolder: new ProjectFolderReference(
                FolderId: projectFolderState == ReferenceState.Pending ? null : FolderIdValue,
                DisplayName: "Context Folder",
                ReferenceState: projectFolderState,
                ReasonCode: null,
                ObservedAt: DateTimeOffset.UnixEpoch),
            FileReferences: [],
            MemoryReferences: memories,
            Lifecycle: lifecycle,
            CreatedAt: DateTimeOffset.UnixEpoch,
            UpdatedAt: DateTimeOffset.UnixEpoch,
            Sequence: 1);
        builder.Services.RemoveAll<IProjectDetailReadModel>();
        builder.Services.AddSingleton<IProjectDetailReadModel>(new StubProjectDetailReadModel(detail));

        WebApplication app = builder.Build();
        if (seedTenantAccess && !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(principalId))
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
            await app.Services.GetRequiredService<IProjectTenantAccessProjectionStore>().SaveAsync(projection, TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        app.MapProjectsServerEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        return app;
    }

    private static async Task StopAsync(WebApplication app)
    {
        await app.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await app.DisposeAsync().ConfigureAwait(true);
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
                        ProjectAuthorizationGate.ReadProjectAction,
                    ]);
    }

    private sealed class NoopProjectCommandSubmitter : IProjectCommandSubmitter
    {
        public Task<ProjectCommandSubmissionResult> SubmitCreateProjectAsync(CreateProject command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitUpdateProjectSetupAsync(UpdateProjectSetup command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitArchiveProjectAsync(ArchiveProject command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitSetProjectFolderAsync(SetProjectFolder command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitLinkFileReferenceAsync(LinkFileReference command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkFileReferenceAsync(UnlinkFileReference command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitLinkMemoryAsync(LinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));

        public Task<ProjectCommandSubmissionResult> SubmitUnlinkMemoryAsync(UnlinkMemory command, CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectCommandSubmissionResult.Accepted("corr", false));
    }

    private sealed class StubConversationDirectory(bool includeOneConversation) : IProjectConversationDirectory
    {
        public Task<ProjectConversationsPage> ListForProjectAsync(
            ProjectId projectId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            PageRequest page,
            CancellationToken cancellationToken = default)
        {
            if (!includeOneConversation)
            {
                return Task.FromResult(ProjectConversationsPage.Empty(projectId, ProjectConversationTrustSignal.Current));
            }

            ProjectConversationItem item = new(
                projectId,
                new ConversationId(ConversationIdValue),
                "active",
                "Context conversation",
                ProjectConversationTrustSignal.Current,
                null,
                null);
            return Task.FromResult(new ProjectConversationsPage(
                projectId,
                [item],
                new ProjectConversationPageMetadata(1),
                ProjectConversationTrustSignal.Current));
        }
    }

    private sealed class StubProjectDetailReadModel(ProjectDetailItem detail) : IProjectDetailReadModel
    {
        public Task<ProjectDetailItem?> GetAsync(string authoritativeTenantId, string projectId, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(detail.TenantId, authoritativeTenantId, StringComparison.Ordinal)
                || !string.Equals(detail.ProjectId, projectId, StringComparison.Ordinal))
            {
                return Task.FromResult<ProjectDetailItem?>(null);
            }

            return Task.FromResult<ProjectDetailItem?>(detail);
        }
    }

    private sealed class UnavailablePageConversationDirectory : IProjectConversationDirectory
    {
        public Task<ProjectConversationsPage> ListForProjectAsync(
            ProjectId projectId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            PageRequest page,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ProjectConversationsPage.Empty(projectId, ProjectConversationTrustSignal.Unavailable));
    }

    private sealed class ThrowingTenantAccessProjectionStore : IProjectTenantAccessProjectionStore
    {
        public Task<ProjectTenantAccessProjection?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
            => Task.FromException<ProjectTenantAccessProjection?>(new InvalidOperationException("projection store unavailable"));

        public Task SaveAsync(ProjectTenantAccessProjection projection, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubFolderDirectory(ProjectFolderValidationOutcome outcome) : IProjectFolderDirectory
    {
        public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(
            ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectFolderValidationResult(ProjectFolderValidationOutcome.Unavailable, correlationId));

        public Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(
            ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectFolderValidationResult(outcome, correlationId));
    }

    private sealed class StubMemoryDirectory(
        IReadOnlyDictionary<string, ProjectMemoryValidationOutcome> outcomesById,
        ProjectMemoryValidationOutcome defaultOutcome) : IProjectMemoryDirectory
    {
        public Task<ProjectMemoryValidationResult> ValidateLinkMemoryReferenceAsync(
            ProjectId projectId,
            string memoryReferenceId,
            string tenantId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectMemoryValidationResult(ProjectMemoryValidationOutcome.Unavailable, correlationId));

        public Task<ProjectMemoryValidationResult> RefreshMemoryReferenceAsync(
            ProjectId projectId,
            string memoryReferenceId,
            string tenantId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
        {
            ProjectMemoryValidationOutcome outcome = outcomesById.TryGetValue(memoryReferenceId, out ProjectMemoryValidationOutcome found)
                ? found
                : defaultOutcome;
            return Task.FromResult(new ProjectMemoryValidationResult(outcome, correlationId));
        }
    }

    private sealed class RecordingFolderDirectory(ProjectFolderValidationOutcome outcome) : IProjectFolderDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(
            ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectFolderValidationResult(ProjectFolderValidationOutcome.Unavailable, correlationId));

        public Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(
            ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectFolderValidationResult(outcome, correlationId));
        }
    }

    private sealed class RecordingMemoryDirectory(ProjectMemoryValidationOutcome outcome) : IProjectMemoryDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectMemoryValidationResult> ValidateLinkMemoryReferenceAsync(
            ProjectId projectId,
            string memoryReferenceId,
            string tenantId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectMemoryValidationResult(ProjectMemoryValidationOutcome.Unavailable, correlationId));

        public Task<ProjectMemoryValidationResult> RefreshMemoryReferenceAsync(
            ProjectId projectId,
            string memoryReferenceId,
            string tenantId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectMemoryValidationResult(outcome, correlationId));
        }
    }
}
