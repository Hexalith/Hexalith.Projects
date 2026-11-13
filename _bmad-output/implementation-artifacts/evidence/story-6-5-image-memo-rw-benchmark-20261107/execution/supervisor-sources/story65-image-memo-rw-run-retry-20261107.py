from pathlib import Path
import datetime
import hashlib
import json
import os
import resource
import shutil
import signal
import stat
import subprocess
import time

BASE = Path('/tmp/story65-image-memo-rw-prep-20261107')
ROOT = Path('/home/administrator/projects/hexalith/projects')
PREPARATION_SHA = 'ff43de5c3ecd8faa1583faa6f384f298fa397d08ed02d754fd84ec091eca24a2'
ATTEMPT = Path('/tmp/story65-image-memo-rw-execution-20261107/attempt-2')
RESULT = Path('/tmp/story65-image-memo-rw-results-retry-20261107')

def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def save(path, value):
    temporary = path.with_suffix(path.suffix + '.tmp')
    temporary.write_text(json.dumps(value, indent=2) + '\n')
    temporary.replace(path)

def identity(pid):
    try:
        fields = Path('/proc', str(pid), 'stat').read_text().rsplit(')', 1)[1].split()
        return None if fields[0] == 'Z' else (fields[19], int(fields[2]))
    except FileNotFoundError:
        return None

def group_members(group):
    return [{'pid': int(path.name), 'startTicks': value[0]} for path in Path('/proc').iterdir()
            if path.name.isdigit() and (value := identity(int(path.name))) is not None and value[1] == group]

def inputs(preparation):
    values = dict(preparation['currentExternalInputsSha256'])
    values.update({str(BASE / path): digest for path, digest in preparation['filesSha256'].items()})
    values[str(BASE / 'preparation-receipt.json')] = PREPARATION_SHA
    assert all(sha(path) == digest for path, digest in values.items())
    return values

def run(command, stage, build):
    env = os.environ.copy()
    removed = []
    for key in ('CustomBeforeDirectoryBuildProps', 'CustomAfterMicrosoftCommonProps', 'CustomBeforeDirectoryBuildTargets', 'CustomAfterDirectoryBuildTargets', 'AfterMicrosoftNETSdkTargets', 'HEXALITH_SOURCE_MAPPING_HASH', 'Configuration', 'CustomBeforeMicrosoftCommonTargets', 'DOTNET_PROCESSOR_COUNT'):
        if key in env:
            removed.append(key)
        env.pop(key, None)
    env.update(MSBUILDDISABLENODEREUSE='1', DOTNET_CLI_USE_MSBUILD_SERVER='0')
    if build:
        env['DOTNET_PROCESSOR_COUNT'] = '2'
    cpu_limit = 120 if build else 300
    wall_limit = 180 if build else 300
    def limits():
        resource.setrlimit(resource.RLIMIT_CPU, (cpu_limit, cpu_limit + 1))
    stdout = ATTEMPT / (stage + '.stdout.log')
    stderr = ATTEMPT / (stage + '.stderr.log')
    record = {'command': command, 'cwd': str(BASE / 'harness'), 'startedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'sourceAndAppBuilds': False, 'buildProcessorOverride': 2 if build else None, 'runtimeNaturalProcessorsExpected': 14 if not build else None, 'environmentKeysRemoved': removed, 'cpuHardLimitSeconds': cpu_limit + 1, 'wallGuardSeconds': wall_limit, 'rssGuardBytes': 2 * 1024**3, 'onlyDirectlyOwnedStandaloneDiagnosticChildMayBeSignaled': True, 'targetSignalsSent': False}
    child = None
    pidfd = None
    peak = 0
    reason = None
    began = time.monotonic()
    try:
        # This supervisor never starts reader threads; every preexec fork occurs in one thread.
        with stdout.open('wb') as out, stderr.open('wb') as err:
            child = subprocess.Popen(command, cwd=BASE / 'harness', env=env, stdout=out, stderr=err, start_new_session=True, preexec_fn=limits)
            record['pid'] = child.pid
            record['startTicks'] = None
            save(ATTEMPT / (stage + '.json'), record)
            pidfd = os.pidfd_open(child.pid)
            value = identity(child.pid)
            record['startTicks'] = value[0] if value else None
            while child.poll() is None:
                try:
                    fields = dict(line.split(':', 1) for line in Path('/proc', str(child.pid), 'status').read_text().splitlines() if ':' in line)
                    peak = max(peak, int(fields.get('VmRSS', '0 kB').split()[0]) * 1024)
                except FileNotFoundError:
                    pass
                if peak > 2 * 1024**3:
                    reason = 'owned-diagnostic-rss-guard'
                if time.monotonic() - began > wall_limit:
                    reason = 'owned-diagnostic-wall-guard'
                if reason:
                    break
                time.sleep(.1)
    finally:
        if child is not None:
            if child.poll() is None:
                if pidfd is None:
                    pidfd = os.pidfd_open(child.pid)
                signal.pidfd_send_signal(pidfd, signal.SIGTERM)
                try:
                    child.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    signal.pidfd_send_signal(pidfd, signal.SIGKILL)
                    child.wait(timeout=5)
                record['ownedStandaloneDiagnosticChildSignaled'] = True
            record['exitCode'] = child.wait()
            until = time.monotonic() + 10
            while group_members(child.pid) and time.monotonic() < until:
                time.sleep(.1)
            record['remainingOwnedDiagnosticGroup'] = group_members(child.pid)
            record['ownedDiagnosticGroupAbsent'] = not record['remainingOwnedDiagnosticGroup']
        if pidfd is not None:
            os.close(pidfd)
        record.update(wallSeconds=time.monotonic() - began, peakObservedRssBytes=peak, guardReason=reason, stdoutSha256=sha(stdout), stderrSha256=sha(stderr))
        save(ATTEMPT / (stage + '.json'), record)
    print(json.dumps({'stage': stage, 'exitCode': record['exitCode'], 'groupAbsent': record['ownedDiagnosticGroupAbsent'], 'guardReason': reason}), flush=True)
    assert record['exitCode'] == 0 and record['ownedDiagnosticGroupAbsent'] and reason is None
    return record

def main():
    os.umask(0o077)
    assert not ATTEMPT.exists() and not RESULT.exists()
    preparation_path = BASE / 'preparation-receipt.json'
    assert sha(preparation_path) == PREPARATION_SHA
    preparation = json.loads(preparation_path.read_text())
    commands = json.loads((BASE / 'harness/commands.json').read_text())['plannedCommands']
    assert len(commands) == 3 and all(command[0] == 'dotnet' for command in commands)
    before = inputs(preparation)
    ATTEMPT.mkdir(mode=0o700, parents=True)
    for relative in preparation['filesSha256']:
        target = ATTEMPT / 'prepared-source-snapshot' / relative
        target.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
        shutil.copy2(BASE / relative, target)
    save(ATTEMPT / 'inputs-before.json', before)
    release = {'releasedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'preparationSha256': PREPARATION_SHA, 'rootAndIndependentReaderAllSourceAndInputPinsVerified': True, 'allowStandaloneRestoreBuildLoadBenchmark': True, 'originalDisabledPreparationPreserved': True, 'productionAppBuildNativeTestsHostsProfilesOrSourceEditsAuthorized': False, 'commands': commands, 'runtimeWallBoundSeconds': 300, 'noAutomaticFixOrRetry': True, 'supervisorPid': os.getpid(), 'supervisorStartTicks': identity(os.getpid())[0]}
    save(ATTEMPT / 'execution-release.json', release)
    passed = False
    records = []
    try:
        for index, command in enumerate(commands):
            inputs(preparation)
            records.append(run(command, f'{index + 1:02d}-' + ('restore', 'build', 'benchmark')[index], index < 2))
            if index == 1:
                binary = BASE / 'harness/bin/Debug/net10.0/Hexalith.EventStore.ServiceDefaults.dll'
                assert sha(binary) == preparation['actualCompiledOracleSdkSha256']
        outcome = json.loads((RESULT / 'result.json').read_text())
        assert outcome['Completed'] and outcome['AllPassed'] and outcome['RawSamples'] == 324
        cohort = json.loads((RESULT / 'inventory.json').read_text())['Files']
        external = preparation['currentExternalInputsSha256']
        assert len(cohort) == 66
        for image in cohort:
            assert image['Path'] in external and external[image['Path']] == image['Sha256'] and sha(image['Path']) == image['Sha256']
        passed = True
    finally:
        after = inputs(preparation)
        assert before == after
        save(ATTEMPT / 'inputs-after.json', after)
        save(ATTEMPT / 'command-receipt.json', {'passed': passed, 'preparationSha256': PREPARATION_SHA, 'steps': records, 'all658DistinctInputsUnchanged': before == after, 'distinctInputPaths': len(before), 'noApplicationHostsOrSourceEdits': True, 'noAutomaticFixOrRetry': True})
    return 0 if passed else 1

if __name__ == '__main__':
    raise SystemExit(main())
