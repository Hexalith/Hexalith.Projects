"""Bind the approved two-file pin to current main without touching its index or worktree."""
import datetime
import difflib
import gzip
import hashlib
import json
import os
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

OUT = Path(__file__).resolve().parent
PACKET = OUT.parents[1]
ROOT = PACKET.parents[3]
SOURCE = "0bb322c2105c841eba150fd6cb5aab2b3d4b2d4c"
TESTED_SOURCE = "85ff97950a7cb290288d56618ed7da8152f5a2c6"
PREVIOUS_PROPOSAL = "cd1fcd62e3180e5b037850cacb1c1756668b2866"
APPROVED_HASH = "1c3bdabc4778467d74b468a47f0413f5f85272539d9969891036c8865f6949c3"
if any(path != Path(__file__).resolve() for path in OUT.iterdir()):
    raise FileExistsError("Refusing existing current-pin-candidate results")
SCRATCH = Path(os.environ["TMPDIR"])
env = os.environ.copy()
bare = SCRATCH / "candidate.git"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def repo_git(*arguments, repository=ROOT):
    return subprocess.check_output(["git", "-C", str(repository), *arguments])


def write(name, data):
    path = OUT / name
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("xb") as stream:
        stream.write(data)


def document(name, value):
    write(name, (json.dumps(value, indent=2) + "\n").encode())


def entries(revision, private=False):
    raw = private_git("ls-tree", "-r", revision) if private else repo_git("ls-tree", "-r", revision)
    return {line.split(b"\t", 1)[1].decode(): line.split(b"\t", 1)[0].decode() for line in raw.splitlines()}


remote_before = repo_git("ls-remote", "origin", "refs/heads/main").decode().split()[0]
if remote_before != SOURCE:
    raise ValueError("Remote main moved before qualification: " + remote_before)
before_status = repo_git("status", "--porcelain=v1", "-z")
current = entries(SOURCE)
tested = entries(TESTED_SOURCE)
previous = entries(PREVIOUS_PROPOSAL)
approved = PACKET / "producer-replay-20261116T214600Z/unpublished-candidates/Hexalith.EventStore.Contracts.5.3.0.nupkg"
if sha(approved.read_bytes()) != APPROVED_HASH:
    raise ValueError("Approved archive hash changed")
subprocess.run(["git", "init", "--bare", "--quiet", str(bare)], check=True)
env["GIT_ALTERNATE_OBJECT_DIRECTORIES"] = repo_git("rev-parse", "--path-format=absolute", "--git-path", "objects").decode().strip()
env["GIT_INDEX_FILE"] = str(SCRATCH / "candidate.index")


def private_git(*arguments, data=None):
    return subprocess.check_output(["git", "--git-dir", str(bare), *arguments], input=data, env=env)


private_git("read-tree", SOURCE)
changes = {}
patch = []
for path in ["Directory.Packages.props", "tools/release-packages.json"]:
    original = repo_git("show", SOURCE + ":" + path)
    if path == "Directory.Packages.props":
        extra = ("  <!-- Preserve the EventStore family while supplying the query result metadata contract. -->\r\n"
                 "  <ItemGroup>\r\n"
                 '    <PackageVersion Update="Hexalith.EventStore.Contracts" Version="5.3.0" />\r\n'
                 "  </ItemGroup>\r\n")
        if original.count(b"</Project>") != 1:
            raise ValueError("Unexpected current package props structure")
        changed = original.replace(b"</Project>", extra.encode() + b"</Project>")
    else:
        if original.count(b'"Hexalith.EventStore.Contracts": "5.1.1"') != 4:
            raise ValueError("Unexpected current release manifest closure")
        changed = original.replace(b'"Hexalith.EventStore.Contracts": "5.1.1"', b'"Hexalith.EventStore.Contracts": "5.3.0"')
    mode, kind, blob = current[path].split()
    new_blob = private_git("hash-object", "-w", "--stdin", data=changed).decode().strip()
    private_git("update-index", "--cacheinfo", mode + "," + new_blob + "," + path)
    changes[path] = {"before": {"mode": mode, "type": kind, "blob": blob, "sha256": sha(original)},
                     "after": {"mode": mode, "type": kind, "blob": new_blob, "sha256": sha(changed)}}
    write("candidate-blobs/" + path, changed)
    patch.extend(difflib.unified_diff(original.decode().splitlines(True), changed.decode().splitlines(True), fromfile="a/" + path, tofile="b/" + path))
patch_bytes = "".join(patch).encode()
write("minimal-contracts.patch", patch_bytes)
if sha(patch_bytes) != sha((PACKET / "projects/minimal-contracts.patch").read_bytes()):
    raise ValueError("Current candidate no longer matches the approved exact two-file patch")
candidate_tree = private_git("write-tree").decode().strip()
candidate = entries(candidate_tree, private=True)
if current.keys() != candidate.keys():
    raise ValueError("Candidate added or removed tree entries")
changed_paths = sorted(path for path in current if current[path] != candidate[path])
if changed_paths != sorted(changes):
    raise ValueError("Candidate differs outside the authorized two files")
gitlinks = {path: value for path, value in current.items() if value.startswith("160000 ")}
if gitlinks != {path: value for path, value in candidate.items() if value.startswith("160000 ")}:
    raise ValueError("Current gitlinks changed in candidate")
for name, mapping in [("current-complete-tree.json.gz", current), ("candidate-complete-tree.json.gz", candidate)]:
    write(name, gzip.compress(json.dumps(mapping, sort_keys=True, separators=(",", ":")).encode(), mtime=0))
tree_proof = {"source": SOURCE, "baseTree": repo_git("rev-parse", SOURCE + "^{tree}").decode().strip(),
              "candidateTree": candidate_tree, "changes": changes, "patchSha256": sha(patch_bytes),
              "completeEntryCount": len(current), "changedEntryCount": len(changed_paths),
              "addedEntries": [], "removedEntries": [], "allOtherEntriesUnchanged": True,
              "gitlinksUnchanged": True, "gitlinks": gitlinks,
              "construction": "Private bare Git object database and private index, reading source objects through alternates; no shared index/object/worktree writes or commits."}
document("candidate-tree.json", tree_proof)


def closure(prefixes, paths=()):
    names = sorted({path for path in set(tested) | set(current) if path in paths or path.startswith(prefixes)})
    return {path: {"tested": tested.get(path), "current": current.get(path), "identical": tested.get(path) == current.get(path)} for path in names}


focused = closure(("src/Hexalith.Projects.Contracts/", "tests/Hexalith.Projects.Contracts.Tests/"))
config_paths = (".editorconfig", ".gitattributes", "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "Module.Directory.Build.props", "Module.Directory.Packages.props", "global.json", "nuget.config", "NuGet.Config", "src/Directory.Build.props", "src/Directory.Packages.props", "src/.editorconfig", "tests/Directory.Build.props", "tests/Directory.Packages.props", "tests/.editorconfig", "Directory.Build.rsp", "MSBuild.rsp")
configs = {path: {"tested": tested.get(path), "current": current.get(path), "identical": tested.get(path) == current.get(path)} for path in config_paths}
production = closure(("src/",))
builds_before = tested["references/Hexalith.Builds"].split()[-1]
builds_current = current["references/Hexalith.Builds"].split()[-1]
catalog_path = "Props/Directory.Packages.props"
catalog_old = repo_git("show", builds_before + ":" + catalog_path, repository=ROOT / "references/Hexalith.Builds")
catalog_new = repo_git("show", builds_current + ":" + catalog_path, repository=ROOT / "references/Hexalith.Builds")
write("direct-catalog.patch", repo_git("diff", builds_before, builds_current, "--", catalog_path, repository=ROOT / "references/Hexalith.Builds"))
refs = []
for path in ["src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj", "tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj"]:
    refs.extend(item.attrib["Include"] for item in ET.fromstring(repo_git("show", SOURCE + ":" + path)).iter("PackageReference") if "Include" in item.attrib)


def selected_versions(data):
    xml = ET.fromstring(data)
    return {name: [item.attrib for item in xml.iter("PackageVersion") if item.attrib.get("Include") == name] for name in sorted(set(refs))}


prior_receipt = PACKET / "consumer-fresh-artifact-20261116T215857Z/qualification.json"
prior_xml = prior_receipt.parent / "candidate-contract-tests.xml"
xml = ET.parse(prior_xml).getroot().find("assembly")
equivalence = {
    "testedSource": TESTED_SOURCE, "currentSource": SOURCE,
    "prior207Receipt": {"pathRelativeToImmutablePacket": str(prior_receipt.relative_to(PACKET)), "sha256": sha(prior_receipt.read_bytes()),
                         "xmlSha256": sha(prior_xml.read_bytes()), "counts": {key: xml.attrib[key] for key in ("total", "passed", "failed", "skipped", "errors", "not-run")}},
    "focusedSourceEntries": focused, "focusedSourceEntriesIdentical": all(item["identical"] for item in focused.values()),
    "configurationEntries": configs, "configurationEntriesIdentical": all(item["identical"] for item in configs.values()),
    "productionEntriesChangedOutsideFocusedClosure": {path: value for path, value in production.items() if not value["identical"]},
    "directCatalog": {"testedSource": builds_before, "currentSource": builds_current, "path": catalog_path,
                      "testedSha256": sha(catalog_old), "currentSha256": sha(catalog_new),
                      "testedSelectedPackageEntries": selected_versions(catalog_old), "currentSelectedPackageEntries": selected_versions(catalog_new)},
    "full207TestContextEquivalent": False,
    "reason": "Current Builds advances the direct Contracts test dependency coverlet.collector from 10.1.0 to 10.1.1. Broader concurrent production changes are preserved and not covered by the prior Contracts-only proof.",
    "decision": "Do not transfer exact-input 207-test qualification. Coordinator authorized a fresh narrow Contracts Release build and 207-case suite against the same approved archive, preserving current Builds and all other pins.",
    "approvedArchiveSha256": APPROVED_HASH,
}
document("prior-test-input-non-equivalence.json", equivalence)
remote_after = repo_git("ls-remote", "origin", "refs/heads/main").decode().split()[0]
concurrent = sorted(path for path in set(previous) | set(current) if previous.get(path) != current.get(path))
summary = {"recordedAt": datetime.datetime.now(datetime.timezone.utc).isoformat(), "source": SOURCE,
           "baseTree": tree_proof["baseTree"], "candidateTree": candidate_tree,
           "remoteMainBefore": remote_before, "remoteMainAfter": remote_after,
           "remoteStillMatchesQualifiedSource": remote_after == SOURCE,
           "changedPaths": changed_paths, "allOtherCurrentEntriesPreserved": True, "allCurrentGitlinksPreserved": True,
           "previousProposalSource": PREVIOUS_PROPOSAL, "preservedConcurrentCommittedChangesSincePreviousProposal": concurrent,
           "approvedArchiveSha256": APPROVED_HASH,
           "priorExactTestEvidenceReusable": False,
           "pendingFreshContractsQualification": "contracts-qualification/qualification.json",
           "publicationPerformed": False, "completeReleaseOrG6Claimed": False,
           "sharedWorktreeNotWritten": True,
           "initialSharedStatusSha256": sha(before_status),
           "limits": "Pin-only candidate. The separate publisher gate-scope decision is pending. No module/owner source, working tree or shared index was changed."}
document("summary.json", summary)
print(json.dumps({"source": SOURCE, "baseTree": tree_proof["baseTree"], "candidateTree": candidate_tree,
                  "priorExactTestContextEquivalent": False, "output": str(OUT)}))
