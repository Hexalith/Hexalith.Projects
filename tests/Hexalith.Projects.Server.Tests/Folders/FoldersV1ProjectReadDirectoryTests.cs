namespace Hexalith.Projects.Server.Tests.Folders;

using System.Net;
using System.Text.Json.Nodes;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Tests.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

/// <summary>Retained owner wire compatibility and fail-closed mappings, independent of Dapr.</summary>
public sealed class FoldersV1ProjectReadDirectoryTests
{
    [Theory]
    [InlineData("oversize-lifecycle", ProjectFolderValidationOutcome.Unavailable)]
    [InlineData("oversize-permissions", ProjectFolderValidationOutcome.Unavailable)]
    [InlineData("ready", ProjectFolderValidationOutcome.Accepted)]
    [InlineData("requested", ProjectFolderValidationOutcome.Accepted)]
    [InlineData("reconciliation_required", ProjectFolderValidationOutcome.Accepted)]
    [InlineData("additive", ProjectFolderValidationOutcome.Accepted)]
    [InlineData("denied_safe", ProjectFolderValidationOutcome.Denied)]
    [InlineData("archived", ProjectFolderValidationOutcome.Archived)]
    [InlineData("failed", ProjectFolderValidationOutcome.Archived)]
    [InlineData("inaccessible", ProjectFolderValidationOutcome.Archived)]
    [InlineData("unknown_provider_outcome", ProjectFolderValidationOutcome.Archived)]
    [InlineData("unknown-state", ProjectFolderValidationOutcome.Unavailable)]
    [InlineData("empty-permissions", ProjectFolderValidationOutcome.Unavailable)]
    [InlineData("duplicate-permissions", ProjectFolderValidationOutcome.Unavailable)]
    [InlineData("missing-required", ProjectFolderValidationOutcome.Unavailable)]
    [InlineData("foreign-folder", ProjectFolderValidationOutcome.Unavailable)]
    [InlineData("stale-lifecycle", ProjectFolderValidationOutcome.Stale)]
    [InlineData("stale-permissions", ProjectFolderValidationOutcome.Stale)]
    [InlineData("400", ProjectFolderValidationOutcome.ValidationFailed)]
    [InlineData("401", ProjectFolderValidationOutcome.Denied)]
    [InlineData("403", ProjectFolderValidationOutcome.Denied)]
    [InlineData("404", ProjectFolderValidationOutcome.Denied)]
    [InlineData("409", ProjectFolderValidationOutcome.Archived)]
    [InlineData("503", ProjectFolderValidationOutcome.Unavailable)]
    public async Task OwnerV1ReadMapsCurrentEvidenceAndToleratesAdditiveFields(string scenario, ProjectFolderValidationOutcome expected)
    {
        var builder = WebApplication.CreateSlimBuilder(); builder.Configuration["urls"] = "http://127.0.0.1:0";
        await using var owner = builder.Build();
        int lifecycleCalls = 0, permissionsCalls = 0;
        owner.MapGet("/api/v1/folders/{folder}/lifecycle-status", (HttpContext http) =>
        {
            lifecycleCalls++;
            http.Request.Headers["X-Correlation-Id"].ToString().ShouldBe("read-test");
            http.Request.Headers["X-Hexalith-Freshness"].ToString().ShouldBe("eventually_consistent");
            if (int.TryParse(scenario, out int status)) return Results.StatusCode(status);
            JsonNode body = JsonNode.Parse("{\"folderId\":\"folder-1\",\"lifecycleState\":\"ready\",\"archived\":false,\"freshness\":{\"readConsistency\":\"eventually_consistent\",\"observedAt\":\"2026-11-16T12:00:00Z\",\"projectionWatermark\":\"17\",\"stale\":false}}")!;
            if (scenario is "requested" or "reconciliation_required" or "failed" or "inaccessible" or "unknown_provider_outcome" or "unknown-state") body["lifecycleState"] = scenario;
            if (scenario == "archived") body["archived"] = true;
            if (scenario == "missing-required") body.AsObject().Remove("archived");
            if (scenario == "foreign-folder") body["folderId"] = "foreign";
            if (scenario == "oversize-lifecycle") body["futureOwnerMetadata"] = new string('x', 20_000);
            if (scenario == "stale-lifecycle") body["freshness"]!["stale"] = true;
            if (scenario == "additive") { body["newOwnerField"] = true; body["freshness"]!["newFreshnessField"] = "future"; }
            return Results.Json(body);
        });
        owner.MapGet("/api/v1/folders/{folder}/effective-permissions", (HttpContext http) =>
        {
            permissionsCalls++;
            http.Request.Headers["X-Hexalith-Freshness"].ToString().ShouldBe("read_your_writes");
            JsonNode body = JsonNode.Parse("{\"folderId\":\"folder-1\",\"permissions\":[\"read\"],\"authorizationOutcome\":\"allowed\",\"freshness\":{\"readConsistency\":\"read_your_writes\",\"observedAt\":\"2026-11-16T12:00:00Z\",\"projectionWatermark\":\"17\",\"stale\":false}}")!;
            if (scenario == "denied_safe") body["authorizationOutcome"] = "denied_safe";
            if (scenario == "empty-permissions") body["permissions"] = new JsonArray();
            if (scenario == "duplicate-permissions") body["permissions"] = new JsonArray("read", "read");
            if (scenario == "oversize-permissions") body["futureOwnerMetadata"] = new string('x', 20_000);
            if (scenario == "stale-permissions") body["freshness"]!["stale"] = true;
            if (scenario == "additive") { body["newOwnerField"] = "future"; body["freshness"]!["newFreshnessField"] = true; }
            return Results.Json(body);
        });
        await owner.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(owner.Urls.Single()) };
            var directory = new FoldersV1ProjectReadDirectory(http, new ProjectReadTestFolders());
            var result = await directory.RefreshFolderReferenceAsync(new ProjectId("project-a"), "folder-1", "read-test", TestContext.Current.CancellationToken);
            result.Outcome.ShouldBe(expected); result.CorrelationId.ShouldBe("read-test");
            lifecycleCalls.ShouldBe(1); permissionsCalls.ShouldBeLessThanOrEqualTo(1);
        }
        finally { await owner.StopAsync(TestContext.Current.CancellationToken); }
    }
}
