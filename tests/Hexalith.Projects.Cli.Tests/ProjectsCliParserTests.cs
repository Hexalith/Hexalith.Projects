// <copyright file="ProjectsCliParserTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Cli.Tests;

using Hexalith.Projects.Cli;

using Shouldly;

using Xunit;

public sealed class ProjectsCliParserTests
{
    [Theory]
    [InlineData("trace-resolution", "trace")]
    [InlineData("validate-references", "validate")]
    public void Parser_Normalizes_Documented_Aliases(string alias, string canonical)
    {
        bool parsed = ProjectsCliParser.TryParse(["projects", alias, "--project-id", "project-1"], out ProjectsCliInvocation invocation, out string error);

        parsed.ShouldBeTrue(error);
        invocation.Command.ShouldBe(canonical);
    }

    [Fact]
    public void Parser_Recognizes_Diagnostic_Export_Command_Group()
    {
        ProjectsCliParser.TryParse(
            ["projects", "diagnostic", "export", "--project-id", "project-1"],
            out ProjectsCliInvocation invocation,
            out string error).ShouldBeTrue(error);

        invocation.Command.ShouldBe("diagnostic export");
    }
}
