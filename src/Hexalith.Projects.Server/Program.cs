// <copyright file="Program.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Hexalith.Projects.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

// Story 1.4: the Server host wires the Project command-async + minimal-read slice. It registers the
// domain processor, the in-memory detail read model, and the tenant-context accessor, then maps the
// /api/v1/projects command-async endpoint + the GetProject minimal read. The production
// IProjectCommandSubmitter (EventStore gateway-backed) + full Dapr/Aspire topology land in Story 1.9;
// authentication/authorization (claim-transform + TenantAccessProjection) land in Story 1.6.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddProjectsServer();

WebApplication app = builder.Build();

app.MapGet("/health", () => Results.Ok("Hexalith.Projects.Server"));
app.MapProjectsServerEndpoints();

await app.RunAsync().ConfigureAwait(false);
