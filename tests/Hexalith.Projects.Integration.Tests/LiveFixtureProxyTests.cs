// <copyright file="LiveFixtureProxyTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Integration.Tests;

using System.Net;
using System.Text.Json;

using Hexalith.Projects.E2E.Fixtures;

using Microsoft.Extensions.Configuration;

using Shouldly;

using Xunit;

/// <summary>Verifies the control resource's typed, metadata-only seed and reverse-order cleanup contract.</summary>
public sealed class LiveFixtureProxyTests
{
    /// <summary>Verifies cleanup attempts roles in reverse provisioning order with typed status results.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RemoveShouldAttemptRolesInReverseOrderWithTypedStatuses()
    {
        FixtureRoleStubHandler handler = new(static (_, role) => role switch
        {
            "memories" => HttpStatusCode.NoContent,
            "folders" => null,
            _ => HttpStatusCode.NotFound,
        });

        FixtureCleanupResult result = await CreateProxy(handler)
            .RemoveAsync("graph-1", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        handler.Requests.ShouldBe(new[] { "DELETE memories", "DELETE folders", "DELETE conversations" });
        result.Attempts.ShouldBe(new[]
        {
            new FixtureCleanupAttempt("memories", 204),
            new FixtureCleanupAttempt("folders", null),
            new FixtureCleanupAttempt("conversations", 404),
        });
        result.Attempts.Select(static attempt => attempt.Succeeded).ShouldBe(new[] { true, false, true });
        result.Succeeded.ShouldBeFalse();
    }

    /// <summary>Verifies a seed failure compensates the failed role and every earlier role in reverse order.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SeedFailureShouldCompensateFailedAndEarlierRolesInReverseOrder()
    {
        FixtureRoleStubHandler handler = new(static (method, role) => (method.Method, role) switch
        {
            ("POST", "folders") => HttpStatusCode.InternalServerError,
            ("POST", _) => HttpStatusCode.OK,
            _ => HttpStatusCode.NoContent,
        });

        FixtureSeedFailure? failure = await CreateProxy(handler)
            .SeedAsync(Graph(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        failure.ShouldNotBeNull();
        failure.FailedRole.ShouldBe("folders");
        failure.StatusCode.ShouldBe(500);
        failure.Compensation.Attempts.Select(static attempt => attempt.Role).ShouldBe(new[] { "folders", "conversations" });
        failure.Compensation.Succeeded.ShouldBeTrue();
        handler.Requests.ShouldBe(new[] { "POST conversations", "POST folders", "DELETE folders", "DELETE conversations" });
    }

    /// <summary>Verifies a fully accepted seed reports no failure and touches roles in provisioning order.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SeedShouldReportNoFailureWhenEveryRoleAccepts()
    {
        FixtureRoleStubHandler handler = new(static (_, _) => HttpStatusCode.OK);

        FixtureSeedFailure? failure = await CreateProxy(handler)
            .SeedAsync(Graph(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        failure.ShouldBeNull();
        handler.Requests.ShouldBe(new[] { "POST conversations", "POST folders", "POST memories" });
    }

    /// <summary>Verifies cleanup and seed-failure evidence serialize as role/status metadata only.</summary>
    [Fact]
    public void CleanupEvidenceShouldSerializeRoleAndStatusOnly()
    {
        FixtureSeedFailure failure = new(
            "folders",
            null,
            new FixtureCleanupResult([new FixtureCleanupAttempt("folders", null), new FixtureCleanupAttempt("conversations", 204)]));

        string json = JsonSerializer.Serialize(failure, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldBe(
            "{\"failedRole\":\"folders\",\"statusCode\":null,\"compensation\":{\"attempts\":["
            + "{\"role\":\"folders\",\"statusCode\":null,\"succeeded\":false},"
            + "{\"role\":\"conversations\",\"statusCode\":204,\"succeeded\":true}],\"succeeded\":false}}");
    }

    private static FixtureProxy CreateProxy(FixtureRoleStubHandler handler)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FixtureEndpoints:Conversations"] = "http://conversations",
                ["FixtureEndpoints:Folders"] = "http://folders",
                ["FixtureEndpoints:Memories"] = "http://memories",
            })
            .Build();
        return new FixtureProxy(new HttpClient(handler), configuration);
    }

    private static LiveFixtureGraph Graph() => LiveFixtureGraphContractTests.ValidGraph();
}
