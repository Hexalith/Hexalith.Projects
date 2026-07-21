// <copyright file="ProjectsAuthenticationContractTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

using Xunit;

/// <summary>Verifies the Projects production identity and authentication contract.</summary>
public sealed class ProjectsAuthenticationContractTests
{
    private const string TestAudience = "hexalith-projects";
    private const string TestIssuer = "https://identity.example/realms/hexalith";
    private const string TestProjectId = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    [Fact]
    public void Validate_ProductionWithoutAuthority_FailsClosed()
    {
        ValidateOptionsResult result = Validate(
            Environments.Production,
            new Dictionary<string, string?>
            {
                ["Authentication:JwtBearer:Issuer"] = "https://identity.example/realms/hexalith",
                ["Authentication:JwtBearer:Audience"] = "hexalith-projects",
            });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Authority");
    }

    [Fact]
    public void Validate_ProductionWithDevelopmentBypass_FailsClosed()
    {
        ValidateOptionsResult result = Validate(
            Environments.Production,
            new Dictionary<string, string?>
            {
                ["Authentication:JwtBearer:AllowAnonymousDevelopment"] = "true",
            });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Development environment");
    }

    [Fact]
    public void Validate_ProductionWithInsecureAuthorityOrMetadata_FailsClosed()
    {
        ValidateOptionsResult result = Validate(
            Environments.Production,
            new Dictionary<string, string?>
            {
                ["Authentication:JwtBearer:Authority"] = "http://identity.example/realms/hexalith",
                ["Authentication:JwtBearer:Issuer"] = "http://identity.example/realms/hexalith",
                ["Authentication:JwtBearer:Audience"] = "hexalith-projects",
                ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
            });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("HTTPS");
    }

    [Fact]
    public void AddProjectsAuthentication_MissingProductionConfiguration_FailsWhenOptionsResolve()
    {
        ServiceCollection services = new();
        IHostEnvironment environment = CreateEnvironment(Environments.Production);
        _ = services.AddSingleton(environment);
        _ = services.AddProjectsAuthentication(
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Authentication:JwtBearer:Issuer"] = "https://identity.example/realms/hexalith",
                    ["Authentication:JwtBearer:Audience"] = "hexalith-projects",
                }),
            environment);
        using ServiceProvider provider = services.BuildServiceProvider();

        Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ProjectsAuthenticationOptions>>().Value);
    }

    [Theory]
    [InlineData(nameof(ProjectsAuthenticationOptions.Authority))]
    [InlineData(nameof(ProjectsAuthenticationOptions.Issuer))]
    [InlineData(nameof(ProjectsAuthenticationOptions.Audience))]
    public async Task HostStartup_MissingRequiredProductionConfiguration_FailsBeforeServing(string missingSetting)
    {
        Dictionary<string, string?> values = ValidProductionConfiguration();
        _ = values.Remove($"{ProjectsAuthenticationOptions.SectionName}:{missingSetting}");
        await using WebApplication app = BuildAuthenticationHost(Environments.Production, values);

        OptionsValidationException exception = await Should.ThrowAsync<OptionsValidationException>(
            async () => await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));

        exception.Message.ShouldContain(missingSetting);
        app.Urls.ShouldBeEmpty();
    }

    [Fact]
    public async Task HostStartup_DevelopmentHttpAuthorityWithHttpsMetadata_FailsBeforeServing()
    {
        Dictionary<string, string?> values = DevelopmentOidcConfiguration(requireHttpsMetadata: true);
        await using WebApplication app = BuildAuthenticationHost(Environments.Development, values);

        OptionsValidationException exception = await Should.ThrowAsync<OptionsValidationException>(
            async () => await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));

        exception.Message.ShouldContain("HTTPS metadata discovery");
        app.Urls.ShouldBeEmpty();
    }

    [Fact]
    public async Task HostStartup_DevelopmentHttpAuthorityWithoutHttpsMetadata_Starts()
    {
        await using WebApplication app = BuildAuthenticationHost(
            Environments.Development,
            DevelopmentOidcConfiguration(requireHttpsMetadata: false));

        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        try
        {
            app.Urls.ShouldNotBeEmpty();
        }
        finally
        {
            await app.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
    }

    [Fact]
    public void Validate_DevelopmentBypass_IsAcceptedOnlyWhenExplicit()
    {
        ValidateOptionsResult result = Validate(
            Environments.Development,
            new Dictionary<string, string?>
            {
                ["Authentication:JwtBearer:AllowAnonymousDevelopment"] = "true",
            });

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void AddProjectsAuthentication_ValidProductionConfigurationEnablesStrictBearerValidation()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                ["Authentication:JwtBearer:Authority"] = "https://identity.example/realms/hexalith",
                ["Authentication:JwtBearer:Issuer"] = "https://identity.example/realms/hexalith",
                ["Authentication:JwtBearer:Audience"] = "hexalith-projects",
            });

        _ = services.AddProjectsAuthentication(configuration, CreateEnvironment(Environments.Production));
        using ServiceProvider provider = services.BuildServiceProvider();

        JwtBearerOptions options = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        options.Authority.ShouldBe("https://identity.example/realms/hexalith");
        options.TokenValidationParameters.ValidateIssuer.ShouldBeTrue();
        options.TokenValidationParameters.ValidateAudience.ShouldBeTrue();
        options.TokenValidationParameters.ValidateLifetime.ShouldBeTrue();
        options.MapInboundClaims.ShouldBeFalse();
    }

    [Fact]
    public void IsAnonymousDevelopmentBypass_DoesNotActivateInProduction()
    {
        IConfiguration configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                ["Authentication:JwtBearer:AllowAnonymousDevelopment"] = "true",
            });

        ProjectsAuthenticationServiceCollectionExtensions.IsAnonymousDevelopmentBypass(
            configuration,
            CreateEnvironment(Environments.Production)).ShouldBeFalse();
    }

    [Fact]
    public void AuthenticationModeSelection_UsesTheSameBinderBooleanRulesAsValidatedOptions()
    {
        IConfiguration configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                ["Authentication:JwtBearer:AllowAnonymousDevelopment"] = "  true  ",
            });
        IHostEnvironment environment = CreateEnvironment(Environments.Development);
        ServiceCollection services = new();
        _ = services.AddSingleton(environment);
        _ = services.AddProjectsAuthentication(configuration, environment);
        using ServiceProvider provider = services.BuildServiceProvider();

        ProjectsAuthenticationServiceCollectionExtensions.IsAnonymousDevelopmentBypass(configuration, environment)
            .ShouldBeTrue();
        provider.GetRequiredService<IOptions<ProjectsAuthenticationOptions>>().Value.AllowAnonymousDevelopment
            .ShouldBeTrue();
    }

    [Fact]
    public void JwtValidationParameters_RejectWrongAudience()
    {
        JwtBearerOptions options = CreateJwtBearerOptions();
        SymmetricSecurityKey signingKey = CreateSigningKey();
        string token = CreateToken(
            signingKey,
            issuer: "https://identity.example/realms/hexalith",
            audience: "wrong-audience",
            expires: DateTime.UtcNow.AddMinutes(5));
        TokenValidationParameters parameters = options.TokenValidationParameters.Clone();
        parameters.IssuerSigningKey = signingKey;

        Should.Throw<SecurityTokenInvalidAudienceException>(
            () => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _));
    }

    [Fact]
    public void JwtValidationParameters_RejectExpiredToken()
    {
        JwtBearerOptions options = CreateJwtBearerOptions();
        SymmetricSecurityKey signingKey = CreateSigningKey();
        string token = CreateToken(
            signingKey,
            issuer: "https://identity.example/realms/hexalith",
            audience: "hexalith-projects",
            expires: DateTime.UtcNow.AddMinutes(-5));
        TokenValidationParameters parameters = options.TokenValidationParameters.Clone();
        parameters.IssuerSigningKey = signingKey;

        Should.Throw<SecurityTokenExpiredException>(
            () => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _));
    }

    [Fact]
    public async Task JwtMiddleware_ValidTokenAuthenticatesAndNormalizesOnlyValidatedClaims()
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync().ConfigureAwait(true);
        try
        {
            string token = CreateToken(
                CreateSigningKey(),
                TestIssuer,
                TestAudience,
                DateTime.UtcNow.AddMinutes(5),
                [
                    new Claim("sub", "actor-a"),
                    new Claim("tenant_id", "tenant-a"),
                    new Claim("permissions", "[\"projects:read\"]"),
                    new Claim("azp", "projects-gateway"),
                    new Claim("scope", "projects.read projects.list"),
                ]);
            using HttpClient client = CreateClient(app, token);

            using HttpResponseMessage response = await client
                .GetAsync("/authentication-contract", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("actor").GetString().ShouldBe("actor-a");
            document.RootElement.GetProperty("tenant").GetString().ShouldBe("tenant-a");
            document.RootElement.GetProperty("permissions")[0].GetString().ShouldBe("projects:read");
            document.RootElement.GetProperty("scopes")[0].GetString().ShouldBe("projects.read projects.list");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task JwtMiddleware_WrongAudienceAndExpiredCredentials_AreRejected()
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync().ConfigureAwait(true);
        try
        {
            string[] invalidTokens =
            [
                CreateToken(CreateSigningKey(), TestIssuer, "wrong-audience", DateTime.UtcNow.AddMinutes(5)),
                CreateToken(CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(-5)),
            ];

            foreach (string token in invalidTokens)
            {
                using HttpClient client = CreateClient(app, token);
                using HttpResponseMessage response = await client
                    .GetAsync("/authentication-contract", TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);

                response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
                (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
                    .ShouldNotContain(TestProjectId);
            }
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ProtectedRead_ValidScopeWithoutRequiredPermission_ReturnsSafeDenial()
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync().ConfigureAwait(true);
        try
        {
            string token = CreateToken(
                CreateSigningKey(),
                TestIssuer,
                TestAudience,
                DateTime.UtcNow.AddMinutes(5),
                [
                    new Claim("sub", "actor-a"),
                    new Claim("tenant_id", "tenant-a"),
                    new Claim("scope", "projects.read"),
                ]);
            using HttpClient client = CreateClient(app, token);

            using HttpResponseMessage response = await client
                .GetAsync($"/api/v1/projects/{TestProjectId}", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            await AssertSafeDenialAsync(response).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ProtectedRead_CrossTenantHint_ReturnsSameSafeDenial()
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync().ConfigureAwait(true);
        try
        {
            string token = CreateToken(
                CreateSigningKey(),
                TestIssuer,
                TestAudience,
                DateTime.UtcNow.AddMinutes(5),
                [
                    new Claim("sub", "actor-a"),
                    new Claim("tenant_id", "tenant-a"),
                    new Claim("permissions", "[\"projects:read\"]"),
                ]);
            using HttpClient client = CreateClient(app, token);
            using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{TestProjectId}");
            request.Headers.Add("X-Hexalith-Tenant-Id", "tenant-b");

            using HttpResponseMessage response = await client
                .SendAsync(request, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            await AssertSafeDenialAsync(response).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task EventStoreGatewayHandler_ForwardsInboundBearerTokenWithoutChangingIt()
    {
        const string token = "header.payload.signature";
        DefaultHttpContext context = new();
        context.Request.Headers.Authorization = $"Bearer {token}";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "actor-a")], "validated-jwt"));
        HttpContextAccessor accessor = new() { HttpContext = context };
        CapturingHttpMessageHandler capture = new();
        using EventStoreGatewayTokenForwardingHandler forwarding = new(accessor) { InnerHandler = capture };
        using HttpMessageInvoker invoker = new(forwarding);
        using HttpRequestMessage request = new(HttpMethod.Post, "http://eventstore/api/v1/commands");

        using HttpResponseMessage response = await invoker
            .SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        capture.Authorization.ShouldNotBeNull();
        capture.Authorization.Scheme.ShouldBe("Bearer");
        capture.Authorization.Parameter.ShouldBe(token);
    }

    private static ValidateOptionsResult Validate(string environmentName, Dictionary<string, string?> values)
    {
        IConfiguration configuration = CreateConfiguration(values);
        ProjectsAuthenticationOptions options = configuration
            .GetSection(ProjectsAuthenticationOptions.SectionName)
            .Get<ProjectsAuthenticationOptions>()!;

        return new ValidateProjectsAuthenticationOptions(CreateEnvironment(environmentName))
            .Validate(null, options);
    }

    private static WebApplication BuildAuthenticationHost(
        string environmentName,
        Dictionary<string, string?> values)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions { EnvironmentName = environmentName });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        _ = builder.Configuration.AddInMemoryCollection(values);
        _ = builder.Services.AddProjectsAuthentication(builder.Configuration, builder.Environment);
        return builder.Build();
    }

    private static async Task<WebApplication> StartAuthenticatedProjectsHostAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        _ = builder.Configuration.AddInMemoryCollection(ValidProductionConfiguration());
        builder.Services.AddProjectsServer();
        builder.Services.AddProjectsServerRuntimeInfrastructure();
        _ = builder.Services.AddProjectsAuthentication(builder.Configuration, builder.Environment);
        _ = builder.Services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                OpenIdConnectConfiguration oidc = new() { Issuer = TestIssuer };
                oidc.SigningKeys.Add(CreateSigningKey());
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(oidc);
            });

        WebApplication app = builder.Build();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.MapGet(
                "/authentication-contract",
                (ClaimsPrincipal principal) => Results.Json(
                    new
                    {
                        Actor = principal.FindFirstValue(ClaimTypes.NameIdentifier),
                        Tenant = principal.FindFirstValue("eventstore:tenant"),
                        Permissions = principal.FindAll("eventstore:permission").Select(static claim => claim.Value).ToArray(),
                        Scopes = principal.FindAll("scope").Select(static claim => claim.Value).ToArray(),
                    }))
            .RequireAuthorization();
        app.MapProjectsServerEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        return app;
    }

    private static Dictionary<string, string?> ValidProductionConfiguration()
        => new()
        {
            [$"{ProjectsAuthenticationOptions.SectionName}:Authority"] = TestIssuer,
            [$"{ProjectsAuthenticationOptions.SectionName}:Issuer"] = TestIssuer,
            [$"{ProjectsAuthenticationOptions.SectionName}:Audience"] = TestAudience,
            [$"{ProjectsAuthenticationOptions.SectionName}:RequireHttpsMetadata"] = "true",
        };

    private static Dictionary<string, string?> DevelopmentOidcConfiguration(bool requireHttpsMetadata)
        => new()
        {
            [$"{ProjectsAuthenticationOptions.SectionName}:Authority"] = "http://identity.example/realms/hexalith",
            [$"{ProjectsAuthenticationOptions.SectionName}:Issuer"] = "http://identity.example/realms/hexalith",
            [$"{ProjectsAuthenticationOptions.SectionName}:Audience"] = TestAudience,
            [$"{ProjectsAuthenticationOptions.SectionName}:RequireHttpsMetadata"] = requireHttpsMetadata.ToString(),
        };

    private static HttpClient CreateClient(WebApplication app, string token)
    {
        HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task AssertSafeDenialAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldNotContain(TestProjectId);
        using JsonDocument document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("category").GetString().ShouldBe("tenant_access_denied");
        document.RootElement.GetProperty("code").GetString().ShouldBe("resource_unavailable");
        document.RootElement.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("redacted");
    }

    private static async Task StopAsync(WebApplication app)
    {
        await app.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await app.DisposeAsync().ConfigureAwait(true);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        ConfigurationBuilder builder = new();
        _ = builder.AddInMemoryCollection(values);
        return builder.Build();
    }

    private static JwtBearerOptions CreateJwtBearerOptions()
    {
        ServiceCollection services = new();
        _ = services.AddProjectsAuthentication(
            CreateConfiguration(
                new Dictionary<string, string?>
                {
                    ["Authentication:JwtBearer:Authority"] = TestIssuer,
                    ["Authentication:JwtBearer:Issuer"] = TestIssuer,
                    ["Authentication:JwtBearer:Audience"] = TestAudience,
                }),
            CreateEnvironment(Environments.Production));
        using ServiceProvider provider = services.BuildServiceProvider();
        return provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
    }

    private static SymmetricSecurityKey CreateSigningKey()
        => new(Encoding.UTF8.GetBytes("ProjectsTestSigningKeyAtLeast32Characters!"));

    private static string CreateToken(
        SecurityKey signingKey,
        string issuer,
        string audience,
        DateTime expires,
        IEnumerable<Claim>? claims = null)
    {
        IEnumerable<Claim> subjectClaims = claims ?? [new Claim("sub", "actor-a")];
        return new JwtSecurityTokenHandler().CreateEncodedJwt(
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(subjectClaims),
                Issuer = issuer,
                Audience = audience,
                NotBefore = DateTime.UtcNow.AddMinutes(-10),
                Expires = expires,
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            });
    }

    private static IHostEnvironment CreateEnvironment(string environmentName)
        => WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = environmentName }).Environment;

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
