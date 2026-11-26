using System.Text.Json;

using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Server.Extensions;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Projects.Contracts.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using JsonDocument manifest = JsonDocument.Parse(File.ReadAllBytes(args[0]));
JsonElement owner = manifest.RootElement.GetProperty("modules").EnumerateArray().Single();
string moduleId = owner.GetProperty("identity").GetProperty("moduleId").GetString()!;
JsonElement extension = owner.GetProperty("runtime").GetProperty("eventStoreExtensions").EnumerateArray().Single();
string packageId = extension.GetProperty("packageId").GetString()!;
string extensionId = extension.GetProperty("extensionId").GetString()!;
int apiMajor = extension.GetProperty("extensionApiMajor").GetInt32();
if (moduleId != "projects" || packageId != "Hexalith.Projects.EventStoreExtensions"
    || extensionId != "projects" || apiMajor != 1 || extension.GetProperty("configurationInputs").GetArrayLength() != 0)
{
    throw new InvalidOperationException("The packed Projects extension manifest has unexpected configuration.");
}

const string prefix = EventStoreExtensionServiceCollectionExtensions.ConfigurationSection + ":0:";
IConfiguration Configuration(int major) => new ConfigurationBuilder().AddInMemoryCollection(
    new Dictionary<string, string?>
    {
        [prefix + "ModuleId"] = moduleId,
        [prefix + "PackageId"] = packageId,
        [prefix + "ExtensionId"] = extensionId,
        [prefix + "ApiMajor"] = major.ToString(System.Globalization.CultureInfo.InvariantCulture),
    }).Build();

var routes = CompiledRouteCatalogDeclarations.Read([typeof(ProjectsRuntimeCatalogDeclarations).Assembly]);
string[] expected = routes.Where(route => route.Kind == "command").Select(route => route.RouteId)
    .Order(StringComparer.Ordinal).ToArray();

var services = new ServiceCollection();
services.AddLogging();
services.AddEventStoreExtensions(Configuration(apiMajor));
using ServiceProvider provider = services.BuildServiceProvider();
string[] registered = provider.GetServices<IIdempotencyIntentAdapter>().Cast<IDeclaredIdempotencyIntentAdapter>()
    .Select(adapter => adapter.Declaration.Route.RouteId).Order(StringComparer.Ordinal).ToArray();
if (expected.Length != 12 || registered.Distinct(StringComparer.Ordinal).Count() != 12
    || !registered.SequenceEqual(expected, StringComparer.Ordinal))
{
    throw new InvalidOperationException("The packed extension did not register each of its 12 declared command routes once.");
}

HealthReport report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(check => check.Tags.Contains("ready"));
if (!report.Entries.TryGetValue(EventStoreExtensionServiceCollectionExtensions.HealthCheckName, out HealthReportEntry readiness)
    || readiness.Status != HealthStatus.Healthy || !readiness.Data.TryGetValue(extensionId, out object? version) || !Equals(version, apiMajor))
{
    throw new InvalidOperationException("The packed extension did not report healthy readiness for its declared identity and major.");
}

var refused = new ServiceCollection();
try
{
    refused.AddEventStoreExtensions(Configuration(apiMajor + 1));
    throw new InvalidOperationException("The gateway accepted an unsupported extension API major.");
}
catch (EventStoreExtensionException error) when (error.Condition == EventStoreExtensionConditions.UnsupportedMajor)
{
    if (refused.Any(service => service.ServiceType == typeof(IIdempotencyIntentAdapter)))
    {
        throw new InvalidOperationException("The gateway partially registered a refused extension.");
    }
}

Console.WriteLine("Packed Projects extension registered 12 declared adapters, reported healthy readiness and refused the wrong API major.");
