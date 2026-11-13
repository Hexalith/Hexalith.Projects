namespace Story65.TechnicalFingerprint.Harness;
internal static class VariantHosts
{
    internal static readonly string[] Names = ["actual", "control", "candidate"];
    internal static ITechnicalHost Create(string name) => name switch
    {
        "actual" => new ActualHost(), "control" => new ControlHost(), "candidate" => new CandidateHost(),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };
    internal static IProofPass Pass(string name, IServiceProvider services) => name switch
    {
        "actual" => new ActualPass(services), "control" => new ControlPass(services), "candidate" => new CandidatePass(services),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };
}
