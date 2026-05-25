// <copyright file="ProjectsTenantEventSubscription.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

/// <summary>Stable Dapr pub/sub subscription metadata for Projects tenant events.</summary>
public static class ProjectsTenantEventSubscription
{
    /// <summary>The Dapr app id for the Projects workers host.</summary>
    public const string AppId = "projects-workers";

    /// <summary>The internal Dapr tenant-events route.</summary>
    public const string Route = "/tenants/events";

    /// <summary>The Dapr pub/sub component name.</summary>
    public const string PubSubName = "pubsub";

    /// <summary>The Tenants event topic name.</summary>
    public const string TopicName = "system.tenants.events";
}
