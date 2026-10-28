"""Exercise review fixes using reversible faults in the disposable workspace only."""
from pathlib import Path
import datetime
import hashlib
import json
import os
import shutil
import subprocess

root = Path(__file__).resolve().parent
workspace = Path(json.loads((root/'selected-inputs.json').read_text())['workspace'])
command = ['python3',str(root/'verify-evidence.py'),'--live']
observations = []

def attempt(label, expected, extra_env=None):
    result = subprocess.run(command,env={**os.environ,**(extra_env or {})},capture_output=True,text=True)
    observations.append(dict(label=label,argv=command,cwd=os.getcwd(),environment=extra_env or {},
        observed_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),exit_code=result.returncode,
        stdout=result.stdout,stderr=result.stderr))
    if (result.returncode == 0) != (expected == 0):
        raise RuntimeError(label+': unexpected audit result')

cache = root/'__pycache__'
if cache.exists():
    shutil.rmtree(cache)  # Only this task's helper bytecode, never an original repository file.
initial = {str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest()
           for p in root.rglob('*') if p.is_file()}
attempt('positive-live-read-only',0)
assert not cache.exists(), 'Live audit wrote Python bytecode'
assert initial == {str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest()
                   for p in root.rglob('*') if p.is_file()}, 'Live audit modified evidence'
attempt('optimized-execution-refused',1,{'PYTHONOPTIMIZE':'1'})

source = workspace/'references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/ReviewAuditControl.cs'
assert not source.exists()
try:
    source.write_text('// Temporary verifier fault control. Never compiled.\n')
    attempt('added-untracked-source-refused',1)
finally:
    source.unlink()

binary = workspace/'references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/bin/Debug/net10.0/Hexalith.Conversations.Server.dll'
original = binary.read_bytes()
try:
    binary.write_bytes(original+b'audit-control')
    attempt('production-dependency-byte-change-refused',1)
finally:
    binary.write_bytes(original)

index = workspace/'.git/index'
original_index = index.read_bytes()
try:
    argv = ['git','update-index','--cacheinfo','160000,1663028fb6d9f46a0bf17919edeb21bb99be1587,references/Hexalith.Commons']
    subprocess.run(argv,cwd=workspace,check=True)
    attempt('isolated-gitlink-drift-refused',1)
finally:
    index.write_bytes(original_index)
attempt('positive-live-after-restoration',0)
(root/'audit-controls.json').write_text(json.dumps(observations,indent=2)+'\n')
print('PASS: read-only and positive controls; optimization, added source, dependency bytes and index drift rejected')
