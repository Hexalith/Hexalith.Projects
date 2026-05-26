// <copyright file="ProjectConversationPageMetadata.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System;

/// <summary>
/// Projects-owned page metadata for project conversation references.
/// </summary>
/// <param name="ReturnedCount">The number of items visible in this authorized page.</param>
/// <param name="ContinuationCursor">An opaque continuation cursor for the next page.</param>
public sealed record ProjectConversationPageMetadata(
    int ReturnedCount,
    string? ContinuationCursor = null)
{
    /// <summary>
    /// Gets the number of items visible in this authorized page.
    /// </summary>
    public int ReturnedCount { get; } = ValidateReturnedCount(ReturnedCount);

    /// <summary>
    /// Gets an opaque continuation cursor for the next page.
    /// </summary>
    public string? ContinuationCursor { get; } = string.IsNullOrWhiteSpace(ContinuationCursor)
        ? null
        : ContinuationCursor;

    private static int ValidateReturnedCount(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        return value;
    }
}
