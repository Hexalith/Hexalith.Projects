using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.Projects.Client;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.UI.Components;
using Hexalith.Projects.UI.Diagnostics;

using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(o => o.ValidateScopes = true);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();
builder.Services.AddProjectsClient();
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

WebApplication app = builder.Build();

app.MapStaticAssets();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddAdditionalAssemblies(typeof(ProjectsFrontComposerDomain).Assembly)
    .AddInteractiveServerRenderMode();

app.Run();
