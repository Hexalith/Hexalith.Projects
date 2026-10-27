#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hexalith.EventStore.ErrorHandling;
using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Server.Projections.ProjectReads;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

/// <summary>Calls only the launched topology; missing resources are qualification failures.</summary>
internal sealed class SupportedProjectReadDistributedFixture : IDisposable
{
    private readonly Dictionary<(string Tenant, string Project), ProjectionDispatchRequest> _seedRequests = [];
    private readonly Dictionary<(string Tenant, string Project), StreamReadPage> _seedStreams = [];
    private readonly List<JsonElement> _replayObservations = [];
    private string[] _ownedDomainKeyCensus = [];
    private bool _duplicateVerified;
    private bool _replayVerified;
    private readonly JsonElement _configuration;
    private readonly Dictionary<string, string> _endpoints;
    private readonly RSA _userKey = RSA.Create();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(45) };
    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;
    /// <summary>Gets the owned primary tenant.</summary>
    internal string Tenant => Setting("Tenant");
    /// <summary>Gets the distinct populated foreign tenant.</summary>
    internal string ForeignTenant => Setting("ForeignTenant");
    /// <summary>Gets the launcher-owned namespace.</summary>
    internal string Isolation => Setting("IsolationId");
    /// <summary>Gets the sanitized evidence destination.</summary>
    internal string Evidence { get; } = Environment.GetEnvironmentVariable("SUPPORTED_PROJECT_READ_EVIDENCE")!;

    /// <summary>Loads only the managed launcher configuration and synthetic user signer.</summary>
    internal SupportedProjectReadDistributedFixture()
    {
        string? configuration = Environment.GetEnvironmentVariable("SUPPORTED_PROJECT_READ_CONFIG");
        string? endpoints = Environment.GetEnvironmentVariable("SUPPORTED_PROJECT_READ_ENDPOINTS");
        Assert.True(configuration is not null && endpoints is not null, "Run the managed supported-read launcher; missing owned resources cannot qualify.");
        _configuration = JsonDocument.Parse(File.ReadAllBytes(configuration!)).RootElement.GetProperty("Projects").GetProperty("SupportedReads").Clone();
        _endpoints = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllBytes(endpoints!))!;
        _userKey.ImportFromPem(File.ReadAllText(Setting("UserKeyPath")));
    }

    /// <summary>Signs a synthetic public credential using trust distinct from workload trust.</summary>
    internal string HumanToken(string tenant, bool chatbot = true, bool inspection = false, bool administrator = false, bool foreignTransportGrant = false)
    {
        List<Claim> claims = [new("sub", tenant == ForeignTenant ? "foreign-actor" : "actor-1"), new("tenant_id", tenant), new("eventstore:tenant", tenant),
            new("azp", chatbot ? "chatbot" : "operator-web"), new("client_id", "synthetic-client"), new("act", "{\"sub\":\"delegation-1\"}"),
            new("scope", "projects.read projects.list"), new("projects:role", chatbot ? "ProjectUser" : "TenantOperator")];
        foreach (string permission in new[] { "command:submit", "query:read", "replay:read", "read_metadata", "projects:read", "projects:list",
            "create_folder", "manage_folder_access", "manage_organization_acl" }) claims.Add(new("eventstore:permission", permission));
        if (inspection) claims.Add(new("eventstore:permission", "projects:inspect_descriptive_metadata"));
        // Admit the negative control through the gateway's coarse tenant check. The current
        // Project/Folder membership remains absent and must produce the same safe refusal.
        if (foreignTransportGrant) claims.Add(new("eventstore:tenant", ForeignTenant));
        if (administrator)
        {
            claims.Add(new("global_admin", "true"));
            claims.Add(new("eventstore:tenant", "system"));
        }
        return new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = Setting("UserIssuer"), Audience = "supported-read-users", Subject = new ClaimsIdentity(claims),
            NotBefore = DateTime.UtcNow.AddSeconds(-5), Expires = DateTime.UtcNow.AddMinutes(20),
            SigningCredentials = new(new RsaSecurityKey(_userKey) { KeyId = "synthetic-user" }, SecurityAlgorithms.RsaSha256),
        });
    }

    /// <summary>Seeds both tenants through real authenticated commands and projection callbacks.</summary>
    internal async Task SeedAsync()
    {
        await CommandAsync("system", "global-administrators", "global-administrators", "BootstrapGlobalAdmin", new { UserId = "actor-1" }, Tenant).ConfigureAwait(false);
        await WaitForStateAsync("projection:global-administrators:singleton").ConfigureAwait(false);
        foreach (string tenant in new[] { Tenant, ForeignTenant })
        {
            string actor = tenant == Tenant ? "actor-1" : "foreign-actor";
            await CommandAsync("system", "tenants", tenant, "CreateTenant", new { TenantId = tenant, Name = "Synthetic tenant", Description = "Isolated read proof" }, Tenant).ConfigureAwait(false);
            await CommandAsync("system", "tenants", tenant, "AddUserToTenant", new { TenantId = tenant, UserId = actor, Role = "TenantOwner" }, Tenant).ConfigureAwait(false);
            StreamReadPage tenantEvents = await StreamAsync("system", "tenants", tenant, tenant).ConfigureAwait(false);
            // The owners intentionally subscribe to different existing tenant topics. Replay the exact
            // command-persisted events through the real Folder worker callback without changing defaults.
            foreach (StreamReadEvent item in tenantEvents.Events)
            {
                var envelope = new EventStoreDomainEventEnvelope(item.MessageId, tenant, "system", item.EventTypeName,
                    item.SequenceNumber, item.Timestamp, item.CorrelationId ?? item.MessageId, item.SerializationFormat, item.Payload)
                {
                    Domain = "tenants", GlobalPosition = item.GlobalPosition, CausationId = item.CausationId, UserId = item.UserId,
                };
                using HttpResponseMessage replay = await _http.PostAsJsonAsync(Dapr("folders-workers", "tenants/events"), envelope, Json, Cancellation).ConfigureAwait(false);
                if (replay.StatusCode != HttpStatusCode.OK)
                {
                    byte[] diagnostic = await replay.Content.ReadAsByteArrayAsync(Cancellation).ConfigureAwait(false);
                    string diagnosticPath = Path.Combine(Path.GetDirectoryName(Setting("CertificatePath"))!, "replay-diagnostic.json");
                    await File.WriteAllBytesAsync(diagnosticPath, diagnostic, Cancellation).ConfigureAwait(false);
                    if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(diagnosticPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                    using JsonDocument refusal = JsonDocument.Parse(diagnostic);
                    string code = refusal.RootElement.TryGetProperty("errorCode", out JsonElement error) ? error.GetString() ?? "none" : "application-response";
                    Require(code.Length < 80 && code.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'), "Unexpected transport error shape.");
                    throw new InvalidOperationException("Folder worker persisted-event callback refused (HTTP " + (int)replay.StatusCode + ", code " + code + ").");
                }
            }
            await WaitForStateAsync("projects:tenant-access:" + tenant).ConfigureAwait(false);
            await WaitForStateAsync("projection:folders:tenant-access:" + tenant).ConfigureAwait(false);
            await CommandAsync(tenant, "folders", tenant, "Hexalith.Folders.Commands.InitializeOrganizationAclBaseline", new
            {
                RequestSchemaVersion = "v1",
                Operations = new[] { new { PrincipalKind = "user", PrincipalId = actor, Action = "create_folder" },
                    new { PrincipalKind = "user", PrincipalId = actor, Action = "manage_folder_access" } },
            }, tenant).ConfigureAwait(false);
            foreach (string project in new[] { "project-a", "project-b" })
            {
                string folder = "folder-" + project;
                await CommandAsync(tenant, "folders", folder, "Hexalith.Folders.Commands.CreateFolder", new
                {
                    RequestSchemaVersion = "v1", FolderId = folder, ParentFolderId = (string?)null,
                    FolderMetadata = new { DisplayName = "Owned folder label", MetadataClass = (string?)null },
                }, tenant).ConfigureAwait(false);
                await ChangeFolderAccessAsync(tenant, folder, revoke: false).ConfigureAwait(false);
                await CommandAsync(tenant, "projects", project, "Hexalith.Projects.Commands.CreateProject", new
                {
                    Name = tenant == ForeignTenant ? "Foreign synthetic name" : "Synthetic project " + project,
                    Description = "Synthetic descriptive metadata", SetupMetadata = (string?)null,
                }, tenant).ConfigureAwait(false);
                await CommandAsync(tenant, "projects", project, "Hexalith.Projects.Commands.SetProjectFolder", new
                {
                    RequestSchemaVersion = "v1", Operation = "set", ProjectId = project, FolderId = folder,
                    FolderMetadata = new { DisplayName = "Owned folder label" }, ReplacementConfirmed = false,
                }, tenant).ConfigureAwait(false);
                await ProjectPersistedStreamAsync(tenant, project).ConfigureAwait(false);
                await WaitForStateAsync("projection:folders:folder-permissions:" + tenant + ":" + folder).ConfigureAwait(false);
                long head = await HeadAsync(tenant, folder).ConfigureAwait(false);
                Require(head >= 2, "The real Folder actor head was not advanced by persisted commands.");
            }
        }
        foreach (string domain in new[] { "projects", "folders" })
        {
            using JsonDocument catalog = await MetadataAsync(domain).ConfigureAwait(false);
            string[] queries = catalog.RootElement.GetProperty("domains").EnumerateArray().Single(item => item.GetProperty("domain").GetString() == domain)
                .GetProperty("queryTypes").Deserialize<string[]>()!;
            Require(queries.Contains(domain == "projects" ? ListProjectsQuery.QueryType : "Hexalith.Folders.Queries.AuthorizeFolderRead.v1"), "Real owner/query catalog missing.");
            // Wait for the platform's metadata publisher, never substitute the gateway registry.
            await WaitForStateAsync("admin:query-types:" + domain).ConfigureAwait(false);
        }
        await VerifyRawObserverBatchAsync().ConfigureAwait(false);
    }

    /// <summary>Changes owned Folder authority through its command handler and waits for the actual head.</summary>
    internal async Task ChangeFolderAccessAsync(string tenant, string folder, bool revoke)
    {
        string id = Unique(), actor = tenant == ForeignTenant ? "foreign-actor" : "actor-1";
        await CommandAsync(tenant, "folders", folder, "Hexalith.Folders.Commands." + (revoke ? "RevokeFolderAccess" : "GrantFolderAccess"), new
        {
            RequestSchemaVersion = "v1",
            Operations = new[] { new { PrincipalKind = "user", PrincipalId = actor, Action = "read_metadata" } },
        }, tenant).ConfigureAwait(false);
        long head = await HeadAsync(tenant, folder).ConfigureAwait(false);
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var value = await StateAsync("projection:folders:folder-permissions:" + tenant + ":" + folder).ConfigureAwait(false);
            if (value.Bytes.Length > 0 && JsonDocument.Parse(value.Bytes).RootElement.GetProperty("lastSequence").GetInt64() == head) return;
            await Task.Delay(100, Cancellation).ConfigureAwait(false);
        }
        throw new InvalidOperationException("The real Folder permission projection did not catch up to its owner actor head.");
    }

    /// <summary>Requires protected success through the real public query gateway.</summary>
    internal async Task<T> ReadAsync<T>(bool open = false, bool chatbot = true, bool inspection = false, ListProjectsQuery? list = null, string project = "project-a", string? tenant = null, bool tenantActor = false)
    {
        using HttpResponseMessage response = await QueryAsync(open, chatbot, inspection, list, project, tenant, tenantActor: tenantActor).ConfigureAwait(false);
        Require(response.StatusCode == HttpStatusCode.OK, "Supported read returned HTTP " + (int)response.StatusCode + ".");
        SubmitQueryResponse? result = await response.Content.ReadFromJsonAsync<SubmitQueryResponse>(Json, Cancellation).ConfigureAwait(false);
        T value = result!.Payload.Deserialize<T>(Json)!;
        if (value is ProjectListPage page)
            await File.AppendAllTextAsync(Path.Combine(Evidence, "list-admission-observations.jsonl"), JsonSerializer.Serialize(new
            {
                observedAt = DateTimeOffset.UtcNow, fixtureElapsedSeconds = (DateTimeOffset.UtcNow - _startedAt).TotalSeconds,
                tenantSha256 = Digest(tenant ?? Tenant), tenantActor, requestedPageSize = list?.PageSize,
                requestedContinuation = list?.Cursor is not null, httpStatus = (int)response.StatusCode, admission = CreateCursorControlEvidence(page),
            }, Json) + Environment.NewLine, CancellationToken.None).ConfigureAwait(false);
        return value;
    }

    /// <summary>Sends one supported list/open request without forwarding human credentials downstream.</summary>
    internal Task<HttpResponseMessage> QueryAsync(bool open = false, bool chatbot = true, bool inspection = false, ListProjectsQuery? list = null, string project = "project-a", string? tenant = null, bool inspectionPermission = true, CancellationToken? cancellationToken = null, bool tenantActor = false)
    {
        string targetTenant = tenant ?? Tenant;
        object input = open ? new GetProjectQuery(inspection) : list ?? new ListProjectsQuery(InspectDescriptiveMetadata: inspection);
        var query = new SubmitQueryRequest(targetTenant, "projects", open ? project : "projects", open ? GetProjectQuery.QueryType : ListProjectsQuery.QueryType,
            Payload: JsonSerializer.SerializeToElement(input, input.GetType(), Json));
        return SendHumanAsync("api/v1/queries", query, HumanToken(tenantActor ? targetTenant : Tenant, chatbot, inspection && inspectionPermission,
            foreignTransportGrant: targetTenant == ForeignTenant), cancellationToken);
    }

    /// <summary>Requires a metadata-free safe denial and returns its correlation-independent shape.</summary>
    internal async Task<string> SafeDenialAsync(string project = "project-a", string? tenant = null, bool inspection = false,
        bool open = true, ListProjectsQuery? list = null, bool chatbot = true, bool inspectionPermission = true)
    {
        using HttpResponseMessage response = await QueryAsync(open: open, project: project, tenant: tenant, inspection: inspection,
            list: list, chatbot: chatbot, inspectionPermission: inspectionPermission).ConfigureAwait(false);
        return await SafeProblemAsync(response).ConfigureAwait(false);
    }

    private async Task<string> SafeProblemAsync(HttpResponseMessage response)
    {
        Require(response.StatusCode == HttpStatusCode.NotFound, "Protected refusal must return safe HTTP 404.");
        Require(response.Content.Headers.ContentType?.MediaType == "application/problem+json", "Safe refusal requires the public problem shape.");
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Cancellation).ConfigureAwait(false));
        JsonElement problem = document.RootElement;
        Require(problem.EnumerateObject().Select(property => property.Name).Order().SequenceEqual(
            new[] { "correlationId", "detail", "instance", "reasonCode", "status", "title", "type" }), "Safe refusal contains unexpected fields.");
        Require(problem.GetProperty("status").GetInt32() == 404
            && problem.GetProperty("type").GetString() == ProblemTypeUris.NotFound
            && problem.GetProperty("title").GetString() == "Not Found"
            && problem.GetProperty("detail").GetString() == "The requested resource was not found."
            && problem.GetProperty("instance").GetString() == "/api/v1/queries"
            && problem.GetProperty("reasonCode").GetString() == QueryProblemReasonCodes.ProjectionMissing,
            "Safe refusal differs from the public metadata-free problem contract.");
        Require(Hexalith.EventStore.Middleware.CorrelationIdMiddleware.IsValidIdentifier(problem.GetProperty("correlationId").GetString()!), "Safe refusal correlation shape is invalid.");
        string canonical = JsonSerializer.Serialize(problem.EnumerateObject().Where(property => property.Name != "correlationId")
            .OrderBy(property => property.Name, StringComparer.Ordinal).ToDictionary(property => property.Name, property => property.Value));
        Require(ProtectedValues().All(value => !canonical.Contains(value, StringComparison.OrdinalIgnoreCase)), "A safe refusal disclosed protected metadata.");
        return canonical;
    }

    /// <summary>Reads actual persisted domain evidence from both tenants.</summary>
    internal async Task<SupportedProjectReadStateSnapshot[]> SnapshotAsync(bool allowIncomplete = false)
    {
        List<SupportedProjectReadStateSnapshot> result = [];
        foreach (string tenant in new[] { Tenant, ForeignTenant })
        {
            foreach (string key in new[] { ProjectReadModelKeys.Index(tenant), ProjectReadModelKeys.Detail(tenant, "project-a"), ProjectReadModelKeys.Detail(tenant, "project-b"),
                "projects:tenant-access:" + tenant, "projection:folders:tenant-access:" + tenant, "projection:folders:organization-permissions:" + tenant,
                "projection:folders:folder-permissions:" + tenant + ":folder-project-a", "projection:folders:folder-permissions:" + tenant + ":folder-project-b" })
            {
                SupportedProjectReadStateSnapshot snapshot = await StateAsync(key).ConfigureAwait(false);
                if (!allowIncomplete) Require(snapshot.Bytes.Length > 0 && snapshot.ETag.Length > 0, "Required persisted domain evidence is absent.");
                result.Add(snapshot);
            }
        }
        string[] domainKeys = (await RedisAsync("--scan").ConfigureAwait(false)).Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(key => IsOwnedDomainKey(key, Tenant, ForeignTenant)).Order(StringComparer.Ordinal).ToArray();
        foreach (string tenant in new[] { Tenant, ForeignTenant })
            foreach (string domain in new[] { "projects", "folders" })
                if (!allowIncomplete) Require(domainKeys.Any(key => key.Contains("AggregateActor", StringComparison.Ordinal)
                    && key.Contains(tenant + ":" + domain + ":", StringComparison.Ordinal)), "Required persisted aggregate journal evidence is absent.");
        foreach (SupportedProjectReadStateSnapshot snapshot in await StatesAsync(domainKeys.Except(result.Select(item => item.Key), StringComparer.Ordinal)).ConfigureAwait(false))
        {
            if (!allowIncomplete) Require(snapshot.Bytes.Length > 0 && snapshot.ETag.Length > 0, "A persisted domain observation is absent.");
            result.Add(snapshot);
        }
        return [.. result];
    }

    /// <summary>Requires exact domain bytes and ETags after read-only operations.</summary>
    internal async Task<SupportedProjectReadStateSnapshot[]> AssertUnchangedAsync(SupportedProjectReadStateSnapshot[] before)
    {
        SupportedProjectReadStateSnapshot[] observed = await SnapshotAsync().ConfigureAwait(false);
        Require(SameDomainState(before, observed), "A read changed the persisted domain key set, bytes or ETags.");
        return observed;
    }

    /// <summary>Restarts only the owned Projects process and requires real authenticated recovery.</summary>
    internal async Task RestartProjectsAsync()
    {
        await ResourceAsync("restart", "projects").ConfigureAwait(false);
        using JsonDocument process = JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(Evidence, "projects-restart-process.json"), Cancellation).ConfigureAwait(false));
        Require(IsRestartProcessEvidence(process.RootElement), "Projects restart lacks distinct observed owned process identities.");
        await WaitForAuthorizedRecoveryAsync("projects-process").ConfigureAwait(false);
    }

    /// <summary>Rejects missing exit/start observations, unchanged processes, or a replaced Debug application.</summary>
    internal static bool IsRestartProcessEvidence(JsonElement evidence)
    {
        if (evidence.ValueKind != JsonValueKind.Object) return false;
        bool Flag(string name) => evidence.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.True;
        if (!Flag("completed") || !Flag("oldProcessExited") || !Flag("endpointsRediscovered")
            || !evidence.TryGetProperty("resource", out JsonElement resource) || resource.ValueKind != JsonValueKind.String || resource.GetString() != "projects"
            || !evidence.TryGetProperty("action", out JsonElement action) || action.ValueKind != JsonValueKind.String || action.GetString() != "restart"
            || !evidence.TryGetProperty("before", out JsonElement before) || before.ValueKind != JsonValueKind.Object
            || !evidence.TryGetProperty("after", out JsonElement after) || after.ValueKind != JsonValueKind.Object) return false;
        foreach ((string pid, string started) in new[] { ("pid", "startTicks"), ("launcherPid", "launcherStartTicks") })
        {
            if (!before.TryGetProperty(pid, out JsonElement oldPid) || oldPid.ValueKind != JsonValueKind.Number || !oldPid.TryGetInt32(out int oldId) || oldId <= 0
                || !after.TryGetProperty(pid, out JsonElement newPid) || newPid.ValueKind != JsonValueKind.Number || !newPid.TryGetInt32(out int newId) || newId <= 0
                || !before.TryGetProperty(started, out JsonElement oldStart) || oldStart.ValueKind != JsonValueKind.String || !long.TryParse(oldStart.GetString(), out long oldTicks) || oldTicks <= 0
                || !after.TryGetProperty(started, out JsonElement newStart) || newStart.ValueKind != JsonValueKind.String || !long.TryParse(newStart.GetString(), out long newTicks) || newTicks <= 0
                || oldId == newId && oldTicks == newTicks) return false;
        }
        foreach (string field in new[] { "resourceName", "binarySha256", "assemblySha256" })
            if (!before.TryGetProperty(field, out JsonElement previous) || previous.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(previous.GetString())
                || !after.TryGetProperty(field, out JsonElement current) || current.ValueKind != JsonValueKind.String || current.GetString() != previous.GetString()) return false;
        return true;
    }

    /// <summary>Compares metadata, version, freshness, order and cursor presence, excluding only volatile observations and opaque token bytes.</summary>
    internal static bool SameStableReadMetadata(object expected, object actual)
    {
        if (expected.GetType() != actual.GetType()) return false;
        static bool Valid(AdmissionSnapshot snapshot) => snapshot.AsOf != default && snapshot.Components.All(component =>
            component.Name is not ("Folder" or "Authorization") || component.LastVerifiedAt is { } verified && verified != default);
        static bool ValidRead(object value) => value switch
        {
            ProjectListPage page => Valid(page.Snapshot) && page.Items.All(row => Valid(row.Snapshot)),
            GetProjectResponse opened => Valid(opened.Snapshot),
            _ => false,
        };
        static AdmissionSnapshot Stable(AdmissionSnapshot snapshot) => snapshot with
        {
            AsOf = default,
            Components = snapshot.Components.Select(component => component.Name is "Folder" or "Authorization"
                ? component with { LastVerifiedAt = default(DateTimeOffset) } : component).ToArray(),
        };
        static object Normalize(object value) => value switch
        {
            ProjectListPage page => page with { Cursor = page.Cursor is null ? null : "present", Snapshot = Stable(page.Snapshot),
                Items = page.Items.Select(row => row with { Snapshot = Stable(row.Snapshot) }).ToArray() },
            GetProjectResponse opened => opened with { Snapshot = Stable(opened.Snapshot) },
            _ => throw new ArgumentException("Unsupported restart observation."),
        };
        return ValidRead(expected) && ValidRead(actual) && JsonSerializer.Serialize(Normalize(expected), Json) == JsonSerializer.Serialize(Normalize(actual), Json);
    }

    /// <summary>Requires exactly the same domain key set, bytes (including exact watermarks) and concurrency identities.</summary>
    internal static bool SameDomainState(SupportedProjectReadStateSnapshot[] before, SupportedProjectReadStateSnapshot[] after)
    {
        if (before.Length == 0 || before.Length != after.Length || before.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count() != before.Length
            || after.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count() != after.Length) return false;
        return before.OrderBy(item => item.Key, StringComparer.Ordinal).Zip(after.OrderBy(item => item.Key, StringComparer.Ordinal))
            .All(pair => pair.First.Key == pair.Second.Key && pair.First.ETag == pair.Second.ETag
                && pair.First.Bytes.AsSpan().SequenceEqual(pair.Second.Bytes));
    }

    /// <summary>Records sanitized success or failure observations before asserting the restart invariant.</summary>
    internal Task VerifyRestartStateAsync(SupportedProjectReadStateSnapshot[] before)
        => CaptureRestartStateEvidenceAsync(Path.Combine(Evidence, "projects-restart-state.json"), before,
            () => SnapshotAsync(allowIncomplete: true), [Tenant, ForeignTenant]);

    /// <summary>Persists explicit missing/unreadable observations without retaining protected state or exception messages.</summary>
    internal static async Task CaptureRestartStateEvidenceAsync(string path, SupportedProjectReadStateSnapshot[] before,
        Func<Task<SupportedProjectReadStateSnapshot[]>> observe, string[] tenants)
    {
        SupportedProjectReadStateSnapshot[]? after = null;
        Exception? failure = null;
        try { after = await observe().ConfigureAwait(false); }
        catch (Exception exception) { failure = exception; }
        static string Hash(string value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
        bool watermarksUsable = true;
        object[] Watermarks(SupportedProjectReadStateSnapshot[]? values) => tenants.SelectMany(tenant =>
            new[] { ProjectReadModelKeys.Index(tenant), ProjectReadModelKeys.Detail(tenant, "project-a"), ProjectReadModelKeys.Detail(tenant, "project-b") }
                .Select(key =>
                {
                    long? position = null;
                    string status = values is null ? "unreadable" : "missing";
                    SupportedProjectReadStateSnapshot[]? matches = values?.Where(item => item.Key == key).ToArray();
                    SupportedProjectReadStateSnapshot? item = matches?.Length == 1 ? matches[0] : null;
                    if (item is { Bytes.Length: > 0 })
                    {
                        status = "unreadable";
                        try
                        {
                            using JsonDocument model = JsonDocument.Parse(item.Bytes);
                            if (model.RootElement.ValueKind == JsonValueKind.Object && model.RootElement.TryGetProperty("globalPosition", out JsonElement value)
                                && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long observed) && observed > 0)
                            { position = observed; status = "observed"; }
                        }
                        catch (JsonException) { }
                    }
                    watermarksUsable &= status == "observed";
                    return (object)new { tenantSha256 = Hash(tenant), keySha256 = Hash(key), globalPosition = position, status };
                })).ToArray();
        object[]? Fingerprints(SupportedProjectReadStateSnapshot[]? values) => values?.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item =>
            (object)new { keySha256 = Hash(item.Key), bytesSha256 = Convert.ToHexString(SHA256.HashData(item.Bytes)), etagSha256 = Hash(item.ETag),
                status = item.Bytes.Length > 0 && item.ETag.Length > 0 ? "observed" : "missing" }).ToArray();
        object[] beforeWatermarks = Watermarks(before), afterWatermarks = Watermarks(after);
        bool unchanged = after is not null && watermarksUsable && SameDomainState(before, after);
        string[]? missingKeys = after is null ? null : before.Select(item => item.Key)
            .Except(after.Where(item => item.Bytes.Length > 0 && item.ETag.Length > 0).Select(item => item.Key), StringComparer.Ordinal).Select(Hash).ToArray();
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            bothTenantsCompared = after is not null && tenants.Length == 2, domainKeysBytesEtagsAndWatermarksUnchanged = unchanged,
            domainKeyCount = before.Length, afterObservation = after is null ? "unreadable" : missingKeys!.Length > 0 ? "missing" : !watermarksUsable ? "unreadable" : "observed",
            failureType = failure?.GetType().Name, missingKeys, before = Fingerprints(before), after = Fingerprints(after), beforeWatermarks, afterWatermarks,
        }, new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None).ConfigureAwait(false);
        if (failure is OperationCanceledException) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        Require(unchanged, "Restart or recovered reads changed or could not observe persisted domain evidence.");
    }

    /// <summary>Enumerates owned domain families; audit and operational catalogs are observed separately.</summary>
    internal static bool IsOwnedDomainKey(string key, string tenant, string foreignTenant)
    {
        if (key.StartsWith("projects:read-audit:", StringComparison.Ordinal) || key.StartsWith("admin:", StringComparison.Ordinal)) return false;
        return new[] { tenant, foreignTenant }.Any(owner =>
            key.StartsWith($"projects:reads:v1:{owner.Length}:{owner}:", StringComparison.Ordinal)
            || key.StartsWith("projects:tenant-access:" + owner, StringComparison.Ordinal)
            || key.StartsWith("projection:folders:", StringComparison.Ordinal) && key.Contains(":" + owner, StringComparison.Ordinal)
            || key.Contains("AggregateActor", StringComparison.Ordinal) && (key.Contains(owner + ":", StringComparison.Ordinal)
                || key.Contains("system:tenants:" + owner + "||", StringComparison.Ordinal)));
    }

    /// <summary>Captures the existing durable audit keys before one inspected operation.</summary>
    internal async Task<string[]> AuditKeysAsync() => (await RedisAsync("--scan", "--pattern", "projects:read-audit:v1:*").ConfigureAwait(false))
        .Split('\n', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Requires this operation's new durable audit with the exact disclosure class/count.</summary>
    internal async Task VerifyDurableAuditDeltaAsync(string[] before, bool open, int count)
    {
        string[] keys = await AuditKeysAsync().ConfigureAwait(false);
        string[] added = keys.Except(before, StringComparer.Ordinal).ToArray();
        Require(added.Length == 1, "The inspected operation did not persist exactly one new audit.");
        string[] exclusions = ProtectedValues().ToArray();
        foreach (string key in keys)
        {
            SupportedProjectReadStateSnapshot snapshot = await StateAsync(key).ConfigureAwait(false);
            using JsonDocument audit = JsonDocument.Parse(snapshot.Bytes);
            Require(IsSafeAudit(audit.RootElement, _startedAt, DateTimeOffset.UtcNow, exclusions), "Audit metadata violates the closed privacy contract.");
            string surrogate = audit.RootElement.GetProperty("requestSurrogate").GetString()!;
            Require(key == "projects:read-audit:v1:" + surrogate, "Audit key does not match its surrogate.");
            if (key == added[0])
                Require(audit.RootElement.GetProperty("outcome").GetString() == "Inspection"
                    && audit.RootElement.GetProperty("projectCount").GetInt32() == count
                    && audit.RootElement.GetProperty("fieldClass").GetString() == (open ? "ProjectNameDescriptionSetupReferenceLabels" : "ProjectName"),
                    "The inspected operation audit has the wrong disclosure class/count.");
            Require((await RedisAsync("TTL", key).ConfigureAwait(false)).Trim() == "-1", "Required audit must have durable retention without an expiry.");
        }
    }

    /// <summary>Checks metadata values and exclusions without putting protected data in assertion messages.</summary>
    internal static bool IsSafeAudit(JsonElement audit, DateTimeOffset earliest, DateTimeOffset latest, IEnumerable<string> protectedValues)
    {
        if (audit.ValueKind != JsonValueKind.Object || !audit.EnumerateObject().Select(property => property.Name).Order().SequenceEqual(
            new[] { "fieldClass", "outcome", "projectCount", "recordedAt", "requestSurrogate" })) return false;
        JsonElement outcome = audit.GetProperty("outcome"), fields = audit.GetProperty("fieldClass"), surrogate = audit.GetProperty("requestSurrogate"),
            count = audit.GetProperty("projectCount"), recorded = audit.GetProperty("recordedAt");
        if (outcome.ValueKind != JsonValueKind.String || fields.ValueKind != JsonValueKind.String || surrogate.ValueKind != JsonValueKind.String
            || count.ValueKind != JsonValueKind.Number || !count.TryGetInt32(out int total) || total < 0 || total > 200
            || recorded.ValueKind != JsonValueKind.String || !recorded.TryGetDateTimeOffset(out DateTimeOffset at) || at < earliest || at > latest.AddSeconds(5)
            || !Regex.IsMatch(surrogate.GetString()!, "^[0-9A-F]{64}$", RegexOptions.CultureInvariant)) return false;
        bool closed = outcome.GetString() == "Inspection" ? fields.GetString() is "ProjectName" or "ProjectNameDescriptionSetupReferenceLabels"
            : outcome.GetString() == "SafeDenial" && total == 0 && fields.GetString() == "None";
        string body = audit.GetRawText();
        return closed && protectedValues.All(value => !string.IsNullOrEmpty(value) && !body.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<string> ProtectedValues() => new[] { Tenant, ForeignTenant, "actor-1", "foreign-actor", "project-a", "project-b", "folder-project",
        "Synthetic", "Foreign synthetic name", "Owned folder label", "delegation-1", "synthetic-client", "operator-web", "chatbot", "projects.read",
        Setting("UserIssuer"), Setting("UserKeyPath"), Setting("CertificatePassword"), Setting("EventStoreSecret"), Setting("ProjectsSecret"),
        Setting("ChannelToken"), Setting("ReceiptKey"), Setting("OwnerPrivateKey"), Setting("OwnerPublicKey"), HumanToken(Tenant) };

    /// <summary>Proves a fresh metadata-only audit write is denied while ordinary reads remain healthy.</summary>
    internal async Task VerifyAuditWriteFaultAsync()
    {
        await WaitForNonDescriptiveControlAsync(open: false).ConfigureAwait(false);
        await WaitForNonDescriptiveControlAsync(open: true).ConfigureAwait(false);
        string key = "projects:read-audit:v1:" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var value = new { requestSurrogate = key["projects:read-audit:v1:".Length..], outcome = "Inspection", projectCount = 1,
            fieldClass = "ProjectName", recordedAt = DateTimeOffset.UtcNow };
        using HttpResponseMessage write = await _http.PostAsJsonAsync(_endpoints["eventstore-dapr-cli"] + "/v1.0/state/statestore",
            new[] { new { key, value } }, Json, Cancellation).ConfigureAwait(false);
        Require(!write.IsSuccessStatusCode, "The injected fault admitted a new metadata-only audit write.");
        Require(!(await AuditKeysAsync().ConfigureAwait(false)).Contains(key, StringComparer.Ordinal), "A denied audit write became durable.");
    }

    /// <summary>Requires a healthy unaudited control immediately before each inspected refusal.</summary>
    internal async Task VerifyInspectedAuditRefusalAsync(bool open)
    {
        await WaitForNonDescriptiveControlAsync(open).ConfigureAwait(false);
        await SafeDenialAsync(open: open, chatbot: false, inspection: true).ConfigureAwait(false);
    }

    private async Task WaitForNonDescriptiveControlAsync(bool open)
    {
        using CancellationTokenSource budget = CancellationTokenSource.CreateLinkedTokenSource(Cancellation);
        budget.CancelAfter(TimeSpan.FromSeconds(90));
        HashSet<int> statuses = [];
        int attempts = 0;
        try
        {
            while (true)
            {
                attempts++;
                using HttpResponseMessage response = await QueryAsync(open: open, chatbot: false, inspection: false,
                    cancellationToken: budget.Token).ConfigureAwait(false);
                statuses.Add((int)response.StatusCode);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    SubmitQueryResponse? result = await response.Content.ReadFromJsonAsync<SubmitQueryResponse>(Json, budget.Token).ConfigureAwait(false);
                    Require(result is not null && OmitsDescriptiveFields(result.Payload, open), "A non-descriptive control disclosed descriptive metadata.");
                    AdmissionResponseState state = result!.Payload.GetProperty("snapshot").GetProperty("responseState").Deserialize<AdmissionResponseState>(Json);
                    if (state == AdmissionResponseState.Complete)
                    {
                        Require(IsNonDescriptiveControl(result.Payload, open), "A Complete non-descriptive control returned incomplete fixture metadata.");
                        await File.AppendAllTextAsync(Path.Combine(Evidence, "recovery-probes.log"), "audit-storage-non-descriptive-" + (open ? "open" : "list")
                            + ": attempts=" + attempts + ", statuses=" + string.Join(',', statuses.Order()) + Environment.NewLine, budget.Token).ConfigureAwait(false);
                        return;
                    }
                    Require(state is AdmissionResponseState.Partial or AdmissionResponseState.Unavailable, "Non-descriptive recovery returned an unexpected admission state.");
                }
                else Require(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable,
                    "Non-descriptive recovery encountered an unexpected authentication or request failure.");
                // Retry observations with bounded backoff; elapsed time alone never establishes health.
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(2000, 100 * Math.Pow(2, Math.Min(attempts - 1, 5)))), budget.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (budget.IsCancellationRequested && !Cancellation.IsCancellationRequested)
        {
            throw new InvalidOperationException("The active audit fault did not recover a Complete non-descriptive control within its budget.");
        }
    }

    /// <summary>Rejects descriptive fields and requires the complete owned list/open metadata.</summary>
    internal static bool IsNonDescriptiveControl(JsonElement payload, bool open)
    {
        if (!OmitsDescriptiveFields(payload, open)) return false;
        if (open)
        {
            GetProjectResponse? result = payload.Deserialize<GetProjectResponse>(Json);
            return result?.ProjectId == "project-a" && result.Snapshot.ResponseState == AdmissionResponseState.Complete;
        }
        ProjectListPage? listed = payload.Deserialize<ProjectListPage>(Json);
        return listed?.Snapshot.ResponseState == AdmissionResponseState.Complete
            && listed.Items.Select(item => item.ProjectId).SequenceEqual(new[] { "project-a", "project-b" }) && !listed.HasMore;
    }

    private static bool OmitsDescriptiveFields(JsonElement payload, bool open)
    {
        if (payload.ValueKind != JsonValueKind.Object) return false;
        if (open)
        {
            foreach (string field in new[] { "name", "description", "setup", "setupMetadata" })
                if (payload.TryGetProperty(field, out _)) return false;
            return payload.TryGetProperty("references", out JsonElement references) && references.ValueKind == JsonValueKind.Array
                && references.EnumerateArray().All(reference => reference.ValueKind == JsonValueKind.Object && !reference.TryGetProperty("displayName", out _));
        }
        return payload.TryGetProperty("items", out JsonElement items) && items.ValueKind == JsonValueKind.Array
            && items.EnumerateArray().All(item => item.ValueKind == JsonValueKind.Object && !item.TryGetProperty("name", out _));
    }

    /// <summary>Denies only new audit keys, preserving all non-audit keys and existing exact audits.</summary>
    internal async Task DenyNewAuditKeysAsync()
    {
        const string prefix = "projects:read-audit:v1:";
        string[] existing = await AuditKeysAsync().ConfigureAwait(false);
        Require(existing.Length > 0 && existing.All(key => Regex.IsMatch(key, "^projects:read-audit:v1:[0-9A-F]{64}$", RegexOptions.CultureInvariant)),
            "Audit fault requires validated existing audit identities.");
        // Redis glob unions express the exact prefix complement: every shorter key,
        // or a different byte at the first mismatching position. The fixed prefix
        // has no glob metacharacters; [^x] is Redis's verified byte-negation syntax.
        string[] allowed = [.. Enumerable.Range(0, prefix.Length).Select(index => "~" + prefix[..index]),
            .. Enumerable.Range(0, prefix.Length).Select(index => "~" + prefix[..index] + "[^" + prefix[index] + "]*"),
            .. existing.Select(key => "~" + key)];
        await RedisAsync(["ACL", "SETUSER", "default", "resetkeys", .. allowed]).ConfigureAwait(false);
    }

    /// <summary>Restores only the owned disposable storage permission profile.</summary>
    internal Task RestoreAuditStorageAsync() => RedisAsync("ACL", "SETUSER", "default", "~*");

    /// <summary>Requires an actual authorized response after a restored dependency has recovered.</summary>
    internal Task WaitForAuthorizedRecoveryAsync(string fault)
        => ObserveAuthorizedRecoveryAsync(fault, token => QueryAsync(open: true, cancellationToken: token),
            Path.Combine(Evidence, "recovery-probes.log"), TimeSpan.FromSeconds(90), TimeSpan.FromSeconds(1), Cancellation);

    /// <summary>Exercises the same recovery path with injectable request and timing seams for bounded failure controls.</summary>
    internal static async Task ObserveAuthorizedRecoveryAsync(string fault, Func<CancellationToken, Task<HttpResponseMessage>> query,
        string diagnostics, TimeSpan recoveryBudget, TimeSpan retryDelay, CancellationToken cancellation)
    {
        using CancellationTokenSource budget = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        budget.CancelAfter(recoveryBudget);
        HashSet<int> statuses = [];
        int attempts = 0, transportFailures = 0;
        bool recovered = false;
        try
        {
            while (true)
            {
                attempts++;
                try
                {
                    using HttpResponseMessage response = await query(budget.Token).WaitAsync(budget.Token).ConfigureAwait(false);
                    statuses.Add((int)response.StatusCode);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        SubmitQueryResponse? result = await response.Content.ReadFromJsonAsync<SubmitQueryResponse>(Json, budget.Token).WaitAsync(budget.Token).ConfigureAwait(false);
                        GetProjectResponse? opened = result is { Success: true } ? result.Payload.Deserialize<GetProjectResponse>(Json) : null;
                        if (opened?.ProjectId == "project-a" && opened.Name is not null && opened.Snapshot.ResponseState == AdmissionResponseState.Complete)
                        {
                            recovered = true;
                            return;
                        }
                    }
                    else if (response.StatusCode is not (HttpStatusCode.NotFound or HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable))
                        throw new InvalidOperationException("Dependency recovery encountered an unexpected authentication or request failure.");
                }
                catch (HttpRequestException) { transportFailures++; }
                catch (OperationCanceledException) when (!budget.IsCancellationRequested) { transportFailures++; }
                await Task.Delay(retryDelay, budget.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (budget.IsCancellationRequested && !cancellation.IsCancellationRequested)
        {
            throw new InvalidOperationException("The restored " + fault + " dependency did not recover an actual authorized read within its recovery budget.");
        }
        finally
        {
            await File.AppendAllTextAsync(diagnostics, fault + ": attempts=" + attempts
                + ", statuses=" + string.Join(',', statuses.Order()) + ", transportFailures=" + transportFailures + ", authorizedRecovery=" + recovered
                + Environment.NewLine, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>Mutates only an allowlisted resource of the exact owned AppHost through Aspire.</summary>
    internal async Task ResourceAsync(string command, string resource = "folders")
    {
        string launcher = Path.Combine(TestRepositoryRoot.Find(), "tools", "qualification", "run_supported_project_reads.py");
        await ProcessAsync("python3", launcher, "--resource-action", command, "--resource", resource, "--ownership", _endpoints["ownership"]).ConfigureAwait(false);
        if (resource == "projects")
        {
            Dictionary<string, string> refreshed = JsonSerializer.Deserialize<Dictionary<string, string>>(
                await File.ReadAllBytesAsync(Environment.GetEnvironmentVariable("SUPPORTED_PROJECT_READ_ENDPOINTS")!, Cancellation).ConfigureAwait(false))!;
            foreach ((string key, string value) in refreshed) _endpoints[key] = value;
        }
    }

    /// <summary>Reads the actual AggregateActor sequence through the owned Folder sidecar.</summary>
    internal async Task<long> HeadAsync(string tenant, string folder)
    {
        string actorId = Uri.EscapeDataString(tenant + ":folders:" + folder);
        using HttpResponseMessage response = await _http.PostAsJsonAsync(_endpoints["folders-dapr-cli"] + "/v1.0/actors/AggregateActor/" + actorId + "/method/GetCurrentSequenceAsync", new { }, Json, Cancellation).ConfigureAwait(false);
        Require(response.StatusCode == HttpStatusCode.OK, "Actual owner stream-head read failed.");
        return long.Parse(await response.Content.ReadAsStringAsync(Cancellation).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Requires dedicated receivers to reject a workload-shaped assertion signed by public-user trust.</summary>
    internal async Task AssertUserTrustCannotAuthenticateWorkloadAsync()
    {
        foreach (string domain in new[] { "projects", "folders" })
        {
            string assertion = new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
            {
                Issuer = Setting("UserIssuer"), Audience = domain,
                Subject = new ClaimsIdentity([new Claim("azp", "eventstore"), new Claim("eventstore:operation", "domain-service:metadata")]),
                NotBefore = DateTime.UtcNow.AddSeconds(-1), IssuedAt = DateTime.UtcNow, Expires = DateTime.UtcNow.AddSeconds(60),
                SigningCredentials = new(new RsaSecurityKey(_userKey) { KeyId = "synthetic-user" }, SecurityAlgorithms.RsaSha256),
            });
            using HttpRequestMessage request = new(HttpMethod.Post, Dapr(domain, "admin/operational-index-metadata"))
            {
                Content = JsonContent.Create(new AdminOperationalIndexMetadata.Request([domain]) { AppId = domain, ServiceVersion = "v1" }, options: Json),
            };
            request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
            using HttpResponseMessage refused = await _http.SendAsync(request, Cancellation).ConfigureAwait(false);
            Require(refused.StatusCode == HttpStatusCode.Unauthorized, "Public-user issuer authenticated at a dedicated workload receiver.");
        }
    }

    private async Task CommandAsync(string tenant, string domain, string aggregate, string type, object payload, string actorTenant)
    {
        string messageId = Unique();
        var command = new SubmitCommandRequest(messageId, tenant, domain, aggregate, type, JsonSerializer.SerializeToElement(payload, Json), messageId,
            new Dictionary<string, string> { ["taskId"] = messageId });
        string token = HumanToken(actorTenant, chatbot: false, administrator: true);
        using HttpResponseMessage response = await SendHumanAsync("api/v1/commands", command, token).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            string diagnosticPath = Path.Combine(Path.GetDirectoryName(Setting("CertificatePath"))!, "command-diagnostic.json");
            await File.WriteAllBytesAsync(diagnosticPath, await response.Content.ReadAsByteArrayAsync(Cancellation).ConfigureAwait(false), Cancellation).ConfigureAwait(false);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(diagnosticPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            throw new InvalidOperationException("Supported fixture command refused: " + type + " (HTTP " + (int)response.StatusCode + ").");
        }
        for (int attempt = 0; attempt < 240; attempt++)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, _endpoints["eventstore"] + "/api/v1/commands/status/" + messageId);
            request.Headers.Authorization = new("Bearer", token);
            using HttpResponseMessage status = await _http.SendAsync(request, Cancellation).ConfigureAwait(false);
            if (status.StatusCode == HttpStatusCode.OK)
            {
                using JsonDocument body = JsonDocument.Parse(await status.Content.ReadAsByteArrayAsync(Cancellation).ConfigureAwait(false));
                string? outcome = body.RootElement.GetProperty("status").GetString();
                if (outcome == "Completed")
                {
                    await File.AppendAllTextAsync(Path.Combine(Evidence, "seed-progress.log"), "Completed " + domain + " " + type + Environment.NewLine, Cancellation).ConfigureAwait(false);
                    return;
                }
                Require(outcome is not ("Rejected" or "TimedOut" or "PublishFailed"), "Fixture command failed: " + type + " (" + outcome + ").");
            }
            await Task.Delay(250, Cancellation).ConfigureAwait(false);
        }
        throw new InvalidOperationException("Fixture command did not reach durable completion: " + type);
    }

    private async Task<StreamReadPage> StreamAsync(string tenant, string domain, string aggregate, string actorTenant)
    {
        using HttpResponseMessage response = await SendHumanAsync("api/v1/streams/read", new StreamReadRequest(tenant, domain, aggregate), HumanToken(actorTenant, chatbot: false, administrator: true)).ConfigureAwait(false);
        Require(response.StatusCode == HttpStatusCode.OK, "Supported source-stream read failed.");
        return (await response.Content.ReadFromJsonAsync<StreamReadPage>(Json, Cancellation).ConfigureAwait(false))!;
    }

    private async Task ProjectPersistedStreamAsync(string tenant, string project)
    {
        StreamReadPage stream = await StreamAsync(tenant, "projects", project, tenant).ConfigureAwait(false);
        ProjectionRequest mapped = MapProjectStream(stream, tenant, project);
        using JsonDocument metadata = await MetadataAsync("projects").ConfigureAwait(false);
        var request = new ProjectionDispatchRequest(mapped, ["project-reads"], Unique(), metadata.RootElement.GetProperty("catalogFingerprint").GetString()!);
        _seedRequests.Add((tenant, project), request);
        _seedStreams.Add((tenant, project), stream);
        using HttpResponseMessage response = await InvokeWorkloadAsync("projects", "project/v2", request, "domain-service:project").ConfigureAwait(false);
        Require(response.StatusCode == HttpStatusCode.OK, "Supported persisted projection endpoint refused seeded source events.");
        ProjectionDispatchResponse? result = await response.Content.ReadFromJsonAsync<ProjectionDispatchResponse>(Json, Cancellation).ConfigureAwait(false);
        Require(result!.Outcomes.All(item => item.Status == ProjectionDispatchStatus.Completed), "Persisted Project projection did not complete.");
    }

    /// <summary>Requires a complete authenticated source prefix using the SDK paging contract and exact source head.</summary>
    internal static bool IsCompleteProjectStream(StreamReadRequest request, StreamReadPage stream)
    {
        try
        {
            long last = StreamReadPageValidator.ValidateAndGetNextSequence(request, stream);
            return request.Domain == "projects" && request.FromSequence == 0 && request.ToSequence is null
                && stream.Metadata.LatestSequence >= 2 && last == stream.Metadata.LatestSequence
                && !stream.Metadata.IsTruncated && stream.Metadata.NextContinuationToken is null
                && stream.Events.Count == last
                && stream.Events.Select((item, index) => item.SequenceNumber == index + 1 && item.GlobalPosition > 0 && item.Timestamp != default).All(value => value)
                && stream.Events.Zip(stream.Events.Skip(1)).All(pair => pair.First.GlobalPosition < pair.Second.GlobalPosition)
                && stream.Events.Select(item => item.MessageId).Distinct(StringComparer.Ordinal).Count() == stream.Events.Count;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { return false; }
    }

    private static ProjectionRequest MapProjectStream(StreamReadPage stream, string tenant, string project)
    {
        Require(IsCompleteProjectStream(new(tenant, "projects", project), stream), "The authenticated Project stream is missing, truncated or inconsistent with its observed head.");
        return new(tenant, "projects", project, stream.Events.Select(item => new ProjectionEventDto(item.EventTypeName,
            item.Payload, item.SerializationFormat, item.SequenceNumber, item.Timestamp, item.CorrelationId ?? item.MessageId,
            item.MessageId, item.UserId, item.GlobalPosition) { StoredPayloadVersion = item.PayloadVersion }).ToArray());
    }

    private async Task<string[]> OwnedDomainKeyCensusAsync()
    {
        string[] persisted = (await RedisAsync("--scan").ConfigureAwait(false)).Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(key => IsOwnedDomainKey(key, Tenant, ForeignTenant)).ToArray();
        string[] required = new[] { Tenant, ForeignTenant }.SelectMany(tenant => new[]
        {
            ProjectReadModelKeys.Index(tenant), ProjectReadModelKeys.Detail(tenant, "project-a"), ProjectReadModelKeys.Detail(tenant, "project-b"),
            "projects:tenant-access:" + tenant, "projection:folders:tenant-access:" + tenant, "projection:folders:organization-permissions:" + tenant,
            "projection:folders:folder-permissions:" + tenant + ":folder-project-a", "projection:folders:folder-permissions:" + tenant + ":folder-project-b",
        }).ToArray();
        return persisted.Concat(required).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    /// <summary>Redelivers each retained seed request and the same source events under a distinct dispatch identity.</summary>
    internal async Task VerifyDuplicateDeliveryAsync()
    {
        _ownedDomainKeyCensus = await OwnedDomainKeyCensusAsync().ConfigureAwait(false);
        await SaveReplayEvidenceAsync().ConfigureAwait(false);
        SupportedProjectReadStateSnapshot[] before = await SnapshotAsync().ConfigureAwait(false);
        Dictionary<string, object> expected = await ReadBothTenantTruthAsync().ConfigureAwait(false);
        foreach ((string tenant, string project) in ProjectScopes())
        {
            ProjectionDispatchRequest retained = _seedRequests[(tenant, project)];
            StreamReadPage source = await ObserveRetainedSourceAsync(tenant, project).ConfigureAwait(false);
            await RecordReplayAsync(new
            {
                kind = "source", tenantSha256 = Digest(tenant), projectSha256 = Digest(project), sourceSha256 = Digest(source),
                requestEventsSha256 = Digest(retained.Request.Events), catalogSha256 = Digest(retained.CatalogFingerprint),
                retainedDispatchIdSha256 = Digest(retained.DispatchId), detailKeySha256 = Digest(ProjectReadModelKeys.Detail(tenant, project)),
                indexKeySha256 = Digest(ProjectReadModelKeys.Index(tenant)), observedHead = source.Metadata.LatestSequence,
                eventCount = source.Events.Count, complete = true,
                events = source.Events.Select((item, index) => new { sequence = item.SequenceNumber, globalPosition = item.GlobalPosition,
                    timestamp = item.Timestamp, sourceEventSha256 = Digest(item), projectionEventSha256 = Digest(retained.Request.Events[index]) }),
            }).ConfigureAwait(false);
            await RequireProjectionAttemptAsync("duplicate", "retained", "project/v2", retained, ProjectionDispatchStatus.AlreadyCompleted).ConfigureAwait(false);
            await RequireProjectionAttemptAsync("duplicate", "fresh", "project/v2", retained with { DispatchId = Unique() }, ProjectionDispatchStatus.AlreadyCompleted).ConfigureAwait(false);
        }
        await ObserveReplayStateAsync("duplicate", before, []).ConfigureAwait(false);
        await VerifyReadTruthAsync("duplicate", expected).ConfigureAwait(false);
        await ObserveReplayStateAsync("duplicate-reads", before, []).ConfigureAwait(false);
        _duplicateVerified = true;
        await SaveReplayEvidenceAsync().ConfigureAwait(false);
    }

    /// <summary>Exercises protected refusal controls, complete replay, cursor invalidation and subsequent read-only state.</summary>
    internal async Task VerifyFullReplayAsync()
    {
        Require(_duplicateVerified, "Duplicate delivery evidence must precede rebuild qualification.");
        SupportedProjectReadStateSnapshot[] before = await SnapshotAsync().ConfigureAwait(false);
        Dictionary<string, object> expected = await ReadBothTenantTruthAsync().ConfigureAwait(false);
        foreach ((string tenant, string project) in ProjectScopes())
        {
            StreamReadPage source = await ObserveRetainedSourceAsync(tenant, project).ConfigureAwait(false);
            ProjectionDispatchRequest valid = _seedRequests[(tenant, project)] with
            {
                DispatchId = Unique(), Request = MapProjectStream(source, tenant, project) with { RebuildSourceHeadSequence = source.Metadata.LatestSequence },
            };
            await RequireProjectionAttemptAsync("invalid", "truncated", "project/rebuild/v1", valid with
            {
                Request = valid.Request with { Events = valid.Request.Events[..^1] },
            }, ProjectionDispatchStatus.Failed).ConfigureAwait(false);
            await RequireProjectionAttemptAsync("invalid", "mismatched-head", "project/rebuild/v1", valid with
            {
                DispatchId = Unique(), Request = valid.Request with { RebuildSourceHeadSequence = source.Events[^2].SequenceNumber },
            }, ProjectionDispatchStatus.Failed).ConfigureAwait(false);
        }
        await ObserveReplayStateAsync("invalid", before, []).ConfigureAwait(false);
        await VerifyReadTruthAsync("invalid", expected).ConfigureAwait(false);
        ProjectionDispatchRequest authRequest = _seedRequests[(Tenant, "project-a")];
        foreach (string route in new[] { "project/v2", "project/rebuild/v1" })
            foreach (string credential in new[] { "anonymous", "human-bearer", "public-user-workload", "wrong-operation" })
                await RequireProjectionAttemptAsync("authentication", credential, route, authRequest with
                {
                    DispatchId = Unique(), Request = authRequest.Request with { RebuildSourceHeadSequence = _seedStreams[(Tenant, "project-a")].Metadata.LatestSequence },
                }, null, credential).ConfigureAwait(false);
        await ObserveReplayStateAsync("authentication", before, []).ConfigureAwait(false);
        await VerifyReadTruthAsync("authentication", expected).ConfigureAwait(false);
        await ObserveReplayStateAsync("refusal-reads", before, []).ConfigureAwait(false);

        DateTimeOffset cursorRequestedAt = DateTimeOffset.UtcNow;
        ProjectListPage cursorPage = await ReadAsync<ProjectListPage>(list: new(PageSize: 1)).ConfigureAwait(false);
        await RecordReplayAsync(new { kind = "cursor-control", observation = CreateCursorControlEvidence(cursorPage) }).ConfigureAwait(false);
        Require(cursorPage.Snapshot.ResponseState == AdmissionResponseState.Complete && cursorPage.Items.Count == 1
            && cursorPage.HasMore && !string.IsNullOrWhiteSpace(cursorPage.Cursor), "A fresh Complete pre-rebuild cursor page was not returned; inspect its sanitized admission observation.");
        ProjectListPage accepted = await ReadAsync<ProjectListPage>(list: new(Cursor: cursorPage.Cursor, PageSize: 1)).ConfigureAwait(false);
        Require(SameStableReadMetadata(expected[Tenant + ":next"], accepted), "The cursor was not accepted at the expected boundary immediately before rebuild.");
        DateTimeOffset acceptedAt = DateTimeOffset.UtcNow;
        SupportedProjectReadStateSnapshot[] previous = before;
        foreach ((string tenant, string project) in ProjectScopes())
        {
            StreamReadPage source = await ObserveRetainedSourceAsync(tenant, project).ConfigureAwait(false);
            ProjectionDispatchRequest rebuild = _seedRequests[(tenant, project)] with
            {
                DispatchId = Unique(), Request = MapProjectStream(source, tenant, project) with { RebuildSourceHeadSequence = source.Metadata.LatestSequence },
            };
            await RequireProjectionAttemptAsync("rebuild", "full-prefix", "project/rebuild/v1", rebuild, ProjectionDispatchStatus.Completed).ConfigureAwait(false);
            previous = await ObserveReplayStateAsync("rebuild:" + Digest(tenant) + ":" + Digest(project), previous,
                [ProjectReadModelKeys.Detail(tenant, project), ProjectReadModelKeys.Index(tenant)]).ConfigureAwait(false);
        }
        string[] projections = ProjectScopes().Select(scope => ProjectReadModelKeys.Detail(scope.Tenant, scope.Project))
            .Concat(new[] { ProjectReadModelKeys.Index(Tenant), ProjectReadModelKeys.Index(ForeignTenant) }).ToArray();
        JsonElement aggregate = CreateReplayStateEvidence(before, previous, [Tenant, ForeignTenant], projections, _ownedDomainKeyCensus);
        await RecordReplayAsync(new { kind = "state", phase = "full-rebuild", comparison = aggregate }).ConfigureAwait(false);
        Require(aggregate.GetProperty("valid").GetBoolean(), "Full replay changed domain bytes, watermarks or unexpected ETags.");
        // This is the new baseline: all intentional projection writes have already completed.
        SupportedProjectReadStateSnapshot[] readBaseline = previous;
        JsonElement rejection = await ObserveRebuildCursorInvalidationAsync(token => QueryAsync(list: new(Cursor: cursorPage.Cursor, PageSize: 1), cancellationToken: token),
            (observation, token) => RecordReplayAsync(new { kind = "cursor-probe", observation }, token), cursorRequestedAt, TimeSpan.FromSeconds(90), Cancellation).ConfigureAwait(false);
        DateTimeOffset rejectedAt = DateTimeOffset.UtcNow;
        await RecordReplayAsync(new
        {
            kind = "cursor", tenantSha256 = Digest(Tenant), indexKeySha256 = Digest(ProjectReadModelKeys.Index(Tenant)), acceptedHttpStatus = 200, rejectedHttpStatus = 400,
            requestedAt = cursorRequestedAt, acceptedAt, rejectedAt,
            observerStartedAt = rejection.GetProperty("observerStartedAt"), observerDeadline = rejection.GetProperty("observerDeadline"),
            rejectionProblem = rejection.GetProperty("problem"), withinLifetime = CursorIsWithinObservedLifetime(cursorRequestedAt, rejectedAt),
            beforeIndexETagSha256 = Digest(before.Single(item => item.Key == ProjectReadModelKeys.Index(Tenant)).ETag),
            afterIndexETagSha256 = Digest(previous.Single(item => item.Key == ProjectReadModelKeys.Index(Tenant)).ETag),
        }).ConfigureAwait(false);
        Require(CursorIsWithinObservedLifetime(cursorRequestedAt, rejectedAt),
            "The index ETag change did not invalidate the cursor within its observed lifetime.");
        await VerifyReadTruthAsync("rebuild", expected).ConfigureAwait(false);
        await ObserveReplayStateAsync("post-rebuild-reads", readBaseline, []).ConfigureAwait(false);
        _replayVerified = true;
        await SaveReplayEvidenceAsync().ConfigureAwait(false);
    }

    /// <summary>Observes a definitive refusal without mistaking honest dependency unavailability for cursor acceptance.</summary>
    internal static async Task<JsonElement> ObserveRebuildCursorInvalidationAsync(Func<CancellationToken, Task<HttpResponseMessage>> query,
        Func<JsonElement, CancellationToken, Task> record, DateTimeOffset requestedAt, TimeSpan duration, CancellationToken cancellation)
    {
        Require(duration > TimeSpan.Zero && duration <= TimeSpan.FromSeconds(90), "The cursor observer requires a positive budget no longer than ninety seconds.");
        DateTimeOffset observerStartedAt = DateTimeOffset.UtcNow, observerDeadline = observerStartedAt + duration;
        using CancellationTokenSource budget = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        budget.CancelAfter(duration);
        void CheckBudget()
        {
            cancellation.ThrowIfCancellationRequested();
            if (budget.IsCancellationRequested || DateTimeOffset.UtcNow >= observerDeadline)
                throw new InvalidOperationException("Old-cursor observation ended without a definitive refusal inside its observer budget.");
        }
        int attempt = 0;
        while (true)
        {
            attempt++;
            HttpStatusCode? status = null;
            JsonElement? admission = null, problem = null;
            bool unavailable = false;
            string? failureType = null;
            Exception? failure = null;
            try
            {
                using HttpResponseMessage response = await query(budget.Token).WaitAsync(budget.Token).ConfigureAwait(false);
                status = response.StatusCode;
                if (status == HttpStatusCode.BadRequest)
                {
                    using JsonDocument body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(budget.Token).ConfigureAwait(false), cancellationToken: budget.Token).ConfigureAwait(false);
                    JsonElement root = body.RootElement;
                    Require(response.Content.Headers.ContentType?.MediaType == "application/problem+json"
                        && root.ValueKind == JsonValueKind.Object && root.TryGetProperty("status", out JsonElement code) && code.ValueKind == JsonValueKind.Number && code.GetInt32() == 400
                        && root.TryGetProperty("reasonCode", out JsonElement reasonCode) && reasonCode.ValueKind == JsonValueKind.String && reasonCode.GetString() == QueryProblemReasonCodes.InvalidPage
                        && root.TryGetProperty("reason", out JsonElement reason) && reason.ValueKind == JsonValueKind.String && reason.GetString() == QueryAdapterFailureReason.InvalidCursor,
                        "The old-cursor refusal lacks the actual safe platform invalid-cursor Problem Details classification.");
                    problem = JsonSerializer.SerializeToElement(new { status = root.GetProperty("status").GetInt32(), reasonCode = root.GetProperty("reasonCode").GetString(), reason = root.GetProperty("reason").GetString() }, Json);
                }
                else if (status == HttpStatusCode.OK)
                {
                    SubmitQueryResponse? submitted = await response.Content.ReadFromJsonAsync<SubmitQueryResponse>(Json, budget.Token).ConfigureAwait(false);
                    ProjectListPage? page = submitted?.Payload.Deserialize<ProjectListPage>(Json);
                    Require(page is not null, "The old-cursor response has no supported list payload.");
                    admission = CreateCursorControlEvidence(page!);
                    unavailable = page!.Snapshot.ResponseState == AdmissionResponseState.Unavailable && page.Items.Count == 0
                        && page.Cursor is null && !page.HasMore && page.Snapshot.AsOf != default && page.Snapshot.ProjectVersion == 0
                        && page.Snapshot.Components.Any(component => !component.Included
                            && component.Freshness is Hexalith.Projects.Contracts.Models.EvidenceFreshnessState.Unavailable or Hexalith.Projects.Contracts.Models.EvidenceFreshnessState.Stale);
                }
            }
            catch (Exception exception) { failure = exception; failureType = exception.GetType().Name; }
            DateTimeOffset observedAt = DateTimeOffset.UtcNow;
            JsonElement observation = JsonSerializer.SerializeToElement(new
            {
                attempt, httpStatus = status is null ? (int?)null : (int)status, admission, problem, honestEmptyUnavailable = unavailable,
                observerStartedAt, observerDeadline, observedAt, withinLifetime = CursorIsWithinObservedLifetime(requestedAt, observedAt), failureType,
            }, Json);
            try { await record(observation, budget.Token).WaitAsync(budget.Token).ConfigureAwait(false); }
            catch (OperationCanceledException)
            {
                cancellation.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Old-cursor evidence recording exceeded its observer budget.");
            }
            CheckBudget();
            if (failure is not null) throw new InvalidOperationException("Old-cursor observation failed before a definitive refusal; inspect sanitized attempts.");
            Require(CursorIsWithinObservedLifetime(requestedAt, DateTimeOffset.UtcNow), "Cursor expiry cannot qualify as rebuild invalidation.");
            if (status == HttpStatusCode.BadRequest && problem is not null) return observation;
            Require(status == HttpStatusCode.OK && unavailable, "The rebuilt index admitted a continuation or returned an unexpected refusal; inspect sanitized attempts.");
            try { await Task.Delay(TimeSpan.FromMilliseconds(200), budget.Token).ConfigureAwait(false); }
            catch (OperationCanceledException)
            {
                cancellation.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Old-cursor observation ended without a definitive refusal.");
            }
        }
    }

    /// <summary>Retains only bounded admission metadata for the actual cursor page, including unsuccessful responses.</summary>
    internal static JsonElement CreateCursorControlEvidence(ProjectListPage page)
        => JsonSerializer.SerializeToElement(new
        {
            httpStatus = 200, responseState = Enum.IsDefined(page.Snapshot.ResponseState) ? page.Snapshot.ResponseState.ToString() : "Undefined",
            asOfPresent = page.Snapshot.AsOf != default, projectVersion = page.Snapshot.ProjectVersion,
            itemCount = page.Items.Count, page.HasMore, cursorPresent = !string.IsNullOrWhiteSpace(page.Cursor),
            components = page.Snapshot.Components.Select(component => new
            {
                name = component.Name is "Enumeration" or "FolderVisibility" or "Authorization" or "Project" or "Folder" or "Setup" or "References" ? component.Name : "Other",
                nameSha256 = Digest(component.Name), component.Included,
                freshness = Enum.IsDefined(component.Freshness) ? component.Freshness.ToString() : "Undefined",
                reason = component.Reason is "Current" or "ProjectionUnavailable" or "AuthorizationUnavailable" or "FolderUnavailable" or "SetupUnavailable" or "OwnerReadUnverified" ? component.Reason : "Other",
                reasonSha256 = Digest(component.Reason), lastVerifiedAtPresent = component.LastVerifiedAt is not null,
            }),
        }, Json);

    private IEnumerable<(string Tenant, string Project)> ProjectScopes()
        => new[] { Tenant, ForeignTenant }.SelectMany(tenant => new[] { "project-a", "project-b" }.Select(project => (tenant, project)));

    private async Task<StreamReadPage> ObserveRetainedSourceAsync(string tenant, string project)
    {
        StreamReadPage source = await StreamAsync(tenant, "projects", project, tenant).ConfigureAwait(false);
        _ = MapProjectStream(source, tenant, project);
        using JsonDocument metadata = await MetadataAsync("projects").ConfigureAwait(false);
        Require(metadata.RootElement.GetProperty("catalogFingerprint").GetString() == _seedRequests[(tenant, project)].CatalogFingerprint,
            "The current projection catalog differs from the retained seeded request.");
        Require(Digest(source) == Digest(_seedStreams[(tenant, project)])
            && Digest(MapProjectStream(source, tenant, project).Events) == Digest(_seedRequests[(tenant, project)].Request.Events),
            "The actual persisted source no longer matches the retained complete seed stream and projection mapping.");
        return source;
    }

    private async Task RequireProjectionAttemptAsync(string kind, string scenario, string route, ProjectionDispatchRequest request,
        ProjectionDispatchStatus? expected, string? credential = null)
    {
        HttpStatusCode? status = null;
        ProjectionDispatchResponse? result = null;
        string? failureType = null;
        try
        {
            using HttpResponseMessage response = credential is null
                ? await InvokeWorkloadAsync("projects", route, request, "domain-service:project").ConfigureAwait(false)
                : await InvokeProjectionCredentialAsync(route, request, credential).ConfigureAwait(false);
            status = response.StatusCode;
            if (status == HttpStatusCode.OK) result = await response.Content.ReadFromJsonAsync<ProjectionDispatchResponse>(Json, Cancellation).ConfigureAwait(false);
        }
        catch (Exception failure) { failureType = failure.GetType().Name; }
        StreamReadPage source = _seedStreams[(request.Request.TenantId, request.Request.AggregateId)];
        await RecordReplayAsync(new
        {
            kind, scenario, route, tenantSha256 = Digest(request.Request.TenantId), projectSha256 = Digest(request.Request.AggregateId),
            sourceSha256 = Digest(source), requestEventsSha256 = Digest(request.Request.Events),
            requestSha256 = Digest(request), catalogSha256 = Digest(request.CatalogFingerprint), dispatchIdSha256 = Digest(request.DispatchId),
            retainedDispatchIdSha256 = Digest(_seedRequests[(source.Tenant, source.AggregateId!)].DispatchId),
            observedHead = source.Metadata.LatestSequence, requestHead = request.Request.RebuildSourceHeadSequence,
            eventCount = request.Request.Events.Length, httpStatus = status is null ? (int?)null : (int)status,
            protocolVersion = result?.Version,
            outcomes = result?.Outcomes.Select(item => new { projectionType = item.ProjectionType, status = item.Status.ToString() }), failureType,
        }).ConfigureAwait(false);
        Require(failureType is null, "A protected projection attempt failed before its HTTP outcome could be observed.");
        if (credential is not null)
            Require(status == (credential == "wrong-operation" ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized), "A projection route admitted an invalid credential or returned an unexpected refusal.");
        else
            Require(status == HttpStatusCode.OK && result?.Version == ProjectionDispatchProtocol.Version
                && result.Outcomes.Count == 1 && result.Outcomes[0].ProjectionType == "project-reads" && result.Outcomes[0].Status == expected,
                "A protected projection route did not return its required explicit durable outcome.");
    }

    private async Task<HttpResponseMessage> InvokeProjectionCredentialAsync(string route, ProjectionDispatchRequest payload, string credential)
    {
        if (credential == "wrong-operation") return await InvokeWorkloadAsync("projects", route, payload, "domain-service:metadata").ConfigureAwait(false);
        using HttpRequestMessage request = new(HttpMethod.Post, Dapr("projects", route)) { Content = JsonContent.Create(payload, options: Json) };
        if (credential == "human-bearer") request.Headers.Authorization = new("Bearer", HumanToken(Tenant));
        if (credential == "public-user-workload")
        {
            string assertion = new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
            {
                Issuer = Setting("UserIssuer"), Audience = "projects", Subject = new ClaimsIdentity([new Claim("sub", "eventstore"),
                    new Claim("azp", "eventstore"), new Claim("eventstore:operation", "domain-service:project")]),
                NotBefore = DateTime.UtcNow.AddSeconds(-1), IssuedAt = DateTime.UtcNow, Expires = DateTime.UtcNow.AddSeconds(60),
                SigningCredentials = new(new RsaSecurityKey(_userKey) { KeyId = "synthetic-user" }, SecurityAlgorithms.RsaSha256),
            });
            request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
        }
        return await _http.SendAsync(request, Cancellation).ConfigureAwait(false);
    }

    private async Task<Dictionary<string, object>> ReadBothTenantTruthAsync()
    {
        Dictionary<string, object> observations = [];
        foreach (string tenant in new[] { Tenant, ForeignTenant })
        {
            ProjectListPage first = await ReadAsync<ProjectListPage>(tenant: tenant, tenantActor: true, list: new(PageSize: 1)).ConfigureAwait(false);
            Require(first.Snapshot.ResponseState == AdmissionResponseState.Complete && !string.IsNullOrWhiteSpace(first.Cursor) && first.HasMore && first.Items.Select(item => item.ProjectId).SequenceEqual(["project-a"]),
                "Replay first-page ordering or continuation is incomplete.");
            ProjectListPage next = await ReadAsync<ProjectListPage>(tenant: tenant, tenantActor: true, list: new(Cursor: first.Cursor, PageSize: 1)).ConfigureAwait(false);
            Require(next.Snapshot.ResponseState == AdmissionResponseState.Complete && !next.HasMore && next.Cursor is null && first.Items.Concat(next.Items).Select(item => item.ProjectId).SequenceEqual(["project-a", "project-b"]),
                "Fresh replay paging duplicated or omitted a Project.");
            observations.Add(tenant + ":first", first);
            observations.Add(tenant + ":next", next);
            foreach (ProjectListRow row in first.Items.Concat(next.Items))
            {
                GetProjectResponse opened = await ReadAsync<GetProjectResponse>(tenant: tenant, tenantActor: true, project: row.ProjectId, open: true).ConfigureAwait(false);
                Require(opened.ProjectId == row.ProjectId && opened.Name is not null && opened.Name == row.Name
                    && opened.Snapshot.ProjectVersion == row.Snapshot.ProjectVersion && opened.LifecycleState == row.LifecycleState
                    && opened.Snapshot.ResponseState == AdmissionResponseState.Complete && row.Snapshot.ResponseState == AdmissionResponseState.Complete,
                    "List and open replay metadata or versions disagree.");
                observations.Add(tenant + ":" + row.ProjectId, opened);
            }
        }
        return observations;
    }

    private async Task VerifyReadTruthAsync(string phase, Dictionary<string, object> expected)
    {
        Dictionary<string, object> observed = await ReadBothTenantTruthAsync().ConfigureAwait(false);
        bool same = expected.Count == 8 && observed.Count == 8 && expected.All(pair => observed.TryGetValue(pair.Key, out object? value) && SameStableReadMetadata(pair.Value, value));
        if (phase != "rebuild")
            foreach (string tenant in new[] { Tenant, ForeignTenant })
            {
                ProjectListPage original = (ProjectListPage)expected[tenant + ":first"];
                ProjectListPage continued = await ReadAsync<ProjectListPage>(tenant: tenant, tenantActor: true,
                    list: new(Cursor: original.Cursor, PageSize: 1)).ConfigureAwait(false);
                Require(SameStableReadMetadata(expected[tenant + ":next"], continued), "The retained continuation changed after a no-write projection attempt.");
            }
        string absent = await SafeDenialAsync(project: "absent").ConfigureAwait(false);
        bool denied = await SafeDenialAsync(tenant: ForeignTenant).ConfigureAwait(false) == absent
            && await SafeDenialAsync(tenant: ForeignTenant, open: false).ConfigureAwait(false) == absent;
        await RecordReplayAsync(new { kind = "reads", phase, bothTenantsCompared = true, readCount = observed.Count,
            stableMetadataAgrees = same, retainedContinuationsAccepted = phase != "rebuild" ? true : (bool?)null, freshPagingWithoutGapsOrDuplicates = true, listOpenAgree = true, foreignAndAbsentDenials = denied }).ConfigureAwait(false);
        Require(same && denied, "Supported list/open truth or safe denials changed after projection attempts.");
    }

    private async Task<SupportedProjectReadStateSnapshot[]> ObserveReplayStateAsync(string phase, SupportedProjectReadStateSnapshot[] before, string[] changedETagKeys)
    {
        SupportedProjectReadStateSnapshot[]? after = null;
        string? failureType = null;
        try { after = await SnapshotAsync(allowIncomplete: true).ConfigureAwait(false); }
        catch (Exception failure) { failureType = failure.GetType().Name; }
        JsonElement comparison = CreateReplayStateEvidence(before, after, [Tenant, ForeignTenant], changedETagKeys, _ownedDomainKeyCensus);
        await RecordReplayAsync(new { kind = "state", phase, comparison, failureType }).ConfigureAwait(false);
        Require(comparison.GetProperty("valid").GetBoolean(), "Replay state is missing, unreadable, or differs outside the expected projection ETags.");
        return after!;
    }

    /// <summary>Independently compares complete persisted fingerprints, all six exact watermarks and the explicit ETag allowlist.</summary>
    internal static JsonElement CreateReplayStateEvidence(SupportedProjectReadStateSnapshot[] before, SupportedProjectReadStateSnapshot[]? after, string[] tenants, string[] changedETagKeys, string[] domainKeyCensus)
    {
        bool complete = domainKeyCensus.Length > 0 && domainKeyCensus.Distinct(StringComparer.Ordinal).Count() == domainKeyCensus.Length
            && before.Select(item => item.Key).Order(StringComparer.Ordinal).SequenceEqual(domainKeyCensus.Order(StringComparer.Ordinal), StringComparer.Ordinal)
            && after is not null && after.Select(item => item.Key).Order(StringComparer.Ordinal).SequenceEqual(domainKeyCensus.Order(StringComparer.Ordinal), StringComparer.Ordinal)
            && before.All(item => item.Bytes.Length > 0 && item.ETag.Length > 0) && after.All(item => item.Bytes.Length > 0 && item.ETag.Length > 0)
            && before.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count() == before.Length
            && after.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count() == after.Length;
        bool sameBytes = complete && before.OrderBy(item => item.Key, StringComparer.Ordinal).Zip(after!.OrderBy(item => item.Key, StringComparer.Ordinal))
            .All(pair => pair.First.Key == pair.Second.Key && pair.First.Bytes.AsSpan().SequenceEqual(pair.Second.Bytes));
        string[] changed = after is null ? [] : before.Join(after, item => item.Key, item => item.Key, (old, current) => (old, current))
            .Where(pair => pair.old.ETag != pair.current.ETag).Select(pair => pair.old.Key).Order(StringComparer.Ordinal).ToArray();
        bool onlyExpected = sameBytes && changed.SequenceEqual(changedETagKeys.Order(StringComparer.Ordinal), StringComparer.Ordinal);
        bool watermarksUsable = tenants.Length == 2 && tenants.Distinct(StringComparer.Ordinal).Count() == 2;
        object[] Watermarks(SupportedProjectReadStateSnapshot[]? values) => tenants.SelectMany(tenant =>
            new[] { ProjectReadModelKeys.Index(tenant), ProjectReadModelKeys.Detail(tenant, "project-a"), ProjectReadModelKeys.Detail(tenant, "project-b") }.Select(key =>
            {
                long? position = null;
                SupportedProjectReadStateSnapshot[]? matches = values?.Where(item => item.Key == key).ToArray();
                if (matches is { Length: 1 } && matches[0].Bytes.Length > 0)
                {
                    try
                    {
                        using JsonDocument model = JsonDocument.Parse(matches[0].Bytes);
                        if (model.RootElement.ValueKind == JsonValueKind.Object && model.RootElement.TryGetProperty("globalPosition", out JsonElement value)
                            && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long observed) && observed > 0) position = observed;
                    }
                    catch (JsonException) { }
                }
                watermarksUsable &= position is > 0;
                return (object)new { tenantSha256 = Digest(tenant), keySha256 = Digest(key), globalPosition = position, status = position is > 0 ? "observed" : "missing-or-unreadable" };
            })).ToArray();
        object[] beforeWatermarks = Watermarks(before), afterWatermarks = Watermarks(after);
        bool sameWatermarks = watermarksUsable && JsonSerializer.Serialize(beforeWatermarks, Json) == JsonSerializer.Serialize(afterWatermarks, Json);
        object[]? Fingerprints(SupportedProjectReadStateSnapshot[]? values) => values?.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item =>
            (object)new { keySha256 = Digest(item.Key), bytesSha256 = Convert.ToHexString(SHA256.HashData(item.Bytes)), etagSha256 = Digest(item.ETag),
                status = item.Bytes.Length > 0 && item.ETag.Length > 0 ? "observed" : "missing" }).ToArray();
        return JsonSerializer.SerializeToElement(new
        {
            valid = sameBytes && onlyExpected && sameWatermarks, bothTenantsCompared = watermarksUsable,
            domainKeysAndBytesUnchanged = sameBytes, exactWatermarksUnchanged = sameWatermarks, onlyExpectedETagsChanged = onlyExpected,
            expectedChangedETagKeys = changedETagKeys.Select(Digest).Order(StringComparer.Ordinal), changedETagKeys = changed.Select(Digest).Order(StringComparer.Ordinal),
            before = Fingerprints(before), after = Fingerprints(after), beforeWatermarks, afterWatermarks,
        }, Json);
    }

    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
    private static string Digest<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, Json)));
    private async Task RecordReplayAsync(object observation, CancellationToken cancellation = default)
    {
        _replayObservations.Add(JsonSerializer.SerializeToElement(observation, Json));
        await SaveReplayEvidenceAsync(cancellation).ConfigureAwait(false);
    }

    private Task SaveReplayEvidenceAsync(CancellationToken cancellation = default) => File.WriteAllTextAsync(Path.Combine(Evidence, "projection-replay.json"),
        JsonSerializer.Serialize(new { version = 1, duplicateVerified = _duplicateVerified, replayVerified = _replayVerified,
            ownedDomainKeyCensus = _ownedDomainKeyCensus.Select(Digest).Order(StringComparer.Ordinal), observations = _replayObservations },
            new JsonSerializerOptions { WriteIndented = true }), cancellation);

    private async Task<JsonDocument> MetadataAsync(string domain)
    {
        using HttpResponseMessage response = await InvokeWorkloadAsync(domain, "admin/operational-index-metadata", new AdminOperationalIndexMetadata.Request([domain]) { AppId = domain, ServiceVersion = "v1" }, "domain-service:metadata").ConfigureAwait(false);
        Require(response.StatusCode == HttpStatusCode.OK, "The real protected query catalog is unavailable.");
        return JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Cancellation).ConfigureAwait(false));
    }

    private async Task<HttpResponseMessage> InvokeWorkloadAsync(string domain, string method, object payload, string operation)
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials", ["client_id"] = "eventstore-fixture", ["client_secret"] = Setting("EventStoreSecret"),
            ["scope"] = new WorkloadAssertionIssuerOptions().BuildScope(domain, operation),
            ["authorization_details"] = WorkloadAuthorizationDetails.Encode(new(domain, operation)),
        });
        using HttpResponseMessage acquired = await _http.PostAsync(_endpoints["broker"] + "/token", form, Cancellation).ConfigureAwait(false);
        Require(acquired.StatusCode == HttpStatusCode.OK, "Properly scoped workload acquisition failed.");
        using JsonDocument token = JsonDocument.Parse(await acquired.Content.ReadAsByteArrayAsync(Cancellation).ConfigureAwait(false));
        using HttpRequestMessage request = new(HttpMethod.Post, Dapr(domain, method)) { Content = JsonContent.Create(payload, options: Json) };
        request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, token.RootElement.GetProperty("access_token").GetString());
        return await _http.SendAsync(request, Cancellation).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendHumanAsync(string route, object body, string token, CancellationToken? cancellationToken = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, _endpoints["eventstore"] + "/" + route) { Content = JsonContent.Create(body, options: Json) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Correlation-Id", Unique());
        return await _http.SendAsync(request, cancellationToken ?? Cancellation).ConfigureAwait(false);
    }

    private async Task WaitForStateAsync(string key)
    {
        for (int attempt = 0; attempt < 150; attempt++)
        {
            if ((await StateAsync(key).ConfigureAwait(false)).Bytes.Length > 0) return;
            await Task.Delay(200, Cancellation).ConfigureAwait(false);
        }
        throw new InvalidOperationException("Required persisted projection/catalog did not appear.");
    }

    private async Task<SupportedProjectReadStateSnapshot> StateAsync(string key)
    {
        if (key.Contains("||", StringComparison.Ordinal))
        {
            // Dapr reserves its actor key separator on the application state API. Observe
            // the owned provider's exact UTF-8 data and version fields without actor activation.
            Require((await RedisAsync("EXISTS", key).ConfigureAwait(false)).Trim() == "1", "A raw persisted actor state observation is absent.");
            byte[] bytes = await RedisBytesAsync("-D", "", "HGET", key, "data").ConfigureAwait(false);
            string version = (await RedisAsync("HGET", key, "version").ConfigureAwait(false)).TrimEnd('\n');
            Require(long.TryParse(version, out long etag) && etag > 0, "A raw persisted actor version observation failed.");
            return new(key, bytes, version);
        }
        using HttpResponseMessage response = await _http.GetAsync(_endpoints["eventstore-dapr-cli"] + "/v1.0/state/statestore/" + Uri.EscapeDataString(key), Cancellation).ConfigureAwait(false);
        Require(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent, "A fresh state-store observation failed.");
        return new(key, await response.Content.ReadAsByteArrayAsync(Cancellation).ConfigureAwait(false),
            response.Headers.TryGetValues("ETag", out IEnumerable<string>? etags) ? etags.Single() : string.Empty);
    }

    private const string RawStateBatchScript = """
        local rows = {}
        local digits = '0123456789ABCDEF'
        for _, key in ipairs(KEYS) do
            local exists = redis.call('EXISTS', key) == 1
            local data = exists and redis.call('HGET', key, 'data') or false
            local version = exists and redis.call('HGET', key, 'version') or false
            local encoded = false
            if data then
                local bytes = {}
                for i = 1, #data do
                    local value = string.byte(data, i)
                    local high = math.floor(value / 16) + 1
                    local low = (value % 16) + 1
                    bytes[i] = string.sub(digits, high, high) .. string.sub(digits, low, low)
                end
                encoded = table.concat(bytes)
            end
            rows[#rows + 1] = { key = key, exists = exists, dataHex = encoded, version = version }
        end
        return cjson.encode(rows)
        """;

    private async Task<SupportedProjectReadStateSnapshot[]> RawStatesAsync(string[] keys)
    {
        if (keys.Length == 0) return [];
        byte[] response = await RedisBytesAsync(["EVAL", RawStateBatchScript, keys.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), .. keys]).ConfigureAwait(false);
        return ParseRawStateBatch(response, keys);
    }

    /// <summary>Rejects missing, duplicate, foreign or malformed observations while preserving every raw byte and version.</summary>
    internal static SupportedProjectReadStateSnapshot[] ParseRawStateBatch(byte[] response, string[] keys)
    {
        Require(keys.Length > 0 && keys.Distinct(StringComparer.Ordinal).Count() == keys.Length, "Raw state observations require distinct existing keys.");
        using JsonDocument document = JsonDocument.Parse(response);
        JsonElement rows = document.RootElement;
        Require(rows.ValueKind == JsonValueKind.Array && rows.GetArrayLength() == keys.Length, "The provider batch is missing state observations.");
        Dictionary<string, SupportedProjectReadStateSnapshot> observed = new(StringComparer.Ordinal);
        foreach (JsonElement row in rows.EnumerateArray())
        {
            Require(row.ValueKind == JsonValueKind.Object && row.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)
                .SequenceEqual(new[] { "dataHex", "exists", "key", "version" }), "The provider batch has malformed observation metadata.");
            Require(row.GetProperty("exists").ValueKind == JsonValueKind.True && row.GetProperty("key").ValueKind == JsonValueKind.String
                && row.GetProperty("dataHex").ValueKind == JsonValueKind.String && row.GetProperty("version").ValueKind == JsonValueKind.String,
                "A raw provider value or concurrency identity is absent.");
            string key = row.GetProperty("key").GetString()!, encoded = row.GetProperty("dataHex").GetString()!, version = row.GetProperty("version").GetString()!;
            Require(keys.Contains(key, StringComparer.Ordinal) && !observed.ContainsKey(key)
                && long.TryParse(version, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out long etag) && etag > 0
                && encoded.Length % 2 == 0 && encoded.All(character => character is >= '0' and <= '9' or >= 'A' and <= 'F'),
                "A raw provider identity, byte encoding or version is malformed.");
            observed.Add(key, new(key, Convert.FromHexString(encoded), version));
        }
        return keys.Select(key => observed[key]).ToArray();
    }

    /// <summary>Compares batched provider reads with the retained individual observer on actual domain data and binary controls.</summary>
    private async Task VerifyRawObserverBatchAsync()
    {
        string[] domainKeys = (await RedisAsync("--scan").ConfigureAwait(false)).Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(key => key.Contains("||", StringComparison.Ordinal) && IsOwnedDomainKey(key, Tenant, ForeignTenant)).Order(StringComparer.Ordinal).ToArray();
        Require(domainKeys.Length > 0, "The provider observer control requires actual persisted domain hashes.");
        byte[][] controls = [Enumerable.Range(0, 256).Select(value => (byte)value).ToArray(), [],
            "\0\r\n\"\\é雪"u8.ToArray(), "{\"data\":\"escaped\\nvalue\"}\n"u8.ToArray()];
        string[] controlKeys = controls.Select(_ => "supported-read-observer-control||" + Isolation + "||" + Unique()).ToArray();
        Require(controlKeys.All(key => !IsOwnedDomainKey(key, Tenant, ForeignTenant)), "Observer controls must not enter any domain family.");
        // These are fresh operational controls, never events, projections or patches to stored state.
        // Refuse every existing key before creating any control; the owned Redis container is their lifecycle.
        const string seed = """
            for _, key in ipairs(KEYS) do
                if redis.call('EXISTS', key) ~= 0 then return redis.error_reply('observer-control-exists') end
            end
            for i, key in ipairs(KEYS) do
                local values = cjson.decode(ARGV[i])
                local parts = {}
                for j, value in ipairs(values) do parts[j] = string.char(value) end
                redis.call('HSET', key, 'data', table.concat(parts), 'version', i == 4 and '9223372036854775807' or tostring(i))
            end
            return #KEYS
            """;
        string created = await RedisAsync(["EVAL", seed, controls.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
            .. controlKeys, .. controls.Select(value => JsonSerializer.Serialize(value.Select(item => (int)item), Json))]).ConfigureAwait(false);
        Require(created.Trim() == controls.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), "Binary provider controls were not created exactly once.");
        string[] keys = [.. domainKeys, .. controlKeys];
        SupportedProjectReadStateSnapshot[] individual = await ObserveIndividualStatesAsync(keys).ConfigureAwait(false);
        SupportedProjectReadStateSnapshot[] batched = await RawStatesAsync(keys).ConfigureAwait(false);
        bool same = SameDomainState(individual, batched);
        bool binary = controlKeys.Select((key, index) => batched.Single(value => value.Key == key).Bytes.AsSpan().SequenceEqual(controls[index])).All(value => value);
        object[] Fingerprints(SupportedProjectReadStateSnapshot[] values) => values.OrderBy(value => value.Key, StringComparer.Ordinal)
            .Select(value => (object)new { keySha256 = Digest(value.Key), bytesSha256 = Convert.ToHexString(SHA256.HashData(value.Bytes)), etagSha256 = Digest(value.ETag) }).ToArray();
        await File.WriteAllTextAsync(Path.Combine(Evidence, "provider-observer-control.json"), JsonSerializer.Serialize(new
        {
            verified = same && binary, domainKeyCount = domainKeys.Length, controlKeyCount = controls.Length,
            binaryAndEscapingBoundariesExact = binary, individual = Fingerprints(individual), batched = Fingerprints(batched),
        }, new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None).ConfigureAwait(false);
        Require(same && binary, "Batched provider observations differ from individual raw bytes or ETags.");
    }

    private async Task<SupportedProjectReadStateSnapshot[]> StatesAsync(IEnumerable<string> keys)
    {
        string[] selected = keys.ToArray();
        SupportedProjectReadStateSnapshot[] raw = await RawStatesAsync(selected.Where(key => key.Contains("||", StringComparison.Ordinal)).ToArray()).ConfigureAwait(false);
        SupportedProjectReadStateSnapshot[] logical = await ObserveIndividualStatesAsync(selected.Where(key => !key.Contains("||", StringComparison.Ordinal))).ConfigureAwait(false);
        return [.. raw, .. logical];
    }

    private async Task<SupportedProjectReadStateSnapshot[]> ObserveIndividualStatesAsync(IEnumerable<string> keys)
    {
        using SemaphoreSlim observations = new(8);
        return await Task.WhenAll(keys.Select(async key =>
        {
            await observations.WaitAsync(Cancellation).ConfigureAwait(false);
            try { return await StateAsync(key).ConfigureAwait(false); }
            finally { observations.Release(); }
        })).ConfigureAwait(false);
    }

    private async Task<string> RedisAsync(params string[] arguments)
        => System.Text.Encoding.UTF8.GetString(await RedisBytesAsync(arguments).ConfigureAwait(false));

    private async Task<byte[]> RedisBytesAsync(params string[] arguments)
    {
        string id = _endpoints["redis-container-id"];
        string observed = await ProcessAsync("docker", "inspect", "--format",
            "{\"id\":{{json .Id}},\"owner\":{{json (index .Config.Labels \"hexalith.supported-reads\")}}}", id).ConfigureAwait(false);
        Require(IsOwnedRedisIdentity(observed, id, Isolation), "Storage observation requires the immutable owned creation identity.");
        return await ProcessBytesAsync("docker", ["exec", id, "redis-cli", "-e", "--raw", .. arguments]).ConfigureAwait(false);
    }

    /// <summary>Refuses a name replacement or owner mismatch before storage access.</summary>
    internal static bool IsOwnedRedisIdentity(string observed, string expected, string isolation)
    {
        using JsonDocument identity = JsonDocument.Parse(observed);
        return Regex.IsMatch(expected, "^[0-9a-f]{64}$", RegexOptions.CultureInvariant)
            && identity.RootElement.ValueKind == JsonValueKind.Object
            && identity.RootElement.TryGetProperty("id", out JsonElement id) && id.ValueKind == JsonValueKind.String && id.GetString() == expected
            && identity.RootElement.TryGetProperty("owner", out JsonElement owner) && owner.ValueKind == JsonValueKind.String && owner.GetString() == isolation;
    }

    private static async Task<string> ProcessAsync(string file, params string[] arguments)
        => System.Text.Encoding.UTF8.GetString(await ProcessBytesAsync(file, arguments).ConfigureAwait(false));

    private static Task<byte[]> ProcessBytesAsync(string file, params string[] arguments)
        => RunOwnedProcessAsync(file, arguments, Cancellation);

    /// <summary>Terminates and observes only this helper and its descendants if the caller cancels.</summary>
    internal static async Task<byte[]> RunOwnedProcessAsync(string file, string[] arguments, CancellationToken cancellation)
    {
        using Process process = new() { StartInfo = new(file) { RedirectStandardOutput = true, RedirectStandardError = true } };
        foreach (string argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        process.Start();
        using MemoryStream output = new();
        Task stdout = process.StandardOutput.BaseStream.CopyToAsync(output);
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(cancellation).ConfigureAwait(false);
            await Task.WhenAll(stdout, stderr).WaitAsync(cancellation).ConfigureAwait(false);
            Require(process.ExitCode == 0, "Owned fixture operation failed: " + file);
            return output.ToArray();
        }
        catch
        {
            List<Process> descendants = [];
            void ObserveChildren(int parent)
            {
                string children;
                try { children = File.ReadAllText($"/proc/{parent}/task/{parent}/children"); }
                catch (FileNotFoundException) { return; }
                catch (DirectoryNotFoundException) { return; }
                foreach (string child in children.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    int pid = int.Parse(child, System.Globalization.CultureInfo.InvariantCulture);
                    try
                    {
                        Process owned = Process.GetProcessById(pid);
                        _ = owned.Handle;
                        descendants.Add(owned);
                        ObserveChildren(pid);
                    }
                    catch (ArgumentException) { }
                }
            }
            try
            {
                Exception? observationFailure = null;
                try { ObserveChildren(process.Id); }
                catch (Exception failure) when (failure is IOException or UnauthorizedAccessException) { observationFailure = failure; }
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                foreach (Process child in descendants)
                    await child.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                if (observationFailure is not null) throw new InvalidOperationException("Owned helper child observation failed during cancellation cleanup.");
            }
            finally { foreach (Process child in descendants) child.Dispose(); }
            throw;
        }
    }

    /// <summary>Uses the request-start lower bound to exclude expiry of the existing fifteen-minute cursor.</summary>
    internal static bool CursorIsWithinObservedLifetime(DateTimeOffset requestedAt, DateTimeOffset observedAt)
        => observedAt >= requestedAt && observedAt < requestedAt.AddMinutes(15);

    private string Dapr(string app, string method) => _endpoints["eventstore-dapr-cli"] + "/v1.0/invoke/" + app + "/method/" + method;
    private string Setting(string key) => _configuration.GetProperty(key).GetString()!;
    private static string Unique() => UniqueIdHelper.GenerateSortableUniqueStringId().ToLowerInvariant();
    private static void Require(bool condition, string message) => Assert.True(condition, message);
    /// <summary>Disposes the in-process client and synthetic user signer.</summary>
    public void Dispose() { _http.Dispose(); _userKey.Dispose(); }
}
#endif
