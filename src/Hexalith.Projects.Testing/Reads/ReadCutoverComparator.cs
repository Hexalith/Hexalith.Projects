namespace Hexalith.Projects.Testing.Reads;

using System.Text.Json;

/// <summary>Strict Story 6.7 comparison; callers must retain proof for each conditional normalization.</summary>
public static class ReadCutoverComparator
{
    /// <summary>The approved rules, independent of any passing acceptance receipt.</summary>
    public const string NormalizationRevision = "story67-read-normalization-2026-11-16-v3";

    /// <summary>Compares output, exact keys/positions, cursor behavior and independent supported assertions.</summary>
    /// <param name="legacy">The authenticated legacy observation.</param>
    /// <param name="supported">The authenticated supported observation.</param>
    /// <returns>Payload-free difference labels only.</returns>
    public static IReadOnlyList<string> Compare(ReadCutoverObservation legacy, ReadCutoverObservation supported)
    {
        ArgumentNullException.ThrowIfNull(legacy);
        ArgumentNullException.ThrowIfNull(supported);
        List<string> differences = [];
        if (!ValidKeys(legacy) || !ValidKeys(supported)
            || !legacy.AggregateIdentities.SequenceEqual(supported.AggregateIdentities, StringComparer.Ordinal)) differences.Add("keys");
        if (!legacy.Positions.SequenceEqual(supported.Positions)) differences.Add("exact-positions");
        if (legacy.SourceGlobalPositions is null || supported.SourceGlobalPositions is null
            || !legacy.Positions.SequenceEqual(legacy.SourceGlobalPositions)
            || !supported.Positions.SequenceEqual(supported.SourceGlobalPositions)) differences.Add("original-global-positions");
        if (legacy.SourceSequenceNumbers is null || supported.SourceSequenceNumbers is null || supported.ProjectVersions is null
            || !legacy.SourceSequenceNumbers.SequenceEqual(supported.SourceSequenceNumbers)
            || supported.SourceSequenceNumbers.Count != supported.Positions.Count
            || !supported.ProjectVersions.SequenceEqual(supported.SourceSequenceNumbers)) differences.Add("original-project-versions");
        if (!Equivalent(legacy.CanonicalPayload, supported.CanonicalPayload)) differences.Add("canonical-output");
        if (legacy.TransportCursorAssertions is not null || supported.TransportCursorAssertions is not null)
        {
            if (legacy.TransportCursorAssertions != true || supported.TransportCursorAssertions != true) differences.Add("cursor-scope-behavior");
        }
        else if (!Equivalent(legacy.CursorEvidence, supported.CursorEvidence)) differences.Add("cursor-scope-behavior");
        if (legacy.PhysicalAddresses is not null || supported.PhysicalAddresses is not null)
        {
            if (!ValidAddresses(legacy.PhysicalAddresses) || !ValidAddresses(supported.PhysicalAddresses)) differences.Add("physical-addresses");
        }
        if (!supported.SupportedOnlyAssertions) differences.Add("supported-only-assertions");
        return differences.AsReadOnly();
    }

    /// <summary>Checks only the approved finite unavailable pairs; raw responses remain caller-owned evidence.</summary>
    public static bool IsApprovedUnavailablePair(string operation, string cause, int legacyStatus, JsonElement legacyBody,
        int supportedStatus, JsonElement supportedBody, bool independentAuthorityAndNondisclosure, bool completeSupportedAssertions)
    {
        if (!independentAuthorityAndNondisclosure || !completeSupportedAssertions || legacyStatus != 503 || supportedStatus != 200
            || legacyBody.ValueKind != JsonValueKind.Object || !legacyBody.TryGetProperty("code", out var code)
            || code.ValueKind != JsonValueKind.String || code.GetString() != "read_model_unavailable") return false;
        bool context = operation is "context" or "explain" or "conversation-start";
        bool advisory = operation is "resolve-conversation" or "resolve-attachments";
        if (cause is "native-binding-unavailable" or "pending-folder-materialization" && !context
            || cause == "stale-tenant" && !context && !advisory
            || cause is "unsupported-file" or "degraded-advisory" && !advisory
            || cause is not ("stale-tenant" or "unsupported-file" or "degraded-advisory" or "native-binding-unavailable" or "pending-folder-materialization")) return false;
        if (context)
        {
            if (operation == "explain")
            {
                if (!supportedBody.TryGetProperty("context", out var nested)) return false;
                supportedBody = nested;
            }
            return supportedBody.TryGetProperty("setup", out var setup) && setup.ValueKind == JsonValueKind.Null
                && supportedBody.TryGetProperty("snapshot", out var snapshot)
                && snapshot.TryGetProperty("responseState", out var state) && state.ValueKind == JsonValueKind.String && state.GetString() == "Unavailable"
                && snapshot.TryGetProperty("components", out var components) && components.ValueKind == JsonValueKind.Array && components.GetArrayLength() >= 4
                && snapshot.TryGetProperty("recoveryActions", out var recovery) && recovery.ValueKind == JsonValueKind.Array && recovery.GetArrayLength() > 0
                && (cause != "pending-folder-materialization" || recovery.EnumerateArray().Select(item => item.GetString()).SequenceEqual(["PollTask"]));
        }
        return advisory && supportedBody.TryGetProperty("incomplete", out var incomplete) && incomplete.ValueKind == JsonValueKind.True
            && supportedBody.TryGetProperty("result", out var result)
            && supportedBody.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array
            && (result.ValueKind == JsonValueKind.Null && candidates.GetArrayLength() == 0
                || cause == "unsupported-file" && operation == "resolve-attachments" && result.ValueKind == JsonValueKind.String
                    && result.GetString() == (candidates.GetArrayLength() == 0 ? "NoMatch" : candidates.GetArrayLength() == 1 ? "SingleCandidate" : "MultipleCandidates")
                    && candidates.EnumerateArray().All(candidate => candidate.ValueKind == JsonValueKind.Object
                        && candidate.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)
                            .SequenceEqual(new[] { "projectId", "reasonCodes", "rank", "score" }.Order(StringComparer.Ordinal))
                        && candidate.GetProperty("projectId").ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(candidate.GetProperty("projectId").GetString())
                        && candidate.GetProperty("reasonCodes").ValueKind == JsonValueKind.Array
                        && candidate.GetProperty("rank").TryGetInt32(out int rank) && rank > 0
                        && candidate.GetProperty("score").TryGetInt32(out int score) && score >= 0))
            && supportedBody.TryGetProperty("unsupportedInputKinds", out var kinds) && kinds.ValueKind == JsonValueKind.Array
            && (cause != "unsupported-file" || kinds.EnumerateArray().Select(kind => kind.GetString()).SequenceEqual(["file"]));
    }

    private static bool ValidAddresses(IReadOnlyList<string>? addresses)
        => addresses is { Count: > 0 } && addresses.All(address => !string.IsNullOrWhiteSpace(address) && address.Contains('/'))
            && addresses.Distinct(StringComparer.Ordinal).Count() == addresses.Count;

    private static bool ValidKeys(ReadCutoverObservation observation)
        => observation.RawKeys.Count == observation.AggregateIdentities.Count
            && observation.RawKeys.Count == observation.Positions.Count
            && observation.RawKeys.All(key => !string.IsNullOrWhiteSpace(key))
            && observation.AggregateIdentities.All(key => !string.IsNullOrWhiteSpace(key))
            && observation.RawKeys.Distinct(StringComparer.Ordinal).Count() == observation.RawKeys.Count
            && observation.AggregateIdentities.Distinct(StringComparer.Ordinal).Count() == observation.AggregateIdentities.Count;

    private static bool Equivalent(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind) return false;
        if (left.ValueKind == JsonValueKind.Object)
        {
            JsonProperty[] a = left.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal).ToArray();
            JsonProperty[] b = right.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal).ToArray();
            return a.Length == b.Length
                && a.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == a.Length
                && b.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == b.Length
                && a.Zip(b).All(pair => pair.First.Name == pair.Second.Name && Equivalent(pair.First.Value, pair.Second.Value));
        }
        if (left.ValueKind == JsonValueKind.Array)
            return left.GetArrayLength() == right.GetArrayLength() && left.EnumerateArray().Zip(right.EnumerateArray()).All(pair => Equivalent(pair.First, pair.Second));
        return JsonElement.DeepEquals(left, right);
    }
}
