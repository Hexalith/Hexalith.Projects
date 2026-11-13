namespace Story65.GatewayControllerProof;

using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Server.Pipeline.Queries;
using Hexalith.EventStore.Server.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;

internal static class SubjectFixture
{
    internal static async Task<InvocationOutcome> InvokeAsync(GatewayVariant variant, DriverCase test, bool canary = false, bool suppressEntry = false)
    {
        FixtureState state = new(test, canary);
        var http = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "workload:projects"),
            new Claim(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "projects"),
            new Claim(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.GatewayOwnerAuthorityQuery),
            new Claim(RouteCatalogSurfacePolicy.ClaimType, test.Fault == "surface" ? "ui" : "service"),
        };
        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "synthetic-workload"));
        http.Request.Method = "POST"; http.Request.Path = "/api/v1/queries/owner-authority/workload";
        var items = new ProbeItems(variant.AdmissionKey, state); http.Items = items;
        object priorMarker = new(), priorAuthorization = new(), sentinel = new();
        items.Inner["sentinel"] = sentinel; items.Inner[GatewayVariant.PrevalidatedKey] = priorAuthorization;
        string correlationKey = (string)variant.Assembly.GetType("Hexalith.EventStore.Middleware.CorrelationIdMiddleware", true)!
            .GetField("HttpContextKey", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
        items.Inner[correlationKey] = "synthetic-fixed-correlation";
        if (test.PriorMarker) items.Inner[variant.AdmissionKey] = priorMarker;
        var requestFeature = http.Features.Get<IHttpRequestFeature>()!;
        requestFeature.Headers = new ProbeHeaders(requestFeature.Headers, state, false);
        var responseFeature = http.Features.Get<IHttpResponseFeature>()!;
        responseFeature.Headers = new ProbeHeaders(responseFeature.Headers, state, true);
        var configuration = new ConfigurationBuilder().Add(new ProbeConfigurationProvider(state, new()
        {
            ["Authentication:BoundStreamRead:PublicKeys:projects"] = SharedResources.PublicKey,
            ["Authentication:BoundOwnerQuery:AllowedQueries:projects"] = SharedResources.QueryType,
        })).Build();
        var original = new QueryEnvelope("tenant-a", "projects", "project-a", "Open", "{}"u8.ToArray(), "synthetic-original-correlation", "actor-a")
        {
            OriginalActorId = "actor-a", AuthenticatedWorkloadId = "operator-web",
            CallerAuthority = new("authenticated-caller", ["ProjectUser"], ["read_metadata"]),
        };
        var request = new BoundOwnerQueryRequest(new("tenant-a", SharedResources.Domain, "aggregate-a", SharedResources.QueryType,
            Payload: JsonSerializer.SerializeToElement(new { selector = "synthetic" })), "actor-a");
        if (test.Origin) request = request with { OriginatingQuery = original,
            OriginatingAssertion = test.Fault == "origin" ? "synthetic-invalid-assertion" : SharedResources.OriginAssertion(original) };
        http.Request.Headers[BoundOwnerQueryProof.HeaderName] = test.Fault == "signature" ? "synthetic-invalid-proof"
            : BoundOwnerQueryProof.Issue(request, "projects", SharedResources.PrivateKey, DateTimeOffset.UtcNow);
        using CancellationTokenSource caller = new();
        IRouteCatalogRemoteBoundary? callerBoundary = RouteCatalogRemoteExecution.Current;
        IRouteCatalogRemoteBoundary priorBoundary = Substitute.For<IRouteCatalogRemoteBoundary>();
        RouteCatalogRemoteExecution.Current = priorBoundary;
        bool MarkerRestored() => test.PriorMarker ? items.Inner.TryGetValue(variant.AdmissionKey, out var value) && ReferenceEquals(value, priorMarker)
            : !items.Inner.ContainsKey(variant.AdmissionKey);
        async Task ReleaseAsync()
        {
            Interlocked.Increment(ref state.Releases); state.Observe("lease.release.begin");
            state.MarkerRestoredAtRelease = MarkerRestored();
            state.RemoteBoundaryRestoredAtRelease = ReferenceEquals(RouteCatalogRemoteExecution.Current, priorBoundary);
            Check.Require(state.MarkerRestoredAtRelease && state.RemoteBoundaryRestoredAtRelease, "Original admission/remote scope was not restored before release.");
            state.ReleaseEntered.TrySetResult();
            if (test.Fault is "held-release" or "cancel-release") await state.ReleaseContinue.Task.ConfigureAwait(false);
            await state.ProbeAsync().ConfigureAwait(false); state.Throw("release");
            state.Observe("lease.release.end");
        }
        async Task<RouteCatalogDispatchLease> AcquireAsync(CancellationToken token)
        {
            Interlocked.Increment(ref state.Acquisitions); state.Observe("catalog.acquire", token: token);
            if (canary) Interlocked.CompareExchange(ref FlowCanaryState.FirstProbe, ExecutionContext.IsFlowSuppressed() ? 1 : 0, -1);
            state.AcquireEntered.TrySetResult(); state.Throw("acquire");
            if (test.Fault == "held-acquire") await state.AcquireContinue.Task.ConfigureAwait(false);
            await state.ProbeAsync().ConfigureAwait(false);
            return new(SharedResources.Snapshot, ReleaseAsync);
        }
        IRouteCatalogAuthority authority = Substitute.For<IRouteCatalogAuthority>();
        authority.AcquireAsync(Arg.Any<CancellationToken>()).Returns(call => AcquireAsync(call.Arg<CancellationToken>()));
        IMediator mediator = Substitute.For<IMediator>(); SubmitQuery? submitted = null;
        async Task<SubmitQueryResult> MediateAsync(SubmitQuery query, CancellationToken token)
        {
            submitted = query; Interlocked.Increment(ref state.Mediations);
            state.Observe("mediator.begin", JsonSerializer.Serialize(query), token);
            state.AdmissionVerified = variant.HasAdmission(http, query, SharedResources.Snapshot);
            Check.Require(state.AdmissionVerified, "Own variant MethodInfo/admission tuple did not verify.");
            await state.ProbeAsync().ConfigureAwait(false); state.Throw("mediator");
            if (test.Fault == "mediator-cancel") { Interlocked.Increment(ref state.Faults); caller.Cancel(); }
            token.ThrowIfCancellationRequested(); state.Observe("mediator.end", token: token);
            return new("synthetic-fixed-correlation", JsonSerializer.SerializeToElement(new { allowed = true }), null, null);
        }
        mediator.Send(Arg.Any<SubmitQuery>(), Arg.Any<CancellationToken>()).Returns(call => MediateAsync(call.Arg<SubmitQuery>(), call.Arg<CancellationToken>()));
        var controller = variant.Create(authority, mediator, Substitute.For<IETagService>(), configuration, SharedResources.Validator, SharedResources.WorkloadOptions, http);
        IActionResult? result = null; Exception? error = null; bool heldAcquire = false, heldRelease = false;
        try
        {
            Task<Microsoft.AspNetCore.Mvc.IActionResult> pending;
            BoundOwnerQueryRequest input = test.Fault == "guard" ? null! : request;
            if (suppressEntry)
            {
                using (ExecutionContext.SuppressFlow())
                {
                    pending = variant.Invoke(controller, input, caller.Token);
                    Check.Require(ExecutionContext.IsFlowSuppressed() && ReferenceEquals(FlowCanaryState.Local.Value, FlowCanaryState.Marker), "Action bootstrap changed caller-owned suppression.");
                }
            }
            else pending = variant.Invoke(controller, input, caller.Token);
            if (test.Fault == "held-acquire")
            {
                await state.AcquireEntered.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                caller.Cancel(); heldAcquire = !pending.IsCompleted;
                Check.Require(heldAcquire, "Original Acquire await acquired a new cancellation wrapper.");
                state.AcquireContinue.TrySetResult();
            }
            if (test.Fault is "held-release" or "cancel-release")
            {
                await state.ReleaseEntered.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                if (test.Fault == "cancel-release") caller.Cancel();
                heldRelease = !pending.IsCompleted;
                Check.Require(heldRelease, "Action did not await original asynchronous release.");
                await Task.Delay(100).ConfigureAwait(false);
                Check.Require(!pending.IsCompleted, "Action completed while release remained held.");
                state.ReleaseContinue.TrySetResult();
            }
            result = await pending.ConfigureAwait(false);
        }
        catch (Exception exception) { error = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception; }
        state.Observe("action.exit");
        Check.Require(MarkerRestored(), "Bound-owner admission marker effect changed.");
        Check.Require(ReferenceEquals(RouteCatalogRemoteExecution.Current, priorBoundary), "Original caller remote boundary changed.");
        Check.Require(ReferenceEquals(items.Inner["sentinel"], sentinel), "Unrelated original item changed.");
        bool prepared = test.Fault is not ("guard" or "signature" or "origin");
        if (prepared) Check.Require(!ReferenceEquals(items.Inner[GatewayVariant.PrevalidatedKey], priorAuthorization), "Original persistent prevalidated item was incorrectly restored.");
        else Check.Require(ReferenceEquals(items.Inner[GatewayVariant.PrevalidatedKey], priorAuthorization), "Guard branch changed original prevalidated item.");
        var finalItems = items.Inner.Select(pair => new KeyValuePair<string, string>(ReferenceEquals(pair.Key, variant.AdmissionKey) ? "admission" : pair.Key.ToString()!,
            ReferenceEquals(pair.Value, priorMarker) ? "prior-marker" : ReferenceEquals(pair.Value, priorAuthorization) ? "prior-authorization"
            : ReferenceEquals(pair.Value, sentinel) ? "sentinel" : JsonSerializer.Serialize(pair.Value, pair.Value?.GetType() ?? typeof(object))))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
        var outcome = new InvocationOutcome(result, error, state, caller.IsCancellationRequested,
            error is OperationCanceledException canceled && canceled.CancellationToken == caller.Token,
            JsonSerializer.Serialize(finalItems), JsonSerializer.Serialize(http.Response.Headers.OrderBy(pair => pair.Key, StringComparer.Ordinal)),
            submitted is null ? null : JsonSerializer.Serialize(submitted), heldAcquire, heldRelease);
        RouteCatalogRemoteExecution.Current = callerBoundary;
        return outcome;
    }
}
