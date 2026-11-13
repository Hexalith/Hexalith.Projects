namespace Story65.InvocationFixture;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.Extensions.Options;

/// <summary>Typed per-ALC fixture; only framework objects and DTO arrays cross its reflection boundary.</summary>
public static class InvocationFixture
{
    public static object[] Facts() => [typeof(IRouteCatalogAuthority), typeof(RouteCatalogHostOptions), typeof(RouteCatalogDispatchLease),
        typeof(CommittedRouteCatalogSnapshot), typeof(RouteCatalogInvocationHandler), typeof(RouteCatalogEnvelope), typeof(HttpResponseMessage)];

    public static async Task<object[]> RunAsync(object[] specification, Action<bool>? firstProbe, Func<Task>? fakeAwait, bool suppressEntry)
    {
        string name = (string)specification[0], catalog = (string)specification[1];
        CaseState state = new(name, firstProbe, fakeAwait);
        IRouteCatalogRemoteBoundary? outerBoundary = RouteCatalogRemoteExecution.Current;
        var priorBoundary = new FixtureBoundary();
        RouteCatalogRemoteExecution.Current = priorBoundary;
        state.ExpectedReleaseBoundary = suppressEntry ? null : priorBoundary;
        try
        {
            RouteCatalogHostIdentity identity = new("eventstore", "eventstore", new('a', 64), new('b', 64), new('c', 64));
            var activation = FixtureCatalog.Create(catalog, identity);
            var store = new FixtureStore(state, new(1, activation, null, null, [], 0, []));
            var options = new RouteCatalogHostOptions
            {
                Identity = identity, Profile = "Development", DevelopmentFixture = true, RequireTechnicalHttpBinding = true,
                ScopedDecodeReuse = true, RetainValidatedCatalogBytes = true, ReuseHeldMetadataDispatch = true,
                ControlPlaneAppIds = ["eventstore"], InvocationTransportFactory = _ => new ProbeTransport(state, ordinary: false),
            };
            var configured = Options.Create(options);
            var environment = new FixtureEnvironment();
            var observer = new FixtureObserver(state, identity);
            using var authority = new RouteCatalogAuthority(store, new FixtureTrust(state, new(configured, environment)), configured, observer, environment: environment);
            IRouteCatalogAuthority outbound = name == "cancel-before-enrollment" ? new CancelAfterAcquireAuthority(state, authority) : authority;
            var native = name is "borrowed-success" or "borrowed-observer-refusal" ? new RouteCatalogInvocationScope(authority) : null;
            HttpMessageHandler ordinary = name == "ordinary-redirect-reentry" ? new SocketsHttpHandler
            {
                AllowAutoRedirect = true, UseProxy = false,
                ConnectCallback = (_, _) => { state.Connections++; return ValueTask.FromResult<Stream>(new RedirectMemoryStream()); },
            } : new ProbeTransport(state, ordinary: true);
            using var handler = new RouteCatalogInvocationHandler(outbound, ordinary, configured, false, native);
            using var boundary = new ReturnBoundaryHandler(state) { InnerHandler = handler };
            using var invoker = new HttpMessageInvoker(boundary, disposeHandler: false);
            using var http = new HttpClient(boundary, disposeHandler: false);
            state.Response = new(HttpStatusCode.OK) { Content = new ProbeContent(state, true, "{\"synthetic\":true}"u8.ToArray()) };
            bool metadata = native is not null || name is "held-success" or "held-scope-closed-refusal";
            string path = name switch
            {
                "ordinary-direct-nochildren" or "ordinary-redirect-reentry" => "ordinary",
                "control-nochildren" => "v1.0/invoke/eventstore/method/" + RouteCatalogEndpointExtensions.ReadbackMethod,
                "undeclared-target-refusal" or "release-fault-overrides-validation" => "v1.0/invoke/foreign/method/query",
                _ => "v1.0/invoke/eventstore/method/" + (metadata ? "api/v1/streams/read/workload" : "query"),
            };
            using var request = new HttpRequestMessage(name == "control-nochildren" ? HttpMethod.Get : HttpMethod.Post, "http://fixture/" + path);
            request.Content = metadata
                ? new ProbeRequestContent(state, JsonContent.Create(new StreamReadRequest("tenant-a", "counter", "counter-a", 0, 0, PageSize: 1) { RequireNativeProof = true }))
                : new ProbeContent(state, false, "{}"u8.ToArray());
            request.Headers.TryAddWithoutValidation("X-Synthetic-Unrelated", "synthetic-value");
            if (name == "unsupported-basic-refusal") request.Headers.Authorization = new("Basic", Convert.ToBase64String("dapr-app-id:eventstore"u8.ToArray()));
            state.NewEntries = name == "ordinary-redirect-reentry" ? 2 : 1;
            if (name == "pre-cancel-before-acquire") state.Caller.Cancel();
            HttpResponseMessage? response = null; Exception? error = null; int insideFences = -1, readsBefore = 0, readsAfter = 0;
            async Task SendAsync()
            {
                Task<HttpResponseMessage> pending;
                if (suppressEntry)
                {
                    using (ExecutionContext.SuppressFlow())
                    {
                        pending = name.StartsWith("outer-buffer-", StringComparison.Ordinal) ? http.SendAsync(request, state.Caller.Token) : invoker.SendAsync(request, state.Caller.Token);
                        Verify.That(ExecutionContext.IsFlowSuppressed(), "Bootstrap changed caller suppression.");
                    }
                }
                else pending = name.StartsWith("outer-buffer-", StringComparison.Ordinal) ? http.SendAsync(request, state.Caller.Token) : invoker.SendAsync(request, state.Caller.Token);
                if (name is "held-transport" or "held-release")
                {
                    await (name == "held-transport" ? state.TransportEntered.Task : state.ReleaseEntered.Task).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                    state.Caller.Cancel(); state.HeldWait = !pending.IsCompleted;
                    Verify.That(state.HeldWait, "Original await gained cancellation wrapping.");
                    await Task.Delay(100).ConfigureAwait(false); Verify.That(!pending.IsCompleted, "Original held work ended early.");
                    (name == "held-transport" ? state.TransportContinue : state.ReleaseContinue).TrySetResult();
                }
                response = await pending.ConfigureAwait(false);
            }
            try
            {
                if (native is not null)
                {
                    await native.ExecuteNativeSourceReadAsync("eventstore", async () =>
                    {
                        readsBefore = state.Reads;
                        var originalLease = native.LeaseFor(authority, "eventstore", "api/v1/streams/read/workload", HttpMethod.Post);
                        state.ScopePathVerified = originalLease?.Activation is not null && store.Current.Leases.Count == 1;
                        Verify.That(state.ScopePathVerified, "Native fixture did not acquire its genuine owner lease.");
                        if (name == "borrowed-observer-refusal") state.DenyObserver = true;
                        try { await SendAsync().ConfigureAwait(false); }
                        finally
                        {
                            readsAfter = state.Reads; insideFences = store.Current.Leases.Count;
                            Verify.That(insideFences == 1 && readsAfter == readsBefore, "Borrowed path acquired or released a second fence.");
                        }
                        return true;
                    }).ConfigureAwait(false);
                }
                else if (name is "held-success" or "held-scope-closed-refusal")
                {
                    await using var parent = await authority.AcquireAsync().ConfigureAwait(false);
                    using var receiver = RouteCatalogRemoteCall.EnterReceiver(parent, state.Caller.Token);
                    readsBefore = state.Reads;
                    state.ScopePathVerified = parent.HasMetadataIssuer && parent.Activation is not null
                        && RouteCatalogRemoteCall.CaptureMetadataScopes(authority) is { Length: 1 } && store.Current.Leases.Count == 1;
                    Verify.That(state.ScopePathVerified, "Held fixture lacks its actual issuer and receiver scope.");
                    try
                    {
                    if (name == "held-scope-closed-refusal")
                    {
                        TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
                        Task escaped = Task.Run(async () => { await resume.Task.ConfigureAwait(false); await SendAsync().ConfigureAwait(false); });
                        receiver.Dispose(); resume.SetResult(); await escaped.ConfigureAwait(false);
                    }
                    else await SendAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        readsAfter = state.Reads; insideFences = store.Current.Leases.Count;
                        Verify.That(insideFences == 1 && readsAfter == readsBefore, "Held path acquired or released another fence.");
                    }
                }
                else await SendAsync().ConfigureAwait(false);
            }
            catch (Exception exception) { error = exception; }
            Verify.That(ReferenceEquals(RouteCatalogRemoteExecution.Current, priorBoundary), "Caller boundary changed after actual invocation or receiver disposal.");
            state.OriginalResponse = ReferenceEquals(response, state.Response);
            int requestBefore = state.RequestDisposals, responseBefore = state.ResponseDisposals, fences = store.Current.Leases.Count;
            string finalHeaders = JsonSerializer.Serialize(request.Headers.OrderBy(h => h.Key, StringComparer.Ordinal).Select(h => new { h.Key, Values = h.Value.ToArray() }));
            FixtureAssertions.Check(state, error, response, fences, requestBefore, responseBefore, insideFences, readsBefore, readsAfter);
            state.Observe("caller.cleanup"); state.Response.Dispose(); request.Dispose();
            Verify.That(state.RequestDisposals == 1 && state.ResponseDisposals == 1, "Caller cleanup failed to dispose each owned resource exactly once.");
            var errorFacts = new object[] { error?.GetType().FullName ?? "", error?.Message ?? "",
                error is RouteCatalogUnavailableException refusal ? refusal.Category.ToString() : "",
                ReferenceEquals(error, state.Injected) && error is not null, error?.InnerException is not null && ReferenceEquals(error.InnerException, state.Injected) };
            var result = new object[] { errorFacts, response is null ? -1 : (int)response.StatusCode, finalHeaders, state.OriginalResponse,
                state.Caller.IsCancellationRequested, error is OperationCanceledException canceled && canceled.CancellationToken == state.Caller.Token,
                state.HeldWait, state.NormalizedTrace(), fences, state.Releases, state.Transports, state.Ordinary, state.ScopePathVerified,
                state.NewEntries, state.Faults.OrderBy(p => p.Key).Select(p => new object[] { p.Key, p.Value }).ToArray(), requestBefore,
                responseBefore, state.HandlerReturned, state.Reads, state.Writes, state.ObserverCalls, state.TrustCalls };
            return result;
        }
        finally
        {
            RouteCatalogRemoteExecution.Current = outerBoundary;
            state.Caller.Dispose();
            Verify.That(ReferenceEquals(RouteCatalogRemoteExecution.Current, outerBoundary), "Outer caller boundary was not restored.");
        }
    }
}
