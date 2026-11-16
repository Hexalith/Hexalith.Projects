namespace Hexalith.Projects.PackageFixtures;

using System.Diagnostics;

/// <summary>Executes fixture-owned processes with bounded waits and awaited cancellation cleanup.</summary>
internal static class FixtureProcess
{
    internal static async Task<string> RunAsync(string tool, IEnumerable<string> arguments, string directory,
        CancellationToken cancellationToken, IReadOnlyDictionary<string, string>? environment = null,
        IEnumerable<string>? protectedValues = null, bool printOutput = false)
    {
        ProcessStartInfo start = new(tool)
        {
            WorkingDirectory = directory, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        if (environment is not null)
            foreach ((string key, string value) in environment) start.Environment[key] = value;
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Fixture process did not start: " + tool);
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> errors = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        string stdout = await output.ConfigureAwait(false), stderr = await errors.ConfigureAwait(false);
        foreach (string value in protectedValues ?? [])
            if (!string.IsNullOrWhiteSpace(value))
            {
                stdout = stdout.Replace(value, "[private]", StringComparison.Ordinal);
                stderr = stderr.Replace(value, "[private]", StringComparison.Ordinal);
            }
        if (printOutput || process.ExitCode != 0)
        {
            Console.Out.Write(stdout);
            Console.Error.Write(stderr);
        }
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Fixture {tool} failed with exit code {process.ExitCode}.");
        return stdout.Trim();
    }
}
