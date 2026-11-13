namespace Story65.GatewayControllerProof;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

internal static class SharedResources
{
    private static readonly RSA Rsa = RSA.Create(2048);
    internal static readonly string PrivateKey = Convert.ToBase64String(Rsa.ExportPkcs8PrivateKey());
    internal static readonly string PublicKey = Convert.ToBase64String(Rsa.ExportSubjectPublicKeyInfo());
    private static readonly string JwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private static WebApplication? _app;
    internal static WorkloadAssertionValidator Validator = null!;
    internal static IOptionsMonitor<WorkloadAuthenticationOptions> WorkloadOptions = null!;
    internal static CommittedRouteCatalogSnapshot Snapshot = null!;
    internal static string QueryType = "", Domain = "";

    internal static void Prepare(string catalogPath)
    {
        if (_app is not null) return;
        var original = RouteCatalogCodec.Decode(File.ReadAllBytes(catalogPath));
        var operations = original.Declarations.Select(declaration => new RouteOperationContract(declaration.RouteId, declaration.ContractVersion,
            declaration.Kind == "projection" || declaration.Execution!.RequiresDelegatedContext ? "internal" : "public", "agent-eligible",
            JsonSerializer.SerializeToElement(new { type = "object" }), JsonSerializer.SerializeToElement(new { type = "object" }), "",
            declaration.Kind, declaration.Domain, declaration.MessageType ?? declaration.ProjectionType))
            .Select(operation => operation with { Digest = RouteCatalogCodec.ComputeOperationContractDigest(operation) }).ToArray();
        var envelope = VersionedRouteCatalogBuilder.Build(new(1, "Development", original.Declarations, operations,
            original.Routes.Select(route => new RouteCompositionBinding(route.RouteId, route.AppId, route.Method, route.ContractVersion, false, route.ResolverTenantPattern)).ToArray(), original.Idempotency));
        var selected = envelope.Declarations.First(d => d.Kind == "query" && d.PlatformAdmission is null
            && operations.Single(operation => operation.RouteId == d.RouteId).Ingress == "public");
        QueryType = selected.MessageType!; Domain = selected.Domain;
        Snapshot = new(RouteCatalogCodec.Encode(envelope), RouteCatalogCodec.ComputeRootDigest(envelope), 1);
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Issuer"] = "synthetic-gateway-issuer", ["Authentication:JwtBearer:Audience"] = "synthetic-human-audience",
            ["Authentication:JwtBearer:SigningKey"] = JwtKey, ["Authentication:JwtBearer:Authority"] = "",
            ["Authentication:JwtBearer:AllowedAlgorithms:0"] = "HS256", ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
        });
        builder.Services.AddAuthentication().AddEventStoreWorkloadScheme("DaprInternal", "Authentication:DaprInternal",
            (options, _) => { options.Audience = "eventstore"; options.AllowedCallers.Add("eventstore"); });
        _app = builder.Build(); // In-memory service construction only; never Start/Run/listen.
        Validator = _app.Services.GetRequiredService<WorkloadAssertionValidator>();
        WorkloadOptions = _app.Services.GetRequiredService<IOptionsMonitor<WorkloadAuthenticationOptions>>();
    }
    internal static string OriginAssertion(QueryEnvelope original)
    {
        var claims = new Dictionary<string, object>
        {
            [EventStoreWorkloadAuthenticationDefaults.CallerClaimType] = "eventstore",
            [EventStoreWorkloadAuthenticationDefaults.OperationClaimType] = EventStoreWorkloadOperations.DomainServiceQuery,
        };
        foreach (var pair in QueryCallerAuthorityBinding.Bind(original)) claims[pair.Key] = pair.Value;
        DateTime now = DateTime.UtcNow;
        return new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "synthetic-gateway-issuer", Audience = "projects", IssuedAt = now, NotBefore = now, Expires = now.AddMinutes(2), Claims = claims,
            SigningCredentials = new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256),
        });
    }
}
