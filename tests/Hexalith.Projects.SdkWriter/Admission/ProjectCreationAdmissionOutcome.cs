// <copyright file="ProjectCreationAdmissionOutcome.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Admission;

/// <summary>Outcome of evaluating a <see cref="ProjectCreationRequest"/> before task admission.</summary>
public enum ProjectCreationAdmissionOutcome
{
    /// <summary>Request is valid and eligible for Durable Task admission.</summary>
    Admitted,

    /// <summary>Request failed P1 schema or metadata validation (<c>400 ValidationFailure</c>).</summary>
    ValidationFailure,

    /// <summary>Caller or operation is denied (safe denial without echoing input).</summary>
    Denied,

    /// <summary>Supplied Folder lacks valid Selection Evidence and requires Preview (<c>RequestPreview</c>).</summary>
    RequestPreview,
}
