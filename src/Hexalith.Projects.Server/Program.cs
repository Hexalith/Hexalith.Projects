// <copyright file="Program.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddProjectsDomainService();

WebApplication app = builder.Build();

app.UseEventStoreDomainService();
app.MapProjectsHumanEndpoints();

await app.RunAsync().ConfigureAwait(false);
