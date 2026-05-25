// <copyright file="ProjectsAppHost.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.AppHost;

/// <summary>
/// Shared helpers for the Hexalith.Projects Aspire AppHost.
/// </summary>
public static class ProjectsAppHost
{
    /// <summary>
    /// Gets the module name used in host diagnostics.
    /// </summary>
    public static string Name => "Hexalith.Projects.AppHost";

    /// <summary>Resolves the Dapr components directory from AppHostDirectory first, then current directory.</summary>
    /// <param name="appHostDirectory">The Aspire AppHost directory.</param>
    /// <param name="currentDirectory">The current working directory.</param>
    /// <returns>The resolved Dapr components directory.</returns>
    public static string ResolveDaprComponentsPath(string appHostDirectory, string currentDirectory)
    {
        string appHostPath = Path.Combine(appHostDirectory, "DaprComponents");
        if (Directory.Exists(appHostPath))
        {
            return appHostPath;
        }

        string currentPath = Path.Combine(currentDirectory, "DaprComponents");
        if (Directory.Exists(currentPath))
        {
            return currentPath;
        }

        throw new DirectoryNotFoundException(
            "DaprComponents directory was not found. Expected it under the AppHost directory or the current working directory.");
    }

    /// <summary>Resolves a required Dapr configuration file and fails fast when it is missing.</summary>
    /// <param name="appHostDirectory">The Aspire AppHost directory.</param>
    /// <param name="currentDirectory">The current working directory.</param>
    /// <param name="fileName">The required configuration file name.</param>
    /// <returns>The resolved file path.</returns>
    public static string ResolveDaprConfigPath(string appHostDirectory, string currentDirectory, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        string appHostPath = Path.Combine(appHostDirectory, "DaprComponents", fileName);
        if (File.Exists(appHostPath))
        {
            return appHostPath;
        }

        string currentPath = Path.Combine(currentDirectory, "DaprComponents", fileName);
        if (File.Exists(currentPath))
        {
            return currentPath;
        }

        throw new FileNotFoundException(
            $"Dapr configuration file '{fileName}' was not found. "
            + "Expected it under DaprComponents in the AppHost directory or current working directory.",
            appHostPath);
    }
}
