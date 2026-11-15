namespace Hexalith.Projects.Contracts.Queries;

using System.ComponentModel;
using System.Text.Json.Serialization;
using Hexalith.McpCli.Abstractions;

/// <summary>An opaque attachment identity, never a file path.</summary>
/// <param name="ReferenceKind">The owner kind: folder or file.</param>
/// <param name="ReferenceId">The exact opaque owner identity.</param>
public sealed record ProjectAdvisoryAttachment(
    [property: Description("Owner kind: folder or file; file mapping is unsupported."), JsonPropertyName("referenceKind")] string ReferenceKind,
    [property: Description("Exact opaque owner identifier, never a path."), JsonPropertyName("referenceId"), HexalithIdentifier] string ReferenceId);
