// <copyright file="ProjectsMcpDescriptors.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Mcp;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.Projects.Contracts.Ui;

/// <summary>
/// Projects MCP protocol names and registry-backed descriptors.
/// </summary>
public static class ProjectsMcpDescriptors
{
    /// <summary>The Projects MCP manifest schema version.</summary>
    public const string SchemaVersion = "hexalith.projects.mcp.v1";

    /// <summary>The bounded context name used in MCP descriptors.</summary>
    public const string BoundedContext = "Projects";

    /// <summary>Read-only MCP resource names.</summary>
    public static readonly IReadOnlyList<string> ResourceNames =
    [
        "projects.inventory",
        "projects.detail",
        "projects.operatorDiagnostic",
        "projects.referenceHealth",
        "projects.resolutionTrace",
        "projects.auditTimeline",
        "projects.safeDiagnosticExport",
        "projects.warningQueue",
        "projects.operationalDashboard",
        "projects.maintenanceAction",
    ];

    /// <summary>
    /// Mutating MCP tool action names approved by Story 5.9. Sourced from the shared
    /// <see cref="ProjectMaintenanceActions"/> constants so MCP/CLI/Web never drift onto
    /// adapter-local magic strings (AC8).
    /// </summary>
    public static readonly IReadOnlyList<string> MaintenanceActionNames =
    [
        ProjectMaintenanceActions.Archive,
        ProjectMaintenanceActions.Restore,
        ProjectMaintenanceActions.Relink,
        ProjectMaintenanceActions.Unlink,
        ProjectMaintenanceActions.Reevaluate,
    ];

    /// <summary>The Projects MCP descriptor manifest consumed by FrontComposer MCP.</summary>
    public static McpManifest Manifest { get; } = new(
        SchemaVersion,
        MaintenanceActionNames.Select(Command).ToArray(),
        [
            Resource("projects.inventory", typeof(ProjectsMcpInventoryItem), "Visible project inventory"),
            Resource("projects.detail", typeof(ProjectsMcpProjectDetailItem), "Project detail metadata"),
            Resource("projects.operatorDiagnostic", typeof(ProjectsMcpOperatorDiagnosticItem), "Operator diagnostic metadata"),
            Resource("projects.referenceHealth", typeof(ProjectsMcpReferenceHealthItem), "Reference health rows"),
            Resource("projects.resolutionTrace", typeof(ProjectsMcpResolutionTraceItem), "Resolution trace metadata"),
            Resource("projects.auditTimeline", typeof(ProjectsMcpAuditTimelineItem), "Audit timeline rows"),
            Resource("projects.safeDiagnosticExport", typeof(ProjectsMcpSafeDiagnosticExportItem), "Safe diagnostic export summary"),
            Resource("projects.warningQueue", typeof(ProjectsMcpWarningQueueItem), "Warning queue rows"),
            Resource("projects.operationalDashboard", typeof(ProjectsMcpOperationalDashboardItem), "Operational dashboard counters"),
            Resource("projects.maintenanceAction", typeof(ProjectsMcpMaintenanceActionItem), "Maintenance action preview metadata"),
        ]);

    /// <summary>Returns true when <paramref name="name"/> is a documented Projects MCP resource.</summary>
    /// <param name="name">The resource name.</param>
    /// <returns>Whether the resource is known.</returns>
    public static bool IsKnownResource(string? name)
        => ResourceNames.Contains(name ?? string.Empty, StringComparer.Ordinal);

    /// <summary>Returns true when <paramref name="action"/> is an approved maintenance action.</summary>
    /// <param name="action">The maintenance action.</param>
    /// <returns>Whether the action is approved.</returns>
    public static bool IsApprovedMaintenanceAction(string? action)
        => MaintenanceActionNames.Contains(action ?? string.Empty, StringComparer.Ordinal);

    private static McpResourceDescriptor Resource(string name, Type projectionType, string title)
        => new(
            ProtocolUri: $"frontcomposer://Projects/projections/{name}",
            Name: name,
            ProjectionTypeName: projectionType.AssemblyQualifiedName!,
            BoundedContext: BoundedContext,
            Title: title,
            Description: "Metadata-only Projects operational surface with server-derived tenant scope and payload exclusion.",
            Fields: CommonFields(projectionType),
            EntityPluralLabel: name);

    private static McpCommandDescriptor Command(string action)
        => new(
            ProtocolName: $"projects.{action}",
            CommandTypeName: typeof(ProjectsMcpMaintenanceCommand).AssemblyQualifiedName!,
            BoundedContext: BoundedContext,
            Title: $"Projects {action}",
            Description: "Tenant-aware metadata-only maintenance action over the generated Projects client.",
            AuthorizationPolicyName: "projects:maintain",
            Parameters: CommandParameters(action),
            DerivablePropertyNames: ["TenantId", "UserId", "CommandId", "CorrelationId"]);

    private static IReadOnlyList<McpParameterDescriptor> CommandParameters(string action)
        =>
        [
            Parameter(nameof(ProjectsMcpMaintenanceCommand.Action), "string", required: true, enumValues: [action]),
            Parameter(nameof(ProjectsMcpMaintenanceCommand.ProjectId), "string", required: true),
            Parameter(nameof(ProjectsMcpMaintenanceCommand.ReferenceKind), "string", required: false, enumValues: ["conversation", "folder", "file", "memory"]),
            Parameter(nameof(ProjectsMcpMaintenanceCommand.ReferenceId), "string", required: false),
            Parameter(nameof(ProjectsMcpMaintenanceCommand.ReferenceDisplayLabel), "string", required: false),
            Parameter(nameof(ProjectsMcpMaintenanceCommand.ReplacementConfirmed), "boolean", required: false),
            Parameter(nameof(ProjectsMcpMaintenanceCommand.TransientFolderId), "string", required: false),
            Parameter(nameof(ProjectsMcpMaintenanceCommand.TransientWorkspaceId), "string", required: false),
            Parameter(nameof(ProjectsMcpMaintenanceCommand.TransientFilePath), "string", required: false),
            Parameter(nameof(ProjectsMcpMaintenanceCommand.Confirmed), "boolean", required: true),
            Parameter(nameof(ProjectsMcpMaintenanceCommand.DryRunEvidence), "string", required: true),
            Parameter(nameof(ProjectsMcpMaintenanceCommand.IdempotencyKey), "string", required: true),
        ];

    private static IReadOnlyList<McpParameterDescriptor> CommonFields(Type projectionType)
        => projectionType.GetProperties()
            .Select(p => Parameter(p.Name, ToJsonType(p.PropertyType), required: false))
            .ToArray();

    private static McpParameterDescriptor Parameter(
        string name,
        string jsonType,
        bool required,
        IReadOnlyList<string>? enumValues = null)
        => new(
            name,
            jsonType == "boolean" ? "System.Boolean" : "System.String",
            jsonType,
            required,
            !required,
            name,
            null,
            enumValues ?? [],
            IsUnsupported: false);

    private static string ToJsonType(Type type)
    {
        Type actual = Nullable.GetUnderlyingType(type) ?? type;
        if (actual == typeof(bool))
        {
            return "boolean";
        }

        if (actual == typeof(int) || actual == typeof(long))
        {
            return "integer";
        }

        return "string";
    }
}
