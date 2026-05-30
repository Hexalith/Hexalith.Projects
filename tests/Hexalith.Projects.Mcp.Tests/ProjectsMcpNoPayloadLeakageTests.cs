// <copyright file="ProjectsMcpNoPayloadLeakageTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp.Tests;

using System.Text.Json;

using Hexalith.Projects.Mcp;

using Shouldly;

using Xunit;

public sealed class ProjectsMcpNoPayloadLeakageTests
{
    [Fact]
    public void Mcp_Result_Dtos_Do_Not_Serialize_Payload_Bearing_Fields()
    {
        string json = JsonSerializer.Serialize(new ProjectsMcpAuditTimelineItem(
            "project-1",
            "audit-1",
            "archive",
            DateTimeOffset.UnixEpoch,
            "corr-1",
            "task-1",
            "file",
            "file-1",
            "archived",
            "server-derived tenant",
            "Safe audit evidence.",
            PayloadExcluded: true));

        json.ShouldNotContain("idempotency", Case.Insensitive);
        json.ShouldNotContain("commandBody", Case.Insensitive);
        json.ShouldNotContain("problemDetails", Case.Insensitive);
        json.ShouldNotContain("transcript", Case.Insensitive);
        json.ShouldNotContain("filePath", Case.Insensitive);
        json.ShouldNotContain("memoryBody", Case.Insensitive);
    }
}
