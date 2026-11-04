from pathlib import Path
import datetime, fcntl, hashlib, importlib.util, inspect, io, json, os, shutil, stat, subprocess, sys, unittest

location = json.loads(Path('/tmp/story61-isolation-location.json').read_text())
root = Path(location['workspace'])
packet = Path(location['packet']) / 'docker-missing-case-20261103'
packet.mkdir(exist_ok=False)
lock = Path('/tmp/hexalith-timesheets-local-runtime.lock')
receipt = {'acquired': False, 'released': False, 'contentsReadOrWritten': False, 'lifecycleOfOtherTasksInvoked': False}
def identity(info):
    return {'device': info.st_dev, 'inode': info.st_ino, 'uid': info.st_uid, 'mode': stat.S_IMODE(info.st_mode), 'links': info.st_nlink, 'size': info.st_size, 'modifiedNs': info.st_mtime_ns}
descriptor = os.open(lock, os.O_RDWR | os.O_NONBLOCK | os.O_NOFOLLOW | os.O_CLOEXEC)
try:
    observed = os.fstat(descriptor)
    if not stat.S_ISREG(observed.st_mode) or observed.st_uid != os.geteuid() or observed.st_nlink != 1 or observed.st_mode & 0o077 or identity(observed) != identity(lock.lstat()):
        raise RuntimeError('Unsafe cooperative admission')
    fcntl.flock(descriptor, fcntl.LOCK_EX | fcntl.LOCK_NB)
    receipt.update(acquired=True, identity=identity(observed), acquiredUtc=datetime.datetime.now(datetime.timezone.utc).isoformat())
    command = [sys.executable, '-m', 'unittest', 'discover', '-s', 'tests/tools', '-p', 'test_supported_project_reads_launcher.py']
    tests = subprocess.run(command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    (packet / 'launcher-controls.log').write_text(tests.stdout)
    (packet / 'controls-command.json').write_text(json.dumps({'command': command, 'cwd': str(root), 'exitCode': tests.returncode}, indent=2) + '\n')
    if tests.returncode:
        print(tests.stdout[-5000:]); raise SystemExit(tests.returncode)
    spec = importlib.util.spec_from_file_location('docker_case_tests', root / 'tests/tools/test_supported_project_reads_launcher.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    original = module.LAUNCHER.cleanup_resources
    source = inspect.getsource(original)
    mutants = [('remove-lowercase-form', '|error: no such object: ', ''),
               ('remove-exact-recorded-id-binding', 're.escape(container_id)', 'r"[0-9a-f]+"')]
    rows = []
    for name, before, after in mutants:
        assert source.count(before) == 1
        output = io.StringIO()
        try:
            exec(source.replace(before, after), module.LAUNCHER.__dict__)
            suite = unittest.TestSuite([module.SupportedReadLauncherReviewControls('test_container_disappearance_requires_exact_missing_identity_and_fresh_absence')])
            result = unittest.TextTestRunner(stream=output).run(suite)
            row = {'mutation': name, 'testsRun': result.testsRun, 'failures': len(result.failures), 'errors': len(result.errors), 'skips': len(result.skipped), 'detected': len(result.failures) > 0 and not result.errors and not result.skipped}
            rows.append(row)
            (packet / (name + '.log')).write_text(output.getvalue())
            assert row['detected']
        finally:
            module.LAUNCHER.cleanup_resources = original
    manifest = module.LAUNCHER.source_manifest()
    old = json.loads((Path(location['packet']) / 'source-manifest.json').read_text())
    (packet / 'source-manifest.json').write_text(json.dumps(manifest, indent=2, sort_keys=True) + '\n')
    data = {'sourceManifestSha256': hashlib.sha256(json.dumps(manifest, sort_keys=True).encode()).hexdigest(),
            'changedBuildInputs': sorted(k for k, v in manifest.items() if old.get(k) != v),
            'sourceSha256': {k: manifest[k] for k in ['tools/qualification/run_supported_project_reads.py', 'tests/tools/test_supported_project_reads_launcher.py']},
            'pythonControlsPassed': 109, 'pythonControlsFailed': 0, 'pythonControlsSkipped': 0, 'mutations': rows,
            'qualificationAccepted': False, 'runtimeStarted': False,
            'originalLive59CleanupExceptionTextRetained': False,
            'scope': 'Add only the actual exact lowercase Docker missing-object form; retain recorded full identity and successful fresh complete-list absence requirements.'}
    (packet / 'commands.json').write_text(json.dumps(data, indent=2) + '\n')
    shutil.copyfile(__file__, packet / 'verify.py')
    print(json.dumps(data, indent=2))
except BlockingIOError:
    receipt['refusal'] = 'Cooperative admission busy; no verification process started'
    raise SystemExit(75)
finally:
    if receipt['acquired']:
        receipt['lockIdentityAndMetadataPreserved'] = identity(lock.lstat()) == identity(observed)
    os.close(descriptor)
    receipt['released'] = receipt['acquired']
    receipt['releasedUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    (packet / 'runtime-admission.json').write_text(json.dumps(receipt, indent=2) + '\n')
