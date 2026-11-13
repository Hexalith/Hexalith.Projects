namespace Story65.OutboundAssertionProof;

using System.Net;
using System.Reflection;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using NSubstitute;

internal static class SubjectFixture
{
    internal static async Task<InvocationOutcome> InvokeAsync(ServerVariant variant, DriverCase test, bool canary = false, bool suppressEntry = false)
    {
        using CancellationTokenSource caller = new();
        FixtureState state = new(test, canary, caller);
        state.Audience = test.Fault == "issuer-null" ? new string('x', 4096) : "sample";
        state.Operation = test.Route == "query" ? EventStoreWorkloadOperations.DomainServiceQuery : EventStoreWorkloadOperations.DomainServiceProcess;
        Uri uri = new("http://synthetic-dapr.invalid:3500/v1.0/invoke/" + Uri.EscapeDataString(state.Audience) + "/method/" + test.Route);
        state.Request = new(HttpMethod.Post, uri) { Content = new ProbeContent(state, response: false) };
        state.Response = new(HttpStatusCode.OK) { Content = new ProbeContent(state, response: true) };
        state.ResponseCreatedByFixture = true;
        state.Request.Headers.TryAddWithoutValidation("Authorization", "Bearer synthetic-forwarded-human");
        state.Request.Headers.TryAddWithoutValidation(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, ["synthetic-forwarded-assertion-a", "synthetic-forwarded-assertion-b"]);
        state.Request.Headers.TryAddWithoutValidation("X-Synthetic-Unrelated", "synthetic-unrelated");
        IReadOnlyDictionary<string, string> queryBindings = new Dictionary<string, string> { ["synthetic-query"] = "synthetic-query-binding" };
        IReadOnlyDictionary<string, string> commandBindings = new Dictionary<string, string> { ["synthetic-command"] = "synthetic-command-binding" };
        // Both key types can be present; the original operation determines the exact dictionary reference.
        state.Request.Options.Set(QueryCallerAuthorityBinding.RequestBindings, queryBindings);
        if (test.Bindings) state.Request.Options.Set(CommandSourceAuthorityBinding.RequestBindings, commandBindings);
        state.ExpectedBindings = test.Route == "query" ? queryBindings : test.Route == "process" && test.Bindings ? commandBindings : null;
        string initialHeaders = HeaderSnapshot.Of(state.Request);
        HttpContent originalContent = state.Request.Content!;
        using var handler = variant.Create(state);
        ((DelegatingHandler)handler).InnerHandler = new ProbePrimaryHandler(state);
        using var boundary = new ReturnBoundaryHandler(state) { InnerHandler = handler };
        using var invoker = new HttpMessageInvoker(boundary, disposeHandler: false);
        using var client = new HttpClient(boundary, disposeHandler: false);
        IRouteCatalogRemoteBoundary? originalCallerBoundary = RouteCatalogRemoteExecution.Current;
        IRouteCatalogRemoteBoundary priorBoundary = Substitute.For<IRouteCatalogRemoteBoundary>();
        RouteCatalogRemoteExecution.Current = priorBoundary;
        HttpResponseMessage? returned = null; Exception? error = null; bool held = false;
        try
        {
            Task<HttpResponseMessage> Start() => test.Fault == "null" ? variant.InvokeNull(handler, caller.Token)
                : test.OuterBuffer ? client.SendAsync(state.Request, caller.Token) : invoker.SendAsync(state.Request, caller.Token);
            Task<HttpResponseMessage> pending;
            if (suppressEntry)
            {
                using (ExecutionContext.SuppressFlow())
                {
                    pending = Start();
                    Check.Require(ExecutionContext.IsFlowSuppressed() && ReferenceEquals(FlowCanaryState.Local.Value, FlowCanaryState.Marker), "Handler bootstrap changed prior caller flow state.");
                }
            }
            else pending = Start();
            if (test.Fault is "held-issuer" or "held-base")
            {
                await (test.Fault == "held-issuer" ? state.IssuerEntered.Task : state.BaseEntered.Task).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                caller.Cancel(); held = !pending.IsCompleted;
                Check.Require(held, "Original uncooperative await acquired a new cancellation wrapper.");
                await Task.Delay(100).ConfigureAwait(false); Check.Require(!pending.IsCompleted, "Original held work ended before its actual completion.");
                state.Mark(test.Fault);
                (test.Fault == "held-issuer" ? state.ReleaseIssuer : state.ReleaseBase).TrySetResult();
            }
            returned = await pending.ConfigureAwait(false);
        }
        catch (Exception exception) { error = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception; }
        foreach (AssertionValueSource source in state.Sources)
        {
            await source.Completion.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            Check.Require(source.Results == 1 && source.Registrations == (test.SourceSync && !canary ? 0 : 1), "Original ValueTask was not consumed once in its original branch.");
        }
        Check.Require(ReferenceEquals(RouteCatalogRemoteExecution.Current, priorBoundary), "Final caller boundary changed.");
        RouteCatalogRemoteExecution.Current = originalCallerBoundary;
        state.Observe("handler.call.exit");
        int beforeRequest = state.RequestDisposed, beforeResponse = state.ResponseDisposed;
        bool contentPreserved = ReferenceEquals(state.Request.Content, originalContent) && ReferenceEquals(state.Request.RequestUri, uri);
        bool optionsPreserved = state.Request.Options.TryGetValue(QueryCallerAuthorityBinding.RequestBindings, out var queryAfter) && ReferenceEquals(queryAfter, queryBindings)
            && (test.Bindings ? state.Request.Options.TryGetValue(CommandSourceAuthorityBinding.RequestBindings, out var commandAfter) && ReferenceEquals(commandAfter, commandBindings)
                : !state.Request.Options.TryGetValue(CommandSourceAuthorityBinding.RequestBindings, out _));
        string headers = HeaderSnapshot.Of(state.Request);
        bool responsePreserved = returned is not null && ReferenceEquals(returned, state.Response);
        state.Observe("caller.cleanup.begin");
        state.Response.Dispose(); state.Request.Dispose();
        Check.Require(state.RequestDisposed == 1 && state.ResponseDisposed == 1, "Caller could not dispose its original messages exactly once.");
        return new(error, state, headers, initialHeaders, responsePreserved, contentPreserved, optionsPreserved,
            caller.IsCancellationRequested, error is OperationCanceledException canceled && canceled.CancellationToken == caller.Token,
            held, beforeRequest, beforeResponse);
    }
}
