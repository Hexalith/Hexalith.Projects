namespace Story65.ImageMemo.Harness;
internal static class Hosts
{
    internal static IMemoHost Create(string name) => name switch
    {
        "production" => new ProductionHost(), "rwl-control" => new ControlHost(), "stream-compare" => new CandidateHost(),
        _ => throw new ArgumentException("Unknown closed variant."),
    };
    internal static readonly string[] Names = ["production", "rwl-control", "stream-compare"];
}
