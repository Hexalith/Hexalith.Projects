// <copyright file="ProjectCreationAdmissionEvaluator.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Admission;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Projects.Aggregates.Project;
using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Server;

/// <summary>
/// Pure P1 admission evaluator for Story 7.1 creation requests.
/// Enforces allowlisted personas, shared <see cref="SensitiveMetadataTierValidator"/>,
/// 7.1-P1 schema rules, recursive duplicate JSON key rejection across all nested objects and arrays,
/// non-blank Surface, bounded Tenant/Actor identifiers, and 5-minute Selection Evidence binding for
/// new supplied-Folder admissions.
/// </summary>
public static class ProjectCreationAdmissionEvaluator
{
    private const int MaxScopeIdentifierLength = 256;
    private static readonly TimeSpan MaxSelectionEvidenceLifetime = TimeSpan.FromMinutes(5);

    /// <summary>Evaluates a creation request without admitting a task or submitting any command.</summary>
    /// <param name="request">The incoming creation request.</param>
    /// <param name="now">The current UTC timestamp.</param>
    /// <param name="skipSelectionEvidenceExpiryCheck">
    /// When <see langword="true"/>, bypasses the 5-minute Selection Evidence expiry check so an
    /// already-admitted supplied-Folder task can complete forward recovery after 5 minutes while still
    /// enforcing syntax, persona, and binding rules.
    /// </param>
    /// <returns>The safe metadata admission decision.</returns>
    public static ProjectCreationAdmissionDecision Evaluate(
        ProjectCreationRequest request,
        DateTimeOffset now,
        bool skipSelectionEvidenceExpiryCheck = false)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Validate Idempotency-Key shape first (matches 7.1-P1 header-first rule).
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)
            || request.IdempotencyKey.Length > MaxScopeIdentifierLength
            || !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(request.IdempotencyKey))
        {
            return ValidationFailure("idempotency_key");
        }

        // 2. Auth-before-parse: allowlist ONLY ProjectUser and ServiceWorkflowCaller; reject all other
        //    personas (including unknown cast enum values, TenantOperator, TenantProjectAdministrator,
        //    WorkloadIdentity) and invalid/system/oversized Tenant/Actor before inspecting body bytes.
        if (request.CallerClass is not (ProjectCreationCallerClass.ProjectUser or ProjectCreationCallerClass.ServiceWorkflowCaller))
        {
            return Denied("caller_not_authorized", 404);
        }

        if (string.IsNullOrWhiteSpace(request.TenantId)
            || request.TenantId.Length > MaxScopeIdentifierLength
            || string.Equals(request.TenantId, "system", StringComparison.OrdinalIgnoreCase)
            || !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(request.TenantId))
        {
            return Denied("tenant_access_denied", 404);
        }

        if (string.IsNullOrWhiteSpace(request.ActorPrincipalId)
            || request.ActorPrincipalId.Length > MaxScopeIdentifierLength
            || !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(request.ActorPrincipalId))
        {
            return Denied("actor_not_authorized", 404);
        }

        if (string.IsNullOrWhiteSpace(request.Surface)
            || request.Surface.Length > 128
            || !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(request.Surface))
        {
            return ValidationFailure("surface");
        }

        string? effectiveSchemaVersion = request.RequestSchemaVersion;
        string? effectiveMetadataClass = request.MetadataClass;
        string? effectiveName = request.Name;
        string? effectiveDescription = request.Description;
        string? effectiveSetupMetadata = request.SetupMetadata;
        string? effectiveSuppliedFolderId = request.SuppliedFolderId;

        // 3. Raw body JSON validation: strict recursive duplicate-key rejection + 7.1-P1 schema rules.
        if (request.RawBodyJson is not null)
        {
            ProjectCreationAdmissionDecision? rawFailure = TryParseAndValidateRawBody(
                request.RawBodyJson,
                ref effectiveSchemaVersion,
                ref effectiveMetadataClass,
                ref effectiveName,
                ref effectiveDescription,
                ref effectiveSetupMetadata,
                ref effectiveSuppliedFolderId);
            if (rawFailure is not null)
            {
                return rawFailure;
            }
        }
        else
        {
            // Enforce 7.1-P1 schema rules on typed requests as well:
            // - RequestSchemaVersion == null with MetadataClass != null is rejected.
            // - Non-null RequestSchemaVersion must be "v1".
            // - When RequestSchemaVersion == "v1", MetadataClass must be a valid SensitiveMetadataTier.
            if (effectiveSchemaVersion is null && effectiveMetadataClass is not null)
            {
                return ValidationFailure("requestSchemaVersion");
            }

            if (effectiveSchemaVersion is not null && !string.Equals(effectiveSchemaVersion, "v1", StringComparison.Ordinal))
            {
                return ValidationFailure("requestSchemaVersion");
            }

            if (string.Equals(effectiveSchemaVersion, "v1", StringComparison.Ordinal)
                && !SensitiveMetadataTierValidator.IsValid(effectiveMetadataClass))
            {
                return ValidationFailure("projectMetadata.metadataClass");
            }
        }

        if (effectiveMetadataClass is not null && !SensitiveMetadataTierValidator.IsValid(effectiveMetadataClass))
        {
            return ValidationFailure("projectMetadata.metadataClass");
        }

        // 4. Shared domain command validation for Name, Description, and SetupMetadata (never echoes input).
        string syntheticCorrelation = !string.IsNullOrWhiteSpace(request.CorrelationId)
            && ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(request.CorrelationId)
            ? request.CorrelationId
            : request.IdempotencyKey;

        CreateProject probeCommand = new(
            request.TenantId,
            new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB"),
            effectiveName ?? string.Empty,
            effectiveDescription,
            effectiveSetupMetadata,
            request.ActorPrincipalId,
            syntheticCorrelation,
            syntheticCorrelation,
            request.IdempotencyKey);

        ProjectCommandValidationResult validation = ProjectCommandValidator.Validate(probeCommand);
        if (!validation.IsAccepted)
        {
            string rejectedField = validation.RejectedField switch
            {
                nameof(CreateProject.Name) => "name",
                nameof(CreateProject.Description) => "description",
                nameof(CreateProject.SetupMetadata) => "setupMetadata",
                _ => validation.RejectedField ?? "body",
            };
            return ValidationFailure(rejectedField);
        }

        // 5. Supplied Folder & FR-25 Selection Evidence rules.
        string? canonicalSuppliedFolderId = null;
        if (effectiveSuppliedFolderId is not null)
        {
            if (string.IsNullOrWhiteSpace(effectiveSuppliedFolderId))
            {
                return ValidationFailure("suppliedFolderId");
            }

            canonicalSuppliedFolderId = effectiveSuppliedFolderId.Trim();
            if (!ProjectCommandValidator.IsSafePersistedReferenceIdentifier(canonicalSuppliedFolderId))
            {
                return ValidationFailure("suppliedFolderId");
            }

            // Service/Workflow Caller may create ONLY with no Folder supplied.
            if (request.CallerClass == ProjectCreationCallerClass.ServiceWorkflowCaller)
            {
                return Denied("service_caller_cannot_supply_folder", 403);
            }

            // Project User with supplied Folder requires valid Selection Evidence bound to
            // (TenantId, ActorPrincipalId, Surface, SuppliedFolderId) with <= 5 minute lifetime.
            if (!IsValidSelectionEvidence(
                    request.SelectionEvidence,
                    request.TenantId,
                    request.ActorPrincipalId,
                    request.Surface,
                    canonicalSuppliedFolderId,
                    now,
                    skipSelectionEvidenceExpiryCheck))
            {
                return new ProjectCreationAdmissionDecision(
                    ProjectCreationAdmissionOutcome.RequestPreview,
                    409,
                    null,
                    "RequestPreview",
                    null,
                    null,
                    null,
                    null,
                    null);
            }
        }

        string requestSha256 = ComputeNormalizedRequestSha256(
            request.TenantId,
            request.ActorPrincipalId,
            request.CallerClass,
            validation.CanonicalName!,
            validation.CanonicalDescription,
            validation.CanonicalSetupMetadata,
            canonicalSuppliedFolderId,
            effectiveSchemaVersion,
            effectiveMetadataClass);

        return new ProjectCreationAdmissionDecision(
            ProjectCreationAdmissionOutcome.Admitted,
            202,
            null,
            null,
            validation.CanonicalName!,
            validation.CanonicalDescription,
            validation.CanonicalSetupMetadata,
            canonicalSuppliedFolderId,
            requestSha256);
    }

    private static ProjectCreationAdmissionDecision? TryParseAndValidateRawBody(
        string rawBodyJson,
        ref string? schemaVersion,
        ref string? metadataClass,
        ref string? name,
        ref string? description,
        ref string? setupMetadata,
        ref string? suppliedFolderId)
    {
        if (string.IsNullOrWhiteSpace(rawBodyJson))
        {
            return ValidationFailure("body");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawBodyJson);
        }
        catch (JsonException)
        {
            return ValidationFailure("body");
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return ValidationFailure("body");
            }

            if (HasDuplicateKeysRecursive(root, out string? duplicatePath))
            {
                return ValidationFailure(
                    string.Equals(duplicatePath, "projectMetadata.metadataClass", StringComparison.OrdinalIgnoreCase)
                        ? "projectMetadata.metadataClass"
                        : "body");
            }

            foreach (JsonProperty rootProp in root.EnumerateObject())
            {
                if (!IsAllowedRootProperty(rootProp.Name))
                {
                    return ValidationFailure("body");
                }
            }

            string? jsonSchemaVersion = null;
            if (TryGetPropertyIgnoreCase(root, "requestSchemaVersion", out JsonElement schemaElement))
            {
                if (schemaElement.ValueKind == JsonValueKind.String)
                {
                    jsonSchemaVersion = schemaElement.GetString();
                }
                else if (schemaElement.ValueKind != JsonValueKind.Null)
                {
                    return ValidationFailure("requestSchemaVersion");
                }
            }

            bool hasProjectMetadata = TryGetPropertyIgnoreCase(root, "projectMetadata", out JsonElement pmElement)
                && pmElement.ValueKind != JsonValueKind.Null;

            if (hasProjectMetadata && pmElement.ValueKind != JsonValueKind.Object)
            {
                return ValidationFailure("projectMetadata");
            }

            if (hasProjectMetadata && !string.Equals(jsonSchemaVersion, "v1", StringComparison.Ordinal))
            {
                return ValidationFailure("requestSchemaVersion");
            }

            if (jsonSchemaVersion is not null
                && !string.Equals(jsonSchemaVersion, "v1", StringComparison.Ordinal))
            {
                return ValidationFailure("requestSchemaVersion");
            }

            if (!hasProjectMetadata && string.Equals(jsonSchemaVersion, "v1", StringComparison.Ordinal))
            {
                return ValidationFailure("projectMetadata");
            }

            string? jsonTopLevelName = null;
            if (TryGetPropertyIgnoreCase(root, "name", out JsonElement nameElement))
            {
                if (nameElement.ValueKind == JsonValueKind.String)
                {
                    jsonTopLevelName = nameElement.GetString();
                }
                else if (nameElement.ValueKind != JsonValueKind.Null)
                {
                    return ValidationFailure("name");
                }
            }

            string? jsonMetadataDisplayName = null;
            string? jsonMetadataClass = null;
            if (hasProjectMetadata)
            {
                foreach (JsonProperty pmProp in pmElement.EnumerateObject())
                {
                    if (!IsAllowedProjectMetadataProperty(pmProp.Name))
                    {
                        return ValidationFailure("projectMetadata");
                    }
                }

                if (!TryGetPropertyIgnoreCase(pmElement, "metadataClass", out JsonElement mcElement)
                    || mcElement.ValueKind != JsonValueKind.String
                    || !SensitiveMetadataTierValidator.IsValid(mcElement.GetString()))
                {
                    return ValidationFailure("projectMetadata.metadataClass");
                }

                jsonMetadataClass = mcElement.GetString();

                if (!TryGetPropertyIgnoreCase(pmElement, "displayName", out JsonElement dnElement)
                    || dnElement.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(dnElement.GetString()))
                {
                    return ValidationFailure("projectMetadata.displayName");
                }

                jsonMetadataDisplayName = dnElement.GetString();
            }

            if (!string.IsNullOrWhiteSpace(jsonMetadataDisplayName)
                && !string.IsNullOrWhiteSpace(jsonTopLevelName)
                && !string.Equals(jsonMetadataDisplayName.Trim(), jsonTopLevelName.Trim(), StringComparison.Ordinal))
            {
                return ValidationFailure("name");
            }

            string? jsonDescription = null;
            if (TryGetPropertyIgnoreCase(root, "description", out JsonElement descElement))
            {
                if (descElement.ValueKind == JsonValueKind.String)
                {
                    jsonDescription = descElement.GetString();
                }
                else if (descElement.ValueKind != JsonValueKind.Null)
                {
                    return ValidationFailure("description");
                }
            }

            string? jsonSetupMetadata = null;
            if (TryGetPropertyIgnoreCase(root, "setupMetadata", out JsonElement setupElement))
            {
                if (setupElement.ValueKind == JsonValueKind.String)
                {
                    jsonSetupMetadata = setupElement.GetString();
                }
                else if (setupElement.ValueKind != JsonValueKind.Null)
                {
                    return ValidationFailure("setupMetadata");
                }
            }

            string? jsonSuppliedFolderId = null;
            if (TryGetPropertyIgnoreCase(root, "suppliedFolderId", out JsonElement folderElement))
            {
                if (folderElement.ValueKind == JsonValueKind.String)
                {
                    jsonSuppliedFolderId = folderElement.GetString();
                }
                else if (folderElement.ValueKind != JsonValueKind.Null)
                {
                    return ValidationFailure("suppliedFolderId");
                }
            }

            schemaVersion = jsonSchemaVersion;
            metadataClass = jsonMetadataClass;
            name = hasProjectMetadata ? jsonMetadataDisplayName : jsonTopLevelName;
            description = jsonDescription;
            setupMetadata = jsonSetupMetadata;
            suppliedFolderId = jsonSuppliedFolderId;
            return null;
        }
    }

    private static bool HasDuplicateKeysRecursive(JsonElement element, out string? duplicatePath, string prefix = "")
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                string path = string.IsNullOrEmpty(prefix) ? property.Name : prefix + "." + property.Name;
                if (!seen.Add(property.Name))
                {
                    duplicatePath = path;
                    return true;
                }

                if (HasDuplicateKeysRecursive(property.Value, out duplicatePath, path))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                string path = $"{prefix}[{index}]";
                if (HasDuplicateKeysRecursive(item, out duplicatePath, path))
                {
                    return true;
                }

                index++;
            }
        }

        duplicatePath = null;
        return false;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
    {
        foreach (JsonProperty property in obj.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool IsValidSelectionEvidence(
        ProjectCreationSelectionEvidence? evidence,
        string tenantId,
        string actorPrincipalId,
        string surface,
        string suppliedFolderId,
        DateTimeOffset now,
        bool skipExpiryCheck)
    {
        if (evidence is null
            || string.IsNullOrWhiteSpace(evidence.EvidenceId)
            || !ProjectCommandValidator.IsSafePersistedEnvelopeIdentifier(evidence.EvidenceId)
            || !string.Equals(evidence.TenantId, tenantId, StringComparison.Ordinal)
            || !string.Equals(evidence.ActorPrincipalId, actorPrincipalId, StringComparison.Ordinal)
            || !string.Equals(evidence.Surface, surface, StringComparison.Ordinal)
            || !string.Equals(evidence.FolderId, suppliedFolderId, StringComparison.Ordinal)
            || evidence.IssuedAt == default
            || evidence.ExpiresAt <= evidence.IssuedAt
            || (evidence.ExpiresAt - evidence.IssuedAt) > MaxSelectionEvidenceLifetime)
        {
            return false;
        }

        if (!skipExpiryCheck && (now < evidence.IssuedAt || now >= evidence.ExpiresAt))
        {
            return false;
        }

        return true;
    }

    private static bool IsAllowedRootProperty(string propertyName)
        => string.Equals(propertyName, "requestSchemaVersion", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "projectMetadata", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "name", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "description", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "setupMetadata", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "suppliedFolderId", StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowedProjectMetadataProperty(string propertyName)
        => string.Equals(propertyName, "metadataClass", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "displayName", StringComparison.OrdinalIgnoreCase);

    private static string ComputeNormalizedRequestSha256(
        string tenantId,
        string actorPrincipalId,
        ProjectCreationCallerClass callerClass,
        string canonicalName,
        string? canonicalDescription,
        string? canonicalSetupMetadata,
        string? suppliedFolderId,
        string? requestSchemaVersion,
        string? metadataClass)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new[]
        {
            "projects:create:v1",
            tenantId,
            actorPrincipalId,
            callerClass.ToString(),
            canonicalName,
            canonicalDescription ?? string.Empty,
            canonicalSetupMetadata ?? string.Empty,
            suppliedFolderId ?? string.Empty,
            requestSchemaVersion ?? string.Empty,
            metadataClass ?? string.Empty,
        });
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    private static ProjectCreationAdmissionDecision ValidationFailure(string rejectedField)
        => new(
            ProjectCreationAdmissionOutcome.ValidationFailure,
            400,
            rejectedField,
            "validation_error",
            null,
            null,
            null,
            null,
            null);

    private static ProjectCreationAdmissionDecision Denied(string reasonCode, int statusCode)
        => new(
            ProjectCreationAdmissionOutcome.Denied,
            statusCode,
            null,
            reasonCode,
            null,
            null,
            null,
            null,
            null);
}
