namespace Story65.AdvisoryWaveProof;

using Hexalith.Projects.Authorization;

internal sealed class TracedAuthorization(TraceLog trace, ProofCase scenario) : IProjectEventStoreAuthorizationValidator
{
    public Task<EventStoreAuthorizationValidationResult> ValidateAsync(EventStoreAuthorizationValidationRequest request, CancellationToken cancellationToken = default)
    {
        string item = request.ProjectId ?? "scope";
        int ordinal = trace.Next("authorize:" + item);
        int wave = request.ProjectId is null ? ordinal == 1 ? 1 : 7 : ordinal == 1 ? 4 : 6;
        return trace.CallAsync(wave, request.ProjectId is null ? "scope-authorize" : "project-authorize", item, cancellationToken, () =>
        {
            TraceLog.Require(request.ActionToken == "projects:read" && request.TenantId == "tenant-a"
                && request.PrincipalId == "actor-1" && request.CorrelationId == "read-request", "Authorization input changed.");
            bool deny = scenario.Mutation == "deny-initial" && wave == 1
                || scenario.Mutation == "revoke-scope" && wave == 7
                || scenario.Mutation == "revoke-project" && wave == 6;
            return Task.FromResult(deny ? EventStoreAuthorizationValidationResult.Denied()
                : EventStoreAuthorizationValidationResult.Allowed("synthetic-current"));
        }, System.Text.Json.JsonSerializer.Serialize(request));
    }
}
