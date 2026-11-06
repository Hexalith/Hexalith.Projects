using System.Diagnostics.Tracing;
using System.Text.Json;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;

string directory = args[0];
await Task.WhenAll(args.Skip(1).Select(async target =>
{
    string[] parts = target.Split(':');
    string role = parts[0];
    int pid = int.Parse(parts[1]);
    List<object> observations = [];
    Dictionary<Guid, (string Operation, double Start, DateTime StartUtc)> pending = [];
    string? failure = null;
    int ignored = 0;
    int eventsLost = 0;
    try
    {
        var client = new DiagnosticsClient(pid);
        using var session = client.StartEventPipeSession(new EventPipeProvider("System.Net.Http", EventLevel.Informational, -1L), requestRundown: false, circularBufferMB: 16);
        using var source = new EventPipeEventSource(session.EventStream);
        source.Dynamic.All += data =>
        {
            if (data.ProviderName != "System.Net.Http") return;
            if (data.EventName is "RequestStart" or "Request/Start")
            {
                string path = data.PayloadByName("pathAndQuery") as string ?? "";
                string operation = path.Contains("/queries/owner-authority/workload", StringComparison.Ordinal) ? "delegated-owner-gateway"
                    : path.Contains("/streams/read/workload", StringComparison.Ordinal) ? "native-source-metadata"
                    : path.EndsWith("/workload-authority/token", StringComparison.Ordinal) ? "workload-token"
                    : path.Contains("/v1.0/state/", StringComparison.Ordinal) ? "dapr-state"
                    : path.Contains("/v1.0/actors/", StringComparison.Ordinal) ? "dapr-actor"
                    : path.Contains("/v1.0/invoke/folders/method/query", StringComparison.Ordinal) ? "folder-owner-query"
                    : path.Contains("/v1.0/invoke/projects/method/query", StringComparison.Ordinal) ? "project-query"
                    : "";
                if (operation.Length == 0) { ignored++; return; }
                if (pending.Count >= 4096 || observations.Count >= 50000) throw new InvalidOperationException("Finite observer capacity exceeded.");
                pending[data.ActivityID] = (operation, data.TimeStampRelativeMSec, data.TimeStamp.ToUniversalTime());
            }
            else if (data.EventName is "RequestStop" or "Request/Stop")
            {
                if (!pending.Remove(data.ActivityID, out var start)) return;
                int status = Convert.ToInt32(data.PayloadByName("statusCode"));
                observations.Add(new { operation = start.Operation, startedAtUtc = start.StartUtc, elapsedMilliseconds = Math.Round(data.TimeStampRelativeMSec - start.Start, 3), httpStatus = status });
            }
        };
        var processing = Task.Run(() => source.Process());
        await Task.WhenAny(processing, Task.Delay(TimeSpan.FromSeconds(110)));
        try { session.Stop(); } catch (Exception) { }
        await processing;
        eventsLost = source.EventsLost;
        foreach (var start in pending.Values) observations.Add(new { operation = start.Operation, startedAtUtc = start.StartUtc, completed = false });
    }
    catch (Exception exception) { failure = exception.GetType().Name; }
    var receipt = new { role, diagnosticOnly = true, rawUrlsRetained = false, headersOrBodiesObserved = false, protectedQueryLoggingEnabled = false, collectionProvider = "System.Net.Http", ignoredOperationCount = ignored, eventsLost, failure, observations };
    await File.WriteAllTextAsync(Path.Combine(directory, role + "-http-timing.json"), JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }));
}));
