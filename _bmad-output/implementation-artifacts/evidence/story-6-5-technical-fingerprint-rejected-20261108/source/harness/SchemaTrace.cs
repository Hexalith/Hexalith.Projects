using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class SchemaTrace : IJsonTypeInfoResolver
{
    private readonly DefaultJsonTypeInfoResolver _inner = new();
    internal readonly List<Type> Types = [];
    internal Type? ThrowType;
    internal Type? ExtraPropertyType;
    internal string? ExtraPropertyName;
    internal readonly InvalidOperationException Failure = new("fixed-schema-fixture-failure");
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        Types.Add(type);
        if (type == ThrowType) throw Failure;
        var info = _inner.GetTypeInfo(type, options);
        if (type == ExtraPropertyType && ExtraPropertyName is not null)
        {
            var property = info.CreateJsonPropertyInfo(typeof(string), ExtraPropertyName);
            property.Get = static _ => string.Empty;
            info.Properties.Add(property);
        }
        return info;
    }
}
