#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

/// <summary>Defines the finite independent native qualification scenarios without composing provider state.</summary>
internal static class SupportedProjectReadScenarioEvidence
{
    /// <summary>Requires complete provider transport verification before persisting the native scenario receipt.</summary>
    internal static async Task WriteAfterProviderVerificationAsync(string path, string scenario, IReadOnlyList<string> completed,
        string isolation, Func<Task> verifyProvider, CancellationToken cancellation)
    {
        JsonElement receipt = Create(scenario, completed, isolation);
        cancellation.ThrowIfCancellationRequested();
        await verifyProvider().ConfigureAwait(false);
        cancellation.ThrowIfCancellationRequested();
        await File.WriteAllTextAsync(path, receipt.GetRawText(), cancellation).ConfigureAwait(false);
    }

    /// <summary>Returns a new copy of the exact ordered cases required by the selected scenario.</summary>
    internal static string[] Cases(string scenario) => scenario switch
    {
        "duplicate" => ["duplicate-project-delivery-retains-supported-reads-and-state"],
        "full-replay" => ["full-project-replay-retains-truth-and-invalidates-old-continuation"],
        "main" => ["authorized-list-continuation-open", "public-user-and-workload-trust-remain-separate",
            "populated-foreign-tenant-and-absent-safe-denial", "owned-projects-process-restart-retains-reads-continuation-and-state",
            "inspection-permission-and-durable-metadata-only-audit", "audit-storage-unavailable-blocks-disclosure",
            "revocation-observes-real-head-and-invalidates-old-continuation", "owned-folder-outage-honest-unavailability-and-recovery",
            "authenticated-command-and-projection-compatibility"],
        _ => throw new InvalidOperationException("Unknown supported-read qualification scenario."),
    };

    /// <summary>Creates a scenario receipt only after every exact case completed in its native fixture.</summary>
    internal static JsonElement Create(string scenario, IReadOnlyList<string> completed, string isolation)
    {
        if (!Regex.IsMatch(isolation, "^reads-[0-9a-f]{24}$", RegexOptions.CultureInvariant))
            throw new InvalidOperationException("The scenario requires its actual owned isolation identity.");
        if (!completed.SequenceEqual(Cases(scenario), StringComparer.Ordinal))
            throw new InvalidOperationException("The native scenario cases are incomplete or out of order.");
        bool main = scenario == "main";
        return JsonSerializer.SerializeToElement(new
        {
            version = 2, qualificationGroup = scenario, providerIsolationSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(isolation))), allGroupCasesExecuted = true, cases = completed,
            providerScope = "one-fresh-owned-runtime", readOnlyDomainBytesAndEtagsUnchanged = true,
            projectsProcessRestartVerified = main, duplicateDeliveryVerified = scenario == "duplicate", fullReplayVerified = scenario == "full-replay",
            preRestartContinuationAcceptedAfterRestart = main, freshPagingAgrees = true,
            retainedCursorAcceptedImmediatelyBeforeRevocation = main, cursorRejectionWithinLifetime = main,
            postRestartForeignAndAbsentDenialsVerified = main, bothTenantDomainWatermarksUnchanged = true,
            actualDaprDiscovery = true, actualOwnerActorHeads = true, acceptedEquivalence = false, productionReadiness = false,
        }, JsonSerializerOptions.Web);
    }
}
#endif
