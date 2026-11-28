namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Contracts.Queries;

using Shouldly;

using Xunit;

/// <summary>Exercises the envelope-paged read through the existing persisted-view and cursor logic.</summary>
public sealed class ListProjectsEnvelopePagingTests
{
    /// <summary>Checks the returned rows and metadata, with both wire versions still supported.</summary>
    [Fact]
    public async Task ReturnsOrderedPagesAndKeepsLegacyContinuationSeparate()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        await fixture.SeedAsync("project-z", 100);
        await fixture.SeedAsync("project-a", 200);
        CancellationToken token = TestContext.Current.CancellationToken;
        QueryEnvelope envelope = fixture.Query(new ListProjectsV2Query()) with { Paging = new(PageSize: 1) };

        QueryResult first = await fixture.Executor.ListV2Async(envelope, token);
        first.Success.ShouldBeTrue(first.ErrorMessage);
        SupportedProjectReadFixture.Payload<ProjectListDocument>(first).Items.ShouldHaveSingleItem().ProjectId.ShouldBe("project-a");
        first.GetPayload().EnumerateObject().Select(field => field.Name).ShouldBe(["items", "snapshot"]);
        QueryPagingMetadata firstPaging = first.Metadata.ShouldNotBeNull().Paging.ShouldNotBeNull();
        firstPaging.PageSize.ShouldBe(1);
        firstPaging.HasMore.ShouldBe(true);
        string cursor = firstPaging.NextCursor.ShouldNotBeNull();
        cursor.ShouldNotBeNullOrWhiteSpace();

        QueryResult next = await fixture.Executor.ListV2Async(envelope with { Paging = new(PageSize: 1, Cursor: cursor) }, token);
        next.Success.ShouldBeTrue(next.ErrorMessage);
        SupportedProjectReadFixture.Payload<ProjectListDocument>(next).Items.ShouldHaveSingleItem().ProjectId.ShouldBe("project-z");
        next.Metadata!.Paging.ShouldBe(new QueryPagingMetadata(1, HasMore: false));
        QueryResult changed = await fixture.Executor.ListV2Async(envelope with { Paging = new(PageSize: 2, Cursor: cursor) }, token);
        changed.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);

        QueryResult legacy = await fixture.Executor.ListAsync(fixture.Query(new ListProjectsQuery(PageSize: 1)), token);
        legacy.Success.ShouldBeTrue(legacy.ErrorMessage);
        legacy.Metadata.ShouldBeNull();
        ProjectListPage legacyPage = SupportedProjectReadFixture.Payload<ProjectListPage>(legacy);
        legacyPage.Items.ShouldHaveSingleItem().ProjectId.ShouldBe("project-a");
        legacyPage.HasMore.ShouldBeTrue();
        string legacyCursor = legacyPage.Cursor.ShouldNotBeNull();
        legacyCursor.ShouldNotBeNullOrWhiteSpace();
        QueryResult wrongWire = await fixture.Executor.ListV2Async(envelope with { Paging = new(PageSize: 1, Cursor: legacyCursor) }, token);
        wrongWire.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);
    }

    /// <summary>Checks default paging and honest unavailable and insufficient-permission outcomes.</summary>
    [Fact]
    public async Task UnavailableViewAndDeniedCallerDoNotDisclosePagingOrRows()
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        QueryEnvelope envelope = fixture.Query(new ListProjectsV2Query());
        CancellationToken token = TestContext.Current.CancellationToken;

        QueryResult unavailable = await fixture.Executor.ListV2Async(envelope, token);
        unavailable.Success.ShouldBeTrue(unavailable.ErrorMessage);
        ProjectListDocument document = SupportedProjectReadFixture.Payload<ProjectListDocument>(unavailable);
        document.Items.ShouldBeEmpty();
        document.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Unavailable);
        unavailable.Metadata!.Paging.ShouldBe(new QueryPagingMetadata(QueryPolicyLimits.DefaultPageSize));

        await fixture.SeedAsync("protected-project");
        fixture.Identity.RemoveClaim(fixture.Identity.FindFirst("projects:role").ShouldNotBeNull());
        QueryResult denied = await fixture.Executor.ListV2Async(envelope, token);
        denied.Success.ShouldBeFalse();
        denied.ErrorMessage.ShouldBe("safe-denial");
        denied.PayloadBytes.ShouldBeNull();
        denied.Metadata.ShouldBeNull();
    }

    /// <summary>Refuses payload paging, supplied offsets and malformed cursor requests.</summary>
    [Theory]
    [InlineData("""{"pageSize":1}""", 1, null, null)]
    [InlineData("""{"cursor":null}""", null, null, null)]
    [InlineData("{}", null, 0, null)]
    [InlineData("{}", null, 1, null)]
    [InlineData("{}", 0, null, null)]
    [InlineData("{}", 201, null, null)]
    [InlineData("{}", 1, null, " ")]
    public async Task InvalidPagingFailsBeforeReadingTheProjection(string payload, int? pageSize, int? offset, string? cursor)
    {
        SupportedProjectReadFixture fixture = await SupportedProjectReadFixture.CreateAsync();
        QueryEnvelope envelope = fixture.Query(new ListProjectsV2Query()) with
        {
            Payload = Encoding.UTF8.GetBytes(payload),
            Paging = new(pageSize, offset, cursor),
        };
        QueryResult result = await fixture.Executor.ListV2Async(envelope, TestContext.Current.CancellationToken);
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        result.PayloadBytes.ShouldBeNull();
        result.Metadata.ShouldBeNull();
    }
}
