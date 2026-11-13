"""File preparation only; copies accepted inputs and changes only the terminal adapter."""
from pathlib import Path
import difflib
import hashlib
import json

BASE = Path('/tmp/story65-advisory-terminal-prep-20261108')
OUT = Path(__file__).parent
sha = lambda data: hashlib.sha256(data).hexdigest()
edits = [
    ('    private int _disposed;\n', '    private int _disposed;\n    private CaptureCounters.Interval? _activeSpan;\n    private int _seenSpanStages;\n    private bool _spansDisabled;\n'),
    ('        try { lock (_gate) { if (!_closed) _last = boundary; } } catch { }\n', '''        try
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
'''),
    ('                if (_closed || _firstFailure != AdvisoryBoundary.None) return;\n', '                StopSpan();\n                if (_closed || _firstFailure != AdvisoryBoundary.None) return;\n'),
    ('                if (_closed) return;\n                _closed = true;\n', '                StopSpan();\n                if (_closed) return;\n                _closed = true;\n'),
    ('    private void Add(Count slot, int value)\n', '''    // The original executor emits selected markers at most once. Unselected markers
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
            AdvisoryBoundary.Index => Stage.ResultTail,
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
'''),
]
copied = {}
for name in ['CaptureCounters.cs', 'AdvisoryWaveBootstrap.cs', 'AdvisoryBoundary.cs', 'AdvisoryExceptionKind.cs',
             'AdvisoryTermination.cs', 'ProjectAdvisoryQueryExecutor.original.cs', 'ProjectAdvisoryQueryExecutor.private.cs',
             'InstrumentedProjectAdvisoryQueryExecutor.cs', 'source.patch', 'normalization.json', 'source-proof.json']:
    data = (BASE / name).read_bytes()
    destination = 'accepted-executor-source-proof.json' if name == 'source-proof.json' else name
    path = OUT / destination
    with path.open('xb') as stream:
        stream.write(data)
    copied[name] = {'originalPath': str(BASE / name), 'destination': destination, 'sha256': sha(data), 'byteExact': True}
original = (BASE / 'AdvisoryTerminalCapture.cs').read_bytes()
with (OUT / 'AdvisoryTerminalCapture.original.cs').open('xb') as stream:
    stream.write(original)
candidate = original.decode()
for before, after in edits:
    assert candidate.count(before) == 1
    candidate = candidate.replace(before, after, 1)
with (OUT / 'AdvisoryTerminalCapture.cs').open('xb') as stream:
    stream.write(candidate.encode())
normalized = candidate
for before, after in reversed(edits):
    assert normalized.count(after) == 1
    normalized = normalized.replace(after, before, 1)
assert normalized.encode() == original
patch = ''.join(difflib.unified_diff(original.decode().splitlines(True), candidate.splitlines(True),
                                  fromfile='accepted/AdvisoryTerminalCapture.cs', tofile='boundary-spans/AdvisoryTerminalCapture.cs'))
(OUT / 'adapter.patch').write_text(patch)
proof = {'filePreparationOnly': True, 'builds': 0, 'runs': 0, 'sinkDirectoryAccesses': 0,
         'adapterOriginalPath': str(BASE / 'AdvisoryTerminalCapture.cs'), 'adapterOriginalSha256': sha(original),
         'adapterCandidateSha256': sha(candidate.encode()), 'reverseBytesEqual': True,
         'adapterPatchSha256': sha(patch.encode()), 'adapterEdits': [{'before': a, 'after': b} for a, b in edits],
         'unchangedAcceptedInputs': copied,
         'acceptedPreparationReceiptPath': str(BASE / 'preparation-receipt.json'),
         'acceptedPreparationReceiptSha256': sha((BASE / 'preparation-receipt.json').read_bytes()),
         'acceptedQualificationHandoffPath': '/tmp/story65-advisory-terminal-build-20261108/qualified-handoff.json',
         'acceptedQualificationHandoffSha256': sha(Path('/tmp/story65-advisory-terminal-build-20261108/qualified-handoff.json').read_bytes())}
(OUT / 'source-proof.json').write_text(json.dumps(proof, indent=2) + '\n')
print(json.dumps({'adapterCandidateSha256': proof['adapterCandidateSha256'], 'reverseBytesEqual': True,
                  'helperSha256': copied['CaptureCounters.cs']['sha256'],
                  'bootstrapSha256': copied['AdvisoryWaveBootstrap.cs']['sha256'],
                  'privateExecutorSha256': copied['ProjectAdvisoryQueryExecutor.private.cs']['sha256'],
                  'patchSha256': proof['adapterPatchSha256']}))
