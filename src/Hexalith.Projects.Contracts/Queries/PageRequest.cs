// <copyright file="PageRequest.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System;

/// <summary>
/// Projects-owned bounded page request for read-side ACL queries.
/// </summary>
/// <param name="PageSize">The requested page size, from 1 to 100.</param>
/// <param name="ContinuationCursor">The opaque continuation cursor from a previous authorized page.</param>
public sealed record PageRequest(
    int PageSize = 25,
    string? ContinuationCursor = null)
{
    /// <summary>
    /// Gets the requested page size.
    /// </summary>
    public int PageSize { get; } = ValidatePageSize(PageSize);

    /// <summary>
    /// Gets the opaque continuation cursor.
    /// </summary>
    public string? ContinuationCursor { get; } = string.IsNullOrWhiteSpace(ContinuationCursor)
        ? null
        : ContinuationCursor;

    private static int ValidatePageSize(int value)
    {
        if (value is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Page size must be between 1 and 100.");
        }

        return value;
    }
}
