namespace Hexalith.Projects.Contracts.Tests.Queries;

using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hexalith.McpCli.Abstractions;
using Hexalith.Projects.Contracts.Queries;
using Shouldly;
using Xunit;

/// <summary>Guards the exact supported read surface and existing server payload allowlists.</summary>
public sealed class ProjectMcpCliDecorationTests
{
    [Fact]
    public void SupportedReadDecorationsMatchTheirWebRouting()
    {
        Assembly assembly = typeof(ListProjectsQuery).Assembly;
        HexalithModuleAttribute module = assembly.GetCustomAttribute<HexalithModuleAttribute>()!;
        module.Name.ShouldBe("projects");
        module.IdentifierKind.ShouldBe(IdentifierKind.String);
        module.FixedTenant.ShouldBeNull();
        var expected = new Dictionary<Type, (string Name, string WireType, string? AggregateId)>
        {
            [typeof(ListProjectsV2Query)] = ("list", ListProjectsV2Query.QueryType, "projects"),
            [typeof(GetProjectQuery)] = ("describe", GetProjectQuery.QueryType, null),
            [typeof(ResolveProjectFromConversationQuery)] = ("trace-conversation", ResolveProjectFromConversationQuery.QueryType, "projects"),
            [typeof(ResolveProjectFromAttachmentsQuery)] = ("trace-attachments", ResolveProjectFromAttachmentsQuery.QueryType, "projects"),
        };
        Type[] enrolled = assembly.GetTypes().Where(type => type.IsDefined(typeof(HexalithQueryAttribute))).ToArray();
        enrolled.OrderBy(type => type.Name).ShouldBe(expected.Keys.OrderBy(type => type.Name));
        foreach (Type type in enrolled)
        {
            HexalithQueryAttribute query = type.GetCustomAttribute<HexalithQueryAttribute>()!;
            query.Name.ShouldBe(expected[type].Name);
            query.Domain.ShouldBe("projects");
            query.WireType.ShouldBe(expected[type].WireType);
            query.ProjectionType.ShouldBeNull();
            query.ProjectionActorType.ShouldBeNull();
            query.AggregateId.ShouldBe(expected[type].AggregateId);
            query.AggregateIdProperty.ShouldBeNull();
            query.TenantProperty.ShouldBeNull();
            foreach (PropertyInfo property in type.GetProperties())
            {
                property.GetCustomAttribute<DescriptionAttribute>()!.Description.ShouldNotBeNullOrWhiteSpace();
            }
        }
        // Exact enumeration also protects setup, context and all command types from enrollment.
        assembly.GetTypes().Where(type => type.IsDefined(typeof(HexalithCommandAttribute))).ShouldBeEmpty();
        typeof(ResolveProjectFromConversationQuery).GetProperty("ConversationId")!.IsDefined(typeof(HexalithIdentifierAttribute)).ShouldBeTrue();
        typeof(ProjectAdvisoryAttachment).GetProperty("ReferenceId")!.IsDefined(typeof(HexalithIdentifierAttribute)).ShouldBeTrue();
        foreach (PropertyInfo property in typeof(ProjectAdvisoryAttachment).GetProperties())
        {
            property.GetCustomAttribute<DescriptionAttribute>()!.Description.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void PayloadKeysExactlyMatchTheServerAllowlistsWithDefaultAndWebSerialization()
    {
        AssertKeys(new ListProjectsQuery(), ["lifecycleState", "cursor", "pageSize", "inspectDescriptiveMetadata", "includeQuarantined"]);
        AssertKeys(new ListProjectsV2Query(), ["lifecycleState", "inspectDescriptiveMetadata", "includeQuarantined"]);
        AssertKeys(new GetProjectAuditQuery(), ["cursor", "pageSize"]);
        AssertKeys(new GetProjectTaskStatusQuery(), []);
        AssertKeys(new GetProjectQuery(), ["inspectDescriptiveMetadata", "inspectQuarantined"]);
        AssertKeys(new ResolveProjectFromConversationQuery("conversation-a"), ["conversationId", "includeArchived"]);
        AssertKeys(new ResolveProjectFromAttachmentsQuery([new("folder", "folder-a")]), ["attachments", "includeArchived"]);
        AssertKeys(new ProjectAdvisoryAttachment("folder", "folder-a"), ["referenceKind", "referenceId"]);
    }

    private static void AssertKeys<T>(T value, string[] expected)
    {
        JsonElement payload = JsonSerializer.SerializeToElement(value);
        payload.EnumerateObject().Select(property => property.Name).ShouldBe(expected);
        payload.GetRawText().ShouldBe(JsonSerializer.Serialize(value, JsonSerializerOptions.Web));
    }
}
