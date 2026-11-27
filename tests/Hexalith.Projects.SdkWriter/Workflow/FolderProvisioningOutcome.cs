// <copyright file="FolderProvisioningOutcome.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

/// <summary>Outcome returned by the Folders provisioning/validation adapter.</summary>
public enum FolderProvisioningOutcome
{
    /// <summary>A Folder was provisioned or validated with a durable receipt and security proof.</summary>
    Provisioned,

    /// <summary>No provisioned Folder exists yet for the step key.</summary>
    Absent,

    /// <summary>Access or authorization was denied (fail closed).</summary>
    Denied,

    /// <summary>Create-only same-name collision (A-2).</summary>
    Collision,

    /// <summary>Supplied Folder is orphaned or reserved (maps to <c>NeedsAttention</c>).</summary>
    OrphanedOrReserved,

    /// <summary>Provisioning input failed Folders domain validation.</summary>
    ValidationFailed,
}
