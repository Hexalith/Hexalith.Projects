namespace Story65.GatewayControllerProof;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

internal static class Assertions
{
    internal static void ForCase(DriverCase test, InvocationOutcome outcome)
    {
        var state = outcome.State;
        bool denied = test.Fault is "guard" or "signature" or "origin" or "surface";
        bool injected = test.Fault is "acquire" or "mediator" or "response" or "release";
        bool canceled = test.Fault is "mediator-cancel" or "held-acquire";
        if (injected) Check.Require(outcome.Error is FixtureFailure && outcome.Result is null && state.Faults == 1, "Expected shared fixture exception did not fire exactly once.");
        else if (canceled) Check.Require(outcome.Error is OperationCanceledException && outcome.Result is null && outcome.CallerCanceled, "Original cancellation result changed.");
        else Check.Require(outcome.Error is null && outcome.Result is not null, "Expected-result action unexpectedly threw.");
        if (denied) Check.Require(outcome.Result is ForbidResult && state.Mediations == 0, "Denied action dispatched a mediator result.");
        if (!denied && !injected && !canceled) Check.Require(outcome.Result is OkObjectResult, "Positive action did not construct shared OkObjectResult.");
        int acquired = test.Fault is "guard" or "signature" or "origin" ? 0 : 1;
        int released = acquired == 1 && test.Fault != "acquire" ? 1 : 0;
        Check.Require(state.Acquisitions == acquired && state.Releases == released, "Original acquire/release count changed.");
        if (released != 0) Check.Require(state.MarkerRestoredAtRelease && state.RemoteBoundaryRestoredAtRelease, "Original restoration did not precede release.");
        if (test.Fault == "held-acquire") Check.Require(outcome.WaitHeldForAcquire, "Uncooperative Acquire was not held after cancellation.");
        if (test.Fault is "held-release" or "cancel-release") Check.Require(outcome.WaitHeldForRelease, "Asynchronous release was not awaited.");
        if (test.Fault == "cancel-release") Check.Require(outcome.CallerCanceled && outcome.Error is null && outcome.Result is OkObjectResult, "A new post-release cancellation check was invented.");
        if (state.Mediations != 0) Check.Require(state.AdmissionVerified, "Variant MethodInfo admission was not verified in flight.");
    }
    internal static void Compare(InvocationOutcome ordinary, InvocationOutcome candidate)
    {
        // Gateway-owned exception Types are distinct across ALCs; compare expected namespace/name, not default-context Type identity.
        Check.Require(ordinary.Error?.GetType().FullName == candidate.Error?.GetType().FullName, "Exception kind changed across variants.");
        Check.Require(ordinary.Result?.GetType() == candidate.Result?.GetType(), "Shared action-result type changed.");
        if (ordinary.Result is OkObjectResult first && candidate.Result is OkObjectResult second)
            Check.Require(JsonSerializer.Serialize(first.Value, first.Value?.GetType() ?? typeof(object))
                == JsonSerializer.Serialize(second.Value, second.Value?.GetType() ?? typeof(object)), "Shared action-result payload/metadata changed.");
        Check.Require(ordinary.ItemsJson == candidate.ItemsJson && ordinary.HeadersJson == candidate.HeadersJson
            && ordinary.SubmittedJson == candidate.SubmittedJson, "Original item/header/submitted-query effects changed.");
        Check.Require(ordinary.CallerCanceled == candidate.CallerCanceled && ordinary.ErrorTokenIsCaller == candidate.ErrorTokenIsCaller
            && ordinary.WaitHeldForAcquire == candidate.WaitHeldForAcquire && ordinary.WaitHeldForRelease == candidate.WaitHeldForRelease,
            "Cancellation/held-wait behavior changed.");
        var a = ordinary.State.Trace.ToArray(); var b = candidate.State.Trace.ToArray();
        Check.Require(a.Length == b.Length, "Original observation count changed.");
        for (int index = 0; index < a.Length; index++)
        {
            Check.Require(a[index].Operation == b[index].Operation && a[index].Argument == b[index].Argument
                && a[index].Cancellable == b[index].Cancellable && a[index].CanceledAtRead == b[index].CanceledAtRead, "Original observation order/input changed.");
            for (int other = 0; other < a.Length; other++)
                Check.Require((a[index].Token == a[other].Token) == (b[index].Token == b[other].Token), "Per-run token equality graph changed.");
        }
    }
}
