// <copyright file="ProjectUiContractsBoundaryTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Tests.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.Projects.Contracts.Ui;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 tests for the AD-16 split: FrontComposer descriptors resolve from the non-packable
/// UI.Contracts assembly under their unchanged namespace, the wire vocabulary stays in the
/// dependency-light Contracts assembly, and the generated registrations still load through
/// <see cref="ProjectsFrontComposerDomain"/>.
/// </summary>
public sealed class ProjectUiContractsBoundaryTests
{
    private static readonly Type[] _uiContractsTypes =
    [
        typeof(ProjectAuditTimelineRowProjection),
        typeof(ProjectDetailInspectorProjection),
        typeof(ProjectInventoryRowProjection),
        typeof(ProjectMaintenanceActionProjection),
        typeof(ProjectOperationalDashboardProjection),
        typeof(ProjectOperatorDiagnosticShellProjection),
        typeof(ProjectReferenceHealthRowProjection),
        typeof(ProjectResolutionTraceCandidateProjection),
        typeof(ProjectResolutionTraceExclusionProjection),
        typeof(ProjectResolutionTraceProjection),
        typeof(ProjectSafeDiagnosticExportProjection),
        typeof(ProjectWarningQueueItemProjection),
        typeof(ProjectMaintenancePanelState),
        typeof(ProjectMaintenancePanelStates),
        typeof(ProjectVocabularyDescriptors),
        typeof(VocabularyDescriptor),
        typeof(ProjectsFrontComposerDomain),
    ];

    private static readonly Type[] _contractsTypes =
    [
        typeof(ProjectContextAssemblyOutcome),
        typeof(ProjectContextFreshness),
        typeof(ProjectContextInclusionCheck),
        typeof(ProjectContextInclusionDiagnostic),
        typeof(ProjectLifecycle),
        typeof(ProjectReasonCode),
        typeof(ReferenceState),
        typeof(ResolutionResult),
        typeof(ProjectMaintenanceActions),
        typeof(ProjectMaintenanceCommandLifecycleStates),
    ];

    [Fact]
    public void DescriptorsResolveFromUiContractsAndWireVocabularyStaysInContracts()
    {
        Assembly uiContracts = typeof(ProjectsFrontComposerDomain).Assembly;
        Assembly contracts = typeof(ProjectLifecycle).Assembly;
        uiContracts.GetName().Name.ShouldBe("Hexalith.Projects.UI.Contracts");
        contracts.GetName().Name.ShouldBe("Hexalith.Projects.Contracts");

        foreach (Type type in _uiContractsTypes)
        {
            type.Assembly.ShouldBe(uiContracts, type.Name);
            type.Namespace.ShouldBe("Hexalith.Projects.Contracts.Ui", type.Name);
        }

        foreach (Type type in _contractsTypes)
        {
            type.Assembly.ShouldBe(contracts, type.Name);
            type.Namespace.ShouldBe("Hexalith.Projects.Contracts.Ui", type.Name);
        }

        string[] uiOnlyPrefixes = ["Hexalith.FrontComposer", "Fluxor", "Microsoft.FluentUI", "Microsoft.AspNetCore"];
        contracts.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => uiOnlyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ShouldBeEmpty();
    }

    [Fact]
    public void GeneratedRegistrationsLoadThroughProjectsFrontComposerDomain()
    {
        ServiceCollection services = [];
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposerQuickstart(o => o.ScanAssemblies(typeof(ProjectsFrontComposerDomain).Assembly));
        _ = services.AddHexalithDomain<ProjectsFrontComposerDomain>();
        using ServiceProvider provider = services.BuildServiceProvider();

        IReadOnlyList<DomainManifest> manifests = provider.GetRequiredService<IFrontComposerRegistry>().GetManifests();

        manifests.Where(manifest => manifest.BoundedContext == "Projects")
            .SelectMany(manifest => manifest.Projections)
            .ShouldBe(
                _uiContractsTypes.Where(type => type.Name.EndsWith("Projection", StringComparison.Ordinal)).Select(type => type.FullName!),
                ignoreOrder: true);
    }
}
