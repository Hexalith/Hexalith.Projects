// <copyright file="AllowingProjectEventStoreAuthorizationValidator.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Explicit dev/test opt-in validator that allows writes.</summary>
public sealed class AllowingProjectEventStoreAuthorizationValidator : IProjectEventStoreAuthorizationValidator
{
    /// <inheritdoc/>
    public Task<EventStoreAuthorizationValidationResult> ValidateAsync(
        EventStoreAuthorizationValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(EventStoreAuthorizationValidationResult.Allowed("eventstore_validator_test_allow"));
    }
}
