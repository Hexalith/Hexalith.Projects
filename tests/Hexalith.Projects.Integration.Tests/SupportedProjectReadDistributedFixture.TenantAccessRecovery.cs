#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Projects.Projections.TenantAccess;
using Hexalith.Projects.AppHost;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Identity;

/// <summary>Exercises supported recovery against the actual launched owners and persisted read model.</summary>
internal sealed partial class SupportedProjectReadDistributedFixture
{
    /// <summary>Reuses existing source authority and the same real OIDC credential across concurrent recovery retries.</summary>
    internal async Task VerifyTenantAccessRecoveryAsync()
    {
        Require(Setting("Scenario") == "tenant-access-recovery", "The recovery proof requires its exact owned scenario.");
        Require(_configuration.TryGetProperty("Browser", out _), "Recovery requires the actual OIDC authority.");
        string token = HumanToken(Tenant, chatbot: false, administrator: true);
        ProjectTenantAccessProjection before = await ReadRecoveryProjectionAsync().ConfigureAwait(false);
        Require(before.Enabled && before.Principals.ContainsKey(PrimaryActor) && before.LastEventTimestamp is not null
            && !before.MalformedEvidence && !before.ReplayConflict, "Existing source membership was not projected before recovery.");
        TimeSpan freshness = TimeSpan.Parse(SupportedProjectReadFixtureProfile.TenantAccessRecoveryFreshnessBudget,
            System.Globalization.CultureInfo.InvariantCulture);
        double staleAge = await AwaitStaleRecoveryListAsync(token, before, freshness).ConfigureAwait(false);

        string anonymousId = Unique();
        SubmitCommandRequest anonymousCommand = RecoveryCommand(anonymousId);
        using (HttpRequestMessage anonymous = new(HttpMethod.Post, _endpoints["eventstore"] + "/api/v1/commands")
        {
            Content = JsonContent.Create(anonymousCommand, options: Json),
        })
        {
            anonymous.Headers.Add("X-Hexalith-Scope", TenantPlatformScopes.Tenants);
            using HttpResponseMessage response = await _http.SendAsync(anonymous, Cancellation).ConfigureAwait(false);
            await ObserveRecoveryAsync("anonymous", (int)response.StatusCode).ConfigureAwait(false);
            Require(response.StatusCode == HttpStatusCode.Unauthorized, "Anonymous recovery was not rejected with HTTP 401; actual=" + (int)response.StatusCode + ".");
        }

        string deniedId = Unique();
        string foreignToken = HumanToken(ForeignTenant, chatbot: false, administrator: true);
        int deniedStatus;
        using (HttpResponseMessage denied = await SendHumanAsync("api/v1/commands", RecoveryCommand(deniedId), foreignToken).ConfigureAwait(false))
        {
            deniedStatus = (int)denied.StatusCode;
            await ObserveRecoveryAsync("foreign", deniedStatus).ConfigureAwait(false);
            Require(denied.StatusCode is HttpStatusCode.Accepted or HttpStatusCode.Forbidden or HttpStatusCode.UnprocessableEntity,
                "Unauthorized recovery returned an unexpected transport status; actual=" + deniedStatus + ".");
            if (denied.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                using JsonDocument problem = JsonDocument.Parse(await denied.Content.ReadAsByteArrayAsync(Cancellation).ConfigureAwait(false));
                Require(problem.RootElement.GetProperty("rejectionType").GetString()?.EndsWith("InsufficientPermissionsRejection", StringComparison.Ordinal) == true,
                    "The synchronous unauthorized rejection was not a permission denial.");
            }

            if (denied.StatusCode is HttpStatusCode.Accepted or HttpStatusCode.UnprocessableEntity)
            {
                await AwaitRecoveryStatusAsync(deniedId, foreignToken, "Rejected").ConfigureAwait(false);
            }
        }

        ProjectTenantAccessProjection afterDenial = await ReadRecoveryProjectionAsync().ConfigureAwait(false);
        Require(JsonSerializer.Serialize(before, Json) == JsonSerializer.Serialize(afterDenial, Json),
            "Unauthorized recovery changed persisted Projects access evidence.");
        StreamReadPage sourceBefore = await StreamAsync(TenantPlatformScopes.Tenants, "tenants", Tenant, Tenant).ConfigureAwait(false);

        string operation = Unique();
        SubmitCommandRequest command = RecoveryCommand(operation);
        int[] responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            using HttpResponseMessage response = await SendHumanAsync("api/v1/commands", command, token).ConfigureAwait(false);
            await ObserveRecoveryAsync("retry", (int)response.StatusCode).ConfigureAwait(false);
            Require(response.StatusCode is HttpStatusCode.Accepted or HttpStatusCode.Conflict,
                "Concurrent recovery retry returned an unexpected transport status; actual=" + (int)response.StatusCode + ".");
            return (int)response.StatusCode;
        })).ConfigureAwait(false);
        Require(responses.Contains((int)HttpStatusCode.Accepted), "No concurrent recovery retry was accepted.");
        await AwaitRecoveryStatusAsync(operation, token, "Completed").ConfigureAwait(false);

        StreamReadPage sourceAfter = await StreamAsync(TenantPlatformScopes.Tenants, "tenants", Tenant, Tenant).ConfigureAwait(false);
        StreamReadEvent[] appended = sourceAfter.Events.Where(item => item.SequenceNumber > sourceBefore.Metadata.LatestSequence).ToArray();
        Require(appended.Length == 1 && appended[0].EventTypeName.EndsWith(nameof(TenantAccessRefreshed), StringComparison.Ordinal),
            "Concurrent retries did not produce exactly one authoritative refresh event.");
        TenantAccessRefreshed snapshot = JsonSerializer.Deserialize<TenantAccessRefreshed>(appended[0].Payload, Json)!;
        Require(snapshot.TenantId == Tenant && snapshot.Members.Count == before.Principals.Count
            && snapshot.Members.All(pair => before.Principals.TryGetValue(pair.Key, out ProjectTenantPrincipalEvidence? member)
                && member.Role == pair.Value.ToString())
            && (snapshot.Status == Hexalith.Tenants.Contracts.Enums.TenantStatus.Active) == before.Enabled,
            "Recovery changed authoritative status, membership or roles.");

        ProjectTenantAccessProjection after = await AwaitRecoveryProjectionAsync(sourceAfter.Metadata.LatestSequence).ConfigureAwait(false);
        Require(after.Enabled == before.Enabled && after.Principals.Count == before.Principals.Count
            && after.Principals.All(pair => before.Principals.TryGetValue(pair.Key, out ProjectTenantPrincipalEvidence? prior) && prior == pair.Value)
            && after.ConfigurationKeys.SetEquals(before.ConfigurationKeys)
            && after.Version > before.Version && after.LastEventTimestamp >= before.LastEventTimestamp
            && !after.ReplayConflict && !after.MalformedEvidence,
            "Persisted recovery projection differs from existing authoritative access.");

        string laterOperation = Unique();
        Require(laterOperation != operation, "The later recovery must use a fresh operation ID.");
        using (HttpResponseMessage response = await SendHumanAsync("api/v1/commands", RecoveryCommand(laterOperation), token).ConfigureAwait(false))
        {
            await ObserveRecoveryAsync("later-operation", (int)response.StatusCode).ConfigureAwait(false);
            Require(response.StatusCode == HttpStatusCode.Accepted, "The later fresh recovery operation was not accepted.");
        }
        await AwaitRecoveryStatusAsync(laterOperation, token, "Completed").ConfigureAwait(false);
        StreamReadPage laterSource = await StreamAsync(TenantPlatformScopes.Tenants, "tenants", Tenant, Tenant).ConfigureAwait(false);
        StreamReadEvent[] laterAppended = laterSource.Events.Where(item => item.SequenceNumber > sourceAfter.Metadata.LatestSequence).ToArray();
        Require(laterAppended.Length == 1 && laterAppended[0].EventTypeName.EndsWith(nameof(TenantAccessRefreshed), StringComparison.Ordinal)
            && laterSource.Metadata.LatestSequence == sourceAfter.Metadata.LatestSequence + 1,
            "The later operation did not append exactly one new authoritative snapshot.");
        TenantAccessRefreshed laterSnapshot = JsonSerializer.Deserialize<TenantAccessRefreshed>(laterAppended[0].Payload, Json)!;
        Require(JsonSerializer.Serialize(snapshot, Json) == JsonSerializer.Serialize(laterSnapshot, Json),
            "The later recovery changed authoritative access business state.");
        ProjectTenantAccessProjection later = await AwaitRecoveryProjectionAsync(laterSource.Metadata.LatestSequence).ConfigureAwait(false);
        Require(later.Version > after.Version && later.SnapshotWatermark > after.SnapshotWatermark
            && later.Enabled == after.Enabled && later.Principals.Count == after.Principals.Count
            && later.Principals.All(pair => after.Principals.TryGetValue(pair.Key, out ProjectTenantPrincipalEvidence? prior) && prior == pair.Value)
            && later.ConfigurationKeys.SetEquals(after.ConfigurationKeys)
            && !later.ReplayConflict && !later.MalformedEvidence,
            "A fresh operation did not persist a newer exact access snapshot without resetting subscriber state.");

        int listStatus = 0;
        using (CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(Cancellation))
        {
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            while (true)
            {
                deadline.Token.ThrowIfCancellationRequested();
                using HttpResponseMessage response = await RecoveryListAsync(token, deadline.Token).ConfigureAwait(false);
                listStatus = (int)response.StatusCode;
                await ObserveRecoveryAsync("projects-list", listStatus).ConfigureAwait(false);
                deadline.Token.ThrowIfCancellationRequested();
                if (response.StatusCode == HttpStatusCode.OK) break;
                await Task.Delay(250, deadline.Token).ConfigureAwait(false);
            }
        }

        Require(listStatus == 200, "The unchanged OIDC tenant/principal did not reach Projects list HTTP 200.");
        await File.WriteAllTextAsync(Path.Combine(Evidence, "tenant-access-recovery.json"), JsonSerializer.Serialize(new
        {
            authenticatedRecovery = true, unauthorizedRejected = true, anonymousRejected = true,
            anonymousHttpStatus = 401, unauthorizedHttpStatus = deniedStatus,
            concurrentRetries = true, exactMembershipAndStatusPreserved = true, persistedProjectionVerified = true,
            sameTokenUsed = true, realOidc = true, projectsListHttpStatus = listStatus,
            staleProjectionObserved = true, preRecoveryProjectsListHttpStatus = 404,
            preRecoveryProjectionAgeSeconds = staleAge, freshnessBudgetSeconds = freshness.TotalSeconds,
            freshOperationVerified = true, snapshotEventCount = appended.Length, retryCount = responses.Length,
            sourceWatermark = laterSource.Metadata.LatestSequence, projectionWatermark = later.Watermark,
            snapshotWatermark = later.SnapshotWatermark,
            tenantSha256 = Digest(Tenant), principalSha256 = Digest(PrimaryActor),
            previousProjectionVersion = before.Version, projectionVersion = later.Version, configurationKeysPreserved = true,
            firstRecovery = RecoveryMetadata(operation, appended[0], sourceAfter, before.Version, after),
            secondRecovery = RecoveryMetadata(laterOperation, laterAppended[0], laterSource, after.Version, later),
        }, Json), Cancellation).ConfigureAwait(false);
    }

    private Task ObserveRecoveryAsync(string phase, int status)
        => AppendReadEvidenceAsync(_readEvidence, Path.Combine(Evidence, "tenant-access-recovery-observations.jsonl"),
            JsonSerializer.Serialize(new { phase, httpStatus = status, observedAt = DateTimeOffset.UtcNow }, Json) + Environment.NewLine,
            Cancellation);

    private SubmitCommandRequest RecoveryCommand(string messageId)
        => new(messageId, TenantPlatformScopes.Tenants, "tenants", Tenant, "RefreshTenantAccess",
            JsonSerializer.SerializeToElement(new { TenantId = Tenant }, Json), messageId,
            new Dictionary<string, string> { ["taskId"] = messageId });

    private async Task AwaitRecoveryStatusAsync(string messageId, string token, string expected)
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(Cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(60));
        while (!deadline.IsCancellationRequested)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, _endpoints["eventstore"] + "/api/v1/commands/status/" + messageId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            BindStatusScope(request, TenantPlatformScopes.Tenants);
            using HttpResponseMessage response = await _http.SendAsync(request, deadline.Token).ConfigureAwait(false);
            Require(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
                "Recovery command status could not be read under its explicit authenticated scope.");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(deadline.Token).ConfigureAwait(false));
                string? status = body.RootElement.GetProperty("status").GetString();
                deadline.Token.ThrowIfCancellationRequested();
                if (status == expected) return;
                Require(status is not ("Completed" or "Rejected" or "TimedOut" or "PublishFailed"),
                    "Recovery command reached an unexpected terminal outcome.");
            }

            await Task.Delay(250, deadline.Token).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Recovery command did not reach its expected terminal outcome within its deadline.");
    }

    private async Task<ProjectTenantAccessProjection> ReadRecoveryProjectionAsync(CancellationToken? cancellationToken = null)
    {
        CancellationToken token = cancellationToken ?? Cancellation;
        token.ThrowIfCancellationRequested();
        SupportedProjectReadStateSnapshot state = await StateAsync("projects:tenant-access:" + Tenant, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        Require(state.Bytes.Length > 0 && !string.IsNullOrWhiteSpace(state.ETag), "Recovery projection was not persisted.");
        using JsonDocument document = JsonDocument.Parse(state.Bytes);
        ProjectTenantAccessProjection projection = document.RootElement.GetProperty("projection").Deserialize<ProjectTenantAccessProjection>(Json)!;
        Require(document.RootElement.GetProperty("version").GetInt64() == projection.Version && projection.Version > 0,
            "The persisted recovery document and projection versions disagree.");
        return projection;
    }

    private Task<ProjectTenantAccessProjection> AwaitRecoveryProjectionAsync(long sequence)
        => ObserveRecoveryProjectionAsync(token => ReadRecoveryProjectionAsync(token), sequence, TimeSpan.FromSeconds(30), Cancellation);

    /// <summary>Propagates the private deadline into state reads and rejects results returned after expiration.</summary>
    internal static async Task<ProjectTenantAccessProjection> ObserveRecoveryProjectionAsync(
        Func<CancellationToken, Task<ProjectTenantAccessProjection>> read, long sequence, TimeSpan duration, CancellationToken cancellation)
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(duration);
        while (true)
        {
            deadline.Token.ThrowIfCancellationRequested();
            ProjectTenantAccessProjection projection = await read(deadline.Token).ConfigureAwait(false);
            deadline.Token.ThrowIfCancellationRequested();
            if (projection.Watermark == sequence && projection.SnapshotWatermark == sequence) return projection;
            Require(projection.Watermark <= sequence, "Recovery projection moved past the expected source head.");
            await Task.Delay(250, deadline.Token).ConfigureAwait(false);
        }
    }

    private async Task<double> AwaitStaleRecoveryListAsync(string token, ProjectTenantAccessProjection before, TimeSpan freshness)
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(Cancellation);
        deadline.CancelAfter(TimeSpan.FromMinutes(2));
        while (true)
        {
            deadline.Token.ThrowIfCancellationRequested();
            using HttpResponseMessage response = await RecoveryListAsync(token, deadline.Token).ConfigureAwait(false);
            await ObserveRecoveryAsync("pre-recovery-projects-list", (int)response.StatusCode).ConfigureAwait(false);
            Require(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
                "The pre-recovery Projects list failed outside the access-freshness boundary.");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                using JsonDocument denied = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(deadline.Token).ConfigureAwait(false));
                ProjectTenantAccessProjection current = await ReadRecoveryProjectionAsync(deadline.Token).ConfigureAwait(false);
                double age = (DateTimeOffset.UtcNow - current.LastEventTimestamp!.Value).TotalSeconds;
                Require(denied.RootElement.GetProperty("category").GetString() == "tenant_access_denied"
                    && age > freshness.TotalSeconds && JsonSerializer.Serialize(current, Json) == JsonSerializer.Serialize(before, Json),
                    "Projects denial was not observed against unchanged, actually stale tenant access evidence.");
                deadline.Token.ThrowIfCancellationRequested();
                return age;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), deadline.Token).ConfigureAwait(false);
        }
    }

    private async Task<HttpResponseMessage> RecoveryListAsync(string token, CancellationToken cancellation)
    {
        using HttpRequestMessage list = new(HttpMethod.Get, _endpoints["projects"] + "/api/v1/projects");
        list.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        list.Headers.Add("X-Hexalith-Tenant-Id", Tenant);
        return await _http.SendAsync(list, cancellation).ConfigureAwait(false);
    }

    private static object RecoveryMetadata(string operation, StreamReadEvent @event, StreamReadPage source,
        long priorVersion, ProjectTenantAccessProjection projection)
    {
        Require(@event.SequenceNumber > 0 && source.Metadata.LatestSequence == @event.SequenceNumber
            && projection.Watermark == @event.SequenceNumber && projection.SnapshotWatermark == @event.SequenceNumber
            && priorVersion > 0 && projection.Version > priorVersion
            && !string.IsNullOrWhiteSpace(@event.MessageId) && projection.ProcessedMessages.ContainsKey(@event.MessageId),
            "Recovery evidence is not bound to the actual appended event and persisted snapshot/version.");
        return new
        {
            operationSha256 = Digest(operation), sourceEventMessageSha256 = Digest(@event.MessageId!),
            sourceEventSequence = @event.SequenceNumber, sourceWatermark = source.Metadata.LatestSequence,
            projectionWatermark = projection.Watermark, snapshotWatermark = projection.SnapshotWatermark,
            priorProjectionVersion = priorVersion, projectionVersion = projection.Version,
            snapshotEventCount = 1, terminalStatus = "Completed",
        };
    }
}
#endif
