// <copyright file="ProjectVocabularyDescriptors.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System;
using System.Collections.Generic;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Single, FrontComposer-consumable lookup that supplies presentation metadata (stable code, display
/// label, accessible name, severity) for <b>every</b> member of <b>every</b> shared-vocabulary enum
/// (UX-DR5). This is the only place these labels and severities are declared — never duplicate them
/// with parallel tables or magic strings.
/// </summary>
/// <remarks>
/// <para>
/// The stable <see cref="VocabularyDescriptor.Code"/> is the enum member name. The
/// <see cref="VocabularyDescriptor.Severity"/> is read from each member's
/// <see cref="ProjectionBadgeAttribute"/> via reflection, so the badge slot is declared once (on the
/// enum) and never drifts from a parallel mapping. Display labels and accessible names are supplied
/// from the static tables below.
/// </para>
/// <para>
/// Netstandard2.0-safe: uses only reflection APIs and collections available on netstandard2.0 so the
/// surface stays consumable by FrontComposer source generators. A Tier-1 test enforces total coverage
/// (no enum member without a descriptor) and name-based codes.
/// </para>
/// </remarks>
public static class ProjectVocabularyDescriptors
{
    private static readonly IReadOnlyDictionary<ProjectLifecycle, VocabularyDescriptor> _lifecycle =
        BuildDescriptors(
            new Dictionary<ProjectLifecycle, (string DisplayLabel, string AccessibleName)>
            {
                [ProjectLifecycle.Active] = ("Active", "Project is active"),
                [ProjectLifecycle.Archived] = ("Archived", "Project is archived"),
            });

    private static readonly IReadOnlyDictionary<ReferenceState, VocabularyDescriptor> _referenceStates =
        BuildDescriptors(
            new Dictionary<ReferenceState, (string DisplayLabel, string AccessibleName)>
            {
                [ReferenceState.Included] = ("Included", "Reference is included"),
                [ReferenceState.Excluded] = ("Excluded", "Reference is excluded"),
                [ReferenceState.Unauthorized] = ("Unauthorized", "Reference access is unauthorized"),
                [ReferenceState.Unavailable] = ("Unavailable", "Reference is unavailable"),
                [ReferenceState.Stale] = ("Stale", "Reference is stale"),
                [ReferenceState.Archived] = ("Archived", "Referenced resource is archived"),
                [ReferenceState.Ambiguous] = ("Ambiguous", "Reference is ambiguous"),
                [ReferenceState.TenantMismatch] = ("Tenant mismatch", "Reference belongs to a different tenant"),
                [ReferenceState.Conflict] = ("Conflict", "Reference is in conflict"),
                [ReferenceState.InvalidReference] = ("Invalid reference", "Reference is invalid"),
            });

    private static readonly IReadOnlyDictionary<ResolutionResult, VocabularyDescriptor> _resolutionResults =
        BuildDescriptors(
            new Dictionary<ResolutionResult, (string DisplayLabel, string AccessibleName)>
            {
                [ResolutionResult.NoMatch] = ("No match", "No matching project found"),
                [ResolutionResult.SingleCandidate] = ("Single candidate", "One matching project found"),
                [ResolutionResult.MultipleCandidates] = ("Multiple candidates", "Multiple matching projects found"),
            });

    private static readonly IReadOnlyDictionary<ProjectReasonCode, VocabularyDescriptor> _reasonCodes =
        BuildDescriptors(
            new Dictionary<ProjectReasonCode, (string DisplayLabel, string AccessibleName)>
            {
                [ProjectReasonCode.ConversationLinked] = ("Conversation linked", "A conversation was linked"),
                [ProjectReasonCode.ProjectFolderMatched] = ("Project folder matched", "The project folder matched"),
                [ProjectReasonCode.FileReferenceMatched] = ("File reference matched", "A file reference matched"),
                [ProjectReasonCode.MemoryMatched] = ("Memory matched", "A memory matched"),
                [ProjectReasonCode.MetadataMatched] = ("Metadata matched", "Project metadata matched"),
            });

    /// <summary>
    /// Gets the descriptors for every <see cref="ProjectLifecycle"/> member, keyed by member.
    /// </summary>
    public static IReadOnlyDictionary<ProjectLifecycle, VocabularyDescriptor> Lifecycle => _lifecycle;

    /// <summary>
    /// Gets the descriptors for every <see cref="ReferenceState"/> member, keyed by member.
    /// </summary>
    public static IReadOnlyDictionary<ReferenceState, VocabularyDescriptor> ReferenceStates => _referenceStates;

    /// <summary>
    /// Gets the descriptors for every <see cref="ResolutionResult"/> member, keyed by member.
    /// </summary>
    public static IReadOnlyDictionary<ResolutionResult, VocabularyDescriptor> ResolutionResults => _resolutionResults;

    /// <summary>
    /// Gets the descriptors for every <see cref="ProjectReasonCode"/> member, keyed by member.
    /// </summary>
    public static IReadOnlyDictionary<ProjectReasonCode, VocabularyDescriptor> ReasonCodes => _reasonCodes;

    /// <summary>
    /// Gets the presentation descriptor for a <see cref="ProjectLifecycle"/> member.
    /// </summary>
    /// <param name="value">The enum member.</param>
    /// <returns>The descriptor for <paramref name="value"/>.</returns>
    public static VocabularyDescriptor Describe(ProjectLifecycle value) => _lifecycle[value];

    /// <summary>
    /// Gets the presentation descriptor for a <see cref="ReferenceState"/> member.
    /// </summary>
    /// <param name="value">The enum member.</param>
    /// <returns>The descriptor for <paramref name="value"/>.</returns>
    public static VocabularyDescriptor Describe(ReferenceState value) => _referenceStates[value];

    /// <summary>
    /// Gets the presentation descriptor for a <see cref="ResolutionResult"/> member.
    /// </summary>
    /// <param name="value">The enum member.</param>
    /// <returns>The descriptor for <paramref name="value"/>.</returns>
    public static VocabularyDescriptor Describe(ResolutionResult value) => _resolutionResults[value];

    /// <summary>
    /// Gets the presentation descriptor for a <see cref="ProjectReasonCode"/> member.
    /// </summary>
    /// <param name="value">The enum member.</param>
    /// <returns>The descriptor for <paramref name="value"/>.</returns>
    public static VocabularyDescriptor Describe(ProjectReasonCode value) => _reasonCodes[value];

    /// <summary>
    /// Reads the <see cref="BadgeSlot"/> declared by the <see cref="ProjectionBadgeAttribute"/> on an
    /// enum member.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The enum member.</param>
    /// <returns>The badge slot declared for <paramref name="value"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the member has no <see cref="ProjectionBadgeAttribute"/>.</exception>
    public static BadgeSlot GetSeverity<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string name = value.ToString();
        FieldInfo? field = typeof(TEnum).GetField(name);
        ProjectionBadgeAttribute? badge = field?.GetCustomAttribute<ProjectionBadgeAttribute>();
        return badge is null
            ? throw new InvalidOperationException(
                $"Enum member '{typeof(TEnum).Name}.{name}' is missing a [ProjectionBadge] attribute.")
            : badge.Slot;
    }

    private static IReadOnlyDictionary<TEnum, VocabularyDescriptor> BuildDescriptors<TEnum>(
        IReadOnlyDictionary<TEnum, (string DisplayLabel, string AccessibleName)> labels)
        where TEnum : struct, Enum
    {
        TEnum[] members = (TEnum[])Enum.GetValues(typeof(TEnum));
        var descriptors = new Dictionary<TEnum, VocabularyDescriptor>(members.Length);
        foreach (TEnum member in members)
        {
            if (!labels.TryGetValue(member, out (string DisplayLabel, string AccessibleName) label))
            {
                throw new InvalidOperationException(
                    $"Enum member '{typeof(TEnum).Name}.{member}' has no display/accessible-name metadata. " +
                    "Every shared-vocabulary member must have a descriptor.");
            }

            descriptors[member] = new VocabularyDescriptor(
                member.ToString(),
                label.DisplayLabel,
                label.AccessibleName,
                GetSeverity(member));
        }

        return descriptors;
    }
}
