"""Conditional external observer. Does not start/stop any fixture or build anything.

Start only after explicit parent live-path/ownership release. Attach iff the first
actual MULTIPLE advisory is not a verified positive. Never promote instrumented
results to acceptance. Parent retains ordinary failure and all managed cleanup.
"""
from pathlib import Path
import argparse
import hashlib
import json
import os
import subprocess
import time

ROOT = Path('/home/administrator/projects/hexalith/projects')
OWNER = ROOT / 'references/Hexalith.EventStore'
ENTRY = OWNER / 'src/Hexalith.EventStore/bin/Debug/net10.0/Hexalith.EventStore'
GATEWAY = ENTRY.parent / 'Hexalith.EventStore.Gateway.dll'


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def read_json(path):
    try: return json.loads(path.read_bytes())
    except (FileNotFoundError, ValueError): return None


def start(pid):
    try:
        fields = Path(f'/proc/{pid}/stat').read_text().rsplit(')', 1)[1].split()
        return None if fields[0] == 'Z' else fields[19]
    except FileNotFoundError: return None


def write(path, value):
    temporary = path.with_suffix(path.suffix + '.tmp')
    temporary.write_text(json.dumps(value, indent=2) + '\n'); temporary.chmod(0o600); temporary.replace(path)


def identity(report, ownership, audit, audit_sha, gateway_sha):
    if sha(audit) != audit_sha: raise ValueError('pinned-owner-source-receipt-mismatch')
    session = report.get('ownedAppHostSession', {})
    for role in ('appHost', 'cli'):
        pid = ownership[role + 'Pid']; ticks = str(ownership[role + 'StartTicks'])
        if session.get(role + 'Pid') != pid or str(session.get(role + 'StartTicks')) != ticks or start(pid) != ticks:
            raise ValueError('owned-apphost-identity-mismatch-skip-attach')
    config = Path(ownership['_configuration'])
    if config.parent != args.ownership.parent or not config.parent.name.startswith('supported-project-reads-private-'):
        raise ValueError('private-owner-run-path-mismatch-skip-attach')
    if sha(config) != ownership['configurationSha256'] or session.get('configurationSha256') != ownership['configurationSha256']:
        raise ValueError('private-owner-config-mismatch-skip-attach')
    mode = report.get('runtimeModes', {}).get('eventstore')
    if not isinstance(mode, dict): raise ValueError('eventstore-runtime-identity-not-yet-available')
    pid, ticks = mode['pid'], str(mode['startTicks'])
    if start(pid) != ticks or start(mode['wrapperPid']) != str(mode['wrapperStartTicks']):
        raise ValueError('owned-eventstore-runtime-identity-mismatch-skip-attach')
    # Verify the already-recorded owned wrapper ancestry, without inspecting any other process arguments/environments.
    child = pid; seen = set()
    while child != mode['wrapperPid']:
        if child in seen or child <= 1: raise ValueError('owned-eventstore-wrapper-ancestry-mismatch-skip-attach')
        seen.add(child)
        fields = Path(f'/proc/{child}/stat').read_text().rsplit(')', 1)[1].split(); child = int(fields[1])
    if sha(Path(str(ENTRY) + '.dll')) != mode['assemblySha256'] or sha(GATEWAY) != gateway_sha:
        raise ValueError('owned-eventstore-assembly-hash-mismatch-skip-attach')
    executable = Path(f'/proc/{pid}/exe').readlink()
    if executable == ENTRY: marker = str(ENTRY)
    elif executable.name == 'dotnet': marker = str(ENTRY) + '.dll'
    else: raise ValueError('owned-eventstore-runtime-executable-mismatch-skip-attach')
    pinned = read_json(audit)
    files = [{'Label':'gateway-assembly','Path':str(GATEWAY),'Sha256':gateway_sha},
             {'Label':'eventstore-entry-assembly','Path':str(ENTRY)+'.dll','Sha256':mode['assemblySha256']},
             {'Label':'owner-config','Path':str(config),'Sha256':ownership['configurationSha256']},
             {'Label':'source-audit','Path':str(audit),'Sha256':audit_sha}]
    for index, (relative, digest) in enumerate(sorted(pinned['sourceSha256'].items())):
        path = OWNER / relative
        if sha(path) != digest: raise ValueError('owned-source-freeze-mismatch-skip-attach')
        files.append({'Label':f'source-{index:03d}','Path':str(path),'Sha256':digest})
    return {'Role':'gateway','Resource':'eventstorehost','Pid':pid,'StartTicks':int(ticks),
            'RequiredCommandToken':marker,'ExecutableSha256':sha(Path(f'/proc/{pid}/exe')),'Files':files}


def main(args):
    os.umask(0o077)
    args.watch_output.mkdir(mode=0o700, parents=False, exist_ok=False)
    receipt = {'diagnosticOnly':True,'featureAcceptance':False,'attachCondition':'first actual MULTIPLE is not a verified positive',
               'runtimeResource':'eventstorehost','runtimeReceiptKey':'eventstore','targetSignalsSent':False,
               'parentOwnsFixtureLifecycle':True,'profilerAttached':False,'ordinaryAcceptanceBeforeAttachmentOnly':True}
    begin = time.monotonic(); cutoff = begin + args.timeout
    try:
        while time.monotonic() < cutoff:
            report = read_json(args.evidence / 'run.json'); ownership = read_json(args.ownership)
            if not isinstance(report, dict) or not isinstance(ownership, dict): time.sleep(.1); continue
            file = args.evidence / 'browser-advisory-seed-readiness.jsonl'
            try: data = file.read_bytes()
            except FileNotFoundError: data = b''
            multiple = []
            # Atomic-read retry: ignore unfinished last JSONL row and transient parse failures.
            for line in data.split(b'\n')[:-1]:
                try: row = json.loads(line)
                except ValueError: continue
                if row.get('multiple') is True: multiple.append(row)
            if multiple:
                first = multiple[0]
                positive = (first.get('result') == 'MultipleCandidates' and first.get('incomplete') is False
                            and type(first.get('candidateCount')) is int and first['candidateCount'] >= 2
                            and first.get('positiveCandidatesVerified') is True)
                receipt['firstMultiple'] = {k:first.get(k) for k in ('attempt','incomplete','candidateCount','positiveCandidatesVerified','elapsedMilliseconds') if first.get(k) is None or type(first.get(k)) in (int,float,bool)}
                receipt['firstMultiple']['result'] = first.get('result') if first.get('result') in (None,'SingleCandidate','MultipleCandidates','Empty') else '[unknown]'
                if positive:
                    receipt.update(phase='skipped-positive-multiple', profilerAttached=False)
                    break
                value = identity(report, ownership, args.owner_receipt, args.owner_receipt_sha256, args.gateway_sha256)
                identity_path = args.watch_output / 'private-identity.json';write(identity_path,value)
                receipt.update(phase='failed-multiple-profile-requested', targetPid=value['Pid'], targetStartTicks=value['StartTicks'], identitySha256=sha(identity_path))
                write(args.watch_output / 'watcher-receipt.json',receipt)
                command = ['python3',str(args.wrapper),'--tool',str(args.tool),'--identity',str(identity_path),'--output',str(args.watch_output/'profile')]
                receipt['profileWrapperExitCode'] = subprocess.run(command, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode
                attached = read_json(args.watch_output / 'profile/attached.json')
                receipt['profilerAttached'] = isinstance(attached, dict)
                receipt['phase'] = 'profile-finished'
                break
            if report.get('result') in ('failed','passed'):
                receipt.update(phase='managed-run-ended-before-multiple',profilerAttached=False);break
            time.sleep(.1)
        else: receipt.update(phase='watcher-timeout-no-attach',profilerAttached=False)
    except Exception as error:
        # ValueErrors above contain only fixed local strings; all other exception messages are discarded.
        fixed = {'pinned-owner-source-receipt-mismatch','owned-apphost-identity-mismatch-skip-attach','private-owner-run-path-mismatch-skip-attach','private-owner-config-mismatch-skip-attach','eventstore-runtime-identity-not-yet-available','owned-eventstore-runtime-identity-mismatch-skip-attach','owned-eventstore-wrapper-ancestry-mismatch-skip-attach','owned-eventstore-assembly-hash-mismatch-skip-attach','owned-eventstore-runtime-executable-mismatch-skip-attach','owned-source-freeze-mismatch-skip-attach'}
        receipt.update(phase='guard-refused-no-attach',fixedReason=str(error) if type(error) is ValueError and str(error) in fixed else 'watcher-error',errorType=type(error).__name__)
    finally:
        private_identity = args.watch_output / 'private-identity.json'
        if private_identity.exists(): private_identity.unlink()
        receipt['privateIdentityRemoved'] = not private_identity.exists()
        receipt['watcherWallSeconds'] = time.monotonic() - begin
        write(args.watch_output / 'watcher-receipt.json', receipt)
    print(json.dumps({'phase':receipt['phase'],'profilerAttached':receipt['profilerAttached'],'featureAcceptance':False}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--evidence',type=Path,required=True)
    parser.add_argument('--ownership',type=Path,required=True)
    parser.add_argument('--owner-receipt',type=Path,required=True)
    parser.add_argument('--owner-receipt-sha256',required=True)
    parser.add_argument('--gateway-sha256',required=True)
    parser.add_argument('--watch-output',type=Path,required=True)
    parser.add_argument('--tool',type=Path,required=True)
    parser.add_argument('--wrapper',type=Path,default=Path(__file__).parent/'run_probe.py')
    parser.add_argument('--timeout',type=int,default=1000)
    args = parser.parse_args()
    main(args)
