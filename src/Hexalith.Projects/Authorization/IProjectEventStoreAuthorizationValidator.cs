// <copyright file="IProjectEventStoreAuthorizationValidator.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System.Threading;
using System.Threading.Tasks;

/// <summary>Defense-in-depth validator for the EventStore write layer.</summary>
public interface IProjectEventStoreAuthorizationValidator
{
    /// <summary>Validates a Projects write request with metadata-only evidence.</summary>
    Task<EventStoreAuthorizationValidationResult> ValidateAsync(
        EventStoreAuthorizationValidationRequest request,
        CancellationToken cancellationToken = default);
}
