namespace Story65.DelegatedClientProof;

internal sealed record Observation(long Sequence, string Operation, string Argument, CancellationToken Token,
    bool CanBeCanceled, bool CanceledAtRead);
