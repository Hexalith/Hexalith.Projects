namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.Client;
using Microsoft.AspNetCore.Components.Authorization;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.Projects.UI.Rendering;

/// <summary>Adapts supported typed SDK reads using only the current FrontComposer server authority.</summary>
public sealed class ProjectReadQueryClient(IEventStoreGatewayClient client, IEnumerable<IFrontComposerTenantContextAccessor> contexts, IEnumerable<AuthenticationStateProvider> authentication)
{
    private readonly ProjectsSupportedReadClient _supportedReads = new(client);

    /// <summary>Gets the current validated server authority, including in auth-disabled local startup.</summary>
    public TenantContextSnapshot? CurrentContext
        => contexts.FirstOrDefault()?.TryGetContext(operationKind: "projects-read") is { Succeeded: true, Context: { } authority }
            ? authority : null;

    /// <summary>Submits one typed query. Credentials remain exclusively owned by FrontComposer.</summary>
    public async Task<ProjectReadResult<T>> QueryAsync<T>(string target, string queryType, object payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return await QueryAsync<T>(target, queryType, _ => payload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Prepares a tenant-scoped payload after validating the current identity.</summary>
    internal async Task<ProjectReadResult<T>> QueryAsync<T>(string target, string queryType, Func<string, object> createPayload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AuthenticationStateProvider? provider;
        AuthenticationState? state;
        TenantContextSnapshot? authority;
        try
        {
            provider = authentication.FirstOrDefault();
            state = provider is null
                ? null
                : await provider.GetAuthenticationStateAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            authority = state?.User.Identity?.IsAuthenticated == true ? CurrentContext : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new(default, ProjectConsoleFeedback.Warning("data_unavailable"), "server-derived tenant");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (authority is not { IsAuthenticated: true }
            || state?.User.Identity?.IsAuthenticated != true
            || state.User.FindFirst("sub")?.Value != authority.UserId
            || state.User.FindFirst(ProjectsUiSecurity.TenantClaimType)?.Value != authority.TenantId
            || string.IsNullOrWhiteSpace(state.User.FindFirst(FrontComposerTokenSession.ClaimType)?.Value))
        {
            return new(default, ProjectConsoleFeedback.FailClosed("safe_denial"), "server-derived tenant");
        }

        try
        {
            object payload = createPayload(authority.TenantId);
            EventStoreQueryResult<T> response = await _supportedReads.QueryAsync<T>(authority.TenantId, target, queryType, payload, cancellationToken).ConfigureAwait(false);
            // An authority change while the query was pending cannot disclose the previous Tenant's result.
            AuthenticationState current = await provider!.GetAuthenticationStateAsync().ConfigureAwait(false);
            if (contexts.First().Revalidate(authority, "projects-read").Succeeded != true
                || current.User.Identity?.IsAuthenticated != true
                || current.User.FindFirst("sub")?.Value != authority.UserId
                || current.User.FindFirst(ProjectsUiSecurity.TenantClaimType)?.Value != authority.TenantId
                || current.User.FindFirst(FrontComposerTokenSession.ClaimType)?.Value != state.User.FindFirst(FrontComposerTokenSession.ClaimType)?.Value)
            {
                return new(default, ProjectConsoleFeedback.FailClosed("safe_denial"), "server-derived tenant");
            }
            return response.IsNotModified || response.Payload is null
                ? new(default, ProjectConsoleFeedback.Warning("data_unavailable"), authority.TenantId)
                : new(response.Payload, null, authority.TenantId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (EventStoreGatewayException exception)
        {
            // Never disclose remote ProblemDetails, correlation IDs, reason strings or exception messages.
            ProjectConsoleFeedback feedback = exception.StatusCode switch
            {
                401 or 403 or 404 => ProjectConsoleFeedback.FailClosed("safe_denial"),
                400 => ProjectConsoleFeedback.Error("validation_error"),
                _ => ProjectConsoleFeedback.Warning("data_unavailable"),
            };
            return new(default, feedback, authority.TenantId);
        }
        catch (Exception)
        {
            return new(default, ProjectConsoleFeedback.Warning("data_unavailable"), authority.TenantId);
        }
    }
}
