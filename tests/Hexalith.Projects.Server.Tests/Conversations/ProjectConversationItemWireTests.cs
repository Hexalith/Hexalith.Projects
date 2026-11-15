namespace Hexalith.Projects.Server.Tests.Conversations;

using System.Text.Json;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Queries;
using Shouldly;
using Xunit;
using ProjectId = Hexalith.Projects.Contracts.Identifiers.ProjectId;

/// <summary>Proves the CLR-only decoupling keeps the Conversations identifier and legacy page bytes.</summary>
public sealed class ProjectConversationItemWireTests
{
    [Theory]
    [InlineData("conversation-a")]
    [InlineData("01HZ9K8YQ3W6V2N4R7T5P0X1AC")]
    [InlineData("\"conv: \"")]
    public void LegacyPageBytesAndOwnerIdentifierConverterRoundTrip(string value)
    {
        ProjectConversationItem item = new(new ProjectId("project-a"), value, "Open", "Label",
            ProjectConversationTrustSignal.Current, "Project", "resolved");
        ProjectConversationsPage page = new(item.ProjectId, [item], new(1, "cursor-a"), ProjectConversationTrustSignal.Current);
        var legacyItem = new
        {
            item.ProjectId, ConversationId = new ConversationId(value), item.LifecycleStatus,
            item.DisplayLabel, item.TrustSignal, item.ProjectSafeLabel, item.ProjectSafeStatus,
        };
        var legacyPage = new { page.ProjectId, Items = new[] { legacyItem }, page.Page, page.TrustSignal };
        foreach (JsonSerializerOptions options in new[] { JsonSerializerOptions.Default, JsonSerializerOptions.Web })
        {
            JsonElement json = JsonSerializer.SerializeToElement(item, options);
            string key = options.PropertyNamingPolicy?.ConvertName("ConversationId") ?? "ConversationId";
            json.GetProperty(key).GetRawText().ShouldBe(JsonSerializer.Serialize(new ConversationId(value), options));
            string actual = JsonSerializer.Serialize(page, options);
            actual.ShouldBe(JsonSerializer.Serialize(legacyPage, options));
            JsonSerializer.Deserialize<ProjectConversationsPage>(actual, options)!.Items.Single().ConversationId.ShouldBe(value);
        }
    }

    [Theory]
    [InlineData("null")]
    [InlineData("123")]
    [InlineData("\"conversation-a\"")]
    [InlineData("\"conv: \"")]
    public void MalformedWireIdentifiersAreRejected(string wire)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ProjectConversationIdJsonConverter());
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<string>(wire, options));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ClrIdentifierMustBeNonblank(string? value)
        => Should.Throw<ArgumentException>(() => new ProjectConversationItem(new("project-a"), value!, "Open", null,
            ProjectConversationTrustSignal.Current, null, null));
}
