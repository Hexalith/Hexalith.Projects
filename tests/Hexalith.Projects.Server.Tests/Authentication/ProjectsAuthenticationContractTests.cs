// <copyright file="ProjectsAuthenticationContractTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Hexalith.Projects.Server.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Microsoft.IdentityModel.Tokens;

using Shouldly;

using Xunit;

/// <summary>Verifies the Projects production identity and authentication contract.</summary>
public sealed class ProjectsAuthenticationContractTests
{
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

    private static ValidateOptionsResult Validate(string environmentName, Dictionary<string, string?> values)
    {
        IConfiguration configuration = CreateConfiguration(values);
        ProjectsAuthenticationOptions options = configuration
            .GetSection(ProjectsAuthenticationOptions.SectionName)
            .Get<ProjectsAuthenticationOptions>()!;

        return new ValidateProjectsAuthenticationOptions(CreateEnvironment(environmentName))
            .Validate(null, options);
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
                    ["Authentication:JwtBearer:Authority"] = "https://identity.example/realms/hexalith",
                    ["Authentication:JwtBearer:Issuer"] = "https://identity.example/realms/hexalith",
                    ["Authentication:JwtBearer:Audience"] = "hexalith-projects",
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
        DateTime expires)
        => new JwtSecurityTokenHandler().CreateEncodedJwt(
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim("sub", "actor-a")]),
                Issuer = issuer,
                Audience = audience,
                NotBefore = DateTime.UtcNow.AddMinutes(-10),
                Expires = expires,
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            });

    private static IHostEnvironment CreateEnvironment(string environmentName)
        => WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = environmentName }).Environment;
}
