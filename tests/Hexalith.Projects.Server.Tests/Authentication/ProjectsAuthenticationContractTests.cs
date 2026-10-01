// <copyright file="ProjectsAuthenticationContractTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Controllers;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Authentication;

using Dapr.Actors.Client;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
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
    private const string TestProjectName = "Authenticated fixture project";
    private static readonly RSAParameters SigningKeyParameters = CreateSigningKeyParameters();

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
    [InlineData(nameof(ProjectsAuthenticationOptions.Authority), null)]
    [InlineData(nameof(ProjectsAuthenticationOptions.Issuer), null)]
    [InlineData(nameof(ProjectsAuthenticationOptions.Audience), null)]
    [InlineData(nameof(ProjectsAuthenticationOptions.Authority), "")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Issuer), "")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Audience), "")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Authority), "   ")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Issuer), "   ")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Audience), "   ")]
    public async Task HostStartup_MissingOrBlankRequiredProductionConfiguration_FailsBeforeServing(
        string missingSetting,
        string? value)
    {
        Dictionary<string, string?> values = ValidProductionConfiguration();
        values[$"{ProjectsAuthenticationOptions.SectionName}:{missingSetting}"] = value;
        await using WebApplication app = BuildAuthenticationHost(Environments.Production, values);

        OptionsValidationException exception = await Should.ThrowAsync<OptionsValidationException>(
            async () => await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));

        exception.Message.ShouldContain(missingSetting);
        app.Urls.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(nameof(ProjectsAuthenticationOptions.Authority), "http://identity.example/realms/hexalith")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Issuer), "http://identity.example/realms/hexalith")]
    [InlineData(nameof(ProjectsAuthenticationOptions.RequireHttpsMetadata), "false")]
    public async Task HostStartup_EachInsecureProductionSetting_FailsBeforeServing(string setting, string value)
    {
        Dictionary<string, string?> values = ValidProductionConfiguration();
        values[$"{ProjectsAuthenticationOptions.SectionName}:{setting}"] = value;
        await using WebApplication app = BuildAuthenticationHost(Environments.Production, values);

        OptionsValidationException exception = await Should.ThrowAsync<OptionsValidationException>(
            async () => await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));

        exception.Message.ShouldContain("HTTPS");
        app.Urls.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(nameof(ProjectsAuthenticationOptions.Authority), "https://user@identity.example/realms/hexalith")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Issuer), "https://user@identity.example/realms/hexalith")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Authority), "https://identity.example/realms/hexalith?unexpected=value")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Issuer), "https://identity.example/realms/hexalith?unexpected=value")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Authority), "https://identity.example/realms/hexalith#unexpected")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Issuer), "https://identity.example/realms/hexalith#unexpected")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Authority), "/realms/hexalith")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Issuer), "/realms/hexalith")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Authority), "ftp://identity.example/realms/hexalith")]
    [InlineData(nameof(ProjectsAuthenticationOptions.Issuer), "ftp://identity.example/realms/hexalith")]
    public async Task HostStartup_InvalidAuthorityOrIssuerUri_FailsBeforeServing(string setting, string value)
    {
        Dictionary<string, string?> values = ValidProductionConfiguration();
        values[$"{ProjectsAuthenticationOptions.SectionName}:{setting}"] = value;
        await using WebApplication app = BuildAuthenticationHost(Environments.Production, values);

        OptionsValidationException exception = await Should.ThrowAsync<OptionsValidationException>(
            async () => await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));

        exception.Message.ShouldContain(setting);
        app.Urls.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task HostStartup_DevelopmentBypassOutsideDevelopment_FailsBeforeServing(string environmentName)
    {
        Dictionary<string, string?> values = ValidProductionConfiguration();
        values[$"{ProjectsAuthenticationOptions.SectionName}:AllowAnonymousDevelopment"] = "true";
        await using WebApplication app = BuildAuthenticationHost(environmentName, values);

        OptionsValidationException exception = await Should.ThrowAsync<OptionsValidationException>(
            async () => await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));

        exception.Message.ShouldContain("Development environment");
        app.Urls.ShouldBeEmpty();
    }

    [Fact]
    public async Task HostStartup_ExplicitDevelopmentBypassWithoutOidc_Starts()
    {
        await using WebApplication app = BuildAuthenticationHost(
            Environments.Development,
            new Dictionary<string, string?>
            {
                [$"{ProjectsAuthenticationOptions.SectionName}:AllowAnonymousDevelopment"] = "true",
            });

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
        options.TokenValidationParameters.RequireSignedTokens.ShouldBeTrue();
        options.MapInboundClaims.ShouldBeFalse();
        options.IncludeErrorDetails.ShouldBeFalse();
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
        RsaSecurityKey signingKey = CreateSigningKey();
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
        RsaSecurityKey signingKey = CreateSigningKey();
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

    [Theory]
    [InlineData(SecurityAlgorithms.RsaSha256)]
    [InlineData(SecurityAlgorithms.RsaSha384)]
    [InlineData(SecurityAlgorithms.RsaSha512)]
    [InlineData(SecurityAlgorithms.RsaSsaPssSha256)]
    [InlineData(SecurityAlgorithms.RsaSsaPssSha384)]
    [InlineData(SecurityAlgorithms.RsaSsaPssSha512)]
    [InlineData(SecurityAlgorithms.EcdsaSha256)]
    [InlineData(SecurityAlgorithms.EcdsaSha384)]
    [InlineData(SecurityAlgorithms.EcdsaSha512)]
    public async Task JwtMiddleware_EachPermittedAsymmetricAlgorithm_AuthenticatesUsingPublicDiscoveryKeys(string algorithm)
    {
        using ECDsa? signingEcdsa = algorithm switch
        {
            SecurityAlgorithms.EcdsaSha256 => ECDsa.Create(ECCurve.NamedCurves.nistP256),
            SecurityAlgorithms.EcdsaSha384 => ECDsa.Create(ECCurve.NamedCurves.nistP384),
            SecurityAlgorithms.EcdsaSha512 => ECDsa.Create(ECCurve.NamedCurves.nistP521),
            _ => null,
        };
        using ECDsa? validationEcdsa = signingEcdsa is null
            ? null
            : ECDsa.Create(signingEcdsa.ExportParameters(includePrivateParameters: false));
        SecurityKey signingKey = signingEcdsa is null
            ? CreateSigningKey()
            : new ECDsaSecurityKey(signingEcdsa) { KeyId = "ecdsa-fixture" };
        SecurityKey validationKey = validationEcdsa is null
            ? CreateValidationKey()
            : new ECDsaSecurityKey(validationEcdsa) { KeyId = "ecdsa-fixture" };
        WebApplication app = await StartAuthenticatedProjectsHostAsync(validationKey).ConfigureAwait(true);
        try
        {
            string token = CreateToken(signingKey, TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5), algorithm: algorithm);
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
            await AssertAccessibleProjectAsync(app, token).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData(SecurityAlgorithms.RsaSha256)]
    [InlineData(SecurityAlgorithms.HmacSha256)]
    public async Task JwtMiddleware_DevelopmentOidc_RequiresAndAcceptsSignedTokensWithoutAnonymousBypass(string algorithm)
    {
        SecurityKey signingKey = algorithm == SecurityAlgorithms.HmacSha256
            ? new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)) { KeyId = "development-fixture" }
            : CreateSigningKey();
        SecurityKey validationKey = signingKey is SymmetricSecurityKey ? signingKey : CreateValidationKey();
        WebApplication app = await StartAuthenticatedProjectsHostAsync(validationKey, environmentName: "Development")
            .ConfigureAwait(true);
        try
        {
            app.Services.GetRequiredService<IOptions<ProjectsAuthenticationOptions>>().Value.AllowAnonymousDevelopment
                .ShouldBeFalse();
            string token = CreateToken(signingKey, TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5), algorithm: algorithm);
            using HttpClient client = CreateClient(app, token);
            using HttpResponseMessage response = await client
                .GetAsync("/authentication-contract", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("actor").GetString().ShouldBe("actor-a");
            await AssertAccessibleProjectAsync(app, token).ConfigureAwait(true);

            using HttpClient unsignedClient = CreateClient(
                app,
                CreateToken(null, TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5)));
            using HttpResponseMessage unsignedResponse = await unsignedClient
                .GetAsync("/authentication-contract", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            unsignedResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            using HttpClient anonymousClient = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpResponseMessage anonymousResponse = await anonymousClient
                .GetAsync("/authentication-contract", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            anonymousResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task JwtMiddleware_ConfiguredIssuer_RemainsValidWhenDiscoveryIssuerDiffers(string environmentName)
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync(
            metadataIssuer: "https://untrusted.example/realms/hexalith",
            environmentName: environmentName).ConfigureAwait(true);
        try
        {
            using HttpClient client = CreateClient(
                app,
                CreateToken(CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5)));
            using HttpResponseMessage response = await client
                .GetAsync("/authentication-contract", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("actor").GetString().ShouldBe("actor-a");
            await AssertAccessibleProjectAsync(app).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task JwtMiddleware_UnknownOptionalEvidence_PreservesPlatformFallbacks(string? actorClaim)
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync().ConfigureAwait(true);
        try
        {
            List<Claim> claims =
            [
                new Claim("sub", "actor-a"),
                new Claim("tenant_id", "tenant-a"),
                new Claim("permissions", "[\"projects:read\"]"),
            ];
            if (actorClaim is not null)
            {
                claims.Add(new Claim("act", actorClaim));
            }

            string token = CreateToken(CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5), claims);
            using HttpClient client = CreateClient(app, token);
            using HttpResponseMessage response = await client
                .GetAsync("/authentication-contract", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            document.RootElement.GetProperty("originalActor").GetString().ShouldBe("actor-a");
            document.RootElement.GetProperty("workload").GetString().ShouldBe(TestAudience);
            document.RootElement.GetProperty("delegated").GetBoolean().ShouldBeFalse();
            document.RootElement.GetProperty("delegationId").ValueKind.ShouldBe(JsonValueKind.Null);
            document.RootElement.GetProperty("routingScopes").ValueKind.ShouldBe(JsonValueKind.Null);
            document.RootElement.GetProperty("scopes").GetArrayLength().ShouldBe(0);
            document.RootElement.GetProperty("alternateScopes").GetArrayLength().ShouldBe(0);
            document.RootElement.GetProperty("audience")[0].GetString().ShouldBe(TestAudience);
            await AssertAccessibleProjectAsync(app, token).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData("Production", "not-json")]
    [InlineData("Production", "{\"sub\":42}")]
    [InlineData("Production", "{\"sub\":\"\"}")]
    [InlineData("Production", "{}")]
    [InlineData("Production", "{\"sub\":\"one\",\"sub\":\"two\"}")]
    [InlineData("Staging", "not-json")]
    [InlineData("Staging", "{\"sub\":42}")]
    [InlineData("Staging", "{\"sub\":\"\"}")]
    [InlineData("Staging", "{}")]
    [InlineData("Staging", "{\"sub\":\"one\",\"sub\":\"two\"}")]
    public async Task JwtMiddleware_DeclaredMalformedDelegation_FailsAuthenticationAndDeniesProtectedRead(
        string environmentName,
        string actorClaim)
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync(environmentName: environmentName).ConfigureAwait(true);
        try
        {
            await AssertAccessibleProjectAsync(app).ConfigureAwait(true);
            Claim[] claims =
            [
                new Claim("sub", "actor-a"),
                new Claim("tenant_id", "tenant-a"),
                new Claim("permissions", "[\"projects:read\"]"),
                new Claim("act", actorClaim),
            ];
            ClaimsPrincipal directPrincipal = new(new ClaimsIdentity(claims, "validated-fixture"));
            DualPrincipalClaimsHelper.Extract(directPrincipal, "actor-a").DelegationId.ShouldBeNull();
            directPrincipal.FindFirst("delegationId").ShouldBeNull();

            string token = CreateToken(CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5), claims);
            using HttpClient client = CreateClient(app, token);
            using HttpResponseMessage response = await client
                .GetAsync("/authentication-contract", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            response.Headers.WwwAuthenticate.Select(static challenge => challenge.ToString()).ShouldBe(["Bearer"]);
            (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
                .ShouldBeEmpty();
            using HttpResponseMessage readResponse = await client
                .GetAsync($"/api/v1/projects/{TestProjectId}", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            await AssertSafeDenialAsync(readResponse).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData(false, "scope")]
    [InlineData(true, "scope")]
    [InlineData(false, "scp")]
    [InlineData(true, "scp")]
    public async Task JwtMiddleware_ValidTokenAuthenticatesAndPreservesDualPrincipalClaims(bool delegated, string scopeClaimType)
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync().ConfigureAwait(true);
        try
        {
            List<Claim> claims =
            [
                new Claim("sub", "actor-a"),
                new Claim("tenant_id", "tenant-a"),
                new Claim("permissions", "[\"projects:read\"]"),
                new Claim("azp", "projects-gateway"),
                new Claim(scopeClaimType, "projects.read projects.list"),
            ];
            if (delegated)
            {
                claims.Add(new Claim("act", "{\"sub\":\"delegation-a\"}"));
            }

            string token = CreateToken(CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5), claims);
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
            document.RootElement.GetProperty(scopeClaimType == "scope" ? "scopes" : "alternateScopes")[0]
                .GetString().ShouldBe("projects.read projects.list");
            document.RootElement.GetProperty("originalActor").GetString().ShouldBe("actor-a");
            document.RootElement.GetProperty("workload").GetString().ShouldBe("projects-gateway");
            document.RootElement.GetProperty("delegated").GetBoolean().ShouldBe(delegated);
            document.RootElement.GetProperty("delegationId").GetString().ShouldBe(delegated ? "delegation-a" : null);
            document.RootElement.GetProperty("routingScopes").EnumerateArray().Select(static scope => scope.GetString())
                .ShouldBe(["projects.read", "projects.list"]);
            document.RootElement.GetProperty("audience")[0].GetString().ShouldBe(TestAudience);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData("Production", "wrong-issuer")]
    [InlineData("Production", "different-metadata-issuer")]
    [InlineData("Production", "wrong-audience")]
    [InlineData("Production", "audience-trailing-slash")]
    [InlineData("Production", "expired")]
    [InlineData("Production", "invalid-signature")]
    [InlineData("Production", "unsigned")]
    [InlineData("Staging", "wrong-issuer")]
    [InlineData("Staging", "different-metadata-issuer")]
    [InlineData("Staging", "wrong-audience")]
    [InlineData("Staging", "audience-trailing-slash")]
    [InlineData("Staging", "expired")]
    [InlineData("Staging", "invalid-signature")]
    [InlineData("Staging", "unsigned")]
    public async Task JwtMiddleware_InvalidCredentials_AreRejectedWithoutDisclosingProtectedMetadata(
        string environmentName,
        string invalidCase)
    {
        const string untrustedIssuer = "https://untrusted.example/realms/hexalith";
        WebApplication app = await StartAuthenticatedProjectsHostAsync(
            metadataIssuer: invalidCase == "different-metadata-issuer" ? untrustedIssuer : TestIssuer,
            environmentName: environmentName)
            .ConfigureAwait(true);
        try
        {
            await AssertAccessibleProjectAsync(app).ConfigureAwait(true);
            string token = invalidCase switch
            {
                "wrong-issuer" or "different-metadata-issuer" => CreateToken(CreateSigningKey(), untrustedIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5)),
                "wrong-audience" => CreateToken(CreateSigningKey(), TestIssuer, "wrong-audience", DateTime.UtcNow.AddMinutes(5)),
                "audience-trailing-slash" => CreateToken(CreateSigningKey(), TestIssuer, TestAudience + "/", DateTime.UtcNow.AddMinutes(5)),
                "expired" => CreateToken(CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(-5)),
                "invalid-signature" => CreateToken(new RsaSecurityKey(CreateSigningKeyParameters()) { KeyId = "projects-fixture" }, TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5)),
                "unsigned" => CreateToken(null, TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5)),
                _ => throw new ArgumentOutOfRangeException(nameof(invalidCase)),
            };
            using HttpClient client = CreateClient(app, token);
            using HttpResponseMessage response = await client
                .GetAsync("/authentication-contract", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            response.Headers.WwwAuthenticate.Select(static challenge => challenge.ToString()).ShouldBe(["Bearer"]);
            (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
                .ShouldBeEmpty();

            using HttpResponseMessage readResponse = await client
                .GetAsync($"/api/v1/projects/{TestProjectId}", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            await AssertSafeDenialAsync(readResponse).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    /// <summary>Verifies unusable subject evidence fails independently of the optional identity alias.</summary>
    /// <param name="environmentName">The host environment using strict bearer authentication.</param>
    /// <param name="invalidCase">The invalid subject or alias evidence to present.</param>
    /// <param name="includeAlias">Whether the token also contains the original actor alias.</param>
    [Theory]
    [InlineData("Production", "missing-sub", true)]
    [InlineData("Production", "blank-sub", true)]
    [InlineData("Production", "multiple-sub", true)]
    [InlineData("Production", "conflicting-alias", true)]
    [InlineData("Staging", "missing-sub", true)]
    [InlineData("Staging", "blank-sub", true)]
    [InlineData("Staging", "multiple-sub", true)]
    [InlineData("Staging", "conflicting-alias", true)]
    [InlineData("Production", "missing-sub", false)]
    [InlineData("Production", "blank-sub", false)]
    [InlineData("Production", "numeric-sub", false)]
    [InlineData("Staging", "missing-sub", false)]
    [InlineData("Staging", "blank-sub", false)]
    [InlineData("Staging", "numeric-sub", false)]
    public async Task JwtMiddleware_InvalidSubjectIdentity_FailsAuthenticationAndDeniesProtectedRead(
        string environmentName,
        string invalidCase,
        bool includeAlias)
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync(environmentName: environmentName).ConfigureAwait(true);
        try
        {
            await AssertAccessibleProjectAsync(app).ConfigureAwait(true);
            List<Claim> claims =
            [
                new Claim("tenant_id", "tenant-a"),
                new Claim("permissions", "[\"projects:read\"]"),
            ];
            if (includeAlias)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "actor-a"));
            }

            if (invalidCase == "numeric-sub")
            {
                claims.Add(new Claim("sub", "42", ClaimValueTypes.Integer64));
            }
            else if (invalidCase != "missing-sub")
            {
                claims.Add(new Claim("sub", invalidCase == "blank-sub" ? "   " : "actor-b"));
            }

            if (invalidCase == "multiple-sub")
            {
                claims.Add(new Claim("sub", "actor-a"));
            }

            string token = CreateToken(CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5), claims);
            using HttpClient client = CreateClient(app, token);
            using HttpResponseMessage response = await client
                .GetAsync("/authentication-contract", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            response.Headers.WwwAuthenticate.Select(static challenge => challenge.ToString()).ShouldBe(["Bearer"]);
            (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBeEmpty();
            using HttpResponseMessage readResponse = await client
                .GetAsync($"/api/v1/projects/{TestProjectId}", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            await AssertSafeDenialAsync(readResponse).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task JwtMiddleware_MatchingSubjectAlias_AllowsProtectedRead(string environmentName)
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync(environmentName: environmentName).ConfigureAwait(true);
        try
        {
            string token = CreateToken(
                CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5),
                [
                    new Claim("sub", "actor-a"),
                    new Claim(ClaimTypes.NameIdentifier, "actor-a"),
                    new Claim("tenant_id", "tenant-a"),
                    new Claim("permissions", "[\"projects:read\"]"),
                ]);
            await AssertAccessibleProjectAsync(app, token).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    /// <summary>Verifies malformed structured permissions cannot disclose an accessible Project.</summary>
    /// <param name="environmentName">The host environment using strict bearer authentication.</param>
    /// <param name="invalidCase">The malformed permission representation to present.</param>
    [Theory]
    [InlineData("Production", "malformed-array")]
    [InlineData("Production", "null-element")]
    [InlineData("Production", "object")]
    [InlineData("Production", "mixed-types")]
    [InlineData("Production", "typed-null")]
    [InlineData("Staging", "malformed-array")]
    [InlineData("Staging", "null-element")]
    [InlineData("Staging", "object")]
    [InlineData("Staging", "mixed-types")]
    [InlineData("Staging", "typed-null")]
    public async Task ProtectedRead_MalformedPermissionEvidence_ReturnsSafeDenial(string environmentName, string invalidCase)
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync(environmentName: environmentName).ConfigureAwait(true);
        try
        {
            await AssertAccessibleProjectAsync(app).ConfigureAwait(true);
            List<Claim> claims =
            [
                new Claim("sub", "actor-a"),
                new Claim("tenant_id", "tenant-a"),
            ];
            if (invalidCase == "mixed-types")
            {
                claims.Add(new Claim("permissions", "projects:read"));
                claims.Add(new Claim("permissions", "false", ClaimValueTypes.Boolean));
            }
            else if (invalidCase != "typed-null")
            {
                string permission = invalidCase switch
                {
                    "null-element" => "[\"projects:read\",null]",
                    "object" => "{ invalid projects:read }",
                    _ => "[\"invalid\" projects:read",
                };
                claims.Add(new Claim("permissions", permission));
            }

            IDictionary<string, object>? additionalClaims = invalidCase == "typed-null"
                ? new Dictionary<string, object> { ["permissions"] = new object?[] { "projects:read", null } }
                : null;
            string token = CreateToken(
                CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5), claims,
                additionalClaims: additionalClaims);
            if (invalidCase == "typed-null")
            {
                using JsonDocument payload = JsonDocument.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Payload.SerializeToJson());
                payload.RootElement.GetProperty("permissions")[1].ValueKind.ShouldBe(JsonValueKind.Null);
            }

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

    /// <summary>Verifies string-valued JWT permission arrays retain their valid authorization evidence.</summary>
    /// <param name="environmentName">The host environment using strict bearer authentication.</param>
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task ProtectedRead_StringPermissionArray_ReturnsSeededProject(string environmentName)
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync(environmentName: environmentName).ConfigureAwait(true);
        try
        {
            string token = CreateToken(
                CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5),
                [
                    new Claim("sub", "actor-a"),
                    new Claim("tenant_id", "tenant-a"),
                    new Claim("permissions", "projects:read"),
                    new Claim("permissions", "projects:list"),
                ]);
            await AssertAccessibleProjectAsync(app, token).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData("Production", SecurityAlgorithms.HmacSha256)]
    [InlineData("Production", SecurityAlgorithms.HmacSha384)]
    [InlineData("Production", SecurityAlgorithms.HmacSha512)]
    [InlineData("Staging", SecurityAlgorithms.HmacSha256)]
    [InlineData("Staging", SecurityAlgorithms.HmacSha384)]
    [InlineData("Staging", SecurityAlgorithms.HmacSha512)]
    public async Task JwtMiddleware_NonDevelopmentRejectsSymmetricSigningEvenWhenMetadataContainsTheKey(
        string environmentName,
        string algorithm)
    {
        SymmetricSecurityKey signingKey = new(RandomNumberGenerator.GetBytes(64)) { KeyId = "symmetric-fixture" };
        WebApplication app = await StartAuthenticatedProjectsHostAsync(signingKey, environmentName: environmentName)
            .ConfigureAwait(true);
        try
        {
            await AssertAccessibleProjectAsync(app).ConfigureAwait(true);
            string token = CreateToken(
                signingKey,
                TestIssuer,
                TestAudience,
                DateTime.UtcNow.AddMinutes(5),
                [
                    new Claim("sub", "actor-a"),
                    new Claim("tenant_id", "tenant-a"),
                    new Claim("permissions", "[\"projects:read\"]"),
                ],
                algorithm);
            using HttpClient client = CreateClient(app, token);

            using HttpResponseMessage response = await client
                .GetAsync("/authentication-contract", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            response.Headers.WwwAuthenticate.Select(static challenge => challenge.ToString()).ShouldBe(["Bearer"]);
            (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))
                .ShouldBeEmpty();

            using HttpResponseMessage readResponse = await client
                .GetAsync($"/api/v1/projects/{TestProjectId}", TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            await AssertSafeDenialAsync(readResponse).ConfigureAwait(true);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task ProtectedRead_ValidSignedToken_ReturnsSeededProject(string environmentName)
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync(environmentName: environmentName).ConfigureAwait(true);
        try
        {
            await AssertAccessibleProjectAsync(app).ConfigureAwait(true);
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
            await AssertAccessibleProjectAsync(app).ConfigureAwait(true);
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
            await AssertAccessibleProjectAsync(app).ConfigureAwait(true);
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
    public void TenantContext_CurrentTenantClaimWinsOverMembershipClaimOrder()
    {
        DefaultHttpContext context = new();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim("sub", "actor-a"),
                    new Claim("eventstore:current-tenant", "tenant-a"),
                    new Claim("eventstore:tenant", "tenant-b"),
                    new Claim("eventstore:tenant", "tenant-a"),
                    new Claim("eventstore:permission", ProjectAuthorizationGate.ListProjectsAction),
                ],
                "validated-jwt"));
        HttpContextProjectTenantContextAccessor accessor = new(new HttpContextAccessor { HttpContext = context });

        accessor.AuthoritativeTenantId.ShouldBe("tenant-a");
        EventStoreClaimTransformEvidence evidence = accessor.GetClaimTransformEvidence(ProjectAuthorizationGate.ListProjectsAction);
        evidence.TenantId.ShouldBe("tenant-a");
        evidence.HasPermissionFor(ProjectAuthorizationGate.ListProjectsAction).ShouldBeTrue();
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

    [Theory]
    [InlineData(false, "scope")]
    [InlineData(true, "scope")]
    [InlineData(false, "scp")]
    [InlineData(true, "scp")]
    public async Task ForwardedJwt_RealQueryControllerPreservesDualPrincipalClaims(bool delegated, string scopeClaimType)
    {
        WebApplication app = await StartAuthenticatedProjectsHostAsync(includeQueryGateway: true).ConfigureAwait(true);
        try
        {
            List<Claim> claims =
            [
                new Claim("sub", "actor-a"),
                new Claim("tenant_id", "tenant-a"),
                new Claim("permissions", "[\"projects:read\",\"query:read\"]"),
                new Claim("azp", "projects-gateway"),
                new Claim(scopeClaimType, "projects.read projects.list"),
            ];
            if (delegated)
            {
                claims.Add(new Claim("act", "{\"sub\":\"delegation-a\"}"));
            }

            string token = CreateToken(CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5), claims);
            await AssertAccessibleProjectAsync(app, token).ConfigureAwait(true);
            DefaultHttpContext context = new();
            context.Request.Headers.Authorization = $"Bearer {token}";
            using EventStoreGatewayTokenForwardingHandler forwarding = new(new HttpContextAccessor { HttpContext = context })
            {
                InnerHandler = new HttpClientHandler(),
            };
            using HttpMessageInvoker invoker = new(forwarding);
            using HttpRequestMessage request = new(HttpMethod.Post, new Uri(new Uri(app.Urls.First()), "/api/v1/queries"))
            {
                Content = JsonContent.Create(new SubmitQueryRequest("tenant-a", "projects", TestProjectId, "GetProject", null)),
            };

            using HttpResponseMessage response = await invoker.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            request.Headers.Authorization?.Parameter.ShouldBe(token);
            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            JsonElement payload = document.RootElement.GetProperty("payload");
            payload.GetProperty("actor").GetString().ShouldBe("actor-a");
            payload.GetProperty("userId").GetString().ShouldBe("actor-a");
            payload.GetProperty("tenant").GetString().ShouldBe("tenant-a");
            payload.GetProperty("workload").GetString().ShouldBe("projects-gateway");
            payload.GetProperty("delegated").GetBoolean().ShouldBe(delegated);
            payload.GetProperty("delegationId").GetString().ShouldBe(delegated ? "delegation-a" : null);
            payload.GetProperty("scopes").EnumerateArray().Select(static scope => scope.GetString())
                .ShouldBe(["projects.read", "projects.list"]);
            payload.GetProperty("audience").EnumerateArray().Select(static audience => audience.GetString())
                .ShouldBe([TestAudience]);
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
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

    private static async Task<WebApplication> StartAuthenticatedProjectsHostAsync(
        SecurityKey? metadataSigningKey = null,
        string metadataIssuer = TestIssuer,
        string environmentName = "Production",
        bool includeQueryGateway = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions { EnvironmentName = environmentName });
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        _ = builder.Configuration.AddInMemoryCollection(ValidProductionConfiguration());
        builder.Services.AddProjectsServer();
        builder.Services.AddProjectsServerRuntimeInfrastructure();
        builder.Services.RemoveAll<IProjectDetailReadModel>();
        builder.Services.AddSingleton<InMemoryProjectDetailReadModel>();
        builder.Services.AddSingleton<IProjectDetailReadModel>(static services => services.GetRequiredService<InMemoryProjectDetailReadModel>());
        builder.Services.RemoveAll<IProjectTenantAccessProjectionStore>();
        builder.Services.AddSingleton<IProjectTenantAccessProjectionStore, InMemoryProjectTenantAccessProjectionStore>();
        _ = builder.Services.AddProjectsAuthentication(builder.Configuration, builder.Environment);
        _ = builder.Services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                OpenIdConnectConfiguration oidc = new() { Issuer = metadataIssuer };
                oidc.SigningKeys.Add(CreateValidationKey());
                if (metadataSigningKey is not null)
                {
                    oidc.SigningKeys.Add(metadataSigningKey);
                }
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(oidc);
            });

        if (includeQueryGateway)
        {
            builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblyContaining<QueryIdentityCaptureHandler>());
            builder.Services.AddSingleton<ITenantValidator, ClaimsTenantValidator>();
            builder.Services.AddSingleton<IRbacValidator, ClaimsRbacValidator>();
            builder.Services.AddSingleton<IETagService>(new DaprETagService(new ActorProxyFactory(), NullLogger<DaprETagService>.Instance));
            _ = builder.Services.AddControllers().AddApplicationPart(typeof(QueriesController).Assembly);
        }

        WebApplication app = builder.Build();
        await SeedAccessibleProjectAsync(app.Services).ConfigureAwait(true);
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.MapGet(
                "/authentication-contract",
                (ClaimsPrincipal principal) =>
                {
                    string actor = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? throw new InvalidOperationException("The authentication fixture requires an actor claim.");
                    DualPrincipalIdentity identity = DualPrincipalClaimsHelper.Extract(principal, actor);
                    return Results.Json(
                        new
                        {
                            Actor = actor,
                            Tenant = principal.FindFirstValue("eventstore:tenant"),
                            Permissions = principal.FindAll("eventstore:permission").Select(static claim => claim.Value).ToArray(),
                            Scopes = principal.FindAll("scope").Select(static claim => claim.Value).ToArray(),
                            AlternateScopes = principal.FindAll("scp").Select(static claim => claim.Value).ToArray(),
                            OriginalActor = identity.OriginalActorId,
                            Workload = identity.AuthenticatedWorkloadId,
                            Delegated = identity.IsDelegated,
                            identity.DelegationId,
                            RoutingScopes = identity.Scopes,
                            identity.Audience,
                        });
                })
            .RequireAuthorization();
        app.MapProjectsServerEndpoints();
        if (includeQueryGateway)
        {
            _ = app.MapControllers();
        }

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

    private static async Task SeedAccessibleProjectAsync(IServiceProvider services)
    {
        services.GetRequiredService<InMemoryProjectDetailReadModel>().Project(
            "tenant-a",
            new ProjectCreated(
                "tenant-a", TestProjectId, TestProjectName, "Fixture metadata", "fixture-setup", ProjectLifecycle.Active,
                "actor-a", "correlation-fixture", "task-fixture", "idempotency-fixture", "sha256:fixture", DateTimeOffset.UtcNow));
        ProjectTenantAccessProjection projection = new()
        {
            TenantId = "tenant-a",
            Enabled = true,
            Watermark = 1,
            ProjectionWatermark = "tenant-a:1",
            LastEventTimestamp = DateTimeOffset.UtcNow,
        };
        projection.Principals["actor-a"] = new ProjectTenantPrincipalEvidence("actor-a", "TenantOwner");
        await services.GetRequiredService<IProjectTenantAccessProjectionStore>()
            .SaveAsync(projection, TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private static async Task AssertAccessibleProjectAsync(WebApplication app, string? token = null)
    {
        using HttpClient client = CreateClient(
            app,
            token ?? CreateToken(CreateSigningKey(), TestIssuer, TestAudience, DateTime.UtcNow.AddMinutes(5)));
        using HttpResponseMessage response = await client
            .GetAsync($"/api/v1/projects/{TestProjectId}", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
        document.RootElement.GetProperty("projectId").GetString().ShouldBe(TestProjectId);
        document.RootElement.GetProperty("name").GetString().ShouldBe(TestProjectName);
        document.RootElement.GetProperty("setupMetadata").GetString().ShouldBe("fixture-setup");
    }

    private static async Task AssertSafeDenialAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldNotContain(TestProjectId);
        body.ShouldNotContain(TestProjectName);
        body.ShouldNotContain("fixture-setup");
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

    private static RsaSecurityKey CreateSigningKey()
        => new(SigningKeyParameters) { KeyId = "projects-fixture" };

    private static RsaSecurityKey CreateValidationKey()
        => new(new RSAParameters { Modulus = SigningKeyParameters.Modulus, Exponent = SigningKeyParameters.Exponent })
        {
            KeyId = "projects-fixture",
        };

    private static RSAParameters CreateSigningKeyParameters()
    {
        using RSA rsa = RSA.Create(2048);
        return rsa.ExportParameters(includePrivateParameters: true);
    }

    private static string CreateToken(
        SecurityKey? signingKey,
        string issuer,
        string audience,
        DateTime expires,
        IEnumerable<Claim>? claims = null,
        string algorithm = SecurityAlgorithms.RsaSha256,
        IDictionary<string, object>? additionalClaims = null)
    {
        IEnumerable<Claim> subjectClaims = claims ??
        [
            new Claim("sub", "actor-a"),
            new Claim("tenant_id", "tenant-a"),
            new Claim("permissions", "[\"projects:read\"]"),
        ];
        JwtSecurityTokenHandler handler = new();
        handler.OutboundClaimTypeMap.Clear();
        return handler.CreateEncodedJwt(
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(subjectClaims),
                Claims = additionalClaims,
                Issuer = issuer,
                Audience = audience,
                NotBefore = DateTime.UtcNow.AddMinutes(-10),
                Expires = expires,
                SigningCredentials = signingKey is null ? null : new SigningCredentials(signingKey, algorithm),
            });
    }

    private static IHostEnvironment CreateEnvironment(string environmentName)
        => WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = environmentName }).Environment;
}
