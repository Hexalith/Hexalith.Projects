// <copyright file="ProjectsUiAuthenticationCompositionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Authentication;

using System.Net.Http.Headers;
using System.Security.Claims;

using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Projects.Client;
using Hexalith.Projects.UI;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

/// <summary>Security-composition checks for the Projects UI host, exercised through the real service graph.</summary>
public sealed class ProjectsUiAuthenticationCompositionTests
{
    private const string Authority = "https://identity.projects.test/realms/hexalith";
    private const string ClientId = "hexalith-projects-ui";
    private const string ClientSecret = "projects-ui-composition-secret";
    private const string OidcScheme = "Hexalith.FrontComposer.Oidc";
    private const string CookieScheme = "Hexalith.FrontComposer.Cookie";
    private const string ProjectsUri = "https://projects.test/api/v1/projects";

    /// <summary>Verifies the host composes the shared helper and protects every interactive endpoint.</summary>
    [Fact]
    public void ProgramShouldComposeSecurityHelperAndProtectInteractiveEndpoints()
    {
        string program = ReadProjectFile("src", "Hexalith.Projects.UI", "Program.cs");

        program.ShouldContain("bool authEnabled = builder.Services.AddProjectsUiSecurity(builder.Configuration, projectsClient);");
        program.ShouldContain("app.UseAuthentication()");
        program.ShouldContain("app.UseAuthorization()");
        program.ShouldContain("razorComponents.RequireAuthorization()");
        program.ShouldContain("app.MapHexalithFrontComposerAuthenticationEndpoints()");
        program.IndexOf("app.UseAuthorization()", StringComparison.Ordinal)
            .ShouldBeLessThan(program.IndexOf("app.MapRazorComponents<App>()", StringComparison.Ordinal));
    }

    /// <summary>Verifies route rendering uses cascading authentication and an authorization-aware route view.</summary>
    [Fact]
    public void RoutesShouldProtectInteractiveRenderingAndChallengeAnonymousUsers()
    {
        string routes = ReadProjectFile("src", "Hexalith.Projects.UI", "Components", "Routes.razor");

        routes.ShouldContain("<CascadingAuthenticationState>");
        routes.ShouldContain("<AuthorizeRouteView");
        routes.ShouldContain("<RedirectToChallenge />");
        routes.ShouldNotContain("<RouteView RouteData=");
    }

    /// <summary>Verifies configured OIDC selects the confidential Keycloak code flow and an HttpOnly cookie session.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ConfiguredOidcShouldUseConfidentialCodeFlowWithHttpOnlyCookieSession()
    {
        (ServiceProvider provider, _, bool enabled, _, _) = Compose(CompleteSettings());
        await using (provider.ConfigureAwait(true))
        {
            enabled.ShouldBeTrue();
            IAuthenticationSchemeProvider schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
            (await schemes.GetDefaultChallengeSchemeAsync().ConfigureAwait(true))!.Name.ShouldBe(OidcScheme);
            (await schemes.GetDefaultAuthenticateSchemeAsync().ConfigureAwait(true))!.Name.ShouldBe(CookieScheme);

            OpenIdConnectOptions oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OidcScheme);
            oidc.Authority.ShouldBe(Authority);
            oidc.ClientId.ShouldBe(ClientId);
            oidc.ClientSecret.ShouldBe(ClientSecret);
            oidc.ResponseType.ShouldBe("code");
            oidc.UsePkce.ShouldBeTrue();
            oidc.SaveTokens.ShouldBeTrue();
            oidc.MapInboundClaims.ShouldBeFalse();
            oidc.SignInScheme.ShouldBe(CookieScheme);
            oidc.Scope.ShouldContain("openid");

            CookieAuthenticationOptions cookie = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieScheme);
            cookie.Cookie.HttpOnly.ShouldBeTrue();
            cookie.Cookie.SameSite.ShouldBe(SameSiteMode.Lax);
            cookie.SlidingExpiration.ShouldBeFalse();
            cookie.LoginPath.Value.ShouldBe("/authentication/challenge");
        }
    }

    /// <summary>Verifies the cookie-authenticated principal reaches interactive Server components.</summary>
    [Fact]
    public void ConfiguredOidcShouldFlowServerAuthenticationStateIntoInteractiveComponents()
    {
        (ServiceProvider provider, ServiceCollection services, bool enabled, _, _) = Compose(CompleteSettings());
        using (provider)
        {
            enabled.ShouldBeTrue();
            services.Last(static descriptor => descriptor.ServiceType == typeof(AuthenticationStateProvider))
                .ImplementationType.ShouldBe(typeof(ServerAuthenticationStateProvider));
        }
    }

    /// <summary>Verifies outbound Projects calls carry the signed-in user's token and anonymous calls carry none.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ConfiguredOidcShouldRelayTheSignedInUserTokenOnProjectsCalls()
    {
        (ServiceProvider provider, _, _, RecordingHttpMessageHandler recorder, string clientName) = Compose(CompleteSettings());
        await using (provider.ConfigureAwait(true))
        {
            provider.GetRequiredService<FrontComposerUserTokenStore>()
                .Set("user-1", "relayed-access-token", DateTimeOffset.UtcNow.AddMinutes(5));
            IHttpContextAccessor accessor = provider.GetRequiredService<IHttpContextAccessor>();
            using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName);

            accessor.HttpContext = AuthenticatedContext("user-1");
            using (HttpResponseMessage response = await client.GetAsync(new Uri(ProjectsUri), TestContext.Current.CancellationToken).ConfigureAwait(true))
            {
                _ = response.EnsureSuccessStatusCode();
            }

            accessor.HttpContext = new DefaultHttpContext();
            using (HttpResponseMessage response = await client.GetAsync(new Uri(ProjectsUri), TestContext.Current.CancellationToken).ConfigureAwait(true))
            {
                _ = response.EnsureSuccessStatusCode();
            }

            recorder.Authorizations.Count.ShouldBe(2);
            AuthenticationHeaderValue relayed = recorder.Authorizations[0].ShouldNotBeNull();
            relayed.Scheme.ShouldBe("Bearer");
            relayed.Parameter.ShouldBe("relayed-access-token");
            recorder.Authorizations[1].ShouldBeNull();
        }
    }

    /// <summary>Verifies absent OIDC settings keep the explicit auth-disabled startup without token relay.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AbsentOidcSettingsShouldKeepAuthDisabledStartupWithoutTokenRelay()
    {
        (ServiceProvider provider, ServiceCollection services, bool enabled, RecordingHttpMessageHandler recorder, string clientName) =
            Compose(new Dictionary<string, string?>());
        await using (provider.ConfigureAwait(true))
        {
            enabled.ShouldBeFalse();
            services.ShouldNotContain(static descriptor => descriptor.ServiceType == typeof(IAuthenticationSchemeProvider));
            services.ShouldNotContain(static descriptor => descriptor.ServiceType == typeof(FrontComposerGatewayAuthorizationHandler));

            provider.GetRequiredService<IHttpContextAccessor>().HttpContext = AuthenticatedContext("user-1");
            using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName);
            using HttpResponseMessage response = await client.GetAsync(new Uri(ProjectsUri), TestContext.Current.CancellationToken).ConfigureAwait(true);

            _ = response.EnsureSuccessStatusCode();
            recorder.Authorizations.ShouldHaveSingleItem().ShouldBeNull();
        }
    }

    /// <summary>Verifies partial OIDC settings fail closed and name only the missing key.</summary>
    /// <param name="missingKey">The configuration key left unset.</param>
    [Theory]
    [InlineData(ProjectsUiSecurity.AuthoritySettingKey)]
    [InlineData(ProjectsUiSecurity.ClientIdSettingKey)]
    [InlineData(ProjectsUiSecurity.ClientSecretSettingKey)]
    public void PartialOidcSettingsShouldFailClosedWithoutEchoingConfiguredValues(string missingKey)
    {
        Dictionary<string, string?> settings = CompleteSettings();
        settings[missingKey] = " ";

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => Compose(settings));

        exception.Message.ShouldContain(missingKey);
        exception.Message.ShouldNotContain(ClientSecret);
        exception.Message.ShouldNotContain(Authority);
    }

    /// <summary>Verifies a relative, rooted-path, or non-HTTP authority fails closed instead of disabling authentication.</summary>
    /// <param name="authority">The invalid authority value.</param>
    [Theory]
    [InlineData("realms/hexalith")]
    [InlineData("/realms/hexalith")]
    [InlineData("ftp://identity.projects.test/realms/hexalith")]
    public void NonHttpAuthorityShouldFailClosed(string authority)
    {
        Dictionary<string, string?> settings = CompleteSettings();
        settings[ProjectsUiSecurity.AuthoritySettingKey] = authority;

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => Compose(settings));

        exception.Message.ShouldContain(ProjectsUiSecurity.AuthoritySettingKey);
        exception.Message.ShouldNotContain(ClientSecret);
    }

    private static Dictionary<string, string?> CompleteSettings()
        => new(StringComparer.Ordinal)
        {
            [ProjectsUiSecurity.AuthoritySettingKey] = Authority,
            [ProjectsUiSecurity.ClientIdSettingKey] = ClientId,
            [ProjectsUiSecurity.ClientSecretSettingKey] = ClientSecret,
        };

    private static (ServiceProvider Provider, ServiceCollection Services, bool Enabled, RecordingHttpMessageHandler Recorder, string ClientName) Compose(
        IReadOnlyDictionary<string, string?> settings)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ServiceCollection services = new();
        _ = services.AddSingleton<IHostEnvironment>(new HostingEnvironment
        {
            EnvironmentName = Environments.Development,
            ApplicationName = "Hexalith.Projects.UI.Tests",
            ContentRootPath = AppContext.BaseDirectory,
        });
        _ = services.AddLogging();
        _ = services.AddHttpContextAccessor();
        IHttpClientBuilder projectsClient = services.AddProjectsClient(options => options.BaseAddress = new Uri("https://projects.test/"));
        RecordingHttpMessageHandler recorder = new();
        _ = projectsClient.ConfigurePrimaryHttpMessageHandler(() => recorder);

        bool enabled = services.AddProjectsUiSecurity(configuration, projectsClient);
        return (services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }), services, enabled, recorder, projectsClient.Name);
    }

    private static DefaultHttpContext AuthenticatedContext(string subject)
        => new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)], authenticationType: "test")),
        };

    private static string ReadProjectFile(params string[] segments)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine([root, .. segments]));
    }
}
