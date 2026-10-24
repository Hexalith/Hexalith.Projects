// <copyright file="ConversationStartSetupResponse.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using Hexalith.Projects.Contracts.Models;

/// <summary>Supported Conversation-start value with owned collections and structural equality.</summary>
/// <param name="Setup">The bounded setup subset, or null when unavailable or denied.</param>
/// <param name="Snapshot">The AD-32 admission snapshot.</param>
public sealed record ConversationStartSetupResponse(
    ConversationStartSetup? Setup,
    ConversationStartAdmissionSnapshot Snapshot)
{
    private readonly ConversationStartSetup? _setup = CopySetup(Setup);
    private readonly ConversationStartAdmissionSnapshot _snapshot = CopySnapshot(Snapshot);

    /// <summary>Gets the bounded setup value, whose collections are immutable copies.</summary>
    public ConversationStartSetup? Setup
    {
        get => _setup;
        init => _setup = CopySetup(value);
    }

    /// <summary>Gets the admission snapshot, whose collections are immutable copies.</summary>
    public ConversationStartAdmissionSnapshot Snapshot
    {
        get => _snapshot;
        init => _snapshot = CopySnapshot(value);
    }

    /// <summary>Compares every scalar and ordered collection by value.</summary>
    /// <param name="other">The response to compare.</param>
    /// <returns>Whether the responses carry identical values.</returns>
    public bool Equals(ConversationStartSetupResponse? other)
        => other is not null
            && SetupsEqual(Setup, other.Setup)
            && Snapshot.ResponseState == other.Snapshot.ResponseState
            && Snapshot.AsOf == other.Snapshot.AsOf
            && Snapshot.ProjectVersion == other.Snapshot.ProjectVersion
            && Snapshot.Components.SequenceEqual(other.Snapshot.Components)
            && Snapshot.RecoveryActions.SequenceEqual(other.Snapshot.RecoveryActions);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Setup is not null);
        if (Setup is { } setup)
        {
            hash.Add(setup.ProjectId);
            hash.Add(setup.Lifecycle);
            AddSequence(ref hash, setup.Goals);
            AddSequence(ref hash, setup.UserInstructions);
            AddSequence(ref hash, setup.PreferredSourceKinds);
            AddSequence(ref hash, setup.ExcludedSourceKinds);
            hash.Add(setup.LinkedSourcePolicy);
            hash.Add(setup.ObservedAt);
            hash.Add(setup.Freshness);
        }

        hash.Add(Snapshot.ResponseState);
        hash.Add(Snapshot.AsOf);
        hash.Add(Snapshot.ProjectVersion);
        AddSequence(ref hash, Snapshot.Components);
        AddSequence(ref hash, Snapshot.RecoveryActions);
        return hash.ToHashCode();
    }

    private static ConversationStartSetup? CopySetup(ConversationStartSetup? setup)
        => setup is null ? null : setup with
        {
            Goals = Array.AsReadOnly(setup.Goals.ToArray()),
            UserInstructions = Array.AsReadOnly(setup.UserInstructions.ToArray()),
            PreferredSourceKinds = Array.AsReadOnly(setup.PreferredSourceKinds.ToArray()),
            ExcludedSourceKinds = Array.AsReadOnly(setup.ExcludedSourceKinds.ToArray()),
        };

    private static ConversationStartAdmissionSnapshot CopySnapshot(ConversationStartAdmissionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot with
        {
            Components = Array.AsReadOnly(snapshot.Components.ToArray()),
            RecoveryActions = Array.AsReadOnly(snapshot.RecoveryActions.ToArray()),
        };
    }

    private static bool SetupsEqual(ConversationStartSetup? left, ConversationStartSetup? right)
        => ReferenceEquals(left, right)
            || left is not null && right is not null
                && left.ProjectId == right.ProjectId
                && left.Lifecycle == right.Lifecycle
                && left.Goals.SequenceEqual(right.Goals)
                && left.UserInstructions.SequenceEqual(right.UserInstructions)
                && left.PreferredSourceKinds.SequenceEqual(right.PreferredSourceKinds)
                && left.ExcludedSourceKinds.SequenceEqual(right.ExcludedSourceKinds)
                && left.LinkedSourcePolicy == right.LinkedSourcePolicy
                && left.ObservedAt == right.ObservedAt
                && left.Freshness == right.Freshness;

    private static void AddSequence<T>(ref HashCode hash, IReadOnlyList<T> values)
    {
        hash.Add(values.Count);
        foreach (T value in values)
        {
            hash.Add(value);
        }
    }
}
