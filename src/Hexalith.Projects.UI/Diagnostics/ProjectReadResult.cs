namespace Hexalith.Projects.UI.Diagnostics;

using Hexalith.Projects.UI.Rendering;

/// <summary>A single supported query observation and its sanitized failure.</summary>
/// <typeparam name="T">The delivered response type.</typeparam>
/// <param name="Value">The unmodified delivered observation.</param>
/// <param name="Feedback">Safe feedback when the query cannot disclose a result.</param>
/// <param name="TenantScope">The authenticated Tenant that scoped the observation.</param>
public sealed record ProjectReadResult<T>(T? Value, ProjectConsoleFeedback? Feedback, string TenantScope);
