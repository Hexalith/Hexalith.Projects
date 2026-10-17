namespace Hexalith.Projects.Testing.Reads;

/// <summary>Metadata-only shadow evidence; divergences contain field paths, never protected values.</summary>
/// <param name="Equivalent">Whether every compared field and boundary matched.</param>
/// <param name="Divergences">Unexplained field-path deltas.</param>
/// <param name="NormalizationRevision">The finite local comparison contract revision.</param>
public sealed record ProjectReadShadowComparison(bool Equivalent, IReadOnlyList<string> Divergences, string NormalizationRevision);
