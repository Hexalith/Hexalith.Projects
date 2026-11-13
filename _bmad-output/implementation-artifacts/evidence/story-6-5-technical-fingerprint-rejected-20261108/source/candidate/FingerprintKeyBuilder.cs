using System.Globalization;
using System.Text;
namespace Story65.TechnicalFingerprint.Candidate;

/// <summary>Encodes complete typed snapshots by ordinal UTF-16; this key is never a JSON preimage.</summary>
internal sealed class FingerprintKeyBuilder
{
    private const int MaximumCharacters = (int)((FingerprintMemoOwner.MaximumChargedBytes - FingerprintMemoOwner.EntryCharge) / 2);
    private readonly StringBuilder _text = new();
    private bool _supported = true;
    internal FingerprintKeyBuilder(bool response)
        => Append(response ? "technical-http-response:v1;" : "technical-http-request:v1;");
    internal void Decline() => _supported = false;
    private void Append(string text)
    {
        if (!_supported) return;
        if (text.Length > MaximumCharacters - _text.Length) { _supported = false; return; }
        _text.Append(text);
    }
    internal void Count(int count) { Append("C"); Append(count.ToString(CultureInfo.InvariantCulture)); Append(";"); }
    internal void Integer(int value) { Append("I"); Append(value.ToString(CultureInfo.InvariantCulture)); Append(";"); }
    internal void NullableEnum(int? value)
    {
        if (value is null) { Append("N;"); return; }
        if (value is not (0 or 1 or 2)) { Decline(); return; }
        Append("E"); Integer(value.Value);
    }
    internal void String(string? value)
    {
        if (value is null) { Append("N;"); return; }
        Append("S"); Append(value.Length.ToString(CultureInfo.InvariantCulture)); Append(":"); Append(value); Append(";");
    }
    internal string? Complete() => _supported ? _text.ToString() : null;
}
