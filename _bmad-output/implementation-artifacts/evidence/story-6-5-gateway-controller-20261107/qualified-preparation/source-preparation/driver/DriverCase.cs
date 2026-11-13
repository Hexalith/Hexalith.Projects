namespace Story65.GatewayControllerProof;

internal sealed record DriverCase(string Name, string Fault = "none", bool Origin = true, bool PriorMarker = false, bool Slow = false);
