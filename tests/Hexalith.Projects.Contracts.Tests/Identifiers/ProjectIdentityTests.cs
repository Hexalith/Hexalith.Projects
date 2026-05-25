// <copyright file="ProjectIdentityTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Identifiers;

using System;
using System.Collections.Generic;

using Hexalith.Projects.Contracts;
using Hexalith.Projects.Contracts.Identifiers;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 conformance tests for the canonical identity-derivation helper (AR-4, FS-3): every derived
/// key/topic/group/scope is a deterministic function of <c>(tenant, projectId)</c> only — never of a
/// payload field, header, or query parameter — and distinct tenants/projects never collide.
/// Pure and deterministic: no wall-clock, random, Dapr, network, or containers.
/// </summary>
public sealed class ProjectIdentityTests
{
    private const string Tenant = "acme";
    private const string ProjectValue = "01HZ9K8YQ3W6V2N4R7T5P0X1AB";

    [Fact]
    public void GlobalIdUsesCanonicalTenantDomainProjectFormat()
    {
        ProjectIdentity identity = Build(Tenant, ProjectValue);
        identity.GlobalId.ShouldBe($"{Tenant}:{ProjectsContractMetadata.DomainName}:{ProjectValue}");
        identity.Domain.ShouldBe("projects");
    }

    [Fact]
    public void AllDerivedValuesContainTenantAndProject()
    {
        ProjectIdentity identity = Build(Tenant, ProjectValue);
        foreach (string derived in DerivedValues(identity))
        {
            derived.ShouldContain(Tenant);
            derived.ShouldContain(ProjectValue);
        }
    }

    [Fact]
    public void DerivationIsDeterministic()
    {
        ProjectIdentity a = Build(Tenant, ProjectValue);
        ProjectIdentity b = Build(Tenant, ProjectValue);

        a.GlobalId.ShouldBe(b.GlobalId);
        a.ActorId.ShouldBe(b.ActorId);
        a.StateStoreKey.ShouldBe(b.StateStoreKey);
        a.EventStreamKeyPrefix.ShouldBe(b.EventStreamKeyPrefix);
        a.SnapshotKey.ShouldBe(b.SnapshotKey);
        a.PubSubTopic.ShouldBe(b.PubSubTopic);
        a.SignalRGroup.ShouldBe(b.SignalRGroup);
        a.LogScope.ShouldBe(b.LogScope);
        a.ProjectionKey("inventory").ShouldBe(b.ProjectionKey("inventory"));
    }

    [Fact]
    public void DifferentProjectsNeverCollide()
    {
        ProjectIdentity a = Build(Tenant, "01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        ProjectIdentity b = Build(Tenant, "01HZ9K8YQ3W6V2N4R7T5P0X1CD");

        a.GlobalId.ShouldNotBe(b.GlobalId);
        a.ActorId.ShouldNotBe(b.ActorId);
        a.StateStoreKey.ShouldNotBe(b.StateStoreKey);
        a.SignalRGroup.ShouldNotBe(b.SignalRGroup);
        a.ProjectionKey("inventory").ShouldNotBe(b.ProjectionKey("inventory"));
    }

    [Fact]
    public void DifferentTenantsNeverCollide()
    {
        ProjectIdentity a = Build("acme", ProjectValue);
        ProjectIdentity b = Build("globex", ProjectValue);

        a.GlobalId.ShouldNotBe(b.GlobalId);
        a.ActorId.ShouldNotBe(b.ActorId);
        a.StateStoreKey.ShouldNotBe(b.StateStoreKey);
        a.PubSubTopic.ShouldNotBe(b.PubSubTopic);
        a.SignalRGroup.ShouldNotBe(b.SignalRGroup);
    }

    [Fact]
    public void DifferentProjectionNamesProduceDifferentKeys()
    {
        ProjectIdentity identity = Build(Tenant, ProjectValue);
        identity.ProjectionKey("inventory").ShouldNotBe(identity.ProjectionKey("audit"));
    }

    [Fact]
    public void ConstructorRejectsNullTenant()
        => Should.Throw<ArgumentNullException>(() => new ProjectIdentity(null!, new ProjectId(ProjectValue)));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void ConstructorRejectsEmptyOrWhitespaceTenant(string tenant)
        => Should.Throw<ArgumentException>(() => new ProjectIdentity(tenant, new ProjectId(ProjectValue)));

    [Fact]
    public void ConstructorRejectsNullProjectId()
        => Should.Throw<ArgumentNullException>(() => new ProjectIdentity(Tenant, null!));

    [Fact]
    public void ProjectionKeyRejectsEmptyName()
    {
        ProjectIdentity identity = Build(Tenant, ProjectValue);
        Should.Throw<ArgumentException>(() => identity.ProjectionKey(" "));
    }

    private static ProjectIdentity Build(string tenant, string projectValue)
        => new(tenant, new ProjectId(projectValue));

    private static IEnumerable<string> DerivedValues(ProjectIdentity identity)
    {
        yield return identity.GlobalId;
        yield return identity.ActorId;
        yield return identity.StateStoreKey;
        yield return identity.EventStreamKeyPrefix;
        yield return identity.SnapshotKey;
        yield return identity.SignalRGroup;
        yield return identity.LogScope;
        yield return identity.ProjectionKey("inventory");
    }
}
