namespace Story65.ImageMemo.Harness;
using System.Reflection;
internal sealed class ImageAssembly(Assembly original, string location) : Assembly
{
    public override string Location => location;
    public override string? FullName => original.FullName;
    public override Module ManifestModule => original.ManifestModule;
}
