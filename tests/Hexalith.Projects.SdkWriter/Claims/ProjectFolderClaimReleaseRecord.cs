// <copyright file="ProjectFolderClaimReleaseRecord.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Claims;

/// <summary>Recorded release tuple for exact idempotent replay verification on AD-35 claim releases.</summary>
/// <param name="ProjectId">The Project that held the released claim.</param>
/// <param name="ReleasedGeneration">The exact claim generation that was released.</param>
public sealed record ProjectFolderClaimReleaseRecord(
    string ProjectId,
    long ReleasedGeneration);
