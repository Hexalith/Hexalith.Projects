// <copyright file="ProjectId.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Identifiers;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Opaque, validated identifier for a Project aggregate (AR-7).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ProjectId"/> performs <b>eager boundary validation</b>: it throws at construction on a
/// null, empty, or whitespace value, so there is never a silently-valid empty instance and no deferred
/// validation downstream. The underlying value is an opaque, case-sensitive string.
/// </para>
/// <para>
/// Per EventStore retro rule R2-A7, Projects/EventStore identifiers are ULID-shaped strings, NOT GUIDs:
/// validation accepts any non-whitespace string (mirroring <c>AggregateIdentity</c> aggregate-id rules)
/// and deliberately does <b>not</b> use <c>Guid.TryParse</c>.
/// </para>
/// <para>
/// AR-7 boundary decision (see <c>docs/adr/identifier-boundary.md</c>): Projects mints exactly ONE
/// value object — <see cref="ProjectId"/>. Sibling references (Conversations, Folders, Files, Memories)
/// are held as plain <see cref="string"/> (ULID) reference identifiers, reusing each owning context's
/// own representation; Projects does not invent parallel <c>ConversationId</c>/<c>FolderId</c> VOs.
/// </para>
/// <para>
/// The type carries a custom <see cref="System.Text.Json.Serialization.JsonConverter{T}"/> registered
/// via <see cref="JsonConverterAttribute"/> so it serializes as its opaque string value (never as an
/// object), keeping the wire contract stable. Kept netstandard2.0-safe because contract types feed
/// FrontComposer source generators.
/// </para>
/// </remarks>
[JsonConverter(typeof(ProjectIdJsonConverter))]
public sealed record ProjectId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectId"/> class.
    /// </summary>
    /// <param name="value">The opaque identifier value (a non-whitespace, ULID-shaped string).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or whitespace.</exception>
    public ProjectId(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "ProjectId value cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ProjectId value cannot be empty or whitespace.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying opaque identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the opaque string value of this identifier.
    /// </summary>
    /// <returns>The underlying identifier value.</returns>
    public override string ToString() => Value;
}
