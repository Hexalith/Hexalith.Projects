namespace Story65.DaprInvokerProof;

using System.Reflection;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Server.DomainServices;
using Hexalith.EventStore.ServiceDefaults.Routing;
using NSubstitute;

internal static class SubjectFixture
{
    internal static async Task<InvocationOutcome> InvokeAsync(GatewayVariant variant, DriverCase test, bool canary = false, bool suppressEntry = false)
    {
        using CancellationTokenSource caller = new();
        FixtureState state = new(test, canary, caller);
        var declaration = test.Fault == "missing-caller" ? SharedResources.Delegated : SharedResources.Selected;
        var target = SharedResources.Snapshot.Envelope.Routes.Single(r => r.RouteId == declaration.RouteId);
        string tenant = target.ResolverTenantPattern is null or "*" ? "tenant-a" : target.ResolverTenantPattern;
        QueryEnvelope query = new(tenant, declaration.Domain, "aggregate-a", declaration.MessageType!, "{}"u8.ToArray(), "synthetic-correlation", "actor-a")
        {
            OriginalActorId = "actor-a", AuthenticatedWorkloadId = "eventstore",
            CallerAuthority = test.Fault == "missing-caller" ? null : new("authenticated-caller", ["ProjectUser"], ["read_metadata"]),
        };
        state.Projection = declaration.ProjectionType ?? "synthetic-projection";
        if (test.Fault == "undeclared") { query = query with { QueryType = "synthetic-undeclared" }; state.MarkFault("undeclared"); }
        if (test.Fault == "missing-caller") state.MarkFault("missing-caller");
        if (test.Fault == "binding-poison") { query = query with { AggregateId = null! }; state.MarkFault("binding-poison"); }
        IRouteCatalogRemoteBoundary? callerBoundary = RouteCatalogRemoteExecution.Current;
        IRouteCatalogRemoteBoundary priorBoundary = Substitute.For<IRouteCatalogRemoteBoundary>();
        RouteCatalogRemoteExecution.Current = priorBoundary;
        IRouteCatalogRemoteBoundary? expectedReleaseBoundary = suppressEntry ? null : priorBoundary;
        async Task ReleaseAsync()
        {
            state.Releases++; state.Observe("lease.release.begin");
            state.RemoteBoundaryRestoredAtRelease = ReferenceEquals(RouteCatalogRemoteExecution.Current, expectedReleaseBoundary);
            Check.Require(state.RemoteBoundaryRestoredAtRelease, "Original remote boundary was not restored before release.");
            state.ReleaseEntered.TrySetResult();
            if (test.Fault is "held-release" or "cancel-release") await state.ReleaseContinue.Task.ConfigureAwait(false);
            await state.ProbeAsync().ConfigureAwait(false); state.ThrowIf("release");
            if (test.Fault == "release-cancel") { state.MarkFault("release-cancel"); caller.Cancel(); throw new OperationCanceledException(caller.Token); }
            if (test.Fault == "release-catalog") { state.MarkFault("release-catalog"); throw new RouteCatalogUnavailableException("synthetic fixture fault"); }
            state.Observe("lease.release.end");
        }
        async Task<RouteCatalogDispatchLease> AcquireAsync(CancellationToken token)
        {
            state.Acquisitions++; state.Observe("catalog.acquire", token: token);
            if (canary) Interlocked.CompareExchange(ref FlowCanaryState.FirstProbe, ExecutionContext.IsFlowSuppressed() ? 1 : 0, -1);
            state.AcquireEntered.TrySetResult(); state.ThrowIf("acquire");
            if (test.Fault == "held-acquire") await state.AcquireContinue.Task.ConfigureAwait(false);
            await state.ProbeAsync().ConfigureAwait(false);
            return new(SharedResources.Snapshot, ReleaseAsync);
        }
        IRouteCatalogAuthority authority = Substitute.For<IRouteCatalogAuthority>();
        authority.AcquireAsync(Arg.Any<CancellationToken>()).Returns(call => AcquireAsync(call.Arg<CancellationToken>()));
        IDomainServiceResolver resolver = Substitute.For<IDomainServiceResolver>();
        async Task<DomainServiceRegistration?> ResolveAsync(string resolvedTenant, string domain, string version, CancellationToken token)
        {
            state.Resolutions++; state.Observe("resolver", resolvedTenant + "|" + domain + "|" + version, token);
            await state.ProbeAsync().ConfigureAwait(false); state.ThrowIf("resolver");
            if (test.Fault == "registration-null") { state.MarkFault("registration-null"); return null; }
            return new("synthetic-legacy-app", "synthetic-command-process", resolvedTenant, domain, "v99");
        }
        resolver.ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => ResolveAsync(call.ArgAt<string>(0), call.ArgAt<string>(1), call.ArgAt<string>(2), call.ArgAt<CancellationToken>(3)));
        using var dapr = ProbeDaprFactory.Create(state, query);
        using var client = new HttpClient(new ProbeHttpHandler(state, query));
        IHttpClientFactory clients = Substitute.For<IHttpClientFactory>();
        clients.CreateClient(Arg.Any<string>()).Returns(call =>
        {
            state.Observe("client.factory", call.Arg<string>()); state.ThrowIf("factory");
            Check.Require(call.Arg<string>() == "domain-query-invocation", "Original named client changed.");
            return client;
        });
        object invoker = variant.Create(test.Legacy ? null : authority, dapr, clients, resolver, state);
        QueryResult? result = null; Exception? error = null; bool heldAcquire = false, heldRelease = false;
        try
        {
            Task<QueryResult> pending;
            if (suppressEntry)
            {
                using (ExecutionContext.SuppressFlow())
                {
                    pending = variant.Invoke(invoker, test.Fault == "null" ? null! : query, caller.Token);
                    Check.Require(ExecutionContext.IsFlowSuppressed() && ReferenceEquals(FlowCanaryState.Local.Value, FlowCanaryState.Marker), "Invoker bootstrap changed caller-owned suppression.");
                }
            }
            else pending = variant.Invoke(invoker, test.Fault == "null" ? null! : query, caller.Token);
            if (test.Fault == "held-acquire")
            {
                await state.AcquireEntered.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                caller.Cancel(); heldAcquire = !pending.IsCompleted;
                Check.Require(heldAcquire, "Original Acquire await gained a cancellation wrapper.");
                state.MarkFault("held-acquire");
                state.AcquireContinue.TrySetResult();
            }
            if (test.Fault is "held-release" or "cancel-release")
            {
                await state.ReleaseEntered.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                if (test.Fault == "cancel-release") caller.Cancel(); heldRelease = !pending.IsCompleted;
                Check.Require(heldRelease, "Invoker did not await original lease release.");
                state.MarkFault(test.Fault);
                await Task.Delay(100).ConfigureAwait(false); Check.Require(!pending.IsCompleted, "Held release ended early.");
                state.ReleaseContinue.TrySetResult();
            }
            result = await pending.ConfigureAwait(false);
        }
        catch (Exception exception) { error = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception; }
        state.Observe("invoker.exit");
        Check.Require(ReferenceEquals(RouteCatalogRemoteExecution.Current, priorBoundary), "Final caller remote boundary changed.");
        RouteCatalogRemoteExecution.Current = callerBoundary;
        return new(result, error, state, caller.IsCancellationRequested,
            error is OperationCanceledException canceled && canceled.CancellationToken == caller.Token, heldAcquire, heldRelease);
    }
}
