"""Rebuild historical Contracts from its claimed immutable commit, without edits."""
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import tarfile
import zipfile
import shutil
import difflib

PACKET = Path(__file__).resolve().parents[1]
ROOT = PACKET.parents[3]
OUT = Path(os.environ.get("HEXALITH_QUALIFICATION_OUTPUT", str(PACKET / "mcp"))).resolve()
if OUT.exists():
    raise FileExistsError("Refusing existing qualification output: " + str(OUT))
SCRATCH = Path(os.environ["TMPDIR"])
if OUT.is_relative_to(SCRATCH.resolve()):
    raise ValueError("Qualification output must survive TMPDIR cleanup")
OUT.mkdir(parents=True, exist_ok=False)
INSPECT_VERSION = "0.27.0"
REV = "5b4bdeacde318a1d948db7ef76843df0c548f084"
BUILDREV = "801f268cd1b37ecae41d4fb20d115c055b4c5793"
RETAINED = ROOT / "_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-pass2/candidate/Hexalith.Projects.Contracts.2.0.0-story66.3.nupkg"
repo = SCRATCH / "projects"
subprocess.run(["git", "clone", "--shared", "--no-checkout", str(ROOT), str(repo)], check=True, stdout=subprocess.DEVNULL)
subprocess.run(["git", "checkout", "--detach", REV], cwd=repo, check=True, stdout=subprocess.DEVNULL)
subprocess.run(["git", "branch", "--force", "main", REV], cwd=repo, check=True, stdout=subprocess.DEVNULL)
subprocess.run(["git", "switch", "main"], cwd=repo, check=True, stdout=subprocess.DEVNULL)
subprocess.run(["git", "config", "remote.origin.url", "https://github.com/Hexalith/Hexalith.Projects.git"], cwd=repo, check=True)
archive = subprocess.check_output(["git", "archive", BUILDREV], cwd=ROOT / "references/Hexalith.Builds")
with tarfile.open(fileobj=io.BytesIO(archive)) as tf:
    tf.extractall(repo / "references/Hexalith.Builds", filter="data")
command = ["dotnet", "pack", "src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj", "--configuration", "Release", "-m:1", "--disable-build-servers", "-p:HexalithProjectsPackageVersion=2.0.0-story66.3", "-p:UseHexalithProjectReferences=false", "--artifacts-path", str(SCRATCH / "package-build"), "--output", str(SCRATCH / "feed")]
env = os.environ.copy()
env.pop("CI", None)
env["NUGET_PACKAGES"] = str(SCRATCH / "packages")
env["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0"
with (OUT / "historical-exact-source-rebuild.log").open("wb") as log:
    result = subprocess.run(command, cwd=repo, env=env, stdout=log, stderr=subprocess.STDOUT)
receipt = {"revision": REV, "buildsRevision": BUILDREV, "command": command, "exitCode": result.returncode,
           "inspectionTool": {"package": "dotnet-inspect", "version": INSPECT_VERSION},
           "retainedPackageRelativeToImmutablePacket": os.path.relpath(RETAINED, PACKET)}
if result.returncode:
    (OUT / "historical-exact-source-rebuild.json").write_text(json.dumps(receipt, indent=2)+"\n")
    raise SystemExit(result.returncode)
sha = lambda b: hashlib.sha256(b).hexdigest()
candidate = SCRATCH / "feed" / RETAINED.name
if candidate.exists():
    shutil.copy2(candidate, OUT / ("rebuilt-" + candidate.name))
    old = zipfile.ZipFile(RETAINED)
    new = zipfile.ZipFile(candidate)
    receipt["retainedSha256"] = sha(RETAINED.read_bytes())
    receipt["rebuildSha256"] = sha(candidate.read_bytes())
    receipt["files"] = [{"path": n, "identical": old.read(n) == new.read(n), "retainedSha256": sha(old.read(n)), "rebuiltSha256": sha(new.read(n))} for n in old.namelist() if n in new.namelist()]
    receipt["dllIdentical"] = old.read("lib/net11.0/Hexalith.Projects.Contracts.dll") == new.read("lib/net11.0/Hexalith.Projects.Contracts.dll")
    (OUT / "historical-rebuild-nuspec-diff.txt").write_text("".join(difflib.unified_diff(old.read("Hexalith.Projects.Contracts.nuspec").decode("utf-8-sig").splitlines(True), new.read("Hexalith.Projects.Contracts.nuspec").decode("utf-8-sig").splitlines(True), fromfile="retained", tofile="rebuilt")))
    for label, package in [("retained", RETAINED), ("rebuilt", candidate)]:
        with (OUT / (label+"-historical-library.log")).open("wb") as log:
            subprocess.run(["dnx", "dotnet-inspect@" + INSPECT_VERSION, "-y", "--", "library", "--package", str(package), "-n", "100"], cwd=repo, env=env, stdout=log, stderr=subprocess.STDOUT, check=True)
else:
    raise RuntimeError("Successful pack did not produce the required candidate")
(OUT / "historical-exact-source-rebuild.json").write_text(json.dumps(receipt, indent=2)+"\n")
print(json.dumps(receipt, indent=2))
