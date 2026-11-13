using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class TrackedJsonOptions : IOptions<JsonOptions>
{
    internal readonly JsonOptions Options = new();
    internal readonly SchemaTrace Trace = new();
    internal int Reads;
    internal TrackedJsonOptions() => Options.JsonSerializerOptions.TypeInfoResolver = Trace;
    public JsonOptions Value { get { Reads++; return Options; } }
}
