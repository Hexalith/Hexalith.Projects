namespace Hexalith.Projects.Client;

using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

/// <summary>An immutable route selection evaluated once at startup, requiring explicit verified rollback.</summary>
public sealed class ProjectsReadRouteSelection
{
    private ProjectsReadRouteSelection(string mode, string? refusalReason = null)
    {
        Mode = mode;
        RefusalReason = refusalReason;
    }

    /// <summary>Gets the selected route: Supported, LegacyRollback, or Unavailable.</summary>
    public string Mode { get; }

    /// <summary>Gets a bounded, payload-free reason when startup refuses the requested route.</summary>
    public string? RefusalReason { get; }

    /// <summary>Loads bounded, hash-bound evidence; all malformed or incomplete evidence selects Unavailable.</summary>
    /// <param name="options">The single startup configuration choice and evidence binding.</param>
    /// <returns>An immutable selection without automatic fallback.</returns>
    public static ProjectsReadRouteSelection Create(ProjectsReadRoutingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Mode == "Unavailable") return new("Unavailable", "configured-unavailable");
        if (options.Mode is not ("Supported" or "LegacyRollback")) return new("Unavailable", "invalid-mode");
        if (string.IsNullOrWhiteSpace(options.EvidencePath) || string.IsNullOrWhiteSpace(options.EvidenceHash)
            || string.IsNullOrWhiteSpace(options.SourceRevision)
            || options.Mode == "Supported" && string.IsNullOrWhiteSpace(options.ContractHash)) return new("Unavailable", "missing-evidence-configuration");
        try
        {
            using FileStream file = File.OpenRead(options.EvidencePath);
            if (file.Length > 1_048_576) return new("Unavailable", "evidence-too-large");
            using var buffer = new MemoryStream();
            file.CopyTo(buffer);
            byte[] bytes = buffer.ToArray();
            if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(options.EvidenceHash, StringComparison.OrdinalIgnoreCase))
                return new("Unavailable", "evidence-hash-mismatch");
            using JsonDocument document = JsonDocument.Parse(bytes);
            if (HasDuplicateProperties(document.RootElement)) return new("Unavailable", "duplicate-evidence-property");
            bool eligible = options.Mode == "Supported"
                ? ProjectReadCutoverGate.CanActivate(document.RootElement, options.SourceRevision, options.ContractHash ?? string.Empty)
                : ProjectReadCutoverGate.CanRollback(document.RootElement, options.SourceRevision);
            if (!eligible) return new("Unavailable", options.Mode == "Supported" ? "supported-gate-rejected" : "legacy-security-rejected");
            return ValidateNativeReceipts(document.RootElement, options.EvidencePath, options.Mode)
                ? new(options.Mode) : new("Unavailable", "native-receipts-rejected");
        }
        catch (FileNotFoundException) { return new("Unavailable", "evidence-file-missing"); }
        catch (DirectoryNotFoundException) { return new("Unavailable", "evidence-directory-missing"); }
        catch (UnauthorizedAccessException) { return new("Unavailable", "evidence-access-denied"); }
        catch (JsonException) { return new("Unavailable", "invalid-evidence-json"); }
        catch (XmlException) { return new("Unavailable", "invalid-native-xml"); }
        catch (ArgumentException) { return new("Unavailable", "invalid-evidence-path"); }
        catch (IOException) { return new("Unavailable", "evidence-read-failed"); }
    }

    private static bool ValidateNativeReceipts(JsonElement evidence, string evidencePath, string mode)
    {
        if (!evidence.TryGetProperty("nativeReceipts", out JsonElement receipts) || receipts.ValueKind != JsonValueKind.Array) return false;
        string directory = Path.GetDirectoryName(Path.GetFullPath(evidencePath))!;
        var passing = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement receipt in receipts.EnumerateArray())
        {
            if (receipt.ValueKind != JsonValueKind.Object
                || !receipt.TryGetProperty("path", out JsonElement pathValue) || pathValue.ValueKind != JsonValueKind.String
                || !receipt.TryGetProperty("sha256", out JsonElement hashValue) || hashValue.ValueKind != JsonValueKind.String) return false;
            string? relative = pathValue.GetString();
            string? hash = hashValue.GetString();
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || string.IsNullOrWhiteSpace(hash)) return false;
            string path = Path.GetFullPath(Path.Combine(directory, relative));
            if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return false;
            if (!File.Exists(path)) return false;
            using FileStream file = File.OpenRead(path);
            if (file.Length > 16_777_216) return false;
            byte[] bytes;
            using (var buffer = new MemoryStream()) { file.CopyTo(buffer); bytes = buffer.ToArray(); }
            if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(hash, StringComparison.OrdinalIgnoreCase) || passing.ContainsKey(hash)) return false;
            using var stream = new MemoryStream(bytes);
            using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 16_777_216 });
            XDocument xml = XDocument.Load(reader);
            XElement[] assemblies = xml.Descendants("assembly").ToArray();
            XElement[] tests = xml.Descendants("test").ToArray();
            if (assemblies.Length == 0 || tests.Length == 0 || assemblies.Any(assembly =>
                !int.TryParse((string?)assembly.Attribute("total"), out int total) || total < 1
                || total != assembly.Descendants("test").Count()
                || !int.TryParse((string?)assembly.Attribute("passed"), out int passed) || passed != total
                || (string?)assembly.Attribute("not-run") != "0"
                || (string?)assembly.Attribute("failed") != "0" || (string?)assembly.Attribute("skipped") != "0"
                || (string?)assembly.Attribute("errors") != "0")
                || tests.Any(test => (string?)test.Attribute("result") != "Pass")) return false;
            passing.Add(hash, tests.Select(test => (string?)test.Attribute("name") ?? string.Empty).ToHashSet(StringComparer.Ordinal));
        }
        string cellsName = mode == "Supported" ? "cases" : "legacySecurity";
        foreach (JsonElement cell in evidence.GetProperty(cellsName).EnumerateArray())
        {
            if (!cell.TryGetProperty("nativeTestName", out JsonElement name) || name.ValueKind != JsonValueKind.String
                || !passing.TryGetValue(cell.GetProperty("nativeXmlHash").GetString()!, out HashSet<string>? tests)
                || !tests.Contains(name.GetString()!)) return false;
        }
        return true;
    }

    private static bool HasDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array) return element.EnumerateArray().Any(HasDuplicateProperties);
        if (element.ValueKind != JsonValueKind.Object) return false;
        var names = new HashSet<string>(StringComparer.Ordinal);
        return element.EnumerateObject().Any(property => !names.Add(property.Name) || HasDuplicateProperties(property.Value));
    }
}
