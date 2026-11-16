namespace Hexalith.Projects.Testing.Reads;

using System.Text.Json;

/// <summary>Independently captured canonical output and storage/cursor observations for one authenticated path.</summary>
/// <param name="CanonicalPayload">The output after only approved, evidenced mappings.</param>
/// <param name="RawKeys">The logical per-aggregate keys retained separately from physical storage containers.</param>
/// <param name="AggregateIdentities">The independently decoded AggregateIdentity for each logical aggregate.</param>
/// <param name="Positions">The exact source positions in the same order.</param>
/// <param name="CursorEvidence">Independently decoded boundary, scope, continuation, expiry and rejection results.</param>
/// <param name="SupportedOnlyAssertions">Whether independent snapshot/component assertions actually passed.</param>
public sealed record ReadCutoverObservation(JsonElement CanonicalPayload, IReadOnlyList<string> RawKeys,
    IReadOnlyList<string> AggregateIdentities, IReadOnlyList<long> Positions, JsonElement CursorEvidence, bool SupportedOnlyAssertions)
{
    /// <summary>Gets actual persisted store/container addresses; a legacy journal may contain multiple logical aggregates.</summary>
    public IReadOnlyList<string>? PhysicalAddresses { get; init; }
    /// <summary>Gets independent validation of this transport's cursor contract when the two transports have different paging contracts.</summary>
    public bool? TransportCursorAssertions { get; init; }
    /// <summary>Gets original GlobalPosition values captured independently from the source event envelopes.</summary>
    public IReadOnlyList<long>? SourceGlobalPositions { get; init; }
    /// <summary>Gets original aggregate SequenceNumber values captured independently from source event envelopes.</summary>
    public IReadOnlyList<long>? SourceSequenceNumbers { get; init; }
    /// <summary>Gets supported ProjectVersion values without renaming a legacy global Sequence.</summary>
    public IReadOnlyList<long>? ProjectVersions { get; init; }
}
