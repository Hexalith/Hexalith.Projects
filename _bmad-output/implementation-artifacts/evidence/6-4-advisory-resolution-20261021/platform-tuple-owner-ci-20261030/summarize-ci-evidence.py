#!/usr/bin/env python3
"""Bind retained actual CI receipts to their producing run and selected source."""
import argparse
import hashlib
import json
from pathlib import Path
import re

parser = argparse.ArgumentParser()
parser.add_argument('--root', type=Path, required=True)
parser.add_argument('--run', type=int, required=True)
parser.add_argument('--job', type=int, required=True)
parser.add_argument('--candidate', required=True)
parser.add_argument('--workflow-head', required=True)
args = parser.parse_args()
output = Path(__file__).resolve().parent / f'run-{args.run}-summary.json'
assert not output.exists()
path = next(args.root.rglob('tuple-ci-result.json'))
root = path.parent
result = json.loads(path.read_bytes())
bootstrap = json.loads(next(args.root.rglob('bootstrap.json')).read_bytes())
identity = json.loads((root / 'builds/artifact-identity.json').read_bytes())
assert result['platformRevision'] == identity['platformRevision'] == args.candidate
assert bootstrap['context']['PLATFORM_CANDIDATE_SHA'] == args.candidate
assert result['platformSourceState']['dirty'] is False
assert identity['platformSourceState']['dirty'] is False
assert result['runner']['GITHUB_RUN_ID'] == str(args.run)
assert result['runner']['GITHUB_RUN_ATTEMPT'] == '1'
assert identity['version'] == '4.38.3'
assert identity['buildsRevision'] == 'd1931b1e803144048d927fff16cb8119afde541e'
archive = root / 'builds/hexalith.builds.module.cli.4.38.3.nupkg'
assert hashlib.sha256(archive.read_bytes()).hexdigest() == '6c074186a9bbfb66c558fe5d1090e1e35fdd58577c0396266074bd1cea8919fd'
commands = {x['name']: x for x in result['commands']}
binding = (root / 'binding-regressions.stderr').read_text()
assert commands['binding-regressions']['exitCode'] == 0 and 'Ran 14 tests' in binding and 'OK' in binding
assert commands['restore']['exitCode'] == 0
graphs = [x for x in commands if re.fullmatch(r'graph-\d+', x)]
assets = list((root / 'assets').glob('*.project.assets.json'))
assert len(graphs) == len(assets) == 27
summary = {'runId': args.run, 'attempt': 1, 'jobId': args.job, 'workflowHead': args.workflow_head,
           'actualPlatformCheckout': args.candidate, 'sourceClean': True,
           'sourceInputDigest': result['platformSourceState']['inputDigest'],
           'toolVersion': identity['version'], 'buildsSource': identity['buildsRevision'],
           'archiveSha256': identity['archiveSha256'], 'archiveAndSignaturePreparationExit': commands['published-artifacts']['exitCode'],
           'projectGraphs': len(graphs), 'durableRestoredAssetGraphs': len(assets), 'bindingControlsPassed': 14,
           'restoreExit': commands['restore']['exitCode'], 'buildExit': commands['build']['exitCode'],
           'requiredTests': result['tests'], 'status': result['status'], 'blocker': result.get('blocker'),
           'bootstrapStatus': bootstrap['status'], 'bootstrapOutcomes': bootstrap['outcomes'],
           'runner': result['runner'], 'candidateAcceptance': result['candidateAcceptance'],
           'runtimeQualified': False, 'publishedTupleAccepted': False}
if result['status'] == 'passed':
    assert sum(int(x['counts']['passed']) for x in result['tests']) == 82
    assert commands['build']['exitCode'] == 0
    assert '0 Warning(s)' in (root / 'build.stdout').read_text()
output.write_text(json.dumps(summary, indent=2) + '\n')
print(json.dumps(summary, indent=2))
