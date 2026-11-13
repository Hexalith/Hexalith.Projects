using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Story65.Canonical.Harness;

/// <summary>Supplies only the already-qualified private development selector, without creating a host.</summary>
internal sealed class BenchmarkHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "Story65.Canonical.Harness";
    public string ContentRootPath { get; set; } = "/tmp/story65-canonical-hash-memo-final-compiled-prep-20261108";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
