namespace Hexalith.Projects.UI.Tests.Diagnostics;

using System.Security.Claims;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Projects.UI.Diagnostics;
using Microsoft.AspNetCore.Components.Authorization;
using NSubstitute;

/// <summary>Creates authenticated scoped SDK adapters without replacing the SDK contract.</summary>
internal sealed class SupportedReadTestContext
{
    public IEventStoreGatewayClient Client { get; } = Substitute.For<IEventStoreGatewayClient>();
    public IFrontComposerTenantContextAccessor Authority { get; } = Substitute.For<IFrontComposerTenantContextAccessor>();
    public AuthenticationStateProvider Authentication { get; } = Substitute.For<AuthenticationStateProvider>();
    public ProjectReadQueryClient Queries
    {
        get;
    }
    public SupportedReadTestContext()
    {
        SetTenant("tenant-a");
        Queries = new(Client, [Authority], [Authentication]);
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0013", Justification = "This call configures an NSubstitute return value; it does not read or cache live authentication state.")]
    public void SetTenant(string tenant)
    {
        TenantContextSnapshot snapshot = new(tenant, "user-1", true, "test-correlation");
        Authority.TryGetContext(Arg.Any<string?>(), Arg.Any<string>()).Returns(TenantContextResult.Success(snapshot));
        Authority.Revalidate(Arg.Any<TenantContextSnapshot>(), Arg.Any<string>()).Returns(call =>
            call.Arg<TenantContextSnapshot>().TenantId == tenant ? TenantContextResult.Success(snapshot)
                : TenantContextResult.Failure(TenantContextFailureCategory.StaleTenantContext, "test-correlation"));
        Authentication.GetAuthenticationStateAsync().Returns(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "user-1"), new Claim("eventstore:current-tenant", tenant),
            new Claim(FrontComposerTokenSession.ClaimType, "session-" + tenant)], "test")))));
    }
}
