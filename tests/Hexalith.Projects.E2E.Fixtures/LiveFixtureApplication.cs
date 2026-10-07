// <copyright file="LiveFixtureApplication.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.E2E.Fixtures;

/// <summary>
/// Builds the metadata-only fixture host for exactly one role: the runner-facing <c>control</c>
/// resource or one sibling role (<c>conversations</c>, <c>folders</c>, <c>memories</c>) that answers
/// the published sibling client contract the Projects server consumes.
/// </summary>
public static class LiveFixtureApplication
{
    /// <summary>Gets the configuration key that selects the fixture role.</summary>
    public const string RoleConfigurationKey = "FixtureRole";

    /// <summary>Builds the fixture web application for the configured role.</summary>
    /// <param name="args">The host arguments.</param>
    /// <param name="configure">Optional builder customization, for example loopback URLs in contract tests.</param>
    /// <returns>The built, not yet started, application.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the role is missing or unsupported.</exception>
    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        configure?.Invoke(builder);
        _ = builder.Services.AddSingleton<LiveFixtureState>();
        _ = builder.Services.AddHttpClient<FixtureProxy>(static client => client.Timeout = TimeSpan.FromSeconds(10));

        // Fixture ingress is metadata-only: malformed requests become bodiless 400s instead of developer
        // diagnostics, and unexpected failures never render stack traces, endpoints, or private paths.
        _ = builder.Services.Configure<RouteHandlerOptions>(static options => options.ThrowOnBadRequest = false);

        WebApplication app = builder.Build();
        _ = app.UseExceptionHandler(static exceptionApp => exceptionApp.Run(static context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }));

        string role = app.Configuration[RoleConfigurationKey]?.Trim().ToLowerInvariant()
            ?? throw new InvalidOperationException($"{RoleConfigurationKey} is required.");
        LiveFixtureState state = app.Services.GetRequiredService<LiveFixtureState>();

        _ = app.MapGet("/health", () => Results.Ok(new { role, status = "ready" }));
        _ = app.MapPost("/_fixtures/graphs", (LiveFixtureGraph graph) =>
            !graph.IsValid()
                ? Results.BadRequest()
                : state.TryAdd(graph) ? Results.Ok(graph) : Results.Conflict());
        _ = app.MapDelete("/_fixtures/graphs/{graphId}", (string graphId) =>
            state.Remove(graphId) ? Results.NoContent() : Results.NotFound());

        switch (role)
        {
            case "control":
                ControlFixtureEndpoints.Map(app, state);
                break;
            case "conversations":
                ConversationsFixtureEndpoints.Map(app, state);
                break;
            case "folders":
                FoldersFixtureEndpoints.Map(app, state);
                break;
            case "memories":
                MemoriesFixtureEndpoints.Map(app, state);
                break;
            default:
                throw new InvalidOperationException($"Unsupported {RoleConfigurationKey} '{role}'.");
        }

        return app;
    }
}
