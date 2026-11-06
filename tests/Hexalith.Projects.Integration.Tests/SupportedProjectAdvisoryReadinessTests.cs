#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Xunit;

/// <summary>Controls for genuine positive advisory setup observations without inferred admission or fabricated state.</summary>
public sealed class SupportedProjectAdvisoryReadinessTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IncompleteEmptySetupCannotQualifyButActualNonemptyObservationCan(bool multiple)
    {
        int attempts = 0;
        await SupportedProjectAdvisoryReadiness.WaitAsync((attempt, _) =>
        {
            attempts = attempt;
            return Task.FromResult<ProjectAdvisoryResolutionResponse?>(attempt == 1
                ? new(null, [], DateTimeOffset.UtcNow, true, []) : Positive(multiple, incomplete: true));
        }, multiple, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(2, attempts);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("rank")]
    [InlineData("score")]
    [InlineData("reason")]
    [InlineData("kind")]
    [InlineData("result")]
    public void WrongCandidatesOrUnsupportedCoverageNeverQualify(string kind)
    {
        ProjectAdvisoryResolutionResponse expected = Positive(false, incomplete: false);
        var first = expected.Candidates[0];
        var mutated = kind switch
        {
            "id" => expected with { Candidates = [first with { ProjectId = "foreign" }] },
            "rank" => expected with { Candidates = [first with { Rank = 2 }] },
            "score" => expected with { Candidates = [first with { Score = 0 }] },
            "reason" => expected with { Candidates = [first with { ReasonCodes = [] }] },
            "kind" => expected with { UnsupportedInputKinds = ["file"] },
            _ => expected with { Result = ResolutionResult.NoMatch },
        };
        Assert.False(SupportedProjectAdvisoryReadiness.Matches(mutated, false));
    }

    [Fact]
    public async Task SetupCancellationStopsPollingWithoutClaimingReadiness()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        int calls = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SupportedProjectAdvisoryReadiness.WaitAsync((_, _) =>
        {
            calls++;
            return Task.FromResult<ProjectAdvisoryResolutionResponse?>(Positive(false, false));
        }, false, cancellation.Token)).ConfigureAwait(true);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task PermanentlyIncompleteSetupHasFiniteObservationAndDelayBounds()
    {
        int calls = 0, delays = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => SupportedProjectAdvisoryReadiness.WaitAsync((_, _) =>
        {
            calls++;
            return Task.FromResult<ProjectAdvisoryResolutionResponse?>(new(null, [], DateTimeOffset.UtcNow, true, []));
        }, false, TestContext.Current.CancellationToken, (_, _) => { delays++; return Task.CompletedTask; })).ConfigureAwait(true);
        Assert.Equal(30, calls);
        Assert.Equal(30, delays);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("short")]
    [InlineData("bootstrap")]
    [InlineData("actor")]
    [InlineData("tenant")]
    [InlineData("issuer")]
    public void AgedBootstrapOrMismatchedReadCredentialIsRejected(string mutation)
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [new("sub", mutation == "actor" ? "other" : "actor"), new("eventstore:current-tenant", mutation == "tenant" ? "other" : "tenant"),
            new("projects:role", "TenantProjectAdministrator"), new("scope", "projects.read projects.list"), new("aud", "hexalith-eventstore")];
        if (mutation == "bootstrap") claims.Add(new("global_admin", "true"));
        JwtSecurityToken token = new(mutation == "issuer" ? "other" : "issuer", "supported-read-users", claims,
            expires: mutation == "expired" ? now.AddSeconds(-1) : mutation == "short" ? now.AddSeconds(89) : now.AddMinutes(5));
        Assert.False(SupportedProjectAdvisoryReadiness.HasFreshAuthority(token, "actor", "tenant", "issuer", now));
    }

    [Fact]
    public void FreshReadCredentialPreservesTheExactActorTenantAudienceAndScope()
    {
        DateTime now = DateTime.UtcNow;
        JwtSecurityToken token = new("issuer", "supported-read-users",
            [new("sub", "actor"), new("eventstore:current-tenant", "tenant"), new("projects:role", "TenantProjectAdministrator"),
                new("scope", "projects.read projects.list"), new("aud", "hexalith-eventstore")], expires: now.AddMinutes(5));
        Assert.True(SupportedProjectAdvisoryReadiness.HasFreshAuthority(token, "actor", "tenant", "issuer", now));
    }

    private static ProjectAdvisoryResolutionResponse Positive(bool multiple, bool incomplete)
        => new(multiple ? ResolutionResult.MultipleCandidates : ResolutionResult.SingleCandidate,
            multiple ? [new("project-a", [ProjectReasonCode.ProjectFolderMatched], 1, 45), new("project-b", [ProjectReasonCode.ProjectFolderMatched], 2, 45)]
                : [new("project-a", [ProjectReasonCode.ProjectFolderMatched], 1, 45)], DateTimeOffset.UtcNow, incomplete, []);
}
#endif
