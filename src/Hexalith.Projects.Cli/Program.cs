// <copyright file="Program.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Hexalith.Projects.Cli;
using Hexalith.Projects.Client.Generated;

string? baseAddress = Environment.GetEnvironmentVariable("HEXALITH_PROJECTS_BASE_ADDRESS");
if (string.IsNullOrWhiteSpace(baseAddress) || !Uri.TryCreate(baseAddress, UriKind.Absolute, out Uri? uri))
{
    await Console.Error.WriteLineAsync("base_address_required").ConfigureAwait(false);
    return ProjectsCliExitCodes.Usage;
}

using var httpClient = new HttpClient { BaseAddress = uri };
var client = new Client(httpClient);
var app = new ProjectsCliApplication(client, Console.Out, Console.Error);
return await app.RunAsync(args).ConfigureAwait(false);
