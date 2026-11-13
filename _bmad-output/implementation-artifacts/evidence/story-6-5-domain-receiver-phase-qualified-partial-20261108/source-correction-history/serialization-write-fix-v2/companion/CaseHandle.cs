using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>Exercises the actual guard/filter/result with prepared immutable catalog inputs and synthetic collaborators.</summary>
internal sealed class CaseHandle
{
    private readonly CaseSpec _spec;
    private readonly Action<int>? _probe;
    private readonly CommittedRouteCatalogSnapshot _snapshot;
    private readonly DefaultEndpointFilterInvocationContext _context;
    private readonly ServiceProvider _services;
    private readonly ProbeResponseStream _body;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly List<RouteCatalogDispatchLease> _leases = [];
    private readonly HashSet<RouteCatalogDispatchLease> _released = [];
    private readonly DomainRouteCatalogFilter _outer;
    private readonly DomainRouteCatalogFilter? _inner;
    private readonly FixtureBoundary _boundary = new();
    private readonly InvalidOperationException _nextFault = new();
    private readonly IOException _releaseFault = new();
    private readonly RouteCatalogUnavailableException _acquireFault = new("synthetic-acquire");
    private readonly RouteCatalogUnavailableException _firstReleaseFault = new("synthetic-first-release");
    private readonly OperationCanceledException _releaseCancellation = new(CancellationToken.None);
    private OperationCanceledException? _requestCancellation;
    private bool _suppressed, _firstObserved, _entrySuppressionMatched = true, _scopeMatched = true;
    private int _used, _filterEntries, _acquires, _nextCalls, _releases, _faults, _writes, _refusals;
    private long _firstReleaseStart, _firstReleaseThrow, _lastReleaseStart, _lastReleaseEnd;

    internal CaseHandle(CaseSpec spec, Action<int>? probe)
    {
        _spec = spec; _probe = probe;
        bool terminal = spec.Name == "terminal-refusal-owned-result";
        object catalog = FixtureReflection.Catalog(terminal);
        var envelope = FixtureReflection.Property<RouteCatalogEnvelope>(catalog, "Envelope");
        var host = FixtureReflection.Property<RouteCatalogHostIdentity[]>(catalog, "Hosts").Single(h => h.AppId == "sample");
        _snapshot = terminal
            ? new CommittedRouteCatalogSnapshot(FixtureReflection.Invoke<RouteCatalogActivation>(catalog, "Candidate"), 1, "sample", null)
            : new CommittedRouteCatalogSnapshot(RouteCatalogCodec.Encode(envelope), RouteCatalogCodec.ComputeRootDigest(envelope), 1, localAppId: "sample");
        var services = new ServiceCollection(); services.AddLogging(); services.AddOptions();
        if (terminal)
        {
            var profile = new BoundedV1DomainSerializerProfile().Add<RefusalPayload>("synthetic-refusal", "json", 128,
                (value, stream, token) => JsonSerializer.SerializeAsync(stream, value, cancellationToken: token));
            services.AddSingleton(new DomainServiceTerminalRefusalRule("counter", "unsupported", (_, _, token) =>
            {
                Check.Require(token == _cancellation.Token, "terminal-refusal-original-token");
                _refusals++;
                return Task.FromResult(DomainServiceTerminalRefusal.Reject(new RefusalPayload("synthetic")));
            }, profile.Build(1024 * 1024)));
        }
        _services = services.BuildServiceProvider();
        var command = new CommandEnvelope("01ARZ3NDEKTSV4RRFFQ69G5FAV", "tenant-a", "counter", "aggregate",
            terminal ? "unsupported" : "increment-counter", "{}"u8.ToArray(), "correlation", null, "user", null);
        var http = new DefaultHttpContext(); http.RequestServices = _services; http.Request.Path = "/process"; http.Request.Method = "POST";
        http.RequestAborted = _cancellation.Token;
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(RouteCatalogSurfacePolicy.ClaimType, "service")], "synthetic"));
        http.Request.Headers[RouteCatalogInvocationHandler.RootHeader] = _snapshot.RootDigest;
        http.Request.Headers[RouteCatalogInvocationHandler.EpochHeader] = "1";
        if (_snapshot.ActivationBindingDigest is { } binding) http.Request.Headers[RouteCatalogInvocationHandler.BindingHeader] = binding;
        if (terminal) http.Request.Headers[RouteCatalogInvocationHandler.ProcessBindingHeader] = _snapshot.CompleteActivationBindingDigest;
        if (spec.Name == "guard-validation-fault") http.Request.Headers[RouteCatalogInvocationHandler.RootHeader] = new string('f', 64);
        _body = new ProbeResponseStream(ObserveWrite); http.Response.Body = _body;
        _context = new DefaultEndpointFilterInvocationContext(http, new DomainServiceRequest(command, null));
        var authority = Substitute.For<IRouteCatalogAuthority>();
        authority.AcquireAsync(Arg.Any<CancellationToken>()).Returns(call => AcquireAsync(call.Arg<CancellationToken>()));
        IDomainRouteCatalogGuard guard = new DomainRouteCatalogGuard(authority, Options.Create(new RouteCatalogHostOptions { Identity = host }));
        if (spec.Name.StartsWith("custom-", StringComparison.Ordinal) || spec.Name.StartsWith("filter-double-", StringComparison.Ordinal))
            guard = new CustomGuard(AcquireCustomAsync);
        _outer = new DomainRouteCatalogFilter(guard);
        if (spec.Entries == 2) _inner = new DomainRouteCatalogFilter(new DomainRouteCatalogGuard(authority, Options.Create(new RouteCatalogHostOptions { Identity = host })));
    }

    internal async Task<object[]> ExecuteAsync(bool suppressed)
    {
        Check.Require(Interlocked.Exchange(ref _used, 1) == 0, "single-use-fixture-handle"); _suppressed = suppressed;
        IRouteCatalogRemoteBoundary? prior = RouteCatalogRemoteExecution.Current;
        RouteCatalogRemoteExecution.Current = _boundary;
        Exception? observed = null;
        bool restored;
        try
        {
            object? returned;
            try
            {
                _filterEntries++;
                returned = _inner is null
                    ? await _outer.InvokeAsync(_context, NextAsync).ConfigureAwait(false)
                    : await _outer.InvokeAsync(_context, async context =>
                    {
                        ObserveNext(context);
                        _filterEntries++;
                        return await _inner.InvokeAsync(context, NextAsync).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                _probe?.Invoke(2);
                Check.Require(RouteCatalogRemoteCall.Current.Count == 0 && ReferenceEquals(RouteCatalogRemoteExecution.Current, ExpectedBoundary), "handoff-restores-original-caller-scope");
                if (returned is IResult result)
                {
                    if (_spec.Mask == 255)
                    {
                        Check.Require(result is DomainRouteCatalogResult && _leases.Count == _spec.Entries && _released.Count == 0, "owned-result-retains-every-lease-after-filter-return");
                        foreach (var lease in _leases) lease.RequireOpenForExecution();
                        await Task.Delay(20).ConfigureAwait(false); // Deliberate handoff gap, never an eager result execution.
                        Check.Require(_released.Count == 0, "handoff-gap-does-not-release-lease");
                    }
                    await result.ExecuteAsync(_context.HttpContext).ConfigureAwait(false);
                }
                else Check.Require(returned is string text && text == "synthetic-result", "nonresult-return-identity");
            }
            catch (Exception error) { observed = error; }
            _probe?.Invoke(6);
            restored = RouteCatalogRemoteCall.Current.Count == 0 && ReferenceEquals(RouteCatalogRemoteExecution.Current, ExpectedBoundary);
            Check.Require(restored, "caller-scope-after-result-and-release");
            AssertExpected(observed);
            long done = Stopwatch.GetTimestamp();
            bool doubleDispose = _spec.Name.StartsWith("filter-double-", StringComparison.Ordinal);
            long minimum = doubleDispose ? _lastReleaseEnd - _lastReleaseStart : 0;
            long maximum = doubleDispose ? done - _firstReleaseThrow : 0;
            long first = doubleDispose ? _firstReleaseThrow - _firstReleaseStart : 0;
            if (doubleDispose) Check.Require(0 < minimum && minimum <= maximum && maximum < first, "two-real-callbacks-have-last-only-bracket");
            return [_spec.Name, true, _filterEntries, _acquires, _nextCalls, _releases, observed is null ? null! : observed.GetType().FullName!, _faults,
                true, true, restored, _spec.Mask, minimum, maximum, first, true, _scopeMatched, true, _firstObserved, _entrySuppressionMatched];
        }
        finally { RouteCatalogRemoteExecution.Current = prior; }
    }

    private IRouteCatalogRemoteBoundary? ExpectedBoundary => _suppressed ? null : _boundary;
    private void FirstProbe()
    {
        if (!_firstObserved) { _firstObserved = true; _entrySuppressionMatched = ExecutionContext.IsFlowSuppressed() == _suppressed; }
        Check.Require(_context.HttpContext.RequestAborted == _cancellation.Token, "original-request-token");
        _probe?.Invoke(0);
    }
    private async Task<RouteCatalogDispatchLease> AcquireAsync(CancellationToken token)
    {
        _acquires++; FirstProbe(); Check.Require(token == _cancellation.Token, "authority-sees-original-token");
        await Task.Yield(); _probe?.Invoke(1);
        if (_spec.Name == "guard-acquire-fault") { _faults++; throw _acquireFault; }
        return NewLease();
    }
    private async Task<RouteCatalogDispatchLease?> AcquireCustomAsync()
    {
        FirstProbe(); await Task.Yield(); _probe?.Invoke(1);
        return _spec.Name == "custom-no-lease-nonresult" ? null : NewLease();
    }
    private RouteCatalogDispatchLease NewLease()
    {
        RouteCatalogDispatchLease lease = null!;
        lease = new RouteCatalogDispatchLease(_snapshot, () => ReleaseAsync(lease)); _leases.Add(lease); return lease;
    }
    private void ObserveNext(EndpointFilterInvocationContext context)
    {
        _nextCalls++;
        Check.Require(ReferenceEquals(context, _context) && context.HttpContext.RequestAborted == _cancellation.Token, "exact-context-and-token-reach-next");
        Check.Require(RouteCatalogRemoteCall.Current.Count == _leases.Count && _released.Count == 0, "every-filter-scope-active-at-next");
        foreach (var lease in _leases) Check.Require(RouteCatalogRemoteCall.Current.Contains(lease), "original-lease-in-active-next-scope");
    }
    private async ValueTask<object?> NextAsync(EndpointFilterInvocationContext context)
    {
        ObserveNext(context); await Task.Yield();
        if (_spec.Name == "filter-next-fault") { _faults++; throw _nextFault; }
        if (_spec.Name == "filter-next-cancel") { _faults++; _cancellation.Cancel(); _requestCancellation = new(_cancellation.Token); throw _requestCancellation; }
        if (_spec.Name.StartsWith("custom-", StringComparison.Ordinal) || _spec.Name.StartsWith("filter-double-", StringComparison.Ordinal)) return "synthetic-result";
        return new ProbeResult(ExecuteLeafAsync);
    }
    private async Task ExecuteLeafAsync(HttpContext context)
    {
        _probe?.Invoke(3); Check.Require(ReferenceEquals(context, _context.HttpContext), "actual-result-gets-original-http-context");
        ObserveWrite(); await Task.Yield(); _probe?.Invoke(4);
        if (_spec.Name is "concrete-result-fault" or "concrete-release-override" or "nested-inner-result-fault" or "result-release-cancel-override") { _faults++; throw _nextFault; }
        if (_spec.Name == "concrete-result-cancel") { _faults++; _cancellation.Cancel(); _requestCancellation = new(_cancellation.Token); throw _requestCancellation; }
        await context.Response.Body.WriteAsync("synthetic"u8.ToArray(), context.RequestAborted).ConfigureAwait(false);
    }
    private void ObserveWrite()
    {
        _writes++;
        Check.Require(RouteCatalogRemoteCall.Current.Count == _spec.Entries && _released.Count == 0, "all-owned-scopes-held-during-actual-result-write");
        foreach (var lease in _leases) Check.Require(RouteCatalogRemoteCall.Current.Contains(lease), "exact-acquired-leases-held-through-result");
    }
    private async Task ReleaseAsync(RouteCatalogDispatchLease lease)
    {
        _releases++; _lastReleaseStart = Stopwatch.GetTimestamp();
        bool inlineFirst = _releases == 1 && (_spec.Name == "custom-owned-nonresult" || _spec.Name.StartsWith("filter-double-", StringComparison.Ordinal));
        var expectedLeases = _leases.Where(item => !_released.Contains(item) && (inlineFirst || !ReferenceEquals(item, lease))).ToArray();
        IRouteCatalogRemoteBoundary? expectedBoundary = RouteCatalogRemoteExecution.Current;
        bool boundary = expectedLeases.Length > 0 ? expectedBoundary is not null : ReferenceEquals(expectedBoundary, ExpectedBoundary);
        _scopeMatched &= RouteCatalogRemoteCall.Current.SequenceEqual(expectedLeases) && boundary;
        Check.Require(_scopeMatched, "release-runs-with-original-branch-scope");
        RequireClosed(lease);
        try
        {
            await Task.Yield(); _probe?.Invoke(5);
            _scopeMatched &= RouteCatalogRemoteCall.Current.SequenceEqual(expectedLeases) && ReferenceEquals(RouteCatalogRemoteExecution.Current, expectedBoundary);
            Check.Require(_scopeMatched, "release-await-preserves-original-branch-scope");
            if (_spec.Name.StartsWith("filter-double-", StringComparison.Ordinal) && _releases == 1)
            {
                _firstReleaseStart = _lastReleaseStart; await Task.Delay(200).ConfigureAwait(false); _firstReleaseThrow = Stopwatch.GetTimestamp(); _faults++; throw _firstReleaseFault;
            }
            await Task.Delay(5).ConfigureAwait(false);
            if (_spec.Name is "concrete-release-override" or "filter-double-dispose-second-fault") { _faults++; throw _releaseFault; }
            if (_spec.Name == "result-release-cancel-override") { _faults++; throw _releaseCancellation; }
            Check.Require(_released.Add(lease), "once-only-successful-release");
        }
        finally { _lastReleaseEnd = Stopwatch.GetTimestamp(); }
    }
    private void AssertExpected(Exception? observed)
    {
        Check.Require(observed?.GetType().FullName == _spec.ErrorType, "exact-expected-exception-type-or-success");
        Exception? expected = _spec.Name switch
        {
            "concrete-result-fault" or "filter-next-fault" or "nested-inner-result-fault" => _nextFault,
            "concrete-result-cancel" or "filter-next-cancel" => _requestCancellation,
            "concrete-release-override" or "filter-double-dispose-second-fault" => _releaseFault,
            "filter-double-dispose-retry-success" => _firstReleaseFault,
            "result-release-cancel-override" => _releaseCancellation,
            _ => null,
        };
        Check.Require(ReferenceEquals(observed, expected), "same-original-injected-exception-object");
        if (observed is OperationCanceledException cancellation)
            Check.Require(cancellation.CancellationToken == (_spec.Name == "result-release-cancel-override" ? CancellationToken.None : _cancellation.Token), "original-cancellation-exception-token");
        Check.Require(_filterEntries == _spec.Entries, "exact-real-filter-invocation-count");
        Check.Require((_acquires, _nextCalls, _releases, _faults) == (_spec.Acquires, _spec.NextCalls, _spec.Releases, _spec.Faults), "exact-acquire-next-release-and-fault-counts");
        bool failedRelease = _spec.Name is "concrete-release-override" or "filter-double-dispose-second-fault" or "result-release-cancel-override";
        Check.Require(_leases.Count - _released.Count == (failedRelease ? 1 : 0), "original-lease-failure-lifetime-retained");
        if (_spec.Name == "terminal-refusal-owned-result")
        {
            Check.Require(_refusals == 1 && _writes > 0 && _nextCalls == 0, "actual-terminal-refusal-short-circuits-next-and-executes-result");
            using JsonDocument json = JsonDocument.Parse(_body.ToArray()); Check.Require(json.RootElement.GetProperty("isRejection").GetBoolean(), "actual-refusal-wire-output");
        }
        if (_spec.Name is "guard-acquire-fault" or "guard-validation-fault") Check.Require(_context.HttpContext.Response.StatusCode == 503, "original-guard-refusal-status");
        foreach (var lease in _leases) RequireClosed(lease);
        Check.Require(_body.CanWrite, "response-stream-remains-caller-owned");
        Check.Require(_firstObserved && _entrySuppressionMatched, "real-synchronous-first-dependency-probe");
    }
    private static void RequireClosed(RouteCatalogDispatchLease lease)
    {
        bool refused = false;
        try { lease.RequireOpenForExecution(); }
        catch (RouteCatalogUnavailableException) { refused = true; }
        Check.Require(refused, "actual-sdk-lease-closed-for-execution");
    }
    internal Task DisposeAsync()
    {
        _services.Dispose(); _body.Dispose(); _cancellation.Dispose(); return Task.CompletedTask;
    }
}
