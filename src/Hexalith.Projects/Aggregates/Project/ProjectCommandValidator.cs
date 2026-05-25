// <copyright file="ProjectCommandValidator.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Aggregates.Project;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Hexalith.Projects.Contracts.Commands;
using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Pure boundary validator for Projects commands (FR-19). Mirrors the Folders
/// <c>FolderCommandValidator</c>: Tier-1, no Dapr/network/ACL/HTTP. It enforces the only-required-input
/// rule (a non-empty project name), fails closed on a missing/unauthorized tenant, validates the
/// project-id shape without <c>Guid.TryParse</c> (ULID-shaped per R2-A7), and rejects unsafe setup
/// content — raw secrets, unrestricted/local file paths, unsupported reference types, and
/// foreign-context payloads — returning the rejected field NAME only (never its value).
/// </summary>
/// <remarks>
/// The accepted result also carries the canonical idempotency fingerprint. The fingerprint reuses the
/// Story 1.3 canonical hasher <em>semantics</em> (operation line + ordered <c>field=…;present=…;value=…</c>
/// lines, joined with <c>\n</c>, SHA-256, <c>sha256:</c>-prefixed) over the spine's
/// <c>x-hexalith-idempotency-equivalence</c> field list (<c>project_metadata.display_name</c>,
/// <c>request_schema_version</c>) — not a parallel scheme. It is recomputed here without a dependency on
/// the Client project (domain core stays pure: no Newtonsoft, no Client reference).
/// </remarks>
public static class ProjectCommandValidator
{
    internal const int MaxNameLength = 160;
    internal const int MaxDescriptionLength = 512;
    internal const int MaxSetupMetadataLength = 512;
    internal const int MaxSetupTextLength = 512;
    internal const int MaxSetupTextItems = 16;
    internal const int MaxSetupSourceKinds = 4;

    // The request schema version the spine's CreateProjectRequest pins. Part of the idempotency
    // equivalence list, so it is canonicalized into the fingerprint.
    private const string RequestSchemaVersion = "v1";

    // Substring blocklist for free-form metadata (name/description/setup). Catches payload-shaped
    // leakage — secrets/tokens, file/URL/path markers, transcript/diff bodies, foreign-context
    // markers — cross-checked against PayloadClassification.ForbiddenContent (Story 1.2). Matching is
    // case-insensitive after NFC-normalization.
    private static readonly string[] ForbiddenMetadataSubstrings =
    [
        "credential",
        "token",
        "secret",
        "password",
        "api-key",
        "apikey",
        "raw file",
        "file content",
        "filecontents",
        "memory body",
        "memorybody",
        "raw prompt",
        "rawprompt",
        "transcript",
        "diff --git",
        "generated context",
        "model provider",
        "model-provider",
        "model=",
        "model:",
        "model id",
        "modelid",
        "provider payload",
        "provider=",
        "provider:",
        "provider id",
        "providerid",
        "begin rsa",
        "-----begin",
        "://",
        "\\",
        "/",
        "..",
    ];

    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Validates a <see cref="CreateProject"/> command and computes its canonical idempotency
    /// fingerprint.
    /// </summary>
    /// <param name="command">The create command to validate.</param>
    /// <returns>An accepted result with canonical fields + fingerprint, or a rejected result with the code and the field NAME.</returns>
    public static ProjectCommandValidationResult Validate(CreateProject command)
    {
        ArgumentNullException.ThrowIfNull(command);

        ProjectCommandValidationResult common = ValidateCommon(command);
        if (!common.IsAccepted)
        {
            return common;
        }

        // The only required user input is the project name.
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > MaxNameLength || !IsSafeMetadata(command.Name))
        {
            return ProjectCommandValidationResult.Rejected(ProjectResultCode.ValidationFailed, nameof(command.Name));
        }

        if (!IsSafeOptionalMetadata(command.Description, MaxDescriptionLength))
        {
            return ProjectCommandValidationResult.Rejected(ProjectResultCode.ValidationFailed, nameof(command.Description));
        }

        if (!IsSafeOptionalMetadata(command.SetupMetadata, MaxSetupMetadataLength))
        {
            return ProjectCommandValidationResult.Rejected(ProjectResultCode.ValidationFailed, nameof(command.SetupMetadata));
        }

        // Envelope identifiers must be present and free of control/line-separator characters so they
        // cannot smuggle log/trace injection downstream.
        if (!IsSafeEnvelopeIdentifier(command.ActorPrincipalId)
            || !IsSafeEnvelopeIdentifier(command.CorrelationId)
            || !IsSafeEnvelopeIdentifier(command.TaskId)
            || !IsSafeEnvelopeIdentifier(command.IdempotencyKey))
        {
            return ProjectCommandValidationResult.Rejected(ProjectResultCode.ValidationFailed, "envelope");
        }

        string canonicalName = command.Name.Trim();
        string? canonicalDescription = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        string? canonicalSetupMetadata = string.IsNullOrWhiteSpace(command.SetupMetadata) ? null : command.SetupMetadata.Trim();

        return ProjectCommandValidationResult.Accepted(
            canonicalName,
            canonicalDescription,
            canonicalSetupMetadata,
            ComputeIdempotencyFingerprint(canonicalName));
    }

    /// <summary>
    /// Validates a <see cref="UpdateProjectSetup"/> command and computes its canonical idempotency
    /// fingerprint.
    /// </summary>
    /// <param name="command">The setup update command.</param>
    /// <returns>An accepted result with canonical setup, or a rejected result with field NAME only.</returns>
    public static ProjectCommandValidationResult Validate(UpdateProjectSetup command)
    {
        ArgumentNullException.ThrowIfNull(command);

        ProjectCommandValidationResult common = ValidateCommon(command);
        if (!common.IsAccepted)
        {
            return common;
        }

        ProjectSetup? setup = command.Setup;
        if (setup is null)
        {
            return ProjectCommandValidationResult.Rejected(ProjectResultCode.ValidationFailed, "setup");
        }

        ProjectSetup? canonical = CanonicalizeSetup(setup, out string? rejectedField);
        return canonical is null
            ? ProjectCommandValidationResult.Rejected(ProjectResultCode.ValidationFailed, rejectedField)
            : ProjectCommandValidationResult.AcceptedSetup(canonical, ComputeUpdateProjectSetupFingerprint(canonical));
    }

    /// <summary>
    /// Validates an <see cref="ArchiveProject"/> command and computes its canonical idempotency
    /// fingerprint.
    /// </summary>
    /// <param name="command">The archive command.</param>
    /// <returns>An accepted or rejected validation result.</returns>
    public static ProjectCommandValidationResult Validate(ArchiveProject command)
    {
        ArgumentNullException.ThrowIfNull(command);

        ProjectCommandValidationResult common = ValidateCommon(command);
        return common.IsAccepted
            ? ProjectCommandValidationResult.AcceptedArchive(ComputeArchiveProjectFingerprint())
            : common;
    }

    /// <summary>
    /// Computes the canonical idempotency fingerprint for a <see cref="CreateProject"/> command using
    /// the Story 1.3 canonical hasher semantics over the spine's equivalence field list.
    /// </summary>
    /// <param name="canonicalDisplayName">The canonical (trimmed) project display name.</param>
    /// <returns>A <c>sha256:</c>-prefixed lowercase hex digest.</returns>
    internal static string ComputeIdempotencyFingerprint(string canonicalDisplayName)
    {
        ArgumentNullException.ThrowIfNull(canonicalDisplayName);

        // Mirror the spine equivalence list order (ordinal-ascending): project_metadata.display_name,
        // request_schema_version. Lines match the Client hasher's canonical line shape so the two
        // surfaces agree on equivalence.
        string[] lines =
        [
            "operation=CreateProject",
            "field=project_metadata.display_name;present=true;value=s:" + Escape(canonicalDisplayName),
            "field=request_schema_version;present=true;value=s:" + Escape(RequestSchemaVersion),
        ];

        string canonical = string.Join('\n', lines);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return "sha256:" + Convert.ToHexString(digest).ToLowerInvariant();
    }

    /// <summary>Computes the canonical setup-update idempotency fingerprint.</summary>
    /// <param name="setup">The canonical setup.</param>
    /// <returns>A <c>sha256:</c>-prefixed lowercase hex digest.</returns>
    internal static string ComputeUpdateProjectSetupFingerprint(ProjectSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

        string[] lines =
        [
            "operation=UpdateProjectSetup",
            "field=project_setup.conversation_start_defaults.linked_source_policy;present="
                + (setup.ConversationStartDefaults is not null ? "true" : "false")
                + ";value="
                + (setup.ConversationStartDefaults is null
                    ? "omitted"
                    : "s:" + Escape(ToWireValue(setup.ConversationStartDefaults.LinkedSourcePolicy))),
            "field=project_setup.excluded_source_kinds;present=true;value=" + FormatEnumList(setup.ExcludedSourceKinds),
            "field=project_setup.goals;present=true;value=" + FormatStringList(setup.Goals),
            "field=project_setup.preferred_source_kinds;present=true;value=" + FormatEnumList(setup.PreferredSourceKinds),
            "field=project_setup.user_instructions;present=true;value=" + FormatStringList(setup.UserInstructions),
            "field=request_schema_version;present=true;value=s:" + Escape(RequestSchemaVersion),
        ];

        string canonical = string.Join('\n', lines);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return "sha256:" + Convert.ToHexString(digest).ToLowerInvariant();
    }

    /// <summary>Computes the canonical archive idempotency fingerprint.</summary>
    /// <returns>A <c>sha256:</c>-prefixed lowercase hex digest.</returns>
    internal static string ComputeArchiveProjectFingerprint()
    {
        string[] lines =
        [
            "operation=ArchiveProject",
            "field=archive_intent;present=true;value=s:archive",
            "field=request_schema_version;present=true;value=s:" + Escape(RequestSchemaVersion),
        ];

        string canonical = string.Join('\n', lines);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return "sha256:" + Convert.ToHexString(digest).ToLowerInvariant();
    }

    // Escapes the same control/separator characters the Client hasher escapes so the recomputed
    // fingerprint stays byte-identical to the cross-surface contract.
    private static string Escape(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char character in value)
        {
            builder.Append(character switch
            {
                '\\' => "\\\\",
                '\t' => "\\t",
                '\r' => "\\r",
                '\n' => "\\n",
                ';' => "\\;",
                '=' => "\\=",
                _ when char.IsControl(character) =>
                    "\\u" + ((int)character).ToString("x4", CultureInfo.InvariantCulture),
                _ => character.ToString(),
            });
        }

        return builder.ToString();
    }

    private static bool IsSafeOptionalMetadata(string? value, int maxLength)
        => string.IsNullOrWhiteSpace(value) || (value.Trim().Length <= maxLength && IsSafeMetadata(value));

    private static ProjectCommandValidationResult ValidateCommon(IProjectCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.TenantId))
        {
            return ProjectCommandValidationResult.Rejected(ProjectResultCode.Unauthorized, nameof(command.TenantId));
        }

        if (command.ProjectId is null || string.IsNullOrWhiteSpace(command.ProjectId.Value))
        {
            return ProjectCommandValidationResult.Rejected(ProjectResultCode.ValidationFailed, nameof(command.ProjectId));
        }

        if (!IsSafeEnvelopeIdentifier(command.ActorPrincipalId)
            || !IsSafeEnvelopeIdentifier(command.CorrelationId)
            || !IsSafeEnvelopeIdentifier(command.TaskId)
            || !IsSafeEnvelopeIdentifier(command.IdempotencyKey))
        {
            return ProjectCommandValidationResult.Rejected(ProjectResultCode.ValidationFailed, "envelope");
        }

        return ProjectCommandValidationResult.AcceptedArchive("sha256:common-validation");
    }

    private static ProjectSetup? CanonicalizeSetup(ProjectSetup setup, out string? rejectedField)
    {
        rejectedField = null;

        string[]? goals = CanonicalizeTextList(setup.Goals, "setup.goals", out rejectedField);
        if (goals is null)
        {
            return null;
        }

        string[]? instructions = CanonicalizeTextList(setup.UserInstructions, "setup.userInstructions", out rejectedField);
        if (instructions is null)
        {
            return null;
        }

        ProjectContextSourceKind[]? preferred = CanonicalizeSourceKinds(
            setup.PreferredSourceKinds,
            "setup.preferredSourceKinds",
            out rejectedField);
        if (preferred is null)
        {
            return null;
        }

        ProjectContextSourceKind[]? excluded = CanonicalizeSourceKinds(
            setup.ExcludedSourceKinds,
            "setup.excludedSourceKinds",
            out rejectedField);
        if (excluded is null)
        {
            return null;
        }

        if (setup.ConversationStartDefaults is not null
            && !Enum.IsDefined(setup.ConversationStartDefaults.LinkedSourcePolicy))
        {
            rejectedField = "setup.conversationStartDefaults.linkedSourcePolicy";
            return null;
        }

        return new ProjectSetup(
            goals,
            instructions,
            preferred,
            excluded,
            setup.ConversationStartDefaults);
    }

    private static string[]? CanonicalizeTextList(IReadOnlyList<string>? values, string field, out string? rejectedField)
    {
        rejectedField = null;
        if (values is null || values.Count > MaxSetupTextItems)
        {
            rejectedField = field;
            return null;
        }

        List<string> canonical = new(values.Count);
        foreach (string? value in values)
        {
            if (string.IsNullOrWhiteSpace(value)
                || value.Trim().Length > MaxSetupTextLength
                || !IsSafeMetadata(value))
            {
                rejectedField = field;
                return null;
            }

            canonical.Add(value.Trim());
        }

        return canonical.ToArray();
    }

    private static ProjectContextSourceKind[]? CanonicalizeSourceKinds(
        IReadOnlyList<ProjectContextSourceKind>? values,
        string field,
        out string? rejectedField)
    {
        rejectedField = null;
        if (values is null || values.Count > MaxSetupSourceKinds)
        {
            rejectedField = field;
            return null;
        }

        ProjectContextSourceKind[] canonical = values.ToArray();
        if (canonical.Any(static value => !Enum.IsDefined(value)))
        {
            rejectedField = field;
            return null;
        }

        return canonical;
    }

    private static string FormatStringList(IReadOnlyList<string> values)
        => "j:" + JsonSerializer.Serialize(values, CanonicalJsonOptions);

    private static string FormatEnumList(IReadOnlyList<ProjectContextSourceKind> values)
        => "j:[" + string.Join(",", values.Select(static value => "\"" + Escape(ToWireValue(value)) + "\"")) + "]";

    private static string ToWireValue(ProjectContextSourceKind value)
        => value switch
        {
            ProjectContextSourceKind.Conversation => "conversation",
            ProjectContextSourceKind.ProjectFolder => "projectFolder",
            ProjectContextSourceKind.FileReference => "fileReference",
            ProjectContextSourceKind.Memory => "memory",
            _ => value.ToString(),
        };

    private static string ToWireValue(LinkedSourcePolicy value)
        => value switch
        {
            LinkedSourcePolicy.None => "none",
            LinkedSourcePolicy.ProjectsOwnedMetadataOnly => "projectsOwnedMetadataOnly",
            LinkedSourcePolicy.AuthorizedReferences => "authorizedReferences",
            _ => value.ToString(),
        };

    private static bool IsSafeMetadata(string value)
    {
        // Normalize before the forbidden-term scan so confusables (NFD-decomposed combiners,
        // invisible format characters) cannot bypass the blocklist.
        string trimmed = value.Trim().Normalize(NormalizationForm.FormC);
        if (trimmed.Any(c => char.IsControl(c) || IsInvisibleFormatChar(c)))
        {
            return false;
        }

        string canonical = trimmed.ToLower(CultureInfo.InvariantCulture);
        return !ForbiddenMetadataSubstrings.Any(term => canonical.Contains(term, StringComparison.Ordinal));
    }

    private static bool IsSafeEnvelopeIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (char c in value)
        {
            if (c == '\r' || c == '\n' || char.IsControl(c))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsInvisibleFormatChar(char c)
        => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format
            || c == '​' // zero-width space
            || c == '‌' // zero-width non-joiner
            || c == '‍' // zero-width joiner
            || c == '﻿'; // BOM / zero-width no-break space
}
