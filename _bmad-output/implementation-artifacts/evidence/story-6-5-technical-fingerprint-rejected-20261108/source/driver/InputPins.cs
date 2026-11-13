using System.Security.Cryptography;
using System.Text.Json;

namespace Story65.TechnicalFingerprint.Driver;

/// <summary>Requires the separately released exact source and built-runtime closure without historical SDK fallbacks.</summary>
internal static class InputPins
{
    internal const string ApiSha256 = "5022a61a34aa132e7195c0729f6acd8b4b9ee9948b47002b53fa99e9747bbe13";
    internal static string SdkSha256 { get; private set; } = string.Empty;
    internal static string ContractsSha256 { get; private set; } = string.Empty;

    internal static string Sha(string path)
    {
        Check.That(File.Exists(path) && string.IsNullOrEmpty(new FileInfo(path).LinkTarget), "regular-pinned-input");
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    internal static void Verify(string path, string expected)
    {
        Check.That(expected.Length == 64 && Sha(path) == expected, "runtime-manifest-hash");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement manifest = document.RootElement;
        Check.That(manifest.GetProperty("executionReleased").GetBoolean(), "separate-root-runtime-release");
        // Source root is supplied by the actual separately reviewed handoff, never guessed from old artifacts.
        string preparedRoot = manifest.GetProperty("preparationRoot").GetString()!;
        Check.That(Path.IsPathFullyQualified(preparedRoot), "absolute-preparation-root");
        Check.That(Sha(Path.Combine(preparedRoot, "fixture-api.json")) == ApiSha256, "frozen-fixture-api");
        SdkSha256 = manifest.GetProperty("sdkSha256").GetString()!;
        ContractsSha256 = manifest.GetProperty("contractsSha256").GetString()!;
        Check.That(SdkSha256.Length == 64 && ContractsSha256.Length == 64, "current-owner-hashes-required");
        Check.That(Sha(manifest.GetProperty("sdkPath").GetString()!) == SdkSha256, "current-sdk-source-handoff");
        Check.That(Sha(manifest.GetProperty("contractsPath").GetString()!) == ContractsSha256, "current-contracts-source-handoff");
        foreach (JsonProperty input in manifest.GetProperty("filesSha256").EnumerateObject())
        {
            Check.That(!Path.IsPathFullyQualified(input.Name), "relative-prepared-coordinate");
            string resolved = Path.GetFullPath(Path.Combine(preparedRoot, input.Name));
            Check.That(resolved.StartsWith(preparedRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal), "contained-prepared-input");
            Check.That(Sha(resolved) == input.Value.GetString(), "frozen-prepared-input");
        }
        foreach (JsonProperty input in manifest.GetProperty("inputsSha256").EnumerateObject())
        {
            Check.That(Path.IsPathFullyQualified(input.Name), "absolute-external-input");
            Check.That(Sha(input.Name) == input.Value.GetString(), "frozen-current-source-rawref");
        }
        JsonProperty[] built = manifest.GetProperty("compiledInputsSha256").EnumerateObject().ToArray();
        Check.That(built.Length >= 4 && built.Any(input => input.Name == typeof(InputPins).Assembly.Location), "actual-built-driver-runtime-closure-required");
        foreach (JsonProperty input in built)
        {
            Check.That(Path.IsPathFullyQualified(input.Name) && Sha(input.Name) == input.Value.GetString(), "frozen-compiled-input");
        }
    }
}
