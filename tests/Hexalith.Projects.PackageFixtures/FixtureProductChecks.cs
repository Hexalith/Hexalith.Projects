namespace Hexalith.Projects.PackageFixtures;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

using Aspire.Hosting.Testing;

using Hexalith.Projects.E2E.Fixtures;
using Hexalith.Commons.UniqueIds;

/// <summary>Seeds through authenticated product commands and independently reads fixture-owned persisted contents.</summary>
internal sealed class FixtureProductChecks(FixtureInputs inputs, FixtureScratch scratch, DistributedApplicationFactory factory)
{
    private const string Observe = """
        local result = {}; local cursor = '0';
        repeat
          local page = redis.call('SCAN', cursor); cursor = page[1];
          for _, key in ipairs(page[2]) do
            if redis.call('TYPE', key).ok == 'hash' then
              local data = redis.call('HGET', key, 'data');
              if data then table.insert(result, {key=key, data=data, version=redis.call('HGET',key,'version')}); end;
            end;
          end;
        until cursor == '0';
        return cjson.encode(result);
        """;

    internal async Task VerifyBootstrapAsync(CancellationToken cancellationToken)
    {
        string planePath = scratch.File("catalog-plane.json");
        using JsonDocument plane = JsonDocument.Parse(await File.ReadAllBytesAsync(planePath, cancellationToken).ConfigureAwait(false));
        if (plane.RootElement.GetProperty("isolationId").GetString() != scratch.Isolation)
            throw new InvalidOperationException("Bootstrap catalog belongs to another fixture.");
        string directory = plane.RootElement.GetProperty("directory").GetString()!;
        DirectoryInfo catalogDirectory = new(directory);
        if (!Path.IsPathFullyQualified(directory) || catalogDirectory.LinkTarget is not null
            || catalogDirectory.FullName != directory
            || catalogDirectory.Parent?.FullName != Path.GetDirectoryName(scratch.Directory)
            || !catalogDirectory.Name.StartsWith("hexalith-route-catalog-", StringComparison.Ordinal)
            || (File.GetUnixFileMode(directory) & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
            throw new InvalidOperationException("Bootstrap receipt is outside the private fixture.");
        string receiptPath = Path.Combine(directory, "tenants.bootstrap.json");
        await FixtureReadiness.AwaitAsync(_ => Task.FromResult(File.Exists(receiptPath)), "original hosted bootstrap receipt",
            TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        byte[] original = await File.ReadAllBytesAsync(receiptPath, cancellationToken).ConfigureAwait(false);
        using JsonDocument receipt = JsonDocument.Parse(original);
        JsonElement binding = receipt.RootElement;
        string actor = inputs.Subjects["actor-1"];
        string message = binding.GetProperty("MessageId").GetString()!;
        string correlation = binding.GetProperty("CorrelationId").GetString()!;
        if (binding.GetProperty("PrincipalId").GetString() != actor
            || binding.GetProperty("RootDigest").GetString() != plane.RootElement.GetProperty("rootDigest").GetString()
            || !ByteAether.Ulid.Ulid.TryParse(message, null, out _)
            || !ByteAether.Ulid.Ulid.TryParse(correlation, null, out _)
            || string.IsNullOrWhiteSpace(binding.GetProperty("AttemptId").GetString()))
            throw new InvalidOperationException("Hosted bootstrap receipt identity differs from this fixture.");
        using HttpClient http = inputs.CreateHttpClient();
        await FixtureReadiness.AwaitAsync(async token =>
        {
            using HttpRequestMessage request = new(HttpMethod.Get,
                new Uri(factory.GetEndpoint("eventstore", "http"), "api/v1/commands/status/" + message));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", inputs.Tokens["actor-1"]);
            request.Headers.Add("X-Hexalith-Scope", "platform:global-administrators");
            using HttpResponseMessage response = await http.SendAsync(request, token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound) return false;
            response.EnsureSuccessStatusCode();
            JsonObject state = (await response.Content.ReadFromJsonAsync<JsonObject>(token).ConfigureAwait(false))!;
            string? outcome = state["status"]?.GetValue<string>();
            if (outcome is "Rejected" or "TimedOut" or "PublishFailed")
                throw new InvalidOperationException("Hosted bootstrap failed: " + FixtureDiagnostics.Status(state, inputs.ProtectedValues));
            return outcome == "Completed" && state["correlationId"]?.GetValue<string>() == correlation;
        }, "original hosted bootstrap command completion", TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        await AwaitStateAsync(value => HasBootstrapProjection(value, actor), "durable administrator projection", cancellationToken).ConfigureAwait(false);
        using JsonDocument final = JsonDocument.Parse(await File.ReadAllBytesAsync(receiptPath, cancellationToken).ConfigureAwait(false));
        foreach (string key in new[] { "AttemptId", "RootDigest", "MessageId", "CorrelationId", "PrincipalId" })
            if (binding.GetProperty(key).GetString() != final.RootElement.GetProperty(key).GetString())
                throw new InvalidOperationException("Hosted bootstrap receipt changed during completion.");
    }

    internal static bool HasBootstrapProjection(string serialized, string actor)
    {
        JsonObject[] matches = JsonNode.Parse(serialized)!.AsArray().OfType<JsonObject>()
            .Where(row => row["key"]?.GetValue<string>().EndsWith("projection:global-administrators:singleton", StringComparison.Ordinal) == true).ToArray();
        if (matches.Length != 1) return false;
        JsonObject model = JsonNode.Parse(matches[0]["data"]!.GetValue<string>())!.AsObject();
        JsonArray? administrators = (model["administrators"] ?? model["Administrators"]) as JsonArray;
        string? version = (model["projectionVersion"] ?? model["ProjectionVersion"])?.GetValue<string>();
        string? projected = (model["projectedAt"] ?? model["ProjectedAt"])?.GetValue<string>();
        return administrators is { Count: 1 } && administrators[0]?.GetValue<string>() == actor
            && !string.IsNullOrWhiteSpace(version) && DateTimeOffset.TryParse(projected,
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out _);
    }

    internal async Task SeedOwnersAsync(CancellationToken cancellationToken)
    {
        foreach (string tenant in new[] { inputs.ForeignTenant, inputs.Tenant })
        {
            string username = tenant == inputs.Tenant ? "actor-1" : "foreign-actor";
            string actor = inputs.Subjects[username];
            await CommandAsync("platform:tenants", "tenants", tenant, "CreateTenant",
                new { TenantId = tenant, Name = "Synthetic tenant", Description = "Isolated read proof" }, "actor-1", cancellationToken).ConfigureAwait(false);
            await CommandAsync("platform:tenants", "tenants", tenant, "AddUserToTenant",
                new { TenantId = tenant, UserId = actor, Role = "TenantOwner" }, "actor-1", cancellationToken).ConfigureAwait(false);
            await ReplayTenantEventsAsync(tenant, cancellationToken).ConfigureAwait(false);
            await AwaitStateAsync(value => value.Contains("projects:tenant-access:" + tenant, StringComparison.Ordinal)
                && value.Contains("projection:folders:tenant-access:" + tenant, StringComparison.Ordinal)
                && value.Contains(actor, StringComparison.Ordinal), "both owning tenant-access projections", cancellationToken).ConfigureAwait(false);
            await CommandAsync(tenant, "folders", tenant, "Hexalith.Folders.Commands.InitializeOrganizationAclBaseline",
                new { RequestSchemaVersion = "v1", Operations = new[] { new { PrincipalKind = "user", PrincipalId = actor, Action = "create_folder" },
                    new { PrincipalKind = "user", PrincipalId = actor, Action = "manage_folder_access" } } }, username, cancellationToken).ConfigureAwait(false);
            foreach (string project in tenant == inputs.Tenant ? new[] { "project-a", "project-b" } : new[] { "foreign-project-a", "foreign-project-b" })
            {
                string folder = "folder-" + project;
                await CommandAsync(tenant, "folders", folder, "Hexalith.Folders.Commands.CreateFolder", new
                {
                    RequestSchemaVersion = "v1", FolderId = folder, ParentFolderId = (string?)null,
                    FolderMetadata = new { DisplayName = "Owned folder label", MetadataClass = (string?)null },
                }, username, cancellationToken).ConfigureAwait(false);
                await CommandAsync(tenant, "folders", folder, "Hexalith.Folders.Commands.GrantFolderAccess", new
                {
                    RequestSchemaVersion = "v1", Operations = new[] { new { PrincipalKind = "user", PrincipalId = actor, Action = "read_metadata" } },
                }, username, cancellationToken).ConfigureAwait(false);
                await CommandAsync(tenant, "projects", project, "Hexalith.Projects.Commands.CreateProject", new
                {
                    Name = tenant == inputs.ForeignTenant ? "Foreign synthetic name" : "Synthetic project " + project,
                    Description = "Synthetic descriptive metadata", SetupMetadata = (string?)null,
                }, username, cancellationToken).ConfigureAwait(false);
                await CommandAsync(tenant, "projects", project, "Hexalith.Projects.Commands.SetProjectFolder", new
                {
                    RequestSchemaVersion = "v1", Operation = "set", ProjectId = project, FolderId = folder,
                    FolderMetadata = new { DisplayName = "Owned folder label" }, ReplacementConfirmed = false,
                }, username, cancellationToken).ConfigureAwait(false);
                await AwaitStateAsync(value => value.Contains("projects:reads:v1:", StringComparison.Ordinal)
                    && value.Contains(project, StringComparison.Ordinal) && value.Contains("projection:folders:folder-permissions:" + tenant + ":" + folder,
                        StringComparison.Ordinal), "persisted Project reads and Folder permission contents", cancellationToken).ConfigureAwait(false);
            }
        }
        foreach (string project in new[] { "legacy-a", "legacy-b" })
            await CommandAsync(inputs.Tenant, "projects", project, "Hexalith.Projects.Commands.CreateProject", new
            {
                Name = "Legacy project " + project, Description = "Restricted legacy description", SetupMetadata = "Restricted legacy setup reference",
            }, "actor-1", cancellationToken).ConfigureAwait(false);
    }

    internal async Task SeedCompatibilityTenantAsync(CancellationToken cancellationToken)
    {
        await CommandAsync("platform:tenants", "tenants", inputs.Tenant, "CreateTenant",
            new { TenantId = inputs.Tenant, Name = "Synthetic tenant", Description = "Isolated compatibility browser proof" },
            "actor-1", cancellationToken).ConfigureAwait(false);
        await CommandAsync("platform:tenants", "tenants", inputs.Tenant, "AddUserToTenant",
            new { TenantId = inputs.Tenant, UserId = inputs.Subjects["actor-1"], Role = "TenantOwner" },
            "actor-1", cancellationToken).ConfigureAwait(false);
        await AwaitStateAsync(value => value.Contains("projects:tenant-access:" + inputs.Tenant, StringComparison.Ordinal)
            && value.Contains(inputs.Subjects["actor-1"], StringComparison.Ordinal),
            "authenticated compatibility tenant membership projection", cancellationToken).ConfigureAwait(false);
        await CommandAsync(inputs.Tenant, "projects", "compatibility-baseline", "Hexalith.Projects.Commands.CreateProject",
            new { Name = "Compatibility baseline", Description = (string?)null, SetupMetadata = (string?)null },
            "actor-1", cancellationToken).ConfigureAwait(false);
        await AwaitStateAsync(value => value.Contains("projects:projection-journal:" + inputs.Tenant, StringComparison.Ordinal)
            && value.Contains("compatibility-baseline", StringComparison.Ordinal),
            "existing compatibility Project journal", cancellationToken).ConfigureAwait(false);
    }

    internal async Task RegisterObservationsAsync(CancellationToken cancellationToken)
    {
        using HttpClient http = factory.CreateHttpClient("browser-read-observer", "http");
        foreach ((string suffix, string tenant, string actor) in new[]
        {
            ("", inputs.Tenant, inputs.Subjects["actor-1"]), ("-foreign", inputs.ForeignTenant, inputs.Subjects["foreign-actor"]),
        })
        {
            Dictionary<string, object> graph = new()
            {
                ["graphId"] = scratch.Isolation + suffix, ["runId"] = scratch.Isolation, ["workerIndex"] = 0, ["retry"] = 0,
                ["repeatEachIndex"] = 0, ["scenario"] = "supported-oidc", ["tenantId"] = tenant, ["principalId"] = actor, ["filePath"] = "fixture.txt",
            };
            foreach (string key in new[] { "projectId", "secondaryProjectId", "proposalProjectId", "proposalRetryProjectId", "degradedProjectId", "conversationId",
                "ambiguousConversationId", "existingConversationId", "staleConversationId", "forbiddenConversationId", "unavailableConversationId", "folderId",
                "secondaryFolderId", "proposalFolderId", "workspaceId", "fileReferenceId", "secondaryFileReferenceId", "proposalFileReferenceId", "deniedFileReferenceId",
                "memoryReferenceId", "correlationId", "taskId", "idempotencyKey" }) graph[key] = "owned-" + key.ToLowerInvariant();
            graph["projectId"] = "project-a"; graph["secondaryProjectId"] = "project-b"; graph["folderId"] = "folder-project-a";
            using HttpResponseMessage response = await http.PostAsJsonAsync("/_fixtures/graphs", graph, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException("Test-owned persisted observation graph was refused.");
        }
    }

    internal async Task VerifyDurableStateAsync(bool actualOwners, CancellationToken cancellationToken)
    {
        await AwaitStateAsync(value => value.Contains("AggregateActor", StringComparison.Ordinal)
            && value.Contains(inputs.Tenant, StringComparison.Ordinal)
            && value.Contains("projects", StringComparison.Ordinal)
            && (!actualOwners || value.Contains("projects:read-audit:v1:", StringComparison.Ordinal)
                && value.Contains("projects:reads:v1:", StringComparison.Ordinal)
                && value.Contains("projection:folders:", StringComparison.Ordinal)
                && value.Contains(inputs.ForeignTenant, StringComparison.Ordinal)), "durable domain, projection and separate audit contents", cancellationToken).ConfigureAwait(false);
    }

    private async Task AwaitStateAsync(Func<string, bool> predicate, string purpose, CancellationToken cancellationToken)
    {
        await FixtureReadiness.AwaitAsync(async token => predicate(await new RedisFixtureClient(inputs.Redis)
            .ReadAsync(["EVAL", Observe, "0"], token).ConfigureAwait(false)), purpose, TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
    }

    private async Task CommandAsync(string tenant, string domain, string aggregate, string type, object payload, string username,
        CancellationToken cancellationToken)
    {
        string message = UniqueIdHelper.GenerateSortableUniqueStringId();
        if (domain == "folders") message = message.ToLowerInvariant();
        using HttpClient http = inputs.CreateHttpClient();
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(1));
        JsonObject body = JsonSerializer.SerializeToNode(new
        {
            messageId = message, tenant, domain, aggregateId = aggregate, commandType = type, payload, correlationId = message,
        }, JsonSerializerOptions.Web)!.AsObject();
        if (domain == "folders") body["extensions"] = new JsonObject { ["taskId"] = message.ToLowerInvariant() };
        int lastSubmission = 0;
        try
        {
            for (int attempt = 0; attempt < 4; attempt++)
            {
                using HttpRequestMessage submit = new(HttpMethod.Post, new Uri(factory.GetEndpoint("eventstore", "http"), "api/v1/commands"))
                {
                    Content = JsonContent.Create(body),
                };
                submit.Headers.Authorization = new AuthenticationHeaderValue("Bearer", inputs.Tokens[username]);
                if (tenant.StartsWith("platform:", StringComparison.Ordinal)) submit.Headers.Add("X-Hexalith-Scope", tenant);
                try
                {
                    using HttpResponseMessage accepted = await http.SendAsync(submit, timeout.Token).ConfigureAwait(false);
                    lastSubmission = (int)accepted.StatusCode;
                    if (lastSubmission != 202)
                        await FixtureDiagnostics.ResponseAsync(accepted, type, inputs.ProtectedValues, timeout.Token).ConfigureAwait(false);
                }
                catch (HttpRequestException) { lastSubmission = 504; }
                if (lastSubmission is 202 or 409 or 422) break;
                if (lastSubmission is not (408 or 429 or 500 or 502 or 503 or 504))
                    throw new InvalidOperationException($"Authenticated fixture command {type} was refused with HTTP {lastSubmission}.");
                // An ambiguous submission may have committed. Observe before retrying its same identity.
                if (await CompletedAsync().ConfigureAwait(false)) return;
                await Task.Delay(250, timeout.Token).ConfigureAwait(false);
            }
            while (!await CompletedAsync().ConfigureAwait(false))
                await Task.Delay(250, timeout.Token).ConfigureAwait(false);
            Console.WriteLine("[package-fixtures] authenticated command completed: " + type);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Fixture command {type} did not finish within one minute; last submission HTTP {lastSubmission}.");
        }

        async Task<bool> CompletedAsync()
        {
            using HttpRequestMessage status = new(HttpMethod.Get, new Uri(factory.GetEndpoint("eventstore", "http"), "api/v1/commands/status/" + message));
            status.Headers.Authorization = new AuthenticationHeaderValue("Bearer", inputs.Tokens[username]);
            status.Headers.Add(tenant.StartsWith("platform:", StringComparison.Ordinal) ? "X-Hexalith-Scope" : "X-Hexalith-Tenant", tenant);
            using HttpResponseMessage response = await http.SendAsync(status, timeout.Token).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                JsonObject state = (await response.Content.ReadFromJsonAsync<JsonObject>(timeout.Token).ConfigureAwait(false))!;
                string? outcome = state["status"]?.GetValue<string>();
                if (outcome == "Completed") return true;
                if (outcome is "Rejected" or "TimedOut" or "PublishFailed")
                    throw new InvalidOperationException("Fixture product command failed: " + type + " (" + outcome + "; "
                        + FixtureDiagnostics.Status(state, inputs.ProtectedValues) + ").");
            }
            else if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.Conflict)
                throw new InvalidOperationException("Authenticated fixture command status was refused: " + type);
            return false;
        }
    }

    private async Task ReplayTenantEventsAsync(string tenant, CancellationToken cancellationToken)
    {
        using HttpClient http = inputs.CreateHttpClient();
        using HttpRequestMessage read = new(HttpMethod.Post, new Uri(factory.GetEndpoint("eventstore", "http"), "api/v1/streams/read"))
        {
            Content = JsonContent.Create(new { tenant = "platform:tenants", domain = "tenants", aggregateId = tenant }),
        };
        read.Headers.Authorization = new AuthenticationHeaderValue("Bearer", inputs.Tokens["actor-1"]);
        read.Headers.Add("X-Hexalith-Scope", "platform:tenants");
        using HttpResponseMessage response = await http.SendAsync(read, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        JsonObject stream = (await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken).ConfigureAwait(false))!;
        if (stream["events"]!.AsArray().Count < 2 || stream["metadata"]?["isTruncated"]?.GetValue<bool>() != false)
            throw new InvalidOperationException("The persisted Tenant event page must contain the complete creation/membership history.");
        foreach (JsonNode item in stream["events"]!.AsArray()!)
        {
            JsonObject envelope = item!.DeepClone().AsObject();
            envelope["aggregateId"] = tenant; envelope["tenantId"] = "system"; envelope["domain"] = "tenants";
            // The normal persisted event is republished on the Folder owner's existing topic.
            using HttpResponseMessage published = await http.PostAsJsonAsync(new Uri(factory.GetEndpoint("eventstore-dapr-cli", "http"),
                "/v1.0/publish/pubsub/tenants.events"), envelope, cancellationToken).ConfigureAwait(false);
            if (published.StatusCode != HttpStatusCode.NoContent) throw new InvalidOperationException("Persisted Tenant event pub/sub delivery was refused.");
        }
    }
}
