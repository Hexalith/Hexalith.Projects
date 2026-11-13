"""External scratch supervisor. Compile only after explicit parent source/build release.

Run only after an actual MULTIPLE readiness failure in the parent's next ordinary live.
No target signals, environment reads/changes, forced GC, heap dumps or network capture.
"""
from pathlib import Path
import argparse
import hashlib
import json
import os
import subprocess
import time
import stat
import threading
import re

SANITIZED_ALLOWLIST = {
    'identity-checks.json', 'attached.json', 'collection-receipt.json',
    'allocation-samples.jsonl', 'allocation-summary.json', 'supervisor-receipt.json',
}


def file_hash(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def remove_private_derivatives(output):
    """Only this fresh owned directory; no symlink target is followed or hashed."""
    removed = []
    if not output.exists() and not output.is_symlink():
        return {'removed': removed, 'allNonAllowlistedPrivateFilesAbsent': True}
    root_stat = output.lstat()
    if not stat.S_ISDIR(root_stat.st_mode) or stat.S_ISLNK(root_stat.st_mode):
        return {'removed': removed, 'allNonAllowlistedPrivateFilesAbsent': False, 'fixedReason': 'owned-output-root-not-directory'}
    for current, directories, files, descriptor in os.fwalk(output, topdown=False, follow_symlinks=False):
        at_root = Path(current) == output
        for name in files:
            info = os.stat(name, dir_fd=descriptor, follow_symlinks=False)
            if at_root and name in SANITIZED_ALLOWLIST and stat.S_ISREG(info.st_mode):
                continue
            entry = {'ordinal': len(removed) + 1, 'kind': 'symlink' if stat.S_ISLNK(info.st_mode) else 'file' if stat.S_ISREG(info.st_mode) else 'special', 'bytes': info.st_size}
            if name in ('allocations.nettrace', 'allocations.etlx'): entry['knownName'] = name
            if stat.S_ISREG(info.st_mode):
                # O_NOFOLLOW prevents a changed leaf from redirecting the hash read.
                fd = os.open(name, os.O_RDONLY | os.O_NOFOLLOW, dir_fd=descriptor)
                with os.fdopen(fd, 'rb') as stream:
                    entry['sha256'] = hashlib.file_digest(stream, 'sha256').hexdigest()
            os.unlink(name, dir_fd=descriptor)
            try: os.stat(name, dir_fd=descriptor, follow_symlinks=False)
            except FileNotFoundError: entry['absenceVerified'] = True
            else: entry['absenceVerified'] = False
            removed.append(entry)
        for name in directories:
            info = os.stat(name, dir_fd=descriptor, follow_symlinks=False)
            if stat.S_ISLNK(info.st_mode):
                os.unlink(name, dir_fd=descriptor)
                removed.append({'ordinal': len(removed) + 1, 'kind': 'symlink-directory', 'targetFollowedOrHashed': False, 'absenceVerified': True})
            else:
                os.rmdir(name, dir_fd=descriptor)
    remaining = list(output.iterdir())
    return {'removed': removed,
            'allNonAllowlistedPrivateFilesAbsent': all(p.name in SANITIZED_ALLOWLIST and stat.S_ISREG(p.lstat().st_mode) and not p.is_symlink() for p in remaining),
            'allSubdirectoriesRemoved': all(not p.is_dir() for p in remaining),
            'symlinkTargetsFollowedOrHashed': False}


def write(path, value):
    temporary = path.with_suffix(path.suffix + '.tmp')
    temporary.write_text(json.dumps(value, indent=2) + '\n')
    temporary.chmod(0o600)
    temporary.replace(path)


def run_owned(command, timeout, cpu_seconds, rss_bytes):
    import resource
    def limits():
        resource.setrlimit(resource.RLIMIT_CPU, (cpu_seconds, cpu_seconds + 1))
    # Drain unknown output without retaining it. Accept only the collector's closed JSON failure shape.
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE, preexec_fn=limits)
    captured = []
    def drain(stream):
        prefix = bytearray()
        while True:
            block = stream.read(4096)
            if not block: break
            if len(prefix) < 8192: prefix.extend(block[:8192-len(prefix)])
        captured.append(bytes(prefix))
    readers = [threading.Thread(target=drain, args=(stream,), daemon=True) for stream in (process.stdout, process.stderr)]
    for reader in readers: reader.start()
    started = time.monotonic()
    peak = 0
    reason = None
    while process.poll() is None:
        try:
            values = dict(line.split(':', 1) for line in Path(f'/proc/{process.pid}/status').read_text().splitlines() if ':' in line)
            rss = int(values.get('VmRSS', '0 kB').split()[0]) * 1024
            peak = max(peak, rss)
            if rss > rss_bytes: reason = 'collector-rss-guard'
        except FileNotFoundError:
            pass
        if time.monotonic() - started > timeout: reason = 'collector-wall-guard'
        if reason:
            # Only this directly spawned collector/parser. Never the target Gateway, launcher or app hosts.
            process.terminate()
            try: process.wait(timeout=3)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
            break
        time.sleep(.1)
    for reader in readers: reader.join(timeout=2)
    fixed_failures = []
    reasons = {'target-already-exited', 'identity-contract-invalid', 'target-identity-changed',
               'unsafe-or-unexpected-owned-gateway-arguments-skip-attach', 'input-label-invalid',
               'required-identity-input-missing', 'input-hash-mismatch',
               'owned-gateway-map-proof-unavailable-skip-attach', 'owned-gateway-assembly-not-mapped-skip-attach', 'collector-error'}
    for prefix in captured:
        for line in prefix.splitlines():
            try: value = json.loads(line)
            except (ValueError, UnicodeDecodeError): continue
            if isinstance(value, dict) and set(value) == {'diagnosticOnly', 'errorType', 'fixedReason'} and value['diagnosticOnly'] is True and value['fixedReason'] in reasons and re.fullmatch('[A-Za-z0-9]{1,80}', value['errorType']):
                fixed_failures.append(value)
    return {'exitCode': process.wait(), 'wallSeconds': time.monotonic() - started,
            'peakObservedRssBytes': peak, 'guardReason': reason,
            'cpuHardLimitSeconds': cpu_seconds + 1, 'rssGuardBytes': rss_bytes,
            'onlyDirectlyOwnedDiagnosticChildSignaled': bool(reason), 'fixedFailures': fixed_failures,
            'unrecognizedStdoutStderrRetained': False}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--tool', type=Path, required=True)
    parser.add_argument('--identity', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    os.umask(0o077)
    if args.output.exists(): raise SystemExit('output-already-exists')
    receipt = {'diagnosticOnly': True, 'featureAcceptance': False,
               'nativeAcceptanceCases': 0, 'browserAcceptanceCases': 0,
               'ordinaryRunResultUntouched': True,
               'toolSha256': file_hash(args.tool),
               'identitySha256': file_hash(args.identity),
               'noTargetSignalsOrGcRequests': True}
    try:
        receipt['collect'] = run_owned(['dotnet', str(args.tool), 'collect', str(args.identity), str(args.output)], 55, 40, 768 * 1024 * 1024)
        if (args.output / 'collection-receipt.json').exists() and (args.output / 'allocations.nettrace').exists():
            receipt['analyze'] = run_owned(['dotnet', str(args.tool), 'analyze', str(args.output)], 60, 45, 1024 * 1024 * 1024)
    finally:
        removal = remove_private_derivatives(args.output)
        receipt['privateRuntimeMetadataRemoval'] = removal
        receipt['sanitizedSummaryExists'] = (args.output / 'allocation-summary.json').exists()
        receipt['incomplete'] = not receipt['sanitizedSummaryExists'] or not removal['allNonAllowlistedPrivateFilesAbsent'] or any(receipt.get(stage, {}).get('guardReason') or receipt.get(stage, {}).get('exitCode', 1) != 0 for stage in ('collect', 'analyze'))
        if args.output.exists(): write(args.output / 'supervisor-receipt.json', receipt)
        else: write(args.output.with_suffix('.failure.json'), receipt)
    print(json.dumps({'diagnosticOnly': True, 'featureAcceptance': False, 'incomplete': receipt['incomplete'], 'privateRuntimeMetadataAbsent': removal['allNonAllowlistedPrivateFilesAbsent']}))
    return 2 if receipt['incomplete'] else 0


if __name__ == '__main__':
    raise SystemExit(main())
