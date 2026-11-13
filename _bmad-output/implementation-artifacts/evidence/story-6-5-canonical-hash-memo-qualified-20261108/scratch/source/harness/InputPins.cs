using System.Text.Json;
namespace Story65.Canonical.Harness;
/// <summary>Rehashes exact Root-reviewed current inputs, with no historical SDK fallback.</summary>
internal static class InputPins
{
    internal static void Verify(string path,string expected)
    {
        Check.That(expected.Length==64 && Datasets.Sha(path)==expected,"runtime-manifest-hash");
        using var doc=JsonDocument.Parse(File.ReadAllBytes(path));
        var root=doc.RootElement;
        Check.That(root.GetProperty("executionReleased").GetBoolean(),"separate-runtime-release");
        foreach(var item in root.GetProperty("inputsSha256").EnumerateObject())
        {
            Check.That(Path.IsPathFullyQualified(item.Name) && string.IsNullOrEmpty(new FileInfo(item.Name).LinkTarget),"absolute-regular-input-coordinate");
            Check.That(File.Exists(item.Name) && Datasets.Sha(item.Name)==item.Value.GetString(),"frozen-current-input");
        }
        Check.That(root.GetProperty("compiledInputsSha256").EnumerateObject().Count()>=4,"compiled-runtime-closure-required");
        foreach(var item in root.GetProperty("compiledInputsSha256").EnumerateObject())
            Check.That(File.Exists(item.Name) && Datasets.Sha(item.Name)==item.Value.GetString(),"frozen-compiled-input");
    }
}
