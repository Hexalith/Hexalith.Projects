// <copyright file="Program.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Hexalith.Projects.ServiceDefaults;
using Hexalith.Projects.Workers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProjectsTenantEventWorkers();

WebApplication app = builder.Build();
app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapDefaultEndpoints();
app.MapProjectsTenantEventWorkerEndpoints();

await app.RunAsync().ConfigureAwait(false);
