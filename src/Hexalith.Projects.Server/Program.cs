// <copyright file="Program.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Hexalith.Projects.Server;
using Hexalith.Projects.ServiceDefaults;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProjectsServer();
builder.Services.AddProjectsServerRuntimeInfrastructure();

// Authenticate inbound requests so the fail-closed authorization gate sees the caller's
// tenant/principal/permission claims (ProjectsClaimsTransformation normalizes them). The AppHost
// injects Authentication:JwtBearer:* only when Keycloak is enabled; when it is not configured the API
// stays anonymous (the unchanged no-Keycloak dev fallback).
IConfigurationSection jwt = builder.Configuration.GetSection("Authentication:JwtBearer");
string? authority = jwt["Authority"];
bool authenticationEnabled = !string.IsNullOrWhiteSpace(authority);
if (authenticationEnabled)
{
    string? audience = jwt["Audience"];
    string? issuer = jwt["Issuer"];
    bool requireHttpsMetadata = !string.Equals(jwt["RequireHttpsMetadata"], "false", StringComparison.OrdinalIgnoreCase);

    _ = builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.RequireHttpsMetadata = requireHttpsMetadata;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                ValidIssuer = issuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                ValidAudience = audience,
            };
        });
    _ = builder.Services.AddAuthorization();
}

WebApplication app = builder.Build();

if (authenticationEnabled)
{
    _ = app.UseAuthentication();
    _ = app.UseAuthorization();
}

app.MapDefaultEndpoints();
app.MapProjectsServerEndpoints();

await app.RunAsync().ConfigureAwait(false);
