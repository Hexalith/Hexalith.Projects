// <copyright file="ProjectsMcpWarningScan.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp;

/// <summary>
/// Captures the complete result of one deterministic MCP warning scan.
/// </summary>
/// <param name="Warnings">All ordered warning rows before an emitted-row limit is applied.</param>
/// <param name="ScannedProjectCount">The number of visible projects diagnosed.</param>
/// <param name="DiagnosticUnavailable">The number of scanned diagnostics that were unavailable.</param>
internal sealed record ProjectsMcpWarningScan(
    IReadOnlyList<ProjectsMcpWarningQueueItem> Warnings,
    int ScannedProjectCount,
    int DiagnosticUnavailable);
