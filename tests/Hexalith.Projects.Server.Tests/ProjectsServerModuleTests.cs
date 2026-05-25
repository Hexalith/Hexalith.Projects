// <copyright file="ProjectsServerModuleTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using Hexalith.Projects.Server;
using Hexalith.Projects.Workers;

using Shouldly;

using Xunit;

/// <summary>
/// Trivial green Tier-2 tests proving the server and workers skeletons load.
/// </summary>
public sealed class ProjectsServerModuleTests
{
    /// <summary>
    /// Verifies the server module marker exposes its name.
    /// </summary>
    [Fact]
    public void ServerModuleNameIsSet()
    {
        ProjectsServerModule.Name.ShouldBe("Hexalith.Projects.Server");
    }

    /// <summary>
    /// Verifies the workers module marker exposes its name.
    /// </summary>
    [Fact]
    public void WorkersModuleNameIsSet()
    {
        ProjectsWorkersModule.Name.ShouldBe("Hexalith.Projects.Workers");
    }
}
