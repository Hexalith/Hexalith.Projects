"""Construct the candidate's complete Git tree in a private object database."""

import difflib
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
OUT = Path(__file__).resolve().parent / "projects"
SOURCE = "85ff97950a7cb290288d56618ed7da8152f5a2c6"
SCRATCH = Path(os.environ["TMPDIR"])
BARE = SCRATCH / "candidate.git"
subprocess.run(["git", "init", "--bare", "--quiet", str(BARE)], check=True)
objects = subprocess.check_output(["git", "-C", str(ROOT), "rev-parse", "--path-format=absolute", "--git-path", "objects"], text=True).strip()
env = os.environ.copy()
env["GIT_ALTERNATE_OBJECT_DIRECTORIES"] = objects
env["GIT_INDEX_FILE"] = str(SCRATCH / "candidate.index")


def git(*arguments, data=None):
    return subprocess.check_output(["git", "--git-dir", str(BARE), *arguments], input=data, env=env)


git("read-tree", SOURCE)
changes = {}
patch = []
for path in ["Directory.Packages.props", "tools/release-packages.json"]:
    original = git("show", SOURCE + ":" + path)
    if path == "Directory.Packages.props":
        extra = ("  <!-- Preserve the EventStore family while supplying the query result metadata contract. -->\r\n"
                 "  <ItemGroup>\r\n"
                 '    <PackageVersion Update="Hexalith.EventStore.Contracts" Version="5.3.0" />\r\n'
                 "  </ItemGroup>\r\n")
        changed = original.replace(b"</Project>", extra.encode() + b"</Project>")
    else:
        assert original.count(b'"Hexalith.EventStore.Contracts": "5.1.1"') == 4
        changed = original.replace(b'"Hexalith.EventStore.Contracts": "5.1.1"', b'"Hexalith.EventStore.Contracts": "5.3.0"')
    entry = git("ls-tree", SOURCE, "--", path).decode().strip().split()
    mode, kind, before = entry[:3]
    after = git("hash-object", "-w", "--stdin", data=changed).decode().strip()
    git("update-index", "--cacheinfo", mode + "," + after + "," + path)
    changes[path] = {"before": {"mode": mode, "type": kind, "blob": before,
                                 "sha256": hashlib.sha256(original).hexdigest()},
                     "after": {"mode": mode, "type": kind, "blob": after,
                                "sha256": hashlib.sha256(changed).hexdigest()}}
    retained = OUT / "candidate-blobs" / path
    retained.parent.mkdir(parents=True, exist_ok=True)
    retained.write_bytes(changed)
    patch.extend(difflib.unified_diff(original.decode().splitlines(True), changed.decode().splitlines(True),
                                    fromfile="a/" + path, tofile="b/" + path))
expected_patch = (OUT / "minimal-contracts.patch").read_bytes()
assert "".join(patch).encode() == expected_patch
tree = git("write-tree").decode().strip()
base_tree = git("rev-parse", SOURCE + "^{tree}").decode().strip()
before_entries = git("ls-tree", "-r", base_tree)
after_entries = git("ls-tree", "-r", tree)
base_map = {line.split(b"\t", 1)[1]: line.split(b"\t", 1)[0] for line in before_entries.splitlines()}
candidate_map = {line.split(b"\t", 1)[1]: line.split(b"\t", 1)[0] for line in after_entries.splitlines()}
assert base_map.keys() == candidate_map.keys()
actual = [path.decode() for path in base_map if base_map[path] != candidate_map[path]]
assert sorted(actual) == sorted(changes)
gitlinks = {path.decode(): entry.decode() for path, entry in candidate_map.items() if entry.startswith(b"160000 ")}
assert gitlinks == {path.decode(): entry.decode() for path, entry in base_map.items() if entry.startswith(b"160000 ")}
record = {
    "source": SOURCE, "baseTree": base_tree, "candidateTree": tree,
    "construction": "Private bare repository and index with the source object database read through alternates; read-tree, hash-object, update-index and write-tree only. No commit, branch, shared-index or shared-object-store mutation.",
    "changes": changes,
    "completeEntryCount": len(base_map), "changedEntryCount": len(actual),
    "addedEntries": [], "removedEntries": [],
    "allOtherEntriesUnchanged": True, "gitlinksUnchanged": True,
    "gitlinks": gitlinks,
    "baselineTreeListingSha256": hashlib.sha256(before_entries).hexdigest(),
    "candidateTreeListingSha256": hashlib.sha256(after_entries).hexdigest(),
    "patchSha256": hashlib.sha256(expected_patch).hexdigest(),
}
(OUT / "candidate-tree.json").write_text(json.dumps(record, indent=2) + "\n")
(OUT / "complete-candidate-tree.txt").write_bytes(after_entries)
print(json.dumps({"baseTree": base_tree, "candidateTree": tree, "changedPaths": actual,
                  "allOtherEntriesUnchanged": True, "gitlinksUnchanged": True}))
