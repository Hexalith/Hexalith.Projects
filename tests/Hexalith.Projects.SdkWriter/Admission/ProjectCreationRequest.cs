// <copyright file="ProjectCreationRequest.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Admission;

/// <summary>Input payload for Story 7.1 Folder-first Project creation admission and execution.</summary>
/// <param name="TenantId">The authoritative single-Tenant identifier.</param>
/// <param name="ActorPrincipalId">The authenticated original actor principal identifier.</param>
/// <param name="CallerClass">The authenticated caller persona class.</param>
/// <param name="Surface">The non-blank authenticated surface (e.g. <c>ui</c>, <c>agent</c>, <c>service</c>).</param>
/// <param name="IdempotencyKey">The caller-supplied idempotency key.</param>
/// <param name="Name">The Project display name (required unless supplied via <paramref name="RawBodyJson"/>).</param>
/// <param name="Description">Optional safe metadata description.</param>
/// <param name="SetupMetadata">Optional safe metadata setup reference.</param>
/// <param name="RequestSchemaVersion">Optional request schema version (<c>v1</c> for canonical requests; null for historical name-only).</param>
/// <param name="MetadataClass">Optional sensitive metadata classification tier.</param>
/// <param name="SuppliedFolderId">Optional actor-selected existing Folder identifier.</param>
/// <param name="SelectionEvidence">Optional FR-25 Selection Evidence for a supplied Folder.</param>
/// <param name="RawBodyJson">Optional raw JSON request body for strict duplicate-key and 7.1-P1 schema checks.</param>
/// <param name="IsDelegated">Whether the call arrived via a delegated Chatbot adapter.</param>
/// <param name="DelegatedWorkloadId">Optional delegated workload identifier (never widens authority).</param>
/// <param name="CorrelationId">Optional correlation identifier.</param>
public sealed record ProjectCreationRequest(
    string TenantId,
    string ActorPrincipalId,
    ProjectCreationCallerClass CallerClass,
    string Surface,
    string IdempotencyKey,
    string? Name,
    string? Description = null,
    string? SetupMetadata = null,
    string? RequestSchemaVersion = null,
    string? MetadataClass = null,
    string? SuppliedFolderId = null,
    ProjectCreationSelectionEvidence? SelectionEvidence = null,
    string? RawBodyJson = null,
    bool IsDelegated = false,
    string? DelegatedWorkloadId = null,
    string? CorrelationId = null);
