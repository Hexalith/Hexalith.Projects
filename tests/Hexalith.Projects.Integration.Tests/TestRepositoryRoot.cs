namespace Hexalith.Projects.Integration.Tests;

/// <summary>Locates source fixtures independently of the build artifact directory.</summary>
internal static class TestRepositoryRoot
{
    internal static string Find([System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
    {
        foreach (string candidate in new[] { Path.GetDirectoryName(sourcePath)!, Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (DirectoryInfo? directory = new(candidate); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Projects.slnx")))
                {
                    return directory.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Projects repository root was not found for source fixtures.");
    }
}
