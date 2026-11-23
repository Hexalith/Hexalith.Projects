// <copyright file="HexalithProjectsResources.Generic.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aspire;

using global::Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

/// <summary>
/// Structural contract for the Projects Aspire topology.
/// </summary>
/// <typeparam name="TProject">The type of .NET application resource in this topology.</typeparam>
/// <param name="StateStore">The Dapr state store component.</param>
/// <param name="PubSub">The Dapr pub/sub component.</param>
/// <param name="EventStore">The EventStore resource.</param>
/// <param name="Tenants">The Tenants resource.</param>
/// <param name="Projects">The Projects API resource.</param>
/// <param name="ProjectsWorkers">The Projects worker resource.</param>
public sealed record HexalithProjectsResources<TProject>(
    IResourceBuilder<IDaprComponentResource> StateStore,
    IResourceBuilder<IDaprComponentResource> PubSub,
    IResourceBuilder<TProject> EventStore,
    IResourceBuilder<TProject> Tenants,
    IResourceBuilder<TProject> Projects,
    IResourceBuilder<TProject> ProjectsWorkers)
    where TProject : IDotnetProgramResource;
