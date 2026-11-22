namespace Hexalith.Projects.Client;

using System.Text.Json;

/// <summary>Validates exhaustive, revision-bound evidence before primary Web selection.</summary>
public static class ProjectReadCutoverGate
{
    /// <summary>The sole approved normalization revision for this cutover.</summary>
    public const string NormalizationRevision = "story67-read-normalization-2026-11-22-v4";

    /// <summary>The seven delivered reads, excluding retained legacy-only operations.</summary>
    public static IReadOnlyList<string> Operations { get; } = Array.AsReadOnly(new[]
    {
        "list", "open", "conversation-start", "context", "explain", "resolve-conversation", "resolve-attachments",
    });

    /// <summary>Required independently executed cells for every read, including rejection of unsupported cursors.</summary>
    public static IReadOnlyList<string> Scenarios { get; } = Array.AsReadOnly(new[]
    {
        "complete", "partial", "unavailable", "absent-denied", "foreign-tenant", "stale-authority",
        "historical-pending-folderless", "cursor-scope-continuation-expiry", "descriptive-audit",
        "conversation-binding", "no-domain-writes",
    });

    /// <summary>Checks every required cell; missing, duplicate, skipped and mismatched cells fail closed.</summary>
    /// <param name="evidence">An externally retained gate receipt.</param>
    /// <param name="revision">The exact application source revision being activated.</param>
    /// <param name="contractHash">The exact contract fingerprint being activated.</param>
    /// <returns>Whether the complete supported route is eligible.</returns>
    public static bool CanActivate(JsonElement evidence, string revision, string contractHash)
    {
        if (!HasString(evidence, "normalizationRevision", NormalizationRevision)
            || !HasString(evidence, "sourceRevision", revision) || !IsDigest(revision, 40)
            || !HasString(evidence, "contractHash", contractHash) || !IsDigest(contractHash, 64)
            || !HasTrue(evidence, "contractParity") || !HasTrue(evidence, "identityCompatibility")
            || !evidence.TryGetProperty("cases", out JsonElement cases) || cases.ValueKind != JsonValueKind.Array)
            return false;

        var required = Operations.SelectMany(operation => Scenarios.Select(scenario => operation + "/" + scenario)).ToHashSet(StringComparer.Ordinal);
        foreach (JsonElement cell in cases.EnumerateArray())
        {
            if (!TryString(cell, "operation", out string? operation) || !TryString(cell, "scenario", out string? scenario)
                || !required.Remove(operation + "/" + scenario)
                || !HasTrue(cell, "executed") || !HasTrue(cell, "passed")
                || !cell.TryGetProperty("skipped", out JsonElement skipped) || skipped.ValueKind != JsonValueKind.False
                || !HasTrue(cell, "canonicalOutput") || !HasTrue(cell, "independentKeys")
                || !HasTrue(cell, "exactPositions") || !HasTrue(cell, "cursorScope")
                || !HasTrue(cell, "isolation") || !HasTrue(cell, "supportedOnlyAssertions") || !HasTrue(cell, "noDomainWrites")
                || !TryString(cell, "nativeXmlHash", out string? xmlHash) || !IsDigest(xmlHash, 64)) return false;
        }
        return required.Count == 0;
    }

    /// <summary>Checks the separate security receipt required for an explicit legacy rollback.</summary>
    /// <param name="evidence">The rollback security receipt.</param>
    /// <param name="revision">The exact application revision.</param>
    /// <returns>Whether all seven legacy slices have current security and persisted no-write evidence.</returns>
    public static bool CanRollback(JsonElement evidence, string revision)
    {
        if (!HasString(evidence, "sourceRevision", revision) || !IsDigest(revision, 40)
            || !evidence.TryGetProperty("legacySecurity", out JsonElement slices) || slices.ValueKind != JsonValueKind.Array)
            return false;
        var required = Operations.ToHashSet(StringComparer.Ordinal);
        foreach (JsonElement slice in slices.EnumerateArray())
        {
            if (!TryString(slice, "operation", out string? operation) || !required.Remove(operation!)
                || !HasTrue(slice, "executed") || !HasTrue(slice, "passed")
                || !HasTrue(slice, "currentAuthority") || !HasTrue(slice, "safeMetadata")
                || !HasTrue(slice, "safeDenial") || !HasTrue(slice, "noDomainWrites")
                || !TryString(slice, "nativeXmlHash", out string? xmlHash) || !IsDigest(xmlHash, 64)) return false;
        }
        return required.Count == 0;
    }

    private static bool HasString(JsonElement value, string name, string expected)
        => TryString(value, name, out string? actual) && string.Equals(actual, expected, StringComparison.Ordinal);

    private static bool TryString(JsonElement value, string name, out string? result)
    {
        result = null;
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(name, out JsonElement item) || item.ValueKind != JsonValueKind.String) return false;
        result = item.GetString();
        return !string.IsNullOrWhiteSpace(result);
    }

    private static bool HasTrue(JsonElement value, string name)
        => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out JsonElement item) && item.ValueKind == JsonValueKind.True;

    private static bool IsDigest(string? value, int length)
        => value?.Length == length && value.All(Uri.IsHexDigit);
}
