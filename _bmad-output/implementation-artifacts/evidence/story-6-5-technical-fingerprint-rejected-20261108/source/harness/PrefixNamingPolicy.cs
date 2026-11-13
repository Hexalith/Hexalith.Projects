using System.Text.Json;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class PrefixNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => "changed_" + name;
}
