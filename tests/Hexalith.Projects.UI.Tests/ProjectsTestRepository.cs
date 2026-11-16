namespace Hexalith.Projects.UI.Tests;

/// <summary>Locates source inputs for native tests built in a disposable artifacts directory.</summary>
internal static class ProjectsTestRepository
{
    /// <summary>Gets the checkout containing the test source inputs.</summary>
    internal static string Root
    {
        get
        {
            foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                for (DirectoryInfo? current = new(start); current is not null; current = current.Parent)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Hexalith.Projects.slnx"))) return current.FullName;
                }
            }
            throw new DirectoryNotFoundException("Could not locate the Projects test source inputs.");
        }
    }
}
