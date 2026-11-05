using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

/// <summary>Exposes a controlled authenticated circuit principal without an HTTP context.</summary>
public sealed class FixedAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
{
    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(principal));
}
