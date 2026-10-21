namespace Hexalith.Projects.Contracts.Queries;

using Hexalith.Projects.Contracts.Ui;

/// <summary>An unselected suggestion with no context or association authority.</summary>
/// <param name="ProjectId">The disclosable Project identity.</param>
/// <param name="ReasonCodes">The deterministic safe scoring reasons.</param>
/// <param name="Rank">The one-based rank.</param>
/// <param name="Score">The existing engine score.</param>
public sealed record ProjectAdvisoryCandidate(string ProjectId, IReadOnlyList<ProjectReasonCode> ReasonCodes, int Rank, int Score);
