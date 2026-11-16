namespace Hexalith.Projects.PackageFixtures;

using System.Security.Cryptography;
using System.Text.Json;

/// <summary>Owns private ephemeral files and exact container identities through every outcome.</summary>
internal sealed class FixtureScratch : IAsyncDisposable
{
    private readonly List<string> _containers = [];
    private readonly Func<string[], CancellationToken, Task<string>> _docker;

    internal FixtureScratch(bool actualOwners, Func<string[], CancellationToken, Task<string>>? docker = null)
    {
        string root = Environment.GetEnvironmentVariable("TMPDIR") ?? Path.GetTempPath();
        Directory = Path.Combine(root, "projects-fixtures-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(12)));
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Package fixtures require Linux.");
        System.IO.Directory.CreateDirectory(Directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Isolation = (actualOwners ? "reads-" : "fixtures-") + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(12));
        _docker = docker ?? ((arguments, cancellationToken) => FixtureProcess.RunAsync("docker", arguments, Directory, cancellationToken));
    }

    internal string Directory { get; }

    internal string Isolation { get; }

    internal string File(string name) => Path.Combine(Directory, name);

    internal async Task WriteAsync(string name, string value, CancellationToken cancellationToken)
    {
        FileStreamOptions options = new()
        {
            Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None,
        };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using FileStream stream = new(File(name), options);
        using StreamWriter writer = new(stream);
        await writer.WriteAsync(value.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    internal async Task<string> ContainerAsync(string name, IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        string cidfile = File(name + ".cid");
        string id;
        try
        {
            id = await _docker(new[] { "create", "--cidfile", cidfile,
                "--name", Isolation + "-" + name, "--label", "hexalith.projects-package-fixtures=" + Isolation }
                .Concat(arguments).ToArray(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // docker can create the resource before a process cancellation. Custody starts at the cidfile.
            if (System.IO.File.Exists(cidfile))
            {
                string recorded = (await System.IO.File.ReadAllTextAsync(cidfile, CancellationToken.None).ConfigureAwait(false)).Trim();
                if (System.Text.RegularExpressions.Regex.IsMatch(recorded, "^[0-9a-f]{64}$")) _containers.Add(recorded);
            }
        }
        if (!_containers.Contains(id, StringComparer.Ordinal))
            throw new InvalidOperationException("Private fixture container custody is unproven.");
        await _docker(["start", id], cancellationToken).ConfigureAwait(false);
        return id;
    }

    public async ValueTask DisposeAsync()
    {
        List<Exception> failures = [];
        foreach (string id in Enumerable.Reverse(_containers))
        {
            try
            {
                using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
                string value = await _docker(["inspect", "--format", "{{json .Config.Labels}}", id], timeout.Token).ConfigureAwait(false);
                using JsonDocument labels = JsonDocument.Parse(value);
                if (labels.RootElement.GetProperty("hexalith.projects-package-fixtures").GetString() != Isolation)
                    throw new InvalidOperationException("Container ownership changed; exact fixture cleanup refused.");
                await _docker(["rm", "--force", "--volumes", id], timeout.Token).ConfigureAwait(false);
                string remaining = await _docker(["ps", "--all", "--no-trunc", "--format", "{{.ID}}"], timeout.Token).ConfigureAwait(false);
                if (remaining.Split('\n').Contains(id, StringComparer.Ordinal))
                    throw new InvalidOperationException("Fixture container cleanup did not remove the owned resource.");
            }
            catch (Exception failure) { failures.Add(failure); }
        }
        try { System.IO.Directory.Delete(Directory, recursive: true); }
        catch (Exception failure) { failures.Add(failure); }
        if (failures.Count != 0) throw new AggregateException("Package fixture cleanup failed.", failures);
    }
}
