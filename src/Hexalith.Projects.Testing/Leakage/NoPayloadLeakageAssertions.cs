// <copyright file="NoPayloadLeakageAssertions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Testing.Leakage;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Reusable FS-2 <c>NoPayloadLeakage</c> guard. The <b>single</b> place every later epic asserts that a
/// serialized event, log scope, or DTO carries no forbidden sibling-owned content category from
/// <see cref="PayloadClassification.ForbiddenContent"/> (the Story 1.2 denylist source of truth). This
/// is intentionally authored as a reusable harness, not a one-off test.
/// </summary>
/// <remarks>
/// The harness serializes a value (or accepts an already-serialized string / log scope), then asserts
/// that none of the forbidden content categories appear — in both their declared PascalCase form and a
/// snake_case form — case-insensitively. It also screens for token-shaped strings and machine-local
/// absolute path prefixes. Pure: no infrastructure.
/// </remarks>
public static class NoPayloadLeakageAssertions
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly string[] HostPathPrefixes =
    [
        "C:\\", "D:\\", "E:\\", "/home/", "/Users/", "/root/", "/var/", "/etc/",
    ];

    /// <summary>
    /// Serializes <paramref name="value"/> and asserts it leaks no forbidden content.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The event/DTO to serialize and scan.</param>
    /// <exception cref="PayloadLeakageException">Thrown when a forbidden category or shape is present.</exception>
    public static void AssertNoLeakage<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        AssertNoLeakageInText(JsonSerializer.Serialize(value, SerializerOptions));
    }

    /// <summary>
    /// Asserts that an already-serialized payload / log scope text leaks no forbidden content.
    /// </summary>
    /// <param name="serialized">The serialized text (JSON, a log message, or a DTO rendering).</param>
    /// <exception cref="PayloadLeakageException">Thrown when a forbidden category or shape is present.</exception>
    public static void AssertNoLeakageInText(string serialized)
    {
        ArgumentNullException.ThrowIfNull(serialized);

        List<string> violations = [];
        string haystack = serialized.ToLowerInvariant();

        foreach (string forbidden in PayloadClassification.ForbiddenContent)
        {
            if (haystack.Contains(forbidden.ToLowerInvariant(), StringComparison.Ordinal))
            {
                violations.Add($"forbidden content category '{forbidden}'");
            }

            string snake = ToSnake(forbidden);
            if (!string.Equals(snake, forbidden, StringComparison.OrdinalIgnoreCase)
                && haystack.Contains(snake, StringComparison.Ordinal))
            {
                violations.Add($"forbidden content category '{snake}'");
            }
        }

        foreach (string prefix in HostPathPrefixes)
        {
            if (serialized.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"machine-local path prefix '{prefix}'");
            }
        }

        // JWT-shaped token (three base64url segments).
        if (System.Text.RegularExpressions.Regex.IsMatch(serialized, "eyJ[A-Za-z0-9_-]{8,}\\.[A-Za-z0-9_-]{8,}\\.[A-Za-z0-9_-]{8,}"))
        {
            violations.Add("token-shaped string");
        }

        if (serialized.Contains("-----BEGIN", StringComparison.OrdinalIgnoreCase))
        {
            violations.Add("PEM key block");
        }

        if (violations.Count > 0)
        {
            throw new PayloadLeakageException(string.Join("; ", violations));
        }
    }

    private static string ToSnake(string pascal)
    {
        StringBuilder builder = new(pascal.Length + 8);
        for (int i = 0; i < pascal.Length; i++)
        {
            char c = pascal[i];
            if (char.IsUpper(c) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}

/// <summary>Thrown by <see cref="NoPayloadLeakageAssertions"/> when a serialized value leaks forbidden content.</summary>
public sealed class PayloadLeakageException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="PayloadLeakageException"/> class.</summary>
    /// <param name="message">The metadata-only violation summary (never echoes the leaked value).</param>
    public PayloadLeakageException(string message)
        : base($"NoPayloadLeakage violation: {message}.")
    {
    }
}
