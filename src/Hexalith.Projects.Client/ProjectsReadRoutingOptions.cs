namespace Hexalith.Projects.Client;

/// <summary>One explicit startup choice for primary Web reads; denial never changes this choice.</summary>
public sealed class ProjectsReadRoutingOptions
{
    /// <summary>Gets or sets Supported, LegacyRollback, or Unavailable. Activation requires complete evidence.</summary>
    public string Mode { get; set; } = "Unavailable";

    /// <summary>Gets or sets the retained receipt path.</summary>
    public string? EvidencePath { get; set; }

    /// <summary>Gets or sets the expected receipt SHA-256.</summary>
    public string? EvidenceHash { get; set; }

    /// <summary>Gets or sets the full application revision to which the receipt must bind.</summary>
    public string? SourceRevision { get; set; }

    /// <summary>Gets or sets the exact Contracts fingerprint.</summary>
    public string? ContractHash { get; set; }
}
