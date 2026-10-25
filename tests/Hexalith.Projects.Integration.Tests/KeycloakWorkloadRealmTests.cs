// <copyright file="KeycloakWorkloadRealmTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;

using Shouldly;

using Xunit;

/// <summary>Protects the local EventStore workload client's authentication and least-privilege grants.</summary>
public sealed class KeycloakWorkloadRealmTests
{
    /// <summary>Verifies that workload credentials cannot be used for interactive or password login.</summary>
    [Fact]
    public void WorkloadClientShouldRequireGeneratedSecretAndServiceAccountAuthentication()
    {
        using JsonDocument realm = ReadRealm();
        JsonElement client = WorkloadClient(realm);

        client.GetProperty("enabled").GetBoolean().ShouldBeTrue();
        client.GetProperty("publicClient").GetBoolean().ShouldBeFalse();
        client.GetProperty("clientAuthenticatorType").GetString().ShouldBe("client-secret");
        client.GetProperty("secret").GetString().ShouldBe("${HEXALITH_EVENTSTORE_WORKLOAD_CLIENT_SECRET}");
        client.GetProperty("serviceAccountsEnabled").GetBoolean().ShouldBeTrue();
        client.GetProperty("directAccessGrantsEnabled").GetBoolean().ShouldBeFalse();
        client.GetProperty("standardFlowEnabled").GetBoolean().ShouldBeFalse();
        client.GetProperty("implicitFlowEnabled").GetBoolean().ShouldBeFalse();
        client.GetProperty("fullScopeAllowed").GetBoolean().ShouldBeFalse();
        int.Parse(client.GetProperty("attributes").GetProperty("access.token.lifespan").GetString()!,
            System.Globalization.CultureInfo.InvariantCulture).ShouldBeInRange(1, 300);
    }

    /// <summary>Verifies that the gateway must explicitly request one of the local domain grants.</summary>
    [Fact]
    public void WorkloadClientShouldGrantNoAuthorityByDefaultAndOnlyDeclareLocalOptionalScopes()
    {
        using JsonDocument realm = ReadRealm();
        JsonElement client = WorkloadClient(realm);

        client.GetProperty("defaultClientScopes").GetArrayLength().ShouldBe(0);
        string[] expected =
        [
            "eventstore-audience.projects",
            "eventstore-audience.tenants",
            "eventstore-operation.domain-service.process",
            "eventstore-operation.domain-service.replay-state",
            "eventstore-operation.domain-service.query",
            "eventstore-operation.domain-service.project",
            "eventstore-operation.domain-service.metadata",
        ];
        string[] actual = client.GetProperty("optionalClientScopes").EnumerateArray()
            .Select(static scope => scope.GetString()!).ToArray();
        actual.Order(StringComparer.Ordinal).ShouldBe(expected.Order(StringComparer.Ordinal));
        if (client.TryGetProperty("protocolMappers", out JsonElement clientMappers))
        {
            clientMappers.GetArrayLength().ShouldBe(0);
        }

        foreach (JsonElement other in realm.RootElement.GetProperty("clients").EnumerateArray()
            .Where(static item => item.GetProperty("clientId").GetString() != "eventstore"))
        {
            foreach (string property in new[] { "defaultClientScopes", "optionalClientScopes" })
            {
                if (other.TryGetProperty(property, out JsonElement scopes))
                {
                    scopes.EnumerateArray().ShouldNotContain(scope => expected.Contains(scope.GetString(), StringComparer.Ordinal));
                }
            }
        }
    }

    /// <summary>Verifies that each optional scope adds exactly its named audience or operation to access tokens.</summary>
    /// <param name="scopeName">The optional scope requested by the gateway.</param>
    /// <param name="mapperType">The permitted mapper type.</param>
    /// <param name="claimKey">The mapper configuration key holding the grant.</param>
    /// <param name="grant">The single allowed grant value.</param>
    [Theory]
    [InlineData("eventstore-audience.projects", "oidc-audience-mapper", "included.custom.audience", "projects")]
    [InlineData("eventstore-audience.tenants", "oidc-audience-mapper", "included.custom.audience", "tenants")]
    [InlineData("eventstore-operation.domain-service.process", "oidc-hardcoded-claim-mapper", "claim.value", "domain-service:process")]
    [InlineData("eventstore-operation.domain-service.replay-state", "oidc-hardcoded-claim-mapper", "claim.value", "domain-service:replay-state")]
    [InlineData("eventstore-operation.domain-service.query", "oidc-hardcoded-claim-mapper", "claim.value", "domain-service:query")]
    [InlineData("eventstore-operation.domain-service.project", "oidc-hardcoded-claim-mapper", "claim.value", "domain-service:project")]
    [InlineData("eventstore-operation.domain-service.metadata", "oidc-hardcoded-claim-mapper", "claim.value", "domain-service:metadata")]
    public void OptionalScopeShouldGrantExactlyItsNamedAuthority(string scopeName, string mapperType, string claimKey, string grant)
    {
        using JsonDocument realm = ReadRealm();
        JsonElement scope = realm.RootElement.GetProperty("clientScopes").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == scopeName);
        scope.GetProperty("protocol").GetString().ShouldBe("openid-connect");
        JsonElement mapper = scope.GetProperty("protocolMappers").EnumerateArray().ShouldHaveSingleItem();
        mapper.GetProperty("protocolMapper").GetString().ShouldBe(mapperType);
        mapper.GetProperty("protocol").GetString().ShouldBe("openid-connect");
        JsonElement config = mapper.GetProperty("config");
        config.GetProperty(claimKey).GetString().ShouldBe(grant);
        config.GetProperty("access.token.claim").GetString().ShouldBe("true");
        config.GetProperty("id.token.claim").GetString().ShouldBe("false");
        if (mapperType == "oidc-hardcoded-claim-mapper")
        {
            config.GetProperty("claim.name").GetString().ShouldBe("eventstore:operation");
            config.GetProperty("jsonType.label").GetString().ShouldBe("String");
        }
        else
        {
            config.TryGetProperty("included.client.audience", out _).ShouldBeFalse();
        }
    }

    private static JsonDocument ReadRealm() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
        TestRepositoryRoot.Find(), "src", "Hexalith.Projects.AppHost", "KeycloakRealms", "hexalith-realm.json")));

    private static JsonElement WorkloadClient(JsonDocument realm) => realm.RootElement.GetProperty("clients")
        .EnumerateArray().Single(static client => client.GetProperty("clientId").GetString() == "eventstore");
}
