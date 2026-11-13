using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Story65.ImageMemo.Harness;

/// <summary>Requires the exact future owner/native-qualified production generation with no historical SDK fallback.</summary>
internal static class FinalTransferInputs
{
    private static Dictionary<string, string>? _inputs;

    internal static string SdkSha256 { get; private set; } = string.Empty;

    internal static object Load(string path, string expectedBindingSha256)
    {
        Require.That(_inputs is null, "final-binding-one-generation");
        Require.That(Regex.IsMatch(expectedBindingSha256, "^[0-9a-f]{64}$") && Hash(path) == expectedBindingSha256, "final-binding-sha");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement root = document.RootElement;
        Require.That(root.GetProperty("sourcePinsReady").GetBoolean()
            && root.GetProperty("rootOwnerAndNativeQualificationPassed").GetBoolean(), "actual-native-qualified-binding-required");
        Require.That(root.GetProperty("compiledProductionMemoGate").GetString() == "System.Threading.ReaderWriterLockSlim", "native-qualified-rwl-gate-required");
        SdkSha256 = root.GetProperty("sdkDllSha256").GetString() ?? string.Empty;
        Require.That(Regex.IsMatch(SdkSha256, "^[0-9a-f]{64}$"), "qualified-production-sdk-sha-required");
        _inputs = root.GetProperty("currentInputsSha256").Deserialize<Dictionary<string, string>>()
            ?? throw new ParityFailure("qualified-current-input-map-required");
        Require.That(_inputs.Count > 0, "qualified-current-input-map-nonempty");
        VerifyUnchanged();
        Require.That(Hash(root.GetProperty("sdkDllPath").GetString()!) == SdkSha256, "actual-production-producer-matches-native-proof");
        return new
        {
            BindingSha256 = expectedBindingSha256,
            ActualCompiledProductionSdkSha256 = SdkSha256,
            OwnerReceiptSha256 = root.GetProperty("ownerReceiptSha256").GetString(),
            NativeAuditSha256 = root.GetProperty("nativeAuditSha256").GetString(),
            SourceControlsSha256 = root.GetProperty("sourceControlsSha256").GetString(),
            SourceCount = root.GetProperty("sourceCount").GetInt32(),
            NativeQualifiedUniquePasses = root.GetProperty("nativeQualifiedUniquePasses").GetInt32(),
            CurrentPinnedInputs = _inputs.Count,
            ActualSourceAndNativeProofRequiredNoHistoricalFallback = true,
        };
    }

    internal static void VerifyUnchanged()
    {
        Require.That(_inputs is not null, "final-binding-not-initialized");
        foreach ((string path, string expected) in _inputs!)
        {
            Require.That(Path.IsPathFullyQualified(path) && Regex.IsMatch(expected, "^[0-9a-f]{64}$"), "qualified-input-coordinate-and-hash");
            Require.That((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0 && Hash(path) == expected, "qualified-input-unchanged");
        }
    }

    private static string Hash(string path)
    {
        using FileStream file = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(file));
    }
}
