namespace Story65.AdvisoryWaveProof;

internal sealed record ProofCase(string Name, string Mutation = "none", int FaultWave = 0,
    string Fault = "none", int Projects = 1, bool Conversation = false, bool Slow = false);
