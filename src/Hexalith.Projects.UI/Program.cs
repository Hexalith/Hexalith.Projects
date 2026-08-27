using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.Projects.Client;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Components;
using Hexalith.Projects.UI.Diagnostics;

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(o => o.ValidateScopes = true);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();
IHttpClientBuilder projectsClient = builder.Services.AddProjectsClient();
builder.Services.AddScoped<IProjectInventorySource, ProjectInventorySource>();
builder.Services.AddScoped<IProjectWarningsDashboardSource, ProjectWarningsDashboardSource>();
builder.Services.AddScoped<IProjectOperatorDiagnosticSource, ProjectOperatorDiagnosticSource>();
builder.Services.AddScoped<IProjectDetailSource, ProjectDetailSource>();
builder.Services.AddScoped<IProjectResolutionTraceSource, ProjectResolutionTraceSource>();
builder.Services.AddScoped<IProjectAuditTimelineSource, ProjectAuditTimelineSource>();
builder.Services.AddScoped<IProjectMaintenanceActionSource, ProjectMaintenanceActionSource>();

builder.Services.AddHexalithFrontComposerQuickstart(
    o => o.ScanAssemblies(typeof(ProjectsFrontComposerDomain).Assembly));
builder.Services.AddHexalithDomain<ProjectsFrontComposerDomain>();
builder.Services.Configure<FcShellOptions>(builder.Configuration.GetSection("Hexalith:Shell"));

bool authEnabled =
    Uri.TryCreate(builder.Configuration["Authentication:OpenIdConnect:Authority"], UriKind.Absolute, out Uri? oidcAuthority)
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:OpenIdConnect:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]);

if (authEnabled)
{
    _ = builder.Services.AddHexalithFrontComposerServerSecurity(options => options.UseKeycloak(
        oidcAuthority!,
        builder.Configuration["Authentication:OpenIdConnect:ClientId"]!,
        builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]!,
        tenantClaimType: "eventstore:current-tenant",
        userClaimType: "sub"));
    _ = projectsClient.AddFrontComposerGatewayAuthorization();
}

WebApplication app = builder.Build();

app.MapStaticAssets();
app.UseStaticFiles();
app.UseRequestLocalization();

if (authEnabled)
{
    _ = app.UseAuthentication();
    _ = app.UseAuthorization();
}

app.UseAntiforgery();

RazorComponentsEndpointConventionBuilder razorComponents = app.MapRazorComponents<App>()
    .AddAdditionalAssemblies(typeof(ProjectsFrontComposerDomain).Assembly)
    .AddInteractiveServerRenderMode();

if (authEnabled)
{
    _ = razorComponents.RequireAuthorization();
    _ = app.MapHexalithFrontComposerAuthenticationEndpoints();
}

app.Run();
