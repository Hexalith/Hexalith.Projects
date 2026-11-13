using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.EventPipe;

// External scratch diagnostic only. No target code, environment, GC or policies are changed.
internal static class Program
{
    private const long FileCap = 256L * 1024 * 1024;
    private const long StopWritingThreshold = FileCap - 32L * 1024 * 1024;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private sealed class GuardException(string code) : Exception { public string Code { get; } = code; }
    private static long StartTicks(int pid)
    {
        string stat = File.ReadAllText($"/proc/{pid}/stat");
        string[] fields = stat[(stat.LastIndexOf(')') + 2)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields[0] == "Z") throw new GuardException("target-already-exited");
        return long.Parse(fields[19], System.Globalization.CultureInfo.InvariantCulture);
    }
    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
    private static void Save(string path, object data)
    {
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(data, Json) + "\n");
        File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(temporary, path, true);
    }
    private static string Label(string value, int cap = 2048)
    {
        if (string.IsNullOrEmpty(value)) return "[unresolved]";
        return new string(value.Where(c => !char.IsControl(c)).Take(cap).ToArray());
    }
    private static string MetadataName(string value, bool method)
    {
        // FullMethodName returns empty for invalid MethodIndex in the pinned TraceEvent source.
        // Still refuse native/address/path fallback formats rather than emitting an opaque raw name.
        if (string.IsNullOrEmpty(value) || value.Length > 2048 || value.Any(char.IsControl)
            || value.Contains('/') || value.Contains('\\') || value.Contains('"') || value.Contains('\'')
            || Regex.IsMatch(value, @"\b0x[0-9a-fA-F]+\b|\.(?:cs|dll|exe|so|pdb)(?:\b|:)", RegexOptions.IgnoreCase)
            || !Regex.IsMatch(value, @"^[A-Za-z_<>][A-Za-z0-9_.+`<>, \[\]:&*()!~=?|\-]*$")
            || (method && !value.Contains('.'))) return "[unresolved]";
        return value;
    }
    private sealed record InputFile(string Label, string Path, string Sha256);
    private sealed record Identity(string Role, string Resource, string SourceProject, int Pid, long StartTicks, string RequiredCommandToken, string ExecutableSha256, InputFile[] Files);
    private sealed record Row(string Label, bool Matches, string Sha256);
    private static bool SafeCommand(string[] args, string marker, ReceiverTarget target, string actualExecutable)
    {
        // Reject unknown options or values, including every key/token/password/connection-string argument.
        string[] sensitive = ["token", "password", "passwd", "secret", "credential", "api-key", "apikey", "authorization", "connectionstring", "access-key"];
        if (args.Length is < 1 or > 32 || args.Any(a => sensitive.Any(word => a.Contains(word, StringComparison.OrdinalIgnoreCase)))) return false;
        if (args.Count(argument => argument == marker) != 1) return false;
        bool native = marker == target.NativeApphost;
        int markerIndex;
        if (native)
        {
            if (args[0] != marker || actualExecutable != marker) return false;
            markerIndex = 0;
        }
        else
        {
            if (marker != target.EntryDll || args[0] != actualExecutable || Path.GetFileName(actualExecutable) != "dotnet") return false;
            markerIndex = args.Length > 1 && args[1] == "exec" ? 2 : 1;
            if (args.Length <= markerIndex || args[markerIndex] != marker) return false;
        }
        bool PathValue(string value) => Path.IsPathFullyQualified(value) && !value.Any(char.IsControl);
        bool UrlValue(string value) => value.Split(';').All(part => Uri.TryCreate(part, UriKind.Absolute, out var uri)
            && (uri.Scheme == "http" || uri.Scheme == "https") && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0
            && (uri.Host is "localhost" or "127.0.0.1" or "[::1]" or "0.0.0.0" or "[::]") && uri.Port is > 0 and < 65536);
        for (int i = 1; i < args.Length; i++)
        {
            string arg = args[i];
            if (i == markerIndex || (!native && i == 1 && arg == "exec")) continue;
            int equal = arg.IndexOf('=');
            string option = equal < 0 ? arg : arg[..equal];
            if (equal < 0 && i + 1 >= args.Length) return false;
            string value = equal < 0 ? args[++i] : arg[(equal + 1)..];
            bool allowed = option switch
            {
                "--urls" => UrlValue(value),
                "--contentRoot" or "--content-root" or "--runtimeconfig" or "--depsfile" => PathValue(value),
                "--environment" => value is "Development" or "Production" or "Staging",
                "--applicationName" => value == target.EntryAssemblyName,
                "--dapr-http-port" or "--dapr-grpc-port" or "--dapr-port" => int.TryParse(value, out int port) && port is > 0 and < 65536,
                _ => false
            };
            if (!allowed) return false;
        }
        return true;
    }
    private static Row[] Verify(Identity identity)
    {
        ReceiverTarget target = ReceiverTargets.Match(identity.Role, identity.Resource, identity.SourceProject, identity.RequiredCommandToken)
            ?? throw new GuardException("identity-contract-invalid");
        if (identity.Pid <= 1 || identity.Pid == Environment.ProcessId) throw new GuardException("identity-contract-invalid");
        if (StartTicks(identity.Pid) != identity.StartTicks) throw new GuardException("target-identity-changed");
        // Exact owned PID only; discard command bytes immediately without logging any argument.
        string actualExecutable = new FileInfo($"/proc/{identity.Pid}/exe").LinkTarget
            ?? throw new GuardException("owned-executable-path-unavailable");
        var command = File.ReadAllBytes($"/proc/{identity.Pid}/cmdline");
        bool safe = SafeCommand(Encoding.UTF8.GetString(command).Split('\0', StringSplitOptions.RemoveEmptyEntries), identity.RequiredCommandToken, target, actualExecutable);
        Array.Clear(command);
        if (!safe) throw new GuardException("unsafe-or-unexpected-owned-receiver-arguments-skip-attach");
        string actualExeHash = Hash($"/proc/{identity.Pid}/exe");
        var rows = new List<Row> { new("runtime-executable", actualExeHash.Equals(identity.ExecutableSha256, StringComparison.OrdinalIgnoreCase), actualExeHash) };
        foreach (var file in identity.Files)
        {
            if (file.Label.Length > 100 || file.Label.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '.')))
                throw new GuardException("input-label-invalid");
            string actual = Hash(file.Path);
            rows.Add(new(file.Label, actual.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase), actual));
        }
        foreach (string required in new[] { "target-entry-assembly", "domain-service-assembly", "sdk-assembly", "owner-config", "source-audit" })
            if (!rows.Any(r => r.Label == required)) throw new GuardException("required-identity-input-missing");
        if (rows.Any(r => !r.Matches)) throw new GuardException("input-hash-mismatch");
        foreach (string label in new[] { "target-entry-assembly", "domain-service-assembly", "sdk-assembly" })
        {
            InputFile file = identity.Files.Single(input => input.Label == label);
            string mappedPath = Path.GetFullPath(file.Path);
            if (!Path.IsPathFullyQualified(file.Path) || !ReceiverTargets.MatchesAssembly(target, label, mappedPath))
                throw new GuardException("required-target-assembly-path-mismatch");
            // Exact owned resource PID only. Never print complete maps, addresses or unrelated module paths.
            // Resource ownership comes from the managed receipt, not the executable's spelling.
            int mapped;
            try { mapped = CountMapped(File.ReadLines($"/proc/{identity.Pid}/maps"), mappedPath); }
            catch { throw new GuardException("owned-receiver-map-proof-unavailable-skip-attach"); }
            RequireMapped(mapped);
            rows.Add(RequireHashMatch("mapped-" + label, Hash(mappedPath), file.Sha256));
        }
        if (StartTicks(identity.Pid) != identity.StartTicks) throw new GuardException("target-identity-changed");
        return rows.ToArray();
    }
    private static void RequireMapped(int mapped)
    {
        if (mapped == 0) throw new GuardException("owned-receiver-assembly-not-mapped-skip-attach");
    }
    private static Row RequireHashMatch(string label, string actual, string expected)
    {
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new GuardException("input-hash-mismatch");
        return new(label, true, actual);
    }
    private static int CountMapped(IEnumerable<string> lines, string mappedPath)
    {
        int mapped = 0;
        foreach (string line in lines)
        {
            string[] fields = line.Split(' ', 6, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 6 && fields[5] == mappedPath) mapped++;
        }
        return mapped;
    }
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 3 && args[0] == "collect") return await Collect(args[1], args[2]);
            if (args.Length == 2 && args[0] == "analyze") return Analyze(args[1]);
            Console.Error.WriteLine("Usage: ReceiverCpuProbe collect IDENTITY_JSON NEW_OUTPUT_DIRECTORY | analyze OWNED_OUTPUT_DIRECTORY");
            return 64;
        }
        catch (Exception error)
        {
            // Never log exception messages: they can carry private paths or runtime command data.
            Console.Error.WriteLine(JsonSerializer.Serialize(new { diagnosticOnly = true, errorType = error.GetType().Name, fixedReason = error is GuardException guard ? guard.Code : "collector-error" }));
            return 1;
        }
    }
    private static async Task<int> Collect(string identityPath, string outputPath)
    {
        Identity identity = JsonSerializer.Deserialize<Identity>(File.ReadAllText(identityPath), Json) ?? throw new InvalidOperationException("MissingIdentity");
        Row[] before = Verify(identity);
        string output = Path.GetFullPath(outputPath);
        if (Directory.Exists(output) || File.Exists(output)) throw new InvalidOperationException("OutputAlreadyExists");
        Directory.CreateDirectory(output, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Save(Path.Combine(output, "identity-checks.json"), new { diagnosticOnly = true, role = identity.Role, identity.Pid, identity.StartTicks, inputManifestSha256 = Hash(identityPath), argvStructuralWhitelistPassed = true, argvPrintedOrRetained = false, checks = before });
        using Process process = Process.GetCurrentProcess();
        TimeSpan cpuStart = process.TotalProcessorTime;
        long peakRss = process.WorkingSet64, beginTicks = Stopwatch.GetTimestamp();
        DateTime beginUtc = DateTime.UtcNow;
        long observedBytes = 0, writtenBytes = 0;
        bool capReached = false, stopCompleted = false, drainCompleted = false;
        string reason = "duration", failureType = "none";
        using var readCancellation = new CancellationTokenSource();
        var cap = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        string raw = Path.Combine(output, "execution.nettrace");
        Row[] immediate = Verify(identity); // Recheck exact start identity + all pinned inputs immediately before attach.
        var provider = new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational, 0);
        var gcProvider = new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, 0x1);
        var config = new EventPipeSessionConfiguration(new[] { provider, gcProvider }, circularBufferSizeMB: 64, requestRundown: true, requestStackwalk: true);
        using EventPipeSession session = await new DiagnosticsClient(identity.Pid).StartEventPipeSessionAsync(config, CancellationToken.None);
        Save(Path.Combine(output, "attached.json"), new { diagnosticOnly = true, featureAcceptance = false, identity.Pid, identity.StartTicks, beginUtc, beginTicks, attachedUtc = DateTime.UtcNow, attachedTicks = Stopwatch.GetTimestamp(), frequency = Stopwatch.Frequency, provider = provider.Name, keywords = "0x0", level = 4, gcProvider = gcProvider.Name, gcKeywords = "0x1", gcLevel = 4, allocationTicksRequested = false, requestStackwalk = true, requestRundown = true, bufferMiB = 64, fileCapBytes = FileCap, stopThresholdBytes = StopWritingThreshold, maxSeconds = 8, collectorCpuGuardSeconds = 4, collectorRssGuardBytes = 512L * 1024 * 1024, immediateChecks = immediate });
        Task drain = Task.Run(async () =>
        {
            using var file = new FileStream(raw, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 65536, FileOptions.Asynchronous);
            File.SetUnixFileMode(raw, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            byte[] buffer = new byte[65536];
            while (true)
            {
                int count = await session.EventStream.ReadAsync(buffer.AsMemory(), readCancellation.Token);
                if (count == 0) break;
                long bytes = Interlocked.Add(ref observedBytes, count);
                int permitted = (int)Math.Clamp(FileCap - writtenBytes, 0, count);
                if (permitted > 0) { await file.WriteAsync(buffer.AsMemory(0, permitted), readCancellation.Token); writtenBytes += permitted; }
                if (permitted != count) capReached = true;
                if (bytes >= StopWritingThreshold) cap.TrySetResult();
                // Even after the hard cap, continue draining/discarding so Stop/rundown can complete.
            }
            await file.FlushAsync(readCancellation.Token);
        });
        try
        {
            while (!drain.IsCompleted && !cap.Task.IsCompleted && Stopwatch.GetElapsedTime(beginTicks).TotalSeconds < 8)
            {
                await Task.Delay(100);
                process.Refresh(); peakRss = Math.Max(peakRss, process.WorkingSet64);
                if ((process.TotalProcessorTime - cpuStart).TotalSeconds > 4) { reason = "collector-cpu-guard"; break; }
                if (process.WorkingSet64 > 512L * 1024 * 1024) { reason = "collector-rss-guard"; break; }
            }
            if (cap.Task.IsCompleted) reason = "file-threshold";
            else if (drain.IsCompleted) reason = "target-stream-ended";
            // Stop only the returned session. Never signal the target or stop another diagnostic session.
            Task stop = Task.Run(session.Stop);
            if (await Task.WhenAny(stop, Task.Delay(TimeSpan.FromSeconds(30))) != stop) reason = "stop-timeout";
            else { await stop; stopCompleted = true; }
            if (await Task.WhenAny(drain, Task.Delay(TimeSpan.FromSeconds(5))) != drain) reason = "drain-timeout";
            else { await drain; drainCompleted = true; }
        }
        catch (Exception error) { failureType = error.GetType().Name; }
        finally
        {
            readCancellation.Cancel();
            try { await drain.WaitAsync(TimeSpan.FromSeconds(2)); } catch { }
            process.Refresh(); peakRss = Math.Max(peakRss, process.WorkingSet64);
            bool incomplete = reason != "duration" || capReached || !stopCompleted || !drainCompleted || failureType != "none";
            Save(Path.Combine(output, "collection-receipt.json"), new { diagnosticOnly = true, featureAcceptance = false, identity.Pid, identity.StartTicks, beginUtc, endUtc = DateTime.UtcNow, beginTicks, endTicks = Stopwatch.GetTimestamp(), frequency = Stopwatch.Frequency, reason, failureType, incomplete, capReached, stopCompleted, drainCompleted, observedBytes, writtenBytes, rawSha256 = File.Exists(raw) ? Hash(raw) : null, collectorCpuMilliseconds = (process.TotalProcessorTime - cpuStart).TotalMilliseconds, collectorPeakObservedRssBytes = peakRss, targetGcRequested = false, onlyOwnedSessionStopped = true });
        }
        return reason == "duration" && stopCompleted && drainCompleted && !capReached && failureType == "none" ? 0 : 2;
    }
    private sealed class CpuGroup
    {
        public int Id { get; set; }
        public string SampleType { get; set; } = "";
        public long Count { get; set; }
        public string[] Frames { get; set; } = [];
    }
    private static int Analyze(string outputPath)
    {
        string output = Path.GetFullPath(outputPath), raw = Path.Combine(output, "execution.nettrace"), etlx = Path.Combine(output, "execution.etlx");
        if (!File.Exists(Path.Combine(output, "collection-receipt.json")) || new FileInfo(raw).Length > FileCap) throw new GuardException("collection-receipt-missing-or-cap-invalid");
        if (File.Exists(etlx)) throw new GuardException("analysis-already-exists");
        long lostOnConvert = 0;
        var options = new TraceLogOptions { ShouldResolveSymbols = _ => false, AlwaysResolveSymbols = false, ConversionLog = TextWriter.Null, OnLostEvents = (_, lost, _) => lostOnConvert = Math.Max(lostOnConvert, lost) };
        TraceLog.CreateFromEventPipeDataFile(raw, etlx, options);
        File.SetUnixFileMode(etlx, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        using var trace = new TraceLog(etlx);
        using var source = trace.Events.GetSource();
        var parser = new SampleProfilerTraceEventParser(source);
        var groups = new Dictionary<string, CpuGroup>(StringComparer.Ordinal);
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        var events = new Dictionary<string, long>(StringComparer.Ordinal);
        var previous = new Dictionary<int, double>();
        var suspend = new Dictionary<int, (double Start, string Reason)>();
        var suspensions = new List<object>();
        var gcStarts = new Dictionary<(int, int), double>();
        var gcCycles = new List<object>();
        long samples = 0, missingStack = 0, unresolvedFrames = 0, truncatedStacks = 0, rowCount = 0, rowBytes = 0, overflowSamples = 0, unpairedRestarts = 0, overwrittenSuspends = 0;
        long intervalCount = 0; double intervalSum = 0, intervalMin = double.MaxValue, intervalMax = 0;
        string rowsPath = Path.Combine(output, "cpu-samples.jsonl");
        using var rows = new StreamWriter(new FileStream(rowsPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read));
        File.SetUnixFileMode(rowsPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        source.AllEvents += data => { string name = Label(data.ProviderName + "/" + data.EventName, 256); events[name] = events.GetValueOrDefault(name) + 1; };
        parser.ThreadSample += data =>
        {
            samples++; string type = data.Type is ClrThreadSampleType.Managed or ClrThreadSampleType.External or ClrThreadSampleType.Error ? data.Type.ToString() : "Unknown";
            counts[type] = counts.GetValueOrDefault(type) + 1;
            if (previous.TryGetValue(data.ThreadID, out double before)) { double interval = data.TimeStampRelativeMSec - before; intervalCount++; intervalSum += interval; intervalMin = Math.Min(intervalMin, interval); intervalMax = Math.Max(intervalMax, interval); }
            previous[data.ThreadID] = data.TimeStampRelativeMSec;
            List<string> frames = new(); var stack = data.CallStack(); if (stack is null) missingStack++;
            for (; stack is not null && frames.Count < 192; stack = stack.Caller)
            {
                string frame = MetadataName(stack.CodeAddress.FullMethodName, true); if (frame.Length > 512) frame = "[unresolved]";
                if (frame == "[unresolved]") unresolvedFrames++; frames.Add(frame);
            }
            if (stack is not null) truncatedStacks++;
            string key = type + "\n" + string.Join(";", frames);
            if (!groups.TryGetValue(key, out CpuGroup? group))
            {
                if (groups.Count >= 8192) { key = type + "\n[unique-key-cap]"; overflowSamples++; }
                if (!groups.TryGetValue(key, out group)) { group = new CpuGroup { Id = groups.Count + 1, SampleType = type, Frames = key.Contains("[unique-key-cap]") ? ["[unique-key-cap]"] : frames.ToArray() }; groups[key] = group; }
            }
            group.Count++;
            string row = JsonSerializer.Serialize(new { timeUtc = data.TimeStamp.ToUniversalTime(), relativeMilliseconds = data.TimeStampRelativeMSec, threadId = data.ThreadID, sampleType = type, stackId = group.Id });
            int size = Encoding.UTF8.GetByteCount(row) + 1;
            if (rowCount < 1000000 && rowBytes + size <= 128L * 1024 * 1024) { rows.WriteLine(row); rowCount++; rowBytes += size; }
        };
        source.Clr.GCSuspendEEStart += data =>
        {
            if (suspend.ContainsKey(data.ClrInstanceID)) overwrittenSuspends++;
            suspend[data.ClrInstanceID] = (data.TimeStampRelativeMSec, data.Reason.ToString());
        };
        source.Clr.GCRestartEEStop += data =>
        {
            if (!suspend.Remove(data.ClrInstanceID, out var start)) { unpairedRestarts++; return; }
            if (suspensions.Count < 50000) suspensions.Add(new { startMilliseconds = start.Start, endMilliseconds = data.TimeStampRelativeMSec, durationMilliseconds = data.TimeStampRelativeMSec - start.Start, reason = start.Reason, gcRelatedByReasonName = start.Reason.Contains("GC", StringComparison.Ordinal) });
        };
        source.Clr.GCStart += data => gcStarts[(data.ClrInstanceID, data.Count)] = data.TimeStampRelativeMSec;
        source.Clr.GCStop += data => { if (gcStarts.Remove((data.ClrInstanceID, data.Count), out double start) && gcCycles.Count < 50000) gcCycles.Add(new { count = data.Count, depth = data.Depth, startMilliseconds = start, endMilliseconds = data.TimeStampRelativeMSec, wallDurationMilliseconds = data.TimeStampRelativeMSec - start }); };
        source.Process(); rows.Flush();
        long managed = counts.GetValueOrDefault("Managed"), external = counts.GetValueOrDefault("External");
        Save(Path.Combine(output, "cpu-summary.json"), new { diagnosticOnly = true, featureAcceptance = false, exactCpuTimeClaimed = false, externalIsNotProofOfBlocked = true, runtimeBaseNominalSamplingSleepMilliseconds = 1, samplingSkipsOtherSuspensions = true, rawSha256 = Hash(raw), sanitizedRowsSha256 = Hash(rowsPath), traceStartUtc = trace.SessionStartTime.ToUniversalTime(), samples, sampleTypeCounts = counts, managedSampleFraction = samples == 0 ? 0 : (double)managed / samples, externalSampleFraction = samples == 0 ? 0 : (double)external / samples, sampledThreads = previous.Count, sameThreadIntervalCount = intervalCount, sameThreadIntervalMeanMilliseconds = intervalCount == 0 ? 0 : intervalSum / intervalCount, sameThreadIntervalMinMilliseconds = intervalCount == 0 ? 0 : intervalMin, sameThreadIntervalMaxMilliseconds = intervalMax, missingStack, unresolvedFrames, truncatedStacks, maximumRetainedFrames = 192, uniqueGroupCapOverflowSamples = overflowSamples, sanitizedRows = rowCount, sanitizedRowsCapped = rowCount != samples, lostOnConvert, traceEventsLost = trace.EventsLost, sourceEventCounts = events, stackGroups = groups.Values.OrderByDescending(g => g.Count), suspendRestartPairs = suspensions, unpairedSuspendStarts = suspend.Count, overwrittenSuspendStarts = overwrittenSuspends, unpairedRestarts, gcCyclePairs = gcCycles, unpairedGcStarts = gcStarts.Count, suspendAndGcCycleIntervalsAreWallTimeNotCpu = true, gcReasonPairingDoesNotMakeAllRuntimeSuspensionsGc = true, symbolLookupRequested = false, heapDumpRequested = false, forcedGcRequested = false });
        return lostOnConvert == 0 && trace.EventsLost == 0 && missingStack == 0 && samples > 0 && rowCount == samples && overflowSamples == 0 ? 0 : 2;
    }
}
