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
    internal static string Status(JsonObject state, IEnumerable<string> protectedValues)
    {
        string[] protectedText = protectedValues.ToArray();
        List<string> categories = [];
        foreach (string field in new[] { "failureReason", "rejectionEventType", "recoveryReasonCode" })
        {
            if (state[field]?.GetValueKind() != JsonValueKind.String) continue;
            string value = state[field]!.GetValue<string>();
            Match redacted = Regex.Match(value,
                @"\AProtected data diagnostic details were redacted\. ReasonCode=([A-Za-z0-9_.:-]+); Stage=([A-Za-z0-9_.:-]+)\.\z");
            if (redacted.Success)
            {
                categories.Add(field + "=" + redacted.Groups[1].Value + "/" + redacted.Groups[2].Value);
                continue;
            }
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

    internal static async Task ResourcesAsync(DistributedApplication? application)
    {
        if (application is null) return;
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        var logs = application.Services.GetRequiredService<ResourceLoggerService>();
        foreach (string resource in new[] { "eventstore", "tenants", "projects", "projects-workers", "folders", "folders-workers" })
        {
            HashSet<string> reported = [];
            try
            {
                await foreach (IReadOnlyList<LogLine> batch in logs.GetAllAsync(resource).WithCancellation(timeout.Token).ConfigureAwait(false))
                    foreach (LogLine line in batch)
                        foreach (Match match in Regex.Matches(line.Content, @"\b[A-Za-z_][A-Za-z0-9_.]*Exception\b"))
                            if (reported.Add(match.Value))
                                Console.Error.WriteLine("[package-fixtures] resource failure resource=" + resource + " exception=" + match.Value);
            }
            catch (OperationCanceledException) { Console.Error.WriteLine("[package-fixtures] resource failure diagnostics deadline reached."); return; }
        }
    }
}
