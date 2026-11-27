// <copyright file="DockerDaprRedisTwoReplicaFixtureTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if SUPPORTED_PROJECT_READ_SOURCE
namespace Hexalith.Projects.Integration.Tests.Story71;

using System;
using System.IO;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

/// <summary>
/// Unit tests for <see cref="DockerDaprRedisTwoReplicaFixture"/> component preparation helpers.
/// </summary>
public sealed class DockerDaprRedisTwoReplicaFixtureTests
{
    /// <summary>
    /// Verifies that <see cref="DockerDaprRedisTwoReplicaFixture.WriteStatestoreComponentAsync"/>
    /// restores nonroot-readable Unix file modes (<c>0755</c>/<c>0644</c>) even when starting from
    /// restrictive <c>0700</c>/<c>0600</c> permissions as produced under <c>umask 0077</c>.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task WriteStatestoreComponentAsync_GrantsNonRootReadPermissionsFromRestrictiveModes()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), $"story71-mode-test-{Guid.NewGuid():N}");
        string componentPath = Path.Combine(tempDir, "statestore.yaml");
        try
        {
            _ = Directory.CreateDirectory(tempDir);
            File.SetUnixFileMode(tempDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            await File.WriteAllTextAsync(componentPath, "stale", TestContext.Current.CancellationToken);
            File.SetUnixFileMode(componentPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);

            string writtenPath = await DockerDaprRedisTwoReplicaFixture
                .WriteStatestoreComponentAsync(tempDir, TestContext.Current.CancellationToken);

            writtenPath.ShouldBe(componentPath);
            UnixFileMode expectedDirMode =
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
            UnixFileMode expectedFileMode =
                UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.GroupRead
                | UnixFileMode.OtherRead;
            File.GetUnixFileMode(tempDir).ShouldBe(expectedDirMode);
            File.GetUnixFileMode(writtenPath).ShouldBe(expectedFileMode);
            string yaml = await File.ReadAllTextAsync(writtenPath, TestContext.Current.CancellationToken);
            yaml.ShouldContain("name: statestore");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
#endif
