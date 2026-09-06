// <copyright file="AdmissionRecoveryAction.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System.Collections.Generic;

/// <summary>Closed recovery-action vocabulary for AD-32 snapshots.</summary>
public static class AdmissionRecoveryAction
{
    /// <summary>No recovery is required.</summary>
    public const string None = "None";

    /// <summary>Retry the same read.</summary>
    public const string Retry = "Retry";

    /// <summary>Refresh context from current owner evidence.</summary>
    public const string RefreshContext = "RefreshContext";

    /// <summary>Request a preview.</summary>
    public const string RequestPreview = "RequestPreview";

    /// <summary>Renew an existing preview.</summary>
    public const string RenewPreview = "RenewPreview";

    /// <summary>Poll an in-flight task.</summary>
    public const string PollTask = "PollTask";

    /// <summary>Resolve a needs-attention state.</summary>
    public const string ResolveNeedsAttention = "ResolveNeedsAttention";

    /// <summary>Select an alternative candidate.</summary>
    public const string SelectAlternative = "SelectAlternative";

    /// <summary>Contact an administrator.</summary>
    public const string ContactAdministrator = "ContactAdministrator";

    /// <summary>Gets the complete closed vocabulary in declaration order.</summary>
    public static IReadOnlyList<string> Values { get; } =
    [
        None,
        Retry,
        RefreshContext,
        RequestPreview,
        RenewPreview,
        PollTask,
        ResolveNeedsAttention,
        SelectAlternative,
        ContactAdministrator,
    ];
}
