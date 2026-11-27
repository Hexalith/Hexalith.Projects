// <copyright file="SdkWriterHostComposition.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Composition;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.Projects.SdkWriter.Aggregates;
using Hexalith.Projects.SdkWriter.Claims;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Test-composition DI helpers for the Story 7.1 SDK writer. Replaces any pre-existing keyed
/// <see cref="IDomainProcessor"/> / <see cref="IAsyncAggregateReplay"/> registrations for
/// <c>projects</c> and <c>project-folder-claims</c> so single-writer fencing holds strictly.
/// </summary>
public static class SdkWriterHostComposition
{
    /// <summary>
    /// Registers <see cref="ProjectSdkAggregate"/> and <see cref="ProjectFolderClaimAggregate"/> as the
    /// single keyed writers for <c>projects</c> and <c>project-folder-claims</c> in test composition.
    /// </summary>
    /// <param name="services">The test service collection.</param>
    /// <param name="isCurrentOwner">Optional current-owner verifier delegate for ChatBot authority commands.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddProjectsSdkWriterTestComposition(
        this IServiceCollection services,
        Func<string, string, string, bool>? isCurrentOwner = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        RemoveKeyedWriters(services, ProjectSdkAggregate.DomainName);
        RemoveKeyedWriters(services, ProjectFolderClaimAggregate.DomainName);

        services.AddSingleton(sp => new ProjectSdkAggregate(sp.GetRequiredService<TimeProvider>(), isCurrentOwner));
        services.AddSingleton(sp => new ProjectFolderClaimAggregate(sp.GetRequiredService<TimeProvider>()));

        services.AddKeyedSingleton<IDomainProcessor>(
            ProjectSdkAggregate.DomainName,
            (sp, _) => sp.GetRequiredService<ProjectSdkAggregate>());
        services.AddKeyedSingleton<IAsyncAggregateReplay>(
            ProjectSdkAggregate.DomainName,
            (sp, _) => sp.GetRequiredService<ProjectSdkAggregate>());

        services.AddKeyedSingleton<IDomainProcessor>(
            ProjectFolderClaimAggregate.DomainName,
            (sp, _) => sp.GetRequiredService<ProjectFolderClaimAggregate>());
        services.AddKeyedSingleton<IAsyncAggregateReplay>(
            ProjectFolderClaimAggregate.DomainName,
            (sp, _) => sp.GetRequiredService<ProjectFolderClaimAggregate>());

        return services;
    }

    /// <summary>Asserts that strictly one writer and one replay handler are registered per SDK domain.</summary>
    /// <param name="provider">The built service provider.</param>
    public static void AssertSingleWriterPerDomain(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        foreach (string domain in SdkWriterRuntimeCatalogDeclarations.Domains)
        {
            IReadOnlyList<IDomainProcessor> processors = provider.GetKeyedServices<IDomainProcessor>(domain).ToArray();
            if (processors.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected strictly one IDomainProcessor for domain '{domain}', found {processors.Count}.");
            }

            IReadOnlyList<IAsyncAggregateReplay> replayers = provider.GetKeyedServices<IAsyncAggregateReplay>(domain).ToArray();
            if (replayers.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected strictly one IAsyncAggregateReplay for domain '{domain}', found {replayers.Count}.");
            }
        }
    }

    private static void RemoveKeyedWriters(IServiceCollection services, string domain)
    {
        for (int index = services.Count - 1; index >= 0; index--)
        {
            ServiceDescriptor descriptor = services[index];
            if (descriptor.IsKeyedService
                && string.Equals(descriptor.ServiceKey as string, domain, StringComparison.Ordinal)
                && (descriptor.ServiceType == typeof(IDomainProcessor)
                    || descriptor.ServiceType == typeof(IAsyncAggregateReplay)))
            {
                services.RemoveAt(index);
            }
        }
    }
}
