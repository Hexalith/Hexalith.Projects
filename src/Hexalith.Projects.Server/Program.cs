// <copyright file="Program.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Microsoft.AspNetCore.Builder;

// Minimal ASP.NET Core host so the Server project is a compiling, runnable skeleton.
// EventStore command-pipeline wiring, authentication, authorization, ACL adapters and
// tenant-access services are added by later Epic-1 stories. No business logic here.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

app.MapGet("/health", () => Results.Ok("Hexalith.Projects.Server skeleton"));

await app.RunAsync().ConfigureAwait(false);
