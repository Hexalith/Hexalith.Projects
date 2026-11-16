namespace Hexalith.Projects.UI;

/// <summary>Records the immutable read route once during host startup without exposing configuration values.</summary>
internal static partial class ProjectsReadRouteLog
{
    /// <summary>Logs the selected route and bounded refusal reason.</summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Project read route {Mode}; refusal reason {RefusalReason}.")]
    internal static partial void Selected(ILogger logger, string mode, string refusalReason);
}
