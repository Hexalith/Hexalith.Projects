// <copyright file="ProjectResolutionPersistsNothingTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Resolution;

using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Testing.Context;

using Microsoft.Extensions.Logging;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Resolution.ProjectResolutionEvidenceBuilder;

/// <summary>Positive proof that Story 4.1's engine persists nothing and performs no I/O.</summary>
public sealed class ProjectResolutionPersistsNothingTests
{
    [Fact]
    public void ProjectResolutionEngine_ConstructorAndFields_HaveNoPersistenceOrNetworkDependencies()
    {
        Type engineType = typeof(ProjectResolutionEngine);
        Type[] inspectedTypes = engineType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(static ctor => ctor.GetParameters().Select(static p => p.ParameterType))
            .Concat(engineType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(static field => field.FieldType))
            .ToArray();

        inspectedTypes.ShouldNotContain(typeof(HttpClient));
        inspectedTypes
            .Select(static t => t.FullName ?? t.Name)
            .ShouldAllBe(static name =>
                !name.Contains("Dapr", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("EventStore", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("CommandSubmitter", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("ProjectionStore", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("HttpClient", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_NormalPath_EmitsNoLogsAndReturnsOnlyComputedResult()
    {
        RecordingLogger<ProjectResolutionEngine> logger = new();

        ProjectResolution result = new ProjectResolutionEngine(logger).Resolve(Context(), [Candidate()]);

        result.Candidates.Count.ShouldBe(1);
        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_FailClosedTenantAuthority_UsesOnlyOptionalWarningLog()
    {
        RecordingLogger<ProjectResolutionEngine> logger = new();

        ProjectResolution result = new ProjectResolutionEngine(logger).Resolve(
            Context(authoritativeTenantId: null),
            [Candidate()]);

        result.Candidates.ShouldBeEmpty();
        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public void ProjectResolutionEngine_Assembly_ReferencesNoSiblingContextOrWebAssemblies()
    {
        string[] forbidden =
        [
            "Hexalith.Conversations",
            "Hexalith.Folders",
            "Hexalith.Memories",
            "Dapr",
            "Microsoft.AspNetCore",
        ];

        string[] referenced = typeof(ProjectResolutionEngine).Assembly
            .GetReferencedAssemblies()
            .Select(static a => a.Name ?? string.Empty)
            .ToArray();

        foreach (string token in forbidden)
        {
            referenced.ShouldNotContain(
                name => name.Contains(token, StringComparison.OrdinalIgnoreCase),
                $"Domain-core resolution engine assembly must not reference {token} (AC9 purity).");
        }
    }
}
