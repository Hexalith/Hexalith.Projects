// <copyright file="SdkWriterRuntimeCatalogDeclarations.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Composition;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.SdkWriter.Aggregates;
using Hexalith.Projects.SdkWriter.Claims;

using WireArchiveProject = Hexalith.Projects.SdkWriter.Commands.ArchiveProject;
using WireConfirmProjectResolution = Hexalith.Projects.SdkWriter.Commands.ConfirmProjectResolution;
using WireCreateBoundProject = Hexalith.Projects.SdkWriter.Commands.CreateBoundProject;
using WireCreateProject = Hexalith.Projects.SdkWriter.Commands.CreateProject;
using WireAbandonProjectCreation = Hexalith.Projects.SdkWriter.Commands.AbandonProjectCreation;
using WireLinkFileReference = Hexalith.Projects.SdkWriter.Commands.LinkFileReference;
using WireLinkMemory = Hexalith.Projects.SdkWriter.Commands.LinkMemory;
using WireRestoreProject = Hexalith.Projects.SdkWriter.Commands.RestoreProject;
using WireSetProjectFolder = Hexalith.Projects.SdkWriter.Commands.SetProjectFolder;
using WireUnlinkFileReference = Hexalith.Projects.SdkWriter.Commands.UnlinkFileReference;
using WireUnlinkMemory = Hexalith.Projects.SdkWriter.Commands.UnlinkMemory;
using WireUpdateProjectSetup = Hexalith.Projects.SdkWriter.Commands.UpdateProjectSetup;

/// <summary>
/// Non-shipped test-composition catalog inventory for the Story 7.1 SDK writer
/// (<c>projects</c> and <c>project-folder-claims</c>). Does not alter deployed production routing.
/// </summary>
public static class SdkWriterRuntimeCatalogDeclarations
{
    /// <summary>Gets the canonical domains hosted in Story 7.1 SDK test composition.</summary>
    public static IReadOnlyList<string> Domains { get; } =
    [
        ProjectSdkAggregate.DomainName,
        ProjectFolderClaimAggregate.DomainName,
    ];

    /// <summary>Gets all 14 command types handled by <see cref="ProjectSdkAggregate"/>.</summary>
    public static IReadOnlyList<Type> ProjectCommandTypes { get; } =
    [
        typeof(WireCreateProject),
        typeof(WireUpdateProjectSetup),
        typeof(WireArchiveProject),
        typeof(WireRestoreProject),
        typeof(WireSetProjectFolder),
        typeof(WireLinkFileReference),
        typeof(WireUnlinkFileReference),
        typeof(WireLinkMemory),
        typeof(WireUnlinkMemory),
        typeof(WireConfirmProjectResolution),
        typeof(GrantProjectChatBotAuthority),
        typeof(RevokeProjectChatBotAuthority),
        typeof(WireCreateBoundProject),
        typeof(WireAbandonProjectCreation),
    ];

    /// <summary>Gets all 3 command types handled by <see cref="ProjectFolderClaimAggregate"/>.</summary>
    public static IReadOnlyList<Type> FolderClaimCommandTypes { get; } =
    [
        typeof(AcquireProjectFolderClaim),
        typeof(ReleaseProjectFolderClaim),
        typeof(CloseProjectFolderClaim),
    ];
}
