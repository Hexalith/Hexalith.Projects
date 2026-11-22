// <copyright file="SupportedProjectReadHttpTests.ReviewPass3.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Projections.ProjectDetail;
using Hexalith.Projects.Server.Conversations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

/// <summary>Review pass 3: retained context reads recheck authority after their last owner await.</summary>
public sealed partial class SupportedProjectReadHttpTests
{
    [Theory]
    [InlineData("/context", "tenant-revoked")]
    [InlineData("/context", "tenant-stale")]
    [InlineData("/context", "tenant-generation")]
    [InlineData("/context", "eventstore-revoked")]
    [InlineData("/context", "eventstore-generation")]
    [InlineData("/context", "detail")]
    [InlineData("/context", "none")]
    [InlineData("/context/explain", "tenant-revoked")]
    [InlineData("/context/explain", "detail")]
    [InlineData("/context/refresh", "tenant-revoked")]
    [InlineData("/context/refresh", "eventstore-revoked")]
    [InlineData("/context/refresh", "detail")]
    [InlineData("/setup/conversation-start", "tenant-revoked")]
    [InlineData("/setup/conversation-start", "eventstore-generation")]
    [InlineData("/setup/conversation-start", "detail")]
    [InlineData("/setup/conversation-start", "none")]
    public async Task Review3LegacyContextRechecksAuthorityAfterBlockedNativeMembership(string path, string change)
    {
        ArgumentNullException.ThrowIfNull(change);
        using var rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var membership = new CurrentProjectConversationMembership(asyncDecision: async () =>
        { entered.TrySetResult(); await release.Task.ConfigureAwait(false); return true; });
        bool changed = false; var validator = new ProjectReadTestAuthorization();
        validator.Decision = _ => changed && change == "eventstore-revoked" ? EventStoreAuthorizationValidationResult.Denied()
            : EventStoreAuthorizationValidationResult.Allowed(changed && change == "eventstore-generation" ? "changed-authority" : "initial-authority");
        Hexalith.Projects.Infrastructure.DaprProjectProjectionStore? journal = null;
        ProjectDetailItem? changedDetail = null;
        var read = new CutoverReviewDetailReadModel((tenant, id, ct) => changedDetail is not null && id == "project-a"
            ? Task.FromResult<ProjectDetailItem?>(changedDetail) : journal!.GetDetailAsync(tenant, id, ct));
        var fixture = await CutoverRuntimeAsync(key, configure: services =>
        {
            services.RemoveAll<IProjectConversationMembershipDirectory>(); services.AddSingleton<IProjectConversationMembershipDirectory>(membership);
            services.RemoveAll<IProjectEventStoreAuthorizationValidator>(); services.AddSingleton<IProjectEventStoreAuthorizationValidator>(validator);
            services.RemoveAll<IProjectDetailReadModel>(); services.AddSingleton<IProjectDetailReadModel>(read);
        });
        journal = fixture.Journal;
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using var client = CutoverClient(fixture.App, key);
        string before = await JournalBytesAsync(fixture.State);
        Task<HttpResponseMessage> pending = client.GetAsync("/api/v1/projects/project-a" + path + "?conversationId=bound-conversation", TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            if (change.StartsWith("tenant-", StringComparison.Ordinal)) await ChangeTenantAsync(fixture.App.Services, change);
            if (change == "detail") changedDetail = (await journal.GetDetailAsync("tenant-a", "project-a", TestContext.Current.CancellationToken))! with { Name = "changed-protected-name", Sequence = 99 };
            changed = true; release.TrySetResult();
            using var response = await pending.ConfigureAwait(true);
            string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(change switch
            {
                "none" => HttpStatusCode.OK,
                "tenant-stale" => HttpStatusCode.ServiceUnavailable,
                _ => HttpStatusCode.NotFound,
            });
            raw.ShouldNotContain("changed-protected-name");
            if (change != "none") { raw.ShouldNotContain("projectSetup"); raw.ShouldNotContain("Project project-a"); }
            (await JournalBytesAsync(fixture.State)).ShouldBe(before);
        }
        finally { release.TrySetResult(); await fixture.App.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Theory]
    [InlineData("context")]
    [InlineData("explain")]
    [InlineData("conversation-start")]
    public async Task Review3FailedNativeMembershipAsksForCurrentContextInsteadOfAnAdministrator(string operation)
    {
        using RSA rsa = RSA.Create(2048); var key = new RsaSecurityKey(rsa);
        var fixture = await CutoverRuntimeAsync(key);
        await using var lifetime = fixture.App.ConfigureAwait(true);
        using HttpClient client = CutoverClient(fixture.App, key);
        string type = operation switch
        {
            "context" => ProjectsServerModule.GetProjectContextQueryType,
            "explain" => ProjectsServerModule.ExplainContextSelectionQueryType,
            _ => ProjectsServerModule.GetConversationStartSetupQueryType,
        };
        foreach (string? conversation in new[] { null, "unbound-conversation" })
        {
            var query = CutoverQuery("project-a", type, new { projectId = "project-a", conversationId = conversation });
            using HttpResponseMessage supported = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
            supported.StatusCode.ShouldBe(HttpStatusCode.OK);
            var result = await supported.Content.ReadFromJsonAsync<Hexalith.EventStore.Contracts.Queries.QueryResult>(TestContext.Current.CancellationToken);
            JsonElement payload = result!.GetPayload();
            JsonElement response = operation == "explain" ? payload.GetProperty("context") : payload;
            JsonElement snapshot = response.GetProperty("snapshot");
            snapshot.GetProperty("responseState").GetString().ShouldBe("Unavailable");
            snapshot.GetProperty("recoveryActions").EnumerateArray().Select(action => action.GetString()).ShouldBe(["RefreshContext"]);
            response.GetProperty("setup").ValueKind.ShouldBe(JsonValueKind.Null);
        }
        await fixture.App.StopAsync(TestContext.Current.CancellationToken);
    }
}
