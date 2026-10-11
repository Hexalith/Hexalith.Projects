using System.Text.Json;

using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Hexalith.Projects.Server.Queries;

using Shouldly;

using Xunit;

namespace Hexalith.Projects.Server.Tests.Authorization;

/// <summary>Checks the current project source read with exact requester binding.</summary>
public sealed class ProjectChatBotAuthorityQueryHandlerTests
{
    [Fact]
    public async Task CurrentExactGrantAllowsAndRevocationOrRequesterMismatchDenies()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var identity = new AggregateIdentity("tenant-alpha", "projects", "project-a");
        StreamReadEvent[] granted =
        [
            Event(1, new ProjectCreated("tenant-alpha", "project-a", "Project", null, null,
                ProjectLifecycle.Active, "issuer", "corr-1", "task-1", "message-1", "fingerprint-1", now), now),
            Event(2, new ProjectChatBotAuthorityGranted("tenant-alpha", "project-a", "actor-alpha",
                "RecordGovernedNote", "human", "Ui", "grant-a", now.AddHours(1), "issuer",
                "corr-2", "task-2", "message-2", "fingerprint-2", now), now),
        ];
        var reader = new ProjectAuthorityReaderFixture(new(new(identity, 2, now, granted, "observation-1"), null));
        var handler = new ProjectChatBotAuthorityQueryHandler(reader, TimeProvider.System);
        QueryEnvelope exact = Envelope("actor-alpha");

        QueryResult allowed = await handler.ExecuteAsync(exact, CancellationToken.None);
        allowed.Success.ShouldBeTrue();
        JsonSerializer.Deserialize<ProjectChatBotAuthorityResult>(allowed.PayloadBytes!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Allowed.ShouldBeTrue();
        (await handler.ExecuteAsync(Envelope("actor-beta") with { Payload = exact.Payload }, CancellationToken.None))
            .Success.ShouldBeFalse();

        StreamReadEvent[] revoked = [.. granted, Event(3, new ProjectChatBotAuthorityRevoked(
            "tenant-alpha", "project-a", "actor-alpha", "RecordGovernedNote", "human", "Ui",
            "grant-a", "issuer", "corr-3", "task-3", "message-3", "fingerprint-3", now), now)];
        var revokedHandler = new ProjectChatBotAuthorityQueryHandler(new ProjectAuthorityReaderFixture(
            new(new(identity, 3, now, revoked, "observation-2"), null)), TimeProvider.System);
        QueryResult denied = await revokedHandler.ExecuteAsync(exact, CancellationToken.None);
        denied.Success.ShouldBeTrue();
        JsonSerializer.Deserialize<ProjectChatBotAuthorityResult>(denied.PayloadBytes!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task LiveCreateWithPendingFolderAndConversationGrantReplaysAsAllowed()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var identity = new AggregateIdentity("tenant-alpha", "projects", "project-a");
        StreamReadEvent[] events =
        [
            Event(1, new ProjectCreated("tenant-alpha", "project-a", "Project", null, null,
                ProjectLifecycle.Active, "actor-alpha", "corr-create", "task-create", "message-create",
                "fingerprint-create", now), now),
            Event(2, new ProjectFolderCreationPending("tenant-alpha", "project-a", "Project",
                "folder_create_external_unavailable", true, "actor-alpha", "corr-create", "task-create",
                "message-pending", "fingerprint-pending", now), now),
            Event(3, new ProjectChatBotAuthorityGranted("tenant-alpha", "project-a", "actor-alpha",
                "project-conversation", "human", "Ui", "grant-conversation", now.AddHours(1),
                "actor-alpha", "corr-grant", "task-grant", "message-grant", "fingerprint-grant", now), now),
        ];
        var reader = new ProjectAuthorityReaderFixture(new(new(identity, 3, now, events, "observation-live"), null));
        var handler = new ProjectChatBotAuthorityQueryHandler(reader, TimeProvider.System);
        var request = new ResolveProjectChatBotAuthority("tenant-alpha", "actor-alpha", "project-a",
            "project-conversation", "human", "Ui");
        QueryEnvelope query = Envelope("actor-alpha") with
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        };

        QueryResult result = await handler.ExecuteAsync(query, CancellationToken.None);
        result.Success.ShouldBeTrue();
        ProjectChatBotAuthorityResult evidence = JsonSerializer.Deserialize<ProjectChatBotAuthorityResult>(
            result.PayloadBytes!, new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldNotBeNull();
        evidence.Allowed.ShouldBeTrue();
        evidence.GrantId.ShouldBe("grant-conversation");
        evidence.SourceVersion.ShouldBe(3);
    }

    private static QueryEnvelope Envelope(string requester)
    {
        var payload = new ResolveProjectChatBotAuthority("tenant-alpha", "actor-alpha", "project-a",
            "RecordGovernedNote", "human", "Ui");
        return new QueryEnvelope("tenant-alpha", "projects", "project-a", ResolveProjectChatBotAuthority.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            "corr-query", requester)
        {
            AuthenticatedWorkloadId = "chatbot", IsDelegated = true, OriginalActorId = requester,
        };
    }

    private static StreamReadEvent Event(long sequence, object payload, DateTimeOffset now)
        => new(sequence, payload.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)), "json", 1, $"message-{sequence}", null,
            null, now, "issuer");
}
