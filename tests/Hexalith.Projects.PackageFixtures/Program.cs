using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Projects.PackageFixtures;

if (args.SequenceEqual(new[] { "--authority" }))
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args: ["--preventHostingStartup=true"]);
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("The package fixture authority requires private Development isolation.");
    builder.Services.AddEventStoreWorkloadBindingAuthority(builder.Configuration);
    WebApplication app = builder.Build();
    app.MapEventStoreWorkloadBindingAuthority();
    app.MapGet("/alive", () => Results.Ok());
    await app.RunAsync().ConfigureAwait(false);
}
else if (args.SequenceEqual(new[] { "--projects-worker" }))
{
    await PackageWorkerHost.RunAsync().ConfigureAwait(false);
}
else
{
    try
    {
        await PackageFixtureRun.RunAsync(args).ConfigureAwait(false);
    }
    catch (Exception failure)
    {
        Console.Error.WriteLine("Package fixture failure: " + failure.GetType().Name + ": " + failure.Message);
        foreach (string stage in FixtureDiagnostics.FailureStages(failure))
            Console.Error.WriteLine("[package-fixtures] failure-stage=" + stage);
        return 1;
    }
}

return 0;
