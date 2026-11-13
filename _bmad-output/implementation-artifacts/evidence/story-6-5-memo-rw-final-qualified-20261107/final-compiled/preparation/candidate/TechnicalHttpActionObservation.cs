using System.Buffers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Hexalith.EventStore.ServiceDefaults.Routing;

namespace Story65.ImageMemo.Candidate;

internal static class TechnicalHttpActionObservation
{
    private const int MaximumPooledImageLength = 16 * 1024 * 1024;
    private static readonly ArrayPool<byte> ImageBuffers = ArrayPool<byte>.Create(MaximumPooledImageLength, 1);
    /// <summary>Recaptures the loaded module's exact current image identity through the bounded private buffer pool.</summary>
    internal static string AssemblyIdentity(Assembly assembly)
        => AssemblyIdentity(assembly, RouteCatalogImageBuffers.Select(ImageBuffers));

    /// <summary>Reads a fresh privately owned image, returning its borrowed buffer only after PE validation and hashing finish.</summary>
    internal static string AssemblyIdentity(Assembly assembly, ArrayPool<byte> imageBuffers)
    {
        if (string.IsNullOrEmpty(assembly.Location)) throw new RouteCatalogUnavailableException("The loaded technical SDK assembly has no independently readable image.");
        byte[]? bytes = null;
        int length = 0;
        bool borrowed = false;
        try
        {
            using (var image = File.OpenRead(assembly.Location))
            {
                long observedLength = image.Length;
                if (observedLength <= MaximumPooledImageLength)
                {
                    length = checked((int)observedLength);
                    bytes = imageBuffers.Rent(length);
                    borrowed = true;
                    image.ReadExactly(bytes.AsSpan(0, length));
                }
            }
            if (!borrowed)
            {
                bytes = File.ReadAllBytes(assembly.Location);
                length = bytes.Length;
            }
            using var pe = new PEReader(new MemoryStream(bytes!, 0, length, writable: false));
            var metadata = pe.GetMetadataReader();
            if (metadata.GetGuid(metadata.GetModuleDefinition().Mvid) != assembly.ManifestModule.ModuleVersionId)
                throw new RouteCatalogUnavailableException("The installed technical SDK image differs from its loaded module.");
            string digest = RouteCatalogImageDigestScope.Current is { } scope && scope.TryCompute(assembly, bytes.AsSpan(0, length), out string computed)
                ? computed : Convert.ToHexStringLower(SHA256.HashData(bytes.AsSpan(0, length)));
            return $"{assembly.FullName}:{assembly.ManifestModule.ModuleVersionId}:{digest}";
        }
        finally
        {
            if (borrowed) imageBuffers.Return(bytes!);
        }
    }
}
