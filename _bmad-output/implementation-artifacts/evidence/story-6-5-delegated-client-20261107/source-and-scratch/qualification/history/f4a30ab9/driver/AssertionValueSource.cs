namespace Story65.DelegatedClientProof;

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
        if (state.Test.ValueSource == "source-sync") { _core.SetResult("synthetic-owner-assertion"); Completion = Task.CompletedTask; }
        else Completion = CompleteAsync();
    }
    internal ValueTask<string?> Value => new(this, _core.Version);
    private async Task CompleteAsync()
    {
        await _registered.Task.ConfigureAwait(false);
        try
        {
            await _state.FakeAwaitAsync().ConfigureAwait(false);
            if (_state.Test.Fault == "issuer-uncooperative") await _state.ReleaseIssuer.Task.ConfigureAwait(false);
            if (_state.Test.Fault == "issuer-cooperative")
            { Interlocked.Increment(ref _state.Triggered); await Task.Delay(Timeout.InfiniteTimeSpan, _token).ConfigureAwait(false); }
            if (_state.Test.Fault == "issuer-source-fault") { Interlocked.Increment(ref _state.Triggered); throw new FixtureFailure(); }
            if (_state.Test.Fault == "issuer-source-cancel")
            { Interlocked.Increment(ref _state.Triggered); throw new OperationCanceledException(new CancellationToken(true)); }
            _core.SetResult("synthetic-owner-assertion");
        }
        catch (Exception exception) { _core.SetException(exception); }
    }
    public string? GetResult(short token)
    { Check.Require(Interlocked.Increment(ref Results) == 1, "ValueTask source consumed more than once."); return _core.GetResult(token); }
    public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        Check.Require(Interlocked.Increment(ref Registrations) == 1, "ValueTask source registered more than once.");
        _core.OnCompleted(continuation, state, token, flags); _registered.TrySetResult();
    }
}
