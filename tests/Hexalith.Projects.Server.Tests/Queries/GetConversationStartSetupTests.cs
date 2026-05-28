// <copyright file="GetConversationStartSetupTests.cs" company="Hexalith">
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
/// Story 3.5 Tier-2 endpoint tests for
/// <c>GET /api/v1/projects/{projectId}/setup/conversation-start</c>.
/// Exercises the host composition end-to-end: authorize, invoke the Story 3.1
/// <c>ProjectContextInclusionPolicy</c> with empty references-evidence (FR-20 fast-path), project
/// <c>ProjectContext.Setup</c> to the bounded <see cref="ConversationStartSetup"/> wire body via
/// the Story 3.5 <c>ConversationStartSetupProjector</c>. Covers the AC 8 outer-collapse matrix
/// cells, the AC 9 Story-3.5-specific contract (wire-shape invariants + the by-construction
/// no-sibling-ACL assertion), AC 10 cross-tenant isolation, and the AC 17 negative-path matrix.
/// </summary>
public sealed class GetConversationStartSetupTests
{
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string CorrelationIdValue = "corr_01HZ9K8YQ3W6V2N4R7T5P0X1XX";

    [Fact]
    public async Task GetConversationStartSetup_HappyPath_Returns200WithBoundedSubset()
    {
        ProjectSetup setup = new(
            Goals: new[] { "keep continuity current", "summarize key risks" },
            UserInstructions: new[] { "use safe project references" },
            PreferredSourceKinds: new[] { ProjectContextSourceKind.Conversation, ProjectContextSourceKind.Memory },
            ExcludedSourceKinds: new[] { ProjectContextSourceKind.FileReference },
            ConversationStartDefaults: new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences));
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: setup).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(CorrelationIdValue);
            response.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");

            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("projectId").GetString().ShouldBe(ProjectIdValue);
            document.RootElement.GetProperty("lifecycle").GetString().ShouldBe("Active");
            document.RootElement.GetProperty("freshness").GetString().ShouldBe("Fresh");
            document.RootElement.GetProperty("linkedSourcePolicy").GetString().ShouldBe("authorizedReferences");
            document.RootElement.GetProperty("goals").EnumerateArray().Select(e => e.GetString()).ShouldBe(new[] { "keep continuity current", "summarize key risks" });
            document.RootElement.GetProperty("userInstructions").EnumerateArray().Select(e => e.GetString()).ShouldBe(new[] { "use safe project references" });
            document.RootElement.GetProperty("preferredSourceKinds").EnumerateArray().Select(e => e.GetString()).ShouldBe(new[] { "conversation", "memory" });
            document.RootElement.GetProperty("excludedSourceKinds").EnumerateArray().Select(e => e.GetString()).ShouldBe(new[] { "fileReference" });
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_NullSetup_ReturnsEmptyBoundedSubset()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("goals").GetArrayLength().ShouldBe(0);
            document.RootElement.GetProperty("userInstructions").GetArrayLength().ShouldBe(0);
            document.RootElement.GetProperty("preferredSourceKinds").GetArrayLength().ShouldBe(0);
            document.RootElement.GetProperty("excludedSourceKinds").GetArrayLength().ShouldBe(0);
            document.RootElement.GetProperty("linkedSourcePolicy").GetString().ShouldBe("none");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_ArchivedProject_Returns200WithLifecycleArchived()
    {
        ProjectSetup setup = new(
            Goals: new[] { "post-archival audit goal" },
            UserInstructions: new[] { "post-archival instruction" },
            PreferredSourceKinds: new[] { ProjectContextSourceKind.Conversation },
            ExcludedSourceKinds: Array.Empty<ProjectContextSourceKind>(),
            ConversationStartDefaults: new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences));
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            setup: setup,
            lifecycle: ProjectLifecycle.Archived).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("lifecycle").GetString().ShouldBe("Archived");
            document.RootElement.GetProperty("goals").EnumerateArray().Select(e => e.GetString()).ShouldBe(new[] { "post-archival audit goal" });
            document.RootElement.GetProperty("userInstructions").EnumerateArray().Select(e => e.GetString()).ShouldBe(new[] { "post-archival instruction" });
            document.RootElement.GetProperty("linkedSourcePolicy").GetString().ShouldBe("authorizedReferences");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_ConversationStartDefaultsMissing_DefaultsToLinkedSourcePolicyNone()
    {
        ProjectSetup setup = new(
            Goals: new[] { "g" },
            UserInstructions: new[] { "ui" },
            PreferredSourceKinds: Array.Empty<ProjectContextSourceKind>(),
            ExcludedSourceKinds: Array.Empty<ProjectContextSourceKind>(),
            ConversationStartDefaults: null);
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: setup).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("linkedSourcePolicy").GetString().ShouldBe("none");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_PreferredAndExcludedSourceKinds_PreserveOrder()
    {
        // Distinctive ordering so any sort would surface as a mismatch.
        ProjectSetup setup = new(
            Goals: Array.Empty<string>(),
            UserInstructions: Array.Empty<string>(),
            PreferredSourceKinds: new[] { ProjectContextSourceKind.Memory, ProjectContextSourceKind.Conversation, ProjectContextSourceKind.ProjectFolder },
            ExcludedSourceKinds: new[] { ProjectContextSourceKind.FileReference, ProjectContextSourceKind.Memory },
            ConversationStartDefaults: null);
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: setup).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("preferredSourceKinds").EnumerateArray().Select(e => e.GetString()).ShouldBe(new[] { "memory", "conversation", "projectFolder" });
            document.RootElement.GetProperty("excludedSourceKinds").EnumerateArray().Select(e => e.GetString()).ShouldBe(new[] { "fileReference", "memory" });
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_IdempotencyKeyPresent_ReturnsValidationProblem()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = StartSetupRequest();
            request.Headers.Add("Idempotency-Key", "idem-should-be-rejected");
            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("category").GetString().ShouldBe("validation_error");
            document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("idempotency_key");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404()
    {
        // Tenant-access never authorized: Idempotency-Key validation must NOT happen before authz.
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null, seedTenantAccess: false).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = StartSetupRequest();
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
    public async Task GetConversationStartSetup_StricterFreshnessRequested_ReturnsValidationProblem()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = StartSetupRequest();
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
    public async Task GetConversationStartSetup_MalformedProjectId_ReturnsSafeDenial404(string malformedId)
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            string encoded = Uri.EscapeDataString(malformedId);
            HttpResponseMessage response = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, $"/api/v1/projects/{encoded}/setup/conversation-start"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_CrossTenant_ReturnsSafeDenial404()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            setup: null,
            projectTenant: "tenant-b").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            body.ShouldNotContain("tenant-b");
            body.ShouldNotContain("\"projectId\":");
            body.ShouldNotContain("\"lifecycle\":");
            body.ShouldNotContain("\"linkedSourcePolicy\":");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_TenantAccessUnavailable_ReturnsReadModelUnavailable503()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            setup: null,
            tenantAccessStoreOverride: new ThrowingTenantAccessProjectionStore()).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
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
    public async Task GetConversationStartSetup_AuthoritativeTenantIdMissing_ReturnsSafeDenial404()
    {
        WebApplication app = await StartAppAsync(tenantId: null, principalId: null, setup: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            body.ShouldNotContain("tenant-a");
            body.ShouldNotContain("principal-a");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_ResponseHeaders_HaveCorrelationAndFreshness()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(CorrelationIdValue);
            response.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_ExtraQueryParameters_AreIgnoredNotFailed()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                StartSetupRequest($"/api/v1/projects/{ProjectIdValue}/setup/conversation-start?expand=full&unknown=ignored"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_BodyDoesNotContainTenantId()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            body.ShouldNotContain("tenantId");
            body.ShouldNotContain("TenantId");
            body.ShouldNotContain("tenant-a");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_BodyDoesNotContainAuditMetadata()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            body.ShouldNotContain("createdAt");
            body.ShouldNotContain("updatedAt");
            body.ShouldNotContain("sequence");
            body.ShouldNotContain("setupMetadata");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_BodyDoesNotContainReferenceInventory()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            // Assert absence of reference-inventory JSON fields by parsing — substring checks would
            // false-positive on the legitimate `excludedSourceKinds` member (which contains "excluded").
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.TryGetProperty("projectFolder", out _).ShouldBeFalse();
            document.RootElement.TryGetProperty("fileReferences", out _).ShouldBeFalse();
            document.RootElement.TryGetProperty("memoryReferences", out _).ShouldBeFalse();
            document.RootElement.TryGetProperty("conversations", out _).ShouldBeFalse();
            document.RootElement.TryGetProperty("excluded", out _).ShouldBeFalse();
            document.RootElement.TryGetProperty("assemblyOutcome", out _).ShouldBeFalse();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_DoesNotCallSiblingAcls()
    {
        // Story 3.5 FR-20 by-construction guarantee: the handler signature has no sibling-ACL
        // dependencies, so no call can happen. This test registers Recording* directories anyway so a
        // future code-path that silently re-injects a sibling will fail this test.
        RecordingConversationDirectory recordingConversations = new();
        RecordingFolderDirectory recordingFolder = new();
        RecordingFileReferenceDirectory recordingFiles = new();
        RecordingMemoryDirectory recordingMemories = new();

        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            setup: null,
            conversationDirectoryOverride: recordingConversations,
            folderDirectoryOverride: recordingFolder,
            fileReferenceDirectoryOverride: recordingFiles,
            memoryDirectoryOverride: recordingMemories).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            recordingConversations.CallCount.ShouldBe(0);
            recordingFolder.CallCount.ShouldBe(0);
            recordingFiles.CallCount.ShouldBe(0);
            recordingMemories.CallCount.ShouldBe(0);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetConversationStartSetup_ResponseBody_HasNoLeakage()
    {
        ProjectSetup populatedSetup = new(
            Goals: new[] { "post-archival audit goal" },
            UserInstructions: new[] { "post-archival instruction" },
            PreferredSourceKinds: new[] { ProjectContextSourceKind.Conversation },
            ExcludedSourceKinds: Array.Empty<ProjectContextSourceKind>(),
            ConversationStartDefaults: new ConversationStartDefaults(LinkedSourcePolicy.AuthorizedReferences));
        foreach ((string label, ProjectSetup? setup, ProjectLifecycle lifecycle) in new (string, ProjectSetup?, ProjectLifecycle)[]
            {
                ("happy", populatedSetup, ProjectLifecycle.Active),
                ("archived", populatedSetup, ProjectLifecycle.Archived),
                ("nullSetup", null, ProjectLifecycle.Active),
            })
        {
            WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: setup, lifecycle: lifecycle).ConfigureAwait(true);
            try
            {
                using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
                HttpResponseMessage response = await client.SendAsync(StartSetupRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
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

    [Fact]
    public async Task GetConversationStartSetup_ErrorResponses_HaveNoLeakage()
    {
        foreach ((string label, Func<HttpRequestMessage> requestFactory, Func<Task<WebApplication>> bootstrap) in new (string, Func<HttpRequestMessage>, Func<Task<WebApplication>>)[]
            {
                ("idempotencyRejected400", () =>
                {
                    HttpRequestMessage req = StartSetupRequest();
                    req.Headers.Add("Idempotency-Key", "idem-x");
                    return req;
                }, () => StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null)),
                ("missingAuthority404", () => StartSetupRequest(), () => StartAppAsync(tenantId: null, principalId: null, setup: null)),
                ("malformedRoute404", () => new HttpRequestMessage(HttpMethod.Get, "/api/v1/projects/%20/setup/conversation-start"), () => StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null)),
                ("tenantAccessUnavailable503", () => StartSetupRequest(), () => StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", setup: null, tenantAccessStoreOverride: new ThrowingTenantAccessProjectionStore())),
            })
        {
            WebApplication app = await bootstrap().ConfigureAwait(true);
            try
            {
                using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
                HttpResponseMessage response = await client.SendAsync(requestFactory(), TestContext.Current.CancellationToken).ConfigureAwait(true);
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

    private static HttpRequestMessage StartSetupRequest(string? url = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url ?? $"/api/v1/projects/{ProjectIdValue}/setup/conversation-start");
        request.Headers.Add("X-Correlation-Id", CorrelationIdValue);
        return request;
    }

    private static async Task<WebApplication> StartAppAsync(
        string? tenantId,
        string? principalId,
        ProjectSetup? setup,
        bool seedTenantAccess = true,
        ProjectLifecycle lifecycle = ProjectLifecycle.Active,
        string? projectTenant = null,
        IProjectTenantAccessProjectionStore? tenantAccessStoreOverride = null,
        IProjectConversationDirectory? conversationDirectoryOverride = null,
        IProjectFolderDirectory? folderDirectoryOverride = null,
        IProjectFileReferenceDirectory? fileReferenceDirectoryOverride = null,
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

        // Sibling directories: required by other endpoints registered on the same host (e.g. Get,
        // Refresh, link/unlink). For Story 3.5's endpoint they MUST NEVER be invoked, which is what
        // the GetConversationStartSetup_DoesNotCallSiblingAcls test asserts.
        if (conversationDirectoryOverride is not null)
        {
            builder.Services.RemoveAll<IProjectConversationDirectory>();
            builder.Services.AddSingleton(conversationDirectoryOverride);
        }

        if (folderDirectoryOverride is not null)
        {
            builder.Services.RemoveAll<IProjectFolderDirectory>();
            builder.Services.AddSingleton(folderDirectoryOverride);
        }

        if (fileReferenceDirectoryOverride is not null)
        {
            builder.Services.RemoveAll<IProjectFileReferenceDirectory>();
            builder.Services.AddSingleton(fileReferenceDirectoryOverride);
        }

        if (memoryDirectoryOverride is not null)
        {
            builder.Services.RemoveAll<IProjectMemoryDirectory>();
            builder.Services.AddSingleton(memoryDirectoryOverride);
        }

        if (tenantAccessStoreOverride is not null)
        {
            builder.Services.RemoveAll<IProjectTenantAccessProjectionStore>();
            builder.Services.AddSingleton(tenantAccessStoreOverride);
        }

        ProjectDetailItem detail = new(
            TenantId: projectTenant ?? tenantId ?? "tenant-a",
            ProjectId: ProjectIdValue,
            Name: "Conversation Start Project",
            Description: "metadata-only conversation start",
            SetupMetadata: null,
            Setup: setup,
            ProjectFolder: null,
            FileReferences: [],
            MemoryReferences: [],
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

    private sealed class ThrowingTenantAccessProjectionStore : IProjectTenantAccessProjectionStore
    {
        public Task<ProjectTenantAccessProjection?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
            => Task.FromException<ProjectTenantAccessProjection?>(new InvalidOperationException("projection store unavailable"));

        public Task SaveAsync(ProjectTenantAccessProjection projection, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class RecordingConversationDirectory : IProjectConversationDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectConversationsPage> ListForProjectAsync(
            ProjectId projectId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            PageRequest page,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(ProjectConversationsPage.Empty(projectId, ProjectConversationTrustSignal.Current));
        }
    }

    private sealed class RecordingFolderDirectory : IProjectFolderDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectFolderValidationResult> ValidateSetProjectFolderAsync(
            ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectFolderValidationResult(ProjectFolderValidationOutcome.Unavailable, correlationId));
        }

        public Task<ProjectFolderValidationResult> RefreshFolderReferenceAsync(
            ProjectId projectId,
            string folderId,
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectFolderValidationResult(ProjectFolderValidationOutcome.Unavailable, correlationId));
        }
    }

    private sealed class RecordingFileReferenceDirectory : IProjectFileReferenceDirectory
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
            return Task.FromResult(new ProjectFileReferenceValidationResult(ProjectFileReferenceValidationOutcome.Unavailable, correlationId));
        }

        public Task<ProjectFileReferenceValidationResult> RefreshFileReferenceAsync(
            ProjectId projectId,
            string fileReferenceId,
            string folderId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectFileReferenceValidationResult(ProjectFileReferenceValidationOutcome.Unavailable, correlationId));
        }
    }

    private sealed class RecordingMemoryDirectory : IProjectMemoryDirectory
    {
        public int CallCount { get; private set; }

        public Task<ProjectMemoryValidationResult> ValidateLinkMemoryReferenceAsync(
            ProjectId projectId,
            string memoryReferenceId,
            string tenantId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectMemoryValidationResult(ProjectMemoryValidationOutcome.Unavailable, correlationId));
        }

        public Task<ProjectMemoryValidationResult> RefreshMemoryReferenceAsync(
            ProjectId projectId,
            string memoryReferenceId,
            string tenantId,
            string correlationId,
            string taskId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ProjectMemoryValidationResult(ProjectMemoryValidationOutcome.Unavailable, correlationId));
        }
    }
}
