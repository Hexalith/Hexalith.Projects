"""Keep the disposable review diff alive until all reviewer children finish."""
from pathlib import Path
import os
import subprocess
import time
import json

packet = Path(__file__).resolve().parent
root = packet.parents[3]
scratch = Path(os.environ["TMPDIR"])
attributes = scratch / "review.attributes"
attributes.write_text("*.log -diff\n*.nupkg -diff\n*.xml -diff\n*complete-*-tree.txt -diff\n*.dll -diff\n")
diff = scratch / "projects-package-compatibility.diff"
files = [root / "_bmad-output/implementation-artifacts/spec-projects-package-compatibility.md"]
files += sorted(path for path in packet.rglob("*") if path.is_file()
                and path.name not in {"review-runtime.json", "review-finished", "prepare-review.py"})
with diff.open("wb") as output:
    output.write((packet / "projects/minimal-contracts.patch").read_bytes())
    for path in files:
        process = subprocess.run(["git", "-c", "core.attributesFile=" + str(attributes),
                                  "diff", "--no-index", "--no-ext-diff", "--", "/dev/null", str(path)],
                                 cwd=scratch, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        if process.returncode not in {0, 1}:
            raise RuntimeError(process.stderr.decode(errors="replace"))
        output.write(process.stdout)
runtime = {"diff_file": str(diff), "bytes": diff.stat().st_size,
           "claims_file": str(files[0]), "scope": "task-owned packet and proposed two-file correction; concurrent edits excluded"}
(packet / "review-runtime.json").write_text(json.dumps(runtime, indent=2) + "\n")
print(json.dumps(runtime), flush=True)
deadline = time.monotonic() + 1800
while not (packet / "review-finished").exists():
    if time.monotonic() > deadline:
        raise TimeoutError("reviewer lifetime exceeded")
    time.sleep(1)
print("Review children completed; disposable diff workspace released", flush=True)
