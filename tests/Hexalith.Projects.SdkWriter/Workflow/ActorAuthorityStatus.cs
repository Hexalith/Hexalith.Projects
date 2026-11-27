// <copyright file="ActorAuthorityStatus.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

/// <summary>Live original-actor authority evaluation status.</summary>
public enum ActorAuthorityStatus
{
    /// <summary>Current same-Tenant original-actor authority is verified and fresh.</summary>
    Allowed,

    /// <summary>Authority is explicitly denied (safe denial 404).</summary>
    Denied,

    /// <summary>Authorization source is unavailable or stale (retryable 503, distinct from 404 Denied).</summary>
    Unavailable,
}
