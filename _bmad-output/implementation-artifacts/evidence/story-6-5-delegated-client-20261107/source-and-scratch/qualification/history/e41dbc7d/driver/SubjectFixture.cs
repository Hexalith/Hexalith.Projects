namespace Story65.DelegatedClientProof;

using System.Diagnostics;
using System.Security.Claims;
using Dapr.Client;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

internal static class SubjectFixture
{
    internal static async Task<InvocationOutcome> InvokeAsync(DriverCase test, bool instrumented,
        bool canary = false, bool suppressFirst = false, CohortBarrier? cohort = null)
    {
        FixtureState state = new(test, canary, cohort);
        QueryEnvelope query = new("synthetic-tenant", "projects", "synthetic-project", "synthetic-owner-proof", [], "synthetic-correlation", "synthetic-actor")
            { OriginalActorId = "synthetic-actor", CallerAuthority = new("authenticated-caller", ["reader"], ["read_metadata"]) };
        DefaultHttpContext context = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"),
                new Claim(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
                .. QueryCallerAuthorityBinding.Bind(query).Select(pair => new Claim(pair.Key, pair.Value))], "workload")),
        };
        context.Request.Headers[EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName] = test.Fault == "duplicate-assertion"
            ? new Microsoft.Extensions.Primitives.StringValues(["synthetic-a", "synthetic-b"]) : "synthetic-original-assertion";
        var feature = context.Features.Get<IHttpRequestFeature>()!;
        feature.Headers = new ProbeHeaders(feature.Headers, state);
        var accessor = new ProbeAccessor(state, context);
        var data = new Dictionary<string, string?>
        {
            ["Authentication:WorkloadIssuer:Workload"] = test.Workload,
            ["Authentication:BoundStreamRead:PrivateKey"] = test.Fault == "missing-key" ? null
                : test.Fault == "whitespace-key" ? " " : test.Fault == "proof-key-invalid" ? "synthetic-invalid-base64" : SharedSyntheticKey.Private,
            [DelegatedOwnerQueryClient.OperationTimeoutConfigurationKey] = test.Budget,
        };
        IConfiguration configuration = new ConfigurationBuilder().Add(new ProbeConfigurationSource(new(state, data))).Build();
        using DaprClient dapr = ProbeDaprFactory.Create(state);
        using var http = new HttpClient(new ProbeHttpHandler(state, SharedSyntheticKey.Public)) { Timeout = Timeout.InfiniteTimeSpan };
        var factory = new ProbeHttpFactory(http, state); var issuer = new ProbeIssuer(state); var clock = new ProbeTimeProvider(state);
        var original = new DelegatedOwnerQueryClient(dapr, factory, issuer, accessor, configuration, clock);
        var changed = new InstrumentedDelegatedOwnerQueryClient(dapr, factory, issuer, accessor, configuration, clock);
        using CancellationTokenSource caller = new(TimeSpan.FromSeconds(2));
        SubmitQueryRequest request = new(test.Fault == "foreign-tenant" ? "other-synthetic-tenant" : query.TenantId,
            "folders", "synthetic-folder", "synthetic-owner-read");
        Task<QueryResult> ClientCall(CancellationToken token) => instrumented
            ? changed.InvokeAsync(test.Fault == "null-query" ? null! : request, token)
            : original.InvokeAsync(test.Fault == "null-query" ? null! : request, token);
        await using ServiceProvider services = new ServiceCollection().AddSingleton<IHttpContextAccessor>(accessor)
            .AddSingleton<IDomainQueryHandler>(new ProbeDomainHandler(ClientCall)).BuildServiceProvider();
        QueryResult? result = null; Exception? error = null; bool unboundedHeld = false;
        Stopwatch watch = Stopwatch.StartNew(); Task<QueryResult> pending;
        try
        {
            if (suppressFirst)
            {
                using (ExecutionContext.SuppressFlow())
                {
                    pending = DomainQueryDispatcher.ExecuteAsync(services, query, caller.Token);
                    Check.Require(ExecutionContext.IsFlowSuppressed(), "Client bootstrap undid caller suppression.");
                    Check.Require(ReferenceEquals(FlowCanaryState.Local.Value, FlowCanaryState.Marker), "Caller marker changed before await.");
                }
            }
            else pending = test.Fault == "missing-caller" ? ClientCall(caller.Token) : DomainQueryDispatcher.ExecuteAsync(services, query, caller.Token);
            if (test.Fault == "issuer-uncooperative")
            {
                await state.IssuerEntered.Task.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                using CancellationTokenSource observed = new(TimeSpan.FromSeconds(3));
                while (!caller.IsCancellationRequested) await Task.Delay(1, observed.Token).ConfigureAwait(false);
                if (test.Budget is null)
                {
                    Check.Require(!pending.IsCompleted, "Unbounded ValueTask acquired a new cancellation wrapper.");
                    unboundedHeld = true; state.ReleaseIssuer.TrySetResult();
                }
            }
            result = await pending.ConfigureAwait(false);
        }
        catch (Exception exception) { error = exception; }
        state.Observe("invoke.exit");
        watch.Stop();
        bool child = state.Sources.Any(source => !source.Completion.IsCompleted || Volatile.Read(ref source.Results) == 0);
        if (test.Fault == "issuer-uncooperative")
        {
            var token = state.Trace.Single(item => item.Operation == "issuer.issue").Token;
            Check.Require(token.IsCancellationRequested, "Held acquisition did not see cancellation.");
            if (test.Budget is not null) Check.Require(child, "Bounded WaitAsync incorrectly quiesced its uncooperative child.");
            Interlocked.Increment(ref state.Triggered); state.ReleaseIssuer.TrySetResult();
        }
        foreach (var source in state.Sources)
        {
            await source.Completion.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            using CancellationTokenSource consumed = new(TimeSpan.FromSeconds(1));
            while (Volatile.Read(ref source.Results) == 0) await Task.Delay(1, consumed.Token).ConfigureAwait(false);
            Check.Require(source.Results == 1, "ValueTask source was not consumed exactly once.");
            Check.Require(source.Registrations == (test.ValueSource == "source-sync" ? 0 : 1), "ValueTask await branch changed.");
        }
        Check.Require(VerifiedQueryCaller.Get(context) is null, "Dispatcher did not clear verified caller.");
        var outcome = new InvocationOutcome(result, error, state, caller.IsCancellationRequested,
            error is OperationCanceledException canceled && canceled.CancellationToken == caller.Token, child, unboundedHeld, watch.Elapsed);
        Assertions.ForCase(test, outcome);
        return outcome;
    }
}
