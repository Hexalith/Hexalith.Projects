namespace Story65.InvocationTransportProof;
internal sealed record DriverCase(string Name, bool Slow, string[][] ExpectedStages)
{
    internal int Entries => ExpectedStages.Length;
}
