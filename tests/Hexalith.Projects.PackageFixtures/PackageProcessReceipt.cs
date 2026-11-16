namespace Hexalith.Projects.PackageFixtures;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;

/// <summary>Records actual application and launcher identities for owned resource restart assertions.</summary>
internal static class PackageProcessReceipt
{
    [SupportedOSPlatform("linux")]
    internal static void Write(string directory, string role)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Package process receipts require Linux.");
        using Process current = Process.GetCurrentProcess();
        string stat = File.ReadAllText("/proc/self/stat");
        int parentId = int.Parse(stat[(stat.LastIndexOf(')') + 2)..].Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture);
        using Process parent = Process.GetProcessById(parentId);
        string assembly = Assembly.GetEntryAssembly()!.Location;
        var receipt = new { resourceName = role, pid = current.Id, startTicks = current.StartTime.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture),
            launcherPid = parent.Id, launcherStartTicks = parent.StartTime.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture),
            binarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Environment.ProcessPath!))),
            assemblySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly))) };
        string temporary = Path.Combine(directory, role + "-process." + Guid.NewGuid().ToString("N") + ".tmp");
        using (FileStream stream = new(temporary, new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write,
            UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite })) JsonSerializer.Serialize(stream, receipt);
        File.Move(temporary, Path.Combine(directory, role + "-process.json"), overwrite: true);
    }
}
