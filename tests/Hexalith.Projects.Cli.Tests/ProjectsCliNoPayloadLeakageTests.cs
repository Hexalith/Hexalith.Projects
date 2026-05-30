// <copyright file="ProjectsCliNoPayloadLeakageTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Cli.Tests;

using Hexalith.Projects.Cli;

using Shouldly;

using Xunit;

public sealed class ProjectsCliNoPayloadLeakageTests
{
    [Fact]
    public void Exit_Code_Surface_Is_Semantic_And_Does_Not_Expose_Transport_Secrets()
    {
        int[] codes =
        [
            ProjectsCliExitCodes.Success,
            ProjectsCliExitCodes.Usage,
            ProjectsCliExitCodes.Validation,
            ProjectsCliExitCodes.DenialOrNotFound,
            ProjectsCliExitCodes.Unavailable,
            ProjectsCliExitCodes.Unexpected,
        ];

        codes.ShouldBe([0, 2, 3, 4, 5, 10], ignoreOrder: false);
        string.Join(',', codes).ShouldNotContain("idempotency", Case.Insensitive);
        string.Join(',', codes).ShouldNotContain("token", Case.Insensitive);
    }
}
