#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>Captures every emitted provider record and exported span during each actual gateway observation.</summary>
internal sealed class ProjectReadDenialObserver : IDisposable
{
    private readonly ConcurrentQueue<string> _logs = new();
    private readonly ConcurrentQueue<string> _traces = new();
    private readonly ConcurrentDictionary<ActivityTraceId, (int Logs, int Traces)> _starts = new();
    private readonly ConcurrentDictionary<ActivityTraceId, Activity> _requests = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _boundaries = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<ActivityTraceId, TaskCompletionSource<(double Duration, string Category)>> _metrics = new();
    private readonly ConcurrentDictionary<ActivityTraceId, TaskCompletionSource> _activities = new();
    private readonly ConcurrentDictionary<ActivityTraceId, ActivitySpanId> _gatewaySpans = new();
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private const string GatewayRequestMarker = "story61.actual-gateway-request";

    /// <summary>Gets the common source-owned category used by the substitution control.</summary>
    internal static string[] LogCategories { get; } = ["Hexalith.EventStore.QueryBoundary"];

    /// <summary>Gets the actual exporter installed on the real SDK tracer provider.</summary>
    internal ProjectReadDenialTraceExporter TraceExporter { get; }

    /// <summary>Observes exact outer completion and metrics without treating raw Activity flags as export proof.</summary>
    internal ProjectReadDenialObserver()
    {
        TraceExporter = new(this);
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == "Microsoft.AspNetCore.Hosting.HttpRequestIn" && IsBoundGatewaySpan(activity)
                    && _activities.TryGetValue(activity.TraceId, out TaskCompletionSource? completion)) completion.TrySetResult();
            },
        };
        ActivitySource.AddActivityListener(_activityListener);
        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Microsoft.AspNetCore.Hosting" && instrument.Name == "http.server.request.duration")
                    listener.EnableMeasurementEvents(instrument);
            },
        };
        _meterListener.SetMeasurementEventCallback<double>((_, duration, tags, _) =>
        {
            if (Activity.Current is not { } activity || !IsBoundGatewaySpan(activity)
                || !_metrics.TryGetValue(activity.TraceId, out var completion)) return;
            Dictionary<string, string?> category = new(StringComparer.Ordinal);
            foreach (var tag in tags) category.Add(tag.Key, Convert.ToString(tag.Value, CultureInfo.InvariantCulture));
            if (category.GetValueOrDefault("http.route") != "api/v1/queries") return;
            completion.TrySetResult((duration, JsonSerializer.Serialize(category.OrderBy(tag => tag.Key, StringComparer.Ordinal))));
        });
        _meterListener.Start();
    }

    /// <summary>Begins an independent trace and a complete provider/exporter observation window.</summary>
    internal Activity BeginRequest()
    {
        Activity activity = new Activity("story61-denial-observation").SetIdFormat(ActivityIdFormat.W3C)
            .SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded).Start();
        _starts[activity.TraceId] = (_logs.Count, _traces.Count);
        _requests[activity.TraceId] = activity;
        _boundaries[activity.TraceId.ToString()] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _metrics[activity.TraceId] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _activities[activity.TraceId] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        return activity;
    }

    /// <summary>Gets completed UTC windows for source-bound native console comparisons.</summary>
    internal object[] CompletedRequestWindows()
        => _requests.Values.OrderBy(activity => activity.StartTimeUtc).Select(activity =>
        {
            if (activity.Duration <= TimeSpan.Zero)
                throw new InvalidOperationException("An observation window has not completed.");
            return (object)new { Trace = activity.TraceId.ToString(), StartUtc = activity.StartTimeUtc,
                EndUtc = activity.StartTimeUtc + activity.Duration };
        }).ToArray();

    /// <summary>Marks the actual registered outer request for completion and duration measurement.</summary>
    internal void BindGatewayRequest(HttpContext context)
    {
        if (context.Request.Path != "/api/v1/queries" || Activity.Current is not { } activity || !_metrics.ContainsKey(activity.TraceId)) return;
        if (!context.Request.Headers["traceparent"].ToString().EndsWith("-01", StringComparison.Ordinal))
            throw new InvalidOperationException("The external observation must enter the real gateway with sampled trace context.");
        if (!_gatewaySpans.TryAdd(activity.TraceId, activity.SpanId))
            throw new InvalidOperationException("A denial observation must bind exactly one actual gateway server span.");
        activity.SetCustomProperty(GatewayRequestMarker, true);
    }

    private bool IsBoundGatewaySpan(Activity activity)
        => activity.GetCustomProperty(GatewayRequestMarker) is true
            && _gatewaySpans.TryGetValue(activity.TraceId, out ActivitySpanId span) && span == activity.SpanId;

    /// <summary>Returns the real measurement and dimensions without a timing threshold.</summary>
    internal Task<(double Duration, string Category)> MetricAsync(ActivityTraceId trace, CancellationToken cancellationToken)
        => _metrics[trace].Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

    /// <summary>Returns every actual exported span after the outer request ends.</summary>
    internal async Task<string> ActivityAsync(ActivityTraceId trace, CancellationToken cancellationToken)
    {
        await _activities[trace].Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        return ExportedActivities(trace);
    }

    /// <summary>Returns every exporter record in the window without weakening the separate root completion gate.</summary>
    internal string ExportedActivities(ActivityTraceId trace)
        => JsonSerializer.Serialize(_traces.ToArray().Skip(_starts[trace].Traces));

    /// <summary>Returns all emitted logs, including unexpected categories and unbound traces.</summary>
    internal string[] Logs(ActivityTraceId trace) => _logs.ToArray().Skip(_starts[trace].Logs).ToArray();

    /// <summary>Waits for actual final-status emission before returning the complete log window.</summary>
    internal async Task<string[]> LogsAsync(ActivityTraceId trace, CancellationToken cancellationToken)
    {
        await _boundaries[trace.ToString()].Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        return Logs(trace);
    }

    /// <summary>Snapshots every provider record, exception and scope without category or trace filtering.</summary>
    internal void RecordLog<TState>(string category, LogLevel level, EventId eventId, TState state, Exception? exception,
        IExternalScopeProvider? scopes = null, string? message = null)
    {
        KeyValuePair<string, object?>[] fields = state is IEnumerable<KeyValuePair<string, object?>> structured ? structured.ToArray() : [];
        List<KeyValuePair<string, object?>> scopeValues = [];
        scopes?.ForEachScope(static (scope, values) =>
        {
            if (scope is IEnumerable<KeyValuePair<string, object?>> entries) values.AddRange(entries);
            else values.Add(new("Scope", scope));
        }, scopeValues);
        _logs.Enqueue(JsonSerializer.Serialize(new
        {
            Category = category, Level = level, eventId.Id, Message = message ?? RenderProviderValue(state),
            State = fields.OrderBy(field => field.Key, StringComparer.Ordinal).Select(field => new { field.Key, Value = SnapshotLogValue(field.Value) }).ToArray(),
            Scopes = scopeValues.OrderBy(field => field.Key, StringComparer.Ordinal).Select(field => new { field.Key, Value = SnapshotLogValue(field.Value) }).ToArray(),
            Trace = Activity.Current?.TraceId.ToString(), Span = Activity.Current?.SpanId.ToString(),
            ExceptionType = exception?.GetType().FullName, ExceptionMessage = RenderProviderValue(exception),
        }));
        string? correlation = fields.FirstOrDefault(field => field.Key == "CorrelationId").Value?.ToString();
        if (category == "Hexalith.EventStore.QueryBoundary" && eventId.Id == 5510 && correlation is not null
            && _boundaries.TryGetValue(correlation, out TaskCompletionSource? completion)) completion.TrySetResult();
    }

    // Preserve provider text for arbitrary CLR state instead of serializing executable endpoint/reflection graphs.
    private static object? SnapshotLogValue(object? value)
    {
        try
        {
            return value switch
            {
                null or string or bool or byte or sbyte or short or ushort or int or uint or long or ulong
                    or decimal or DateTime or DateTimeOffset or Guid => value,
                float number when float.IsFinite(number) => number,
                double number when double.IsFinite(number) => number,
                JsonElement element when element.ValueKind != JsonValueKind.Undefined => element.Clone(),
                _ => new { Type = value.GetType().FullName, Text = RenderProviderValue(value) },
            };
        }
        catch (Exception error)
        {
            return new { Type = value?.GetType().FullName, RenderingFailure = error.GetType().FullName };
        }
    }

    /// <summary>Retains an explicit marker when a provider value cannot render itself.</summary>
    internal static string? RenderProviderValue(object? value)
    {
        if (value is null) return null;
        try { return Convert.ToString(value, CultureInfo.InvariantCulture); }
        catch (Exception error) { return "[rendering-failed:" + error.GetType().FullName + "]"; }
    }

    /// <summary>Snapshots every actual exported Activity without omitting sources or child spans.</summary>
    internal void RecordExportedActivity(Activity activity)
        => _traces.Enqueue(JsonSerializer.Serialize(new
        {
            Source = activity.Source.Name, activity.OperationName, activity.Kind, activity.Status,
            Trace = activity.TraceId.ToString(), Span = activity.SpanId.ToString(), Parent = activity.ParentSpanId.ToString(),
            Tags = activity.TagObjects.OrderBy(tag => tag.Key, StringComparer.Ordinal).Select(tag => new { tag.Key, Value = SnapshotLogValue(tag.Value) }).ToArray(),
            Events = activity.Events.Select(item => new { item.Name, Tags = item.Tags.OrderBy(tag => tag.Key, StringComparer.Ordinal).Select(tag => new { tag.Key, Value = SnapshotLogValue(tag.Value) }).ToArray() }).ToArray(),
            Links = activity.Links.Select(link => new { Trace = link.Context.TraceId.ToString(), Span = link.Context.SpanId.ToString(), Tags = link.Tags?.OrderBy(tag => tag.Key, StringComparer.Ordinal).Select(tag => new { tag.Key, Value = SnapshotLogValue(tag.Value) }).ToArray() }).ToArray(),
        }));

    /// <inheritdoc/>
    public void Dispose()
    {
        _meterListener.Dispose();
        _activityListener.Dispose();
        TraceExporter.Dispose();
    }
}
#endif
