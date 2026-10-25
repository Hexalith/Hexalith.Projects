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
    internal async Task<T> ReadAsync<T>(bool open = false, bool chatbot = true, bool inspection = false, ListProjectsQuery? list = null, string project = "project-a", string? tenant = null)
    {
        using HttpResponseMessage response = await QueryAsync(open, chatbot, inspection, list, project, tenant).ConfigureAwait(false);
        Require(response.StatusCode == HttpStatusCode.OK, "Supported read returned HTTP " + (int)response.StatusCode + ".");
        SubmitQueryResponse? result = await response.Content.ReadFromJsonAsync<SubmitQueryResponse>(Json, Cancellation).ConfigureAwait(false);
        return result!.Payload.Deserialize<T>(Json)!;
    }

    /// <summary>Sends one supported list/open request without forwarding human credentials downstream.</summary>
    internal Task<HttpResponseMessage> QueryAsync(bool open = false, bool chatbot = true, bool inspection = false, ListProjectsQuery? list = null, string project = "project-a", string? tenant = null, bool inspectionPermission = true, CancellationToken? cancellationToken = null)
    {
        string targetTenant = tenant ?? Tenant;
        object input = open ? new GetProjectQuery(inspection) : list ?? new ListProjectsQuery(InspectDescriptiveMetadata: inspection);
        var query = new SubmitQueryRequest(targetTenant, "projects", open ? project : "projects", open ? GetProjectQuery.QueryType : ListProjectsQuery.QueryType,
            Payload: JsonSerializer.SerializeToElement(input, input.GetType(), Json));
        return SendHumanAsync("api/v1/queries", query, HumanToken(Tenant, chatbot, inspection && inspectionPermission,
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
    internal async Task<SupportedProjectReadStateSnapshot[]> SnapshotAsync()
    {
        List<SupportedProjectReadStateSnapshot> result = [];
        foreach (string tenant in new[] { Tenant, ForeignTenant })
        {
            foreach (string key in new[] { ProjectReadModelKeys.Index(tenant), ProjectReadModelKeys.Detail(tenant, "project-a"), ProjectReadModelKeys.Detail(tenant, "project-b"),
                "projects:tenant-access:" + tenant, "projection:folders:tenant-access:" + tenant, "projection:folders:organization-permissions:" + tenant,
                "projection:folders:folder-permissions:" + tenant + ":folder-project-a", "projection:folders:folder-permissions:" + tenant + ":folder-project-b" })
            {
                SupportedProjectReadStateSnapshot snapshot = await StateAsync(key).ConfigureAwait(false);
                Require(snapshot.Bytes.Length > 0 && snapshot.ETag.Length > 0, "Required persisted domain evidence is absent.");
                result.Add(snapshot);
            }
        }
        string[] domainKeys = (await RedisAsync("--scan").ConfigureAwait(false)).Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(key => IsOwnedDomainKey(key, Tenant, ForeignTenant)).Order(StringComparer.Ordinal).ToArray();
        foreach (string tenant in new[] { Tenant, ForeignTenant })
            foreach (string domain in new[] { "projects", "folders" })
                Require(domainKeys.Any(key => key.Contains("AggregateActor", StringComparison.Ordinal)
                    && key.Contains(tenant + ":" + domain + ":", StringComparison.Ordinal)), "Required persisted aggregate journal evidence is absent.");
        foreach (SupportedProjectReadStateSnapshot snapshot in await StatesAsync(domainKeys.Except(result.Select(item => item.Key), StringComparer.Ordinal)).ConfigureAwait(false))
        {
            Require(snapshot.Bytes.Length > 0 && snapshot.ETag.Length > 0, "A persisted domain observation is absent.");
            result.Add(snapshot);
        }
        return [.. result];
    }

    /// <summary>Requires exact domain bytes and ETags after read-only operations.</summary>
    internal async Task AssertUnchangedAsync(SupportedProjectReadStateSnapshot[] before)
    {
        SupportedProjectReadStateSnapshot[] observed = await SnapshotAsync().ConfigureAwait(false);
        Require(before.Select(item => item.Key).SequenceEqual(observed.Select(item => item.Key)), "A read changed the persisted domain key set.");
        foreach ((SupportedProjectReadStateSnapshot snapshot, SupportedProjectReadStateSnapshot after) in before.Zip(observed))
        {
            Require(after.Bytes.AsSpan().SequenceEqual(snapshot.Bytes) && after.ETag == snapshot.ETag, "A read changed persisted domain bytes or ETags.");
        }
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
    internal async Task WaitForAuthorizedRecoveryAsync(string fault)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        HashSet<int> statuses = [];
        for (int attempt = 1; elapsed.Elapsed < TimeSpan.FromSeconds(90); attempt++)
        {
            using HttpResponseMessage response = await QueryAsync(open: true).ConfigureAwait(false);
            statuses.Add((int)response.StatusCode);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                SubmitQueryResponse? result = await response.Content.ReadFromJsonAsync<SubmitQueryResponse>(Json, Cancellation).ConfigureAwait(false);
                GetProjectResponse? opened = result!.Payload.Deserialize<GetProjectResponse>(Json);
                if (opened?.Name is not null && opened.Snapshot.ResponseState == AdmissionResponseState.Complete)
                {
                    await File.AppendAllTextAsync(Path.Combine(Evidence, "recovery-probes.log"), fault + ": attempts=" + attempt
                        + ", statuses=" + string.Join(',', statuses.Order()) + Environment.NewLine, Cancellation).ConfigureAwait(false);
                    return;
                }
            }
            else Require(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable,
                "Dependency recovery encountered an unexpected authentication or request failure.");
            await Task.Delay(1000, Cancellation).ConfigureAwait(false);
        }
        throw new InvalidOperationException("The restored " + fault + " dependency did not recover an actual authorized read.");
    }

    /// <summary>Stops or starts the exact owned Folder resource through Aspire.</summary>
    internal async Task ResourceAsync(string command)
    {
        string launcher = Path.Combine(TestRepositoryRoot.Find(), "tools", "qualification", "run_supported_project_reads.py");
        await ProcessAsync("python3", launcher, "--resource-action", command, "--ownership", _endpoints["ownership"]).ConfigureAwait(false);
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
        Require(stream.Events.Count >= 2 && stream.Events.All(item => item.GlobalPosition > 0), "Source projection evidence is incomplete.");
        using JsonDocument metadata = await MetadataAsync("projects").ConfigureAwait(false);
        var events = stream.Events.Select(item => new ProjectionEventDto(item.EventTypeName, item.Payload, item.SerializationFormat, item.SequenceNumber,
            item.Timestamp, item.CorrelationId ?? item.MessageId, item.MessageId, item.UserId, item.GlobalPosition) { StoredPayloadVersion = item.PayloadVersion }).ToArray();
        var request = new ProjectionDispatchRequest(new(tenant, "projects", project, events), ["project-reads"], Unique(), metadata.RootElement.GetProperty("catalogFingerprint").GetString()!);
        using HttpResponseMessage response = await InvokeWorkloadAsync("projects", "project/v2", request, "domain-service:project").ConfigureAwait(false);
        Require(response.StatusCode == HttpStatusCode.OK, "Supported persisted projection endpoint refused seeded source events.");
        ProjectionDispatchResponse? result = await response.Content.ReadFromJsonAsync<ProjectionDispatchResponse>(Json, Cancellation).ConfigureAwait(false);
        Require(result!.Outcomes.All(item => item.Status == ProjectionDispatchStatus.Completed), "Persisted Project projection did not complete.");
    }

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

    private async Task<SupportedProjectReadStateSnapshot[]> StatesAsync(IEnumerable<string> keys)
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

    private static async Task<byte[]> ProcessBytesAsync(string file, params string[] arguments)
    {
        using Process process = new() { StartInfo = new(file) { RedirectStandardOutput = true, RedirectStandardError = true } };
        foreach (string argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        process.Start();
        using MemoryStream output = new();
        Task stdout = process.StandardOutput.BaseStream.CopyToAsync(output, Cancellation);
        Task<string> stderr = process.StandardError.ReadToEndAsync(Cancellation);
        await process.WaitForExitAsync(Cancellation).ConfigureAwait(false);
        Require(process.ExitCode == 0, "Owned fixture operation failed: " + file);
        _ = await stderr.ConfigureAwait(false);
        await stdout.ConfigureAwait(false);
        return output.ToArray();
    }

    private string Dapr(string app, string method) => _endpoints["eventstore-dapr-cli"] + "/v1.0/invoke/" + app + "/method/" + method;
    private string Setting(string key) => _configuration.GetProperty(key).GetString()!;
    private static string Unique() => UniqueIdHelper.GenerateSortableUniqueStringId().ToLowerInvariant();
    private static void Require(bool condition, string message) => Assert.True(condition, message);
    /// <summary>Disposes the in-process client and synthetic user signer.</summary>
    public void Dispose() { _http.Dispose(); _userKey.Dispose(); }
}
#endif
