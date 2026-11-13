using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace Story65.ImageMemo.Harness;
internal static class FinalTransferInputs
{
    private static Dictionary<string, string>? _inputs;
    internal static string SdkSha256 { get; private set; } = string.Empty;
    internal static object Load(string path, string expected)
    {
        Require.That(_inputs is null && Regex.IsMatch(expected, "^[0-9a-f]{64}$") && Hash(path) == expected, "exact-current-input-manifest");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement root = document.RootElement;
        Require.That(root.GetProperty("executionReleased").GetBoolean(), "root-release-required");
        SdkSha256 = root.GetProperty("sdkSha256").GetString()!;
        Require.That(SdkSha256 == "6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542", "actual-sdk6947-required");
        _inputs = root.GetProperty("inputsSha256").Deserialize<Dictionary<string,string>>()!;
        Require.That(_inputs.Count > 0, "current-inputs-nonempty"); VerifyUnchanged();
        return new { ManifestSha256 = expected, ActualSdkSha256 = SdkSha256, CurrentInputCount = _inputs.Count,
            NoProductSourceChangesOrHostAcceptanceClaim = true };
    }
    internal static void VerifyUnchanged()
    {
        Require.That(_inputs is not null, "inputs-bound");
        foreach ((string path,string digest) in _inputs!)
            Require.That(Path.IsPathFullyQualified(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0 && Hash(path) == digest, "current-input-unchanged");
    }
    private static string Hash(string path)
    { using FileStream file = File.OpenRead(path); return Convert.ToHexStringLower(SHA256.HashData(file)); }
}
