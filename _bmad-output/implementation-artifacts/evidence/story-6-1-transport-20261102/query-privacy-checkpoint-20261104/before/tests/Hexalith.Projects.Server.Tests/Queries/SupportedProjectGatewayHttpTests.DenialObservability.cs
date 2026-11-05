#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Middleware;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Projects.Contracts.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectGatewayHttpTests
{
    /// <summary>Prevents an ambient test trace from joining independent request observations.</summary>
    [Fact]
    public void DenialObserverCreatesIndependentTracesUnderAnAmbientActivity()
    {
        using Activity ambient = new Activity("ambient-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        using ProjectReadDenialObserver observer = new();
        ActivityTraceId firstTrace;
        using (Activity first = observer.BeginRequest())
        {
            firstTrace = first.TraceId;
            firstTrace.ShouldNotBe(ambient.TraceId);
            observer.Logs(firstTrace).ShouldBeEmpty();
        }
        Activity.Current.ShouldBeSameAs(ambient);
        using Activity second = observer.BeginRequest();
        second.TraceId.ShouldNotBe(ambient.TraceId);
        second.TraceId.ShouldNotBe(firstTrace);
        observer.Logs(second.TraceId).ShouldBeEmpty();
    }

    /// <summary>Rejects an earlier nested server observation even when it shares the registered trace and HTTP categories.</summary>
    [Fact]
    public async Task UnboundNestedServerObservationsCannotCompleteAGatewayRequest()
    {
        using ProjectReadDenialObserver observer = new();
        using Activity request = observer.BeginRequest();
        using ActivitySource source = new("Microsoft.AspNetCore");
        using Meter meter = new("Microsoft.AspNetCore.Hosting");
        Histogram<double> duration = meter.CreateHistogram<double>("http.server.request.duration");
        using (Activity nested = source.StartActivity("Microsoft.AspNetCore.Hosting.HttpRequestIn", ActivityKind.Server)!)
        {
            nested.ShouldNotBeNull();
            nested.TraceId.ShouldBe(request.TraceId);
            nested.SpanId.ShouldNotBe(request.SpanId);
            duration.Record(0.1, new("http.route", "api/v1/queries"), new("http.response.status_code", 404));
            observer.RecordLog(ProjectReadDenialObserver.LogCategories[0], Microsoft.Extensions.Logging.LogLevel.Information,
                new Microsoft.Extensions.Logging.EventId(1210), new Dictionary<string, object?> { ["AggregateId"] = "nested-owner-target" }, null);
        }
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => observer.MetricAsync(request.TraceId, cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => observer.ActivityAsync(request.TraceId, cancellation.Token));
        observer.Logs(request.TraceId).ShouldBeEmpty();
    }

    /// <summary>Exercises the actual SDK HTTP boundary independently of Dapr and domain authority fixtures.</summary>
    [Fact]
    public async Task SupportedHandlerDenialsHaveSameHeadersSdkLogsAndHttpTelemetryCategories()
    {
        const string tenant = "denial-observability";
        var receiverBuilder = WebApplication.CreateBuilder();
        receiverBuilder.Configuration["urls"] = "http://127.0.0.1:0";
        WebApplication receiver = receiverBuilder.Build();
        await using var receiverLifetime = receiver.ConfigureAwait(true);
        List<string> received = [];
        receiver.MapPost("/query", (QueryEnvelope query) =>
        {
            received.Add(query.AggregateId);
            // The owner outcomes have already been canonicalized; this control tests the downstream SDK boundary.
            return QueryResult.Failure("safe-denial");
        });
        await receiver.StartAsync(TestContext.Current.CancellationToken);
        var routes = new GatewayFixtureLocalRoutes();
        routes.Add("projects", receiver);
        using RSA ownerKey = RSA.Create(2048);
        using ProjectReadDenialObserver observer = new();
        WebApplication gateway = await StartGatewayAsync("http://127.0.0.1:1", "http://127.0.0.1:1", routes, ownerKey, tenant, observer);
        await using var gatewayLifetime = gateway.ConfigureAwait(true);
        gateway.Services.GetRequiredService<IQueryRouter>().ShouldBeOfType<Hexalith.EventStore.Queries.HandlerAwareQueryRouter>();
        using HttpClient client = new() { BaseAddress = new(gateway.Urls.Single()) };
        client.DefaultRequestHeaders.Authorization = new("Bearer", HumanToken(tenant, inspection: false));
        SubmitQueryRequest Open(string project) => new(tenant, "projects", project, GetProjectQuery.QueryType,
            Payload: JsonSerializer.SerializeToElement(new GetProjectQuery(), JsonSerializerOptions.Web));
        var absent = await ObserveDenialAsync(client, Open("absent"), observer);
        var denied = await ObserveDenialAsync(client, Open("denied"), observer);
        AssertSameDenialObservations(absent, denied);
        output.WriteLine("Actual SDK denial observations: " + JsonSerializer.Serialize(new
        {
            FirstTrace = absent.Trace.ToString(), SecondTrace = denied.Trace.ToString(),
            absent.Headers, absent.Logs, MetricCategory = absent.Metric, ServerActivity = absent.Activity,
        }));
        received.ShouldBe(["absent", "denied"]);
        routes.TransportResponses.ShouldBe(["projects:query:200", "projects:query:200"]);
        routes.HumanBearerForwarded.ShouldBeFalse();
    }

    private static async Task<(ActivityTraceId Trace, string Body, string Headers, string[] Logs, string Metric, string Activity)> ObserveDenialAsync(
        HttpClient client, SubmitQueryRequest query, ProjectReadDenialObserver observer)
    {
        using Activity trace = observer.BeginRequest();
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/queries", query, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false));
        string correlation = body.RootElement.GetProperty("correlationId").GetString()!;
        CorrelationIdMiddleware.IsValidIdentifier(correlation).ShouldBeTrue();
        if (response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var correlationHeaders))
            correlationHeaders.ShouldBe([correlation]);
        string canonicalBody = JsonSerializer.Serialize(body.RootElement.EnumerateObject().Where(property => property.Name != "correlationId")
            .OrderBy(property => property.Name, StringComparer.Ordinal).ToDictionary(property => property.Name, property => property.Value));
        Dictionary<string, string[]> headers = new(StringComparer.Ordinal);
        foreach (var header in response.Headers.Concat(response.Content.Headers))
        {
            string name = header.Key.ToLowerInvariant();
            if (name == "date")
            {
                DateTimeOffset.TryParseExact(header.Value.Single(), "r", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _).ShouldBeTrue();
                headers.Add(name, ["[http-date]"]);
            }
            else if (name == "x-correlation-id") headers.Add(name, ["[correlation-id]"]);
            else headers.Add(name, header.Value.ToArray());
        }
        var measurement = await observer.MetricAsync(trace.TraceId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        double.IsFinite(measurement.Duration).ShouldBeTrue();
        measurement.Duration.ShouldBeGreaterThan(0);
        string activity = await observer.ActivityAsync(trace.TraceId, TestContext.Current.CancellationToken).ConfigureAwait(false);
        string[] logs = observer.Logs(trace.TraceId).Select(log => NormalizeDenialLog(log, query, correlation)).ToArray();
        logs.Length.ShouldBe(3);
        activity.ShouldNotBeNullOrWhiteSpace();
        return (trace.TraceId, canonicalBody, JsonSerializer.Serialize(headers.OrderBy(header => header.Key, StringComparer.Ordinal)), logs, measurement.Category, activity);
    }

    private static string NormalizeDenialLog(string log, SubmitQueryRequest query, string correlation)
    {
        JsonObject entry = JsonNode.Parse(log)!.AsObject();
        foreach (JsonNode? field in entry["State"]!.AsArray())
        {
            string name = field!["Key"]!.GetValue<string>();
            string? expected = name switch { "TenantId" => query.Tenant, "AggregateId" => query.AggregateId, "CorrelationId" => correlation, _ => null };
            if (expected is null) continue;
            field["Value"]!.GetValue<string>().ShouldBe(expected, "Only this request's actual supplied coordinates may be normalized.");
            field["Value"] = "[request-coordinate]";
        }
        return entry.ToJsonString();
    }

    private static void AssertSameDenialObservations(
        (ActivityTraceId Trace, string Body, string Headers, string[] Logs, string Metric, string Activity) first,
        (ActivityTraceId Trace, string Body, string Headers, string[] Logs, string Metric, string Activity) second)
    {
        first.Trace.ShouldNotBe(second.Trace);
        first.Body.ShouldBe(second.Body);
        first.Headers.ShouldBe(second.Headers);
        first.Logs.ShouldBe(second.Logs);
        first.Logs.Length.ShouldBe(3);
        first.Logs.ShouldContain(log => log.Contains("QueryReceived", StringComparison.Ordinal));
        first.Logs.ShouldContain(log => log.Contains("QueryHandlerRouting", StringComparison.Ordinal));
        first.Logs.ShouldContain(log => log.Contains("Query not found", StringComparison.Ordinal));
        first.Logs.ShouldAllBe(log => !log.Contains("SafeDenialForbidden", StringComparison.Ordinal)
            && !log.Contains("SafeDenialMissingProjectionState", StringComparison.Ordinal));
        // Standard duration metric dimensions classify both outcomes as the same HTTP 404.
        // Response-time side-channel tolerance is unspecified; durations are deliberately not compared.
        first.Metric.ShouldBe(second.Metric);
        using JsonDocument metric = JsonDocument.Parse(first.Metric);
        var dimensions = metric.RootElement.EnumerateArray().ToDictionary(tag => tag.GetProperty("Key").GetString()!, tag => tag.GetProperty("Value").GetString());
        dimensions["http.response.status_code"].ShouldBe("404");
        dimensions["http.route"].ShouldBe("api/v1/queries");
        first.Activity.ShouldBe(second.Activity);
    }
}
#endif
