// <copyright file="PayloadClassification.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Models;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Machine-usable payload-classification allowlist / denylist (FS-1, NFR-2). This is the
/// <b>single source of truth</b> for the FS-2 <c>NoPayloadLeakage</c> harness built in Story 1.4: every
/// leakage test asserts against the collections below, and ProjectContext assembly (Epic 3) derives its
/// safe-field set from them.
/// </summary>
/// <remarks>
/// <para>
/// The human-readable companion is <c>docs/payload-taxonomy.md</c> (relative to the repository root).
/// Both forms must stay in sync; the doc carries the rationale, this type carries the assertions.
/// </para>
/// <para>
/// <see cref="SafeFields"/> enumerates reference-only / metadata-safe field categories that MAY appear
/// in events, logs, DTOs, and audit records. <see cref="ForbiddenContent"/> enumerates sibling-owned
/// content categories that MUST NEVER appear on the wire. Netstandard2.0-safe: only string collections.
/// </para>
/// </remarks>
public static class PayloadClassification
{
    /// <summary>
    /// The relative path (from the repository root) to the human-readable taxonomy and rationale.
    /// </summary>
    public const string TaxonomyDocumentPath = "docs/payload-taxonomy.md";

    /// <summary>
    /// A short statement of the source-of-truth relationship, embedded so the dependency is discoverable
    /// from code as well as from the doc.
    /// </summary>
    public const string SourceOfTruthStatement =
        "FS-2 NoPayloadLeakage harness (Story 1.4) is built against this allowlist.";

    private static readonly IReadOnlyList<string> _safeFields = new[]
    {
        "OpaqueId",
        "ETag",
        "Version",
        "TenantId",
        "ReferenceKind",
        "OwnerContext",
        "Timestamp",
        "LifecycleState",
        "InclusionState",
        "ResolutionState",
        "SetupPreference",
        "ReasonCode",
        "CorrelationId",
        "CausationId",
        "AuditId",
        "UiFeedbackCode",
        "UiProjectionDescriptor",
        "TransientTraceMetadata",
    };

    private static readonly IReadOnlyList<string> _forbiddenContent = new[]
    {
        "ConversationTranscriptText",
        "FileContents",
        "MemoryBody",
        "RawPrompt",
        "Secret",
        "RawToken",
        "FullCommandBody",
        "UnrestrictedFilePath",
        "LocalFilePath",
        "SensitiveFolderName",
    };

    /// <summary>
    /// Gets the allowlist of reference-only / metadata-safe field categories that MAY appear on the wire
    /// (opaque IDs, ETags/versions, tenant ID, reference kind/owner-context, timestamps,
    /// lifecycle/inclusion/resolution states, bounded setup preferences, reason codes,
    /// correlation/causation/audit IDs, and transient trace metadata).
    /// </summary>
    public static IReadOnlyList<string> SafeFields => _safeFields;

    /// <summary>
    /// Gets the denylist of forbidden sibling-owned content categories that MUST NEVER appear on the wire
    /// (conversation transcript text, file contents, memory bodies, raw prompts, secrets, raw tokens,
    /// full command bodies, unrestricted/local file paths, and sensitive folder names).
    /// </summary>
    public static IReadOnlyList<string> ForbiddenContent => _forbiddenContent;

    /// <summary>
    /// Determines whether a field category is on the safe allowlist.
    /// </summary>
    /// <param name="fieldCategory">The field category to test (case-sensitive).</param>
    /// <returns><see langword="true"/> when the category is explicitly allowed; otherwise <see langword="false"/>.</returns>
    public static bool IsSafe(string fieldCategory)
    {
        ArgumentNullException.ThrowIfNull(fieldCategory);
        return _safeFields.Contains(fieldCategory);
    }

    /// <summary>
    /// Determines whether a content category is on the forbidden denylist.
    /// </summary>
    /// <param name="contentCategory">The content category to test (case-sensitive).</param>
    /// <returns><see langword="true"/> when the category is explicitly forbidden; otherwise <see langword="false"/>.</returns>
    public static bool IsForbidden(string contentCategory)
    {
        ArgumentNullException.ThrowIfNull(contentCategory);
        return _forbiddenContent.Contains(contentCategory);
    }
}
