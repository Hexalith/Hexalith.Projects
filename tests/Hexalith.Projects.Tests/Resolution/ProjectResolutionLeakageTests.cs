// <copyright file="ProjectResolutionLeakageTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Tests.Resolution;

using System.Text.Json;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Resolution;
using Hexalith.Projects.Testing.Leakage;

using Shouldly;

using Xunit;

using static Hexalith.Projects.Testing.Resolution.ProjectResolutionEvidenceBuilder;

/// <summary>FS-2 leakage and FS-8/SM-3 no-TenantId coverage for Project resolution DTOs.</summary>
public sealed class ProjectResolutionLeakageTests
{
    [Fact]
    public void ProjectResolution_NoPayloadLeakageHarnessPasses()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(
            Context(),
            [
                Candidate(signals:
                [
                    Signal(ProjectReasonCode.ConversationLinked),
                    Signal(ProjectReasonCode.FileReferenceMatched, ReferenceState.Unauthorized, "file-001", "file"),
                ]),
            ]);

        NoPayloadLeakageAssertions.AssertProjectResolutionNoLeakage(result);
    }

    [Fact]
    public void ProjectResolution_DoesNotEmitTenantId()
    {
        ProjectResolution result = new ProjectResolutionEngine().Resolve(Context(), [Candidate()]);

        string serialized = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        serialized.ShouldNotContain("tenantId", Case.Insensitive);
        serialized.ShouldNotContain(DefaultTenant, Case.Insensitive);
    }
}
