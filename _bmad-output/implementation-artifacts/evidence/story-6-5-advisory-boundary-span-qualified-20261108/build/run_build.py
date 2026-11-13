from pathlib import Path
import hashlib, importlib.util, json, os, sys


def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


release_path = Path(sys.argv[1])
assert sha(release_path) == sys.argv[2]
release = json.loads(release_path.read_text())
assert release['compileReleased'] is True and release['executionReleased'] is False
helper = Path('/tmp/story65-gateway-current-cpu-prep-20261108/watcher/run_one.py')
assert sha(helper) == 'b0d1edc30318a87914a2055d38706f04ef56d146182b0fd4b69bba1d1875947c'
assert sha(helper.with_name('common.py')) == '29c8809f64bbf1b682e570942155891d910447e8ef3ecfe66e34d115d8f448f6'
sys.path.insert(0, str(helper.parent))
spec = importlib.util.spec_from_file_location('owned_build', helper)
owned = importlib.util.module_from_spec(spec)
spec.loader.exec_module(owned)


def verify():
    for path, expected in release['pinnedInputsSha256'].items():
        assert sha(path) == expected, path


os.umask(0o077)
verify()
os.chdir(release['cwd'])
for name in release['environmentRemove']:
    os.environ.pop(name, None)
os.environ.update(release['environmentAssignments'])
receipt = {'compileOnly': True, 'executionReleased': False,
           'releaseSha256': sys.argv[2], 'commands': [], 'passed': False}
target = Path(__file__).with_name('receipt.json')
try:
    for command in release['commands']:
        verify()
        result = owned.run_owned(command['argv'], 180, 120, 2 * 1024 ** 3)
        receipt['commands'].append({'name': command['name'], 'argv': command['argv'], **result})
        assert result['exitCode'] == 0 and result['guardReason'] is None, command['name']
        verify()
    receipt['passed'] = True
finally:
    target.write_text(json.dumps(receipt, indent=2) + '\n')
print(json.dumps({'compileOnly': True, 'passed': receipt['passed']}))
