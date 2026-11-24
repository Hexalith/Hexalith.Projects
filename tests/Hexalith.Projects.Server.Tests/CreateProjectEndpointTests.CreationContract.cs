// <copyright file="CreateProjectEndpointTests.CreationContract.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Projects.Authorization;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Server;
using Hexalith.Projects.Testing.Leakage;

using Microsoft.AspNetCore.Builder;

using Shouldly;

using Xunit;

// Package 7.1-P1 creation-contract qualification. Every case comes from the checked-in fixture
// Fixtures/CreationContract/creation-contract-fixtures.v1.json and runs through HTTP, the production
// EventStore submission mapping and the single-writer ProjectsDomainProcessor.
public sealed partial class CreateProjectEndpointTests
{
    private const string CreationContractPackage = "7.1-P1";

    private static readonly string[] AcceptedCommandOnlyFields = ["acceptedAt", "idempotentReplay"];

    public static TheoryData<string> CreationContractCaseIds()
    {
        TheoryData<string> data = new();
        foreach (JsonElement fixtureCase in CreationContractFixture().GetProperty("cases").EnumerateArray())
        {
            data.Add(fixtureCase.GetProperty("id").GetString()!);
        }

        return data;
    }

    [Theory]
    [Trait("Package", CreationContractPackage)]
    [MemberData(nameof(CreationContractCaseIds))]
    public async Task CreationContract_FixtureCase_MatchesPinnedHttpCommandAndEventOutcome(string caseId)
    {
        ArgumentNullException.ThrowIfNull(caseId);
        JsonElement fixture = CreationContractFixture();
        JsonElement fixtureCase = FindCreationContractCase(fixture, caseId);
        JsonElement expect = fixtureCase.GetProperty("expect");
        int expectedStatus = expect.GetProperty("status").GetInt32();

        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a").ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = RawCreateRequest(
                fixtureCase.GetProperty("body").GetString()!,
                fixtureCase.GetProperty("idempotencyKey").GetString()!);
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            ((int)response.StatusCode).ShouldBe(expectedStatus, caseId);
            if (expectedStatus == (int)HttpStatusCode.Accepted)
            {
                CreateProject command = submitter.Submitted.ShouldHaveSingleItem();
                await AssertAcceptedCreationContractCaseAsync(fixtureCase, body, command).ConfigureAwait(true);
            }
            else
            {
                // Invalid authorized input: no command reaches the submitter, so no event can be written.
                submitter.Submitted.ShouldBeEmpty();
                AssertMetadataOnlyCreationRejection(fixture, body, expect.GetProperty("rejectedField").GetString()!);
            }
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Theory]
    [Trait("Package", CreationContractPackage)]
    [InlineData("missing-tenant")]
    [InlineData("system-tenant")]
    [InlineData("unknown-tenant")]
    [InlineData("disabled-tenant")]
    [InlineData("non-member")]
    [InlineData("stale-projection")]
    public async Task CreationContract_UnauthorizedCaller_ReturnsSafeDenialBeforeReadingAnyBodyByte(string denialMode)
    {
        ArgumentNullException.ThrowIfNull(denialMode);
        JsonElement fixture = CreationContractFixture();
        (string Kind, string Body, int AuthorizedStatus, string? AuthorizedRejectedField)[] controls =
        [
            ("valid", FindCreationContractCase(fixture, "canonical-tier-2").GetProperty("body").GetString()!, (int)HttpStatusCode.Accepted, null),
            ("malformed", FindCreationContractCase(fixture, "malformed-json").GetProperty("body").GetString()!, (int)HttpStatusCode.BadRequest, "body"),
            ("invalid", FindCreationContractCase(fixture, "class-unknown").GetProperty("body").GetString()!, (int)HttpStatusCode.BadRequest, "projectMetadata.metadataClass"),
        ];

        RequestBodyReadCounter deniedCounter = new();
        FakeProjectCommandSubmitter deniedSubmitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication denied = await StartDeniedCreationContractAppAsync(denialMode, deniedSubmitter, deniedCounter).ConfigureAwait(true);
        List<string> denialBodies = [];
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(denied.Urls.First()) };
            foreach ((string kind, string requestBody, _, _) in controls)
            {
                deniedCounter.Reset();
                using HttpRequestMessage request = RawCreateRequest(requestBody, "p1-denied-" + kind);
                using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
                string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

                response.StatusCode.ShouldBe(HttpStatusCode.NotFound, kind);
                deniedCounter.BytesRead.ShouldBe(0L, kind);
                using JsonDocument document = JsonDocument.Parse(body);
                document.RootElement.GetProperty("category").GetString().ShouldBe("tenant_access_denied");
                document.RootElement.GetProperty("code").GetString().ShouldBe("resource_unavailable");
                JsonElement details = document.RootElement.GetProperty("details");
                details.GetProperty("visibility").GetString().ShouldBe("redacted");
                details.TryGetProperty("rejectedField", out _).ShouldBeFalse(kind);
                AssertNoCreationContractEcho(fixture, body);
                denialBodies.Add(body);
            }

            deniedSubmitter.Submitted.ShouldBeEmpty();

            // Valid, malformed and invalid bodies produce byte-identical denials: no validation feedback.
            denialBodies.Distinct(StringComparer.Ordinal).Count().ShouldBe(1);
        }
        finally
        {
            await StopAsync(denied).ConfigureAwait(true);
        }

        // Paired control: the same bodies from an authorized caller are read and validated.
        RequestBodyReadCounter authorizedCounter = new();
        FakeProjectCommandSubmitter authorizedSubmitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication authorized = await StartAppAsync(
            authorizedSubmitter,
            tenantId: "tenant-a",
            principalId: "principal-a",
            configurePipeline: authorizedCounter.Install).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(authorized.Urls.First()) };
            foreach ((string kind, string requestBody, int authorizedStatus, string? authorizedRejectedField) in controls)
            {
                authorizedCounter.Reset();
                using HttpRequestMessage request = RawCreateRequest(requestBody, "p1-authorized-" + kind);
                using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
                string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

                ((int)response.StatusCode).ShouldBe(authorizedStatus, kind);
                authorizedCounter.BytesRead.ShouldBeGreaterThan(0L, kind);
                if (authorizedRejectedField is not null)
                {
                    AssertMetadataOnlyCreationRejection(fixture, body, authorizedRejectedField);
                }
            }

            authorizedSubmitter.Submitted.Count.ShouldBe(1);
        }
        finally
        {
            await StopAsync(authorized).ConfigureAwait(true);
        }
    }

    [Fact]
    [Trait("Package", CreationContractPackage)]
    public async Task CreationContract_MissingIdempotencyKey_IsRejectedFirstWithoutReadingAnyBodyByte()
    {
        RequestBodyReadCounter counter = new();
        FakeProjectCommandSubmitter submitter = new(ProjectCommandSubmissionResult.Accepted("corr-a", idempotentReplay: false));
        WebApplication app = await StartAppAsync(submitter, tenantId: null, principalId: null, configurePipeline: counter.Install).ConfigureAwait(true);
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(app.Urls.First()) };
            using HttpRequestMessage request = RawCreateRequest("{ not valid json", "p1-header-first");
            request.Headers.Remove("Idempotency-Key");
            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            counter.BytesRead.ShouldBe(0L);
            submitter.Submitted.ShouldBeEmpty();
            using JsonDocument document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("details").GetProperty("rejectedField").GetString().ShouldBe("idempotency_key");
        }
        finally
        {
            await StopAsync(app).ConfigureAwait(true);
        }
    }

    [Fact]
    [Trait("Package", CreationContractPackage)]
    public void CreationContract_OpenApiClassificationEnum_MatchesValidatorDecisionTable()
    {
        JsonElement fixture = CreationContractFixture();
        JsonElement table = fixture.GetProperty("metadataClassDecisionTable");
        string[] accepted = table.GetProperty("accepted").EnumerateArray()
            .Select(static row => row.GetProperty("value").GetString()!)
            .ToArray();

        ReadOpenApiEnum("SensitiveMetadataTier").ShouldBe(accepted);
        foreach (JsonElement row in table.GetProperty("accepted").EnumerateArray())
        {
            string value = row.GetProperty("value").GetString()!;
            SensitiveMetadataTierValidator.IsValid(value).ShouldBeTrue(value);
            JsonElement expect = FindCreationContractCase(fixture, row.GetProperty("createCase").GetString()!).GetProperty("expect");
            expect.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.Accepted, value);
            expect.GetProperty("metadataClass").GetString().ShouldBe(value);
        }

        foreach (JsonElement row in table.GetProperty("rejected").EnumerateArray())
        {
            string id = row.GetProperty("id").GetString()!;
            SensitiveMetadataTierValidator.IsValid(DecisionTableValue(row)).ShouldBeFalse(id);
            JsonElement expect = FindCreationContractCase(fixture, row.GetProperty("createCase").GetString()!).GetProperty("expect");
            expect.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.BadRequest, id);
            expect.GetProperty("rejectedField").GetString().ShouldBe("projectMetadata.metadataClass", id);
        }
    }

    private static async Task AssertAcceptedCreationContractCaseAsync(JsonElement fixtureCase, string responseBody, CreateProject command)
    {
        JsonElement expect = fixtureCase.GetProperty("expect");
        JsonElement expectedCommand = expect.GetProperty("command");
        string idempotencyKey = fixtureCase.GetProperty("idempotencyKey").GetString()!;

        using (JsonDocument accepted = JsonDocument.Parse(responseBody))
        {
            accepted.RootElement.GetProperty("status").GetString().ShouldBe("accepted");
            accepted.RootElement.GetProperty("idempotentReplay").GetBoolean().ShouldBeFalse();
        }

        // HTTP request -> CreateProject command.
        command.TenantId.ShouldBe("tenant-a");
        command.ActorPrincipalId.ShouldBe("principal-a");
        command.CorrelationId.ShouldBe("corr-a");
        command.TaskId.ShouldBe("task-a");
        command.IdempotencyKey.ShouldBe(idempotencyKey);
        command.Name.ShouldBe(expectedCommand.GetProperty("name").GetString());
        command.Description.ShouldBe(OptionalFixtureString(expectedCommand, "description"));
        command.SetupMetadata.ShouldBe(OptionalFixtureString(expectedCommand, "setupMetadata"));
        string? expectedProjectId = OptionalFixtureString(expectedCommand, "projectId");
        if (expectedProjectId is null)
        {
            Regex.IsMatch(command.ProjectId.Value, "^[0-9a-f]{32}$", RegexOptions.CultureInvariant).ShouldBeTrue(command.ProjectId.Value);
        }
        else
        {
            command.ProjectId.Value.ShouldBe(expectedProjectId);
        }

        // CreateProject -> production EventStore submission mapping.
        CapturingEventStoreGatewayClient gateway = new();
        ProjectCommandSubmissionResult mapped = await new EventStoreProjectCommandSubmitter(gateway)
            .SubmitCreateProjectAsync(command, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        mapped.Outcome.ShouldBe(ProjectCommandSubmissionOutcome.Accepted);
        SubmitCommandRequest submitted = gateway.Requests.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe(idempotencyKey);
        submitted.Tenant.ShouldBe("tenant-a");
        submitted.Domain.ShouldBe(ProjectsServerModule.DomainName);
        submitted.AggregateId.ShouldBe(command.ProjectId.Value);
        submitted.CommandType.ShouldBe(ProjectsServerModule.CreateProjectCommandType);
        submitted.CorrelationId.ShouldBe("corr-a");
        submitted.Extensions.ShouldNotBeNull()["taskId"].ShouldBe("task-a");
        submitted.Payload.EnumerateObject().Select(static property => property.Name)
            .ShouldBeSubsetOf(["name", "description", "setupMetadata"]);
        string payload = submitted.Payload.GetRawText();

        // Submitted command -> single-writer processor events.
        CommandEnvelope envelope = new(
            MessageId: submitted.MessageId,
            TenantId: submitted.Tenant,
            Domain: submitted.Domain,
            AggregateId: submitted.AggregateId,
            CommandType: submitted.CommandType,
            Payload: Encoding.UTF8.GetBytes(payload),
            CorrelationId: submitted.CorrelationId!,
            CausationId: null,
            UserId: command.ActorPrincipalId,
            Extensions: submitted.Extensions);
        DomainResult result = await new ProjectsDomainProcessor(
                new CreationContractTimeProvider(),
                new AllowingProjectEventStoreAuthorizationValidator())
            .ProcessAsync(envelope, currentState: null)
            .ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Select(static item => item.GetType().Name)
            .ShouldBe(expect.GetProperty("events").EnumerateArray().Select(static item => item.GetString()!));

        JsonElement expectedCreated = expect.GetProperty("projectCreated");
        ProjectCreated created = result.Events[0].ShouldBeOfType<ProjectCreated>();
        created.TenantId.ShouldBe("tenant-a");
        created.ProjectId.ShouldBe(command.ProjectId.Value);
        created.Name.ShouldBe(expectedCreated.GetProperty("name").GetString());
        created.Description.ShouldBe(command.Description);
        created.SetupMetadata.ShouldBe(command.SetupMetadata);
        created.Lifecycle.ToString().ShouldBe(expectedCreated.GetProperty("lifecycle").GetString());
        created.IdempotencyKey.ShouldBe(idempotencyKey);
        created.IdempotencyFingerprint.ShouldBe(expectedCreated.GetProperty("idempotencyFingerprint").GetString());

        JsonElement expectedPending = expect.GetProperty("projectFolderCreationPending");
        ProjectFolderCreationPending pending = result.Events[1].ShouldBeOfType<ProjectFolderCreationPending>();
        pending.TenantId.ShouldBe("tenant-a");
        pending.ProjectId.ShouldBe(command.ProjectId.Value);
        pending.DisplayNameIntent.ShouldBe(expectedPending.GetProperty("displayNameIntent").GetString());
        pending.IdempotencyKey.ShouldBe(idempotencyKey + expectedPending.GetProperty("idempotencyKeySuffix").GetString());
        pending.IdempotencyFingerprint.ShouldBe(expectedPending.GetProperty("idempotencyFingerprint").GetString());

        // A canonical classification is validated, never persisted: neither the submitted payload nor
        // any event carries it.
        if (expect.TryGetProperty("metadataClass", out JsonElement metadataClass))
        {
            string tier = metadataClass.GetString()!;
            payload.ShouldNotContain("metadataClass", Case.Insensitive);
            payload.ShouldNotContain(tier, Case.Sensitive);
            foreach (object item in result.Events)
            {
                string eventJson = JsonSerializer.Serialize(item, item.GetType());
                eventJson.ShouldNotContain("metadataClass", Case.Insensitive);
                eventJson.ShouldNotContain(tier, Case.Sensitive);
            }
        }
    }

    private static void AssertMetadataOnlyCreationRejection(JsonElement fixture, string body, string expectedRejectedField)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;
        root.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.BadRequest);
        root.GetProperty("category").GetString().ShouldBe("validation_error");
        root.GetProperty("code").GetString().ShouldBe("validation_error");
        JsonElement details = root.GetProperty("details");
        details.GetProperty("visibility").GetString().ShouldBe("metadata_only");
        details.GetProperty("rejectedField").GetString().ShouldBe(expectedRejectedField);
        details.EnumerateObject().Count().ShouldBe(2);
        foreach (string field in AcceptedCommandOnlyFields)
        {
            root.TryGetProperty(field, out _).ShouldBeFalse(field);
        }

        AssertNoCreationContractEcho(fixture, body);
        Should.NotThrow(() => NoPayloadLeakageAssertions.AssertNoLeakageInText(body));
    }

    private static void AssertNoCreationContractEcho(JsonElement fixture, string body)
    {
        foreach (JsonElement probe in fixture.GetProperty("echoProbes").EnumerateArray())
        {
            body.ShouldNotContain(probe.GetString()!, Case.Sensitive);
        }
    }

    private static Task<WebApplication> StartDeniedCreationContractAppAsync(
        string denialMode,
        FakeProjectCommandSubmitter submitter,
        RequestBodyReadCounter counter)
        => denialMode switch
        {
            "missing-tenant" => StartAppAsync(submitter, tenantId: null, principalId: null, configurePipeline: counter.Install),
            "system-tenant" => StartAppAsync(submitter, tenantId: "system", principalId: "principal-a", configurePipeline: counter.Install),
            "unknown-tenant" => StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a", seedTenantAccess: false, configurePipeline: counter.Install),
            "disabled-tenant" => StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a", tenantEnabled: false, configurePipeline: counter.Install),
            "non-member" => StartAppAsync(submitter, tenantId: "tenant-a", principalId: "principal-a", membershipPrincipalId: "principal-b", configurePipeline: counter.Install),
            "stale-projection" => StartAppAsync(
                submitter,
                tenantId: "tenant-a",
                principalId: "principal-a",
                lastTenantAccessTimestamp: DateTimeOffset.UtcNow.AddHours(-1),
                configurePipeline: counter.Install),
            _ => throw new ArgumentOutOfRangeException(nameof(denialMode), denialMode, "Unknown denial mode."),
        };

    private static JsonElement CreationContractFixture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CreationContract", "creation-contract-fixtures.v1.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }

    private static JsonElement FindCreationContractCase(JsonElement fixture, string caseId)
        => fixture.GetProperty("cases").EnumerateArray()
            .Single(fixtureCase => string.Equals(fixtureCase.GetProperty("id").GetString(), caseId, StringComparison.Ordinal));

    private static string? OptionalFixtureString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? DecisionTableValue(JsonElement row)
    {
        JsonElement json = row.GetProperty("metadataClassJson");
        if (json.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        using JsonDocument literal = JsonDocument.Parse(json.GetString()!);
        return literal.RootElement.ValueKind == JsonValueKind.String ? literal.RootElement.GetString() : null;
    }

    private static string[] ReadOpenApiEnum(string schemaName)
    {
        string spine = Path.Combine(CreationContractRepositoryRoot(), "src", "Hexalith.Projects.Contracts", "openapi", "hexalith.projects.v1.yaml");
        string[] lines = File.ReadAllLines(spine);
        int start = Array.FindIndex(lines, line => string.Equals(line.TrimEnd(), "    " + schemaName + ":", StringComparison.Ordinal));
        start.ShouldBeGreaterThanOrEqualTo(0, schemaName);

        List<string> values = [];
        bool inEnum = false;
        for (int index = start + 1; index < lines.Length; index++)
        {
            string line = lines[index];
            if (line.Trim().Length > 0 && !line.StartsWith("      ", StringComparison.Ordinal))
            {
                break;
            }

            string trimmed = line.Trim();
            if (string.Equals(trimmed, "enum:", StringComparison.Ordinal))
            {
                inEnum = true;
            }
            else if (inEnum && trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                values.Add(trimmed[2..].Trim().Trim('"', '\''));
            }
            else if (trimmed.Length > 0)
            {
                inEnum = false;
            }
        }

        return [.. values];
    }

    private static string CreationContractRepositoryRoot([System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
    {
        foreach (string candidate in new[] { AppContext.BaseDirectory, Path.GetDirectoryName(sourcePath)!, Directory.GetCurrentDirectory() })
        {
            for (DirectoryInfo? directory = new(candidate); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Projects.slnx")))
                {
                    return directory.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Projects repository root was not found for the creation-contract fixture.");
    }

    private sealed class CreationContractTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;
    }

    private sealed class RequestBodyReadCounter
    {
        private long _bytesRead;

        public long BytesRead => Interlocked.Read(ref _bytesRead);

        public void Reset() => Interlocked.Exchange(ref _bytesRead, 0);

        public void Add(int count) => Interlocked.Add(ref _bytesRead, count);

        public void Install(WebApplication app)
            => app.Use(async (context, next) =>
            {
                context.Request.Body = new CountingRequestStream(context.Request.Body, this);
                await next(context).ConfigureAwait(false);
            });
    }

    private sealed class CountingRequestStream(Stream inner, RequestBodyReadCounter counter) : Stream
    {
        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
            => Count(inner.Read(buffer, offset, count));

        public override int Read(Span<byte> buffer)
            => Count(inner.Read(buffer));

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Count(await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => Count(await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false));

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int Count(int read)
        {
            counter.Add(read);
            return read;
        }
    }

    private sealed class CapturingEventStoreGatewayClient : IEventStoreGatewayClient
    {
        public List<SubmitCommandRequest> Requests { get; } = [];

        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(new SubmitCommandResponse(request.CorrelationId ?? request.MessageId));
        }

        public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
