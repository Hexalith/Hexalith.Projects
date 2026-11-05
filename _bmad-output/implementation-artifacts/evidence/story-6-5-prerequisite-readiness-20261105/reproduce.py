#!/usr/bin/env python3
"""Compile unchanged FrontComposer sources in scratch and test circuit credential isolation.

Exit 1 means the expected isolation invariant failed; no network or real credential is used.
"""
import argparse
import json
from pathlib import Path
import shutil
import subprocess

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--workspace", type=Path, required=True)
parser.add_argument("--output", type=Path, required=True)
args = parser.parse_args()
workspace = args.workspace.resolve()
output = args.output.resolve()
output.mkdir(parents=True, exist_ok=False)
for source in (Path(__file__).resolve().parent / "repro").iterdir():
    shutil.copyfile(source, output / source.name)
owner = workspace / "references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Services/Auth"
commands = [["dotnet", "build", str(output / "AuthIsolation.csproj"), "--configuration", "Debug", "-m:1", "-p:NuGetAudit=false", "-p:FrontComposerAuthRoot=" + str(owner)], ["dotnet", str(output / "bin/Debug/net10.0/AuthIsolation.dll")]]
receipts = []
for command, name in zip(commands, ("build.txt", "run.txt")):
    completed = subprocess.run(command, cwd=workspace, capture_output=True, text=True, check=False)
    (output / name).write_text(completed.stdout + completed.stderr)
    receipts.append({"command": command, "exit_code": completed.returncode, "output": name})
    if name == "build.txt" and completed.returncode != 0:
        break
(output / "commands.json").write_text(json.dumps(receipts, indent=2) + "\n")
print((output / receipts[-1]["output"]).read_text(), end="")
raise SystemExit(receipts[-1]["exit_code"])
