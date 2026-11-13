namespace Story65.DaprInvokerProof;

internal sealed record Observation(long Sequence, string Operation, string Argument, CancellationToken Token, bool Cancellable, bool CanceledAtRead);
