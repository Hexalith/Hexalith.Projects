// <copyright file="ProjectsCliApplication.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Cli;

using System.Globalization;
using System.Text.Json;

using Hexalith.Projects.Client.Generated;

/// <summary>Generated-client backed Projects command application.</summary>
public sealed class ProjectsCliApplication(IClient client, TextWriter output, TextWriter error)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    private readonly IClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly TextWriter _output = output ?? throw new ArgumentNullException(nameof(output));
    private readonly TextWriter _error = error ?? throw new ArgumentNullException(nameof(error));

    /// <summary>Runs a Projects CLI invocation.</summary>
    public async Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken = default)
    {
        if (!ProjectsCliParser.TryParse(args, out ProjectsCliInvocation invocation, out string parseError))
        {
            await WriteErrorAsync(parseError).ConfigureAwait(false);
            return ProjectsCliExitCodes.Usage;
        }

        try
        {
            object result = invocation.Command switch
            {
                "list" => await ListAsync(cancellationToken).ConfigureAwait(false),
                "describe" or "inspect" => await DescribeAsync(Required(invocation, "project-id"), cancellationToken).ConfigureAwait(false),
                "audit" => await AuditAsync(Required(invocation, "project-id"), cancellationToken).ConfigureAwait(false),
                "validate" => await ValidateReferencesAsync(Required(invocation, "project-id"), cancellationToken).ConfigureAwait(false),
                "warnings" => await WarningsAsync(cancellationToken).ConfigureAwait(false),
                "dashboard" => await DashboardAsync(cancellationToken).ConfigureAwait(false),
                "diagnostic export" => await DiagnosticExportAsync(Required(invocation, "project-id"), cancellationToken).ConfigureAwait(false),
                "trace" => await TraceAsync(invocation, cancellationToken).ConfigureAwait(false),
                "dry-run" or "preview" => Preview(invocation),
                "archive" or "restore" or "relink" or "unlink" or "reevaluate" => await MutateAsync(invocation, cancellationToken).ConfigureAwait(false),
                _ => throw new ProjectsCliUsageException("unsupported_command"),
            };

            await WriteResultAsync(result).ConfigureAwait(false);
            return ProjectsCliExitCodes.Success;
        }
        catch (ProjectsCliUsageException ex)
        {
            await WriteErrorAsync(ex.SafeCode).ConfigureAwait(false);
            return ProjectsCliExitCodes.Usage;
        }
        catch (ProjectsCliValidationException ex)
        {
            await WriteErrorAsync(ex.SafeCode).ConfigureAwait(false);
            return ProjectsCliExitCodes.Validation;
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 400)
        {
            await WriteErrorAsync("validation_error").ConfigureAwait(false);
            return ProjectsCliExitCodes.Validation;
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 404 || ex.StatusCode == 403 || ex.StatusCode == 401)
        {
            await WriteErrorAsync("safe_denial").ConfigureAwait(false);
            return ProjectsCliExitCodes.DenialOrNotFound;
        }
        catch (HexalithProjectsApiException ex) when (ex.StatusCode == 503 || ex.StatusCode == 502 || ex.StatusCode == 504)
        {
            await WriteErrorAsync("data_unavailable").ConfigureAwait(false);
            return ProjectsCliExitCodes.Unavailable;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await WriteErrorAsync("operation_canceled").ConfigureAwait(false);
            return ProjectsCliExitCodes.Unavailable;
        }
        catch
        {
            await WriteErrorAsync("unexpected_sanitized_failure").ConfigureAwait(false);
            return ProjectsCliExitCodes.Unexpected;
        }
    }

    private async Task<object> ListAsync(CancellationToken cancellationToken)
    {
        ProjectListResponse response = await _client.ListProjectsAsync(
            Lifecycle.All,
            NewCorrelationId(),
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken).ConfigureAwait(false);
        return new
        {
            command = "projects list",
            explanation = "Visible projects from the generated client; tenant scope is server-derived.",
            tenantScope = "server-derived tenant",
            payloadExcluded = true,
            items = response.Items.Select(static item => new
            {
                projectId = item.ProjectId,
                name = item.Name,
                lifecycleState = EnumCode(item.LifecycleState),
                updatedAt = item.UpdatedAt,
                freshnessTrustState = EnumCode(item.Freshness.TrustState),
                projectionWatermark = item.Freshness.ProjectionWatermark,
            }),
        };
    }

    private async Task<object> DescribeAsync(string projectId, CancellationToken cancellationToken)
    {
        Project project = await _client.GetProjectAsync(
            projectId,
            NewCorrelationId(),
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken).ConfigureAwait(false);
        return new
        {
            command = "projects describe",
            explanation = "Project detail metadata only.",
            tenantScope = "server-derived tenant",
            payloadExcluded = true,
            projectId = project.ProjectId,
            name = project.Name,
            description = project.Description,
            lifecycleState = EnumCode(project.LifecycleState),
            referenceCount = project.References.Count,
            freshnessTrustState = EnumCode(project.Freshness.TrustState),
            projectionWatermark = project.Freshness.ProjectionWatermark,
        };
    }

    private async Task<object> AuditAsync(string projectId, CancellationToken cancellationToken)
    {
        ProjectOperatorDiagnostic diagnostic = await LoadDiagnosticAsync(projectId, cancellationToken).ConfigureAwait(false);
        return new
        {
            command = "projects audit",
            explanation = "Safe audit rows exclude idempotency keys and command bodies.",
            tenantScope = "server-derived tenant",
            payloadExcluded = true,
            projectId = diagnostic.ProjectId,
            items = diagnostic.AuditTimeline.Select(static item => new
            {
                auditEventId = item.AuditEventId,
                operationType = item.OperationType,
                occurredAt = item.OccurredAt,
                correlationId = item.CorrelationId,
                taskId = item.TaskId,
                referenceKind = item.ReferenceKind?.ToString().ToLowerInvariant(),
                referenceId = item.ReferenceId,
                reasonCode = item.ReasonCode,
            }),
        };
    }

    private async Task<object> ValidateReferencesAsync(string projectId, CancellationToken cancellationToken)
    {
        ProjectOperatorDiagnostic diagnostic = await LoadDiagnosticAsync(projectId, cancellationToken).ConfigureAwait(false);
        return new
        {
            command = "projects validate",
            explanation = "Reference validation rows use shared safe state and reason vocabulary.",
            tenantScope = "server-derived tenant",
            payloadExcluded = true,
            projectId = diagnostic.ProjectId,
            items = diagnostic.References.Select(static reference => new
            {
                referenceKind = EnumCode(reference.ReferenceKind),
                referenceId = reference.ReferenceId,
                referenceState = EnumCode(reference.ReferenceState),
                reasonCode = reference.ReasonCode,
                freshnessTrustState = EnumCode(reference.Freshness.TrustState),
                projectionWatermark = reference.Freshness.ProjectionWatermark,
            }),
        };
    }

    private async Task<object> WarningsAsync(CancellationToken cancellationToken)
    {
        WarningScan scan = await ScanWarningsAsync(cancellationToken).ConfigureAwait(false);
        return new
        {
            command = "projects warnings",
            explanation = "Warning queue enriches visible project rows with bounded operator diagnostics; unavailable diagnostics are reported as an explicit count.",
            tenantScope = "server-derived tenant",
            payloadExcluded = true,
            visibleProjectCount = scan.VisibleProjectCount,
            diagnosticUnavailable = scan.DiagnosticUnavailable,
            items = scan.Rows,
        };
    }

    private async Task<object> DashboardAsync(CancellationToken cancellationToken)
    {
        WarningScan scan = await ScanWarningsAsync(cancellationToken).ConfigureAwait(false);
        return new
        {
            command = "projects dashboard",
            explanation = "Dashboard counters are derived from visible inventory and bounded diagnostics; unavailable diagnostics are reported as an explicit count.",
            tenantScope = "server-derived tenant",
            payloadExcluded = true,
            totalVisibleProjects = scan.VisibleProjectCount,
            activeProjects = scan.ActiveProjects,
            archivedProjects = scan.ArchivedProjects,
            projectsWithWarnings = scan.ProjectsWithWarnings,
            diagnosticUnavailable = scan.DiagnosticUnavailable,
        };
    }

    private async Task<WarningScan> ScanWarningsAsync(CancellationToken cancellationToken)
    {
        ProjectListResponse list = await _client.ListProjectsAsync(
            Lifecycle.All,
            NewCorrelationId(),
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken).ConfigureAwait(false);
        var rows = new List<object>();
        var warnedProjects = new HashSet<string>(StringComparer.Ordinal);
        int diagnosticUnavailable = 0;
        foreach (ProjectListItem item in list.Items.Take(25))
        {
            ProjectOperatorDiagnostic diagnostic;
            try
            {
                diagnostic = await LoadDiagnosticAsync(item.ProjectId!, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Preserve partial failure as an explicit count rather than aborting the queue (Story 5.8).
                diagnosticUnavailable++;
                continue;
            }

            foreach (ProjectReferenceSummary reference in diagnostic.References
                .Where(static r => r.ReferenceState is not (ProjectReferenceSummaryReferenceState.Included or ProjectReferenceSummaryReferenceState.Pending)))
            {
                warnedProjects.Add(diagnostic.ProjectId ?? string.Empty);
                rows.Add(new
                {
                    projectId = diagnostic.ProjectId,
                    projectName = diagnostic.Name,
                    lifecycleState = EnumCode(diagnostic.LifecycleState),
                    referenceKind = EnumCode(reference.ReferenceKind),
                    referenceId = reference.ReferenceId,
                    referenceState = EnumCode(reference.ReferenceState),
                    reasonCode = reference.ReasonCode,
                    lastObservedAt = reference.Freshness.ObservedAt,
                    freshnessTrustState = EnumCode(reference.Freshness.TrustState),
                    projectionWatermark = reference.Freshness.ProjectionWatermark,
                });
            }
        }

        IReadOnlyList<object> ordered = [.. rows];
        return new WarningScan(
            list.Items.Count,
            list.Items.Count(static item => item.LifecycleState == ProjectLifecycleState.Active),
            list.Items.Count(static item => item.LifecycleState == ProjectLifecycleState.Archived),
            warnedProjects.Count,
            diagnosticUnavailable,
            ordered);
    }

    private async Task<object> DiagnosticExportAsync(string projectId, CancellationToken cancellationToken)
    {
        ProjectOperatorDiagnostic diagnostic = await LoadDiagnosticAsync(projectId, cancellationToken).ConfigureAwait(false);
        return new
        {
            command = "projects diagnostic export",
            contractVersion = "projects.safe-diagnostic-export.v1",
            explanation = "Safe diagnostic export excludes raw payloads, paths, problem bodies, and idempotency keys.",
            tenantScope = "server-derived tenant",
            payloadExcluded = true,
            projectId = diagnostic.ProjectId,
            referenceHealthRowCount = diagnostic.References.Count,
            auditRowCount = diagnostic.AuditTimeline.Count,
            freshnessTrustState = EnumCode(diagnostic.Freshness.TrustState),
            projectionWatermark = diagnostic.Freshness.ProjectionWatermark,
        };
    }

    private async Task<object> TraceAsync(ProjectsCliInvocation invocation, CancellationToken cancellationToken)
    {
        string correlationId = invocation.Option("correlation-id") ?? NewCorrelationId();
        ProjectResolution resolution = !string.IsNullOrWhiteSpace(invocation.Option("conversation-id"))
            ? await _client.ResolveProjectFromConversationAsync(
                invocation.Option("conversation-id")!,
                invocation.Has("include-archived"),
                correlationId,
                ReadConsistencyClass.Eventually_consistent,
                cancellationToken).ConfigureAwait(false)
            : await _client.ResolveProjectFromAttachmentsAsync(
                invocation.Values("folder-id"),
                invocation.Values("file-id"),
                invocation.Has("include-archived"),
                correlationId,
                ReadConsistencyClass.Eventually_consistent,
                cancellationToken).ConfigureAwait(false);

        return new
        {
            command = "projects trace",
            explanation = "Resolution trace is transient metadata; candidate scores and ranks stay in trace output only.",
            tenantScope = "server-derived tenant",
            payloadExcluded = true,
            correlationId,
            resultState = resolution.Result.ToString(),
            candidateCount = resolution.Candidates.Count,
            exclusionCount = resolution.Excluded.Count,
            observedAt = resolution.ObservedAt,
        };
    }

    private static object Preview(ProjectsCliInvocation invocation)
        => new
        {
            command = "projects " + invocation.Command,
            explanation = "Preview metadata only; no state change was submitted.",
            tenantScope = "server-derived tenant",
            payloadExcluded = true,
            action = invocation.Option("action") ?? invocation.Command,
            projectId = invocation.Option("project-id"),
            lifecycleWireState = "Running",
            webLifecycleLabel = "Idle",
        };

    private async Task<object> MutateAsync(ProjectsCliInvocation invocation, CancellationToken cancellationToken)
    {
        string action = invocation.Command;
        string projectId = Required(invocation, "project-id");
        string correlationId = Required(invocation, "correlation-id");
        string taskId = Required(invocation, "task-id");
        if (action is not "reevaluate")
        {
            _ = Required(invocation, "idempotency-key");
            if (!invocation.Has("confirm") || string.IsNullOrWhiteSpace(invocation.Option("dry-run-evidence")))
            {
                throw new ProjectsCliValidationException("confirmation_required");
            }
        }

        AcceptedCommand accepted = action switch
        {
            "archive" => await _client.ArchiveProjectAsync(
                projectId,
                invocation.Option("idempotency-key")!,
                correlationId,
                taskId,
                new ArchiveProjectRequest
                {
                    ArchiveIntent = ArchiveProjectRequestArchiveIntent.Archive,
                    RequestSchemaVersion = ArchiveProjectRequestRequestSchemaVersion.V1,
                },
                cancellationToken).ConfigureAwait(false),
            "restore" => await _client.RestoreProjectAsync(
                projectId,
                invocation.Option("idempotency-key")!,
                correlationId,
                taskId,
                new RestoreProjectRequest
                {
                    RestoreIntent = RestoreProjectRequestRestoreIntent.Restore,
                    RequestSchemaVersion = RestoreProjectRequestRequestSchemaVersion.V1,
                },
                cancellationToken).ConfigureAwait(false),
            "relink" => await RelinkAsync(invocation, projectId, correlationId, taskId, cancellationToken).ConfigureAwait(false),
            "unlink" => await UnlinkAsync(invocation, projectId, correlationId, taskId, cancellationToken).ConfigureAwait(false),
            "reevaluate" => await ReevaluateAsync(projectId, correlationId, taskId, cancellationToken).ConfigureAwait(false),
            _ => throw new ProjectsCliValidationException("unsupported_mutation"),
        };

        ProjectOperatorDiagnostic diagnostic = await LoadDiagnosticAsync(projectId, cancellationToken).ConfigureAwait(false);
        bool confirmed = diagnostic.AuditTimeline.Any(item =>
            string.Equals(item.CorrelationId, accepted.CorrelationId, StringComparison.Ordinal));
        return new
        {
            command = "projects " + action,
            explanation = "Mutation accepted; final state confirmation is based on operator diagnostic reload/audit evidence.",
            tenantScope = "server-derived tenant",
            payloadExcluded = true,
            action,
            projectId,
            lifecycleWireState = confirmed ? "Confirmed" : "Accepted",
            webLifecycleLabel = confirmed ? "Confirmed" : "Acknowledged(202)",
            correlationId = accepted.CorrelationId,
            taskId = accepted.TaskId,
            auditObserved = confirmed,
        };
    }

    private async Task<AcceptedCommand> RelinkAsync(
        ProjectsCliInvocation invocation,
        string projectId,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken)
    {
        string referenceKind = Required(invocation, "reference-kind");
        string referenceId = Required(invocation, "reference-id");
        string idempotencyKey = invocation.Option("idempotency-key")!;
        return referenceKind switch
        {
            "folder" => await _client.SetProjectFolderAsync(
                projectId,
                idempotencyKey,
                correlationId,
                taskId,
                new SetProjectFolderRequest
                {
                    RequestSchemaVersion = SetProjectFolderRequestRequestSchemaVersion.V1,
                    Operation = SetProjectFolderRequestOperation.Set,
                    ProjectId = projectId,
                    FolderId = referenceId,
                    ReplacementConfirmed = invocation.Has("confirm"),
                    FolderMetadata = new ProjectFolderMetadata { DisplayName = invocation.Option("reference-label") ?? referenceId },
                },
                cancellationToken).ConfigureAwait(false),
            "file" => await _client.LinkFileReferenceAsync(
                projectId,
                referenceId,
                idempotencyKey,
                correlationId,
                taskId,
                new LinkFileReferenceRequest
                {
                    RequestSchemaVersion = LinkFileReferenceRequestRequestSchemaVersion.V1,
                    Operation = LinkFileReferenceRequestOperation.Link,
                    ProjectId = projectId,
                    FileReferenceId = referenceId,
                    FolderId = Required(invocation, "transient-folder-id"),
                    WorkspaceId = Required(invocation, "transient-workspace-id"),
                    FilePath = Required(invocation, "transient-file-path"),
                    FileMetadata = new ProjectFileReferenceMetadata { DisplayName = invocation.Option("reference-label") ?? referenceId },
                },
                cancellationToken).ConfigureAwait(false),
            "memory" => await _client.LinkMemoryAsync(
                projectId,
                referenceId,
                idempotencyKey,
                correlationId,
                taskId,
                new LinkMemoryRequest
                {
                    RequestSchemaVersion = LinkMemoryRequestRequestSchemaVersion.V1,
                    Operation = LinkMemoryRequestOperation.Link,
                    ProjectId = projectId,
                    MemoryReferenceId = referenceId,
                    MemoryMetadata = new ProjectMemoryReferenceMetadata { DisplayName = invocation.Option("reference-label") ?? referenceId },
                },
                cancellationToken).ConfigureAwait(false),
            _ => throw new ProjectsCliValidationException("unsupported_reference_kind"),
        };
    }

    private async Task<AcceptedCommand> UnlinkAsync(
        ProjectsCliInvocation invocation,
        string projectId,
        string correlationId,
        string taskId,
        CancellationToken cancellationToken)
    {
        string referenceKind = Required(invocation, "reference-kind");
        string referenceId = Required(invocation, "reference-id");
        string idempotencyKey = invocation.Option("idempotency-key")!;
        return referenceKind switch
        {
            "conversation" => await _client.UnlinkProjectConversationAsync(
                projectId,
                referenceId,
                idempotencyKey,
                correlationId,
                taskId,
                new UnlinkProjectConversationRequest
                {
                    RequestSchemaVersion = UnlinkProjectConversationRequestRequestSchemaVersion.V1,
                    Operation = UnlinkProjectConversationRequestOperation.Unlink,
                    UnlinkIntent = UnlinkProjectConversationRequestUnlinkIntent.Clear,
                    ProjectId = projectId,
                    ConversationId = referenceId,
                },
                cancellationToken).ConfigureAwait(false),
            "file" => await _client.UnlinkFileReferenceAsync(
                projectId,
                referenceId,
                idempotencyKey,
                correlationId,
                taskId,
                new UnlinkFileReferenceRequest
                {
                    RequestSchemaVersion = UnlinkFileReferenceRequestRequestSchemaVersion.V1,
                    Operation = UnlinkFileReferenceRequestOperation.Unlink,
                    UnlinkIntent = UnlinkFileReferenceRequestUnlinkIntent.RemoveReference,
                    ProjectId = projectId,
                    FileReferenceId = referenceId,
                },
                cancellationToken).ConfigureAwait(false),
            "memory" => await _client.UnlinkMemoryAsync(
                projectId,
                referenceId,
                idempotencyKey,
                correlationId,
                taskId,
                new UnlinkMemoryRequest
                {
                    RequestSchemaVersion = UnlinkMemoryRequestRequestSchemaVersion.V1,
                    Operation = UnlinkMemoryRequestOperation.Unlink,
                    UnlinkIntent = UnlinkMemoryRequestUnlinkIntent.RemoveReference,
                    ProjectId = projectId,
                    MemoryReferenceId = referenceId,
                },
                cancellationToken).ConfigureAwait(false),
            _ => throw new ProjectsCliValidationException("unsupported_reference_kind"),
        };
    }

    private async Task<AcceptedCommand> ReevaluateAsync(string projectId, string correlationId, string taskId, CancellationToken cancellationToken)
    {
        await _client.RefreshProjectContextAsync(
            projectId,
            correlationId,
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken).ConfigureAwait(false);
        return new AcceptedCommand
        {
            AcceptedAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            TaskId = taskId,
            Status = AcceptedCommandStatus.Accepted,
            IdempotentReplay = false,
        };
    }

    private Task<ProjectOperatorDiagnostic> LoadDiagnosticAsync(string projectId, CancellationToken cancellationToken)
        => _client.GetProjectOperatorDiagnosticsAsync(
            projectId,
            25,
            NewCorrelationId(),
            ReadConsistencyClass.Eventually_consistent,
            cancellationToken);

    // Output is machine-readable JSON by default and via `--format json`; no color is used, so the
    // JSON contract carries all state/reason text (AC5). A separate text/table renderer is not yet
    // implemented, so the writer emits JSON unconditionally rather than silently aliasing a format.
    private Task WriteResultAsync(object result)
        => _output.WriteLineAsync(JsonSerializer.Serialize(result, JsonOptions));

    private Task WriteErrorAsync(string safeCode)
        => _error.WriteLineAsync(safeCode);

    private static string Required(ProjectsCliInvocation invocation, string name)
        => string.IsNullOrWhiteSpace(invocation.Option(name))
            ? throw new ProjectsCliUsageException(name + "_required")
            : invocation.Option(name)!;

    private static string NewCorrelationId()
        => Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

    private static string EnumCode<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => value.ToString().ToLower(CultureInfo.InvariantCulture);
}

/// <summary>Bounded warning/dashboard scan result shared by the warnings and dashboard commands.</summary>
internal sealed record WarningScan(
    int VisibleProjectCount,
    int ActiveProjects,
    int ArchivedProjects,
    int ProjectsWithWarnings,
    int DiagnosticUnavailable,
    IReadOnlyList<object> Rows);

internal sealed class ProjectsCliUsageException(string safeCode) : Exception
{
    public string SafeCode { get; } = safeCode;
}

internal sealed class ProjectsCliValidationException(string safeCode) : Exception
{
    public string SafeCode { get; } = safeCode;
}
