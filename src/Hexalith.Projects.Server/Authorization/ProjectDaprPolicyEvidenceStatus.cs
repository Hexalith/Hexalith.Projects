// <copyright file="ProjectDaprPolicyEvidenceStatus.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

/// <summary>Status of Dapr deny-by-default policy evidence for Projects service calls.</summary>
public enum ProjectDaprPolicyEvidenceStatus
{
    /// <summary>The policy evidence allows the operation.</summary>
    Allowed,

    /// <summary>The policy evidence denies the operation.</summary>
    Denied,

    /// <summary>The policy evidence is unavailable and must fail closed.</summary>
    Unavailable,
}
