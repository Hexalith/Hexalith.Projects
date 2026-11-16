namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Conversations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectReadHttpTests
{
    [Fact]
    public async Task ContextSafeMetadataWithholdsRealFolderLabelsOnSupportedAndAllRetainedContextPaths()
    {
        using var rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        var fixture = await CutoverRuntimeAsync(key);
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using var client = CutoverClient(fixture.App, key);
        string before = await JournalBytesAsync(fixture.State);
        var source = (await fixture.Journal.GetDetailAsync("tenant-a", "project-a", TestContext.Current.CancellationToken))!;
        source.ProjectFolder!.DisplayName.ShouldNotBeNullOrWhiteSpace();
        var context = await ReadShadowQueryAsync<ProjectContextReadResponse>(client,
            CutoverQuery("project-a", ProjectsServerModule.GetProjectContextQueryType, new GetProjectContextQuery("project-a") { ConversationId = "bound-conversation" }));
        context.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        context.ProjectFolder.ShouldNotBeNull(); context.ProjectFolder.DisplayName.ShouldBeNull();
        var explain = await ReadShadowQueryAsync<ExplainContextSelectionResponse>(client,
            CutoverQuery("project-a", ProjectsServerModule.ExplainContextSelectionQueryType, new ExplainContextSelectionQuery("project-a") { ConversationId = "bound-conversation" }));
        explain.Context.ProjectFolder.ShouldNotBeNull(); explain.Context.ProjectFolder.DisplayName.ShouldBeNull();
        foreach (string suffix in new[] { "context", "context/explain", "context/refresh" })
        {
            using var response = await client.GetAsync("/api/v1/projects/project-a/" + suffix + "?conversationId=bound-conversation", TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            JsonElement result = suffix == "context/explain" ? body.RootElement.GetProperty("context") : body.RootElement;
            var folder = result.GetProperty("projectFolder");
            folder.GetProperty("referenceId").GetString().ShouldBe(source.ProjectFolder.FolderId);
            (!folder.TryGetProperty("displayName", out JsonElement label) || label.ValueKind == JsonValueKind.Null).ShouldBeTrue();
        }
        (await JournalBytesAsync(fixture.State)).ShouldBe(before);
        await fixture.App.StopAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(ReferenceState.Unauthorized)]
    [InlineData(ReferenceState.Excluded)]
    public async Task LegacyConversationResolutionHidesOwnerDeniedOrExcludedMetadata(ReferenceState state)
    {
        using var rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        var fixture = await CutoverRuntimeAsync(key, configure: services =>
        {
            services.RemoveAll<IProjectConversationResolutionDirectory>();
            services.AddSingleton<IProjectConversationResolutionDirectory>(new CutoverConversationResolutionDirectory(state));
        });
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using var client = CutoverClient(fixture.App, key);
        string before = await JournalBytesAsync(fixture.State);
        using var response = await client.GetAsync("/api/v1/projects/resolution/from-conversation?conversationId=conversation", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        raw.ShouldNotContain("Project project-"); raw.ShouldNotContain("candidates");
        (await JournalBytesAsync(fixture.State)).ShouldBe(before);
        await fixture.App.StopAsync(TestContext.Current.CancellationToken);
    }
}
