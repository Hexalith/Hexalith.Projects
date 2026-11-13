namespace Story65.ImageMemo.Harness;
internal static class Hosts
{
    internal static IMemoHost Create(string name) => name switch
    {
        "production" => new ProductionHost(), "monitor-control" => new ControlHost(), "rw-copy" => new CandidateHost(),
        _ => throw new ArgumentException("Unknown closed variant."),
    };
    internal static readonly string[] Names = ["production", "monitor-control", "rw-copy"];
}
