namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.RegularExpressions;

/// <summary>Restricts audit-only write observations to the durable record and per-Project index schemas.</summary>
internal static partial class ProjectAuditStorageKeys
{
    internal static bool IsAuditKey(string key) => AuditKeyPattern().IsMatch(key);

    [GeneratedRegex(@"\Aprojects:(?:read-audit:v1:[0-9A-F]{64}|audit-index:v1:[0-9A-F]{64}:(?:0|[1-9][0-9]*))\z")]
    private static partial Regex AuditKeyPattern();
}
