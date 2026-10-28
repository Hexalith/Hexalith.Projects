"""Exercise the receipt auditor on disposable copies; preserve all real evidence."""
from pathlib import Path
import hashlib, json, shutil, subprocess, sys, tempfile
E = Path(__file__).resolve().parent

def fingerprint(root):
    return {str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in root.rglob('*') if p.is_file()}

with tempfile.TemporaryDirectory(prefix='audit-control-', dir=E.parent) as directory:
    copy = Path(directory)
    shutil.copytree(E, copy, dirs_exist_ok=True)
    def audit(expected_failure=None):
        before = fingerprint(copy)
        result = subprocess.run([sys.executable,str(copy/'verify-preparation.py')],capture_output=True,text=True)
        assert fingerprint(copy) == before, 'The audit mutated its input evidence.'
        if expected_failure is None:
            assert result.returncode == 0, result.stderr
            assert json.loads(result.stdout)['archive_bytes_reverified'] is False
        else:
            assert result.returncode != 0 and expected_failure in result.stderr, result.stderr
    audit()
    checks = [
        ('source-baseline','reviewed-source-baseline.json',lambda b: b.replace(b'"references/Hexalith.Platform/README.md": "',b'"references/Hexalith.Platform/README.md": "00',1),'Source changed: references/Hexalith.Platform/README.md'),
        ('qualification-asset','g4-tool-package-inventory.json',lambda b: b+b'\n',"sha256(inventory_bytes) == asset['digest']"),
        ('owner-failure-cause','platform-pin.xml',lambda b: b.replace(b'fatal: ambiguous argument',b'fatal: unrelated assertion'),"fatal: ambiguous argument 'v4.38.1^{commit}'"),
    ]
    for name, relative, mutate, expected in checks:
        path = copy/relative; original = path.read_bytes(); altered = mutate(original); assert altered != original
        path.write_bytes(altered)
        audit(expected)
        path.write_bytes(original)
        print(name+': correctly rejected; input evidence unchanged by audit')
    audit()
print('Positive offline audit and all three negative controls passed.')
