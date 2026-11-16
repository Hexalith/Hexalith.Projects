namespace Hexalith.Projects.PackageFixtures;

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>Creates private trust, fresh OIDC identities and an isolated store without source diagnostics.</summary>
internal sealed class FixtureInputs(FixtureScratch scratch, string root, bool actualOwners, string scenario = "browser") : IAsyncDisposable
{
    private PackageUserAuthority? _nativeAuthority;
    internal bool ActualOwners => actualOwners;

    internal bool NativeCredentials => scenario is "main" or "duplicate" or "full-replay";
    internal Dictionary<string, string> ReaderTokens { get; } = [];
    internal string Password { get; } = Secret();

    internal string ClientSecret { get; } = Secret();

    internal string Authority { get; private set; } = string.Empty;

    internal string Redis { get; private set; } = string.Empty;

    internal string RedisContainer { get; private set; } = string.Empty;

    internal string Tenant => scratch.Isolation + "-tenant";

    internal string ForeignTenant => scratch.Isolation + "-foreign";

    internal Dictionary<string, string> Tokens { get; } = [];

    internal Dictionary<string, string> Subjects { get; } = [];

    internal IEnumerable<string> ProtectedValues => new[] { Password, ClientSecret,
        "Synthetic project", "Synthetic descriptive metadata", "Restricted legacy", "Foreign synthetic name" }.Concat(Tokens.Values).Concat(ReaderTokens.Values);

    internal static string Secret() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    internal static int Port()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    internal async Task PrepareAsync(CancellationToken cancellationToken)
    {
        using RSA key = RSA.Create(2048);
        using RSA nativeUserKey = RSA.Create(2048);
        await scratch.WriteAsync("native-user.pem", nativeUserKey.ExportPkcs8PrivateKeyPem(), cancellationToken).ConfigureAwait(false);
        foreach (string name in new[] { "invocation", "owner-source" })
        {
            Directory.CreateDirectory(scratch.File(name));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(scratch.File(name), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        CertificateRequest request = new("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        SubjectAlternativeNameBuilder names = new();
        names.AddDnsName("localhost"); names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyCertSign, true));
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        string certificatePassword = Secret();
        await scratch.WriteAsync("certificate.pem", certificate.ExportCertificatePem(), cancellationToken).ConfigureAwait(false);
        await scratch.WriteAsync("certificate.key", key.ExportPkcs8PrivateKeyPem(), cancellationToken).ConfigureAwait(false);
        await System.IO.File.WriteAllBytesAsync(scratch.File("certificate.pfx"), certificate.Export(X509ContentType.Pfx, certificatePassword), cancellationToken).ConfigureAwait(false);
        if (!OperatingSystem.IsWindows())
            System.IO.File.SetUnixFileMode(scratch.File("certificate.pfx"), UnixFileMode.UserRead | UnixFileMode.UserWrite);
        JsonObject realm = JsonNode.Parse(await System.IO.File.ReadAllTextAsync(Path.Combine(root,
            "src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json"), cancellationToken).ConfigureAwait(false))!.AsObject();
        JsonArray originalClients = realm["clients"]!.AsArray();
        JsonObject Client(string id) => originalClients.Single(node => node!["clientId"]!.GetValue<string>() == id)!.DeepClone().AsObject();
        JsonObject bootstrap = Client("hexalith-eventstore"), reader = Client("hexalith-eventstore"), ui = Client("hexalith-projects-ui");
        bootstrap["clientId"] = "supported-read-bootstrap"; reader["clientId"] = "supported-read-reader"; ui["secret"] = ClientSecret;
        foreach (JsonObject client in new[] { bootstrap, reader, ui })
        {
            JsonArray mappers = client["protocolMappers"]!.AsArray();
            if (client != bootstrap)
                foreach (JsonNode mapper in mappers.Where(node => node!["config"]?["claim.name"]?.GetValue<string>() is "global_admin" or "is_global_admin").ToArray()!) mappers.Remove(mapper);
            mappers.Add(new JsonObject { ["name"] = "supported-read-users-audience", ["protocol"] = "openid-connect",
                ["protocolMapper"] = "oidc-audience-mapper", ["consentRequired"] = false,
                ["config"] = new JsonObject { ["included.client.audience"] = "supported-read-users", ["access.token.claim"] = "true", ["id.token.claim"] = "false" } });
        }
        realm["clients"] = new JsonArray(bootstrap, reader, ui);
        JsonObject template = realm["users"]!.AsArray().Single(node => node!["username"]!.GetValue<string>() == "admin-user")!.AsObject();
        JsonArray users = [];
        string[] permissions = ["command:submit", "query:read", "replay:read", "read_metadata", "projects:read", "projects:list",
            "projects:inspect_descriptive_metadata", "create_folder", "manage_folder_access", "manage_organization_acl", "tenants:create",
            "tenants:add-user", "tenants:refresh-access", "tenants:read", "global-administrators:read",
            "admin:reconcile-global-administrators-delivery", "global-administrators:bootstrap", "projects:create", "projects:archive",
            "projects:restore", "projects:update_setup", "projects:set_folder", "projects:link_file_reference", "projects:unlink_file_reference",
            "projects:link_memory", "projects:unlink_memory", "projects:confirm_resolution", "projects:link_conversation",
            "projects:move_conversation", "projects:unlink_conversation"];
        foreach ((string username, string tenant) in new[] { ("actor-1", Tenant), ("foreign-actor", ForeignTenant) })
        {
            JsonObject user = template.DeepClone().AsObject();
            user["id"] = username; user["username"] = username; user["email"] = username + "@fixture.invalid";
            user["firstName"] = "Fixture"; user["lastName"] = "Reader";
            user["credentials"] = new JsonArray(new JsonObject { ["type"] = "password", ["value"] = Password, ["temporary"] = false });
            user["attributes"] = JsonSerializer.SerializeToNode(new { tenants = new[] { tenant }, current_tenant = new[] { tenant },
                domains = new[] { "projects", "folders", "tenants", "global-administrators" }, permissions,
                global_admin = new[] { "true" }, projects_role = new[] { "TenantProjectAdministrator" } });
            users.Add(user);
        }
        realm["users"] = users;
        await scratch.WriteAsync("realm.json", realm.ToJsonString(), cancellationToken).ConfigureAwait(false);
        int oidcPort = Port();
        string origin = "https://127.0.0.1:" + oidcPort;
        Authority = origin + "/realms/hexalith";
        if (NativeCredentials)
            _nativeAuthority = await PackageUserAuthority.StartAsync(origin, Authority, scratch.File("native-user.pem"), certificate,
                Password, new Dictionary<string, string> { ["actor-1"] = Tenant, ["foreign-actor"] = ForeignTenant }, permissions, cancellationToken).ConfigureAwait(false);
        else _ = await scratch.ContainerAsync("oidc", ["-p", $"127.0.0.1:{oidcPort}:8443", "-v",
            scratch.File("realm.json") + ":/opt/keycloak/data/import/fixture.json:ro", "-v",
            scratch.File("certificate.pem") + ":/opt/keycloak/conf/server.crt:ro", "-v",
            scratch.File("certificate.key") + ":/opt/keycloak/conf/server.key:ro", "quay.io/keycloak/keycloak:26.6",
            "start-dev", "--import-realm", "--http-enabled=false", "--https-certificate-file=/opt/keycloak/conf/server.crt",
            "--https-certificate-key-file=/opt/keycloak/conf/server.key", "--hostname=" + origin], cancellationToken).ConfigureAwait(false);
        Authority = origin + "/realms/hexalith";
        using HttpClient http = CreateHttpClient();
        await AwaitOidcAsync(http, cancellationToken).ConfigureAwait(false);
        foreach (string username in new[] { "actor-1", "foreign-actor" })
        {
            using FormUrlEncodedContent body = new(new Dictionary<string, string> { ["grant_type"] = "password",
                ["client_id"] = "supported-read-bootstrap", ["username"] = username, ["password"] = Password, ["scope"] = "openid" });
            using HttpResponseMessage response = await http.PostAsync(Authority + "/protocol/openid-connect/token", body, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
            string token = json.RootElement.GetProperty("access_token").GetString()!;
            Tokens.Add(username, token);
            string payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
            using JsonDocument claims = JsonDocument.Parse(Convert.FromBase64String(payload.PadRight((payload.Length + 3) / 4 * 4, '=')));
            Subjects.Add(username, claims.RootElement.GetProperty("sub").GetString()!);
            using FormUrlEncodedContent readerBody = new(new Dictionary<string, string> { ["grant_type"] = "password",
                ["client_id"] = "supported-read-reader", ["username"] = username, ["password"] = Password, ["scope"] = "openid" });
            using HttpResponseMessage readerResponse = await http.PostAsync(Authority + "/protocol/openid-connect/token", readerBody, cancellationToken).ConfigureAwait(false);
            readerResponse.EnsureSuccessStatusCode();
            using JsonDocument readerJson = JsonDocument.Parse(await readerResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
            ReaderTokens.Add(username, readerJson.RootElement.GetProperty("access_token").GetString()!);
        }
        int redisPort = Port();
        RedisContainer = await scratch.ContainerAsync("redis", ["-p", $"127.0.0.1:{redisPort}:6379", "redis:6"], cancellationToken).ConfigureAwait(false);
        Redis = "127.0.0.1:" + redisPort;
        await scratch.WriteAsync("discovery.json", JsonSerializer.Serialize(new { apiVersion = "dapr.io/v1alpha1", kind = "Configuration",
            metadata = new { name = scratch.Isolation }, spec = new { accessControl = new { defaultAction = "deny", trustDomain = "public" },
                nameResolution = new { component = "sqlite", version = "v1", configuration = new { connectionString = scratch.File("discovery.sqlite") } } } }), cancellationToken).ConfigureAwait(false);
        using RSA owner = RSA.Create(2048), tenantOwner = RSA.Create(2048), folderOwner = RSA.Create(2048);
        using RSA folderScan = RSA.Create(2048), folderWorker = RSA.Create(2048), folderWorkerScan = RSA.Create(2048);
        var fixture = new { Enabled = true, IsolationId = scratch.Isolation, Root = root, UserIssuer = Authority,
            Mode = actualOwners ? "actual-owner" : "compatibility", Scenario = scenario == "browser" ? "main" : scenario, ChannelToken = Secret(),
            FoldersSecret = Secret(), FolderOwnerPrivateKey = Convert.ToBase64String(folderOwner.ExportPkcs8PrivateKey()),
            FolderOwnerPublicKey = Convert.ToBase64String(folderOwner.ExportSubjectPublicKeyInfo()),
            FolderScanPrivateKey = Convert.ToBase64String(folderScan.ExportPkcs8PrivateKey()), FolderScanPublicKey = Convert.ToBase64String(folderScan.ExportSubjectPublicKeyInfo()),
            FolderWorkersSecret = Secret(), FolderWorkerPrivateKey = Convert.ToBase64String(folderWorker.ExportPkcs8PrivateKey()),
            FolderWorkerPublicKey = Convert.ToBase64String(folderWorker.ExportSubjectPublicKeyInfo()),
            FolderWorkerScanPrivateKey = Convert.ToBase64String(folderWorkerScan.ExportPkcs8PrivateKey()), FolderWorkerScanPublicKey = Convert.ToBase64String(folderWorkerScan.ExportSubjectPublicKeyInfo()),
            CertificatePath = scratch.File("certificate.pfx"), CertificatePassword = certificatePassword, TrustBundle = scratch.File("certificate.pem"),
            BootstrapPassword = Password, DiscoveryConfiguration = scratch.File("discovery.json"), Tenant, ForeignTenant,
            Browser = new { Enabled = true, PrimaryActor = Subjects["actor-1"], ForeignActor = Subjects["foreign-actor"], ClientSecret,
                Authority, Origin = origin, Password, TokenEndpoint = Authority + "/protocol/openid-connect/token" },
            CatalogDigestKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)), ReceiptKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            EventStoreSecret = Secret(), ProjectsSecret = Secret(), TenantsSecret = Secret(),
            OwnerPrivateKey = Convert.ToBase64String(owner.ExportPkcs8PrivateKey()), OwnerPublicKey = Convert.ToBase64String(owner.ExportSubjectPublicKeyInfo()),
            TenantOwnerPrivateKey = Convert.ToBase64String(tenantOwner.ExportPkcs8PrivateKey()), TenantOwnerPublicKey = Convert.ToBase64String(tenantOwner.ExportSubjectPublicKeyInfo()) };
        await scratch.WriteAsync("configuration.json", JsonSerializer.Serialize(new { EnableKeycloak = false, KeycloakPersistent = false,
            Projects = new { PackageFixtures = fixture }, Dapr = new { RedisHost = Redis, PlacementHostAddress = "127.0.0.1:" + Port(),
                SchedulerHostAddress = "127.0.0.1:" + Port() } }), cancellationToken).ConfigureAwait(false);
    }

    internal HttpClientHandler CreateHttpHandler()
    {
        using X509Certificate2 trusted = X509CertificateLoader.LoadCertificateFromFile(scratch.File("certificate.pem"));
        byte[] trustedHash = trusted.GetCertHash(HashAlgorithmName.SHA256);
        HttpClientHandler handler = new();
        handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
            errors == System.Net.Security.SslPolicyErrors.None || certificate is not null
                && CryptographicOperations.FixedTimeEquals(certificate.GetCertHash(HashAlgorithmName.SHA256), trustedHash);
        return handler;
    }

    internal HttpClient CreateHttpClient() => new(CreateHttpHandler(), disposeHandler: true) { Timeout = TimeSpan.FromSeconds(15) };

    public async ValueTask DisposeAsync()
    {
        if (_nativeAuthority is not null) await _nativeAuthority.DisposeAsync().ConfigureAwait(false);
    }

    private async Task AwaitOidcAsync(HttpClient http, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        while (true)
        {
            try
            {
                using HttpResponseMessage response = await http.GetAsync(Authority + "/.well-known/openid-configuration", timeout.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(false));
                    if (document.RootElement.GetProperty("issuer").GetString() == Authority) return;
                }
            }
            catch (HttpRequestException) { }
            await Task.Delay(250, timeout.Token).ConfigureAwait(false);
        }
    }
}
