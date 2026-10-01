// <copyright file="QueryIdentityCaptureHandler.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server.Tests.Authentication;

using System.Text.Json;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Server.Pipeline.Queries;

using MediatR;

/// <summary>Captures identity supplied by the real query controller without invoking persistence.</summary>
internal sealed class QueryIdentityCaptureHandler : IRequestHandler<SubmitQuery, SubmitQueryResult>
{
    /// <inheritdoc />
    public Task<SubmitQueryResult> Handle(SubmitQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new SubmitQueryResult(
            request.CorrelationId,
            JsonSerializer.SerializeToElement(new
            {
                Actor = request.OriginalActorId,
                request.UserId,
                Tenant = request.Tenant,
                Workload = request.AuthenticatedWorkloadId,
                Delegated = request.IsDelegated,
                request.DelegationId,
                request.Scopes,
                request.Audience,
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            "projects",
            new QueryResponseMetadata { Provenance = QueryResponseProvenance.HandlerComputed }));
    }
}
