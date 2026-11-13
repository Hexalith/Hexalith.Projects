namespace Story65.InvocationFixture;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

internal sealed class FixtureEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "synthetic-fixture";
    public string ContentRootPath { get; set; } = "/tmp";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
