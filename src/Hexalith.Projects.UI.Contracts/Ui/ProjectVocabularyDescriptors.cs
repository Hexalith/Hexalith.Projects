// <copyright file="ProjectVocabularyDescriptors.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using System;
using System.Collections.Generic;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Single, FrontComposer-consumable lookup that supplies presentation metadata (stable code, display
/// label, accessible name, severity) for <b>every</b> member of <b>every</b> shared-vocabulary enum
/// (UX-DR5). This is the only place these labels and severities are declared — never duplicate them
/// with parallel tables or magic strings.
/// </summary>
/// <remarks>
/// <para>
/// The stable <see cref="VocabularyDescriptor.Code"/> is the enum member name. Display labels,
/// accessible names and the <see cref="VocabularyDescriptor.Severity"/> badge slot are declared once
/// per member in the explicit tables below. The vocabulary enums live in the dependency-light
/// <c>Hexalith.Projects.Contracts</c> package and carry no presentation attributes, so this UI-only
/// type is the single place their badge slots are declared. The context-assembly enums
/// (<see cref="ProjectContextAssemblyOutcome"/>, <see cref="ProjectContextFreshness"/> and
/// <see cref="ProjectContextInclusionCheck"/>) have no descriptors; their slots are declared in
/// private slot tables and exposed only through <see cref="GetSeverity{TEnum}(TEnum)"/>.
/// </para>
/// <para>
/// Tier-1 tests enforce total coverage (no enum member without a descriptor), name-based codes, and a
/// golden table that pins every member's slot to its pre-split <see cref="ProjectionBadgeAttribute"/>
/// value.
/// </para>
/// </remarks>
public static class ProjectVocabularyDescriptors
{
    private static readonly IReadOnlyDictionary<ProjectLifecycle, VocabularyDescriptor> _lifecycle =
        BuildDescriptors(
            new Dictionary<ProjectLifecycle, (string DisplayLabel, string AccessibleName, BadgeSlot Severity)>
            {
                [ProjectLifecycle.Active] = ("Active", "Project is active", BadgeSlot.Success),
                [ProjectLifecycle.Archived] = ("Archived", "Project is archived", BadgeSlot.Warning),
            });

    private static readonly IReadOnlyDictionary<ReferenceState, VocabularyDescriptor> _referenceStates =
        BuildDescriptors(
            new Dictionary<ReferenceState, (string DisplayLabel, string AccessibleName, BadgeSlot Severity)>
            {
                [ReferenceState.Pending] = ("Pending", "Reference is pending confirmation", BadgeSlot.Warning),
                [ReferenceState.Included] = ("Included", "Reference is included", BadgeSlot.Success),
                [ReferenceState.Excluded] = ("Excluded", "Reference is excluded", BadgeSlot.Neutral),
                [ReferenceState.Unauthorized] = ("Unauthorized", "Reference access is unauthorized", BadgeSlot.Danger),
                [ReferenceState.Unavailable] = ("Unavailable", "Reference is unavailable", BadgeSlot.Danger),
                [ReferenceState.Stale] = ("Stale", "Reference is stale", BadgeSlot.Warning),
                [ReferenceState.Archived] = ("Archived", "Referenced resource is archived", BadgeSlot.Warning),
                [ReferenceState.Ambiguous] = ("Ambiguous", "Reference is ambiguous", BadgeSlot.Warning),
                [ReferenceState.TenantMismatch] = ("Tenant mismatch", "Reference belongs to a different tenant", BadgeSlot.Danger),
                [ReferenceState.Conflict] = ("Conflict", "Reference is in conflict", BadgeSlot.Danger),
                [ReferenceState.InvalidReference] = ("Invalid reference", "Reference is invalid", BadgeSlot.Danger),
            });

    private static readonly IReadOnlyDictionary<ResolutionResult, VocabularyDescriptor> _resolutionResults =
        BuildDescriptors(
            new Dictionary<ResolutionResult, (string DisplayLabel, string AccessibleName, BadgeSlot Severity)>
            {
                [ResolutionResult.NoMatch] = ("No match", "No matching project found", BadgeSlot.Neutral),
                [ResolutionResult.SingleCandidate] = ("Single candidate", "One matching project found", BadgeSlot.Success),
                [ResolutionResult.MultipleCandidates] = ("Multiple candidates", "Multiple matching projects found", BadgeSlot.Warning),
            });

    private static readonly IReadOnlyDictionary<ProjectReasonCode, VocabularyDescriptor> _reasonCodes =
        BuildDescriptors(
            new Dictionary<ProjectReasonCode, (string DisplayLabel, string AccessibleName, BadgeSlot Severity)>
            {
                [ProjectReasonCode.ConversationLinked] = ("Conversation linked", "A conversation was linked", BadgeSlot.Info),
                [ProjectReasonCode.ProjectFolderMatched] = ("Project folder matched", "The project folder matched", BadgeSlot.Info),
                [ProjectReasonCode.FileReferenceMatched] = ("File reference matched", "A file reference matched", BadgeSlot.Info),
                [ProjectReasonCode.MemoryMatched] = ("Memory matched", "A memory matched", BadgeSlot.Info),
                [ProjectReasonCode.MetadataMatched] = ("Metadata matched", "Project metadata matched", BadgeSlot.Info),
            });

    private static readonly IReadOnlyDictionary<ProjectContextAssemblyOutcome, BadgeSlot> _assemblyOutcomeSlots =
        new Dictionary<ProjectContextAssemblyOutcome, BadgeSlot>
        {
            [ProjectContextAssemblyOutcome.Assembled] = BadgeSlot.Success,

            // Safe-denial 404 surface: never signal existence by colour.
            [ProjectContextAssemblyOutcome.ProjectUnavailable] = BadgeSlot.Neutral,
            [ProjectContextAssemblyOutcome.Unauthorized] = BadgeSlot.Danger,
        };

    private static readonly IReadOnlyDictionary<ProjectContextFreshness, BadgeSlot> _freshnessSlots =
        new Dictionary<ProjectContextFreshness, BadgeSlot>
        {
            [ProjectContextFreshness.Fresh] = BadgeSlot.Success,
            [ProjectContextFreshness.Stale] = BadgeSlot.Warning,
            [ProjectContextFreshness.Unavailable] = BadgeSlot.Danger,
            [ProjectContextFreshness.Unknown] = BadgeSlot.Neutral,
        };

    private static readonly IReadOnlyDictionary<ProjectContextInclusionCheck, BadgeSlot> _inclusionCheckSlots =
        new Dictionary<ProjectContextInclusionCheck, BadgeSlot>
        {
            [ProjectContextInclusionCheck.TenantAuthority] = BadgeSlot.Info,
            [ProjectContextInclusionCheck.ProjectVisibility] = BadgeSlot.Info,
            [ProjectContextInclusionCheck.ProjectLifecycle] = BadgeSlot.Info,
            [ProjectContextInclusionCheck.ReferenceAuthorization] = BadgeSlot.Info,
            [ProjectContextInclusionCheck.ReferenceLifecycle] = BadgeSlot.Info,
            [ProjectContextInclusionCheck.ReferenceFreshness] = BadgeSlot.Info,
            [ProjectContextInclusionCheck.ReferenceKindAllowlist] = BadgeSlot.Info,
        };

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
    /// Gets the <see cref="BadgeSlot"/> declared in the explicit descriptor or slot tables for a
    /// shared-vocabulary or context-assembly enum member.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The enum member.</param>
    /// <returns>The badge slot declared for <paramref name="value"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the member has no declared badge slot.</exception>
    public static BadgeSlot GetSeverity<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        BadgeSlot? slot = value switch
        {
            ProjectLifecycle member => Find(_lifecycle, member)?.Severity,
            ReferenceState member => Find(_referenceStates, member)?.Severity,
            ResolutionResult member => Find(_resolutionResults, member)?.Severity,
            ProjectReasonCode member => Find(_reasonCodes, member)?.Severity,
            ProjectContextAssemblyOutcome member => FindSlot(_assemblyOutcomeSlots, member),
            ProjectContextFreshness member => FindSlot(_freshnessSlots, member),
            ProjectContextInclusionCheck member => FindSlot(_inclusionCheckSlots, member),
            _ => null,
        };
        return slot ?? throw new InvalidOperationException(
            $"Enum member '{typeof(TEnum).Name}.{value}' has no declared badge severity slot.");
    }

    private static IReadOnlyDictionary<TEnum, VocabularyDescriptor> BuildDescriptors<TEnum>(
        IReadOnlyDictionary<TEnum, (string DisplayLabel, string AccessibleName, BadgeSlot Severity)> labels)
        where TEnum : struct, Enum
    {
        TEnum[] members = (TEnum[])Enum.GetValues(typeof(TEnum));
        var descriptors = new Dictionary<TEnum, VocabularyDescriptor>(members.Length);
        foreach (TEnum member in members)
        {
            if (!labels.TryGetValue(member, out (string DisplayLabel, string AccessibleName, BadgeSlot Severity) label))
            {
                throw new InvalidOperationException(
                    $"Enum member '{typeof(TEnum).Name}.{member}' has no display/accessible-name/severity metadata. " +
                    "Every shared-vocabulary member must have a descriptor.");
            }

            descriptors[member] = new VocabularyDescriptor(
                member.ToString(),
                label.DisplayLabel,
                label.AccessibleName,
                label.Severity);
        }

        return descriptors;
    }

    private static VocabularyDescriptor? Find<TEnum>(IReadOnlyDictionary<TEnum, VocabularyDescriptor> descriptors, TEnum member)
        where TEnum : struct, Enum
        => descriptors.TryGetValue(member, out VocabularyDescriptor? descriptor) ? descriptor : null;

    private static BadgeSlot? FindSlot<TEnum>(IReadOnlyDictionary<TEnum, BadgeSlot> slots, TEnum member)
        where TEnum : struct, Enum
        => slots.TryGetValue(member, out BadgeSlot slot) ? slot : null;
}
