#!/usr/bin/env python3
"""Capture exact argv, cwd, native output bytes and exit status for readiness checks."""
import datetime
import json
from pathlib import Path
import subprocess
import sys

output = Path(__file__).resolve().parent
name, cwd, *command = sys.argv[1:]
started = datetime.datetime.now(datetime.timezone.utc).isoformat()
result = subprocess.run(command, cwd=cwd, capture_output=True, check=False)
stdout = name + '.stdout'
stderr = name + '.stderr'
(output / stdout).write_bytes(result.stdout)
(output / stderr).write_bytes(result.stderr)
index = output / 'commands.json'
rows = json.loads(index.read_text()) if index.exists() else []
rows.append(dict(name=name, cwd=cwd, command=command, started_utc=started,
                 ended_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                 exit_code=result.returncode, stdout=stdout, stderr=stderr))
index.write_text(json.dumps(rows, indent=2) + '\n')
print(result.stdout.decode(errors='replace')[-3500:], end='')
print(result.stderr.decode(errors='replace')[-1500:], end='')
print('EXIT:', result.returncode)
sys.exit(result.returncode)
