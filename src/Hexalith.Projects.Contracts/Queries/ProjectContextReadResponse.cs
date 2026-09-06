// <copyright file="ProjectContextReadResponse.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System.Collections.Generic;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Supported metadata-only Project Context read response.</summary>
/// <param name="ProjectId">The opaque Project identifier.</param>
/// <param name="Lifecycle">The owning Project lifecycle.</param>
/// <param name="Setup">Metadata-only setup, or null when the snapshot is unavailable.</param>
/// <param name="ProjectFolder">The included Project Folder reference, or null.</param>
/// <param name="Conversations">Included conversation references in deterministic order.</param>
/// <param name="FileReferences">Included file references in deterministic order.</param>
/// <param name="MemoryReferences">Included memory references in deterministic order.</param>
/// <param name="Excluded">Explicit optional omissions in deterministic order.</param>
/// <param name="Snapshot">The shared AD-32 admission snapshot.</param>
public sealed record ProjectContextReadResponse(
    string ProjectId,
    ProjectLifecycle Lifecycle,
    ProjectSetup? Setup,
    ProjectContextReference? ProjectFolder,
    IReadOnlyList<ProjectContextReference> Conversations,
    IReadOnlyList<ProjectContextReference> FileReferences,
    IReadOnlyList<ProjectContextReference> MemoryReferences,
    IReadOnlyList<ProjectContextExclusion> Excluded,
    AdmissionSnapshot Snapshot);
