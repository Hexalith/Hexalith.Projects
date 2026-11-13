using System.Reflection;
using System.Security.Cryptography;

namespace Story65.ImageMemo.Harness;

/// <summary>Builds the exact owner, DomainService and direct-reference cohort in original ordinal name order.</summary>
internal sealed class ImageInventory
{
    internal const string Workspace = "/home/administrator/projects/hexalith/projects";
    internal const string OwnerPath = Workspace + "/references/Hexalith.Folders/src/Hexalith.Folders.Server/bin/Debug/net10.0/Hexalith.Folders.Server.dll";
    private readonly InventoryLoadContext _context;

    private ImageInventory(InventoryLoadContext context, Assembly[] images, InventoryImage[] files)
    {
        _context = context;
        Images = images;
        Files = files;
    }

    internal Assembly[] Images { get; }
    internal InventoryImage[] Files { get; }
    internal long Bytes => Files.Sum(image => image.Bytes);
    internal long ChargedBytes => Files.Sum(image => image.Bytes + 512);
    internal int ImageSizeEligibleCount => Files.Count(image => image.Bytes is > 0 and <= 16 * 1024 * 1024);
    internal bool EntireCohortFitsOriginalBounds => ImageSizeEligibleCount == Images.Length && Images.Length <= 128 && ChargedBytes <= 64 * 1024 * 1024;

    internal static ImageInventory Load()
    {
        var context = new InventoryLoadContext(OwnerPath);
        using var reflection = context.EnterContextualReflection();
        Assembly owner = context.LoadFromAssemblyPath(OwnerPath);
        Assembly domain = context.LoadFromAssemblyName(new AssemblyName("Hexalith.EventStore.DomainService"));
        Assembly[] images = new[] { owner, domain }
            .Concat(owner.GetReferencedAssemblies().Select(Assembly.Load)).Distinct()
            .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal).ToArray();
        Require.That(images.Length == 66, "inventory-count");
        Require.That(images.Distinct(ReferenceEqualityComparer.Instance).Count() == images.Length, "inventory-reference-keys");
        InventoryImage[] files = images.Select((assembly, ordinal) => Describe(assembly, ordinal)).ToArray();
        InventoryImage sdkImage = files.Single(image => image.FullName.StartsWith("Hexalith.EventStore.ServiceDefaults,", StringComparison.Ordinal));
        Require.That(sdkImage.Sha256 == FinalTransferInputs.SdkSha256, "inventory-sdk-is-same-native-qualified-production-generation");
        Require.That(files.All(image => image.Bytes > 0), "inventory-positive-image-lengths");
        Require.That(files.Select(image => image.FullName).SequenceEqual(files.Select(image => image.FullName).OrderBy(name => name, StringComparer.Ordinal)), "inventory-original-name-order");
        return new ImageInventory(context, images, files);
    }

    internal static string HashFile(string path)
    {
        using FileStream file = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(file));
    }

    internal void VerifyUnchanged()
    {
        using var reflection = _context.EnterContextualReflection();
        for (int index = 0; index < Images.Length; index++)
        {
            InventoryImage current = Describe(Images[index], index);
            Require.That(current == Files[index], "inventory-file-or-loaded-metadata-changed");
        }
    }

    private static InventoryImage Describe(Assembly assembly, int ordinal)
    {
        string name = assembly.FullName ?? throw new InvalidOperationException("inventory-name");
        string path = assembly.Location;
        Require.That(!string.IsNullOrEmpty(path) && File.Exists(path), "inventory-readable-image");
        Require.That((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0, "inventory-regular-image");
        long bytes = new FileInfo(path).Length;
        string sha256 = HashFile(path);
        string mvid = assembly.ManifestModule.ModuleVersionId.ToString();
        return new InventoryImage(ordinal, name, path, bytes, sha256, mvid, $"{name}:{mvid}:{sha256}");
    }
}
