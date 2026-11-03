// <copyright file="Program.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Hexalith.Projects.Server;
using Hexalith.Projects.Server.Authentication;
using Hexalith.Projects.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProjectsServer();
bool developmentCatalog = builder.AddProjectsDevelopmentCatalog();
builder.Services.AddProjectsServerRuntimeInfrastructure();
_ = builder.Services.AddProjectsAuthentication(builder.Configuration, builder.Environment);
bool authenticationEnabled = !ProjectsAuthenticationServiceCollectionExtensions.IsAnonymousDevelopmentBypass(
    builder.Configuration,
    builder.Environment);

WebApplication app = builder.Build();

if (authenticationEnabled)
{
    _ = app.UseAuthentication();
    if (developmentCatalog)
    {
        _ = Hexalith.EventStore.ServiceDefaults.Authentication.EventStoreHttpHostSecurityExtensions.UseEventStoreHttpHostSecurity(app);
    }
    _ = app.UseAuthorization();
}

app.MapDefaultEndpoints();
app.MapProjectsServerEndpoints();
app.MapProjectsDevelopmentCatalog();

await app.RunAsync().ConfigureAwait(false);
