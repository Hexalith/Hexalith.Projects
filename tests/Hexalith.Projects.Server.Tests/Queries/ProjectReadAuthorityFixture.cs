#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Dapr.Client;
using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Controllers;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ErrorHandling;
using Hexalith.EventStore.Queries;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.DomainServices;
using Hexalith.EventStore.Server.Pipeline;
using Hexalith.EventStore.Server.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Folders;
using Hexalith.Folders.Aggregates.Folder;
using Hexalith.Folders.Authorization;
using Hexalith.Folders.Contracts;
using Hexalith.Folders.Projections.TenantAccess;
using Hexalith.Folders.Server;
using Hexalith.Folders.Server.Authentication;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server.Authentication;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Hexalith.Projects.Server.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

/// <summary>Owned production-mode broker, issuer and receiver hosts over real persisted Project and Folder handlers.</summary>
internal sealed class ProjectReadAuthorityFixture : IAsyncDisposable
{
    /// <summary>The separate synthetic public-user issuer.</summary>
    internal const string UserIssuer = "https://synthetic-users.example.test";
    /// <summary>The synthetic public gateway audience.</summary>
    internal const string UserAudience = "configured-read-audience";
    private readonly string _directory = Directory.CreateTempSubdirectory("projects-authority-").FullName;
    private readonly string _password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private readonly RSA _userKey = RSA.Create(2048);
    private readonly RSA _ownerKey = RSA.Create(2048);
    private readonly List<WebApplication> _hosts = [];
    private readonly X509Certificate2 _certificate;
    private readonly bool _failCleanup;
    private bool _disposed;
    private bool _certificateDisposed;
    private bool _userKeyDisposed;
    private bool _ownerKeyDisposed;
    private readonly string _http;
    private readonly string _grpc;
    private readonly Dictionary<string, string> _secrets = new(StringComparer.Ordinal)
    {
        ["eventstore"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
        ["projects"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
        ["folders"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
    };

    private ProjectReadAuthorityFixture(string http, string grpc, bool failCleanup)
    {
        _http = http; _grpc = grpc; _failCleanup = failCleanup;
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(_directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        using RSA signer = RSA.Create(2048);
        var request = new CertificateRequest("CN=synthetic-projects-authority", signer, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        var names = new SubjectAlternativeNameBuilder(); names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        _certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow.AddHours(1));
        string path = Path.Combine(_directory, "synthetic.pfx");
        File.WriteAllBytes(path, _certificate.Export(X509ContentType.Pkcs12, _password));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    /// <summary>Gets the unique tenant explicitly enrolled by the fixture.</summary>
    public string Tenant { get; } = "rsa-" + Guid.NewGuid().ToString("N");
    /// <summary>Gets a populated foreign tenant with overlapping opaque resource IDs.</summary>
    public string ForeignTenant { get; } = "foreign-" + Guid.NewGuid().ToString("N");
    /// <summary>Gets attempted cleanup stages, including stages whose real operation failed.</summary>
    public List<string> CleanupAttempts { get; } = [];
    /// <summary>Gets whether the fixture-owned private directory still exists.</summary>
    public bool PrivateDirectoryExists => Directory.Exists(_directory);
    /// <summary>Gets whether all private cryptographic objects were disposed.</summary>
    public bool PrivateKeysDisposed => _certificateDisposed && _userKeyDisposed && _ownerKeyDisposed;
    /// <summary>Gets the ephemeral application-channel credential.</summary>
    public string Channel { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    /// <summary>Gets local service-discovery substitutions and observations.</summary>
    public ProjectAuthorityRoutes Routes { get; } = new();
    /// <summary>Gets deterministic external owner-head evidence.</summary>
    public GatewayFixtureOwnerHeads Heads { get; } = new();
    /// <summary>Gets the fixture authorization evidence time.</summary>
    public DateTimeOffset Now { get; } = DateTimeOffset.UtcNow;
    /// <summary>Gets the exact owned HTTPS workload broker issuer.</summary>
    public string Issuer { get; private set; } = "https://127.0.0.1/workload-authority";
    /// <summary>Gets the broker current loaded configuration.</summary>
    public WorkloadAuthorityOptions AuthorityOptions { get; private set; } = null!;
    /// <summary>Gets the production-mode Projects receiver.</summary>
    public WebApplication Projects { get; private set; } = null!;
    /// <summary>Gets the production-mode Folder receiver.</summary>
    public WebApplication Folders { get; private set; } = null!;
    /// <summary>Gets the real public and owner gateway.</summary>
    public WebApplication Gateway { get; private set; } = null!;
    /// <summary>Gets the authenticated public gateway client.</summary>
    public HttpClient Client { get; private set; } = null!;
    /// <summary>Gets durable audit write observations.</summary>
    public ProjectAuthorityAuditObserver Audit { get; private set; } = null!;

    /// <summary>Starts owned hosts and seeds real persisted domain projections.</summary>
    public static async Task<ProjectReadAuthorityFixture> CreateAsync(bool failCleanup = false)
    {
        string? http = Environment.GetEnvironmentVariable("STORY61_DAPR_HTTP_ENDPOINT"), grpc = Environment.GetEnvironmentVariable("STORY61_DAPR_GRPC_ENDPOINT");
        Assert.True(http is not null && grpc is not null, "Qualification requires owned isolated STORY61_DAPR_HTTP_ENDPOINT and STORY61_DAPR_GRPC_ENDPOINT; missing persistence is a failure.");
        var fixture = new ProjectReadAuthorityFixture(http!, grpc!, failCleanup);
        try { await fixture.StartAsync().ConfigureAwait(false); return fixture; }
        catch { await fixture.DisposeAsync().ConfigureAwait(false); throw; }
    }

    /// <summary>Signs a synthetic user credential using separate public trust.</summary>
    public string HumanToken(bool inspection = false, bool projectUser = false)
    {
        List<Claim> claims = [new("sub", "actor-1"), new("tenant_id", Tenant), new("eventstore:tenant", Tenant),
            new("azp", projectUser ? "chatbot" : "operator-web"), new("client_id", "issuing-client"),
            new("act", "{\"sub\":\"delegation-1\"}"), new("scope", "projects.read projects.list"),
            new("projects:role", projectUser ? "ProjectUser" : "TenantOperator"), new("eventstore:permission", "query:read"),
            new("eventstore:permission", "read_metadata"), new("eventstore:permission", ProjectAuthorizationGate.ReadProjectAction),
            new("eventstore:permission", ProjectAuthorizationGate.ListProjectsAction)];
        if (inspection) claims.Add(new("eventstore:permission", ProjectReadQueryExecutor.InspectionPermission));
        var handler = new JwtSecurityTokenHandler(); handler.OutboundClaimTypeMap.Clear();
        return handler.CreateEncodedJwt(new SecurityTokenDescriptor { Issuer = UserIssuer, Audience = UserAudience,
            NotBefore = DateTime.UtcNow.AddSeconds(-1), Expires = DateTime.UtcNow.AddMinutes(5), Subject = new(claims),
            SigningCredentials = new(new RsaSecurityKey(_userKey) { KeyId = "synthetic-user" }, SecurityAlgorithms.RsaSha256) });
    }

    /// <summary>Signs an otherwise valid workload assertion using the wrong user issuer and key.</summary>
    public string UserIssuedWorkloadAssertion(QueryEnvelope query)
    {
        // Every workload field is otherwise valid, so only the independent issuer/key trust prevents impersonation.
        List<Claim> claims = [new(EventStoreWorkloadAuthenticationDefaults.CallerClaimType, "eventstore"),
            new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
            .. QueryCallerAuthorityBinding.Bind(query).Select(binding => new Claim(binding.Key, binding.Value))];
        return new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = UserIssuer, Audience = query.Domain, Subject = new(claims),
            NotBefore = DateTime.UtcNow.AddSeconds(-1), IssuedAt = DateTime.UtcNow, Expires = DateTime.UtcNow.AddSeconds(60),
            SigningCredentials = new(new RsaSecurityKey(_userKey) { KeyId = "synthetic-user" }, SecurityAlgorithms.RsaSha256),
        });
    }
    /// <summary>Creates the supported singleton query.</summary>
    public SubmitQueryRequest Open(bool inspection = false, string id = "project-a")
        => new(Tenant, "projects", id, GetProjectQuery.QueryType, Payload: JsonSerializer.SerializeToElement(new GetProjectQuery(inspection), JsonSerializerOptions.Web));
    /// <summary>Creates the supported tenant enumeration query.</summary>
    public SubmitQueryRequest List(ListProjectsQuery? query = null)
        => new(Tenant, "projects", "projects", ListProjectsQuery.QueryType, Payload: JsonSerializer.SerializeToElement(query ?? new(), JsonSerializerOptions.Web));
    /// <summary>Executes a public gateway query and requires real protected success.</summary>
    public async Task<T> ReadAsync<T>(SubmitQueryRequest query)
    {
        using HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/queries", query, TestContext.Current.CancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
        return JsonSerializer.Deserialize<SubmitQueryResponse>(body, JsonSerializerOptions.Web)!.Payload.Deserialize<T>(JsonSerializerOptions.Web)!;
    }
    /// <summary>Requires one scenario-specific safe closed Problem Details response and returns its canonical stable outcome.</summary>
    public async Task<string> RefusedAsync(SubmitQueryRequest query, HttpStatusCode expected)
    {
        expected.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
        using HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/queries", query, TestContext.Current.CancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.StatusCode.ShouldBe(expected, body);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement problem = document.RootElement;
        string[] fields = ["type", "title", "status", "detail", "instance", "correlationId", "reasonCode"];
        if (expected == HttpStatusCode.InternalServerError) fields = [.. fields, "retryAfter", "degradation", "reason"];
        problem.EnumerateObject().Select(property => property.Name).Order().ShouldBe(fields.Order());
        problem.GetProperty("status").GetInt32().ShouldBe((int)expected);
        problem.GetProperty("instance").GetString().ShouldBe("/api/v1/queries");
        string correlation = problem.GetProperty("correlationId").GetString()!;
        Hexalith.EventStore.Middleware.CorrelationIdMiddleware.IsValidIdentifier(correlation).ShouldBeTrue();
        bool dependency = expected == HttpStatusCode.InternalServerError;
        problem.GetProperty("type").GetString().ShouldBe(dependency ? ProblemTypeUris.InternalServerError : ProblemTypeUris.NotFound);
        problem.GetProperty("title").GetString().ShouldBe(dependency ? "Query Failed" : "Not Found");
        problem.GetProperty("detail").GetString().ShouldBe(dependency ? "actor-exception" : "The requested resource was not found.");
        problem.GetProperty("reasonCode").GetString().ShouldBe(dependency ? QueryProblemReasonCodes.InternalError : QueryProblemReasonCodes.ProjectionMissing);
        if (dependency)
        {
            problem.GetProperty("retryAfter").GetString().ShouldBe("30");
            problem.GetProperty("degradation").GetString().ShouldBe("projection-query-unavailable");
            problem.GetProperty("reason").GetString().ShouldBe("Projection query could not be completed safely. Retry after the advised interval.");
            response.Headers.RetryAfter!.Delta.ShouldBe(TimeSpan.FromSeconds(30));
        }
        foreach (string sensitive in new[] { Tenant, ForeignTenant, "actor-1", "foreign-actor", "project-a", "project-b", "folder-project",
            "Project project", "Foreign project", "Safe description", "Foreign secret", "Owner label", "Foreign owner", "delegation-1",
            "operator-web", "issuing-client", "synthetic-client", "projects.read", Issuer, UserIssuer, Channel, _directory }) body.ShouldNotContain(sensitive);
        return JsonSerializer.Serialize(problem.EnumerateObject().Where(property => property.Name != "correlationId")
            .OrderBy(property => property.Name, StringComparer.Ordinal).ToDictionary(property => property.Name, property => property.Value));
    }
    private string[] DomainKeys => new[] { Tenant, ForeignTenant }.SelectMany(tenant => new[]
    {
        ProjectReadModelKeys.Index(tenant), ProjectReadModelKeys.Detail(tenant, "project-a"), ProjectReadModelKeys.Detail(tenant, "project-b"),
        "projects:tenant-access:" + tenant, "projection:folders:tenant-access:" + tenant,
        "projection:folders:folder-permissions:" + tenant + ":folder-project-a", "projection:folders:folder-permissions:" + tenant + ":folder-project-b",
    }).ToArray();
    /// <summary>Reads physical domain and optional audit bytes plus ETags using a fresh HTTP client.</summary>
    public async Task<ProjectAuthoritySnapshot[]> SnapshotAsync(params string[] additional)
    {
        string[] keys = [.. DomainKeys, .. additional];
        using HttpClient state = new() { BaseAddress = new(_http) };
        List<ProjectAuthoritySnapshot> snapshots = [];
        foreach (string key in keys)
        {
            using HttpResponseMessage response = await state.GetAsync("/v1.0/state/statestore/" + Uri.EscapeDataString(key), TestContext.Current.CancellationToken).ConfigureAwait(false);
            response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
            byte[] bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            snapshots.Add(new(key, bytes, response.Headers.TryGetValues("ETag", out var etags) ? etags.Single() : string.Empty));
        }
        return [.. snapshots];
    }
    /// <summary>Requires exact persisted bytes and ETags to survive the observed reads.</summary>
    public async Task AssertUnchangedAsync(ProjectAuthoritySnapshot[] before)
    {
        ProjectAuthoritySnapshot[] after = await SnapshotAsync(before.Select(item => item.Key).Skip(DomainKeys.Length).ToArray()).ConfigureAwait(false);
        for (int i = 0; i < before.Length; i++)
        {
            after[i].Key.ShouldBe(before[i].Key); after[i].Bytes.ShouldBe(before[i].Bytes, before[i].Key); after[i].ETag.ShouldBe(before[i].ETag, before[i].Key);
        }
    }
    /// <summary>Seeds a real owner ACL revocation before observing subsequent read effects.</summary>
    public async Task RevokeFolderAsync()
    {
        await Folders.Services.GetRequiredService<EventStoreFolderEffectivePermissionsReadModel>().ApplyAsync(Tenant, "folder-project-a", "revoked", 3, "revoked",
            new FolderAccessRevoked(Tenant, Tenant, "folder-project-a", FolderAccessPrincipalKind.User, "actor-1", "read_metadata", "actor-1",
                "corr", "task", "revoke", "digest", 3, Now), TestContext.Current.CancellationToken).ConfigureAwait(false);
        Heads.Set(Tenant, "folder-project-a", 3);
    }
    /// <summary>Removes the owned synthetic owner projection to exercise actual absent-owner behavior.</summary>
    public async Task RemoveFolderAsync()
    {
        using DaprClient dapr = new DaprClientBuilder().UseHttpEndpoint(_http).UseGrpcEndpoint(_grpc).Build();
        await dapr.DeleteStateAsync("statestore", "projection:folders:folder-permissions:" + Tenant + ":folder-project-a", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Heads.Set(Tenant, "folder-project-a", 0);
    }
    /// <summary>Independently validates actual broker signatures, lifetimes, grants and optional exact bindings.</summary>
    public async Task VerifyHopAsync(ProjectAuthorityHop hop, bool exact = true)
    {
        var token = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(hop.Assertion);
        token.Alg.ShouldBe("RS256"); token.Issuer.ShouldBe(Issuer);
        token.GetClaim("eventstore:grant-id").Value.ShouldBe("gateway-to-" + hop.Target);
        (token.ValidTo - token.IssuedAt).ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(120));
        var keys = new JsonWebKeySet(JsonSerializer.Serialize(_hosts[0].Services.GetRequiredService<WorkloadBindingAuthority>().GetPublicKeys()));
        var verified = await new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().ValidateTokenAsync(hop.Assertion, new TokenValidationParameters
        {
            ValidIssuer = Issuer, ValidAudience = hop.Target, IssuerSigningKeys = keys.GetSigningKeys(),
            ValidAlgorithms = ["RS256"], ValidateIssuerSigningKey = true, RequireSignedTokens = true, ClockSkew = TimeSpan.Zero,
        }).ConfigureAwait(false);
        verified.IsValid.ShouldBeTrue();
        if (exact)
        {
            foreach (var binding in QueryCallerAuthorityBinding.Bind(hop.Query)) token.GetClaim(binding.Key).Value.ShouldBe(binding.Value);
            token.Claims.Count(claim => claim.Type.StartsWith(EventStoreWorkloadAuthenticationDefaults.BindingClaimPrefix, StringComparison.Ordinal)).ShouldBe(3);
        }
    }
    /// <summary>Gets a client current loaded credential/profile options.</summary>
    public WorkloadAssertionIssuerOptions ClientOptions(string workload) => Host(workload).Services.GetRequiredService<IOptionsMonitor<WorkloadAssertionIssuerOptions>>().CurrentValue;
    /// <summary>Rebinds named client trust from a changed configuration snapshot.</summary>
    public void ReplaceTrustIssuer(string workload, string issuer)
    {
        Host(workload).Configuration["Authentication:WorkloadJwtBearer:Issuer"] = issuer;
        Host(workload).Services.GetRequiredService<IOptionsMonitorCache<JwtBearerAuthenticationOptions>>().TryRemove(EventStoreWorkloadAuthenticationDefaults.JwtContractOptionsName);
    }
    /// <summary>Finds one owned workload host.</summary>
    public WebApplication Host(string workload) => workload switch { "eventstore" => Gateway, "projects" => Projects, "folders" => Folders, _ => throw new ArgumentException("Unknown fixture host.", nameof(workload)) };

    /// <summary>Attempts every owned cleanup, always removes private material, and then reports all failures.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        List<Exception> failures = [];
        async ValueTask AttemptAsync(string stage, Func<ValueTask> cleanup)
        {
            CleanupAttempts.Add(stage);
            try { await cleanup().ConfigureAwait(false); }
            catch (Exception exception) { failures.Add(exception); }
        }
        await AttemptAsync("client", () => { Client?.Dispose(); return ValueTask.CompletedTask; }).ConfigureAwait(false);
        for (int i = _hosts.Count - 1; i >= 0; i--)
        {
            WebApplication app = _hosts[i];
            await AttemptAsync($"host-{i}-stop", () => new(app.StopAsync(CancellationToken.None))).ConfigureAwait(false);
            await AttemptAsync($"host-{i}-dispose", app.DisposeAsync).ConfigureAwait(false);
        }
        await AttemptAsync("certificate", () => { _certificate.Dispose(); _certificateDisposed = true; return ValueTask.CompletedTask; }).ConfigureAwait(false);
        await AttemptAsync("user-key", () => { _userKey.Dispose(); _userKeyDisposed = true; return ValueTask.CompletedTask; }).ConfigureAwait(false);
        await AttemptAsync("owner-key", () => { _ownerKey.Dispose(); _ownerKeyDisposed = true; return ValueTask.CompletedTask; }).ConfigureAwait(false);
        await AttemptAsync("private-directory", () => { Directory.Delete(_directory, true); return ValueTask.CompletedTask; }).ConfigureAwait(false);
        if (failures.Count > 0) throw new AggregateException("Owned fixture cleanup failed after every cleanup was attempted.", failures);
    }

    /// <summary>Creates a validly signed synthetic receiver-boundary control without modifying receiver trust.</summary>
    public string BoundaryAssertion(QueryEnvelope query, string audience, DateTimeOffset issuedAt)
    {
        using RSA signer = _certificate.GetRSAPrivateKey()!;
        var claims = new Dictionary<string, object>
        {
            [EventStoreWorkloadAuthenticationDefaults.CallerClaimType] = "eventstore",
            [EventStoreWorkloadAuthenticationDefaults.OperationClaimType] = EventStoreWorkloadOperations.DomainServiceQuery,
            ["eventstore:grant-id"] = "gateway-to-" + query.Domain, ["eventstore:grant-revision"] = 1,
        };
        foreach (var binding in QueryCallerAuthorityBinding.Bind(query)) claims.Add(binding.Key, binding.Value);
        return new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer, Audience = audience, IssuedAt = issuedAt.UtcDateTime, NotBefore = issuedAt.UtcDateTime,
            Expires = issuedAt.AddSeconds(120).UtcDateTime, Claims = claims,
            SigningCredentials = new(new RsaSecurityKey(signer) { KeyId = AuthorityOptions.KeyId }, SecurityAlgorithms.RsaSha256)
            { CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false } },
        });
    }
    /// <summary>Checks the boundary control signature and audience independently; receiver lifetime checks stay untouched.</summary>
    public async Task VerifyBoundarySignatureAsync(string token, string audience)
    {
        var keys = new JsonWebKeySet(JsonSerializer.Serialize(_hosts[0].Services.GetRequiredService<WorkloadBindingAuthority>().GetPublicKeys()));
        var result = await new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Issuer, ValidAudience = audience, IssuerSigningKeys = keys.GetSigningKeys(), ValidAlgorithms = ["RS256"],
            ValidateIssuerSigningKey = true, RequireSignedTokens = true, ValidateLifetime = false,
        }).ConfigureAwait(false);
        result.IsValid.ShouldBeTrue();
    }
    /// <summary>Sends malformed or ordinary OAuth form controls through the same passive observer to the actual broker.</summary>
    public async Task<HttpStatusCode> SendTokenControlAsync(string fault)
    {
        using HttpClient client = new(new ProjectAuthorityBrokerHandler(Routes, TrustedBrokerHandler()));
        Dictionary<string, string> fields = new()
        {
            ["grant_type"] = "client_credentials", ["client_id"] = "eventstore-synthetic-client", ["client_secret"] = _secrets["eventstore"],
            ["scope"] = new WorkloadAssertionIssuerOptions().BuildScope("projects", EventStoreWorkloadOperations.DomainServiceQuery),
        };
        if (fault == "malformed") fields["authorization_details"] = "not-json";
        using var form = new FormUrlEncodedContent(fields);
        using HttpResponseMessage response = await client.PostAsync(Issuer + "/token", form, TestContext.Current.CancellationToken).ConfigureAwait(false);
        return response.StatusCode;
    }

    private HttpClientHandler TrustedBrokerHandler() => new()
    {
        AllowAutoRedirect = false,
        ServerCertificateCustomValidationCallback = (_, certificate, _, _) => certificate?.Thumbprint == _certificate.Thumbprint,
    };
    private WebApplicationBuilder Builder(string workload)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Configuration["urls"] = "http://127.0.0.1:0"; builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["APP_API_TOKEN"] = Channel, ["Authentication:JwtBearer:Authority"] = UserIssuer,
            ["Authentication:JwtBearer:Issuer"] = UserIssuer, ["Authentication:JwtBearer:Audience"] = UserAudience,
            ["Authentication:JwtBearer:AllowedAlgorithms:0"] = "RS256", ["Authentication:JwtBearer:RequireHttpsMetadata"] = "true",
            ["Authentication:WorkloadJwtBearer:Authority"] = Issuer, ["Authentication:WorkloadJwtBearer:Issuer"] = Issuer,
            ["Authentication:WorkloadJwtBearer:Audience"] = workload, ["Authentication:WorkloadJwtBearer:AllowedAlgorithms:0"] = "RS256",
            ["Authentication:WorkloadJwtBearer:RequireHttpsMetadata"] = "true",
            ["Authentication:WorkloadIssuer:Workload"] = workload, ["Authentication:WorkloadIssuer:ClientId"] = workload + "-synthetic-client",
            ["Authentication:WorkloadIssuer:ClientSecret"] = _secrets[workload], ["Authentication:WorkloadIssuer:TokenEndpoint"] = Issuer + "/token",
            ["Authentication:WorkloadIssuer:EnableResourceBindingRequests"] = "true",
        });
        builder.Services.AddSingleton(_ => new DaprClientBuilder().UseHttpEndpoint(_http).UseGrpcEndpoint(_grpc).Build());
        builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        builder.Services.AddHttpContextAccessor(); builder.Services.AddAuthorization(); builder.Services.AddEventStoreWorkloadAssertionIssuer();
        builder.Services.AddHttpClient(JwtWorkloadAssertionIssuer.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new ProjectAuthorityBrokerHandler(Routes, TrustedBrokerHandler()));
        builder.Services.AddHttpClient(Options.DefaultName).ConfigurePrimaryHttpMessageHandler(() => new ProjectAuthorityLoopbackHandler(Routes, Channel));
        builder.Services.PostConfigureAll<JwtBearerOptions>(options => options.BackchannelHttpHandler = TrustedBrokerHandler());
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var metadata = new OpenIdConnectConfiguration { Issuer = UserIssuer };
            metadata.SigningKeys.Add(new RsaSecurityKey(_userKey) { KeyId = "synthetic-user" }); options.Configuration = metadata;
        });
        return builder;
    }

    private async Task StartAsync()
    {
        await StartAuthorityAsync().ConfigureAwait(false);
        var folderBuilder = Builder("folders");
        folderBuilder.Services.AddFoldersProductionAuthentication(folderBuilder.Configuration, folderBuilder.Environment);
        folderBuilder.Services.AddFoldersServer(); folderBuilder.Services.AddEventStoreReadModelStore();
        folderBuilder.Services.AddSingleton<IFolderRepository, InMemoryFolderRepository>();
        folderBuilder.Services.AddSingleton<Hexalith.Folders.Projections.TenantAccess.IUtcClock>(new Hexalith.Folders.Projections.TenantAccess.FixedUtcClock(Now.AddSeconds(1)));
        folderBuilder.Services.AddSingleton<IOrganizationStreamSequenceReader>(Heads);
        folderBuilder.Services.AddSingleton<EventStoreOrganizationEffectivePermissionsReadModel>();
        folderBuilder.Services.AddSingleton<EventStoreFolderEffectivePermissionsReadModel>();
        folderBuilder.Services.AddSingleton<IEffectivePermissionsReadModel>(sp => sp.GetRequiredService<EventStoreFolderEffectivePermissionsReadModel>());
        folderBuilder.Services.AddSingleton<IFolderTenantAccessProjectionStore, EventStoreFolderTenantAccessProjectionStore>();
        folderBuilder.Services.AddSingleton<IEventStoreAuthorizationValidator, VerifiedReceiptEventStoreAuthorizationValidator>();
        folderBuilder.Services.AddAuthentication().AddEventStoreWorkloadScheme(EventStoreWorkloadAuthenticationDefaults.WorkloadScheme, "Authentication:Workload",
            (options, _) => { options.Audience = "folders"; options.AllowedCallers.Add("eventstore"); });
        folderBuilder.Services.AddEventStoreWorkloadPolicies(EventStoreWorkloadAuthenticationDefaults.WorkloadScheme,
            [EventStoreWorkloadOperations.DomainServiceQuery, EventStoreWorkloadOperations.DomainServiceMetadata]);
        Folders = folderBuilder.Build(); _hosts.Add(Folders); Folders.UseAuthentication(); Folders.UseAuthorization(); Folders.MapFoldersDomainServiceEndpoints();
        await Folders.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false); Routes.Add("folders", Folders);
        var projectsBuilder = Builder("projects");
        projectsBuilder.Services.AddProjectsAuthentication(projectsBuilder.Configuration, projectsBuilder.Environment);
        projectsBuilder.Configuration["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(_ownerKey.ExportPkcs8PrivateKey());
        projectsBuilder.Services.AddProjectsServer(); projectsBuilder.Services.AddProjectsServerRuntimeInfrastructure();
        projectsBuilder.Services.AddSingleton<Hexalith.Projects.Projections.TenantAccess.IUtcClock>(new ProjectReadTestClock(Now.AddSeconds(1)));
        projectsBuilder.Services.AddSingleton<TimeProvider>(new ProjectReadTestClock(Now.AddSeconds(1)));
        projectsBuilder.Services.AddSingleton(sp => new ProjectAuthorityAuditObserver(sp.GetRequiredService<IReadModelStore>()));
        projectsBuilder.Services.AddSingleton(sp => new ProjectReadAudit(sp.GetRequiredService<ProjectAuthorityAuditObserver>(), sp.GetRequiredService<TimeProvider>()));
        Projects = projectsBuilder.Build(); _hosts.Add(Projects); Projects.UseAuthentication(); Projects.UseAuthorization(); Projects.MapProjectsServerEndpoints();
        await Projects.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false); Routes.Add("projects", Projects);
        Audit = Projects.Services.GetRequiredService<ProjectAuthorityAuditObserver>();
        await StartGatewayAsync().ConfigureAwait(false);
        await SeedAsync().ConfigureAwait(false);
        Client = new() { BaseAddress = new(Gateway.Urls.Single()) }; Client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken());
    }

    private async Task StartAuthorityAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0, endpoint => endpoint.UseHttps(_certificate)));
        builder.Services.AddEventStoreWorkloadBindingAuthority(builder.Configuration);
        builder.Services.AddOptions<WorkloadAuthorityOptions>().Configure(options =>
        {
            options.Enabled = true; options.Issuer = Issuer; options.KeyId = "synthetic-projects-authority";
            options.SigningCertificatePath = Path.Combine(_directory, "synthetic.pfx"); options.SigningCertificatePassword = _password;
            options.Grants = [QueryGrant("projects"), QueryGrant("folders"), new()
            {
                GrantId = "projects-to-owner-gateway", Revision = 1, ClientId = "projects-synthetic-client", Workload = "projects", Audience = "eventstore",
                ClientSecretSha256 = Digest(_secrets["projects"]), Operations = [EventStoreWorkloadOperations.GatewayOwnerAuthorityQuery],
                AllowUnbound = true, NotBefore = Now.AddMinutes(-10), ExpiresAt = Now.AddMinutes(10),
            }];
        });
        WebApplication app = builder.Build(); _hosts.Add(app); app.MapEventStoreWorkloadBindingAuthority();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Issuer = app.Urls.Single() + "/workload-authority";
        AuthorityOptions = app.Services.GetRequiredService<IOptionsMonitor<WorkloadAuthorityOptions>>().CurrentValue; AuthorityOptions.Issuer = Issuer;
    }
    private WorkloadAuthorityGrant QueryGrant(string domain) => new()
    {
        GrantId = "gateway-to-" + domain, Revision = 1, ClientId = "eventstore-synthetic-client", Workload = "eventstore", Audience = domain,
        ClientSecretSha256 = Digest(_secrets["eventstore"]), Operations = [EventStoreWorkloadOperations.DomainServiceQuery],
        AllowQueryAuthorityDigest = true, NotBefore = Now.AddMinutes(-10), ExpiresAt = Now.AddMinutes(10),
        Bindings = new(StringComparer.Ordinal)
        {
            [EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType] = [Tenant],
            [EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType] = [domain],
        },
    };
    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private async Task StartGatewayAsync()
    {
        var builder = Builder("eventstore");
        if (_failCleanup) builder.Services.AddSingleton<IHostedService, ProjectAuthorityCleanupFailure>();
        builder.Configuration["Authentication:BoundStreamRead:PublicKeys:projects"] = Convert.ToBase64String(_ownerKey.ExportSubjectPublicKeyInfo());
        builder.Configuration["Authentication:BoundOwnerQuery:AllowedQueries:projects"] = FolderOwnerReadQuery.QueryType;
        builder.Configuration["Authentication:QueryAuthority:RequiredQueries:projects:0"] = ListProjectsQuery.QueryType;
        builder.Configuration["Authentication:QueryAuthority:RequiredQueries:projects:1"] = GetProjectQuery.QueryType;
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new() { ValidateIssuer = true, ValidIssuer = UserIssuer, ValidateAudience = true, ValidAudience = UserAudience,
                ValidateLifetime = true, ValidateIssuerSigningKey = true, RequireSignedTokens = true, ValidAlgorithms = ["RS256"] };
        }).AddEventStoreWorkloadScheme(DaprInternalAuthenticationOptions.SchemeName, "Authentication:DaprInternal",
            (options, _) => { options.Audience = "eventstore"; options.AllowedCallers.Add("projects"); });
        builder.Services.AddEventStoreWorkloadPolicies(DaprInternalAuthenticationOptions.SchemeName, [EventStoreWorkloadOperations.GatewayOwnerAuthorityQuery]);
        builder.Services.Configure<Microsoft.AspNetCore.Authorization.AuthorizationOptions>(options =>
            options.DefaultPolicy = Hexalith.EventStore.Extensions.EventStoreServiceCollectionExtensions.CreateHumanDefaultPolicy());
        builder.Services.AddControllers().AddApplicationPart(typeof(QueriesController).Assembly);
        builder.Services.AddMediatR(options => options.RegisterServicesFromAssembly(typeof(QueryEnvelope).Assembly));
        builder.Services.AddTransient<MediatR.IRequestHandler<Hexalith.EventStore.Server.Pipeline.Queries.SubmitQuery, Hexalith.EventStore.Server.Pipeline.Queries.SubmitQueryResult>, SubmitQueryHandler>();
        builder.Services.AddSingleton<ITenantValidator, ClaimsTenantValidator>(); builder.Services.AddSingleton<IRbacValidator, ClaimsRbacValidator>();
        builder.Services.AddSingleton<IETagService, GatewayFixtureNoProjectionETag>();
        builder.Services.AddSingleton<IDomainServiceResolver, GatewayFixtureRoutesResolver>();
        builder.Services.AddSingleton<IDomainQueryHandlerRegistry, DaprDomainQueryHandlerRegistry>();
        builder.Services.AddSingleton<IDomainQueryInvoker, DaprDomainQueryInvoker>();
        builder.Services.AddSingleton<IQueryRouter>(sp => new HandlerAwareQueryRouter(new GatewayFixtureUnsupportedActorRouter(), sp.GetRequiredService<IDomainQueryHandlerRegistry>(),
            sp.GetRequiredService<IDomainQueryInvoker>(), sp.GetRequiredService<ILogger<HandlerAwareQueryRouter>>()));
        builder.Services.AddTransient<DomainServiceWorkloadAssertionHandler>();
        builder.Services.AddHttpClient(Options.DefaultName).AddHttpMessageHandler<DomainServiceWorkloadAssertionHandler>();
        builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<QueryNotFoundExceptionHandler>(); builder.Services.AddExceptionHandler<QueryExecutionFailedExceptionHandler>();
        Gateway = builder.Build(); _hosts.Add(Gateway); Gateway.UseMiddleware<Hexalith.EventStore.Middleware.CorrelationIdMiddleware>(); Gateway.UseExceptionHandler(); Gateway.UseAuthentication(); Gateway.UseAuthorization(); Gateway.MapControllers();
        await Gateway.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false); Routes.Add("eventstore", Gateway);
    }

    private async Task SeedAsync()
    {
        foreach (string fixtureTenant in new[] { Tenant, ForeignTenant })
        {
            string actor = fixtureTenant == Tenant ? "actor-1" : "foreign-actor";
            var tenant = new ProjectTenantAccessProjection { TenantId = fixtureTenant, Enabled = true, Watermark = 1, ProjectionWatermark = fixtureTenant + ":1", LastEventTimestamp = Now };
            tenant.Principals[actor] = new(actor, "TenantOwner");
            await Projects.Services.GetRequiredService<IProjectTenantAccessProjectionStore>().SaveAsync(tenant, TestContext.Current.CancellationToken).ConfigureAwait(false);
            var folderTenant = new FolderTenantAccessProjection { TenantId = fixtureTenant, Enabled = true, Watermark = 1, ProjectionWatermark = fixtureTenant + ":1", LastEventTimestamp = Now };
            folderTenant.Principals[actor] = new(actor, "TenantOwner");
            await Folders.Services.GetRequiredService<IFolderTenantAccessProjectionStore>().SaveAsync(folderTenant, TestContext.Current.CancellationToken).ConfigureAwait(false);
            using DaprClient dapr = new DaprClientBuilder().UseHttpEndpoint(_http).UseGrpcEndpoint(_grpc).Build();
            await dapr.SaveStateAsync("statestore", "admin:query-types:projects", Projects.Services.GetServices<IDomainQueryHandler>().Where(handler => handler.Domain == "projects").Select(handler => handler.QueryType).Distinct(StringComparer.Ordinal).ToArray(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);
            await dapr.SaveStateAsync("statestore", "admin:query-types:folders", Folders.Services.GetServices<IDomainQueryHandler>().Where(handler => handler.Domain == "folders").Select(handler => handler.QueryType).Distinct(StringComparer.Ordinal).ToArray(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);
            foreach (string id in new[] { "project-a", "project-b" })
            {
                var request = SupportedProjectReadFixture.Request(id);
                request = request with { TenantId = fixtureTenant, Events = request.Events.Select(item => item with
                    { Payload = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(item.Payload).Replace("tenant-a", fixtureTenant, StringComparison.Ordinal)
                        .Replace("Project " + id, fixtureTenant == ForeignTenant ? "Foreign project " + id : "Project " + id, StringComparison.Ordinal)
                        .Replace("Safe description", fixtureTenant == ForeignTenant ? "Foreign secret description" : "Safe description", StringComparison.Ordinal)) }).ToArray() };
                var projected = await Projects.Services.GetRequiredService<ProjectReadProjectionHandler>().ProjectAsync(request, "synthetic-seed-" + fixtureTenant + "-" + id, TestContext.Current.CancellationToken).ConfigureAwait(false);
                projected.Status.ShouldBe(Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus.Completed);
                var owner = Folders.Services.GetRequiredService<EventStoreFolderEffectivePermissionsReadModel>();
                string folder = "folder-" + id;
                await owner.ApplyAsync(fixtureTenant, folder, "created", 1, "created", new FolderCreated(fixtureTenant, fixtureTenant, folder, fixtureTenant == ForeignTenant ? "Foreign owner label" : "Owner label", null, null, [],
                    FolderLifecycleState.Active, FolderRepositoryBindingState.Unbound, actor, "corr", "task", "create", "digest", Now), TestContext.Current.CancellationToken).ConfigureAwait(false);
                await owner.ApplyAsync(fixtureTenant, folder, "granted", 2, "granted", new FolderAccessGranted(fixtureTenant, fixtureTenant, folder, FolderAccessPrincipalKind.User,
                    actor, "read_metadata", actor, "corr", "task", "grant", "digest", 2, Now), TestContext.Current.CancellationToken).ConfigureAwait(false);
                Heads.Set(fixtureTenant, folder, 2);
            }
        }
    }
}
#endif
