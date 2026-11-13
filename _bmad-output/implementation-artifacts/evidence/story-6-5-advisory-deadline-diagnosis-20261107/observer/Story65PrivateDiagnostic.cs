namespace Hexalith.Projects.Server;

using System.Text.Json;
using System.Threading;

/// <summary>Private diagnostic-build observer with no protected values.</summary>
internal static class Story65PrivateDiagnostic
{
    private static readonly object Sync = new();
    /// <summary>Independent local diagnostic ordinal, unrelated to business identity.</summary>
    internal static readonly AsyncLocal<long> Observation = new();
    /// <summary>Records only compile-time stage labels and closed outcomes.</summary>
    internal static void Stage(string source, string stage, string? outcome = null)
    {
        lock (Sync)
        {
            File.AppendAllText("/tmp/story65-advisory-diagnostic-overlay-20261107/stages.jsonl",
                JsonSerializer.Serialize(new { observation = Observation.Value, at = DateTimeOffset.UtcNow,
                    source, stage, outcome }) + Environment.NewLine);
        }
    }
}
