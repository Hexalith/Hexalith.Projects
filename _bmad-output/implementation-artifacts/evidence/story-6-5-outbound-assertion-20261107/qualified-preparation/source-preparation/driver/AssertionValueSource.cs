namespace Story65.OutboundAssertionProof;

using System.Threading.Tasks.Sources;

internal sealed class AssertionValueSource : IValueTaskSource<string?>
{
    private ManualResetValueTaskSourceCore<string?> _core = new() { RunContinuationsAsynchronously = true };
    private readonly FixtureState _state;
    private readonly CancellationToken _token;
    private readonly TaskCompletionSource _registered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal int Results, Registrations;
    internal Task Completion { get; }
    internal AssertionValueSource(FixtureState state, CancellationToken token)
    {
        _state = state; _token = token;
        if (state.Test.SourceSync && !state.Canary)
        {
            if (state.Test.Fault == "issuer-sync-fault") _core.SetException(state.Inject("issuer-sync-fault", new FixtureFailure()));
            else _core.SetResult(FixtureState.FreshAssertion);
            Completion = Task.CompletedTask;
        }
        else Completion = CompleteAsync();
    }
    internal ValueTask<string?> Value => new(this, _core.Version);
    private async Task CompleteAsync()
    {
        await _registered.Task.ConfigureAwait(false);
        try
        {
            await _state.ProbeAsync().ConfigureAwait(false);
            if (_state.Test.Fault == "held-issuer") await _state.ReleaseIssuer.Task.ConfigureAwait(false);
            if (_state.Test.Fault == "issuer-async-fault") throw _state.Inject("issuer-async-fault", new FixtureFailure());
            if (_state.Test.Fault == "issuer-cancel")
            { _state.Caller.Cancel(); throw _state.Inject("issuer-cancel", new OperationCanceledException(_token)); }
            _core.SetResult(FixtureState.FreshAssertion);
        }
        catch (Exception exception) { _core.SetException(exception); }
    }
    public string? GetResult(short token)
    { Check.Require(Interlocked.Increment(ref Results) == 1, "ValueTask source consumed more than once."); return _core.GetResult(token); }
    public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        Check.Require(Interlocked.Increment(ref Registrations) == 1, "ValueTask continuation registered more than once.");
        _core.OnCompleted(continuation, state, token, flags); _registered.TrySetResult();
    }
}
