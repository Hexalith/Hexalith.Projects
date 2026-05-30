// <copyright file="RejectionEventTaxonomyTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Events;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Projects.Contracts.Events;
using Hexalith.Projects.Contracts.Identifiers;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>
/// Tier-1 tests for the rejection-event taxonomy (AR-6, FS-4): every rejection implements
/// <see cref="IRejectionEvent"/>, carries the canonical reason from the shared vocabulary, and is
/// metadata-only. Pure: no infrastructure.
/// </summary>
public sealed class RejectionEventTaxonomyTests
{
    private static readonly Type[] _rejectionEvents =
    [
        typeof(ProjectCreationRejected),
        typeof(ProjectSetupUpdateRejected),
        typeof(ProjectArchiveRejected),
        typeof(ProjectRestoreRejected),
        typeof(ProjectReferenceLinkRejected),
        typeof(ProjectReferenceUnlinkRejected),
        typeof(ProjectResolutionConfirmationRejected),
    ];

    public static IEnumerable<object[]> RejectionEventTypes()
        => _rejectionEvents.Select(t => new object[] { t });

    [Fact]
    public void AllSevenRejectionEventsAreDefined()
        => _rejectionEvents.Length.ShouldBe(7);

    [Theory]
    [MemberData(nameof(RejectionEventTypes))]
    public void RejectionEventImplementsIRejectionEvent(Type rejectionType)
        => typeof(IRejectionEvent).IsAssignableFrom(rejectionType).ShouldBeTrue();

    [Theory]
    [MemberData(nameof(RejectionEventTypes))]
    public void RejectionEventIsSealedRecord(Type rejectionType)
    {
        ArgumentNullException.ThrowIfNull(rejectionType);
        rejectionType.IsSealed.ShouldBeTrue();

        // Records expose a compiler-generated EqualityContract property.
        rejectionType
            .GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(RejectionEventTypes))]
    public void RejectionEventCarriesSharedVocabularyReasonCode(Type rejectionType)
    {
        ArgumentNullException.ThrowIfNull(rejectionType);
        PropertyInfo? reason = rejectionType.GetProperty("Reason");
        reason.ShouldNotBeNull();
        reason!.PropertyType.ShouldBe(typeof(ReferenceState));
    }

    [Theory]
    [MemberData(nameof(RejectionEventTypes))]
    public void RejectionEventNameIsPastTenseWithoutEventSuffix(Type rejectionType)
    {
        ArgumentNullException.ThrowIfNull(rejectionType);
        rejectionType.Name.ShouldEndWith("Rejected");
        rejectionType.Name.ShouldNotEndWith("Event");
    }

    [Theory]
    [MemberData(nameof(RejectionEventTypes))]
    public void RejectionEventIsMetadataOnly(Type rejectionType)
    {
        ArgumentNullException.ThrowIfNull(rejectionType);

        // Allow only safe metadata member types: opaque ids, tenant, reason code, safe field name, correlation.
        HashSet<Type> allowed =
        [
            typeof(ProjectId),
            typeof(string),
            typeof(ReferenceState),
        ];

        foreach (PropertyInfo property in rejectionType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.Name == "EqualityContract")
            {
                continue;
            }

            allowed.ShouldContain(
                property.PropertyType,
                $"{rejectionType.Name}.{property.Name} has non-metadata type {property.PropertyType.Name}.");
        }
    }

    [Fact]
    public void ProjectCreationRejectedConstructsWithMetadataOnly()
    {
        var rejection = new ProjectCreationRejected("acme", ReferenceState.InvalidReference, "Title", "corr-1");
        rejection.TenantId.ShouldBe("acme");
        rejection.Reason.ShouldBe(ReferenceState.InvalidReference);
        rejection.RejectedField.ShouldBe("Title");
        rejection.CorrelationId.ShouldBe("corr-1");
        rejection.ShouldBeAssignableTo<IRejectionEvent>();
    }

    [Fact]
    public void ProjectReferenceLinkRejectedCarriesReferenceMetadata()
    {
        var projectId = new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        var rejection = new ProjectReferenceLinkRejected(
            projectId,
            "acme",
            "conversation",
            "01HZCONVERSATION0000000000",
            ReferenceState.TenantMismatch);

        rejection.ProjectId.ShouldBe(projectId);
        rejection.ReferenceKind.ShouldBe("conversation");
        rejection.ReferenceId.ShouldBe("01HZCONVERSATION0000000000");
        rejection.Reason.ShouldBe(ReferenceState.TenantMismatch);
    }
}
