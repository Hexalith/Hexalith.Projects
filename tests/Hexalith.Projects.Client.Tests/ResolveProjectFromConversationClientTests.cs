// <copyright file="ResolveProjectFromConversationClientTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client.Tests;

using System;
using System.Linq;
using System.Reflection;

using Hexalith.Projects.Client.Generated;

using Shouldly;

using Xunit;

/// <summary>
/// Story 4.2 generation assertions for the <c>ResolveProjectFromConversation</c> typed client method
/// and the <c>ProjectResolution</c> wire types. A GET query produces a typed method but no idempotency
/// helper, mirroring the other Projects query operations.
/// </summary>
public sealed class ResolveProjectFromConversationClientTests
{
    [Fact]
    public void GeneratedClientExposesResolveQueryWithoutIdempotencyParameter()
    {
        Assembly clientAssembly = typeof(CreateProjectRequest).Assembly;
        Type clientInterface = clientAssembly.GetType("Hexalith.Projects.Client.Generated.IClient").ShouldNotBeNull();

        MethodInfo[] methods = clientInterface.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == "ResolveProjectFromConversationAsync")
            .ToArray();

        methods.ShouldNotBeEmpty();
        methods.Any(m => m.ReturnType.FullName?.Contains("ProjectResolution", StringComparison.Ordinal) == true).ShouldBeTrue();
        methods.SelectMany(m => m.GetParameters())
            .Any(p => p.Name?.Contains("idempotency", StringComparison.OrdinalIgnoreCase) == true)
            .ShouldBeFalse();
    }

    [Fact]
    public void GeneratedResolutionTypesCarryNoIdempotencyHelper()
    {
        Assembly clientAssembly = typeof(CreateProjectRequest).Assembly;
        foreach (string typeName in new[] { "ProjectResolution", "ResolutionCandidate", "ResolutionExclusion" })
        {
            Type type = clientAssembly.GetType($"Hexalith.Projects.Client.Generated.{typeName}").ShouldNotBeNull(typeName);
            type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Any(m => m.Name == "ComputeIdempotencyHash").ShouldBeFalse(typeName);
        }
    }

    [Fact]
    public void GeneratedProjectResolutionExposesOutcomeCandidatesAndExclusions()
    {
        Assembly clientAssembly = typeof(CreateProjectRequest).Assembly;
        Type resolution = clientAssembly.GetType("Hexalith.Projects.Client.Generated.ProjectResolution").ShouldNotBeNull();

        resolution.GetProperty("Result").ShouldNotBeNull();
        resolution.GetProperty("Candidates").ShouldNotBeNull();
        resolution.GetProperty("Excluded").ShouldNotBeNull();
        resolution.GetProperty("ObservedAt").ShouldNotBeNull();

        // No tenant authority is ever projected onto the wire type (FS-8 / SM-3).
        resolution.GetProperties()
            .Any(p => p.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse();
    }
}
