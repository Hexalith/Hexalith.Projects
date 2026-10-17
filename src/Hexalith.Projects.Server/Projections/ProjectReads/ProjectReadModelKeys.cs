namespace Hexalith.Projects.Server.Projections.ProjectReads;

/// <summary>Collision-safe logical keys for supported list/open read models.</summary>
public static class ProjectReadModelKeys
{
    /// <summary>The existing configured platform state-store component.</summary>
    public const string StoreName = "statestore";

    /// <summary>Returns the tenant's supported index key.</summary>
    public static string Index(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return $"projects:reads:v1:{tenantId.Length}:{tenantId}:index";
    }

    /// <summary>Returns the supported detail key.</summary>
    public static string Detail(string tenantId, string projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        return $"projects:reads:v1:{tenantId.Length}:{tenantId}:{projectId.Length}:{projectId}";
    }
}
