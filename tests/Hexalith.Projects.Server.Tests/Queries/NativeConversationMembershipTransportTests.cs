#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Api;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Conversations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

/// <summary>Real native owner HTTP transport, caller credential forwarding, boundedness and direct-call compatibility.</summary>
public sealed class NativeConversationMembershipTransportTests
{
    [Fact]
    public async Task DefaultProjectsTransportForwardsTheOriginalActorAndRestrictsNativeMembership()
    {
        using RSA rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var freshness = new ProjectionFreshnessV1(SchemaVersion.Current, "native-cursor", 17, now, now,
            TimeSpan.Zero, false, ProjectionTrustState.Current, ProjectionFreshnessReasonCode.Current);
        TenantId tenant = new("tenant-a"); ConversationId conversation = new("bound-conversation");
        var model = new ConversationProjectedReadModels(
            new(SchemaVersion.Current, tenant, conversation, freshness, "Open", "private-label", ProjectId: new("project-a")),
            new(SchemaVersion.Current, tenant, conversation, freshness, "Open", "private-label", ProjectId: new("project-a"),
                Attributes: new Dictionary<string, string> { ["private"] = "private-adopter-metadata" }));
        var store = new NativeMembershipOwnerStore(model);
        var access = new NativeMembershipOwnerAccess();
        WebApplicationBuilder ownerBuilder = WebApplication.CreateSlimBuilder();
        ownerBuilder.Configuration["urls"] = "http://127.0.0.1:0";
        ownerBuilder.Services.AddDataProtection();
        var advisoryStore = new Hexalith.EventStore.Testing.Fakes.InMemoryReadModelStore();
        var isolated = new ConversationResolutionMetadataReadModel(tenant, conversation,
            new(conversation.Value, "advisory-project", "classified-scoring-label", "Open"), freshness,
            "committed-advisory", new string('a', 64), true, ConversationResolutionScoringLabel.OwnerClassification)
            { LabelPurpose = ConversationResolutionScoringLabel.SupportedPurpose };
        string advisoryKey = "projection:conversations-resolution:"
            + Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(tenant.Value))) + ":"
            + Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(conversation.Value)));
        await advisoryStore.SaveAsync("statestore", advisoryKey, isolated, TestContext.Current.CancellationToken);
        ownerBuilder.Services.AddSingleton<Hexalith.EventStore.Client.Projections.IReadModelStore>(advisoryStore);
        ownerBuilder.Services.AddSingleton<Hexalith.Conversations.Server.TenantAccess.IConversationTenantAccessService>(access);
        ownerBuilder.Services.AddSingleton<IConversationProjectionReadStore>(store);
        ownerBuilder.Services.AddConversationQueries(_ => { });
        ownerBuilder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new() { ValidateIssuer = true, ValidIssuer = "native-owner", ValidateAudience = true,
                ValidAudience = "native-owner", IssuerSigningKey = key, ValidateLifetime = true };
        });
        ownerBuilder.Services.AddAuthorization();
        await using WebApplication owner = ownerBuilder.Build();
        owner.UseAuthentication(); owner.UseAuthorization(); owner.MapConversationReadApi();
        // A former owner ignores the newly requested mode and emits an ordinary rich result.
        owner.MapGet("/api/v1/conversations/old-owner", () => ConversationDetailResult.Visible(SchemaVersion.Current,
            new ConversationDetailsV1(SchemaVersion.Current, tenant, new("old-owner"), freshness, "Open", "private-old-label", ProjectId: new("project-a")), "Visible."))
            .RequireAuthorization();
        await owner.StartAsync(TestContext.Current.CancellationToken);
        ServiceCollection projects = new(); projects.AddLogging(); projects.AddProjectsServer(); projects.AddProjectsServerRuntimeInfrastructure();
        projects.AddHttpClient(nameof(IConversationClient), client => client.BaseAddress = new Uri(owner.Urls.Single()));
        await using ServiceProvider provider = projects.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider, User = new(new ClaimsIdentity([new("sub", "actor-1")], "test")) };
        JwtSecurityTokenHandler tokens = new(); tokens.OutboundClaimTypeMap.Clear();
        string bearer = tokens.CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = "native-owner", Audience = "native-owner", Expires = now.UtcDateTime.AddMinutes(5),
            Subject = new ClaimsIdentity([new("sub", "actor-1"), new(ClaimTypes.NameIdentifier, "actor-1"), new("tid", "tenant-a")]),
            SigningCredentials = new(key, SecurityAlgorithms.RsaSha256),
        });
        context.Request.Headers.Authorization = "Bearer " + bearer;
        IHttpContextAccessor accessor = provider.GetRequiredService<IHttpContextAccessor>(); accessor.HttpContext = context;
        try
        {
            var directory = provider.GetRequiredService<IProjectConversationMembershipDirectory>();
            (await directory.IsCurrentMemberAsync("tenant-a", "project-a", "bound-conversation", "actor-1", "native-test", TestContext.Current.CancellationToken)).ShouldBeTrue();
            foreach (var requested in new[] { ("foreign", "project-a", "bound-conversation", "actor-1"),
                ("tenant-a", "foreign-project", "bound-conversation", "actor-1"), ("tenant-a", "project-a", "absent", "actor-1"),
                ("tenant-a", "project-a", "bound-conversation", "service-principal") })
                (await directory.IsCurrentMemberAsync(requested.Item1, requested.Item2, requested.Item3, requested.Item4, "native-test", TestContext.Current.CancellationToken)).ShouldBeFalse();
            IConversationClient client = provider.GetRequiredService<IConversationClient>();
            var query = new GetConversationQuery(SchemaVersion.Current, tenant, "actor-1", "native-test", conversation);
            var direct = await client.GetConversationAsync(query, TestContext.Current.CancellationToken);
            direct.IsSuccess.ShouldBeTrue(); direct.Value!.Details!.Label.ShouldBe("private-label");
            var bounded = await client.GetConversationAsync(query with { NativeMembershipOnly = true }, TestContext.Current.CancellationToken);
            bounded.IsSuccess.ShouldBeTrue(); bounded.Value!.Details!.Label.ShouldBeNull();
            bounded.Value.Details.Messages.ShouldBeEmpty(); bounded.Value.Details.Attributes.ShouldBeEmpty();
            bounded.Value.Details.ProjectHydration.ShouldBeNull();
            bounded.Value.ReadPurpose.ShouldBe(GetConversationQuery.NativeMembershipPurpose);
            var oldOwner = provider.GetRequiredService<Hexalith.Projects.Server.Conversations.IProjectConversationResolutionDirectory>();
            var advisory = await oldOwner.ReadConversationMetadataAsync(conversation, tenant, new("actor-1"), "native-test", TestContext.Current.CancellationToken);
            advisory.ReferenceState.ShouldBe(Hexalith.Projects.Contracts.Ui.ReferenceState.Included);
            advisory.LinkedProjectId.ShouldBe("advisory-project"); advisory.SafeLabel.ShouldBe("classified-scoring-label");
            var advisoryWire = await client.GetConversationAsync(query with { AdvisoryMetadataOnly = true }, TestContext.Current.CancellationToken);
            advisoryWire.Value!.ReadPurpose.ShouldBe(ConversationResolutionScoringLabel.SupportedPurpose);
            advisoryWire.Value.Details!.Messages.ShouldBeEmpty(); advisoryWire.Value.Details.Attributes.ShouldBeEmpty();
            advisoryWire.Value.Details.ProjectHydration.ShouldBeNull();
            JsonSerializer.Serialize(advisoryWire.Value).ShouldNotContain("private-");
            (await directory.IsCurrentMemberAsync("tenant-a", "advisory-project", "bound-conversation", "actor-1", "native-test", TestContext.Current.CancellationToken)).ShouldBeFalse();
            var oldMetadata = await oldOwner.ReadConversationMetadataAsync(new("old-owner"), tenant, new("actor-1"), "native-test", TestContext.Current.CancellationToken);
            oldMetadata.ReferenceState.ShouldBe(Hexalith.Projects.Contracts.Ui.ReferenceState.Unavailable);
            oldMetadata.SafeLabel.ShouldBeNull(); oldMetadata.LinkedProjectId.ShouldBeNull();
            (await directory.IsCurrentMemberAsync("tenant-a", "project-a", "old-owner", "actor-1", "native-test", TestContext.Current.CancellationToken)).ShouldBeFalse();
            var conflicting = await client.GetConversationAsync(query with { NativeMembershipOnly = true, AdvisoryMetadataOnly = true }, TestContext.Current.CancellationToken);
            conflicting.Value.ShouldNotBeNull(); conflicting.Value.Details.ShouldBeNull();
            conflicting.Value.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
            JsonSerializer.Serialize(bounded.Value).ShouldNotContain("private-");
            JsonSerializer.Serialize(store.Models).ShouldBe(JsonSerializer.Serialize(model));
            access.ObservedActors.ShouldNotBeEmpty(); access.ObservedActors.All(actor => actor == "actor-1").ShouldBeTrue();
            context.Request.Headers.Authorization = string.Empty;
            (await directory.IsCurrentMemberAsync("tenant-a", "project-a", "bound-conversation", "actor-1", "native-test", TestContext.Current.CancellationToken)).ShouldBeFalse();
        }
        finally { accessor.HttpContext = null; await owner.StopAsync(TestContext.Current.CancellationToken); }
    }
}
#endif
