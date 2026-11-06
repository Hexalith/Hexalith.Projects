#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.IdentityModel.Tokens.Jwt;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

/// <summary>Requires actual positive advisory observations before browser reads; supplies no authority or persisted state.</summary>
internal static class SupportedProjectAdvisoryReadiness
{
    /// <summary>Polls a bounded number of real read observations under the setup deadline.</summary>
    internal static async Task WaitAsync(Func<int, CancellationToken, Task<ProjectAdvisoryResolutionResponse?>> read,
        bool multiple, CancellationToken cancellationToken, Func<TimeSpan, CancellationToken, Task>? wait = null)
    {
        wait ??= static (duration, token) => Task.Delay(duration, token);
        for (int attempt = 1; attempt <= 30; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProjectAdvisoryResolutionResponse? result = await read(attempt, cancellationToken).ConfigureAwait(false);
            if (Matches(result, multiple)) return;
            await wait(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
        }
        throw new InvalidOperationException("Real supported Folder advisory readiness was not established; inspect its closed setup observations.");
    }

    /// <summary>Rejects aged or bootstrap tokens before a real public gateway performs normal signature/authority checks.</summary>
    internal static bool HasFreshAuthority(JwtSecurityToken token, string actor, string tenant, string issuer, DateTime now)
        => token.Subject == actor && token.Issuer == issuer && token.ValidTo > now.AddSeconds(90)
            && token.Audiences.Contains("supported-read-users", StringComparer.Ordinal)
            && token.Audiences.Contains("hexalith-eventstore", StringComparer.Ordinal)
            && token.Claims.Count(claim => claim.Type == "eventstore:current-tenant" && claim.Value == tenant) == 1
            && !token.Claims.Any(claim => claim.Type is "global_admin" or "is_global_admin" && claim.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
            && token.Claims.Any(claim => claim.Type == "projects:role" && claim.Value == "TenantProjectAdministrator")
            && token.Claims.Any(claim => claim.Type == "scope"
                && new[] { "projects.read", "projects.list" }.All(scope => claim.Value.Split(' ').Contains(scope, StringComparer.Ordinal)));

    /// <summary>Checks delivered unselected IDs/ranks/scores/reasons while preserving independent incomplete coverage.</summary>
    internal static bool Matches(ProjectAdvisoryResolutionResponse? result, bool multiple)
    {
        if (result is null || result.UnsupportedInputKinds.Count != 0
            || result.Result != (multiple ? ResolutionResult.MultipleCandidates : ResolutionResult.SingleCandidate)
            || result.Candidates.Count != (multiple ? 2 : 1)) return false;
        return result.Candidates.Select((candidate, index) => candidate.ProjectId == (index == 0 ? "project-a" : "project-b")
            && candidate.Rank == index + 1 && candidate.Score == 45
            && candidate.ReasonCodes.SequenceEqual([ProjectReasonCode.ProjectFolderMatched])).All(valid => valid);
    }
}
#endif
