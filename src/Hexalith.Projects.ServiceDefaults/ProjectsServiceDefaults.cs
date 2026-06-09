// <copyright file="ProjectsServiceDefaults.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.ServiceDefaults;

using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

/// <summary>
/// Shared Aspire service defaults for Hexalith.Projects hosts.
/// </summary>
public static class ProjectsServiceDefaults
{
    private const string HealthEndpointPath = "/health";
    private const string AliveEndpointPath = "/alive";
    private const string ReadyEndpointPath = "/ready";

    /// <summary>Gets the module name used in host diagnostics.</summary>
    public static string Name => "Hexalith.Projects.ServiceDefaults";

    /// <summary>Adds telemetry, health checks, service discovery, and outbound HTTP resilience.</summary>
    /// <typeparam name="TBuilder">The host builder type.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.ConfigureOpenTelemetry();
        _ = builder.AddDefaultHealthChecks();

        _ = builder.Services.AddServiceDiscovery();
        _ = builder.Services.ConfigureHttpClientDefaults(http =>
        {
            _ = http.AddStandardResilienceHandler();
            _ = http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>Configures logs, metrics, traces, and optional OTLP export.</summary>
    /// <typeparam name="TBuilder">The host builder type.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = false;
            logging.IncludeScopes = true;
        });
        _ = builder.Logging.AddJsonConsole(options => options.UseUtcTimestamp = true);

        _ = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddSource("Hexalith.Projects")
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                        && !context.Request.Path.StartsWithSegments(AliveEndpointPath)
                        && !context.Request.Path.StartsWithSegments(ReadyEndpointPath);
                })
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            _ = builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>Adds default liveness and readiness health checks.</summary>
    /// <typeparam name="TBuilder">The host builder type.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

        return builder;
    }

    /// <summary>Maps health, liveness, and readiness endpoints.</summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same application for chaining.</returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        Dictionary<HealthStatus, int> statusCodes = new()
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
        };

        HealthCheckOptions allChecks = new()
        {
            ResultStatusCodes = statusCodes,
        };
        HealthCheckOptions liveChecks = new()
        {
            Predicate = registration => registration.Tags.Contains("live"),
            ResultStatusCodes = statusCodes,
        };
        HealthCheckOptions readyChecks = new()
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResultStatusCodes = statusCodes,
        };

        if (app.Environment.IsDevelopment())
        {
            allChecks.ResponseWriter = WriteMetadataOnlyHealthResponseAsync;
            readyChecks.ResponseWriter = WriteMetadataOnlyHealthResponseAsync;
        }

        _ = app.MapHealthChecks(HealthEndpointPath, allChecks);
        _ = app.MapHealthChecks(AliveEndpointPath, liveChecks);
        _ = app.MapHealthChecks(ReadyEndpointPath, readyChecks);

        return app;
    }

    private static async Task WriteMetadataOnlyHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        Utf8JsonWriter writer = new(context.Response.Body, new JsonWriterOptions { Indented = true });
        await using (writer.ConfigureAwait(false))
        {
            writer.WriteStartObject();
            writer.WriteString("status", report.Status.ToString());
            writer.WriteStartObject("results");

            foreach (KeyValuePair<string, HealthReportEntry> entry in report.Entries)
            {
                writer.WriteStartObject(entry.Key);
                writer.WriteString("status", entry.Value.Status.ToString());
                writer.WriteString("description", entry.Value.Description);
                writer.WriteString("duration", entry.Value.Duration.ToString());
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
            await writer.FlushAsync(context.RequestAborted).ConfigureAwait(false);
        }
    }
}
