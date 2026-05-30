// <copyright file="GetProjectContextExplanationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System;
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
using Hexalith.Projects.Testing.Leakage;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Xunit;

/// <summary>
/// Story 3.3 Tier-2 endpoint tests for <c>GET /api/v1/projects/{projectId}/context/explain</c>.
/// Mirrors Story 3.2's <c>GetProjectContextTests</c> shape with the additional coverage Story 3.3
/// owns: assertions over the <c>ProjectContextExplanation.Evaluations</c> array (count, deterministic
/// sort, closed-vocabulary diagnostics, per-fixture verdict shape), plus FS-2 leakage coverage of
/// the full wire body including evaluations.
/// </summary>
public sealed class GetProjectContextExplanationTests
{
    private const string ProjectIdValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";
    private const string FolderIdValue = "folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC";
    private const string FileRefId = "file_01HZ9K8YQ3W6V2N4R7T5P0X1F1";
    private const string MemoryRefId = "case_01HZ9K8YQ3W6V2N4R7T5P0X1M1";
    private const string ConversationIdValue = "conv_01HZ9K8YQ3W6V2N4R7T5P0X1C1";
    private const string CorrelationIdValue = "corr_01HZ9K8YQ3W6V2N4R7T5P0X1XX";

    [Fact]
    public async Task GetProjectContextExplanation_HappyPath_Returns200WithAssembledContextAndEvaluations()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            seedFileReference: true,
            seedMemoryReference: true).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
            response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe(CorrelationIdValue);
            response.Headers.GetValues("X-Hexalith-Freshness").Single().ShouldBe("eventually_consistent");

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement context = document.RootElement.GetProperty("context");
            context.GetProperty("projectId").GetString().ShouldBe(ProjectIdValue);
            context.GetProperty("lifecycle").GetString().ShouldBe("Active");
            context.GetProperty("assemblyOutcome").GetString().ShouldBe("Assembled");
            context.GetProperty("freshness").GetString().ShouldBe("Fresh");
            context.GetProperty("projectFolder").GetProperty("referenceId").GetString().ShouldBe(FolderIdValue);
            context.TryGetProperty("tenantId", out _).ShouldBeFalse("FS-8/SM-3: tenant authority must never appear on the wire.");

            JsonElement evaluations = document.RootElement.GetProperty("evaluations");
            // 1 folder + 1 file + 1 memory + 1 conversation = 4 candidate rows.
            evaluations.GetArrayLength().ShouldBe(4);
            foreach (JsonElement row in evaluations.EnumerateArray())
            {
                row.GetProperty("resultState").GetString().ShouldBe("Included");
                // Null fields are omitted by the canonical response options (WhenWritingNull). The
                // closed-vocab contract on the wire is therefore: failedCheck is absent OR equals one
                // of the seven known check names (structurally enforced by ProjectContextEvaluation).
                row.TryGetProperty("failedCheck", out JsonElement failed).ShouldBeFalse();
            }

            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContextExplanation_EvaluationsAreDeterministicallySorted()
    {
        // Two files with ids that sort differently under invariant culture vs ordinal: `_` (0x5F) <
        // letters in Ordinal but typically > letters in invariant culture.
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            extraFileIds: ["file_b", "file_Z", "file_a"]).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement evaluations = document.RootElement.GetProperty("evaluations");
            string[] keys = evaluations
                .EnumerateArray()
                .Select(static e => $"{e.GetProperty("referenceKind").GetString()}::{e.GetProperty("referenceId").GetString()}")
                .ToArray();

            string[] expected = keys.OrderBy(static k => k, StringComparer.Ordinal).ToArray();
            keys.ShouldBe(expected);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContextExplanation_ArchivedProject_Returns200WithEvaluationsMarkingProjectLifecycleFailure()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            lifecycle: ProjectLifecycle.Archived,
            seedFileReference: true).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("context").GetProperty("lifecycle").GetString().ShouldBe("Archived");

            JsonElement evaluations = document.RootElement.GetProperty("evaluations");
            evaluations.GetArrayLength().ShouldBeGreaterThanOrEqualTo(1);
            foreach (JsonElement row in evaluations.EnumerateArray())
            {
                row.GetProperty("failedCheck").GetString().ShouldBe("ProjectLifecycle");
                row.GetProperty("diagnostic").GetString().ShouldBe("projectArchived");
            }

            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContextExplanation_ForbiddenConversation_HasEvaluationWithReferenceAuthorizationCheck()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            conversationDirectoryOverride: new ForbiddenConversationDirectory()).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement evaluations = document.RootElement.GetProperty("evaluations");
            JsonElement conversationRow = evaluations
                .EnumerateArray()
                .Single(static e => e.GetProperty("referenceKind").GetString() == "conversation");
            conversationRow.GetProperty("failedCheck").GetString().ShouldBe("ReferenceAuthorization");
            conversationRow.GetProperty("diagnostic").GetString().ShouldBe("referenceUnauthorized");
            conversationRow.GetProperty("resultState").GetString().ShouldBe("Unauthorized");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContextExplanation_StaleFileReference_HasEvaluationWithReferenceFreshnessCheck()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            seedFileReference: true,
            fileReferenceState: ReferenceState.Stale).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement evaluations = document.RootElement.GetProperty("evaluations");
            JsonElement fileRow = evaluations
                .EnumerateArray()
                .Single(static e => e.GetProperty("referenceKind").GetString() == "file");
            fileRow.GetProperty("failedCheck").GetString().ShouldBe("ReferenceFreshness");
            fileRow.GetProperty("diagnostic").GetString().ShouldBe("referenceStale");
            fileRow.GetProperty("resultState").GetString().ShouldBe("Stale");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContextExplanation_ArchivedMemoryReference_HasEvaluationWithReferenceLifecycleCheck()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            seedMemoryReference: true,
            memoryReferenceState: ReferenceState.Archived).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement evaluations = document.RootElement.GetProperty("evaluations");
            JsonElement memoryRow = evaluations
                .EnumerateArray()
                .Single(static e => e.GetProperty("referenceKind").GetString() == "memory");
            memoryRow.GetProperty("failedCheck").GetString().ShouldBe("ReferenceLifecycle");
            memoryRow.GetProperty("diagnostic").GetString().ShouldBe("referenceArchived");
            memoryRow.GetProperty("resultState").GetString().ShouldBe("Archived");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContextExplanation_IdempotencyKeyPresent_ReturnsValidationProblem()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = GetExplainRequest();
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
    public async Task GetProjectContextExplanation_IdempotencyKeyPresentAndUnauthorized_ReturnsSafeDenial404()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            seedTenantAccess: false,
            projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = GetExplainRequest();
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
    public async Task GetProjectContextExplanation_StricterFreshnessRequested_ReturnsValidationProblem()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpRequestMessage request = GetExplainRequest();
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
    public async Task GetProjectContextExplanation_MalformedProjectId_ReturnsSafeDenial404(string malformedId)
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            string encoded = Uri.EscapeDataString(malformedId);
            HttpResponseMessage response = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, $"/api/v1/projects/{encoded}/context/explain"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContextExplanation_CrossTenant_ReturnsSafeDenial404()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            projectTenant: "tenant-b").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            body.ShouldNotContain("tenant-b");
            Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContextExplanation_TenantAccessUnavailable_ReturnsReadModelUnavailable503()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            tenantAccessStoreOverride: new ThrowingTenantAccessProjectionStore()).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
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
    public async Task GetProjectContextExplanation_AuthoritativeTenantIdMissing_ReturnsSafeDenial404()
    {
        WebApplication app = await StartAppAsync(
            tenantId: null,
            principalId: null,
            projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
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
    public async Task GetProjectContextExplanation_ConversationsPageUnavailable_AssemblesWithExclusions()
    {
        WebApplication app = await StartAppAsync(
            tenantId: "tenant-a",
            principalId: "principal-a",
            projectFolderState: ReferenceState.Included,
            conversationDirectoryOverride: new UnavailablePageConversationDirectory()).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("context").GetProperty("assemblyOutcome").GetString().ShouldBe("Assembled");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContextExplanation_ResponseHeaders_HaveCorrelationAndFreshness()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);

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
    public async Task GetProjectContextExplanation_ExtraQueryParameters_AreIgnoredNotFailed()
    {
        WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HttpResponseMessage response = await client.SendAsync(
                GetExplainRequest($"/api/v1/projects/{ProjectIdValue}/context/explain?expand=full&unknown=ignored"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GetProjectContextExplanation_ResponseBody_HasNoLeakageAcrossOutcomes()
    {
        // Composite leakage check across every outcome including the new evaluations array. This is
        // the new endpoint-response coverage Story 3.3 owns; Story 3.2's matching test did not iterate
        // over the evaluations property.
        (string Label, Func<StartAppOptions> Options, Func<HttpClient, Task<HttpResponseMessage>> Send)[] cases =
        [
            ("happy", () => new StartAppOptions(SeedFileReference: true, SeedMemoryReference: true),
             c => c.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken)),
            ("archivedProject", () => new StartAppOptions(SeedFileReference: true, Lifecycle: ProjectLifecycle.Archived),
             c => c.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken)),
            ("staleReferences", () => new StartAppOptions(SeedFileReference: true, FileReferenceState: ReferenceState.Stale),
             c => c.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken)),
            ("forbiddenConversation", () => new StartAppOptions(ConversationDirectoryOverride: new ForbiddenConversationDirectory()),
             c => c.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken)),
        ];

        foreach ((string label, Func<StartAppOptions> options, Func<HttpClient, Task<HttpResponseMessage>> send) in cases)
        {
            StartAppOptions opts = options();
            WebApplication app = await StartAppAsync(
                tenantId: "tenant-a",
                principalId: "principal-a",
                projectFolderState: ReferenceState.Included,
                lifecycle: opts.Lifecycle,
                seedFileReference: opts.SeedFileReference,
                seedMemoryReference: opts.SeedMemoryReference,
                fileReferenceState: opts.FileReferenceState,
                conversationDirectoryOverride: opts.ConversationDirectoryOverride).ConfigureAwait(true);
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

    [Fact]
    public async Task GetProjectContextExplanation_ErrorResponses_HaveNoLeakage()
    {
        (string Label, Func<HttpClient, Task<HttpResponseMessage>> Send)[] cases =
        [
            ("validation_idempotency", c => c.SendAsync(WithHeader(GetExplainRequest(), "Idempotency-Key", "idem-x"), TestContext.Current.CancellationToken)),
            ("validation_freshness", c => c.SendAsync(WithHeader(GetExplainRequest(), "X-Hexalith-Freshness", "strong"), TestContext.Current.CancellationToken)),
            ("safe_denial_malformed", c => c.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/v1/projects/%20/context/explain"), TestContext.Current.CancellationToken)),
        ];

        foreach ((string label, Func<HttpClient, Task<HttpResponseMessage>> send) in cases)
        {
            WebApplication app = await StartAppAsync(tenantId: "tenant-a", principalId: "principal-a", projectFolderState: ReferenceState.Included).ConfigureAwait(true);
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

    [Fact]
    public async Task GetProjectContextExplanation_EvaluationDiagnostics_AllInClosedVocabulary()
    {
        // Iterate through several seeded fixtures and confirm every diagnostic on every evaluation
        // row is null OR a member of the closed ProjectContextInclusionDiagnostic vocabulary.
        Action<StartAppOptions>[] mutators =
        [
            o => { o.SeedFileReference = true; o.SeedMemoryReference = true; },
            o => { o.Lifecycle = ProjectLifecycle.Archived; o.SeedFileReference = true; },
            o => { o.SeedFileReference = true; o.FileReferenceState = ReferenceState.Stale; },
            o => { o.SeedMemoryReference = true; o.MemoryReferenceState = ReferenceState.Archived; },
            o => { o.ConversationDirectoryOverride = new ForbiddenConversationDirectory(); },
        ];

        foreach (Action<StartAppOptions> mutate in mutators)
        {
            StartAppOptions opts = new();
            mutate(opts);
            WebApplication app = await StartAppAsync(
                tenantId: "tenant-a",
                principalId: "principal-a",
                projectFolderState: ReferenceState.Included,
                lifecycle: opts.Lifecycle,
                seedFileReference: opts.SeedFileReference,
                seedMemoryReference: opts.SeedMemoryReference,
                fileReferenceState: opts.FileReferenceState,
                memoryReferenceState: opts.MemoryReferenceState,
                conversationDirectoryOverride: opts.ConversationDirectoryOverride).ConfigureAwait(true);
            try
            {
                using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
                HttpResponseMessage response = await client.SendAsync(GetExplainRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true);
                string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                using JsonDocument document = JsonDocument.Parse(body);
                foreach (JsonElement row in document.RootElement.GetProperty("evaluations").EnumerateArray())
                {
                    string? diagnostic = row.TryGetProperty("diagnostic", out JsonElement d) && d.ValueKind == JsonValueKind.String
                        ? d.GetString()
                        : null;
                    ProjectContextInclusionDiagnostic.IsKnown(diagnostic).ShouldBeTrue($"diagnostic={diagnostic ?? "null"}");
                }
            }
            finally
            {
                await StopAsync(app).ConfigureAwait(true);
            }
        }
    }

    private static HttpRequestMessage GetExplainRequest(string? url = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url ?? $"/api/v1/projects/{ProjectIdValue}/context/explain");
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
        string? projectTenant = null,
        IProjectConversationDirectory? conversationDirectoryOverride = null,
        IProjectTenantAccessProjectionStore? tenantAccessStoreOverride = null,
        bool seedFileReference = false,
        ReferenceState fileReferenceState = ReferenceState.Included,
        bool seedMemoryReference = false,
        ReferenceState memoryReferenceState = ReferenceState.Included,
        string[]? extraFileIds = null)
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
        if (tenantAccessStoreOverride is not null)
        {
            builder.Services.RemoveAll<IProjectTenantAccessProjectionStore>();
            builder.Services.AddSingleton(tenantAccessStoreOverride);
        }

        ProjectFileReference[] fileReferences = [];
        if (seedFileReference)
        {
            fileReferences =
            [
                new ProjectFileReference(
                    FileReferenceId: FileRefId,
                    FolderId: FolderIdValue,
                    DisplayName: "context-note.md",
                    ReferenceState: fileReferenceState,
                    ReasonCode: null,
                    ObservedAt: DateTimeOffset.UnixEpoch),
            ];
        }

        if (extraFileIds is not null && extraFileIds.Length > 0)
        {
            fileReferences =
            [
                ..fileReferences,
                ..extraFileIds.Select(static id => new ProjectFileReference(
                    FileReferenceId: id,
                    FolderId: FolderIdValue,
                    DisplayName: "sort-fixture.md",
                    ReferenceState: ReferenceState.Included,
                    ReasonCode: null,
                    ObservedAt: DateTimeOffset.UnixEpoch)),
            ];
        }

        ProjectMemoryReference[] memoryReferences = seedMemoryReference
            ?
            [
                new ProjectMemoryReference(
                    MemoryReferenceId: MemoryRefId,
                    DisplayName: "context-case",
                    ReferenceState: memoryReferenceState,
                    ReasonCode: null,
                    ObservedAt: DateTimeOffset.UnixEpoch),
            ]
            : [];

        ProjectDetailItem detail = new(
            TenantId: projectTenant ?? tenantId ?? "tenant-a",
            ProjectId: ProjectIdValue,
            Name: "Context Project",
            Description: "metadata-only context",
            SetupMetadata: "metadata",
            Setup: null,
            ProjectFolder: new ProjectFolderReference(
                FolderId: projectFolderState == ReferenceState.Pending ? null : FolderIdValue,
                DisplayName: "Context Folder",
                ReferenceState: projectFolderState,
                ReasonCode: null,
                ObservedAt: DateTimeOffset.UnixEpoch),
            FileReferences: fileReferences,
            MemoryReferences: memoryReferences,
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

    private sealed class StartAppOptions
    {
        public ProjectLifecycle Lifecycle { get; set; } = ProjectLifecycle.Active;

        public bool SeedFileReference { get; set; }

        public bool SeedMemoryReference { get; set; }

        public ReferenceState FileReferenceState { get; set; } = ReferenceState.Included;

        public ReferenceState MemoryReferenceState { get; set; } = ReferenceState.Included;

        public IProjectConversationDirectory? ConversationDirectoryOverride { get; set; }

        public StartAppOptions()
        {
        }

        public StartAppOptions(
            bool SeedFileReference = false,
            bool SeedMemoryReference = false,
            ProjectLifecycle Lifecycle = ProjectLifecycle.Active,
            ReferenceState FileReferenceState = ReferenceState.Included,
            IProjectConversationDirectory? ConversationDirectoryOverride = null)
        {
            this.SeedFileReference = SeedFileReference;
            this.SeedMemoryReference = SeedMemoryReference;
            this.Lifecycle = Lifecycle;
            this.FileReferenceState = FileReferenceState;
            this.ConversationDirectoryOverride = ConversationDirectoryOverride;
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

        public Task<ProjectCommandSubmissionResult> SubmitConfirmProjectResolutionAsync(
            ConfirmProjectResolution command,
            CancellationToken cancellationToken = default)
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

    private sealed class ForbiddenConversationDirectory : IProjectConversationDirectory
    {
        public Task<ProjectConversationsPage> ListForProjectAsync(
            ProjectId projectId,
            ConversationTenantId tenantId,
            CallerPrincipalId caller,
            PageRequest page,
            CancellationToken cancellationToken = default)
        {
            ProjectConversationItem item = new(
                projectId,
                new ConversationId(ConversationIdValue),
                "active",
                "Context conversation",
                ProjectConversationTrustSignal.Forbidden,
                null,
                null);
            return Task.FromResult(new ProjectConversationsPage(
                projectId,
                [item],
                new ProjectConversationPageMetadata(1),
                ProjectConversationTrustSignal.Forbidden));
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
}
