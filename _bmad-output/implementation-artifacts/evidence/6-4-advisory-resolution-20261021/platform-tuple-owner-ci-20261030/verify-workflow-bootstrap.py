#!/usr/bin/env python3
"""Check the registration-only diff without initializing any dependency."""
import hashlib
import json
from pathlib import Path
import subprocess
import yaml

candidate = '9f43cefe11bd246ce0de1cd0740261b557c0c7a0'
base = 'f60f2c7596b1fa3010c91d1d546c71600da24715'
path = '.github/workflows/tuple-ci.yml'
data = Path(path).read_bytes()
assert data == subprocess.check_output(['git', 'show', f'{candidate}:{path}'])
workflow = yaml.load(data, Loader=yaml.BaseLoader)
assert set(workflow['on']) == {'workflow_dispatch'}
assert workflow['permissions'] == {'contents': 'read'}
assert set(workflow['jobs']) == {'tuple'}
assert workflow['jobs']['tuple']['env']['PLATFORM_CANDIDATE_SHA'] == '${{ inputs.candidate-sha }}'
steps = workflow['jobs']['tuple']['steps']
assert next(s for s in steps if s.get('id') == 'checkout')['with']['ref'] == '${{ inputs.candidate-sha }}'
for step in steps:
    if 'run' in step:
        subprocess.run(['bash', '-n'], input=step['run'], text=True, check=True)
    if 'uses' in step:
        assert len(step['uses'].split('@')[1]) == 40
subprocess.run(['git', 'diff', '--check'], check=True)
changes = subprocess.check_output(['git', 'status', '--porcelain', '--untracked-files=all'], text=True)
assert changes.strip() == '?? ' + path, changes
tracked = subprocess.check_output(['git', 'diff', '--name-only', base], text=True)
assert tracked == '', tracked
print(json.dumps({'base': base, 'candidateToQualify': candidate, 'onlyAddition': path,
                  'workflowSha256': hashlib.sha256(data).hexdigest(),
                  'reviewedWorkflowBytesUnchanged': True, 'yamlAndShellSyntax': 'passed',
                  'manualTriggerOnly': True, 'readOnlyWorkflowPermissions': True,
                  'dependencyChanges': 0, 'localSubmodulesInitialized': 0}, indent=2))
