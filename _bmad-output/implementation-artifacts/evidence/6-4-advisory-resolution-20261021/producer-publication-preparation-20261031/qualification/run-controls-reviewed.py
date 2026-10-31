#!/usr/bin/env python3
"""Replay an explicitly supplied argv list, retaining and propagating every failure.

This helper makes no safety assertion about supplied commands or their transitive
calls. The caller must authorize and audit them; no historical control list runs
by default. It neither stages nor commits anything itself.
"""
import argparse
import json
from pathlib import Path
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('plan', type=Path, help='JSON array of {name, argv, cwd}')
    parser.add_argument('result', type=Path, help='new result path; never overwritten')
    args = parser.parse_args()
    plan = json.loads(args.plan.read_text())
    if not isinstance(plan, list) or not plan:
        parser.error('plan must contain at least one explicitly audited command')
    with args.result.open('x') as stream:
        results = []
        for entry in plan:
            argv = entry['argv']
            if not isinstance(argv, list) or not argv or any(not isinstance(x, str) for x in argv):
                raise ValueError('argv must be a nonempty string array')
            try:
                child = subprocess.run(argv, cwd=entry['cwd'], check=False)
                code = child.returncode
                error = None
            except OSError as exc:
                code, error = 127, str(exc)
            results.append({'name': entry['name'], 'argv': argv, 'cwd': entry['cwd'], 'exitCode': code, 'startError': error})
            print(entry['name'], code, flush=True)
        failed = any(result['exitCode'] != 0 for result in results)
        json.dump({'results': results, 'failed': failed}, stream, indent=2)
        stream.write('\n')
    return 1 if failed else 0


if __name__ == '__main__':
    sys.exit(main())
