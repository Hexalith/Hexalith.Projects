namespace Hexalith.Projects.UI.Diagnostics;

using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.Projects.UI.Rendering;

/// <summary>Adapts supported typed SDK reads using only the current FrontComposer server authority.</summary>
public sealed class ProjectReadQueryClient(IEventStoreGatewayClient client, IEnumerable<IFrontComposerTenantContextAccessor> contexts, IEnumerable<AuthenticationStateProvider> authentication)
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Gets the current validated server authority, including in auth-disabled local startup.</summary>
    public TenantContextSnapshot? CurrentContext
        => contexts.FirstOrDefault()?.TryGetContext(operationKind: "projects-read") is { Succeeded: true, Context: { } authority }
            ? authority : null;

    /// <summary>Submits one typed query. Credentials remain exclusively owned by FrontComposer.</summary>
    public async Task<ProjectReadResult<T>> QueryAsync<T>(string target, string queryType, object payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();
        TenantContextSnapshot? authority = CurrentContext;
        if (authority is not { IsAuthenticated: true })
            return new(default, ProjectConsoleFeedback.FailClosed("safe_denial"), "server-derived tenant");
        AuthenticationStateProvider? provider = authentication.FirstOrDefault();
        AuthenticationState? state = provider is null ? null : await provider.GetAuthenticationStateAsync().ConfigureAwait(false);
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
            SubmitQueryRequest request = new(authority.TenantId, "projects", target, queryType,
                Payload: JsonSerializer.SerializeToElement(payload, payload.GetType(), PayloadOptions));
            EventStoreQueryResult<T> response = await client.SubmitQueryAsync<T>(request, null, cancellationToken).ConfigureAwait(false);
            // An authority change while the query was pending cannot disclose the previous Tenant's result.
            AuthenticationState current = await provider!.GetAuthenticationStateAsync().ConfigureAwait(false);
            if (contexts.First().Revalidate(authority, "projects-read").Succeeded != true
                || current.User.Identity?.IsAuthenticated != true
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
