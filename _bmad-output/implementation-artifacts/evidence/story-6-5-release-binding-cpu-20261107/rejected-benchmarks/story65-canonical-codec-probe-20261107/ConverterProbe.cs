using System.Text.Json.Serialization;
[JsonConverter(typeof(CountingProbeConverter))]
sealed class ConverterProbe { public int Writes; public bool Fail; }
