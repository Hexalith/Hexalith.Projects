"""Qualify the unchanged retained candidate against exact pinned McpCli source.

Run under run-with-temp. The local package feed is qualification-only and never
changes repository NuGet configuration or represents public availability.
"""
import hashlib
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import tarfile
import time

PACKET = Path(__file__).resolve().parents[1]
ROOT = PACKET.parents[3]
OUT = Path(os.environ.get("HEXALITH_QUALIFICATION_OUTPUT", str(PACKET / "mcp"))).resolve()
if OUT.exists():
    raise FileExistsError("Refusing existing qualification output: " + str(OUT))
SCRATCH = Path(os.environ["TMPDIR"])
if OUT.is_relative_to(SCRATCH.resolve()):
    raise ValueError("Qualification output must survive TMPDIR cleanup")
OUT.mkdir(parents=True, exist_ok=False)
MCP = "5008de388c5e37257ca78e48d8afe899cf7aaa2a"
BUILDS = "c751d23aa4e73e8f7d74579db26a9fcf1df4c8e5"
CANDIDATE = ROOT / "_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-pass2/candidate/Hexalith.Projects.Contracts.2.0.0-story66.3.nupkg"
EXPECTED = "349f938db77dd83ff45e200997b1cba97147c46b1855ec58fd6e2a4b0572eff5"

def sha(data):
    return hashlib.sha256(data).hexdigest()

assert sha(CANDIDATE.read_bytes()) == EXPECTED
workspace = SCRATCH / "exact-source"
workspace.mkdir()
receipt = {"kind": "qualification-only", "candidateSha256": EXPECTED,
           "candidateSource": str(CANDIDATE.relative_to(ROOT)),
           "candidateSourceRelativeToImmutablePacket": os.path.relpath(CANDIDATE, PACKET),
           "status": "running", "sources": {}, "commands": []}
for name, revision in [("Hexalith.McpCli", MCP), ("Hexalith.Builds", BUILDS)]:
    source = ROOT / "references" / name
    archive = subprocess.check_output(["git", "archive", revision], cwd=source)
    destination = workspace / "references" / name
    destination.mkdir(parents=True)
    with tarfile.open(fileobj=io.BytesIO(archive)) as tf:
        tf.extractall(destination, filter="data")
    receipt["sources"][name] = {"revision": revision, "archiveSha256": sha(archive)}
feed = SCRATCH / "feed"
feed.mkdir()
shutil.copyfile(CANDIDATE, feed / CANDIDATE.name)
config = workspace / "NuGet.Config"
config.write_text(f'''<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources><clear />
    <add key="retained-qualification-candidate" value="{feed}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="retained-qualification-candidate"><package pattern="Hexalith.Projects.Contracts" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
''')
env = os.environ.copy()
env.pop("CI", None)
env["NUGET_PACKAGES"] = str(SCRATCH / "packages")
env["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0"
project = "references/Hexalith.McpCli/src/Hexalith.McpCli/Hexalith.McpCli.csproj"
commands = [
    ("historical-candidate-restore", ["dotnet", "restore", project,
     "-p:UseHexalithProjectReferences=true", "-p:HexalithConversationsFromSource=false",
     "--configfile", str(config)]),
    ("historical-candidate-release-build", ["dotnet", "build", project, "--no-restore",
     "--configuration", "Release", "-warnaserror", "-m:1", "--disable-build-servers"]),
    ("historical-candidate-projects-discovery", ["dotnet",
     "references/Hexalith.McpCli/src/Hexalith.McpCli/bin/Release/net11.0/Hexalith.McpCli.dll",
     "operations", "projects", "--strict"]),
]
for name, command in commands:
    start = time.monotonic()
    with (OUT / (name + ".log")).open("xb") as log:
        result = subprocess.run(command, cwd=workspace, env=env, stdout=log, stderr=subprocess.STDOUT)
    receipt["commands"].append({"name": name, "command": command, "exitCode": result.returncode,
        "seconds": round(time.monotonic()-start, 3), "logSha256": sha((OUT/(name+".log")).read_bytes())})
    (OUT / "historical-candidate-qualification.json").write_text(json.dumps(receipt, indent=2)+"\n")
    print(name, result.returncode, flush=True)
    if result.returncode:
        break
assets = workspace / "references/Hexalith.McpCli/src/Hexalith.McpCli/obj/project.assets.json"
if assets.exists():
    data = json.loads(assets.read_text())
    receipt["resolvedPackageIdentities"] = sorted(k for k,v in data["libraries"].items() if v["type"] == "package")
    receipt["restoredCandidate"] = data["libraries"].get("Hexalith.Projects.Contracts/2.0.0-story66.3")
receipt["allRequestedCommandsPassed"] = len(receipt["commands"]) == len(commands) and all(c["exitCode"] == 0 for c in receipt["commands"])
receipt["status"] = "passed" if receipt["allRequestedCommandsPassed"] else "failed"
(OUT / "historical-candidate-qualification.json").write_text(json.dumps(receipt, indent=2)+"\n")
print(json.dumps({"allRequestedCommandsPassed": receipt["allRequestedCommandsPassed"]}))
if not receipt["allRequestedCommandsPassed"]:
    raise SystemExit(1)
