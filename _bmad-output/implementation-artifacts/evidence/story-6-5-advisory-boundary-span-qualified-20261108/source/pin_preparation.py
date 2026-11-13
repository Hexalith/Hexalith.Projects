"""Hash-only source preparation freeze. Does not build, load, inspect processes or touch the sink."""
from pathlib import Path
import ast
import difflib
import hashlib
import json
import xml.etree.ElementTree as ET

P = Path(__file__).resolve().parent
B = Path('/tmp/story65-advisory-terminal-prep-20261108')
R = Path('/home/administrator/projects/hexalith/projects')
sha = lambda path: hashlib.sha256(Path(path).read_bytes()).hexdigest()
write = lambda name, value: (P / name).write_text(json.dumps(value, indent=2) + '\n')
old = json.loads((B / 'preparation-receipt.json').read_bytes())
inputs = {}
for name in old['inputsSha256']:
    path = Path(name)
    assert path.is_file() and not path.is_symlink() and all(not a.is_symlink() for a in path.parents)
    inputs[str(path)] = sha(path)
for name, expected in old['sourceSha256'].items():
    path = Path(name) if Path(name).is_absolute() else R / name
    assert sha(path) == expected, name
apps = {name: sha(Path(name) if Path(name).is_absolute() else R / name) for name in old['app147Sha256']}
assert len(old['sourceSha256']) == 65 and len(apps) == 147
project = ET.fromstring((P / 'differential/Differential.csproj').read_text())
assert not project.findall('.//PackageReference') and not project.findall('.//ProjectReference')
refs = [item.findtext('HintPath') for item in project.findall('.//Reference')]
assert len(refs) == 148 and all(Path(name).name != 'Hexalith.Projects.Server.Tests.dll' for name in refs)
assert all(str(Path(name)) in inputs for name in refs)
for name in ['CaptureCounters.cs', 'AdvisoryWaveBootstrap.cs', 'ProjectAdvisoryQueryExecutor.private.cs',
             'ProjectAdvisoryQueryExecutor.original.cs', 'InstrumentedProjectAdvisoryQueryExecutor.cs']:
    assert (P / name).read_bytes() == (B / name).read_bytes(), name
proof = json.loads((P / 'source-proof.json').read_bytes())
reverse = (P / 'AdvisoryTerminalCapture.cs').read_text()
for edit in reversed(proof['adapterEdits']):
    assert reverse.count(edit['after']) == 1
    reverse = reverse.replace(edit['after'], edit['before'], 1)
assert reverse.encode() == (P / 'AdvisoryTerminalCapture.original.cs').read_bytes()
assert sha(P / 'AdvisoryTerminalCapture.cs') == proof['adapterCandidateSha256']
for file in P.glob('*.py'):
    ast.parse(file.read_text())
delta = ''
for name in sorted(old['filesSha256']):
    here = P / name
    if here.is_file() and here.read_bytes() != (B / name).read_bytes():
        delta += ''.join(difflib.unified_diff((B / name).read_text().splitlines(True), here.read_text().splitlines(True),
                                            fromfile='accepted-terminal/' + name, tofile='boundary-spans/' + name))
(P / 'harness-and-adapter.patch').write_text(delta)
write('input-baseline.json', {'source65Sha256': old['sourceSha256'], 'currentApplication147Sha256': apps,
    'currentOrdinaryInputsSha256': inputs, 'rawReferences': refs,
    'priorQualifiedInputMapAssertedHistoricallyCurrent': False,
    'generationLimit': 'Current file hashes only. No equality to all prior733 compiled/input generations is claimed.',
    'mutableCurrentSpecPinned': False, 'priorQualificationHandoff': proof['acceptedQualificationHandoffPath'],
    'priorQualificationHandoffSha256': proof['acceptedQualificationHandoffSha256']})
for name in ['acceptedPreparationReceiptPath', 'acceptedQualificationHandoffPath']:
    path = Path(proof[name]); inputs[str(path)] = sha(path)
history = Path(proof['priorUnexecutedProposalReceiptPath']); inputs[str(history)] = sha(history)
files = {str(path.relative_to(P)): sha(path) for path in sorted(P.rglob('*'))
         if path.is_file() and path.name != 'preparation-receipt.json'
         and not any(part in ['bin', 'obj', '__pycache__'] for part in path.parts)}
write('preparation-receipt.json', {'compileReleased': False, 'executionReleased': False, 'sourceQualificationReady': False,
    'preparedFiles': len(files), 'filesSha256': files, 'inputsSha256': inputs, 'sourceSha256': old['sourceSha256'],
    'app147Sha256': apps, 'compiledInputsSha256': {}, 'compiledOutputsRequireSeparateRootBinding': True,
    'oraclePath': old['oraclePath'], 'oracleSha256': sha(old['oraclePath']), 'rawReferenceCount': len(refs),
    'adapterOriginalSha256': proof['adapterOriginalSha256'], 'adapterCandidateSha256': proof['adapterCandidateSha256'],
    'helperSha256': sha(P / 'CaptureCounters.cs'), 'bootstrapSha256': sha(P / 'AdvisoryWaveBootstrap.cs'),
    'privateExecutorSha256': sha(P / 'ProjectAdvisoryQueryExecutor.private.cs'),
    'allBusinessExecutorBytesAndMarkersUnchanged': True, 'adapterReverseRestoresAcceptedBytes': True,
    'maximumRowsPerCapture': 8, 'plannedBehavioralCases': 49, 'plannedBehavioralComparisons': 196,
    'plannedFreshFlowCanaries': 3, 'retainedTerminalProtocolCases': 4, 'newBoundarySpanCases': 10,
    'plannedProtocolCases': 14, 'plannedControlledProcesses': 23,
    'actualNewBuilds': 0, 'actualNewSyntheticRuns': 0, 'sinkDirectoryAccesses': 0,
    'applicationBuilds': 0, 'hostsStarted': 0, 'nativeAcceptanceClaim': False, 'featureAcceptance': False,
    'mutableCurrentSpecPinned': False})
for name, expected in files.items():
    assert sha(P / name) == expected
for name, expected in inputs.items():
    assert sha(name) == expected
print(json.dumps({'preparationSha256': sha(P / 'preparation-receipt.json'), 'files': len(files),
                  'externalInputs': len(inputs), 'source': 65, 'app': 147, 'rawReferences': len(refs),
                  'allCompileAndExecutionFlagsFalse': True}))
