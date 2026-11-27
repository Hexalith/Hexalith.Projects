// <copyright file="Story71ManualTimeProvider.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Story71;

using System;

/// <summary>Deterministic advanceable <see cref="TimeProvider"/> for Story 7.1 lease and expiry tests.</summary>
public sealed class Story71ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    /// <summary>Initializes a new instance of the <see cref="Story71ManualTimeProvider"/> class.</summary>
    /// <param name="startUtc">The initial UTC timestamp.</param>
    public Story71ManualTimeProvider(DateTimeOffset startUtc)
    {
        _utcNow = startUtc.ToUniversalTime();
    }

    /// <summary>Advances the clock by <paramref name="delta"/>.</summary>
    /// <param name="delta">The positive time increment.</param>
    public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _utcNow;
}
