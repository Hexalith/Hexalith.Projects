// <copyright file="ProjectContextReadLimits.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Context;

/// <summary>Approved bounds for supported Project Context assembly.</summary>
public static class ProjectContextReadLimits
{
    /// <summary>Maximum candidate references a supported context query may evaluate.</summary>
    public const int MaxReferences = 5000;
}
