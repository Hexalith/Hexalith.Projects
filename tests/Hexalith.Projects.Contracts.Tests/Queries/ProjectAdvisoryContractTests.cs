namespace Hexalith.Projects.Contracts.Tests.Queries;

using System.Text.Json;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Shouldly;
using Xunit;

/// <summary>Literal metadata-only wire contracts for all five advisory DTOs.</summary>
public sealed class ProjectAdvisoryContractTests
{
    [Theory]
    [InlineData("folder")]
    [InlineData("file")]
    public void AttachmentAndAttachmentQueryUseLiteralFieldsAndOptionalArchiveFlag(string kind)
    {
        var attachment = new ProjectAdvisoryAttachment(kind, "opaque-id");
        var item = JsonSerializer.SerializeToElement(attachment, JsonSerializerOptions.Web);
        Properties(item).ShouldBe(["referenceId", "referenceKind"]);
        item.GetProperty("referenceKind").GetString().ShouldBe(kind);
        item.GetProperty("referenceId").GetString().ShouldBe("opaque-id");
        var query = JsonSerializer.SerializeToElement(new ResolveProjectFromAttachmentsQuery([attachment]), JsonSerializerOptions.Web);
        Properties(query).ShouldBe(["attachments", "includeArchived"]);
        query.GetProperty("includeArchived").GetBoolean().ShouldBeFalse();
        var minimal = JsonSerializer.Deserialize<ResolveProjectFromAttachmentsQuery>(
            "{\"attachments\":[{\"referenceKind\":\"folder\",\"referenceId\":\"folder-id\"}]}", JsonSerializerOptions.Web)!;
        minimal.IncludeArchived.ShouldBeFalse(); minimal.Attachments.Single().ShouldBe(new("folder", "folder-id"));
        JsonSerializer.Deserialize<ResolveProjectFromAttachmentsQuery>(
            "{\"attachments\":[],\"includeArchived\":true}", JsonSerializerOptions.Web)!.IncludeArchived.ShouldBeTrue();
    }

    [Fact]
    public void ConversationQueryUsesLiteralFieldsAndOptionalArchiveFlag()
    {
        var query = JsonSerializer.SerializeToElement(new ResolveProjectFromConversationQuery("conversation-id"), JsonSerializerOptions.Web);
        Properties(query).ShouldBe(["conversationId", "includeArchived"]);
        query.GetProperty("conversationId").GetString().ShouldBe("conversation-id");
        query.GetProperty("includeArchived").GetBoolean().ShouldBeFalse();
        JsonSerializer.Deserialize<ResolveProjectFromConversationQuery>(
            "{\"conversationId\":\"conversation-id\"}", JsonSerializerOptions.Web)!.IncludeArchived.ShouldBeFalse();
        JsonSerializer.Deserialize<ResolveProjectFromConversationQuery>(
            "{\"conversationId\":\"conversation-id\",\"includeArchived\":true}", JsonSerializerOptions.Web)!.IncludeArchived.ShouldBeTrue();
    }

    [Fact]
    public void CandidateUsesLiteralFieldsAndNameBasedReasons()
    {
        var candidate = new ProjectAdvisoryCandidate("project-id", [ProjectReasonCode.ConversationLinked,
            ProjectReasonCode.ProjectFolderMatched, ProjectReasonCode.FileReferenceMatched, ProjectReasonCode.MemoryMatched, ProjectReasonCode.MetadataMatched], 1, 50);
        var item = JsonSerializer.SerializeToElement(candidate, JsonSerializerOptions.Web);
        Properties(item).ShouldBe(["projectId", "rank", "reasonCodes", "score"]);
        item.GetProperty("projectId").GetString().ShouldBe("project-id");
        item.GetProperty("rank").GetInt32().ShouldBe(1); item.GetProperty("score").GetInt32().ShouldBe(50);
        item.GetProperty("reasonCodes").EnumerateArray().Select(x => x.GetString()).ShouldBe(
            ["ConversationLinked", "ProjectFolderMatched", "FileReferenceMatched", "MemoryMatched", "MetadataMatched"]);
    }

    [Theory]
    [InlineData(ResolutionResult.NoMatch, "NoMatch")]
    [InlineData(ResolutionResult.SingleCandidate, "SingleCandidate")]
    [InlineData(ResolutionResult.MultipleCandidates, "MultipleCandidates")]
    public void ResponseHasOnlyLiteralSafeFieldsAndNameBasedOutcome(ResolutionResult outcome, string name)
    {
        var response = new ProjectAdvisoryResolutionResponse(outcome, [new("project-id", [ProjectReasonCode.ProjectFolderMatched], 1, 45)],
            DateTimeOffset.UnixEpoch, false, []);
        var json = JsonSerializer.SerializeToElement(response, JsonSerializerOptions.Web);
        Properties(json).ShouldBe(["candidates", "evaluatedAt", "incomplete", "result", "unsupportedInputKinds"]);
        Properties(json.GetProperty("candidates")[0]).ShouldBe(["projectId", "rank", "reasonCodes", "score"]);
        json.GetProperty("result").ValueKind.ShouldBe(JsonValueKind.String);
        json.GetProperty("result").GetString().ShouldBe(name);
        json.GetProperty("evaluatedAt").GetDateTimeOffset().ShouldBe(DateTimeOffset.UnixEpoch);
        foreach (string protectedField in new[] { "displayName", "name", "safeLabel", "excluded", "selected", "snapshot", "capability", "tenantId", "conversationId", "folderId" })
        {
            json.TryGetProperty(protectedField, out _).ShouldBeFalse();
            json.GetProperty("candidates")[0].TryGetProperty(protectedField, out _).ShouldBeFalse();
        }
    }

    [Fact]
    public void EmptyIncompleteResponseRetainsLiteralNullResultAndUnsupportedKinds()
    {
        var response = JsonSerializer.Deserialize<ProjectAdvisoryResolutionResponse>(
            "{\"result\":null,\"candidates\":[],\"evaluatedAt\":\"2026-10-22T10:00:00Z\",\"incomplete\":true,\"unsupportedInputKinds\":[\"file\"]}", JsonSerializerOptions.Web)!;
        response.Result.ShouldBeNull(); response.Candidates.ShouldBeEmpty(); response.Incomplete.ShouldBeTrue();
        response.UnsupportedInputKinds.ShouldBe(["file"]);
        var json = JsonSerializer.SerializeToElement(response, JsonSerializerOptions.Web);
        json.GetProperty("result").ValueKind.ShouldBe(JsonValueKind.Null);
        json.GetProperty("unsupportedInputKinds")[0].GetString().ShouldBe("file");
        Properties(json).ShouldBe(["candidates", "evaluatedAt", "incomplete", "result", "unsupportedInputKinds"]);
    }

    private static string[] Properties(JsonElement item)
        => item.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray();
}
