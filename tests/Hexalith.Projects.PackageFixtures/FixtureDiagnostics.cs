namespace Hexalith.Projects.PackageFixtures;

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Reports bounded failure identities/categories without retaining raw response or resource text.</summary>
internal static class FixtureDiagnostics
{
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
