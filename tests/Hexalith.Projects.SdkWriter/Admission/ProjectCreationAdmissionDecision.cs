// <copyright file="ProjectCreationAdmissionDecision.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Admission;

/// <summary>Safe Metadata result of evaluating a Story 7.1 creation request before task admission.</summary>
/// <param name="Outcome">The admission evaluation outcome.</param>
/// <param name="HttpStatusCode">The mapped HTTP status code.</param>
/// <param name="RejectedField">The rejected field NAME only (never its value), or null.</param>
/// <param name="ReasonCode">The Safe Metadata reason code, or null.</param>
/// <param name="CanonicalName">The canonical trimmed Project name when admitted.</param>
/// <param name="CanonicalDescription">The canonical trimmed optional description when admitted.</param>
/// <param name="CanonicalSetupMetadata">The canonical trimmed optional setup metadata when admitted.</param>
/// <param name="SuppliedFolderId">The canonical supplied Folder identifier, or null.</param>
/// <param name="RequestSha256">The normalized lowercase SHA-256 request digest when admitted.</param>
public sealed record ProjectCreationAdmissionDecision(
    ProjectCreationAdmissionOutcome Outcome,
    int HttpStatusCode,
    string? RejectedField,
    string? ReasonCode,
    string? CanonicalName,
    string? CanonicalDescription,
    string? CanonicalSetupMetadata,
    string? SuppliedFolderId,
    string? RequestSha256);
