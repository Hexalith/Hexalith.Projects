// <copyright file="ReadRoutingRegistrationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client.Tests;

using Hexalith.Projects.Client;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;
using Xunit;

/// <summary>Proves the startup read route binds the documented configuration section once per process.</summary>
public sealed class ReadRoutingRegistrationTests
{
    [Theory]
    [InlineData(null, null, "configured-unavailable")]
    [InlineData("supported", null, "invalid-mode")]
    [InlineData("Unavailable", null, "configured-unavailable")]
    [InlineData("LegacyRollback", null, "missing-evidence-configuration")]
    [InlineData("LegacyRollback", "bound", "evidence-file-missing")]
    [InlineData("Supported", "bound", "missing-evidence-configuration")]
    [InlineData("Supported", "bound-with-contract", "evidence-file-missing")]
    public void ProjectsReadsSectionIsBoundToOneStartupSelection(string? mode, string? evidence, string reason)
    {
        Dictionary<string, string?> values = [];
        if (mode is not null) values["Projects:Reads:Mode"] = mode;
        if (evidence is not null)
        {
            values["Projects:Reads:EvidencePath"] = Path.Combine(Path.GetTempPath(), "story67-registration-" + Guid.NewGuid().ToString("N") + ".json");
            values["Projects:Reads:EvidenceHash"] = new string('a', 64);
            values["Projects:Reads:SourceRevision"] = new string('b', 40);
            if (evidence == "bound-with-contract") values["Projects:Reads:ContractHash"] = new string('c', 64);
        }

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        services.AddProjectsReadRouting();
        using ServiceProvider provider = services.BuildServiceProvider();

        ProjectsReadRouteSelection selected = provider.GetRequiredService<ProjectsReadRouteSelection>();
        selected.Mode.ShouldBe("Unavailable");
        selected.RefusalReason.ShouldBe(reason);
        provider.GetRequiredService<ProjectsReadRouteSelection>().ShouldBeSameAs(selected);
    }
}
