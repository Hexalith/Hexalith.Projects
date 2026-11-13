namespace Story65.OutboundAssertionProof;

internal sealed record DriverCase(string Name, string Fault = "", string Route = "query", bool Bindings = true,
    bool SourceSync = false, bool OuterBuffer = false, bool Slow = false);
