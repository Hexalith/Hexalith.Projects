#!/usr/bin/env python3
"""Record a nonpublishing command without replacing any prior output component."""
import argparse
import datetime
import hashlib
import json
import os
import pathlib
import subprocess
import sys
import time


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', required=True)
    parser.add_argument('--cwd', required=True)
    parser.add_argument('--env', action='append', default=[])
    parser.add_argument('--unset-env', action='append', default=[])
    parser.add_argument('argv', nargs=argparse.REMAINDER)
    args = parser.parse_args()
    argv = args.argv[1:] if args.argv[:1] == ['--'] else args.argv
    if not argv:
        parser.error('A child command is required')
    prefix = pathlib.Path(args.output)
    paths = {name: prefix.with_suffix(suffix) for name, suffix in {
        'receipt': '.command.json', 'stdout': '.stdout.log', 'stderr': '.stderr.log'}.items()}
    for path in paths.values():
        if os.path.lexists(path):
            raise SystemExit('Refusing existing output component: ' + str(path))
    prefix.parent.mkdir(parents=True, exist_ok=True)
    env = os.environ.copy()
    for name in args.unset_env:
        env.pop(name, None)
    explicit = dict(value.split('=', 1) for value in args.env)
    env.update(explicit)
    public_names = ['HOME', 'PATH', 'DOTNET_ROOT', 'DOTNET_CLI_HOME',
        'DOTNET_PROCESSOR_COUNT', 'DOTNET_CLI_TELEMETRY_OPTOUT', 'DOTNET_NOLOGO',
        'NUGET_PACKAGES', 'NO_COLOR', 'CI', 'GITHUB_ACTIONS', 'GITHUB_REF', 'GITHUB_SHA']
    started = datetime.datetime.now(datetime.timezone.utc).isoformat()
    clock = time.monotonic()
    # Exclusive creation also protects against a competing writer after preflight.
    with paths['receipt'].open('x', encoding='utf-8') as receipt_file:
        with paths['stdout'].open('xb') as stdout, paths['stderr'].open('xb') as stderr:
            launch_error = None
            child_exit = None
            try:
                child = subprocess.run(argv, cwd=args.cwd, env=env, stdout=stdout,
                    stderr=stderr, check=False)
                child_exit = child.returncode
                exit_code = child_exit if child_exit >= 0 else 128 - child_exit
                status = 'completed' if child_exit == 0 else 'child-failed'
            except (OSError, ValueError) as error:
                launch_error = {'type': type(error).__name__, 'message': str(error),
                    'errno': getattr(error, 'errno', None),
                    'filename': getattr(error, 'filename', None)}
                exit_code = 127 if isinstance(error, FileNotFoundError) else 126
                status = 'launch-failed'
                stderr.write(('recorder launch failure: ' + json.dumps(launch_error) + '\n').encode())
        receipt = {'schema': 'hexalith.command-capture.reviewed.v1', 'status': status,
            'argv': argv, 'cwd': args.cwd,
            'environment': {name: env[name] for name in public_names if name in env},
            'explicitEnvironment': explicit, 'unsetEnvironment': args.unset_env,
            'startedUtc': started,
            'finishedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
            'elapsedSeconds': round(time.monotonic() - clock, 3),
            'exitCode': exit_code, 'childExitCode': child_exit, 'launchError': launch_error}
        for name in ['stdout', 'stderr']:
            data = paths[name].read_bytes()
            receipt[name] = {'file': paths[name].name,
                'sha256': hashlib.sha256(data).hexdigest(), 'sizeBytes': len(data)}
        json.dump(receipt, receipt_file, indent=2)
        receipt_file.write('\n')
    print(json.dumps({'receipt': str(paths['receipt']), 'status': status,
        'exitCode': exit_code}), flush=True)
    return exit_code


if __name__ == '__main__':
    raise SystemExit(main())
