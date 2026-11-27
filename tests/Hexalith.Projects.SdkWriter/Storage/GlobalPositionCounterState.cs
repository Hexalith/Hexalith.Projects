// <copyright file="GlobalPositionCounterState.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Storage;

/// <summary>Shared-store monotonic global position counter persisted under CAS.</summary>
/// <param name="LastAllocated">The highest allocated global position (&gt;= 1 once initialized).</param>
public sealed record GlobalPositionCounterState(long LastAllocated);
