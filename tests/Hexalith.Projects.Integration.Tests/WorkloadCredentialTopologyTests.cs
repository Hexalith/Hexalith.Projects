namespace Hexalith.Projects.Integration.Tests;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;
using global::Aspire.Hosting.Testing;

using Shouldly;

using Xunit;

/// <summary>Verifies the actual AppHost shares one secret across the local workload issuer and authority.</summary>
public sealed class WorkloadCredentialTopologyTests
{
    /// <summary>Verifies both local container lifetime modes wire the exact same secret parameter.</summary>
    /// <param name="persistent">Whether the local Keycloak resource persists across sessions.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KeycloakAndEventStoreShouldShareOneSecretParameterAsync(bool persistent)
    {
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_Projects_AppHost>(
                ["--EnableKeycloak=true", $"--KeycloakPersistent={persistent}", "--Projects:E2E:LiveFixtures=false"],
                TestContext.Current.CancellationToken).ConfigureAwait(true);
        await using var lifetime = builder.ConfigureAwait(true);
        ParameterResource secret = builder.Resources.OfType<ParameterResource>()
            .Single(static resource => resource.Name == "eventstore-workload-client-secret");
        secret.Secret.ShouldBeTrue();
        if (persistent)
        {
            // Aspire's persistence wrapper is internal; inspect it without resolving or storing a credential.
            secret.Default.ShouldNotBeNull().GetType().Name.ShouldBe("UserSecretsParameterDefault");
        }
        else
        {
            secret.Default.ShouldBeOfType<GenerateParameterDefault>().MinLength.ShouldBeGreaterThanOrEqualTo(32);
        }

        IResource keycloak = builder.Resources.Single(static resource => resource.Name == "security");
        IResource gateway = builder.Resources.Single(static resource => resource.Name == "eventstore");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        IReadOnlyDictionary<string, object> authorityEnvironment = await EnvironmentAsync(keycloak, builder.ExecutionContext, timeout.Token).ConfigureAwait(true);
        IReadOnlyDictionary<string, object> gatewayEnvironment = await EnvironmentAsync(gateway, builder.ExecutionContext, timeout.Token).ConfigureAwait(true);
        authorityEnvironment["HEXALITH_EVENTSTORE_WORKLOAD_CLIENT_SECRET"].ShouldBeSameAs(secret);
        gatewayEnvironment["Authentication__WorkloadIssuer__ClientSecret"].ShouldBeSameAs(secret);
        gatewayEnvironment["Authentication__WorkloadIssuer__ClientId"].ShouldBe("eventstore");
    }

    /// <summary>Verifies the explicit security-disabled profile adds no unused workload secret or credentials.</summary>
    [Fact]
    public async Task DisabledKeycloakShouldNotConfigureWorkloadCredentialsAsync()
    {
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_Projects_AppHost>(
                ["--EnableKeycloak=false", "--Projects:E2E:LiveFixtures=false"],
                TestContext.Current.CancellationToken).ConfigureAwait(true);
        await using var lifetime = builder.ConfigureAwait(true);
        builder.Resources.ShouldNotContain(static resource => resource.Name == "eventstore-workload-client-secret");
        IResource gateway = builder.Resources.Single(static resource => resource.Name == "eventstore");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        IReadOnlyDictionary<string, object> environment = await EnvironmentAsync(gateway, builder.ExecutionContext, timeout.Token).ConfigureAwait(true);
        environment.ShouldNotContainKey("Authentication__WorkloadIssuer__ClientSecret");
        environment.ShouldNotContainKey("Authentication__WorkloadIssuer__ClientId");
    }

    private static async Task<IReadOnlyDictionary<string, object>> EnvironmentAsync(
        IResource resource,
        DistributedApplicationExecutionContext execution,
        CancellationToken cancellationToken)
    {
        var context = new EnvironmentCallbackContext(execution, resource, new Dictionary<string, object>(), cancellationToken);
        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context).WaitAsync(cancellationToken).ConfigureAwait(true);
        }

        return context.EnvironmentVariables;
    }
}
