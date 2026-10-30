#!/usr/bin/env python3
"""Capture an actual command without reconstructing shell quoting or environment."""
import argparse
import datetime
import json
import os
from pathlib import Path
import subprocess
import time

parser = argparse.ArgumentParser()
parser.add_argument("--name", required=True)
parser.add_argument("--cwd", required=True)
parser.add_argument("--env", action="append", default=[])
parser.add_argument("argv", nargs=argparse.REMAINDER)
args = parser.parse_args()
argv = args.argv[1:] if args.argv[:1] == ["--"] else args.argv
assert argv and "/" not in args.name and ".." not in args.name
receipt = Path(__file__).resolve().parent
paths = {suffix: receipt / (args.name + suffix) for suffix in (".stdout", ".stderr", ".command.json")}
assert not any(path.exists() for path in paths.values()), "Receipt already exists"
explicit = dict(value.split("=", 1) for value in args.env)
environment = os.environ.copy()
environment.update(explicit)
started = datetime.datetime.now(datetime.timezone.utc).isoformat()
clock = time.monotonic()
with paths[".stdout"].open("xb") as stdout, paths[".stderr"].open("xb") as stderr:
    result = subprocess.run(argv, cwd=args.cwd, env=environment, stdout=stdout, stderr=stderr, check=False)
record = {
    "name": args.name,
    "argv": argv,
    "cwd": args.cwd,
    "environment": explicit,
    "inherited_environment": "Existing process environment; no credentials are archived.",
    "started_utc": started,
    "finished_utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "seconds": round(time.monotonic() - clock, 3),
    "exit_code": result.returncode,
    "stdout": paths[".stdout"].name,
    "stderr": paths[".stderr"].name,
}
paths[".command.json"].write_text(json.dumps(record, indent=2) + "\n")
print(json.dumps({key: record[key] for key in ("name", "exit_code", "seconds")}))
raise SystemExit(result.returncode)
