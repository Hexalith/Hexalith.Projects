namespace Hexalith.Projects.Server;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Shares the command payload contract between processing and idempotency admission.</summary>
internal static class ProjectCommandPayloadJson
{
    /// <summary>Gets the existing strict payload settings, omitting null fields when writing.</summary>
    internal static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };
}
