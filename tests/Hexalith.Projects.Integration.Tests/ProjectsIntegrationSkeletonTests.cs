// <copyright file="ProjectsIntegrationSkeletonTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using Hexalith.Projects.Server;
using Hexalith.Projects.Testing;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-3 placeholder. Real integration tests (Testcontainers / Dapr slim / Aspire topology)
/// land with the boundaries they exercise in later stories. This trivial test only proves the
/// project compiles, references resolve, and the lane is real; it is excluded from the fast lane.
/// </summary>
public sealed class ProjectsIntegrationSkeletonTests
{
    /// <summary>
    /// Verifies the server and testing skeletons are reachable from the integration project.
    /// </summary>
    [Fact]
    public void SkeletonReferencesResolve()
    {
        ProjectsServerModule.Name.ShouldBe("Hexalith.Projects.Server");
        ProjectsTestingMarker.Name.ShouldBe("Hexalith.Projects.Testing");
    }
}
