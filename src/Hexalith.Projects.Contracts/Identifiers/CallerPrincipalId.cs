// <copyright file="CallerPrincipalId.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Identifiers;

using System;

/// <summary>
/// Validated caller principal identity used by Projects server-side ACLs.
/// </summary>
public sealed record CallerPrincipalId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallerPrincipalId"/> class.
    /// </summary>
    /// <param name="value">The authenticated principal identifier.</param>
    public CallerPrincipalId(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "Caller principal id cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Caller principal id cannot be empty or whitespace.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the principal identifier value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}
