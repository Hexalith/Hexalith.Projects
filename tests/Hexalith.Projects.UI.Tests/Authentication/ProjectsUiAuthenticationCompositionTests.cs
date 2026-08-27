// <copyright file="ProjectsUiAuthenticationCompositionTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Authentication;

using Shouldly;

using Xunit;

/// <summary>Structural security-composition checks for the Projects UI host.</summary>
public sealed class ProjectsUiAuthenticationCompositionTests
{
    /// <summary>Verifies configured OIDC composes FrontComposer server security and token relay.</summary>
    [Fact]
    public void ProgramShouldComposeConditionalServerOidcAndGatewayTokenRelay()
    {
        string program = ReadProjectFile("src", "Hexalith.Projects.UI", "Program.cs");

        program.ShouldContain("AddHexalithFrontComposerServerSecurity");
        program.ShouldContain("tenantClaimType: \"eventstore:current-tenant\"");
        program.ShouldContain("userClaimType: \"sub\"");
        program.ShouldContain("projectsClient.AddFrontComposerGatewayAuthorization()");
        program.ShouldContain("app.UseAuthentication()");
        program.ShouldContain("app.UseAuthorization()");
        program.ShouldContain("razorComponents.RequireAuthorization()");
        program.ShouldContain("app.MapHexalithFrontComposerAuthenticationEndpoints()");
    }

    /// <summary>Verifies absent OIDC settings retain the explicit auth-disabled startup path.</summary>
    [Fact]
    public void ProgramShouldGateAuthenticationCompositionOnCompleteOidcSettings()
    {
        string program = ReadProjectFile("src", "Hexalith.Projects.UI", "Program.cs");

        program.ShouldContain("bool authEnabled =");
        program.ShouldContain("Authentication:OpenIdConnect:Authority");
        program.ShouldContain("Authentication:OpenIdConnect:ClientId");
        program.ShouldContain("Authentication:OpenIdConnect:ClientSecret");
        program.ShouldContain("if (authEnabled)");
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

    private static string ReadProjectFile(params string[] segments)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine([root, .. segments]));
    }
}
