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
using Hexalith.EventStore.ServiceDefaults;
using Hexalith.Projects.Contracts.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using OpenTelemetry;
using OpenTelemetry.Trace;
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
        observer.CompletedRequestWindows().Length.ShouldBe(1);
        using Activity second = observer.BeginRequest();
        Should.Throw<InvalidOperationException>(() => observer.CompletedRequestWindows());
        second.TraceId.ShouldNotBe(ambient.TraceId);
        second.TraceId.ShouldNotBe(firstTrace);
        observer.Logs(second.TraceId).ShouldBeEmpty();
    }

    /// <summary>Retains real provider output for non-scalar endpoint metadata without perturbing ordinary requests.</summary>
    [Fact]
    public async Task DenialObserverRetainsProviderMetadataScopesAndExceptionsWithoutSerializingExecutableGraphs()
    {
        using ProjectReadDenialObserver observer = new();
        using Activity trace = observer.BeginRequest();
        using ILoggerFactory factory = LoggerFactory.Create(logging => logging.AddProvider(new ProjectReadDenialLogProvider(observer)));
        ILogger logger = factory.CreateLogger("ordinary-endpoint-metadata");
        var metadata = new { Metadata = typeof(SupportedProjectGatewayHttpTests).GetMethod(nameof(DenialObserverCreatesIndependentTracesUnderAnAmbientActivity))! };
        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?> { ["EndpointMetadata"] = metadata });
        logger.LogWarning(new InvalidOperationException("ordinary-provider-control"), "Endpoint {Metadata} handled {Status}", metadata, 200);
        string[] records = observer.Logs(trace.TraceId);
        records.Length.ShouldBe(1);
        using JsonDocument record = JsonDocument.Parse(records[0]);
        JsonElement root = record.RootElement;
        root.GetProperty("Category").GetString().ShouldBe("ordinary-endpoint-metadata");
        root.GetProperty("Message").GetString()!.ShouldContain(nameof(DenialObserverCreatesIndependentTracesUnderAnAmbientActivity));
        root.GetProperty("ExceptionMessage").GetString()!.ShouldContain("ordinary-provider-control");
        var state = root.GetProperty("State").EnumerateArray().ToDictionary(field => field.GetProperty("Key").GetString()!, field => field.GetProperty("Value"));
        state["Metadata"].GetProperty("Type").GetString().ShouldNotBeNullOrWhiteSpace();
        state["Metadata"].GetProperty("Text").GetString()!.ShouldContain(nameof(DenialObserverCreatesIndependentTracesUnderAnAmbientActivity));
        state["Status"].GetInt32().ShouldBe(200);
        root.GetProperty("Scopes")[0].GetProperty("Value").GetProperty("Text").GetString()!.ShouldContain(nameof(DenialObserverCreatesIndependentTracesUnderAnAmbientActivity));
        Should.Throw<ShouldAssertException>(() => NormalizeDenialLog(records[0], trace.TraceId.ToString()));
        logger.LogWarning("Unrenderable diagnostic {Value}", new ProjectReadThrowingDiagnosticValue());
        string[] afterFailure = observer.Logs(trace.TraceId);
        afterFailure.Length.ShouldBe(2);
        afterFailure[1].ShouldContain("formatter-failed:System.InvalidOperationException");
        afterFailure[1].ShouldContain("rendering-failed:System.InvalidOperationException");
        afterFailure[1].ShouldContain(nameof(ProjectReadThrowingDiagnosticValue));

        ILogger counterfeit = factory.CreateLogger("Hexalith.EventStore.QueryBoundary");
        counterfeit.Log(LogLevel.Information, new EventId(5510),
            new Dictionary<string, object?> { ["CorrelationId"] = new ProjectReadThrowingDiagnosticValue() }, null,
            static (_, _) => "counterfeit boundary control");
        string[] afterCorrelation = observer.Logs(trace.TraceId);
        afterCorrelation.Length.ShouldBe(3);
        afterCorrelation[2].ShouldContain("rendering-failed:System.InvalidOperationException");
        Should.Throw<ShouldAssertException>(() => NormalizeDenialLog(afterCorrelation[2], trace.TraceId.ToString()));
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => observer.LogsAsync(trace.TraceId, cancellation.Token));

        using ActivitySource source = new("story61-provider-metadata-control");
        using TracerProvider provider = Sdk.CreateTracerProviderBuilder().AddSource(source.Name)
            .AddProcessor(new SimpleActivityExportProcessor(observer.TraceExporter)).Build();
        using (Activity span = source.StartActivity("ordinary-exported-span", ActivityKind.Internal, trace.Context,
                   tags: new ActivityTagsCollection { ["Metadata"] = metadata, ["Unrenderable"] = new ProjectReadThrowingDiagnosticValue() },
                   links: [new ActivityLink(trace.Context, new ActivityTagsCollection { ["Metadata"] = metadata })])!)
        {
            span.ShouldNotBeNull();
            span.AddEvent(new ActivityEvent("ordinary-exported-event", tags: new ActivityTagsCollection { ["Metadata"] = metadata }));
        }
        using JsonDocument exported = JsonDocument.Parse(observer.ExportedActivities(trace.TraceId));
        exported.RootElement.GetArrayLength().ShouldBe(1);
        using JsonDocument exportedRecord = JsonDocument.Parse(exported.RootElement[0].GetString()!);
        JsonElement exportedSpan = exportedRecord.RootElement;
        exportedSpan.GetProperty("Tags").GetArrayLength().ShouldBe(2);
        exportedSpan.GetProperty("Events")[0].GetProperty("Tags")[0].GetProperty("Value").GetProperty("Text").GetString()!
            .ShouldContain(nameof(DenialObserverCreatesIndependentTracesUnderAnAmbientActivity));
        exportedSpan.GetProperty("Links")[0].GetProperty("Tags")[0].GetProperty("Value").GetProperty("Text").GetString()!
            .ShouldContain(nameof(DenialObserverCreatesIndependentTracesUnderAnAmbientActivity));
        exported.RootElement.ToString().ShouldContain("rendering-failed:System.InvalidOperationException");
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
        observer.Logs(request.TraceId).Length.ShouldBe(1, "Unbound nested log evidence must remain observable even though it cannot complete the root request.");
    }

    /// <summary>Exercises the actual SDK HTTP boundary independently of Dapr and domain authority fixtures.</summary>
    [Fact]
    public async Task SupportedHandlerDenialsHaveSameHeadersSdkLogsAndHttpTelemetryCategories()
    {
        const string tenant = "denial-observability";
        using ProjectReadDenialObserver observer = new();
        var receiverBuilder = WebApplication.CreateBuilder();
        receiverBuilder.Configuration["urls"] = "http://127.0.0.1:0";
        receiverBuilder.ConfigureOpenTelemetry();
        receiverBuilder.Logging.AddProvider(new ProjectReadDenialLogProvider(observer));
        receiverBuilder.Logging.AddFilter<ProjectReadDenialLogProvider>(static (_, _) => true);
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

    private static async Task<(ActivityTraceId Trace, string Body, string Headers, string[] Logs, string Metric, string Activity, double Duration)> ObserveDenialAsync(
        HttpClient client, SubmitQueryRequest query, ProjectReadDenialObserver observer)
    {
        using Activity trace = observer.BeginRequest();
        using HttpResponseMessage response = await SubmitObservedQueryAsync(client, query, trace.TraceId).ConfigureAwait(false);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false));
        string correlation = body.RootElement.GetProperty("correlationId").GetString()!;
        CorrelationIdMiddleware.IsValidIdentifier(correlation).ShouldBeTrue();
        correlation.ShouldBe(trace.TraceId.ToString());
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
        string[] logs = (await observer.LogsAsync(trace.TraceId, TestContext.Current.CancellationToken).ConfigureAwait(false))
            .Select(log => NormalizeDenialLog(log, correlation)).ToArray();
        logs.Length.ShouldBe(1);
        activity.ShouldNotBeNullOrWhiteSpace();
        return (trace.TraceId, canonicalBody, JsonSerializer.Serialize(headers.OrderBy(header => header.Key, StringComparer.Ordinal)), logs, measurement.Category, activity, measurement.Duration);
    }

    private static async Task<HttpResponseMessage> SubmitObservedQueryAsync(HttpClient client, SubmitQueryRequest query, ActivityTraceId trace)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/queries") { Content = JsonContent.Create(query) };
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, trace.ToString());
        // The initiating test client is outside the service chain. The real gateway separately requires incoming sampled (-01) context.
        using IDisposable initiatingClientOnly = SuppressInstrumentationScope.Begin();
        return await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeDenialLog(string log, string correlation)
    {
        JsonObject entry = JsonNode.Parse(log)!.AsObject();
        entry["Category"]!.GetValue<string>().ShouldBe("Hexalith.EventStore.QueryBoundary");
        entry["Id"]!.GetValue<int>().ShouldBe(5510);
        entry["Message"]!.GetValue<string>().ShouldBe($"Query boundary completed: CorrelationId={correlation}, StatusCode=404, Category=QueryBoundary");
        entry["Message"] = "Query boundary completed: CorrelationId=[request-correlation], StatusCode=404, Category=QueryBoundary";
        entry["Scopes"]!.AsArray().ShouldBeEmpty();
        entry["Trace"].ShouldBeNull(); entry["Span"].ShouldBeNull();
        entry["ExceptionType"].ShouldBeNull(); entry["ExceptionMessage"].ShouldBeNull();
        entry["State"]!.AsArray().Select(field => field!["Key"]!.GetValue<string>()).Order(StringComparer.Ordinal)
            .ShouldBe(new[] { "CorrelationId", "StatusCode", "{OriginalFormat}" }.Order(StringComparer.Ordinal));
        foreach (JsonNode? field in entry["State"]!.AsArray())
        {
            string name = field!["Key"]!.GetValue<string>();
            if (name == "StatusCode") field["Value"]!.GetValue<int>().ShouldBe(404);
            if (name != "CorrelationId") continue;
            field["Value"]!.GetValue<string>().ShouldBe(correlation, "Only the exact source-owned request correlation may be normalized.");
            field["Value"] = "[request-correlation]";
        }
        return entry.ToJsonString();
    }

    private static void AssertSameDenialObservations(
        (ActivityTraceId Trace, string Body, string Headers, string[] Logs, string Metric, string Activity, double Duration) first,
        (ActivityTraceId Trace, string Body, string Headers, string[] Logs, string Metric, string Activity, double Duration) second)
    {
        first.Trace.ShouldNotBe(second.Trace);
        first.Body.ShouldBe(second.Body);
        first.Headers.ShouldBe(second.Headers);
        first.Logs.ShouldBe(second.Logs);
        first.Logs.Length.ShouldBe(1);
        first.Logs.Single().ShouldContain("Query boundary completed");
        // Standard duration metric dimensions classify both outcomes as the same HTTP 404.
        // Response-time side-channel tolerance is unspecified; durations are deliberately not compared.
        first.Metric.ShouldBe(second.Metric);
        using JsonDocument metric = JsonDocument.Parse(first.Metric);
        var dimensions = metric.RootElement.EnumerateArray().ToDictionary(tag => tag.GetProperty("Key").GetString()!, tag => tag.GetProperty("Value").GetString());
        dimensions["http.response.status_code"].ShouldBe("404");
        dimensions["http.route"].ShouldBe("api/v1/queries");
        first.Activity.ShouldBe(second.Activity);
        first.Activity.ShouldBe("[]", "No service-chain span may be emitted; the initiating external test client was excluded at its own source only.");
    }
}
#endif
