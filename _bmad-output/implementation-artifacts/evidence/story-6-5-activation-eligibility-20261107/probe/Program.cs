using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

string inputPath = args[0], libraryDirectory = args[1], outputPath = args[2];
const BindingFlags Methods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
try
{
    AssemblyLoadContext.Default.Resolving += (_, name) => File.Exists(Path.Combine(libraryDirectory, name.Name + ".dll"))
        ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(libraryDirectory, name.Name + ".dll")) : null;
    Assembly contracts = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(libraryDirectory, "Hexalith.EventStore.Contracts.dll"));
    Assembly sdk = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(libraryDirectory, "Hexalith.EventStore.ServiceDefaults.dll"));
    Type activationType = contracts.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogActivation", true)!;
    Type authorityType = sdk.GetType("Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogAuthority", true)!;
    Type inputType = sdk.GetType("Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogBindingInput", true)!;
    Type memoType = sdk.GetType("Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogBindingMemo", true)!;
    Type scopeType = sdk.GetType("Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogBindingScope", true)!;
    Type bindingType = sdk.GetType("Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogActivationBinding", true)!;
    Type decodeType = contracts.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogDecodeScope", true)!;
    Type decodeMemoType = contracts.GetType("Hexalith.EventStore.Contracts.Routing.RouteCatalogValidatedBytesMemo", true)!;
    byte[] originalBytes = File.ReadAllBytes(inputPath);
    JsonObject document = JsonNode.Parse(originalBytes)!.AsObject();
    CryptographicOperations.ZeroMemory(originalBytes);
    string[] proofNames = document.Select(property => property.Key).Where(key => key.Equals("TrustProof", StringComparison.OrdinalIgnoreCase)).ToArray();
    foreach (string name in proofNames) document.Remove(name);
    document["trustProof"] = string.Empty;
    byte[] consumedBytes = JsonSerializer.SerializeToUtf8Bytes(document);
    object activation = JsonSerializer.Deserialize(consumedBytes, activationType, JsonSerializerOptions.Web)!;
    CryptographicOperations.ZeroMemory(consumedBytes);
    document.Clear();
    MethodInfo freezeMethod = authorityType.GetMethod("Freeze", Methods)!;
    MethodInfo captureMethod = inputType.GetMethod("TryCapture", Methods)!;
    MethodInfo coreMethod = bindingType.GetMethod("ComputeDigestCore", Methods)!;
    MethodInfo publicMethod = bindingType.GetMethod("ComputeDigest", Methods)!;
    MethodInfo enterDecode = decodeType.GetMethod("EnterWithMemo", Methods)!;
    MethodInfo enterBinding = scopeType.GetMethod("Enter", Methods)!;
    object frozen = freezeMethod.Invoke(null, [activation])!;
    object? Get(object? value, string name) => value?.GetType().GetProperty(name, Methods)?.GetValue(value);
    object[] Items(object? value) => value is IEnumerable entries ? entries.Cast<object>().ToArray() : [];
    var collections = new List<object>();
    var textCharges = new Dictionary<string, long>();
    var collectionCharges = new Dictionary<string, long>();
    long total = 512;
    void Text(string category, params object?[] values)
    {
        long charged = values.Sum(value => 24L + ((string?)value)?.Length * 2L ?? 24L);
        total += charged; textCharges[category] = textCharges.GetValueOrDefault(category) + charged;
    }
    void Fields(object? value, string category, params string[] names) => Text(category, names.Select(name => Get(value, name)).ToArray());
    void Collection(object? value, string path)
    {
        if (value is null) return;
        Type type = value.GetType(); int count = Items(value).Length;
        bool supported = type.IsSZArray || type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)
            || type.GetGenericTypeDefinition() == typeof(System.Collections.ObjectModel.ReadOnlyCollection<>));
        long charged = 32L + count * 128L;
        total += charged; collectionCharges[path] = collectionCharges.GetValueOrDefault(path) + charged;
        collections.Add(new { path, count, type = type.ToString(), supportedShape = supported, withinItemBudget = count <= 4096, chargedBytes = charged });
    }
    object Accounting(object graph)
    {
        total = 512; collections.Clear(); textCharges.Clear(); collectionCharges.Clear();
        Fields(graph, "activation", "AttemptId", "RootDigest", "PredecessorRootDigest", "Owner");
        foreach (string property in new[] { "RequiredHosts", "AuxiliaryOperations", "ProjectionCapabilities", "TechnicalHttpReceivers" }) Collection(Get(graph, property), property);
        int catalogBytes = ((byte[])Get(graph, "CanonicalCatalog")!).Length; total += catalogBytes;
        foreach (object host in Items(Get(graph, "RequiredHosts"))) Fields(host, "hosts", "HostId", "AppId", "InventoryDigest", "TopologyDigest", "AuthenticationFingerprint", "ExecutableDigest");
        foreach (object operation in Items(Get(graph, "AuxiliaryOperations")))
        {
            Fields(operation, "auxiliary", "AppId", "Method", "Operation", "Domain");
            if (Get(operation, "Http") is { } http)
            {
                Fields(http, "auxiliary.http", "Owner", "Verbs", "CodeDigest"); Collection(Get(http, "Grants"), "AuxiliaryOperations[].Http.Grants");
                foreach (object grant in Items(Get(http, "Grants"))) Fields(grant, "auxiliary.http.grants", "Method", "Verbs");
            }
            if (Get(operation, "NativeActor") is { } native) Fields(native, "auxiliary.native", "Kind", "Verbs", "CodeDigest", "FrameworkDigest", "InventoryDigest");
            if (Get(operation, "DaprCallback") is { } callback) Fields(callback, "auxiliary.callback", "Kind", "Verbs", "CodeDigest", "FrameworkDigest", "InventoryDigest");
            if (Get(operation, "DomainAuxiliary") is { } domain) Fields(domain, "auxiliary.domain", "Owner", "RequestType", "ScopeMethod", "CodeDigest");
            if (Get(operation, "Sidecar") is { } sidecar) Fields(sidecar, "auxiliary.sidecar", "Owner", "CodeDigest", "PubSubName", "TopicName");
        }
        foreach (object capability in Items(Get(graph, "ProjectionCapabilities")))
        {
            Collection(Get(capability, "ProjectionTypes"), "ProjectionCapabilities[].ProjectionTypes");
            Fields(capability, "capabilities", "AppId", "Domain", "ContractVersion", "Fingerprint"); Text("capabilities.types", Items(Get(capability, "ProjectionTypes")));
        }
        foreach (object receiver in Items(Get(graph, "TechnicalHttpReceivers")))
        {
            Fields(receiver, "receivers", "AppId", "FenceVersion"); Collection(Get(receiver, "Endpoints"), "TechnicalHttpReceivers[].Endpoints"); Collection(Get(receiver, "Invocations"), "TechnicalHttpReceivers[].Invocations");
            foreach (object endpoint in Items(Get(receiver, "Endpoints")))
            {
                Collection(Get(endpoint, "Methods"), "TechnicalHttpReceivers[].Endpoints[].Methods"); Collection(Get(endpoint, "Policies"), "TechnicalHttpReceivers[].Endpoints[].Policies");
                Fields(endpoint, "endpoints", "Pattern", "ActionIdentity", "RequestDigest", "ResponseDigest", "PolicyDigest", "FilterDigest", "CredentialKind");
                Text("endpoints.methods", Items(Get(endpoint, "Methods"))); Text("endpoints.policies", Items(Get(endpoint, "Policies")));
            }
            foreach (object invocation in Items(Get(receiver, "Invocations")))
            {
                Collection(Get(invocation, "HttpMethods"), "TechnicalHttpReceivers[].Invocations[].HttpMethods");
                Fields(invocation, "invocations", "CallerAppId", "Method"); Text("invocations.methods", Items(Get(invocation, "HttpMethods")));
            }
        }
        return new { canonicalCatalogBytes = catalogBytes, conservativeChargedBytes = total, inputByteBudget = 4 * 1024 * 1024,
            withinConservativeByteBudget = total <= 4 * 1024 * 1024, textChargedBytes = new Dictionary<string, long>(textCharges),
            collectionChargedBytes = new Dictionary<string, long>(collectionCharges), collections = collections.ToArray() };
    }
    object originalAccounting = Accounting(activation), frozenAccounting = Accounting(frozen);
    object?[] captureArgs = [frozen, null]; bool eligible = (bool)captureMethod.Invoke(null, captureArgs)!;
    int? actualBytes = captureArgs[1] is { } captured ? (int)Get(captured, "Bytes")! : null;
    bool? captureTrustExcluded = captureArgs[1] is { } cap ? (string)Get(Get(cap, "Activation"), "TrustProof")! == string.Empty : null;
    object Measure(int iterations, Func<object?> operation)
    {
        List<double> samples = [];
        for (int index = 0; index < iterations; index++) { long started = Stopwatch.GetTimestamp(); _ = operation(); samples.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds); }
        samples.Sort(); return new { iterations, minimumMilliseconds = samples[0], medianMilliseconds = samples[samples.Count / 2], maximumMilliseconds = samples[^1], totalMilliseconds = samples.Sum() };
    }
    object decodeMemo = Activator.CreateInstance(decodeMemoType, nonPublic: true)!;
    using IDisposable decoding = (IDisposable)enterDecode.Invoke(null, [decodeMemo])!;
    _ = coreMethod.Invoke(null, [frozen]);
    string coreDigest = (string)coreMethod.Invoke(null, [frozen])!;
    object freezeCosts = Measure(20, () => freezeMethod.Invoke(null, [activation]));
    object captureCosts = Measure(30, () => captureMethod.Invoke(null, [frozen, null]));
    object coreCosts = Measure(30, () => coreMethod.Invoke(null, [frozen]));
    object bindingMemo = Activator.CreateInstance(memoType, nonPublic: true)!;
    using IDisposable bindingScope = (IDisposable)enterBinding.Invoke(null, [bindingMemo])!;
    string scopedDigest = (string)publicMethod.Invoke(null, [frozen])!;
    object memoCosts = Measure(30, () => publicMethod.Invoke(null, [frozen]));
    object fullMemoCosts = Measure(20, () => publicMethod.Invoke(null, [freezeMethod.Invoke(null, [activation])!]));
    object fullCoreCosts = Measure(20, () => coreMethod.Invoke(null, [freezeMethod.Invoke(null, [activation])!]));
    var receipt = new { diagnosticOnly = true, featureAcceptance = false, actualActivation = true, trustProofRemovedBeforeDeserialization = true,
        activationCopyPersisted = false, actualFreezeInvoked = true, actualTryCaptureInvoked = true, captureEligible = eligible,
        actualCaptureChargedBytes = actualBytes, capturedTrustProofIsEmpty = captureTrustExcluded,
        accountingMatchesActualCapture = actualBytes is null ? (bool?)null : actualBytes == total,
        originalAccounting, frozenAccounting, bindingDigestsEqual = scopedDigest == coreDigest,
        bindingMemoCount = Get(bindingMemo, "Count"), bindingMemoRetainedBytes = Get(bindingMemo, "RetainedBytes"), bindingScopeCacheHits = Get(bindingScope, "CacheHits"),
        costs = new { freeze = freezeCosts, capture = captureCosts, coreWithWarmedDecodeMemo = coreCosts, bindingWithWarmedMemo = memoCosts, freezeAndMemo = fullMemoCosts, freezeAndCore = fullCoreCosts },
        assemblies = new[] { contracts, sdk }.Select(assembly => new { name = assembly.GetName().Name, sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly.Location))) }).ToArray(),
        limits = new[] { "External diagnostic process; no live authority result or trusted resource permission is established.", "Reflection/observer overhead and host load are included; no performance acceptance is claimed.", "Full accounting mirrors current source without early budget exits; only actual TryCapture establishes SDK eligibility." } };
    File.WriteAllText(outputPath, JsonSerializer.Serialize(receipt, json) + Environment.NewLine);
    ((IDisposable)bindingMemo).Dispose(); ((IDisposable)decodeMemo).Dispose();
}
catch (Exception exception)
{
    Exception cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
    File.WriteAllText(outputPath, JsonSerializer.Serialize(new { diagnosticOnly = true, featureAcceptance = false, failed = true, exceptionType = cause.GetType().Name }, json) + Environment.NewLine);
    Environment.ExitCode = 1;
}
