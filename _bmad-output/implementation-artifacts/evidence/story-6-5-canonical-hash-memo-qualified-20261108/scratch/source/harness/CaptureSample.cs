namespace Story65.Canonical.Harness;
/// <summary>One whole scoped observation elapsed wall time, without request or payload identity.</summary>
internal sealed record CaptureSample(int Worker, int Ordinal, long ElapsedTicks);
