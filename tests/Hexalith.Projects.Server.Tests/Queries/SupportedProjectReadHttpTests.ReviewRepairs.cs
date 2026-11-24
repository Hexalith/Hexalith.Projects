namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Folders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

/// <summary>Targeted retained-read regressions from the Story 6.7 review.</summary>
public sealed partial class SupportedProjectReadHttpTests
{
    [Theory]
    [InlineData("row-validator", 503)]
    [InlineData("row-read", 503)]
    [InlineData("row-stale", 503)]
    [InlineData("final-row", 503)]
    [InlineData("audit-stale", 503)]
    [InlineData("last-row-stale", 404)]
    public async Task ReviewLegacyListRefusesRetryableOrNoncurrentRowsAndFinalAuthority(string fault, int status)
    {
        using RSA rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        WebApplication? active = null;
        var validator = new ProjectReadTestAuthorization();
        Dictionary<string, int> reads = new(StringComparer.Ordinal);
        validator.Decision = request =>
        {
            string id = request.ProjectId ?? "scope";
            int count = reads.GetValueOrDefault(id) + 1; reads[id] = count;
            if (fault == "row-stale" && id == "scope" || fault == "last-row-stale" && id == "project-c" && count > 1)
                ((ProjectReadTestClock)active!.Services.GetRequiredService<TimeProvider>()).Now += TimeSpan.FromMinutes(6);
            return (fault == "row-validator" && id == "project-b" || fault == "final-row" && id == "project-a" && count > 1)
                ? EventStoreAuthorizationValidationResult.Unavailable() : EventStoreAuthorizationValidationResult.Allowed("review-current");
        };
        Hexalith.Projects.Infrastructure.DaprProjectProjectionStore? journal = null;
        var details = new CutoverReviewDetailReadModel((tenant, id, token) =>
            fault == "row-read" && id == "project-b" ? Task.FromException<ProjectDetailItem?>(new IOException("controlled row outage"))
                : journal!.GetDetailAsync(tenant, id, token));
        var fixture = await CutoverRuntimeAsync(key, configure: services =>
        {
            services.RemoveAll<IProjectEventStoreAuthorizationValidator>(); services.AddSingleton<IProjectEventStoreAuthorizationValidator>(validator);
            services.RemoveAll<IProjectDetailReadModel>(); services.AddSingleton<IProjectDetailReadModel>(details);
        });
        active = fixture.App; journal = fixture.Journal;
        await using var lifetime = active.ConfigureAwait(true);
        if (fault == "audit-stale")
            active.Services.GetRequiredService<Hexalith.EventStore.Testing.Fakes.InMemoryReadModelStore>().ConcurrentWriteBeforeTrySave = () =>
                ((ProjectReadTestClock)active.Services.GetRequiredService<TimeProvider>()).Now += TimeSpan.FromMinutes(6);
        using HttpClient client = CutoverClient(active, key);
        string before = await JournalBytesAsync(fixture.State);
        using var response = await client.GetAsync("/api/v1/projects?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
        ((int)response.StatusCode).ShouldBe(status);
        string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        raw.ShouldNotContain("Project project-"); raw.ShouldNotContain("\"items\"");
        (await JournalBytesAsync(fixture.State)).ShouldBe(before);
        await active.StopAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("current")]
    [InlineData("wrong-project")]
    [InlineData("invalid-metadata")]
    public async Task ReviewLegacyListUsesOnlyMetadataFromTheAuthorizedProject(string observation)
    {
        using RSA rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa);
        Hexalith.Projects.Infrastructure.DaprProjectProjectionStore? journal = null;
        DateTimeOffset updated = SupportedProjectReadFixture.EventTime.AddDays(1);
        var details = new CutoverReviewDetailReadModel(async (tenant, id, token) =>
        {
            var detail = await journal!.GetDetailAsync(tenant, id, token).ConfigureAwait(false);
            return detail?.ProjectId == "project-a" ? detail with
            {
                ProjectId = observation == "wrong-project" ? "project-other" : detail.ProjectId,
                Name = observation == "invalid-metadata" ? " " : "Authorized detail name",
                Lifecycle = ProjectLifecycle.Archived,
                UpdatedAt = updated,
                Sequence = 999,
            } : detail;
        });
        var fixture = await CutoverRuntimeAsync(key, configure: services =>
        {
            services.RemoveAll<IProjectDetailReadModel>();
            services.AddSingleton<IProjectDetailReadModel>(details);
        });
        journal = fixture.Journal;
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using HttpClient client = CutoverClient(fixture.App, key);
        using var response = await client.GetAsync("/api/v1/projects?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
        string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        raw.ShouldNotContain("Project project-a");
        raw.ShouldNotContain("project-other");
        if (observation != "current")
        {
            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            raw.ShouldNotContain("Authorized detail name");
        }
        else
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument body = JsonDocument.Parse(raw);
            JsonElement row = body.RootElement.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("projectId").GetString() == "project-a");
            row.GetProperty("name").GetString().ShouldBe("Authorized detail name");
            row.GetProperty("lifecycleState").GetString().ShouldBe("archived");
            row.GetProperty("updatedAt").GetDateTimeOffset().ShouldBe(updated);
            row.GetProperty("freshness").GetProperty("projectionWatermark").GetString().ShouldBe("watermark_00000999");

            using var active = await client.GetAsync("/api/v1/projects?inspectDescriptiveMetadata=true&lifecycle=active", TestContext.Current.CancellationToken);
            active.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await active.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain("project-a");

            using var archived = await client.GetAsync("/api/v1/projects?inspectDescriptiveMetadata=true&lifecycle=archived", TestContext.Current.CancellationToken);
            archived.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument archivedBody = JsonDocument.Parse(await archived.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            archivedBody.RootElement.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("projectId").GetString()).ShouldContain("project-a");
        }
        await fixture.App.StopAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("projects:role")]
    [InlineData("role")]
    [InlineData("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")]
    public async Task ReviewLegacyPendingHistoricalFolderIdUsesTheSupportedQuarantineBoundary(string roleClaimType)
    {
        using RSA rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        Hexalith.Projects.Infrastructure.DaprProjectProjectionStore? journal = null;
        var detail = new CutoverReviewDetailReadModel(async (tenant, id, token) =>
        {
            var value = await journal!.GetDetailAsync(tenant, id, token).ConfigureAwait(false);
            return value?.ProjectId == "project-a" ? value with { ProjectFolder = value.ProjectFolder! with { ReferenceState = ReferenceState.Pending } } : value;
        });
        var fixture = await CutoverRuntimeAsync(key, configure: services => { services.RemoveAll<IProjectDetailReadModel>(); services.AddSingleton<IProjectDetailReadModel>(detail); });
        journal = fixture.Journal; await using var lifetime = fixture.App.ConfigureAwait(true);
        using HttpClient client = CutoverClient(fixture.App, key);
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(key, role: "TenantProjectAdministrator", roleClaimType: roleClaimType));
        string before = await JournalBytesAsync(fixture.State);
        using var ordinary = await client.GetAsync("/api/v1/projects/project-a?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
        ordinary.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var list = await client.GetAsync("/api/v1/projects?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
        (await list.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain("Project project-a");
        using var inspected = await client.GetAsync("/api/v1/projects/project-a?inspectDescriptiveMetadata=true&inspectQuarantined=true", TestContext.Current.CancellationToken);
        inspected.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await inspected.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var folder = body.RootElement.GetProperty("references").EnumerateArray().Single(r => r.GetProperty("referenceKind").GetString() == "folder");
        folder.GetProperty("referenceId").GetString().ShouldBe("folder-project-a");
        folder.GetProperty("referenceState").GetString().ShouldBe("pending");
        folder.TryGetProperty("displayName", out var label).ShouldBeFalse();
        (await JournalBytesAsync(fixture.State)).ShouldBe(before);
        await fixture.App.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void ReviewRuntimeCompositionPreservesAnExplicitCustomFolderDirectory()
    {
        var explicitDirectory = new ProjectReadTestFolders();
        ServiceCollection services = new(); services.AddLogging(); services.AddSingleton<IProjectFolderDirectory>(explicitDirectory);
        services.AddProjectsServer(); services.AddProjectsServerRuntimeInfrastructure();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IProjectFolderDirectory>().ShouldBeSameAs(explicitDirectory);
    }

    [Theory]
    [InlineData("lifecycle")]
    [InlineData("permissions")]
    public async Task ReviewPreregisteredFoldersEndpointStillUsesV1AndWithholdsStaleFolderEvidence(string stale)
    {
        using RSA rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(); builder.Configuration["urls"] = "http://127.0.0.1:0";
        await using WebApplication owner = builder.Build();
        List<string> requests = [];
        owner.MapGet("/api/v1/folders/{folder}/lifecycle-status", (string folder, HttpContext http) =>
        {
            requests.Add(http.Request.Path); http.Request.Headers.Authorization.ToString().ShouldStartWith("Bearer ");
            return Results.Json(new { folderId = folder, lifecycleState = "ready", archived = false, repositoryBindingId = (string?)null,
                providerBindingRef = (string?)null, freshness = new { readConsistency = "eventually_consistent", observedAt = DateTimeOffset.UtcNow, projectionWatermark = "owner-17", stale = stale == "lifecycle" } });
        });
        owner.MapGet("/api/v1/folders/{folder}/effective-permissions", (string folder, HttpContext http) =>
        {
            requests.Add(http.Request.Path); http.Request.Headers.Authorization.ToString().ShouldStartWith("Bearer ");
            return Results.Json(new { folderId = folder, permissions = new[] { "read" }, authorizationOutcome = "allowed",
                freshness = new { readConsistency = "read_your_writes", observedAt = DateTimeOffset.UtcNow, projectionWatermark = "owner-17", stale = stale == "permissions" } });
        });
        await owner.StartAsync(TestContext.Current.CancellationToken);
        ServiceCollection services = new(); services.AddLogging();
        Hexalith.Folders.Client.FoldersClientServiceCollectionExtensions.AddFoldersClient(services, options => options.BaseAddress = new Uri(owner.Urls.Single()));
        services.AddProjectsServer(); services.AddProjectsServerRuntimeInfrastructure();
        await using var production = services.BuildServiceProvider();
        var fixture = await CutoverRuntimeAsync(key, configure: configured =>
        {
            configured.RemoveAll<IProjectFolderDirectory>(); configured.AddSingleton(production.GetRequiredService<IProjectFolderDirectory>());
        });
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using HttpClient client = CutoverClient(fixture.App, key);
        // The production handler forwards the actual authenticated request's bearer through the shared accessor.
        string before = await JournalBytesAsync(fixture.State);
        using var response = await client.GetAsync("/api/v1/projects/project-a?inspectDescriptiveMetadata=true", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(raw);
        var folder = document.RootElement.GetProperty("references").EnumerateArray().Single(r => r.GetProperty("referenceKind").GetString() == "folder");
        folder.GetProperty("referenceState").GetString().ShouldBe("unavailable");
        folder.TryGetProperty("displayName", out _).ShouldBeFalse(); raw.ShouldNotContain("Folder project-a");
        requests.Count.ShouldBe(stale == "lifecycle" ? 1 : 2);
        requests.ShouldAllBe(path => path.StartsWith("/api/v1/", StringComparison.Ordinal));
        (await JournalBytesAsync(fixture.State)).ShouldBe(before);
        await fixture.App.StopAsync(TestContext.Current.CancellationToken); await owner.StopAsync(TestContext.Current.CancellationToken);
    }
}
