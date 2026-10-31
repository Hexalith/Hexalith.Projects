#!/usr/bin/env bash
set -euo pipefail
# Invoke from the Projects workspace root. Every Git write/install is confined to new scratch.
for story64_tool in git bash node npm python3; do command -v "$story64_tool" >/dev/null; done
python3 -c 'import yaml'
node -e 'const [major,minor]=process.versions.node.split(".").map(Number); if(major<24||(major===24&&minor<10))process.exit(1)'
npm --version | python3 -c 'import sys; raise SystemExit(0 if sys.stdin.read().strip().split(".")[0]=="11" else 1)'
story64_security="$(pwd)/_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/producer-publication-verification-20261031/security"
story64_scratch="$(mktemp -d /var/tmp/story64-security-reconstruction.XXXXXX)"
story64_source="$story64_scratch/source"
story64_capsule="$story64_scratch/capsule"
story64_selected=2186b9a4bc507a116a457c590c80945c2e232cbd
git init --quiet "$story64_source"
git -C "$story64_source" -c submodule.recurse=false fetch --no-recurse-submodules --no-tags --depth=1 https://github.com/Hexalith/Hexalith.Builds.git "$story64_selected"
git -C "$story64_source" -c submodule.recurse=false checkout --detach "$story64_selected"
test "$(git -C "$story64_source" rev-parse HEAD)" = "$story64_selected"
test "$(git -C "$story64_source" rev-parse 'HEAD^{tree}')" = 18d735e56eeb17332e2d2b57a6b116097f42fdec
mkdir "$story64_capsule" "$story64_scratch/home"
cp "$story64_source/package.json" "$story64_source/package-lock.json" "$story64_capsule/"
python3 - "$story64_capsule" <<'PY'
import hashlib,pathlib,sys
root=pathlib.Path(sys.argv[1])
expected={'package.json':'6b24b99692d3ea18a3554e6018a271b951bd5a4c83db5895e1a83a98218d61a9','package-lock.json':'1ff1ac5a67d88d1c16eb10ac4b861e989d5c9c629f73a461ccc151c1f7e87724'}
for name,digest in expected.items():
 if hashlib.sha256((root/name).read_bytes()).hexdigest()!=digest:raise SystemExit('Selected input differs: '+name)
PY
env HOME="$story64_scratch/home" npm --prefix "$story64_capsule" ci --ignore-scripts --no-fund
git -C "$story64_source" apply --check "$story64_security/corrections/fail-closed-npm-audit.patch"
git -C "$story64_source" apply "$story64_security/corrections/fail-closed-npm-audit.patch"
python3 -O "$story64_security/replay-proposed-gate-portable.py" --candidate-root "$story64_source" --capsule "$story64_capsule" --output-dir "$story64_scratch/replay"
python3 - "$story64_scratch" <<'PY'
import json,sys
print(json.dumps({'scratch':sys.argv[1],'scope':'Affected-lock diagnostic reconstruction; no remediation, source publication, release hook or real-repository Git mutation'}))
PY
