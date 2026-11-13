namespace Hexalith.PrivateCaptureCounter;

// Only closed enums and booleans enter this private per-call adapter. No DTO, exception,
// cancellation-token struct, callback, string or application count is accepted.
internal sealed class AdvisoryTerminalCapture : IDisposable
{
    private readonly object _gate = new();
    private readonly CaptureCounters _capture;
    private AdvisoryBoundary _last;
    private AdvisoryBoundary _firstFailure;
    private AdvisoryExceptionKind _exception;
    private AdvisoryTermination _termination;
    private bool _closed;
    private int _disposed;
    private CaptureCounters.Interval? _activeSpan;
    private int _seenSpanStages;
    private bool _spansDisabled;

    private AdvisoryTerminalCapture(CaptureCounters capture) => _capture = capture;

    internal static AdvisoryTerminalCapture? Begin()
    {
        CaptureCounters? capture = null;
        try
        {
            capture = AdvisoryWaveBootstrap.Begin();
            return capture is null ? null : new(capture);
        }
        catch
        {
            try { capture?.Dispose(); } catch { }
            return null;
        }
    }

    internal void Enter(AdvisoryBoundary boundary)
    {
        if (boundary is < AdvisoryBoundary.None or > AdvisoryBoundary.EnvelopeValidation) return;
        try
        {
            lock (_gate)
            {
                if (_closed) return;
                StopSpan();
                _last = boundary;
                StartSpan(boundary);
            }
        }
        catch { }
    }

    internal void Failure(AdvisoryExceptionKind exception)
    {
        if (exception is not (AdvisoryExceptionKind.Cancellation or AdvisoryExceptionKind.Other)) return;
        try
        {
            lock (_gate)
            {
                StopSpan();
                if (_closed || _firstFailure != AdvisoryBoundary.None) return;
                _firstFailure = _last;
                _exception = exception;
            }
        }
        catch { }
    }

    internal void Termination(AdvisoryTermination termination)
    {
        if (termination is < AdvisoryTermination.UnobservedExit or > AdvisoryTermination.CallerCancellationRethrown) return;
        try { lock (_gate) { if (!_closed) _termination = termination; } } catch { }
    }

    // This is one fresh observation of a local flag AFTER the original serialization.
    // It does not classify the serialized DTO, result success, or why omission occurred.
    internal void ResponseReturned(bool localOmissionObservedAfterSerialization)
    {
        try
        {
            lock (_gate)
            {
                if (!_closed && _termination == AdvisoryTermination.UnobservedExit)
                    _termination = localOmissionObservedAfterSerialization
                        ? AdvisoryTermination.ResponseReturnedLocalOmissionObservedUnattributed
                        : AdvisoryTermination.ResponseReturnedLocalOmissionNotObserved;
            }
        }
        catch { }
    }

    internal void Close(bool callerCancellationObserved, bool linkedCancellationObserved)
    {
        try
        {
            lock (_gate)
            {
                StopSpan();
                if (_closed) return;
                _closed = true;
                // Stage4 is the exact unchanged helper's ONLY count-bearing row. These six
                // fields are bounded unary encodings of this protocol, not business counts.
                using var terminal = _capture.Stage(Stage.SettingsEnumerationSortDictionary, AllocationMode.None);
                Add(Count.Yielded, (int)_last);
                Add(Count.NullValue, (int)_firstFailure);
                Add(Count.AuthenticationAccepted, (int)_termination);
                Add(Count.NonAuthenticationDiscarded, (int)_exception);
                Add(Count.SensitiveSegmentExcluded, callerCancellationObserved ? 1 : 0);
                Add(Count.AuthenticationOtherExcluded, linkedCancellationObserved ? 1 : 0);
            }
        }
        catch { }
    }

    // The original executor emits selected markers at most once. Unselected markers
    // still end a selected interval. Never overwrite a prior helper stage on a repeat.
    private void StartSpan(AdvisoryBoundary boundary)
    {
        if (_spansDisabled) return;
        Stage? stage = boundary switch
        {
            AdvisoryBoundary.InitialScope => Stage.ComponentFile,
            AdvisoryBoundary.InitialInputs => Stage.ComponentPolicy,
            AdvisoryBoundary.Discovery => Stage.AclFile,
            AdvisoryBoundary.FinalInputs => Stage.AuthenticationSerialization,
            AdvisoryBoundary.FinalCandidates => Stage.ActorBoundary,
            AdvisoryBoundary.FinalScope => Stage.ResultTail,
            _ => null,
        };
        if (stage is not { } selected) return;
        int bit = 1 << (int)selected;
        if ((_seenSpanStages & bit) != 0) { _spansDisabled = true; return; }
        _seenSpanStages |= bit;
        _activeSpan = _capture.Stage(selected, AllocationMode.None);
        if (_activeSpan is null) _spansDisabled = true;
    }

    private void StopSpan()
    {
        CaptureCounters.Interval? active = _activeSpan;
        _activeSpan = null;
        try { active?.Dispose(); } catch { _spansDisabled = true; }
    }

    private void Add(Count slot, int value)
    {
        // Public-to-this-adapter entry points validate each enum's actual range.
        for (int n = 0; n < value; n++) _capture.Count(slot);
    }

    public void Dispose()
    {
        try
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            try
            {
                // Covers setup exceptions before the executor installs its finally. With
                // boundary None these false bits mean unobserved, not proven uncancelled.
                Close(false, false);
            }
            finally { try { _capture.Dispose(); } catch { } }
        }
        catch { }
    }
}
