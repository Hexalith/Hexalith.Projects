using Microsoft.AspNetCore.Authorization;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class MutablePolicyProvider(IAuthorizationPolicyProvider inner) : IAuthorizationPolicyProvider
{
    internal string? NullPolicy;
    internal Exception? Failure;
    internal readonly List<string> Calls = [];
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        Calls.Add(policyName);
        if (Failure is not null) throw Failure;
        return policyName == NullPolicy ? Task.FromResult<AuthorizationPolicy?>(null) : inner.GetPolicyAsync(policyName);
    }
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => inner.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => inner.GetFallbackPolicyAsync();
}
