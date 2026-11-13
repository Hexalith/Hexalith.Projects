using System.Diagnostics;
using System.Reflection;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace Hexalith.PrivateReceiverFixture;

/// <summary>Owns one prepared actual receiver entry and keeps all synthetic values and assertions in memory.</summary>
internal sealed class CaseHandle
{
    private readonly CaseSpec _spec;
    private readonly Action<int>? _flowProbe;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly InvalidOperationException _nextFailure = new("synthetic-next-failure");
    private readonly RouteCatalogUnavailableException _acquireFailure = new("synthetic-acquire-refusal");
    private readonly RouteCatalogUnavailableException _firstReleaseFailure = new("synthetic-first-release-failure");
    private readonly InvalidOperationException _secondReleaseFailure = new("synthetic-second-release-failure");
    private readonly IOException _overrideReleaseFailure = new("synthetic-release-overrides-next");
    private readonly long[] _releaseStarts = new long[2];
    private readonly long[] _releaseEnds = new long[2];
    private readonly MemoryStream _requestBody = new("synthetic-owned-body"u8.ToArray());
    private object? _nativeFixture;
    private object? _technicalFixture;
    private IServiceScope? _requestScope;
    private RequestDelegate _pipeline = null!;
    private DefaultHttpContext _context = null!;
    private CommittedRouteCatalogSnapshot? _snapshot;
    private RouteCatalogDispatchLease? _lease;
    private OperationCanceledException _nextCancellation = null!;
    private FixtureBoundary? _entryBoundary;
    private bool _suppressEntry;
    private bool _firstDependencyProbe;
    private bool _firstSuppressionMatched = true;
    private bool _tokenMatched = true;
    private bool _remoteMatched = true;
    private bool _releaseBoundaryMatched = true;
    private bool _nextContextMatched = true;
    private int _executed;
    private int _acquires;
    private int _nextCalls;
    private int _releases;
    private int _faults;

    private CaseHandle(CaseSpec spec, Action<int>? flowProbe) { _spec = spec; _flowProbe = flowProbe; }

    /// <summary>Prepares all real framework/fixture metadata before the diagnostic window.</summary>
    internal static async Task<CaseHandle> CreateAsync(CaseSpec spec, Action<int>? flowProbe)
    {
        var handle = new CaseHandle(spec, flowProbe);
        try { await handle.PrepareAsync().ConfigureAwait(false); return handle; }
        catch { await handle.DisposeAsync().ConfigureAwait(false); throw; }
    }

    private async Task PrepareAsync()
    {
        if (_spec.Technical)
        {
            IRouteCatalogAuthority authority = Substitute.For<IRouteCatalogAuthority>();
            _technicalFixture = await CreateTechnicalFixtureAsync(authority, terminal: true).ConfigureAwait(false);
            WebApplication app = FixtureReflection.Property<WebApplication>(_technicalFixture, "App");
            _pipeline = ((IApplicationBuilder)app).Build();
            RouteEndpoint endpoint = Endpoints(app).Single(value => value.RoutePattern.RawText?.Trim('/') == "api/v1/admin/storage/snapshot-policy"
                && value.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains("PUT"));
            PrepareContext(app, endpoint, "PUT");
            HttpClient client = FixtureReflection.Property<HttpClient>(_technicalFixture, "Client");
            foreach (var header in client.DefaultRequestHeaders) _context.Request.Headers[header.Key] = header.Value.ToArray();
            if (_spec.Fault == "auth") _context.Request.Headers.Remove("Authorization");
            object catalog = FixtureReflection.Property<object>(_technicalFixture, "Catalog");
            RouteCatalogActivation activation = (RouteCatalogActivation)FixtureReflection.Invoke(catalog, "Candidate")!;
            if (_spec.Fault is "grant" or "release-first" or "release-both")
                activation = activation with { TechnicalHttpReceivers = activation.TechnicalHttpReceivers!.Select(receiver => receiver with { Invocations = [] }).ToArray() };
            _snapshot = new(activation, 1, "eventstore", Coordinates(catalog));
            var receiver = app.Services.GetRequiredService<TechnicalHttpRouteCatalogReceiver>();
            var pass = new TechnicalHttpObservationPass(app.Services);
            object?[] arguments = [endpoint, pass, null];
            string? fingerprint = (string?)typeof(TechnicalHttpRouteCatalogReceiver).GetMethod("Fingerprint", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(receiver, arguments);
            pass.Complete();
            Check.That(fingerprint?.StartsWith("technical:", StringComparison.Ordinal) == true
                && arguments[2] is RouteCatalogTechnicalHttpEndpoint descriptor
                && TechnicalHttpCatalogBinding.Contains(_snapshot.TechnicalHttpReceivers, "eventstore", descriptor), "actual-technical-binding-prerequisite");
            bool granted = _snapshot.TechnicalHttpReceivers.Any(receiver => receiver.AppId == _snapshot.LocalAppId
                && receiver.Invocations?.Any(invocation => invocation.HttpMethods.Contains(_context.Request.Method, StringComparer.Ordinal)
                    && DevelopmentTechnicalHttpTransport.GrantAllows(invocation.Method, _context.Request.Path.Value!)) == true);
            Check.That(granted == (_spec.Fault is not ("grant" or "release-first" or "release-both")), "actual-exact-technical-path-grant-prerequisite");
            Configure(authority);
        }
        else
        {
            _nativeFixture = FixtureReflection.Create("ReceiverImagePrecheckFixture", true);
            WebApplication app = FixtureReflection.Property<WebApplication>(_nativeFixture, "App");
            FixtureReflection.Set(_nativeFixture, "OnNext", (Action<HttpContext>)NativeNext);
            _pipeline = FixtureReflection.Field<RequestDelegate>(_nativeFixture, "_pipeline");
            bool discovery = _spec.Fault is "bypass" or "channel" or "discovery";
            string path = discovery ? "dapr/config" : "actors/{actorTypeName}/{actorId}/method/{methodName}";
            RouteEndpoint endpoint = (RouteEndpoint)FixtureReflection.Invoke(_nativeFixture, "Endpoint", new Dictionary<string, object?> { ["path"] = path })!;
            PrepareContext(app, endpoint, discovery ? "GET" : "PUT");
            _context.Request.Path = discovery ? "/dapr/config" : "/actors/AggregateActor/synthetic-actor/method/ProcessCommandAsync";
            _context.Request.RouteValues["actorTypeName"] = "AggregateActor";
            _context.Request.RouteValues["actorId"] = "synthetic-actor";
            _context.Request.RouteValues["methodName"] = _spec.Fault == "allows" ? "UnregisteredMethod" : "ProcessCommandAsync";
            if (_spec.Fault != "bypass")
                _context.Request.Headers[DaprAppChannelToken.HeaderName] = _spec.Fault == "channel" ? "invalid-synthetic-channel"
                    : app.Configuration[DaprAppChannelToken.ConfigurationKey];
            if (_spec.Acquires > 0)
            {
                FixtureReflection.Invoke(_nativeFixture, "CaptureNativeBaseline");
                // Reuse actual captured SDK MVC membership, not a fabricated technical descriptor.
                _technicalFixture = await CreateTechnicalFixtureAsync(authority: null, terminal: false).ConfigureAwait(false);
                object catalog = FixtureReflection.Property<object>(_technicalFixture, "Catalog");
                RouteCatalogActivation activation = (RouteCatalogActivation)FixtureReflection.Invoke(catalog, "Candidate")!;
                RouteCatalogNativeActorTransport native = app.Services.GetRequiredService<IDevelopmentNativeActorReceiver>().Observe(endpoint)!;
                Check.That(native is not null, "actual-closed-native-descriptor");
                activation = activation with { AuxiliaryOperations = _spec.Fault == "snapshot" ? []
                    : [new("eventstore", endpoint.RoutePattern.RawText!, "sdk-native-actor:method", "sdk") { NativeActor = native }] };
                _snapshot = new(activation, 1, "eventstore", Coordinates(catalog));
                Check.That(_snapshot.Envelope.SchemaVersion == 3 && _snapshot.Envelope.Profile == "Development"
                    && _snapshot.ActivationBindingDigest is not null, "genuine-schema3-v2-native-prerequisite");
            }
            Configure(app.Services.GetRequiredService<IRouteCatalogAuthority>());
        }
        _nextCancellation = new OperationCanceledException("synthetic-next-cancellation", _cancellation.Token);
    }

    private async Task<object> CreateTechnicalFixtureAsync(IRouteCatalogAuthority? authority, bool terminal)
    {
        object fixture = FixtureReflection.Create("TechnicalHttpCatalogFixture", null, true);
        try
        {
            Action<IServiceCollection> services = collection => { if (authority is not null) collection.AddSingleton(authority); };
            Action<WebApplication> endpoints = app => { if (terminal) ((IApplicationBuilder)app).Use(_ => TechnicalNextAsync); };
            await ((Task)FixtureReflection.Invoke(fixture, "StartAsync", new Dictionary<string, object?>
            { ["token"] = CancellationToken.None, ["services"] = services, ["endpoints"] = endpoints, ["activate"] = false })!).ConfigureAwait(false);
            return fixture;
        }
        catch { await FixtureReflection.DisposeAsync(fixture).ConfigureAwait(false); throw; }
    }

    private static IEnumerable<RouteEndpoint> Endpoints(WebApplication app)
        => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();

    private static RouteCatalogStateCoordinates Coordinates(object catalog)
        => FixtureReflection.Property<RouteCatalogStateCoordinates>(FixtureReflection.Property<object>(catalog, "Store"), "Coordinates");

    private void PrepareContext(WebApplication app, RouteEndpoint endpoint, string verb)
    {
        _requestScope = app.Services.CreateScope();
        _context = new DefaultHttpContext { RequestServices = _requestScope.ServiceProvider, RequestAborted = _cancellation.Token };
        _context.SetEndpoint(endpoint);
        _context.Request.Method = verb;
        _context.Request.Scheme = "http";
        _context.Request.Host = new HostString("localhost");
        _context.Request.Path = "/" + endpoint.RoutePattern.RawText!.TrimStart('/');
        _context.Request.Body = _requestBody;
        _context.Response.Body = new MemoryStream();
    }

    private void Configure(IRouteCatalogAuthority authority)
        => authority.AcquireAsync(Arg.Any<CancellationToken>()).Returns(call => Acquire(call.Arg<CancellationToken>()));

    private Task<RouteCatalogDispatchLease> Acquire(CancellationToken token)
    {
        _acquires++;
        _tokenMatched &= token == _context.RequestAborted;
        FirstDependency();
        return AcquireAfterYieldAsync();
    }

    private async Task<RouteCatalogDispatchLease> AcquireAfterYieldAsync()
    {
        await Task.Yield();
        _flowProbe?.Invoke(1);
        if (_spec.Fault == "acquire") { _faults++; throw _acquireFailure; }
        _lease = new(_snapshot!, ReleaseAsync);
        return _lease;
    }

    private void FirstDependency()
    {
        if (_firstDependencyProbe) return;
        _firstDependencyProbe = true;
        _firstSuppressionMatched = ExecutionContext.IsFlowSuppressed() == _suppressEntry;
        _flowProbe?.Invoke(0);
    }

    private void EnterNext(HttpContext context)
    {
        _nextCalls++;
        if (_acquires == 0) FirstDependency();
        _nextContextMatched &= ReferenceEquals(context, _context);
        _tokenMatched &= context.RequestAborted == _cancellation.Token;
        _remoteMatched &= _spec.Acquires > 0 ? RouteCatalogRemoteExecution.Current is not null
            && !ReferenceEquals(RouteCatalogRemoteExecution.Current, _entryBoundary)
            : ReferenceEquals(RouteCatalogRemoteExecution.Current, _entryBoundary);
        _flowProbe?.Invoke(2);
    }

    private void NativeNext(HttpContext context)
    {
        EnterNext(context);
        if (_spec.Fault == "next") { _faults++; throw _nextFailure; }
        if (_spec.Fault == "cancel") { _faults++; _cancellation.Cancel(); throw _nextCancellation; }
    }

    private async Task TechnicalNextAsync(HttpContext context)
    {
        EnterNext(context);
        await Task.Yield();
        if (_spec.Fault == "next-release") { _faults++; throw _nextFailure; }
        context.Response.StatusCode = 204;
    }

    private async Task ReleaseAsync()
    {
        int release = ++_releases;
        Check.That(release is 1 or 2, "at-most-two-original-release-callbacks");
        _releaseStarts[release - 1] = Stopwatch.GetTimestamp();
        _releaseBoundaryMatched &= ReferenceEquals(RouteCatalogRemoteExecution.Current, _suppressEntry ? null : _entryBoundary);
        await Task.Yield();
        _flowProbe?.Invoke(3);
        if (_spec.Fault is "release-first" or "release-both")
        {
            await Task.Delay(release == 1 ? 200 : 5).ConfigureAwait(false);
            _releaseEnds[release - 1] = Stopwatch.GetTimestamp();
            if (release == 1) { _faults++; throw _firstReleaseFailure; }
            if (_spec.Fault == "release-both") { _faults++; throw _secondReleaseFailure; }
        }
        else
        {
            _releaseEnds[release - 1] = Stopwatch.GetTimestamp();
            if (_spec.Fault == "next-release") { _faults++; throw _overrideReleaseFailure; }
        }
    }

    /// <summary>Executes exactly one actual middleware entry; all payload, identity and exception-object assertions remain local.</summary>
    internal async Task<object[]> ExecuteAsync(bool suppressEntry)
    {
        Check.That(Interlocked.Exchange(ref _executed, 1) == 0, "opaque-handle-is-single-entry");
        _suppressEntry = suppressEntry;
        var originalHeaders = _context.Request.Headers.ToDictionary(header => header.Key, header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
        IRouteCatalogRemoteBoundary? callerBoundary = RouteCatalogRemoteExecution.Current;
        _entryBoundary = new FixtureBoundary();
        RouteCatalogRemoteExecution.Current = _entryBoundary;
        Exception? observed = null;
        long ended = 0;
        bool callerRestored;
        try
        {
            Task pending;
            if (suppressEntry && !ExecutionContext.IsFlowSuppressed())
            {
                using (ExecutionContext.SuppressFlow()) pending = _pipeline(_context);
            }
            else pending = _pipeline(_context);
            try { await pending.ConfigureAwait(false); }
            catch (Exception error) { observed = error; }
            ended = Stopwatch.GetTimestamp();
            callerRestored = ReferenceEquals(RouteCatalogRemoteExecution.Current, _entryBoundary);
        }
        finally { RouteCatalogRemoteExecution.Current = callerBoundary; }

        Check.That(observed?.GetType() == _spec.ExpectedError, "exact-expected-exception-type");
        Exception? expectedObject = _spec.Fault switch
        { "next" => _nextFailure, "cancel" => _nextCancellation, "release-both" => _secondReleaseFailure, "next-release" => _overrideReleaseFailure, _ => null };
        Check.That(ReferenceEquals(observed, expectedObject), "original-exception-object-and-release-precedence");
        if (observed is OperationCanceledException canceled) Check.That(canceled.CancellationToken == _context.RequestAborted, "original-cancellation-token-identity");
        Check.That(_context.Response.StatusCode == _spec.ExpectedStatus, "exact-original-response-status");
        Check.That(_acquires == _spec.Acquires && _nextCalls == _spec.NextCalls && _releases == _spec.Releases, "exact-original-dependency-and-release-counts");
        Check.That(_faults == _spec.Faults, "every-requested-fault-fired-exactly");
        Check.That(_context.Request.Headers.Count == originalHeaders.Count && originalHeaders.All(header =>
            _context.Request.Headers.TryGetValue(header.Key, out var current) && current.SequenceEqual(header.Value)), "original-request-header-values-remain-owned");
        Check.That(ReferenceEquals(_context.Request.Body, _requestBody) && _requestBody.CanRead && _context.Response.Body.CanWrite,
            "middleware-does-not-dispose-caller-owned-streams");
        if (_lease is not null)
        {
            Check.That(FixtureReflection.Field<bool>(_lease, "_closedForExecution"), "original-lease-closed-for-new-execution");
            Check.That(FixtureReflection.Field<bool>(_lease, "_retained") == (!_spec.Technical && (_spec.Fault is "next" or "cancel")),
                "original-native-ambiguity-retention");
            Check.That(FixtureReflection.Field<bool>(_lease, "_disposed") == (_spec.Fault is not ("release-both" or "next-release")),
                "original-failed-release-remains-retryable");
        }
        Check.That(_tokenMatched && _nextContextMatched && _remoteMatched && _releaseBoundaryMatched && callerRestored, "original-token-context-remote-and-release-lifetimes");
        Check.That(_firstSuppressionMatched, "original-first-dependent-flow-restored");
        long minimum = 0, maximum = 0, firstHold = 0;
        bool doubleOrder = true;
        if (_spec.Fault is "release-first" or "release-both")
        {
            minimum = _releaseEnds[1] - _releaseStarts[1];
            maximum = ended - _releaseEnds[0];
            firstHold = _releaseEnds[0] - _releaseStarts[0];
            doubleOrder = _releases == 2 && _releaseStarts[0] < _releaseEnds[0]
                && _releaseEnds[0] <= _releaseStarts[1] && _releaseStarts[1] < _releaseEnds[1] && _releaseEnds[1] <= ended;
            Check.That(doubleOrder && minimum > 0 && maximum >= minimum && firstHold > maximum, "two-real-release-callbacks-have-disjoint-last-interval-bracket");
        }
        object observedType = observed is null ? null! : observed.GetType().FullName!;
        return [_spec.Name, true, 1, _acquires, _nextCalls, _releases, observedType, _faults,
            _tokenMatched, _remoteMatched, callerRestored, _spec.StageMask, minimum, maximum, firstHold, doubleOrder,
            _releaseBoundaryMatched, _nextContextMatched, _firstDependencyProbe, _firstSuppressionMatched];
    }

    /// <summary>Closes owned fixture resources after the original task and all expected callbacks have finished.</summary>
    internal async Task DisposeAsync()
    {
        _requestScope?.Dispose();
        if (_context is not null) await _context.Response.Body.DisposeAsync().ConfigureAwait(false);
        await FixtureReflection.DisposeAsync(_nativeFixture).ConfigureAwait(false);
        await FixtureReflection.DisposeAsync(_technicalFixture).ConfigureAwait(false);
        _cancellation.Dispose();
        _requestBody.Dispose();
    }
}
