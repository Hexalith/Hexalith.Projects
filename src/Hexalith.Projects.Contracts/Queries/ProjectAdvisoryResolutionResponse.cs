namespace Hexalith.Projects.Contracts.Queries;

using Hexalith.Projects.Contracts.Ui;

/// <summary>Transient advisory observations. NoMatch covers evaluated observations; one candidate is not exclusive or selected.</summary>
/// <param name="Result">The observed outcome; null for an empty incomplete evaluation.</param>
/// <param name="Candidates">Authorized unselected suggestions without names or protected exclusions.</param>
/// <param name="EvaluatedAt">The request evaluation time, never a source watermark.</param>
/// <param name="Incomplete">Whether bounds, dependencies or unsupported coverage omitted observations.</param>
/// <param name="UnsupportedInputKinds">Presented kinds with no production identity mapping, currently file.</param>
public sealed record ProjectAdvisoryResolutionResponse(ResolutionResult? Result, IReadOnlyList<ProjectAdvisoryCandidate> Candidates,
    DateTimeOffset EvaluatedAt, bool Incomplete, IReadOnlyList<string> UnsupportedInputKinds);
