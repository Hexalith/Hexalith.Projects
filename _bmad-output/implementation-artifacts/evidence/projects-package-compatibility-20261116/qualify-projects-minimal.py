"""Qualify the two-file Contracts-only candidate under run-with-temp.

Only the disposable committed-source copy receives the proposed dependency pin.
The candidate feed and all build outputs are private to the wrapper's TMPDIR.
"""

import argparse
import datetime
import difflib
import hashlib
import io
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tarfile
import time
import xml.etree.ElementTree as ET
import zipfile

PACKET = Path(__file__).resolve().parent
ROOT = PACKET.parents[3]
DEFAULT_CANDIDATE = PACKET / "unpublished-candidates/Hexalith.EventStore.Contracts.5.3.0.nupkg"
DEFAULT_SHA256 = "e367e3e9a8d4a5d68942ed56b380acaddaeb4368f19273e6481c0ce2fce941ce"
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--candidate", type=Path, help="Exact EventStore.Contracts 5.3.0 archive; independent of the output directory")
parser.add_argument("--expected-sha256", help="Required when --candidate is supplied")
parser.add_argument("--focused-contracts", action="store_true", help="Build Contracts and run its 207 tests only; complete Release and G-6 remain unverified")
options = parser.parse_args()
if options.candidate is not None and options.expected_sha256 is None:
    parser.error("--candidate requires --expected-sha256")
CANDIDATE = (options.candidate or DEFAULT_CANDIDATE).resolve()
EXPECTED = options.expected_sha256 or DEFAULT_SHA256
if not re.fullmatch(r"[0-9a-f]{64}", EXPECTED):
    parser.error("--expected-sha256 must contain exactly 64 lowercase hexadecimal characters")
OUT = Path(os.environ.get("HEXALITH_QUALIFICATION_OUTPUT", str(PACKET / "projects"))).resolve()
if OUT.exists():
    raise FileExistsError("Refusing existing qualification output: " + str(OUT))
SCRATCH = Path(os.environ["TMPDIR"])
if OUT.is_relative_to(SCRATCH.resolve()):
    raise ValueError("Qualification output must survive TMPDIR cleanup")
OUT.mkdir(parents=True, exist_ok=False)
WORK = SCRATCH / "projects"
WORK.mkdir()
SOURCE = "85ff97950a7cb290288d56618ed7da8152f5a2c6"
VERSION = "5.3.0"
RESULT = {
    "kind": "qualification-only",
    "status": "running",
    "recordedAt": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "source": SOURCE,
    "verificationScope": "focused-contracts-only" if options.focused_contracts else "original-planned-qualification",
    "contractsCandidate": {"version": VERSION, "sha256": EXPECTED,
                           "pathRelativeToImmutablePacket": os.path.relpath(CANDIDATE, PACKET)},
    "materialization": "Exact committed product trees; root-declared dependencies are ordinary archives, with no submodule initialization. Agent skills and retained planning/evidence are not build inputs and are omitted.",
    "sources": {},
    "commands": [],
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def save():
    (OUT / "qualification.json").write_text(json.dumps(RESULT, indent=2) + "\n")


def git(repository, *arguments):
    return subprocess.check_output(["git", "-C", str(repository), *arguments])


def materialize(repository, revision, destination):
    destination.mkdir(parents=True, exist_ok=True)
    entries = git(repository, "ls-tree", "--name-only", revision).decode().splitlines()
    omitted = {"_bmad-output", "_bmad", ".agents", ".agent", ".claude", "references"}
    selected = [entry for entry in entries if entry not in omitted]
    archive = git(repository, "archive", revision, "--", *selected)
    with tarfile.open(fileobj=io.BytesIO(archive)) as tf:
        tf.extractall(destination, filter="data")
    RESULT["sources"][str(destination.relative_to(WORK)) or "."] = {
        "revision": revision,
        "tree": git(repository, "rev-parse", revision + "^{tree}").decode().strip(),
        "selectedArchiveSha256": sha(archive),
        "omittedTopLevelNonProductTrees": sorted(set(entries) & omitted),
    }


def run(label, command, env, expected_failure=None):
    path = OUT / (label + ".log")
    start = time.monotonic()
    with path.open("xb") as log:
        process = subprocess.run(command, cwd=WORK, env=env, stdout=log, stderr=subprocess.STDOUT)
    entry = {"label": label, "command": command, "exitCode": process.returncode,
             "seconds": round(time.monotonic() - start, 3), "log": path.name,
             "logSha256": sha(path.read_bytes())}
    RESULT["commands"].append(entry)
    save()
    print(label, process.returncode, flush=True)
    if process.returncode:
        diagnostics = set(line for line in path.read_text().splitlines() if ": error " in line)
        limits = {
            "baseline": (14, ("CS0234:",), ("QueryResultContract",)),
            "later-release": (13, ("CS0117:", "CS1061:"),
                              ("'GetConversationQuery'", "'ConversationDetailResult'", "'IEventStoreGatewayClient'")),
            "later-client": (7, ("CS1061:",), ("'IEventStoreGatewayClient'",)),
        }
        expected = limits.get(expected_failure)
        if (expected and len(diagnostics) == expected[0]
                and all(any(code in line for code in expected[1])
                        and any(member in line for member in expected[2]) for line in diagnostics)):
            entry["expectedObservation"] = expected_failure
            RESULT.setdefault("expectedFailureObservations", []).append(label)
            save()
        else:
            RESULT.update(status="failed", error=label + " failed unexpectedly")
            save()
            raise RuntimeError(label + " failed; inspect " + str(path))
    if label.endswith("tests-run"):
        matches = re.findall(r"Total: (\d+), Errors: (\d+), Failed: (\d+), Skipped: (\d+), Not Run: (\d+)", path.read_text())
        if len(matches) != 1 or int(matches[0][0]) < 1 or any(map(int, matches[0][1:])):
            RESULT.update(status="failed", error=label + " did not execute every selected test successfully")
            save()
            raise RuntimeError(RESULT["error"])
        entry["tests"] = dict(zip(("total", "errors", "failed", "skipped", "notRun"), map(int, matches[0])))
        if label == "candidate-contract-tests-run" and options.focused_contracts and entry["tests"]["total"] != 207:
            RESULT.update(status="failed", error="Focused Contracts qualification must execute exactly 207 tests")
            save()
            raise RuntimeError(RESULT["error"])
        save()
    return process.returncode


if sha(CANDIDATE.read_bytes()) != EXPECTED:
    raise ValueError("Candidate archive SHA256 differs from --expected-sha256")
with zipfile.ZipFile(CANDIDATE) as package:
    metadata = ET.fromstring(package.read("Hexalith.EventStore.Contracts.nuspec")).find("{*}metadata")
    if metadata.findtext("{*}id") != "Hexalith.EventStore.Contracts" or metadata.findtext("{*}version") != VERSION:
        raise ValueError("Only Hexalith.EventStore.Contracts 5.3.0 can be qualified")
    RESULT["contractsCandidate"]["producerSource"] = metadata.find("{*}repository").attrib["commit"]
materialize(ROOT, SOURCE, WORK)
tree = git(ROOT, "ls-tree", "-r", SOURCE).decode()
(OUT / "complete-baseline-tree.txt").write_text(tree)
gitlinks = {}
for line in tree.splitlines():
    metadata, path = line.split("\t", 1)
    mode, kind, revision = metadata.split()
    if mode == "160000":
        gitlinks[path] = revision
        if not options.focused_contracts or path == "references/Hexalith.Builds":
            materialize(ROOT / path, revision, WORK / path)
RESULT["rootGitlinks"] = gitlinks
RESULT["sourceTreeSha256"] = sha(tree.encode())
save()

env = os.environ.copy()
env["CI"] = "true"
env["NUGET_PACKAGES"] = str(SCRATCH / "packages")
env["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0"
env["PYTHONDONTWRITEBYTECODE"] = "1"
common = ["--configuration", "Release", "-p:UseHexalithProjectReferences=false", "-m:1", "--disable-build-servers"]
contracts = "src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj"
if not options.focused_contracts:
    run("baseline-contracts-build", ["dotnet", "build", contracts, *common,
        "-p:RestoreConfigFile=" + str(WORK / "nuget.config")], env, expected_failure="baseline")

props = WORK / "Directory.Packages.props"
manifest = WORK / "tools/release-packages.json"
before = {str(path.relative_to(WORK)): path.read_bytes() for path in (props, manifest)}
raw = props.read_bytes()
assert raw.count(b"</Project>") == 1
extra = ("  <!-- Preserve the EventStore family while supplying the query result metadata contract. -->\r\n"
         "  <ItemGroup>\r\n"
         '    <PackageVersion Update="Hexalith.EventStore.Contracts" Version="5.3.0" />\r\n'
         "  </ItemGroup>\r\n")
props.write_bytes(raw.replace(b"</Project>", extra.encode() + b"</Project>"))
assert manifest.read_bytes().count(b'"Hexalith.EventStore.Contracts": "5.1.1"') == 4
manifest.write_bytes(manifest.read_bytes().replace(b'"Hexalith.EventStore.Contracts": "5.1.1"', b'"Hexalith.EventStore.Contracts": "5.3.0"'))
patch = []
for relative, original in before.items():
    changed = (WORK / relative).read_bytes()
    patch.extend(difflib.unified_diff(original.decode().splitlines(True), changed.decode().splitlines(True),
                                    fromfile="a/" + relative, tofile="b/" + relative))
(OUT / "minimal-contracts.patch").write_text("".join(patch), newline="")
RESULT["changedFiles"] = {relative: {"beforeSha256": sha(original), "afterSha256": sha((WORK / relative).read_bytes())}
                          for relative, original in before.items()}
RESULT["unchangedFamilyProperty"] = "HexalithEventStoreVersion=5.1.1"
RESULT["patchSha256"] = sha((OUT / "minimal-contracts.patch").read_bytes())
if options.focused_contracts:
    proof = PACKET / "projects/candidate-tree.json"
    tree_proof = json.loads(proof.read_text())
    if tree_proof["patchSha256"] != RESULT["patchSha256"] or tree_proof["source"] != SOURCE:
        raise ValueError("Focused candidate differs from the complete retained Git tree proof")
    RESULT["completeTreeProof"] = {"pathRelativeToImmutablePacket": str(proof.relative_to(PACKET)),
        "sha256": sha(proof.read_bytes()), "candidateTree": tree_proof["candidateTree"], "reused": True}
    prior = PACKET / "projects/qualification.json"
    RESULT["priorArtifactQualification"] = {
        "pathRelativeToImmutablePacket": str(prior.relative_to(PACKET)), "sha256": sha(prior.read_bytes()),
        "candidateSha256": DEFAULT_SHA256,
        "fullReleaseBuild": "previously failed with 13 subsequent errors; not rerun for this archive",
        "otherLibraryBuilds": "previous artifact qualification only; not rerun for this archive"}
save()

feed = SCRATCH / "candidate-feed"
feed.mkdir()
shutil.copyfile(CANDIDATE, feed / DEFAULT_CANDIDATE.name)
config = SCRATCH / "NuGet.qualification.Config"
config.write_text(f'''<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources><clear />
    <add key="unpublished-qualification-only" value="{feed}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="unpublished-qualification-only"><package pattern="Hexalith.EventStore.Contracts" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
''')
RESULT["qualificationConfig"] = config.read_text().replace(str(feed), "${TMPDIR}/candidate-feed")
save()
restore = ["-p:RestoreConfigFile=" + str(config)]
if options.focused_contracts and EXPECTED != DEFAULT_SHA256:
    libraries = []
    for label, archive in [("original", DEFAULT_CANDIDATE), ("selected", CANDIDATE)]:
        library = SCRATCH / (label + "-Hexalith.EventStore.Contracts.dll")
        with zipfile.ZipFile(archive) as package:
            library.write_bytes(package.read("lib/net11.0/Hexalith.EventStore.Contracts.dll"))
        libraries.append(library)
    run("candidate-public-api-comparison", ["dnx", "dotnet-inspect@0.27.0", "-y", "--", "diff", "--library",
        str(libraries[0]) + ".." + str(libraries[1]), "--json"], env)
    api = json.loads((OUT / "candidate-public-api-comparison.log").read_text())
    if api["outcome"] != "available" or any(api["document"]["summary"].values()):
        raise ValueError("Selected candidate public API differs from the original qualified archive")
    with zipfile.ZipFile(DEFAULT_CANDIDATE) as package:
        original_metadata = ET.fromstring(package.read("Hexalith.EventStore.Contracts.nuspec")).find("{*}metadata")
    if original_metadata.find("{*}repository").attrib["commit"] != RESULT["contractsCandidate"]["producerSource"]:
        raise ValueError("Selected candidate producer source differs from the original qualification")
    RESULT["priorArtifactQualification"]["sameProducerSourceAndPublicApi"] = True
    RESULT["priorArtifactQualification"]["inspectionToolVersion"] = "0.27.0"
    RESULT["priorArtifactQualification"]["publicApiComparisonSummary"] = api["document"]["summary"]
    save()
run("candidate-contracts-build", ["dotnet", "build", contracts, *common, *restore], env)
if not options.focused_contracts:
    run("candidate-solution-restore", ["dotnet", "restore", "Hexalith.Projects.CI.slnx",
        "-p:Configuration=Release", "-p:UseHexalithProjectReferences=false", "--configfile", str(config), "-m:1"], env)
    run("candidate-solution-build", ["dotnet", "build", "Hexalith.Projects.CI.slnx", "--no-restore", *common], env,
        expected_failure="later-release")

test_project = "tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj"
code = run("candidate-contract-tests-build", ["dotnet", "build", test_project, *common, *restore], env)
if code == 0:
    assembly = str(WORK / "tests/Hexalith.Projects.Contracts.Tests/bin/Release/net11.0/Hexalith.Projects.Contracts.Tests.dll")
    xml = OUT / "candidate-contract-tests.xml"
    run("candidate-contract-tests-run", ["dotnet", assembly, "-xml", str(xml)], env)
    RESULT["contractsTestXml"] = {"path": xml.name, "sha256": sha(xml.read_bytes())}
    if not options.focused_contracts:
        run("candidate-enrollment-tests-run", ["dotnet", assembly, "-class", "*ProjectMcpCliDecorationTests"], env)

for name in ([] if options.focused_contracts else ["Hexalith.Projects", "Hexalith.Projects.Client", "Hexalith.Projects.Testing", "Hexalith.Projects.ServiceDefaults"]):
    project = f"src/{name}/{name}.csproj"
    run("candidate-" + name.lower() + "-build", ["dotnet", "build", project, *common, *restore], env,
        expected_failure="later-client" if name == "Hexalith.Projects.Client" else None)

assets = WORK / "src/Hexalith.Projects.Contracts/obj/project.assets.json"
if assets.exists():
    data = json.loads(assets.read_text())
    RESULT["contractsResolvedPackages"] = sorted(key for key, value in data["libraries"].items() if value["type"] == "package")
    restored = env["NUGET_PACKAGES"] + "/hexalith.eventstore.contracts/5.3.0/hexalith.eventstore.contracts.5.3.0.nupkg"
    RESULT["restoredCandidateSha256"] = sha(Path(restored).read_bytes())
    assert RESULT["restoredCandidateSha256"] == EXPECTED
    RESULT["contractsDependencyGraph"] = {target: {identity: item.get("dependencies", {}) for identity, item in entries.items()}
                                          for target, entries in data["targets"].items()}
else:
    raise RuntimeError("Missing restored Contracts assets")
RESULT["status"] = ("partial" if options.focused_contracts or any(label != "baseline-contracts-build" for label in
                     RESULT.get("expectedFailureObservations", [])) else "passed")
RESULT["focusedContractsPassed"] = True
if options.focused_contracts:
    RESULT["completeReleaseBuild"] = "not run for this archive; prior independent blockers remain"
    RESULT["g6"] = "not run"
RESULT["hostedAcceptance"] = False
save()
print(json.dumps({"commands": len(RESULT["commands"]), "qualification": str(OUT / "qualification.json")}))
