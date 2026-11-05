using System.Security.Claims;
using System.Text.Json;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

FrontComposerUserTokenStore store = new();
CircuitServicesAccessor circuits = new();
RecordingHandler recorder = new();
using HttpClient client = new(new FrontComposerGatewayAuthorizationHandler(new HttpContextAccessor(), circuits, store)
{
    InnerHandler = recorder,
});
using ServiceProvider tenantA = Circuit("shared-subject", "tenant-a");
using ServiceProvider tenantB = Circuit("shared-subject", "tenant-b");
using ServiceProvider distinctUser = Circuit("distinct-subject", "tenant-c");
store.Set("shared-subject", "synthetic-first-session", DateTimeOffset.UtcNow.AddMinutes(5));
store.Set("distinct-subject", "synthetic-distinct-user", DateTimeOffset.UtcNow.AddMinutes(5));
circuits.Services = tenantA;
using (HttpResponseMessage response = await client.GetAsync("https://no-network.invalid/first"))
{
    response.EnsureSuccessStatusCode();
}
bool initialSessionCorrect = recorder.LastBearer == "synthetic-first-session";
store.Set("shared-subject", "synthetic-second-session", DateTimeOffset.UtcNow.AddMinutes(5));
circuits.Services = tenantB;
using (HttpResponseMessage response = await client.GetAsync("https://no-network.invalid/second"))
{
    response.EnsureSuccessStatusCode();
}
bool secondSessionCorrect = recorder.LastBearer == "synthetic-second-session";
circuits.Services = distinctUser;
using (HttpResponseMessage response = await client.GetAsync("https://no-network.invalid/distinct-user"))
{
    response.EnsureSuccessStatusCode();
}
bool distinctUserCorrect = recorder.LastBearer == "synthetic-distinct-user";
circuits.Services = tenantA;
using (HttpResponseMessage response = await client.GetAsync("https://no-network.invalid/first-after-second-login"))
{
    response.EnsureSuccessStatusCode();
}
bool sameSubjectTenantIsolation = recorder.LastBearer == "synthetic-first-session";
bool firstCircuitReceivedSecondSessionCredential = recorder.LastBearer == "synthetic-second-session";
circuits.Services = null;
Console.WriteLine(JsonSerializer.Serialize(new
{
    initialSessionCorrect,
    secondSessionCorrect,
    distinctUserCorrect,
    sameSubjectTenantIsolation,
    firstCircuitReceivedSecondSessionCredential,
    networkUsed = false,
    syntheticCredentialsOnly = true,
}));
return initialSessionCorrect && secondSessionCorrect && distinctUserCorrect && sameSubjectTenantIsolation ? 0 : 1;

static ServiceProvider Circuit(string subject, string tenant)
{
    ClaimsPrincipal principal = new(new ClaimsIdentity(new[]
    {
        new Claim("sub", subject),
        new Claim("eventstore:current-tenant", tenant),
    }, "controlled-server-session"));
    return new ServiceCollection()
        .AddSingleton<AuthenticationStateProvider>(new FixedAuthenticationStateProvider(principal))
        .BuildServiceProvider();
}
