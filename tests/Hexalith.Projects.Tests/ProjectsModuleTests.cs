// <copyright file="ProjectsModuleTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests;

using Hexalith.Projects;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

/// <summary>
/// Trivial green Tier-1 tests proving the domain core skeleton loads and the registration
/// surface is callable. Pure: no Dapr, Aspire, network, browser, or containers.
/// </summary>
public sealed class ProjectsModuleTests
{
    /// <summary>
    /// Verifies the module marker exposes its name.
    /// </summary>
    [Fact]
    public void ModuleNameIsSet()
    {
        ProjectsModule.Name.ShouldBe("Hexalith.Projects");
    }

    /// <summary>
    /// Verifies the placeholder registration extension is callable and returns the collection.
    /// </summary>
    [Fact]
    public void AddProjectsModuleReturnsServices()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddProjectsModule();

        result.ShouldBeSameAs(services);
    }
}
