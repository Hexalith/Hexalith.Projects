namespace Hexalith.Projects.PackageFixtures;

using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;

/// <summary>Records actual application and launcher identities for owned resource restart assertions.</summary>
internal static class PackageProcessReceipt
{
    // Field 22 of /proc/<pid>/stat is measured from boot and agrees across observers.
    internal static string? ReadStartTicks(int pid)
    {
        string stat;
        try { stat = File.ReadAllText($"/proc/{pid}/stat"); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }

        // The parenthesized command may itself contain spaces and closing parentheses.
        int end = stat.LastIndexOf(')');
        string[] fields = stat[(end + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (end < 0 || fields.Length <= 19 || !ulong.TryParse(fields[19], NumberStyles.None, CultureInfo.InvariantCulture, out _))
            throw new InvalidDataException("The kernel process identity is malformed.");
        return fields[0] is "Z" or "X" or "x" ? null : fields[19];
    }

    [SupportedOSPlatform("linux")]
    internal static void Write(string directory, string role)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Package process receipts require Linux.");
        int pid = Environment.ProcessId;
        // DCP launches this exact dotnet process with the already-built Release DLL.
        string startTicks = ReadStartTicks(pid) ?? throw new InvalidOperationException("The selected process is no longer running.");
        string assembly = Assembly.GetEntryAssembly()!.Location;
        _ = Uri.TryCreate(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"), UriKind.Absolute, out Uri? otlp);
        string? protocol = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
        string[] headers = (Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS") ?? string.Empty)
            .Split(',').Where(value => value.StartsWith("x-otlp-api-key=", StringComparison.OrdinalIgnoreCase)).ToArray();
        string? otlpKeyHash = headers.Length == 1 && headers[0].Length > "x-otlp-api-key=".Length
            ? Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(headers[0]["x-otlp-api-key=".Length..]))) : null;
        var receipt = new { resourceName = role, pid, startTicks,
            launcherPid = pid, launcherStartTicks = startTicks,
            binarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Environment.ProcessPath!))),
            assemblySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly))),
            otlpLoopback = otlp?.IsLoopback == true, otlpPort = otlp?.Port,
            otlpProtocol = protocol is "grpc" or "http/protobuf" ? protocol : "unrecognized", otlpApiKeySha256 = otlpKeyHash };
        string temporary = Path.Combine(directory, role + "-process." + Guid.NewGuid().ToString("N") + ".tmp");
        using (FileStream stream = new(temporary, new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write,
            UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite })) JsonSerializer.Serialize(stream, receipt);
        File.Move(temporary, Path.Combine(directory, role + "-process.json"), overwrite: true);
    }
}
