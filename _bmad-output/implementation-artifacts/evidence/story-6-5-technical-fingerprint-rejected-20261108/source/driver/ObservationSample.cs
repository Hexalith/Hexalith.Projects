namespace Story65.TechnicalFingerprint.Driver;

/// <summary>One whole fresh pass with scope disposal; no descriptor, schema or memo-key value.</summary>
internal sealed record ObservationSample(int Reader, int Ordinal, long ElapsedTicks, int EndpointCount, int ImageHits);
