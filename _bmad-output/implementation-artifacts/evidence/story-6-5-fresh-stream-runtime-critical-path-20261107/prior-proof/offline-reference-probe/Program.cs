using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

const long ImageLimit = 16 * 1024 * 1024, ChargedLimit = 64 * 1024 * 1024;
const int EntryLimit = 128;
var results = new List<object>();
foreach (string path in args.Skip(1))
{
    var context = new OwnerContext(path);
    using var reflection = context.EnterContextualReflection();
    Assembly owner = context.LoadFromAssemblyPath(path);
    Assembly sdk = context.LoadFromAssemblyName(new AssemblyName("Hexalith.EventStore.DomainService"));
    Assembly[] directReferences = owner.GetReferencedAssemblies().Select(Assembly.Load).ToArray();
    Assembly[] previous = new[] { owner, sdk }.Concat(directReferences).Distinct().OrderBy(item => item.FullName, StringComparer.Ordinal).ToArray();
    var unstable = new HashSet<string>(StringComparer.Ordinal);
    bool sameReferenceSequence = true;
    for (int iteration = 0; iteration < 100; iteration++)
    {
        Assembly[] loaded = owner.GetReferencedAssemblies().Select(Assembly.Load).ToArray();
        Assembly[] current = new[] { owner, sdk }.Concat(loaded).Distinct().OrderBy(item => item.FullName, StringComparer.Ordinal).ToArray();
        if (loaded.Length != directReferences.Length || current.Length != previous.Length) sameReferenceSequence = false;
        for (int index = 0; index < Math.Min(loaded.Length, directReferences.Length); index++)
            if (!ReferenceEquals(loaded[index], directReferences[index])) unstable.Add(loaded[index].FullName ?? "unnamed");
        for (int index = 0; index < Math.Min(current.Length, previous.Length); index++)
            if (!ReferenceEquals(current[index], previous[index])) sameReferenceSequence = false;
    }
    var images = previous.Select(item =>
    {
        string location = item.Location;
        var file = string.IsNullOrEmpty(location) ? null : new FileInfo(location);
        long? bytes = file?.Exists == true ? file.Length : null;
        return new { assembly = item.GetName().Name, fullName = item.FullName, location, bytes,
            chargedBytes = bytes + 512, withinPerImageLimit = bytes is > 0 and <= ImageLimit,
            context = AssemblyLoadContext.GetLoadContext(item)?.Name };
    }).ToArray();
    var eligible = images.Where(item => item.withinPerImageLimit).ToArray();
    long charge = eligible.Sum(item => item.chargedBytes ?? 0);
    long sdkCharge = images.Single(item => item.assembly == "Hexalith.EventStore.ServiceDefaults").chargedBytes ?? 0;
    results.Add(new { owner = owner.GetName().Name, ownerPath = path, directReferenceCount = directReferences.Length,
        distinctOwnersSdkAndDirectReferences = images.Length, observedIterations = 100,
        unstableAssemblyLoadReferences = unstable.ToArray(), allAssemblyLoadReferencesStable = unstable.Count == 0,
        completeOrderedReferenceSequenceStable = sameReferenceSequence,
        imageCountLimit = EntryLimit, perImageBytesLimit = ImageLimit, chargedBytesLimit = ChargedLimit,
        eligibleImages = eligible.Length, ineligibleImages = images.Where(item => !item.withinPerImageLimit).ToArray(),
        totalImageBytes = images.Sum(item => item.bytes ?? 0), totalChargedBytes = charge,
        countFits = eligible.Length <= EntryLimit, totalChargeFits = charge <= ChargedLimit,
        extraTechnicalImagesHeadroomBytes = ChargedLimit - charge, countHeadroom = EntryLimit - eligible.Length,
        existingTechnicalSdkIsAlreadyInInventory = sdkCharge > 0,
        coverageLimit = "Exact owners + DomainService SDK + each owner's direct Assembly.Load references. Discovered projection/aggregate/payload/processor/handler assemblies are not instantiated or separately runtime-observed here; compare this metadata inventory with the actual capture census before treating it as exhaustive.",
        images });
}
File.WriteAllText(args[0], JsonSerializer.Serialize(new { metadataOnly = true, hostsInstantiated = false,
    credentialsRead = false, ownerOutputsEdited = false, runtime = Environment.Version.ToString(), results }, new JsonSerializerOptions { WriteIndented = true }));

sealed class OwnerContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    public OwnerContext(string path) : base(Path.GetFileNameWithoutExtension(path), isCollectible: false) => _resolver = new(path);
    protected override Assembly? Load(AssemblyName name)
    {
        string? path = _resolver.ResolveAssemblyToPath(name);
        return path is null ? null : LoadFromAssemblyPath(path);
    }
}
