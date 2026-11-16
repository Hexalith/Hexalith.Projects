namespace Hexalith.Projects.PackageFixtures;

using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

/// <summary>Runs both real package-mode browser phases and always awaits AppHost and private-state teardown.</summary>
internal static class PackageFixtureRun
{
    internal static async Task RunAsync(string[] arguments)
    {
        if (arguments.Length != 2 || arguments[0] != "--root")
            throw new ArgumentException("Expected --root <Projects repository root>.");
        string root = Path.GetFullPath(arguments[1]);
        if (!File.Exists(Path.Combine(root, "Hexalith.Projects.CI.slnx")))
            throw new InvalidOperationException("Package fixture repository root is missing.");
        string host = Path.Combine(root, "tests/Hexalith.Projects.PackageFixtures.AppHost/bin/Release/net11.0/Hexalith.Projects.PackageFixtures.AppHost.dll");
        if (!File.Exists(host)) throw new InvalidOperationException("Build the Release/package fixture AppHost before running its driver.");
        if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") is { } environment && environment != "Development")
            throw new InvalidOperationException("Package fixtures cannot activate outside their private Development context.");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("DOTNET_WATCH", "false");
        Type entryPoint = Assembly.LoadFrom(host).GetType("Program")
            ?? throw new InvalidOperationException("The prebuilt AppHost entry point is missing.");
        foreach (bool actualOwners in new[] { false, true })
        {
            Console.WriteLine(actualOwners ? "[package-fixtures] actual-owner journey" : "[package-fixtures] compatibility browser phases");
            FixtureScratch scratch = new(actualOwners);
            await using var scratchCleanup = scratch.ConfigureAwait(false);
            using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(45));
            FixtureInputs inputs = new(scratch, root, actualOwners);
            await inputs.PrepareAsync(timeout.Token).ConfigureAwait(false);
            PackageApplicationFactory factory = new(entryPoint, ["--environment", "Development",
                "--Logging:LogLevel:Default=Warning", "--Projects:PackageFixtures:Enabled=true", "--Projects:PackageFixtures:ConfigurationFile=" + scratch.File("configuration.json")]);
            await using var factoryCleanup = factory.ConfigureAwait(false);
            DistributedApplication? application = null;
            try
            {
                using (CancellationTokenSource startup = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token))
                {
                    startup.CancelAfter(TimeSpan.FromMinutes(5));
                    await factory.StartAsync(startup.Token).ConfigureAwait(false);
                    application = factory.Application ?? throw new InvalidOperationException("AppHost did not publish its built application.");
                    string[] resources = actualOwners
                        ? ["eventstore", "tenants", "projects", "projects-workers", "projects-ui", "folders", "folders-workers", "browser-read-observer"]
                        : ["eventstore", "tenants", "projects", "projects-workers", "projects-ui", "conversations", "folders", "memories", "live-fixtures"];
                    foreach (string resource in resources)
                        await application.ResourceNotifications.WaitForResourceHealthyAsync(resource, startup.Token).ConfigureAwait(false);
                }
                FixtureProductChecks checks = new(inputs, scratch, factory);
                await checks.VerifyBootstrapAsync(timeout.Token).ConfigureAwait(false);
                if (!actualOwners)
                {
                    await checks.SeedCompatibilityTenantAsync(timeout.Token).ConfigureAwait(false);
                    await VerifyReadRecoveryAsync(root, factory, inputs, scratch, timeout.Token).ConfigureAwait(false);
                }
                if (actualOwners)
                {
                    await checks.SeedOwnersAsync(timeout.Token).ConfigureAwait(false);
                    await checks.RegisterObservationsAsync(timeout.Token).ConfigureAwait(false);
                }
                FixtureTelemetry? telemetry = actualOwners ? new FixtureTelemetry(application,
                    ["eventstore", "tenants", "projects", "projects-workers", "projects-ui", "folders", "folders-workers"]) : null;
                if (telemetry is not null) await telemetry.BeginAsync(timeout.Token).ConfigureAwait(false);
                Dictionary<string, string> browser = new()
                {
                    ["CI"] = "true", ["E2E_LIVE_APPHOST"] = "1", ["E2E_PACKAGE_FIXTURES"] = "1",
                    ["E2E_SUPPORTED_READ_APPHOST"] = actualOwners ? "1" : "0",
                    ["E2E_RUN_ID"] = scratch.Isolation, ["BASE_URL"] = factory.GetEndpoint("projects-ui", "http").ToString(),
                    ["API_URL"] = factory.GetEndpoint("projects", "http").ToString(),
                    ["EVENTSTORE_API_URL"] = factory.GetEndpoint("eventstore", "http").ToString(),
                    ["FIXTURE_API_URL"] = factory.GetEndpoint(actualOwners ? "browser-read-observer" : "live-fixtures", "http").ToString(),
                    ["KEYCLOAK_URL"] = new Uri(inputs.Authority).GetLeftPart(UriPartial.Authority),
                    ["KEYCLOAK_CLIENT_ID"] = "supported-read-bootstrap", ["KEYCLOAK_REALM"] = "hexalith",
                    ["TEST_USER_USERNAME"] = "actor-1", ["TEST_USER_PASSWORD"] = inputs.Password,
                    ["E2E_FOREIGN_USERNAME"] = "foreign-actor", ["E2E_FOREIGN_PASSWORD"] = inputs.Password,
                    ["TEST_TENANT_ID"] = inputs.Tenant, ["TEST_PRINCIPAL_ID"] = inputs.Subjects["actor-1"],
                    ["E2E_BROWSER_SESSION_FILE"] = scratch.File("browser-session.json"),
                    ["E2E_PACKAGE_OUTPUT"] = scratch.File("browser-output"),
                    ["NODE_EXTRA_CA_CERTS"] = scratch.File("certificate.pem"),
                    ["E2E_PROTECTED_FIXTURE_MARKERS"] = "[\"Synthetic project\",\"Synthetic descriptive metadata\",\"Restricted legacy\"]",
                };
                string workspace = Path.Combine(root, "tests/e2e");
                await FixtureProcess.RunAsync("node", [Path.Combine(workspace, "node_modules/typescript/bin/tsc"), "--noEmit"],
                    workspace, timeout.Token, browser, inputs.ProtectedValues, printOutput: true).ConfigureAwait(false);
                if (actualOwners)
                    await BrowserAsync("actual-owner", ["specs/projects-supported-read-browser.spec.ts"], ["--workers", "1"],
                        workspace, browser, inputs, scratch, timeout.Token).ConfigureAwait(false);
                else
                {
                    foreach ((string phase, string[] selectors, string[] extra) in new[]
                    {
                        ("smoke", new[] { "specs/live-apphost-startup.spec.ts", "specs/projects-authentication.spec.ts" }, Array.Empty<string>()),
                        ("full", Array.Empty<string>(), Array.Empty<string>()),
                        ("repeat", new[] { "specs/live-fixtures-lifecycle.spec.ts" }, new[] { "--repeat-each", "2" }),
                        ("retry", new[] { "specs/live-fixtures-retry.spec.ts" }, new[] { "--retries", "1", "--repeat-each", "2" }),
                    })
                        await BrowserAsync(phase, selectors, extra, workspace, browser, inputs, scratch, timeout.Token).ConfigureAwait(false);
                }
                if (telemetry is not null) await telemetry.VerifyAsync(inputs.ProtectedValues, timeout.Token).ConfigureAwait(false);
                await checks.VerifyDurableStateAsync(actualOwners, timeout.Token).ConfigureAwait(false);
            }
            catch
            {
                await FixtureDiagnostics.ResourcesAsync(application ?? factory.Application).ConfigureAwait(false);
                throw;
            }
            finally
            {
                application ??= factory.Application;
                if (application is not null)
                {
                    using CancellationTokenSource stop = new(TimeSpan.FromMinutes(2));
                    await application.StopAsync(stop.Token).ConfigureAwait(false);
                }
            }
        }
    }

    private static async Task BrowserAsync(string phase, string[] selectors, string[] extra, string workspace,
        Dictionary<string, string> environment, FixtureInputs inputs, FixtureScratch scratch, CancellationToken cancellationToken)
    {
        environment["E2E_RUN_ID"] = phase == "actual-owner" ? scratch.Isolation : scratch.Isolation + "-" + phase;
        environment["E2E_FIXTURE_RETRY_PROBE"] = phase == "retry" ? "1" : "0";
        environment["E2E_FIXTURE_RETRY_DIRECTORY"] = scratch.File("retry-probes");
        environment["E2E_PROPOSAL_OBSERVATIONS_FILE"] = scratch.File(phase + "-proposal-observations.jsonl");
        environment["E2E_PACKAGE_OUTPUT"] = scratch.File(phase + "-output");
        string[] arguments = [Path.Combine(workspace, "node_modules/@playwright/test/cli.js"), "test", .. selectors,
            "--project", "chromium", "--workers", "2", "--retries", "0", .. extra];
        string output = await FixtureProcess.RunAsync("node", arguments, workspace, cancellationToken,
            environment, inputs.ProtectedValues, printOutput: true).ConfigureAwait(false);
        Match counts = Regex.Match(output, @"\[zero-live-skip\] collected=(\d+) skipped=(\d+) status=passed");
        if (!counts.Success || int.Parse(counts.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) <= 0
            || counts.Groups[2].Value != "0")
            throw new InvalidOperationException("The complete browser phase did not prove a nonempty zero-skip result: " + phase);
    }

    private static async Task VerifyReadRecoveryAsync(string root, PackageApplicationFactory factory, FixtureInputs inputs,
        FixtureScratch scratch, CancellationToken cancellationToken)
    {
        const string method = "Hexalith.Projects.Server.Tests.Queries.SupportedProjectReadHttpTests.ProtectedSdkDispatchAndReadsSurviveHostAndStoreClientRestart";
        string assembly = Path.Combine(root, "tests/Hexalith.Projects.Server.Tests/bin/Release/net11.0/Hexalith.Projects.Server.Tests.dll");
        string result = scratch.File("read-recovery.json");
        Dictionary<string, string> environment = new()
        {
            ["STORY61_DAPR_HTTP_ENDPOINT"] = factory.GetEndpoint("projects-dapr-cli", "http").ToString(),
            ["STORY61_DAPR_GRPC_ENDPOINT"] = factory.GetEndpoint("projects-dapr-cli", "grpc").ToString(),
        };
        await FixtureProcess.RunAsync("dotnet", [assembly, "-method", method, "-noColor", "-result-ctrf", result],
            root, cancellationToken, environment, inputs.ProtectedValues, printOutput: true).ConfigureAwait(false);
        using JsonDocument report = JsonDocument.Parse(await File.ReadAllBytesAsync(result, cancellationToken).ConfigureAwait(false));
        JsonElement tests = report.RootElement.GetProperty("results").GetProperty("tests");
        if (tests.GetArrayLength() != 1 || tests[0].GetProperty("status").GetString() != "passed")
            throw new InvalidOperationException("The real SDK host/store-client restart assertion did not pass without skips.");
    }
}
