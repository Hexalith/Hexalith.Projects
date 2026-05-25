// <copyright file="ProjectIdJsonConverter.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Identifiers;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// <see cref="System.Text.Json"/> converter that serializes a <see cref="ProjectId"/> as its opaque
/// string value and deserializes a JSON string back into a validated <see cref="ProjectId"/>.
/// </summary>
/// <remarks>
/// The wire shape is always a plain JSON string (never an object), keeping the contract stable.
/// Deserialization re-runs <see cref="ProjectId"/> eager validation, so a null or malformed token is
/// rejected at the boundary rather than producing a silently-invalid instance.
/// </remarks>
public sealed class ProjectIdJsonConverter : JsonConverter<ProjectId>
{
    /// <inheritdoc/>
    public override ProjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a JSON string for ProjectId but found token '{reader.TokenType}'.");
        }

        string? value = reader.GetString();
        try
        {
            return new ProjectId(value!);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException($"Invalid ProjectId value: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, ProjectId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.Value);
    }
}
