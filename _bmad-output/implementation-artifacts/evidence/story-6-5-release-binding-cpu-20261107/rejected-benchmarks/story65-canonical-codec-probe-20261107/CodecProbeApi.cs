using System.Reflection;
using Hexalith.EventStore.Contracts.Routing;
sealed class CodecProbeApi(Type type)
{
    public string Name => type.Name;
    public Func<RouteCatalogEnvelope,byte[]> Encode { get; } = type.GetMethod("Encode")!.CreateDelegate<Func<RouteCatalogEnvelope,byte[]>>();
    public Func<ReadOnlyMemory<byte>,string?,RouteCatalogEnvelope> Decode { get; } = type.GetMethod("Decode")!.CreateDelegate<Func<ReadOnlyMemory<byte>,string?,RouteCatalogEnvelope>>();
    public Func<RouteCatalogEnvelope,RouteCatalogEnvelope> Seal { get; } = type.GetMethod("Seal")!.CreateDelegate<Func<RouteCatalogEnvelope,RouteCatalogEnvelope>>();
    public Func<RouteCatalogEnvelope,string> Root { get; } = type.GetMethod("ComputeRootDigest")!.CreateDelegate<Func<RouteCatalogEnvelope,string>>();
    public Func<RouteContractDeclaration,string> Declaration { get; } = type.GetMethod("ComputeDeclarationDigest")!.CreateDelegate<Func<RouteContractDeclaration,string>>();
    public Func<IdempotencyDescriptorSchema,string> Descriptor { get; } = type.GetMethod("ComputeDescriptorDigest")!.CreateDelegate<Func<IdempotencyDescriptorSchema,string>>();
    public Func<IdempotencyCatalogEntry,string> Entry { get; } = type.GetMethod("ComputeIdempotencyEntryDigest")!.CreateDelegate<Func<IdempotencyCatalogEntry,string>>();
    public Func<IdempotencyCatalogEntry,string> Policy { get; } = type.GetMethod("ComputeIdempotencyPolicyDigest")!.CreateDelegate<Func<IdempotencyCatalogEntry,string>>();
    public Func<RouteOperationContract,string> Operation { get; } = type.GetMethod("ComputeOperationContractDigest")!.CreateDelegate<Func<RouteOperationContract,string>>();
    public byte[] Raw(object? value)
    {
        try { return (byte[])type.GetMethod("CanonicalBytes",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(value?.GetType()??typeof(object)).Invoke(null,[value])!; }
        catch(TargetInvocationException e) when(e.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw; }
    }
}
