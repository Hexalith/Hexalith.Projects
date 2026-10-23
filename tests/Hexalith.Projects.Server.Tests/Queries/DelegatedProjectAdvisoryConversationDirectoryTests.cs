namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Dapr.Client;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Folders.Contracts;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server.Folders;
using Hexalith.Projects.Server.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

/// <summary>Exercises the real delegated Conversation owner hop in source and package test builds.</summary>
public sealed class DelegatedProjectAdvisoryConversationDirectoryTests
{
    /// <summary>Preserves exact owner routing and original authority for successful and failed owner responses.</summary>
    /// <param name="status">The owner gateway HTTP response status.</param>
    /// <param name="envelopeSuccess">Whether the gateway's response envelope reports success.</param>
    /// <param name="incomplete">Whether the successful advisory metadata reports incomplete observations.</param>
    [Theory]
    [InlineData(HttpStatusCode.OK, true, false)]
    [InlineData(HttpStatusCode.OK, true, true)]
    [InlineData(HttpStatusCode.NotFound, false, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, false, false)]
    [InlineData(HttpStatusCode.OK, false, false)]
    public async Task OwnerHopBindsExactRequestAndPreservesResponseClassification(
        HttpStatusCode status, bool envelopeSuccess, bool incomplete)
    {
        using RSA signingKey = RSA.Create(2048);
        DateTimeOffset now = SupportedProjectReadFixture.EventTime;
        QueryEnvelope original = OriginalQuery();
        int sends = 0;
        OwnerQueryTestAssertionIssuer issuer = new("outbound-workload-assertion");
        ConversationAdvisoryMetadataResult metadata = new(
            incomplete ? null : new ConversationResolutionMetadataV1("conversation-a", "project-a", "Project project-b", "Open"), incomplete);
        JsonElement payload = JsonSerializer.SerializeToElement(metadata, JsonSerializerOptions.Web);
        using OwnerQueryTestHttpHandler handler = new(async (message, token) =>
        {
            sends++;
            message.Method.ShouldBe(HttpMethod.Post);
            message.RequestUri!.AbsolutePath.ShouldBe("/v1.0/invoke/owner-gateway/method/api/v1/queries/owner-authority/workload");
            message.Headers.GetValues(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).ShouldBe(["outbound-workload-assertion"]);
            BoundOwnerQueryRequest request = await ReadOwnerRequestAsync(message, token).ConfigureAwait(false);
            request.PrincipalId.ShouldBe("actor-a");
            request.Query.Tenant.ShouldBe("tenant-a");
            request.Query.Domain.ShouldBe("conversations");
            request.Query.AggregateId.ShouldBe("conversation-a");
            request.Query.QueryType.ShouldBe(GetConversationAdvisoryMetadataQuery.QueryType);
            request.Query.Payload.ShouldNotBeNull();
            request.Query.Payload!.Value.GetRawText().ShouldBe("{}");
            request.Query.ProjectionType.ShouldBeNull();
            request.Query.EntityId.ShouldBeNull();
            request.Query.ProjectionActorType.ShouldBeNull();
            request.OriginatingAssertion.ShouldBe("originating-gateway-assertion");
            JsonSerializer.Serialize(request.OriginatingQuery, JsonSerializerOptions.Web)
                .ShouldBe(JsonSerializer.Serialize(original, JsonSerializerOptions.Web));
            string proof = message.Headers.GetValues(BoundOwnerQueryProof.HeaderName).Single();
            string publicKey = Convert.ToBase64String(signingKey.ExportSubjectPublicKeyInfo());
            BoundOwnerQueryProof.Verify(request, proof, "projects", publicKey, now).ShouldBeTrue();
            BoundOwnerQueryProof.Verify(request with { PrincipalId = "different-actor" }, proof, "projects", publicKey, now).ShouldBeFalse();
            return new HttpResponseMessage(status)
            {
                Content = JsonContent.Create(new SubmitQueryResponse("owner-correlation", payload, envelopeSuccess)),
            };
        });

        QueryResult result = await ExecuteAsync(original, signingKey, now, handler, issuer).ConfigureAwait(true);

        sends.ShouldBe(1);
        issuer.Requests.Count.ShouldBe(1);
        issuer.Requests.Single().Audience.ShouldBe("owner-gateway");
        issuer.Requests.Single().Operation.ShouldBe(EventStoreWorkloadOperations.GatewayOwnerAuthorityQuery);
        if (status == HttpStatusCode.OK && envelopeSuccess)
        {
            result.Success.ShouldBeTrue(result.ErrorMessage);
            result.ProjectionType.ShouldBe(GetConversationAdvisoryMetadataQuery.QueryType);
            result.GetPayload().GetRawText().ShouldBe(payload.GetRawText());
            result.GetPayload().Deserialize<ConversationAdvisoryMetadataResult>(JsonSerializerOptions.Web).ShouldBe(metadata);
        }
        else
        {
            result.Success.ShouldBeFalse();
            result.ErrorMessage.ShouldBe(status == HttpStatusCode.NotFound ? "safe-denial"
                : status == HttpStatusCode.OK ? QueryAdapterFailureReason.ActorResponseMismatch : QueryAdapterFailureReason.ActorException);
            result.PayloadBytes.ShouldBeNull();
        }
    }

    /// <summary>Preserves the originating actor when the same Folder authorizes one actor and denies another.</summary>
    /// <param name="actor">The gateway-verified original actor.</param>
    /// <param name="expected">The Projects classification of the owner response.</param>
    [Theory]
    [InlineData("actor-a", ProjectFolderValidationOutcome.Accepted)]
    [InlineData("actor-b", ProjectFolderValidationOutcome.Denied)]
    public async Task FolderOwnerReadUsesTheCurrentOriginalActor(string actor, ProjectFolderValidationOutcome expected)
    {
        using RSA signingKey = RSA.Create(2048);
        QueryEnvelope original = OriginalQuery() with { UserId = actor, OriginalActorId = actor };
        int sends = 0;
        using OwnerQueryTestHttpHandler handler = new(async (message, token) =>
        {
            sends++;
            message.Method.ShouldBe(HttpMethod.Post);
            message.RequestUri!.AbsolutePath.ShouldBe("/v1.0/invoke/owner-gateway/method/api/v1/queries/owner-authority/workload");
            BoundOwnerQueryRequest request = await ReadOwnerRequestAsync(message, token).ConfigureAwait(false);
            request.PrincipalId.ShouldBe(original.UserId);
            request.Query.Tenant.ShouldBe("tenant-a");
            request.Query.Domain.ShouldBe("folders");
            request.Query.AggregateId.ShouldBe("folder-a");
            request.Query.QueryType.ShouldBe(FolderOwnerReadQuery.QueryType);
            request.Query.Payload!.Value.GetRawText().ShouldBe("{}");
            request.OriginatingQuery!.UserId.ShouldBe(actor);
            request.OriginatingQuery.OriginalActorId.ShouldBe(actor);
            BoundOwnerQueryProof.Verify(request, message.Headers.GetValues(BoundOwnerQueryProof.HeaderName).Single(), "projects",
                Convert.ToBase64String(signingKey.ExportSubjectPublicKeyInfo()), SupportedProjectReadFixture.EventTime).ShouldBeTrue();
            return new HttpResponseMessage(request.PrincipalId == "actor-a" ? HttpStatusCode.OK : HttpStatusCode.NotFound)
            {
                Content = JsonContent.Create(new SubmitQueryResponse("owner-correlation",
                    JsonSerializer.SerializeToElement(new FolderOwnerReadResult(FolderOwnerReadOutcome.Allowed), JsonSerializerOptions.Web))),
            };
        });
        OwnerQueryTestAssertionIssuer issuer = new("outbound-workload-assertion");
        QueryResult result = await ExecuteAsync(original, signingKey, SupportedProjectReadFixture.EventTime,
            handler, issuer, folderRead: true).ConfigureAwait(true);
        result.Success.ShouldBeTrue(result.ErrorMessage);
        ProjectFolderValidationResult folder = result.GetPayload().Deserialize<ProjectFolderValidationResult>(JsonSerializerOptions.Web)!;
        folder.Outcome.ShouldBe(expected);
        folder.CorrelationId.ShouldBe(original.CorrelationId);
        sends.ShouldBe(1);
    }

    /// <summary>Refuses unverified or unbound owner requests before any outbound transport.</summary>
    /// <param name="failure">The missing trust or credential input.</param>
    [Theory]
    [InlineData("unverified")]
    [InlineData("foreign-tenant")]
    [InlineData("missing-assertion")]
    [InlineData("duplicate-assertion")]
    [InlineData("missing-key")]
    [InlineData("issuer-unavailable")]
    public async Task MissingAuthorityOrCredentialsNeverCallsOwner(string failure)
    {
        using RSA signingKey = RSA.Create(2048);
        int sends = 0;
        using OwnerQueryTestHttpHandler handler = new((_, _) =>
        {
            sends++;
            throw new InvalidOperationException("Untrusted owner query reached the transport.");
        });
        OwnerQueryTestAssertionIssuer issuer = new(failure == "issuer-unavailable" ? null : "outbound-workload-assertion");
        QueryResult result = await ExecuteAsync(OriginalQuery(), signingKey, SupportedProjectReadFixture.EventTime, handler, issuer, failure).ConfigureAwait(true);
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(failure is "missing-key" or "issuer-unavailable"
            ? QueryAdapterFailureReason.ActorException : "safe-denial");
        result.PayloadBytes.ShouldBeNull();
        sends.ShouldBe(0);
        issuer.Requests.Count.ShouldBe(failure == "issuer-unavailable" ? 1 : 0);
    }

    private static QueryEnvelope OriginalQuery()
        => new("tenant-a", "projects", "projects", DelegatedDirectoryTestQueryHandler.Route,
            "{\"conversationId\":\"conversation-a\"}"u8.ToArray(), "originating-correlation", "actor-a")
        {
            OriginalActorId = "actor-a", AuthenticatedWorkloadId = "chatbot", IsDelegated = true,
            DelegationId = "delegation-a", Scopes = ["projects.read"], Audience = ["projects-read-audience"],
            CallerAuthority = new("authenticated-caller", ["ProjectUser"], ["projects.read"]),
        };

    private static async Task<QueryResult> ExecuteAsync(QueryEnvelope original, RSA signingKey, DateTimeOffset now,
        OwnerQueryTestHttpHandler handler, OwnerQueryTestAssertionIssuer issuer, string? failure = null, bool folderRead = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:WorkloadIssuer:Workload"] = "projects",
            ["Authentication:BoundStreamRead:PrivateKey"] = failure == "missing-key" ? null : Convert.ToBase64String(signingKey.ExportPkcs8PrivateKey()),
            ["Authentication:QueryAuthority:GatewayWorkloadId"] = "owner-gateway",
        });
        builder.Services.AddProjectsServer();
        builder.Services.AddSingleton<DaprClient>(_ => new DaprClientBuilder().UseHttpEndpoint("http://127.0.0.1:1").Build());
        builder.Services.AddSingleton<TimeProvider>(new ProjectReadTestClock(now));
        builder.Services.AddSingleton<IWorkloadAssertionIssuer>(issuer);
        builder.Services.AddHttpClient(Options.DefaultName).ConfigurePrimaryHttpMessageHandler(() => handler);
        builder.Services.RemoveAll<IDomainQueryHandler>();
        IProjectAdvisoryConversationDirectory? directory = null;
        IProjectFolderDirectory? folders = null;
        builder.Services.AddSingleton<IDomainQueryHandler>(new DelegatedDirectoryTestQueryHandler(async token =>
        {
            if (!folderRead)
                return await directory!.ReadAsync(failure == "foreign-tenant" ? "tenant-b" : "tenant-a", "conversation-a", token).ConfigureAwait(false);
            ProjectFolderValidationResult folder = await folders!.RefreshFolderReferenceAsync(new ProjectId("project-a"),
                "folder-a", original.CorrelationId, token).ConfigureAwait(false);
            return QueryResult.FromPayload(JsonSerializer.SerializeToElement(folder, JsonSerializerOptions.Web));
        }));
        ServiceProvider provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var providerLifetime = provider.ConfigureAwait(false);
        directory = provider.GetRequiredService<IProjectAdvisoryConversationDirectory>();
        directory.ShouldBeOfType<DelegatedProjectAdvisoryConversationDirectory>();
        if (folderRead)
            folders = provider.GetRequiredService<IProjectFolderDirectory>().ShouldBeOfType<DelegatedProjectFolderDirectory>();
        AsyncServiceScope scope = provider.CreateAsyncScope();
        await using var scopeLifetime = scope.ConfigureAwait(false);
        IHttpContextAccessor accessor = provider.GetRequiredService<IHttpContextAccessor>();
        List<Claim> claims =
        [
            new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "owner-gateway"),
            new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
            .. QueryCallerAuthorityBinding.Bind(original).Select(pair => new Claim(pair.Key, pair.Value)),
        ];
        DefaultHttpContext http = new()
        {
            RequestServices = scope.ServiceProvider,
            User = new(new ClaimsIdentity(claims, "Workload")),
        };
        http.Request.Headers.Authorization = "Bearer human-bearer-sentinel";
        if (failure != "missing-assertion")
            http.Request.Headers[EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName] = failure == "duplicate-assertion"
                ? new Microsoft.Extensions.Primitives.StringValues(["originating-gateway-assertion", "extra-assertion"])
                : "originating-gateway-assertion";
        accessor.HttpContext = http;
        try
        {
            QueryResult result = failure == "unverified"
                ? await directory.ReadAsync("tenant-a", "conversation-a", TestContext.Current.CancellationToken).ConfigureAwait(false)
                : await DomainQueryDispatcher.ExecuteAsync(scope.ServiceProvider, original, TestContext.Current.CancellationToken).ConfigureAwait(false);
            VerifiedQueryCaller.Get(http).ShouldBeNull();
            return result;
        }
        finally
        {
            accessor.HttpContext = null;
        }
    }

    private static async Task<BoundOwnerQueryRequest> ReadOwnerRequestAsync(HttpRequestMessage message, CancellationToken token)
    {
        message.Headers.Authorization.ShouldBeNull();
        message.Headers.ToString().ShouldNotContain("human-bearer-sentinel");
        string body = await message.Content!.ReadAsStringAsync(token).ConfigureAwait(false);
        body.ShouldNotContain("human-bearer-sentinel");
        return JsonSerializer.Deserialize<BoundOwnerQueryRequest>(body, JsonSerializerOptions.Web)!;
    }
}
