namespace Hexalith.Projects.Contracts.Tests.Queries;

using System.Text.Json;
using Hexalith.Projects.Contracts.Queries;
using Shouldly;
using Xunit;

/// <summary>Retains the original binary/source positional APIs and additive JSON flags.</summary>
public sealed class ProjectReadQueryCompatibilityTests
{
    [Fact]
    public void OriginalConstructorsAndDeconstructionRemainAvailable()
    {
        typeof(GetProjectQuery).GetConstructor([typeof(bool)]).ShouldNotBeNull();
        GetProjectQuery open = new(true);
        open.Deconstruct(out bool inspect);
        inspect.ShouldBeTrue();
        open.InspectQuarantined.ShouldBeFalse();
        typeof(ListProjectsQuery).GetConstructor([typeof(string), typeof(string), typeof(int), typeof(bool)]).ShouldNotBeNull();
        ListProjectsQuery list = new("Active", "opaque", 12, true);
        var (lifecycle, cursor, size, descriptive) = list;
        lifecycle.ShouldBe("Active"); cursor.ShouldBe("opaque"); size.ShouldBe(12); descriptive.ShouldBeTrue();
        list.IncludeQuarantined.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void JsonSelectsExpandedConstructorAndMissingFlagsRetainDefaults(bool expanded)
    {
        string openJson = expanded ? "{\"inspectDescriptiveMetadata\":true,\"inspectQuarantined\":true}" : "{\"inspectDescriptiveMetadata\":true}";
        GetProjectQuery open = JsonSerializer.Deserialize<GetProjectQuery>(openJson, JsonSerializerOptions.Web)!;
        open.InspectDescriptiveMetadata.ShouldBeTrue(); open.InspectQuarantined.ShouldBe(expanded);
        JsonSerializer.Deserialize<GetProjectQuery>(JsonSerializer.Serialize(open, JsonSerializerOptions.Web), JsonSerializerOptions.Web).ShouldBe(open);
        string listJson = "{\"lifecycleState\":\"Active\",\"cursor\":\"opaque\",\"pageSize\":12,\"inspectDescriptiveMetadata\":true" + (expanded ? ",\"includeQuarantined\":true}" : "}");
        ListProjectsQuery list = JsonSerializer.Deserialize<ListProjectsQuery>(listJson, JsonSerializerOptions.Web)!;
        list.ShouldBe(new("Active", "opaque", 12, true, expanded));
        JsonSerializer.Deserialize<ListProjectsQuery>(JsonSerializer.Serialize(list, JsonSerializerOptions.Web), JsonSerializerOptions.Web).ShouldBe(list);
        JsonSerializer.Deserialize<GetProjectQuery>("{}", JsonSerializerOptions.Web).ShouldBe(new GetProjectQuery());
        JsonSerializer.Deserialize<ListProjectsQuery>("{}", JsonSerializerOptions.Web).ShouldBe(new ListProjectsQuery());
    }
}
