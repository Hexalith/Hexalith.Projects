namespace Story65.GatewayControllerProof;

using Microsoft.AspNetCore.Mvc;

internal sealed record InvocationOutcome(IActionResult? Result, Exception? Error, FixtureState State,
    bool CallerCanceled, bool ErrorTokenIsCaller, string ItemsJson, string HeadersJson, string? SubmittedJson,
    bool WaitHeldForAcquire, bool WaitHeldForRelease);
