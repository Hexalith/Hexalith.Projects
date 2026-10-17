namespace Hexalith.Projects.Testing.Reads;

using System.Text.Json;

/// <summary>Strict list/open comparison after the fixture's explicit legacy-to-AD-32 mapping.</summary>
public static class ProjectReadShadowComparator
{
    /// <summary>The local normalization contract; formal owner acceptance remains a separate P4 gate.</summary>
    public const string NormalizationRevision = "story61-local-2026-10-17-v1";

    /// <summary>Compares metadata, ordering, exact watermarks, scope, and logical cursor boundaries.</summary>
    public static ProjectReadShadowComparison Compare(ProjectReadShadowObservation legacy, ProjectReadShadowObservation supported)
    {
        ArgumentNullException.ThrowIfNull(legacy);
        ArgumentNullException.ThrowIfNull(supported);
        List<string> differences = [];
        if (!legacy.ReadModelKeys.SequenceEqual(supported.ReadModelKeys, StringComparer.Ordinal)) { differences.Add("readModelKeys"); }
        if (!legacy.GlobalPositions.SequenceEqual(supported.GlobalPositions)) { differences.Add("globalPositions"); }
        if (legacy.CursorBoundary != supported.CursorBoundary) { differences.Add("cursorBoundary"); }
        if (legacy.ScopeHash != supported.ScopeHash) { differences.Add("scopeHash"); }
        CompareElement(legacy.Payload, supported.Payload, "payload", differences);
        return new(differences.Count == 0, differences, NormalizationRevision);
    }

    private static void CompareElement(JsonElement left, JsonElement right, string path, List<string> differences)
    {
        if (left.ValueKind != right.ValueKind) { differences.Add(path); return; }
        if (left.ValueKind == JsonValueKind.Object)
        {
            JsonProperty[] leftFields = left.EnumerateObject().OrderBy(field => field.Name, StringComparer.Ordinal).ToArray();
            JsonProperty[] rightFields = right.EnumerateObject().OrderBy(field => field.Name, StringComparer.Ordinal).ToArray();
            if (!leftFields.Select(field => field.Name).SequenceEqual(rightFields.Select(field => field.Name), StringComparer.Ordinal))
            {
                differences.Add(path + ".fields"); return;
            }
            for (int index = 0; index < leftFields.Length; index++)
            {
                CompareElement(leftFields[index].Value, rightFields[index].Value, path + "." + leftFields[index].Name, differences);
            }
        }
        else if (left.ValueKind == JsonValueKind.Array)
        {
            if (left.GetArrayLength() != right.GetArrayLength()) { differences.Add(path + ".length"); return; }
            for (int index = 0; index < left.GetArrayLength(); index++) { CompareElement(left[index], right[index], $"{path}[{index}]", differences); }
        }
        else if (!JsonElement.DeepEquals(left, right)) { differences.Add(path); }
    }
}
