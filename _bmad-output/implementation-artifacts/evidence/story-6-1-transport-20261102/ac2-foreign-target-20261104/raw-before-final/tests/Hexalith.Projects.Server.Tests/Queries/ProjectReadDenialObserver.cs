#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>Captures actual SDK logs and standard server metric/activity classifications by trace.</summary>
internal sealed class ProjectReadDenialObserver : ILoggerProvider
{
    private readonly ConcurrentDictionary<ActivityTraceId, ConcurrentQueue<string>> _logs = new();
    private readonly ConcurrentDictionary<ActivityTraceId, TaskCompletionSource<(double Duration, string Category)>> _metrics = new();
    private readonly ConcurrentDictionary<ActivityTraceId, TaskCompletionSource<string>> _activities = new();
    private readonly ConcurrentDictionary<ActivityTraceId, ActivitySpanId> _gatewaySpans = new();
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private const string GatewayRequestMarker = "story61.actual-gateway-request";

    /// <summary>Gets SDK categories that can classify a supported handler refusal.</summary>
    internal static string[] LogCategories { get; } =
    [
        "Hexalith.EventStore.Queries.HandlerAwareQueryRouter",
        "Hexalith.EventStore.Server.Queries.SafeDenialQueryRouter",
        "Hexalith.EventStore.Server.Pipeline.SubmitQueryHandler",
        "Hexalith.EventStore.ErrorHandling.QueryNotFoundExceptionHandler",
    ];

    /// <summary>Enables actual hosting instruments without installing application telemetry policy.</summary>
    internal ProjectReadDenialObserver()
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == "Microsoft.AspNetCore.Hosting.HttpRequestIn" && IsBoundGatewaySpan(activity)
                    && _activities.TryGetValue(activity.TraceId, out var completion))
                    completion.TrySetResult(JsonSerializer.Serialize(new
                    {
                        activity.OperationName, activity.Kind, activity.Status,
                        Tags = activity.TagObjects.OrderBy(tag => tag.Key, StringComparer.Ordinal).ToArray(),
                        Events = activity.Events.Select(item => new { item.Name, Tags = item.Tags.OrderBy(tag => tag.Key, StringComparer.Ordinal).ToArray() }).ToArray(),
                    }));
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

    /// <summary>Begins an independent trace so concurrent HTTP fixtures cannot supply this test's observations.</summary>
    internal Activity BeginRequest()
    {
        Activity activity = new Activity("story61-denial-observation").SetIdFormat(ActivityIdFormat.W3C)
            .SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded).Start();
        _logs[activity.TraceId] = new();
        _metrics[activity.TraceId] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _activities[activity.TraceId] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        return activity;
    }

    /// <summary>Marks only the actual outer gateway activity whose trace was registered by this request.</summary>
    internal void BindGatewayRequest(HttpContext context)
    {
        if (context.Request.Path != "/api/v1/queries" || Activity.Current is not { } activity
            || !_metrics.ContainsKey(activity.TraceId)) return;
        if (!_gatewaySpans.TryAdd(activity.TraceId, activity.SpanId))
            throw new InvalidOperationException("A denial observation must bind exactly one actual gateway server span.");
        activity.SetCustomProperty(GatewayRequestMarker, true);
    }

    private bool IsBoundGatewaySpan(Activity activity)
        => activity.GetCustomProperty(GatewayRequestMarker) is true
            && _gatewaySpans.TryGetValue(activity.TraceId, out ActivitySpanId span) && span == activity.SpanId;

    /// <summary>Returns the real server measurement and tags, without a response-latency threshold.</summary>
    internal Task<(double Duration, string Category)> MetricAsync(ActivityTraceId trace, CancellationToken cancellationToken)
        => _metrics[trace].Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

    /// <summary>Returns the real server activity outcome, tags and events.</summary>
    internal Task<string> ActivityAsync(ActivityTraceId trace, CancellationToken cancellationToken)
        => _activities[trace].Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

    /// <summary>Gets unmodified structured SDK log fields for this request.</summary>
    internal string[] Logs(ActivityTraceId trace) => _logs[trace].ToArray();

    /// <summary>Records structured SDK state from only the selected request trace.</summary>
    internal void RecordLog<TState>(string category, LogLevel level, EventId eventId, TState state, Exception? exception)
    {
        if (!LogCategories.Contains(category, StringComparer.Ordinal) || Activity.Current is not { } activity
            || !_logs.TryGetValue(activity.TraceId, out var entries)) return;
        var fields = state is IEnumerable<KeyValuePair<string, object?>> structured ? structured : [];
        entries.Enqueue(JsonSerializer.Serialize(new
        {
            Category = category, Level = level, eventId.Id,
            State = fields.OrderBy(field => field.Key, StringComparer.Ordinal).ToArray(),
            ExceptionType = exception?.GetType().FullName, ExceptionMessage = exception?.Message,
        }));
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new ProjectReadDenialLogger(categoryName, this);

    /// <inheritdoc/>
    public void Dispose()
    {
        _meterListener.Dispose();
        _activityListener.Dispose();
    }
}
#endif
