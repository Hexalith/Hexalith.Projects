namespace Hexalith.Projects.PackageFixtures;

using System.Text.RegularExpressions;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Checks the complete browser interval for protected fixture text without retaining raw resource logs.</summary>
internal sealed class FixtureTelemetry(DistributedApplication application, IEnumerable<string> resources)
{
    private readonly ResourceLoggerService _logs = application.Services.GetRequiredService<ResourceLoggerService>();
    private readonly string[] _resources = resources.ToArray();
    private readonly Dictionary<string, int> _before = [];

    internal async Task BeginAsync(CancellationToken cancellationToken)
    {
        foreach (string resource in _resources)
        {
            int last = 0;
            await foreach (IReadOnlyList<LogLine> batch in _logs.GetAllAsync(resource).WithCancellation(cancellationToken).ConfigureAwait(false))
                foreach (LogLine line in batch) last = Math.Max(last, line.LineNumber);
            _before.Add(resource, last);
        }
    }

    internal async Task VerifyAsync(IEnumerable<string> protectedValues, CancellationToken cancellationToken)
    {
        string[] markers = protectedValues.Where(value => !string.IsNullOrWhiteSpace(value)).Concat(
            ["Synthetic project", "Synthetic descriptive metadata", "Restricted legacy", "Foreign synthetic name"]).ToArray();
        foreach (string resource in _resources)
        {
            SortedSet<int> observed = [];
            await foreach (IReadOnlyList<LogLine> batch in _logs.GetAllAsync(resource).WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                foreach (LogLine line in batch.Where(line => line.LineNumber > _before[resource]))
                {
                    if (markers.Any(marker => line.Content.Contains(marker, StringComparison.Ordinal))
                        || Regex.IsMatch(line.Content, @"eyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}"))
                        throw new InvalidOperationException("Protected fixture data appeared in resource telemetry: " + resource);
                    observed.Add(line.LineNumber);
                }
            }
            if (observed.Count > 0 && (observed.Min != _before[resource] + 1 || observed.Max - observed.Min + 1 != observed.Count))
                throw new InvalidOperationException("Complete browser telemetry interval is unavailable: " + resource);
            await application.ResourceNotifications.WaitForResourceHealthyAsync(resource, cancellationToken).ConfigureAwait(false);
        }
    }
}
