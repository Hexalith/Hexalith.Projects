using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

int checks = 0;
try
{
    string collector = Path.GetFullPath(args.Single());
    Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(collector);
    Type program = assembly.GetType("Program", true)!, targets = assembly.GetType("ReceiverTargets", true)!;
    const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    object? Call(Type type, string method, params object?[] values)
    {
        try { return type.GetMethod(method, flags)!.Invoke(null, values); }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
    }
    void Check(bool value) { checks++; if (!value) throw new InvalidOperationException(); }
    void Reject(string method, string expected, params object?[] values)
    {
        bool refused = false;
        try { Call(program, method, values); }
        catch (Exception error) { refused = error.GetType().Name == "GuardException" && (string?)error.GetType().GetProperty("Code")!.GetValue(error) == expected; }
        Check(refused);
    }
    string prep = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
    // The pinned map is source metadata only; no application process is launched or read.
    using JsonDocument map = JsonDocument.Parse(File.ReadAllText(Path.Combine(prep, "collector-guard-map.json")));
    foreach (JsonElement row in map.RootElement.GetProperty("roles").EnumerateArray())
    {
        string role = row.GetProperty("role").GetString()!, resource = row.GetProperty("resource").GetString()!, project = row.GetProperty("sourceProject").GetString()!;
        string dll = row.GetProperty("entryDll").GetString()!, native = row.GetProperty("nativeApphost").GetString()!, name = row.GetProperty("entryAssemblyName").GetString()!;
        object target = Call(targets, "Match", role, resource, project, dll)!;
        Check(target is not null && Call(targets, "Match", role, resource, project, native) is not null);
        foreach (object?[] bad in new[] { new object?[] { "gateway", resource, project, dll }, [role, "unknown", project, dll], [role, resource, project + ".wrong", dll], [role, resource, project, "/tmp/arbitrary.dll"] }) Check(Call(targets, "Match", bad) is null);
        string dotnet = "/home/administrator/.dotnet/dotnet";
        Check((bool)Call(program, "SafeCommand", new[] { native }, native, target, native)!);
        Check((bool)Call(program, "SafeCommand", new[] { dotnet, dll }, dll, target, dotnet)!);
        Check((bool)Call(program, "SafeCommand", new[] { dotnet, "exec", dll, "--applicationName", name }, dll, target, dotnet)!);
        foreach (object?[] bad in new[] {
            new object?[] { new[] { native, dll }, dll, target, native },
            [new[] { dotnet, native }, native, target, dotnet],
            [new[] { "/tmp/dotnet", dll }, dll, target, dotnet],
            [new[] { dotnet, dll, dll }, dll, target, dotnet],
            [new[] { dotnet, "--environment", "Development", dll }, dll, target, dotnet],
            [new[] { dotnet, "exec", "exec", dll }, dll, target, dotnet],
            [new[] { dotnet, dll, "--applicationName", "Hexalith.EventStore" }, dll, target, dotnet],
            [new[] { dotnet, dll, "--password", "synthetic" }, dll, target, dotnet],
            [new[] { dotnet, dll, "--unrecognized", "synthetic" }, dll, target, dotnet],
        }) Check(!(bool)Call(program, "SafeCommand", bad)!);
        foreach ((string label, string path) in new[] { ("target-entry-assembly", dll), ("domain-service-assembly", row.GetProperty("domainServiceDll").GetString()!), ("sdk-assembly", row.GetProperty("sdkDll").GetString()!) })
        {
            Check((bool)Call(targets, "MatchesAssembly", target, label, path)!);
            Check(!(bool)Call(targets, "MatchesAssembly", target, label, path + ".wrong")!);
        }
        Type identity = program.GetNestedType("Identity", BindingFlags.NonPublic)!;
        foreach (int pid in new[] { 0, Environment.ProcessId })
        {
            string json = JsonSerializer.Serialize(new { Role = role, Resource = resource, SourceProject = project, Pid = pid, StartTicks = 0L, RequiredCommandToken = dll, ExecutableSha256 = "", Files = Array.Empty<object>() });
            object value = JsonSerializer.Deserialize(json, identity)!;
            Reject("Verify", "identity-contract-invalid", value);
        }
    }
    string mapped = "/synthetic/entry.dll";
    int found = (int)Call(program, "CountMapped", new[] { "0000-1000 r--p 0000 00:00 1 " + mapped, "0000-1000 r--p 0000 00:00 1 /other.dll" }, mapped)!;
    Check(found == 1); Call(program, "RequireMapped", found);
    int missing = (int)Call(program, "CountMapped", new[] { "0000-1000 r--p 0000 00:00 1 /other.dll" }, mapped)!;
    Reject("RequireMapped", "owned-receiver-assembly-not-mapped-skip-attach", missing);
    string temporary = Path.Combine(Path.GetTempPath(), "receiver-cpu-guard-smoke-" + Guid.NewGuid().ToString("N"));
    try
    {
        File.WriteAllBytes(temporary, [1, 2, 3, 4]); string initial = (string)Call(program, "Hash", temporary)!;
        Call(program, "RequireHashMatch", "mapped-sdk-assembly", initial, initial);
        File.WriteAllBytes(temporary, [4, 3, 2, 1]); string fresh = (string)Call(program, "Hash", temporary)!;
        Check(initial != fresh); Reject("RequireHashMatch", "input-hash-mismatch", "mapped-sdk-assembly", fresh, initial);
    }
    finally { File.Delete(temporary); }
    long firstStart = (long)Call(program, "StartTicks", Environment.ProcessId)!;
    Check(firstStart > 0 && firstStart == (long)Call(program, "StartTicks", Environment.ProcessId)!);
    foreach (string name in new[] { "Hexalith.Folders.Server.Queries.FolderOwnerReadQueryHandler.ExecuteAsync", "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.GetResult" }) Check((string)Call(program, "MetadataName", name, true)! == name);
    foreach (string name in new[] { "", "0xabcdef", "/private/path", "System.Type.dll", "System.Type\nSecret", "System.Type\"Value", "0xdeadbeef.Some", "unknown" }) Check((string)Call(program, "MetadataName", name, true)! == "[unresolved]");
    Console.WriteLine(JsonSerializer.Serialize(new { smokeOnly = true, allPassed = true, checks, fixedRoles = 3, applicationProcessesStarted = 0, targetAttachCalls = 0, traceParserExecuted = false, parserMetadataLabelGuardsExercised = true, fullParserAndProviderReuseRequiresPinnedSourceEquality = true, rawArgumentsEnvironmentMapsOrPayloadPrinted = false }));
    return 0;
}
catch (Exception error)
{
    Console.WriteLine(JsonSerializer.Serialize(new { smokeOnly = true, allPassed = false, checks, fixedError = error.GetType().Name, applicationProcessesStarted = 0, targetAttachCalls = 0 })); return 1;
}
