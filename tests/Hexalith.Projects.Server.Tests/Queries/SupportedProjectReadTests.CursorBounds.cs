namespace Hexalith.Projects.Server.Tests.Queries;

using System.Security.Claims;
using Hexalith.Projects.Contracts.Queries;
using Shouldly;
using Xunit;

public sealed partial class SupportedProjectReadTests
{
    [Fact]
    public async Task LongAuthenticatedScopeAndAudienceProduceBoundedReusableContinuation()
    {
        var fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-a"); await fixture.SeedAsync("project-b", 200);
        string[] scopes = ["projects.read", "projects.list", .. Enumerable.Range(0, 12).Select(index => index + new string('s', 500))];
        string[] audiences = ["configured-read-audience", .. Enumerable.Range(0, 12).Select(index => index + new string('a', 500))];
        fixture.Identity.RemoveClaim(fixture.Identity.FindFirst("scope")!);
        fixture.Identity.AddClaim(new Claim("scope", string.Join(' ', scopes)));
        foreach (string audience in audiences.Skip(1)) fixture.Identity.AddClaim(new Claim("aud", audience));
        var query = fixture.Query(new ListProjectsQuery(PageSize: 1)) with { Scopes = scopes, Audience = audiences };
        var result = await fixture.Executor.ListAsync(query, TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue(result.ErrorMessage);
        var first = SupportedProjectReadFixture.Payload<ProjectListPage>(result);
        first.Cursor.ShouldNotBeNull().Length.ShouldBeLessThan(4096);
        var next = fixture.Query(new ListProjectsQuery(PageSize: 1, Cursor: first.Cursor)) with { Scopes = query.Scopes, Audience = query.Audience };
        var second = SupportedProjectReadFixture.Payload<ProjectListPage>(await fixture.Executor.ListAsync(next, TestContext.Current.CancellationToken));
        second.Items.Single().ProjectId.ShouldBe("project-b");
    }
}
