#!/usr/bin/env python3
"""Compare original detached checkout to available historical evidence, read-only."""
import datetime
import hashlib
import json
import os
import pathlib
import subprocess
import sys

here = pathlib.Path(__file__).resolve().parent
history = here.parent.parent / 'producer-publication-preparation-20261031'
root = pathlib.Path('/tmp/story64-builds-producer-2186b9a4')
env = os.environ.copy()
env['GIT_OPTIONAL_LOCKS'] = '0'
commands = []


def git(*arguments):
    argv = ['git', '-C', str(root), *arguments]
    result = subprocess.run(argv, capture_output=True, env=env)
    commands.append({'argv': argv, 'exitCode': result.returncode,
        'stdout': result.stdout.decode(), 'stderr': result.stderr.decode()})
    return result


def sha(data):
    return hashlib.sha256(data).hexdigest()


final = json.loads((history / 'qualification/final-source-state.json').read_text())
selected = json.loads((history / 'qualification/selected-source-state.json').read_text())
inputs = json.loads((history / 'source-inputs.json').read_text())['inputs']
patch_path = history / 'qualification/corrections/loader-parameter-guards.patch'
patch = patch_path.read_bytes()
index_path = pathlib.Path(git('rev-parse', '--path-format=absolute', '--git-path', 'index').stdout.decode().strip())
index_before = sha(index_path.read_bytes())
refs_before = git('for-each-ref', '--format=%(objectname) %(refname)').stdout
head = git('rev-parse', 'HEAD').stdout.decode().strip()
tree = git('rev-parse', 'HEAD^{tree}').stdout.decode().strip()
symbolic = git('symbolic-ref', '-q', 'HEAD')
status = git('status', '--porcelain=v1', '--untracked-files=all').stdout.decode()
cached = git('diff', '--cached', '--exit-code', 'HEAD')
submodules = git('submodule', 'status').stdout.decode()
corrected_name = final['correctionFingerprint']['file']
corrected = (root / corrected_name).read_bytes()
actual_patch = git('diff', '--', corrected_name).stdout
protected = []
for name, previous in inputs.items():
    data = (root / name).read_bytes()
    committed = git('show', head + ':' + name).stdout
    working_object = git('hash-object', '--path=' + name, '--', str(root / name)).stdout.decode().strip()
    old_checkout = selected['inputs'].get(name, {}).get('checkoutSha256')
    protected.append({'file': name, 'currentCheckoutSha256': sha(data),
        'historicalCheckoutSha256': old_checkout,
        'checkoutBytesMatchHistorical': sha(data) == old_checkout if old_checkout else None,
        'historicalCommittedSha256': previous['sha256'],
        'currentCommittedSha256': sha(committed),
        'committedBytesMatchHistorical': sha(committed) == previous['sha256'],
        'historicalGitBlob': previous['blob'], 'currentCheckoutGitObject': working_object,
        'checkoutMatchesHistoricalGitBlobAfterDeclaredFilters': working_object == previous['blob'],
        'limitation': None if old_checkout else 'No historical raw checkout-byte hash in these inputs; canonical Git content is compared separately.'})
refs_after = git('for-each-ref', '--format=%(objectname) %(refname)').stdout
index_after = sha(index_path.read_bytes())
correction = {'file': corrected_name, 'historicalAfterSha256': final['correctionFingerprint']['afterSha256'],
    'currentSha256': sha(corrected), 'matchesHistoricalDirtyBytes': sha(corrected) == final['correctionFingerprint']['afterSha256'],
    'historicalPatchSha256': final['correctionFingerprint']['patchSha256'],
    'retainedPatchSha256': sha(patch), 'currentGitDiffSha256': sha(actual_patch),
    'retainedPatchMatchesCurrentGitDiff': patch == actual_patch}
passed = (head == final['baseRevision'] and status == final['status'] and
    correction['matchesHistoricalDirtyBytes'] and correction['retainedPatchMatchesCurrentGitDiff'] and
    sha(patch) == final['correctionFingerprint']['patchSha256'] and
    all(p['committedBytesMatchHistorical'] and p['checkoutMatchesHistoricalGitBlobAfterDeclaredFilters'] and p['checkoutBytesMatchHistorical'] is not False for p in protected) and
    index_before == index_after and refs_before == refs_after)
report = {'capturedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'scope': 'New read-only monitoring compared with specifically named historical evidence; not a retroactive before-state capture.',
    'checkout': str(root), 'historicalEvidence': {
        'dirtyState': str(history / 'qualification/final-source-state.json'),
        'cleanInputs': str(history / 'qualification/selected-source-state.json'),
        'committedInputs': str(history / 'source-inputs.json'),
        'patch': str(patch_path)},
    'currentGit': {'head': head, 'tree': tree, 'status': status,
        'symbolicHead': symbolic.stdout.decode().strip() or None,
        'symbolicHeadCommandExit': symbolic.returncode, 'cachedDiffExit': cached.returncode,
        'indexPath': str(index_path), 'indexSha256': index_after,
        'refsSha256': sha(refs_after), 'refs': refs_after.decode(), 'submoduleStatus': submodules},
    'historicalComparison': {'headMatchesHistorical': head == final['baseRevision'],
        'dirtyStatusMatchesHistorical': status == final['status'],
        'rawDetachedIndexBeforeState': None, 'detachedRefsBeforeState': None,
        'symbolicHeadBeforeState': None,
        'limitation': 'The historical detached-checkout receipts identify HEAD/status/submodules and selected file hashes, but contain no raw detached-index hash, exact ref snapshot or symbolic-ref command. Current identities do not prove those missing historical states.'},
    'monitoringInterval': {'indexBytesUnchanged': index_before == index_after,
        'refsUnchanged': refs_before == refs_after,
        'meaning': 'Only equality across this new read-only monitoring command; not equality across the earlier work period.'},
    'dirtyCorrection': correction, 'protectedInputs': protected, 'commands': commands,
    'result': 'passed' if passed else 'mismatch'}
print(json.dumps(report, indent=2))
sys.exit(0 if passed else 1)
