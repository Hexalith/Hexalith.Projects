from pathlib import Path
import datetime, fcntl, hashlib, importlib.util, json, os, stat, subprocess, sys, time
from unittest.mock import patch

location = json.loads(Path('/tmp/story61-isolation-location.json').read_text())
root = Path(location['workspace'])
packet = Path(location['packet']) / 'child-exit-fix-20261103'
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
    spec = importlib.util.spec_from_file_location('launcher', root / 'tools/qualification/run_supported_project_reads.py')
    launcher = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(launcher)
    test_command = [sys.executable, '-m', 'unittest', 'discover', '-s', 'tests/tools', '-p', 'test_supported_project_reads_launcher.py']
    test = subprocess.run(test_command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    (packet / 'launcher-controls.log').write_text(test.stdout)
    (packet / 'controls-command.json').write_text(json.dumps({'command': test_command, 'cwd': str(root), 'exitCode': test.returncode}, indent=2) + '\n')
    if test.returncode != 0:
        print(test.stdout[-5000:]); raise SystemExit(test.returncode)
    program = "import os,sys; data=bytearray(128*1024*1024); print('ready',flush=True); sys.stdin.buffer.read(1); os._exit(0)"
    rows = []
    sleep = time.sleep
    for trial in range(40):
        command = [sys.executable, '-u', '-c', program]
        child = subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True)
        try:
            assert child.stdout.readline() == b'ready\n'
            owned = launcher.capture_launcher_identity(child, command)
            child.stdin.write(b'x'); child.stdin.flush()
            row = {'trial': trial, 'pid': child.pid}
            observe_exit = launcher.owned_launcher_exit_started
            def observing_exit(*args):
                value = observe_exit(*args)
                row['kernelExitObserved'] = row.get('kernelExitObserved', False) or value
                return value
            with patch.object(launcher.time, 'sleep', side_effect=lambda _: sleep(0)), patch.object(launcher, 'owned_launcher_exit_started', side_effect=observing_exit):
                try:
                    row['exitCode'] = launcher.wait_owned_launcher(child, owned, {})
                except RuntimeError as failure:
                    row['failure'] = str(failure)
                    row['pollAtFailure'] = child.poll()
                    process = Path('/proc', str(child.pid))
                    try:
                        fields = process.joinpath('stat').read_text().rsplit(')', 1)[1].split()
                        row['procAtFailure'] = {'state': fields[0], 'flags': int(fields[6]), 'startTicks': fields[19], 'commandBytes': len(process.joinpath('cmdline').read_bytes())}
                    except FileNotFoundError:
                        row['procAtFailure'] = None
            row['actualFinalExitCode'] = child.wait(timeout=5)
            rows.append(row)
        finally:
            if child.poll() is None:
                child.kill()
            child.wait(timeout=5)
            for stream in (child.stdin, child.stdout, child.stderr):
                stream.close()
    result = {'diagnosticOnly': True, 'qualificationAccepted': False, 'source': str(root / 'tools/qualification/run_supported_project_reads.py'),
              'sourceSha256': hashlib.sha256((root / 'tools/qualification/run_supported_project_reads.py').read_bytes()).hexdigest(),
              'timingSelection': 'Yield after each exact production wait iteration instead of its 50ms interval, solely to observe the native exit transition.',
              'trials': rows, 'failures': sum('failure' in row for row in rows), 'allOwnedChildrenReaped': True}
    (packet / 'results.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({'trials': len(rows), 'failures': result['failures'], 'failedExamples': [r for r in rows if 'failure' in r][:3]}, indent=2))
except BlockingIOError:
    receipt['refusal'] = 'Cooperative admission busy; no diagnostic process started'
    raise SystemExit(75)
finally:
    if receipt['acquired']:
        receipt['lockIdentityAndMetadataPreserved'] = identity(lock.lstat()) == identity(observed)
    os.close(descriptor)
    receipt['released'] = receipt['acquired']
    receipt['releasedUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    (packet / 'runtime-admission.json').write_text(json.dumps(receipt, indent=2) + '\n')
