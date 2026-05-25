// <copyright file="ProjectTenantEventOptionsValidator.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.TenantAccess;

using System;

using Microsoft.Extensions.Options;

/// <summary>Validates tenant-event projection writer ownership options.</summary>
public sealed class ProjectTenantEventOptionsValidator : IValidateOptions<ProjectTenantEventOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ProjectTenantEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Enum.IsDefined(options.ProjectionWriter)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"Projects tenant-event ProjectionWriter must be one of {string.Join(", ", Enum.GetNames<ProjectTenantEventProjectionWriter>())}.");
    }
}
