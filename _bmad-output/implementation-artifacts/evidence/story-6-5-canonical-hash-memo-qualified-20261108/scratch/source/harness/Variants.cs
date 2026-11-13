namespace Story65.Canonical.Harness;
/// <summary>Constructs independent owners without cross-variant memo sharing.</summary>
internal static class Variants
{
    internal static IVariant Create(string name) => name switch
    {
        "production" => new ProductionVariant(), "control" => new ControlVariant(), "candidate" => new CandidateVariant(),
        _ => throw new ClosedFailure("unknown-variant"),
    };
}
