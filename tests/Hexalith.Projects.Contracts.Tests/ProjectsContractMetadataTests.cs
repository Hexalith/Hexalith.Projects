// <copyright file="ProjectsContractMetadataTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests;

using Hexalith.Projects.Contracts;

using Shouldly;

using Xunit;

/// <summary>
/// Trivial green tests proving the Contracts skeleton loads and the contract namespace is correct.
/// Pure Tier-1 style: no Dapr, Aspire, network, browser, or containers.
/// </summary>
public sealed class ProjectsContractMetadataTests
{
    /// <summary>
    /// Verifies the contract metadata marker exposes the canonical domain name.
    /// </summary>
    [Fact]
    public void DomainNameIsProjects()
    {
        ProjectsContractMetadata.DomainName.ShouldBe("projects");
    }
}
