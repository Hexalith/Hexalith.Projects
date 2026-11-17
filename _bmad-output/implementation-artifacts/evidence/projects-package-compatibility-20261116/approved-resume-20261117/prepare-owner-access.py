"""Prepare a one-file read-only CiCd probe in a disposable owner checkout."""
from pathlib import Path
import hashlib
import io
import json
import os
import subprocess
import sys
import tarfile
import time

OUT = Path(__file__).resolve().parent
ORIGINAL = Path('/home/administrator/projects/hexalith/cicd')
BUILDS = ORIGINAL / 'references/Hexalith.Builds'
TOOL_SOURCE = 'fdda17c0a4538b0f7c99f813c20ca24b49006530'
SCRATCH = Path(os.environ['TMPDIR'])
WORK = SCRATCH / 'cicd'
env = os.environ.copy()
env.pop('GH_TOKEN', None)
env.pop('GITHUB_TOKEN', None)
env['GH_CONFIG_DIR'] = '/home/administrator/.config/gh-cicd-agent'


def run(*command, cwd=WORK):
    value = subprocess.run(command, cwd=cwd, env=env, capture_output=True, timeout=240)
    if value.returncode:
        raise RuntimeError(value.stderr.decode(errors='replace'))
    return value.stdout


run('git', 'clone', '--shared', '--no-checkout', str(ORIGINAL), str(WORK), cwd=SCRATCH)
run('git', 'remote', 'set-url', 'origin', 'https://github.com/Hexalith/Hexalith.CiCd.git')
run('git', 'fetch', '--no-tags', '--no-recurse-submodules', 'origin', 'main')
source = run('git', 'rev-parse', 'origin/main').decode().strip()
run('git', 'sparse-checkout', 'init', '--no-cone')
run('git', 'sparse-checkout', 'set', '--no-cone', '.github/workflows', 'tools', 'templates', 'setup', 'docs', '/AGENTS.md', '/pyproject.toml', '/uv.lock', '/.gitmodules')
run('git', '-c', 'submodule.recurse=false', 'checkout', '--quiet', '--detach', source)
path = '.github/workflows/qualify-package-owner-access.yml'
target = WORK / path
if target.exists():
    raise FileExistsError('Existing owner workflow must be inspected before reuse')
target.parent.mkdir(parents=True, exist_ok=True)
target.write_bytes((OUT / 'owner-access-workflow.yml').read_bytes())
run('actionlint', str(target))
trusted = SCRATCH / 'trusted-builds'
trusted.mkdir()
raw = run('git', 'archive', TOOL_SOURCE, 'Github/guard', 'Github/lib', cwd=BUILDS)
with tarfile.open(fileobj=io.BytesIO(raw)) as archive:
    archive.extractall(trusted, filter='data')
sys.path.insert(0, str(trusted / 'Github/lib'))
from guard_tools import Tools
tools_directory = SCRATCH / 'guard-tools'
tools_directory.mkdir()
message = 'build(cicd): qualify approved package owner access\n'
tools = Tools(tools_directory)
tools.lint_message(message, 'approved package owner read access')
(OUT / 'owner-access-commit-message.txt').write_text(message)
binding = {'owner_base': source, 'work': str(WORK), 'workflow': path,
           'workflow_sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
           'actionlint_exit': 0, 'commitlint_exit': 0, 'message': message,
           'validator_source': TOOL_SOURCE, 'validator_release': 'v6.0.6',
           'validator_archive_sha256': hashlib.sha256(raw).hexdigest(),
           'read_only_probe': True, 'publication_or_module_write': False}
(OUT / 'owner-access-workspace.json').write_text(json.dumps(binding, indent=2) + '\n')
print(json.dumps(binding), flush=True)
deadline = time.monotonic() + 1800
while not (OUT / 'owner-access-workspace-finished').exists():
    if time.monotonic() > deadline:
        raise TimeoutError('Owner access workspace lifetime exceeded')
    time.sleep(1)
print('Owner access work completed; temporary checkout released', flush=True)
