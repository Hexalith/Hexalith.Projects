namespace Hexalith.Projects.AppHost;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

/// <summary>Adds real OIDC and a test-owned observer to the existing private, actual-owner read topology.</summary>
public static class SupportedProjectBrowserFixtureProfile
{
    /// <summary>Gets whether browser verification was explicitly selected in the owned private configuration.</summary>
    public static bool IsEnabled(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.GetValue<bool>("Projects:SupportedReads:Browser:Enabled");
    }

    /// <summary>Composes only UI security and test observation; SDK owner trust remains unchanged.</summary>
    public static void Add(IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> ui)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(ui);
        if (!IsEnabled(builder.Configuration))
            return;
        if (!SupportedProjectReadFixtureProfile.IsEnabled(builder.Configuration) || !builder.Environment.IsDevelopment())
            throw new InvalidOperationException("Browser reads require the existing private Development supported-read profile.");
        string authority = builder.Configuration["Projects:SupportedReads:Browser:Authority"]
            ?? throw new InvalidOperationException("The owned HTTPS OIDC authority is required.");
        if (!Uri.TryCreate(authority, UriKind.Absolute, out Uri? issuer) || issuer.Scheme != Uri.UriSchemeHttps || !issuer.IsLoopback)
            throw new InvalidOperationException("Browser fixture OIDC requires its isolated HTTPS loopback issuer.");
        string secret = builder.Configuration["Projects:SupportedReads:Browser:ClientSecret"]
            ?? throw new InvalidOperationException("The private OIDC client credential is required.");
        IResourceBuilder<ParameterResource> clientSecret = builder.AddParameter("browser-ui-client-secret", secret, secret: true);
        _ = ui.WithEnvironment("Authentication__OpenIdConnect__Authority", authority)
            .WithEnvironment("Authentication__OpenIdConnect__ClientId", "hexalith-projects-ui")
            .WithEnvironment("Authentication__OpenIdConnect__ClientSecret", clientSecret)
            .WithEnvironment("SSL_CERT_FILE", builder.Configuration["Projects:SupportedReads:TrustBundle"]!);
        string root = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../.."));
        _ = builder.AddProject("browser-read-observer", Path.Combine(root, "tests", "Hexalith.Projects.E2E.Fixtures", "Hexalith.Projects.E2E.Fixtures.csproj"))
            .WithEnvironment("FixtureRole", "control")
            .WithEnvironment("FixtureStateStore__Enabled", "true")
            .WithEnvironment("FixtureStateStore__IsolationId", builder.Configuration["Projects:SupportedReads:IsolationId"]!)
            .WithEnvironment("FixtureStateStore__Tenant", builder.Configuration["Projects:SupportedReads:Tenant"]!)
            .WithEnvironment("FixtureStateStore__ForeignTenant", builder.Configuration["Projects:SupportedReads:ForeignTenant"]!)
            .WithEnvironment("FixtureStateStore__PrimaryActor", builder.Configuration["Projects:SupportedReads:Browser:PrimaryActor"]!)
            .WithEnvironment("FixtureStateStore__ForeignActor", builder.Configuration["Projects:SupportedReads:Browser:ForeignActor"]!)
            .WithEnvironment("FixtureStateStore__RedisHost", builder.Configuration["Dapr:RedisHost"]!)
            .WithHttpEndpoint().WithHttpHealthCheck("/health");
    }
}
