namespace Hexalith.Projects.PackageFixtures;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.IdentityModel.Tokens;

/// <summary>Private native-matrix public-user issuer with a key distinct from workload trust.</summary>
internal sealed class PackageUserAuthority(WebApplication application, RSA key, X509Certificate2 serverCertificate) : IAsyncDisposable
{
    internal static async Task<PackageUserAuthority> StartAsync(string origin, string issuer, string keyPath,
        X509Certificate2 certificate, string password, IReadOnlyDictionary<string, string> tenants, string[] permissions,
        CancellationToken cancellationToken)
    {
        RSA key = RSA.Create();
        key.ImportFromPem(await File.ReadAllTextAsync(keyPath, cancellationToken).ConfigureAwait(false));
        RSAParameters publicKey = key.ExportParameters(false);
        X509Certificate2 serverCertificate = X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), null);
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args: ["--preventHostingStartup=true"]);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.ConfigureKestrel(options => options.Listen(System.Net.IPAddress.Loopback, new Uri(origin).Port,
            listen => listen.UseHttps(serverCertificate)));
        WebApplication app = builder.Build();
        app.MapGet("/realms/hexalith/.well-known/openid-configuration", () => Results.Json(new
        {
            issuer, jwks_uri = issuer + "/jwks", token_endpoint = issuer + "/protocol/openid-connect/token",
            authorization_endpoint = issuer + "/unsupported-native-browser", response_types_supported = new[] { "code" },
            subject_types_supported = new[] { "public" }, id_token_signing_alg_values_supported = new[] { "RS256" },
        }));
        app.MapGet("/realms/hexalith/jwks", () => Results.Json(new { keys = new[] { new
        {
            kty = "RSA", use = "sig", alg = "RS256", kid = "synthetic-user",
            n = Base64UrlEncoder.Encode(publicKey.Modulus!), e = Base64UrlEncoder.Encode(publicKey.Exponent!),
        } } }));
        app.MapPost("/realms/hexalith/protocol/openid-connect/token", async (HttpRequest request) =>
        {
            IFormCollection form = await request.ReadFormAsync(request.HttpContext.RequestAborted).ConfigureAwait(false);
            string username = form["username"].ToString(), client = form["client_id"].ToString();
            if (form["grant_type"] != "password" || form["password"] != password || !tenants.TryGetValue(username, out string? tenant)
                || client is not ("supported-read-bootstrap" or "supported-read-reader")) return Results.Unauthorized();
            List<Claim> claims = [new("sub", username), new("tenant_id", tenant), new("eventstore:tenant", tenant),
                new("azp", "operator-web"), new("eventstore:surface", "ui"), new("client_id", "synthetic-client"),
                new("projects:role", "TenantProjectAdministrator")];
            foreach (string permission in permissions) claims.Add(new("eventstore:permission", permission));
            foreach (string domain in new[] { "projects", "folders", "tenants", "global-administrators" }) claims.Add(new("eventstore:domain", domain));
            if (client == "supported-read-bootstrap") claims.Add(new("global_admin", "true"));
            string token = new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
            {
                Issuer = issuer, Audience = "supported-read-users", Subject = new ClaimsIdentity(claims),
                NotBefore = DateTime.UtcNow.AddSeconds(-5), Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = new(new RsaSecurityKey(key) { KeyId = "synthetic-user" }, SecurityAlgorithms.RsaSha256),
            });
            return Results.Json(new { access_token = token, token_type = "Bearer", expires_in = 1800 });
        });
        try { await app.StartAsync(cancellationToken).ConfigureAwait(false); return new(app, key, serverCertificate); }
        catch { await app.DisposeAsync().ConfigureAwait(false); key.Dispose(); serverCertificate.Dispose(); throw; }
    }

    public async ValueTask DisposeAsync()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        try { await application.StopAsync(timeout.Token).ConfigureAwait(false); }
        finally { await application.DisposeAsync().ConfigureAwait(false); key.Dispose(); serverCertificate.Dispose(); }
    }
}
