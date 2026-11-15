namespace Hexalith.Projects.Contracts.Queries;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Preserves the Conversations-owned conv: wire prefix without importing its Contracts.</summary>
internal sealed class ProjectConversationIdJsonConverter : JsonConverter<string>
{
    /// <inheritdoc/>
    public override bool HandleNull => true;

    /// <inheritdoc/>
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Conversation identifier must be a prefixed string.");
        }

        string raw = reader.GetString()!;
        if (!raw.StartsWith("conv:", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(raw[5..]))
        {
            throw new JsonException("Conversation identifier must contain a nonblank conv: reference.");
        }

        return raw[5..];
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        writer.WriteStringValue("conv:" + value);
    }
}
