using System.Reflection;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class DiskImageAssembly(Assembly actual, string path) : Assembly
{
    public override string Location => path;
    public override string? FullName => actual.FullName;
    public override Module ManifestModule => actual.ManifestModule;
}
