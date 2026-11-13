using System.Reflection;
using Hexalith.EventStore.ServiceDefaults.Routing;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class ImageFixture : IDisposable
{
    internal string Path { get; }
    internal Assembly Assembly { get; }
    internal ImageFixture(string directory)
    {
        Path = System.IO.Path.Combine(directory, "image.dll");
        Assembly source = typeof(RouteCatalogAuthority).Assembly;
        File.Copy(source.Location, Path, overwrite: false);
        Assembly = new DiskImageAssembly(source, Path);
    }
    public void Dispose() { if (File.Exists(Path)) File.Delete(Path); }
}
