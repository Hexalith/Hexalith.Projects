// <copyright file="DenyAllProjectEventStoreAuthorizationValidator.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Fail-closed production-shaped default validator for unconfigured deployments.</summary>
public sealed class DenyAllProjectEventStoreAuthorizationValidator : IProjectEventStoreAuthorizationValidator
{
    /// <inheritdoc/>
    public Task<EventStoreAuthorizationValidationResult> ValidateAsync(
        EventStoreAuthorizationValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(EventStoreAuthorizationValidationResult.Denied());
    }
}
