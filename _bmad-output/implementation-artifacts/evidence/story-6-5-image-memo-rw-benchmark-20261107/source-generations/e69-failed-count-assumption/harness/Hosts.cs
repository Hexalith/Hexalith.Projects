namespace Story65.ImageMemo.Harness;
internal static class Hosts
{
    internal static IMemoHost Create(string name) => name switch
    {
        "oracle" => new OracleHost(), "control" => new ControlHost(), "candidate" => new CandidateHost(),
        _ => throw new ArgumentException("Unknown closed variant."),
    };
    internal static readonly string[] Names = ["oracle", "control", "candidate"];
}
