// <copyright file="ProjectQueryEnvelopePrincipalBinding.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Queries;

using System.Security.Claims;

using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Projects.Server.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

/// <summary>Binds an immutable query envelope to the authenticated callback principal.</summary>
public sealed class ProjectQueryEnvelopePrincipalBinding(
    IHttpContextAccessor httpContextAccessor,
    IProjectTenantContextAccessor tenantContextAccessor,
    IConfiguration configuration,
    IHostEnvironment environment)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    private readonly IProjectTenantContextAccessor _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));

    /// <summary>Gets the current callback HTTP context.</summary>
    public HttpContext? HttpContext
    {
        get
        {
            HttpContext? original = _httpContextAccessor.HttpContext;
            VerifiedQueryCaller? caller = VerifiedQueryCaller.Get(original);
            ClaimsPrincipal? authority = caller?.Principal;
            if (authority is null)
            {
                if (original?.User.Identity is not ClaimsIdentity { IsAuthenticated: true } primary
                    || original.User.Identities.Count() == 1)
                {
                    return original;
                }

                authority = new ClaimsPrincipal(primary);
            }

            DefaultHttpContext effective = new() { User = authority, TraceIdentifier = original!.TraceIdentifier, RequestServices = original.RequestServices };
            foreach (var header in original.Request.Headers) { effective.Request.Headers[header.Key] = header.Value; }
            effective.Request.QueryString = original.Request.QueryString;
            return effective;
        }
    }

    /// <summary>Gets the deployment-owned audience validated by Projects bearer authentication.</summary>
    public string? ExpectedAudience => _configuration[$"{ProjectsAuthenticationOptions.SectionName}:Audience"];

    /// <summary>Validates principal-to-envelope identity and returns the authority context to evaluate.</summary>
    /// <param name="query">The immutable EventStore query envelope.</param>
    /// <param name="tenantContext">The authenticated or explicit Development authority context.</param>
    /// <returns><see langword="true"/> when every immutable identity field matches.</returns>
    public bool TryBind(QueryEnvelope query, out IProjectTenantContextAccessor tenantContext)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (ProjectsAuthenticationServiceCollectionExtensions.IsAnonymousDevelopmentBypass(_configuration, _environment))
        {
            string actorId = query.OriginalActorId ?? query.UserId;
            tenantContext = new DevelopmentProjectQueryTenantContextAccessor(query.TenantId, actorId);
            return !string.IsNullOrWhiteSpace(query.TenantId)
                && !string.IsNullOrWhiteSpace(actorId)
                && string.Equals(query.UserId, actorId, StringComparison.Ordinal);
        }

        if (VerifiedQueryCaller.Get(_httpContextAccessor.HttpContext) is { } verified)
        {
            tenantContext = new VerifiedProjectQueryTenantContextAccessor(verified);
            return ReferenceEquals(verified.Query, query) && verified.Query.CallerAuthority?.Origin == "authenticated-caller";
        }

        tenantContext = _tenantContextAccessor;
        ClaimsPrincipal? principal = HttpContext?.User;
        if (principal?.Identity is not ClaimsIdentity { IsAuthenticated: true } authenticatedIdentity)
        {
            return false;
        }

        Claim[] subjectClaims = authenticatedIdentity.FindAll("sub").ToArray();
        if (subjectClaims.Length != 1 || string.IsNullOrWhiteSpace(subjectClaims[0].Value))
        {
            return false;
        }

        string subject = subjectClaims[0].Value;
        if (string.IsNullOrWhiteSpace(query.OriginalActorId)
            || !string.Equals(subject, query.OriginalActorId, StringComparison.Ordinal)
            || !string.Equals(subject, query.UserId, StringComparison.Ordinal)
            || !string.Equals(_tenantContextAccessor.PrincipalId, subject, StringComparison.Ordinal)
            || !string.Equals(_tenantContextAccessor.AuthoritativeTenantId, query.TenantId, StringComparison.Ordinal))
        {
            return false;
        }

        DualPrincipalIdentity identity = DualPrincipalClaimsHelper.Extract(new ClaimsPrincipal(authenticatedIdentity), subject);
        return string.Equals(identity.AuthenticatedWorkloadId, query.AuthenticatedWorkloadId, StringComparison.Ordinal)
            && identity.IsDelegated == query.IsDelegated
            && string.Equals(identity.DelegationId, query.DelegationId, StringComparison.Ordinal)
            && SequencesMatch(identity.Scopes, query.Scopes)
            && SequencesMatch(identity.Audience, query.Audience);
    }

    private static bool SequencesMatch(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return left.SequenceEqual(right, StringComparer.Ordinal);
    }
}
