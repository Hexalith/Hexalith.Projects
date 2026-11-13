#!/usr/bin/env python3
"""Exact-copy publication after Root review; no spec, Git, build or runtime actions."""
from pathlib import Path
import argparse, hashlib, json, re, shutil
SOURCE = Path(__file__).parent
BASE = Path('/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/evidence')
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
p = argparse.ArgumentParser()
p.add_argument('--manifest-sha256', required=True)
p.add_argument('--destination', required=True)
p.add_argument('--execute', action='store_true')
a = p.parse_args()
assert re.fullmatch('[0-9a-f]{64}', a.manifest_sha256) and sha(SOURCE / 'SHA256SUMS') == a.manifest_sha256
dest = Path(a.destination)
assert dest.is_absolute() and dest.parent.resolve() == BASE.resolve() and re.fullmatch('story-6-5-stream-compare-rejected-[a-z0-9-]+', dest.name) and not dest.exists()
rows = {}
for line in (SOURCE / 'SHA256SUMS').read_text().splitlines():
    h, name = line.split('  ', 1)
    relative = Path(name)
    assert re.fullmatch('[0-9a-f]{64}', h) and not relative.is_absolute() and '..' not in relative.parts and name not in rows
    f = SOURCE / relative
    assert f.is_file() and not f.is_symlink() and sha(f) == h and f.suffix not in ['.dll', '.pdb', '.exe', '.nettrace', '.etlx'] and f.name != 'window.control'
    rows[name] = h
actual = {str(f.relative_to(SOURCE)) for f in SOURCE.rglob('*') if f.is_file() and f.name != 'SHA256SUMS'}
assert actual == set(rows)
if not a.execute:
    print(json.dumps({'execution': False, 'files': len(rows) + 1, 'manifestSha256': a.manifest_sha256, 'rootReviewAndExplicitExecuteRequired': True}))
    raise SystemExit(0)
dest.mkdir(mode=0o755, parents=False, exist_ok=False)
for name, h in rows.items():
    target = dest / name
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(SOURCE / name, target)
    assert sha(target) == h
shutil.copyfile(SOURCE / 'SHA256SUMS', dest / 'SHA256SUMS')
assert sha(dest / 'SHA256SUMS') == a.manifest_sha256
print(json.dumps({'publishedExactCopies': len(rows) + 1, 'destination': str(dest), 'manifestSha256': a.manifest_sha256, 'noSpecGitBuildOrRuntimeActions': True}))
