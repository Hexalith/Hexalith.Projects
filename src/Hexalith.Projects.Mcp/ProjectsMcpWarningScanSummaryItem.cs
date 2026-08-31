// <copyright file="ProjectsMcpWarningScanSummaryItem.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp;

/// <summary>
/// Safe MCP summary for one deterministic warning diagnostic scan.
/// </summary>
/// <param name="ScannedProjectCount">The number of visible projects diagnosed.</param>
/// <param name="DiagnosticUnavailable">The number of scanned diagnostics that were unavailable.</param>
/// <param name="TenantScope">The server-derived tenant scope label.</param>
/// <param name="ShortExplanation">A short metadata-only explanation.</param>
/// <param name="PayloadExcluded">Whether payload categories are excluded.</param>
public sealed record ProjectsMcpWarningScanSummaryItem(
    int ScannedProjectCount,
    int DiagnosticUnavailable,
    string TenantScope,
    string ShortExplanation,
    bool PayloadExcluded);
