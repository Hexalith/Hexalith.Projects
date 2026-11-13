namespace Story65.DelegatedClientProof;

internal sealed record DriverCase(string Name, string Fault = "none", string? Budget = null,
    string Workload = "projects", string ValueSource = "sync", int Status = 200, bool Slow = false, int Concurrency = 1);
