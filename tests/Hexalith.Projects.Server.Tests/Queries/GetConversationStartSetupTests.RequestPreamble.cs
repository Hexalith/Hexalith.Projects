// <copyright file="GetConversationStartSetupTests.RequestPreamble.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ByteAether.Ulid;

using Microsoft.AspNetCore.Builder;

using Shouldly;

using Xunit;

/// <summary>Exercises the shared legacy query boundary through real HTTP requests.</summary>
public sealed partial class GetConversationStartSetupTests
{
    /// <summary>Gets every retained read operation, including the read-only POST proposal preview.</summary>
    public static TheoryData<string, string> ReadRequestRoutes => new()
    {
        { "GET", "/api/v1/projects" },
        { "GET", $"/api/v1/projects/{ProjectIdValue}" },
        { "GET", $"/api/v1/projects/{ProjectIdValue}/operator-diagnostics" },
        { "GET", $"/api/v1/projects/{ProjectIdValue}/conversations" },
        { "GET", $"/api/v1/projects/{ProjectIdValue}/context" },
        { "GET", $"/api/v1/projects/{ProjectIdValue}/context/explain" },
        { "GET", $"/api/v1/projects/{ProjectIdValue}/context/refresh" },
        { "GET", $"/api/v1/projects/{ProjectIdValue}/setup/conversation-start" },
        { "GET", "/api/v1/projects/resolution/from-conversation?conversationId=conversation-a" },
        { "GET", "/api/v1/projects/resolution/from-attachments?fileId=unsupported-file" },
        { "POST", "/api/v1/projects/resolution/new-project-proposal" },
    };

    [Theory]
    [MemberData(nameof(ReadRequestRoutes))]
    public async Task QueryPreamble_AllReadOperations_GenerateCorrelationAndPreventCaching(string method, string url)
    {
        WebApplication app = await StartAppAsync("tenant-a", "principal-a", setup: null, allowList: true).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = new(new HttpMethod(method), url);
            if (method == "POST")
            {
                request.Content = JsonContent.Create(new { });
            }

            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            // The proposal has no required fields and reaches its normal body-validation response.
            response.StatusCode.ShouldBe(method == "POST" ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
            Ulid.IsValid(AssertQueryResponseHeaders(response)).ShouldBeTrue();
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [MemberData(nameof(ReadRequestRoutes))]
    public async Task QueryPreamble_QueryIdempotencyKeyIsRejectedOnlyAfterAuthorization(string method, string url)
    {
        ArgumentNullException.ThrowIfNull(url);
        foreach (bool authorized in new[] { true, false })
        {
            WebApplication app = await StartAppAsync("tenant-a", "principal-a", setup: null, seedTenantAccess: authorized, allowList: true).ConfigureAwait(true);
            try
            {
                using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
                foreach (string query in new[] { "Idempotency-Key=value", "idempotency-key=", "IDEMPOTENCY-KEY", "Idempotency%2DKey=one&Idempotency-Key=two" })
                {
                    foreach (string? taskId in new string?[] { null, " read-query-task ", "bad:task" })
                    {
                        using HttpRequestMessage request = new(new HttpMethod(method), url + (url.Contains('?') ? "&" : "?") + query);
                        request.Headers.Add("X-Correlation-Id", "read-query-correlation");
                        if (taskId is not null)
                        {
                            request.Headers.TryAddWithoutValidation("X-Hexalith-Task-Id", taskId).ShouldBeTrue();
                        }

                        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
                        response.StatusCode.ShouldBe(authorized ? HttpStatusCode.BadRequest : HttpStatusCode.NotFound);
                        AssertQueryResponseHeaders(response).ShouldBe("read-query-correlation");
                        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
                        body.RootElement.GetProperty("correlationId").GetString().ShouldBe("read-query-correlation");
                        bool disclosesTask = taskId == " read-query-task " && url.Contains("/resolution/", StringComparison.Ordinal);
                        body.RootElement.TryGetProperty("taskId", out JsonElement task).ShouldBe(disclosesTask);
                        if (disclosesTask)
                        {
                            task.GetString().ShouldBe("read-query-task");
                        }

                        if (authorized)
                        {
                            body.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("idempotency_key");
                        }
                        else
                        {
                            body.RootElement.GetProperty("code").GetString().ShouldBe("resource_unavailable");
                            body.RootElement.GetProperty("details").TryGetProperty("rejectedField", out _).ShouldBeFalse();
                        }
                    }
                }
            }
            finally
            {
                await StopAsync(app).ConfigureAwait(true);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ReadRequestRoutes))]
    public async Task QueryPreamble_StrictFreshnessValidationRemainsShared(string method, string url)
    {
        WebApplication app = await StartAppAsync("tenant-a", "principal-a", setup: null, allowList: true).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            foreach (string freshness in new[] { "strong", "Eventually_Consistent", "eventually_consistent, strong" })
            {
                using HttpRequestMessage request = new(new HttpMethod(method), url);
                request.Headers.Add("X-Hexalith-Freshness", freshness);
                using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
                response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                string correlationId = AssertQueryResponseHeaders(response);
                using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
                body.RootElement.GetProperty("correlationId").GetString().ShouldBe(correlationId);
                body.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("freshness");
            }
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(503)]
    public async Task QueryPreamble_CorrelationPolicyAppliesToSuccessAndEveryProblemStatus(int status)
    {
        WebApplication app = await StartAppAsync("tenant-a", "principal-a", setup: null,
            seedTenantAccess: status != 404,
            tenantAccessStoreOverride: status == 503 ? new ThrowingTenantAccessProjectionStore() : null).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            HashSet<string> generated = new(StringComparer.Ordinal);
            foreach ((string? presented, string? expected) in new (string?, string?)[]
            {
                (null, null), ("", null), ("   ", null), ("bad:value", null),
                (new string('x', 129), null), (new string('x', 128), new string('x', 128)),
                (" caller_corr-1.2 ", "caller_corr-1.2"), ("caller_corr-1.2", "caller_corr-1.2"),
            })
            {
                using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/projects/{ProjectIdValue}/setup/conversation-start");
                if (presented is not null)
                {
                    request.Headers.TryAddWithoutValidation("X-Correlation-Id", presented).ShouldBeTrue();
                }

                if (status == 400)
                {
                    request.Headers.Add("X-Hexalith-Freshness", "strong");
                }

                using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
                response.StatusCode.ShouldBe((HttpStatusCode)status);
                string correlationId = AssertQueryResponseHeaders(response);
                if (expected is not null)
                {
                    correlationId.ShouldBe(expected);
                }
                else
                {
                    Ulid.IsValid(correlationId).ShouldBeTrue();
                    generated.Add(correlationId).ShouldBeTrue();
                }

                if (status != 200)
                {
                    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
                    body.RootElement.GetProperty("correlationId").GetString().ShouldBe(correlationId);
                }
            }
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [InlineData("Accept-Encoding")]
    [InlineData("authorization, Cookie, Accept-Encoding")]
    [InlineData("*")]
    public async Task QueryPreamble_PreservesExistingVaryEntries(string vary)
    {
        WebApplication app = await StartAppAsync("tenant-a", "principal-a", setup: null, existingVary: vary).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = StartSetupRequest();
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            AssertQueryResponseHeaders(response).ShouldBe(CorrelationIdValue);
            if (vary == "*")
            {
                response.Headers.Vary.ShouldBe(new[] { "*" });
            }
            else
            {
                response.Headers.Vary.ShouldContain("Accept-Encoding");
                response.Headers.Vary.Count(value => string.Equals(value, "Authorization", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
                response.Headers.Vary.Count(value => string.Equals(value, "Cookie", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
            }
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    private static string AssertQueryResponseHeaders(HttpResponseMessage response)
    {
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        if (!response.Headers.Vary.Contains("*"))
        {
            response.Headers.Vary.ShouldContain(value => string.Equals(value, "Authorization", StringComparison.OrdinalIgnoreCase));
            response.Headers.Vary.ShouldContain(value => string.Equals(value, "Cookie", StringComparison.OrdinalIgnoreCase));
        }
        return response.Headers.GetValues("X-Correlation-Id").Single();
    }
}
