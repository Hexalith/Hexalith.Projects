namespace Hexalith.Projects.PackageFixtures;

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Reports bounded failure identities/categories without retaining raw response or resource text.</summary>
internal static class FixtureDiagnostics
{
    /// <summary>Reports only bounded owning method identities, never exception data or source paths.</summary>
    internal static string[] FailureStages(Exception failure)
        => (new System.Diagnostics.StackTrace(failure, fNeedFileInfo: false).GetFrames() ?? [])
            .Select(frame => frame.GetMethod())
            .Where(method => method?.DeclaringType?.Assembly == typeof(FixtureDiagnostics).Assembly
                || method?.DeclaringType?.Assembly.GetName().Name == "Hexalith.Projects.Integration.Tests")
            .Select(method => method!.DeclaringType!.FullName + "." + method.Name)
            .Distinct(StringComparer.Ordinal).Take(16).ToArray();

    internal static string Status(JsonObject state, IEnumerable<string> protectedValues)
    {
        string[] protectedText = protectedValues.ToArray();
        List<string> categories = [];
        foreach (string field in new[] { "failureReason", "rejectionEventType", "recoveryReasonCode" })
        {
            if (state[field]?.GetValueKind() != JsonValueKind.String) continue;
            string value = state[field]!.GetValue<string>();
            if (field == "failureReason")
            {
                Match redacted = Regex.Match(value,
                    @"\AProtected data diagnostic details were redacted\. ReasonCode=(?<reason>protected-data-diagnostic-redacted|logical-event-read-rejected|SnapshotStreamUnavailable|readable|deferred-validation|restore-conflict|quarantine-required|operator-decision-required|missing-key|key-invalidated|provider-unavailable|provider-denied|consistency-mismatch|malformed-metadata|unknown-metadata-version|provider-opaque-unsupported|bytes-metadata-mismatch); Stage=(?<stage>unspecified|pipeline|command-status|dead-letter-publication|drain|Processing|EventsStored|EventsPublished|Rejected|PublishFailed|TimedOut|rehydrate|publish|replay|rebuild|snapshot-load|backup-admission|admin-inspection)\.\z");
                if (redacted.Success && !protectedText.Any(item => !string.IsNullOrWhiteSpace(item)
                    && (redacted.Groups["reason"].Value.Contains(item, StringComparison.Ordinal)
                        || redacted.Groups["stage"].Value.Contains(item, StringComparison.Ordinal))))
                {
                    categories.Add("failureReasonCode=" + redacted.Groups["reason"].Value);
                    categories.Add("failureStage=" + redacted.Groups["stage"].Value);
                    continue;
                }
            }
            if (Regex.IsMatch(value, @"\A[A-Za-z][A-Za-z0-9_.:-]{0,95}\z")
                && !protectedText.Any(item => !string.IsNullOrWhiteSpace(item) && value.Contains(item, StringComparison.Ordinal)))
                categories.Add(field + "=" + value);
        }
        return string.Join(',', categories);
    }

    internal static async Task ResponseAsync(HttpResponseMessage response, string operation, IEnumerable<string> protectedValues,
        CancellationToken cancellationToken)
    {
        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        byte[] buffer = new byte[16 * 1024];
        int count = 0;
        while (count < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(count), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            count += read;
        }
        string[] protectedText = protectedValues.ToArray();
        List<string> categories = [];
        try
        {
            using JsonDocument document = JsonDocument.Parse(buffer.AsMemory(0, count));
            if (document.RootElement.ValueKind == JsonValueKind.Object)
                foreach (string name in new[] { "code", "errorCode", "reason", "failureCode", "category", "phase", "rejectionEventType" })
                    if (document.RootElement.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
                        && value.GetString() is { } text && Regex.IsMatch(text, @"\A[A-Za-z][A-Za-z0-9_.:-]{0,95}\z")
                        && !protectedText.Any(item => !string.IsNullOrWhiteSpace(item) && text.Contains(item, StringComparison.Ordinal)))
                        categories.Add(name + "=" + text);
        }
        catch (JsonException) { }
        Console.Error.WriteLine($"[package-fixtures] command response operation={operation} status={(int)response.StatusCode} "
            + $"categories={string.Join(',', categories)} bodyPrefixSha256={Convert.ToHexStringLower(SHA256.HashData(buffer.AsSpan(0, count)))}");
    }

    internal static string CatalogRefusal(string content, IEnumerable<string> protectedValues)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("EventId", out JsonElement id) || id.ValueKind != JsonValueKind.Number
                || !id.TryGetInt32(out int eventId) || eventId != 5130
                || !root.TryGetProperty("State", out JsonElement state) || state.ValueKind != JsonValueKind.Object
                || !state.TryGetProperty("Reason", out JsonElement reason) || reason.ValueKind != JsonValueKind.String)
                return string.Empty;
            string? code = reason.GetString();
            if (code is not ("configuration-missing" or "owner-trust-refused" or "topology-disagreement"
                or "installed-inventory-disagreement" or "store-or-transport-unavailable" or "prior-dispatch-draining"
                or "catalog-disagreement" or "catalog-invalid" or "canceled" or "host-validation-unavailable"))
                return string.Empty;
            return protectedValues.Any(value => !string.IsNullOrWhiteSpace(value) && code.Contains(value, StringComparison.Ordinal))
                ? string.Empty : code;
        }
        catch (JsonException) { return string.Empty; }
        catch (InvalidOperationException) { return string.Empty; }
    }

    /// <summary>Extracts only a closed transport status, never its details, trailers or credentials.</summary>
    internal static string GrpcStatus(string content, IEnumerable<string> protectedValues)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("Exception", out JsonElement exception)
                && exception.ValueKind == JsonValueKind.String)
                content = exception.GetString()!;
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { return string.Empty; }
        Match match = Regex.Match(content,
            @"\bStatusCode[""']?\s*[:=]\s*[""']?(?<status>OK|Cancelled|Unknown|InvalidArgument|DeadlineExceeded|NotFound|AlreadyExists|PermissionDenied|ResourceExhausted|FailedPrecondition|Aborted|OutOfRange|Unimplemented|Internal|Unavailable|DataLoss|Unauthenticated)(?=[""'\s,)}\]]|$)");
        if (!match.Success) return string.Empty;
        string status = match.Groups["status"].Value;
        return protectedValues.Any(value => !string.IsNullOrWhiteSpace(value) && status.Contains(value, StringComparison.Ordinal))
            ? string.Empty : status;
    }

    internal static async Task ResourcesAsync(DistributedApplication? application, IEnumerable<string> protectedValues)
    {
        if (application is null) return;
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        var logs = application.Services.GetRequiredService<ResourceLoggerService>();
        var model = application.Services.GetRequiredService<DistributedApplicationModel>();
        foreach (string name in new[] { "eventstore", "tenants", "projects", "projects-workers", "folders", "folders-workers" })
        {
            IResource? resource = model.Resources.SingleOrDefault(item => item.Name == name);
            if (resource is null) continue;
            HashSet<string> reported = [];
            int examined = 0;
            try
            {
                await foreach (IReadOnlyList<LogLine> batch in logs.GetAllAsync(resource).WithCancellation(timeout.Token).ConfigureAwait(false))
                    foreach (LogLine line in batch)
                    {
                        examined++;
                        string refusal = CatalogRefusal(line.Content, protectedValues);
                        if (refusal.Length > 0 && reported.Add("catalog:" + refusal))
                            Console.Error.WriteLine("[package-fixtures] resource failure resource=" + name + " catalogReason=" + refusal);
                        string status = GrpcStatus(line.Content, protectedValues);
                        if (status.Length > 0 && reported.Add("grpc:" + status))
                            Console.Error.WriteLine("[package-fixtures] resource failure resource=" + name + " grpcStatus=" + status);
                        foreach (Match match in Regex.Matches(line.Content, @"\b[A-Za-z_][A-Za-z0-9_.]*Exception\b"))
                            if (reported.Add(match.Value))
                                Console.Error.WriteLine("[package-fixtures] resource failure resource=" + name + " exception=" + match.Value);
                    }
                Console.Error.WriteLine("[package-fixtures] resource failure resource=" + name + " examinedLines=" + examined);
            }
            catch (OperationCanceledException) { Console.Error.WriteLine("[package-fixtures] resource failure diagnostics deadline reached."); return; }
        }
    }
}
