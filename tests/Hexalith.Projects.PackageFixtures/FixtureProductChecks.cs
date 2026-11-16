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
        await AwaitStateAsync(value => value.Contains("global-administrators", StringComparison.Ordinal)
            && value.Contains(inputs.Subjects["actor-1"], StringComparison.Ordinal), "durable authenticated administrator bootstrap", cancellationToken).ConfigureAwait(false);
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
        using HttpClient http = inputs.CreateHttpClient();
        using HttpRequestMessage submit = new(HttpMethod.Post, new Uri(factory.GetEndpoint("eventstore", "http"), "api/v1/commands"))
        {
            Content = JsonContent.Create(new { messageId = message, tenant, domain, aggregateId = aggregate, commandType = type, payload, correlationId = message }),
        };
        submit.Headers.Authorization = new AuthenticationHeaderValue("Bearer", inputs.Tokens[username]);
        if (tenant.StartsWith("platform:", StringComparison.Ordinal)) submit.Headers.Add("X-Hexalith-Scope", tenant);
        using HttpResponseMessage accepted = await http.SendAsync(submit, cancellationToken).ConfigureAwait(false);
        if (accepted.StatusCode != HttpStatusCode.Accepted)
            throw new InvalidOperationException($"Authenticated fixture command {type} was refused with HTTP {(int)accepted.StatusCode}.");
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(1));
        while (true)
        {
            using HttpRequestMessage status = new(HttpMethod.Get, new Uri(factory.GetEndpoint("eventstore", "http"), "api/v1/commands/status/" + message));
            status.Headers.Authorization = new AuthenticationHeaderValue("Bearer", inputs.Tokens[username]);
            status.Headers.Add(tenant.StartsWith("platform:", StringComparison.Ordinal) ? "X-Hexalith-Scope" : "X-Hexalith-Tenant", tenant);
            using HttpResponseMessage response = await http.SendAsync(status, timeout.Token).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                JsonObject state = (await response.Content.ReadFromJsonAsync<JsonObject>(timeout.Token).ConfigureAwait(false))!;
                string? outcome = state["status"]?.GetValue<string>();
                if (outcome == "Completed") return;
                if (outcome is "Rejected" or "TimedOut" or "PublishFailed") throw new InvalidOperationException("Fixture product command failed: " + type + " (" + outcome + ").");
            }
            else if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.Conflict)
                throw new InvalidOperationException("Authenticated fixture command status was refused: " + type);
            await Task.Delay(250, timeout.Token).ConfigureAwait(false);
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
