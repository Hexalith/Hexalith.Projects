// <copyright file="AdmissionSnapshotTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.Queries;

using System;
using System.Text.Json;

using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

using Shouldly;

using Xunit;

/// <summary>Contract tests for the shared AD-32 snapshot vocabulary and additive context queries.</summary>
public sealed class AdmissionSnapshotTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AdmissionSnapshot_RoundTripsAndKeepsConversationStartWireShape()
    {
        AdmissionSnapshot snapshot = new(
            AdmissionResponseState.Complete,
            new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero),
            4,
            [new AdmissionComponent("Project", true, EvidenceFreshnessState.Current, "current")],
            [AdmissionRecoveryAction.None]);
        ConversationStartSetupResponse conversationStart = new(
            ConversationStartSetup.Empty("project-1", ProjectLifecycle.Active, snapshot.AsOf, ProjectContextFreshness.Fresh),
            snapshot);

        string json = JsonSerializer.Serialize(conversationStart, WebOptions);
        ConversationStartSetupResponse roundTripped = JsonSerializer.Deserialize<ConversationStartSetupResponse>(json, WebOptions)!;

        json.ShouldContain("\"responseState\":\"Complete\"");
        json.ShouldContain("\"asOf\"");
        json.ShouldContain("\"projectVersion\":4");
        json.ShouldContain("\"components\"");
        json.ShouldContain("\"recoveryActions\"");
        json.ShouldNotContain("tenantId");
        roundTripped.Snapshot.ResponseState.ShouldBe(AdmissionResponseState.Complete);
        roundTripped.Snapshot.ProjectVersion.ShouldBe(4);
        roundTripped.Setup.ShouldNotBeNull();
    }

    [Fact]
    public void GetProjectContextQuery_RoundTripsProjectId()
    {
        string json = JsonSerializer.Serialize(new GetProjectContextQuery("01HZ9K8YQ3W6V2N4R7T5P0X1AB"), WebOptions);
        GetProjectContextQuery parsed = JsonSerializer.Deserialize<GetProjectContextQuery>(json, WebOptions)!;
        parsed.ProjectId.ShouldBe("01HZ9K8YQ3W6V2N4R7T5P0X1AB");
        json.ShouldNotContain("tenantId");
    }

    [Fact]
    public void RefreshAndExplainQueries_RemainAdditiveSingletons()
    {
        JsonSerializer.Deserialize<RefreshProjectContextQuery>(
            JsonSerializer.Serialize(new RefreshProjectContextQuery("p1"), WebOptions),
            WebOptions)!.ProjectId.ShouldBe("p1");
        JsonSerializer.Deserialize<ExplainContextSelectionQuery>(
            JsonSerializer.Serialize(new ExplainContextSelectionQuery("p1"), WebOptions),
            WebOptions)!.ProjectId.ShouldBe("p1");
    }

    [Fact]
    public void ProjectContextReadResponse_DoesNotEmitTenantAuthority()
    {
        ProjectContextReadResponse response = new(
            "01HZ9K8YQ3W6V2N4R7T5P0X1AB",
            ProjectLifecycle.Active,
            ProjectSetup.Empty,
            ProjectFolder: null,
            Conversations: [],
            FileReferences: [],
            MemoryReferences: [],
            Excluded: [],
            new AdmissionSnapshot(AdmissionResponseState.Complete, DateTimeOffset.UnixEpoch, 1, [], [AdmissionRecoveryAction.None]));

        string json = JsonSerializer.Serialize(response, WebOptions);
        json.ShouldNotContain("tenantId");
        json.ShouldNotContain("TenantId");
        json.ShouldContain("\"responseState\":\"Complete\"");
    }
}
