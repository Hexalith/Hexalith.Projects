"""Record sequential local-only checks without restoring or accessing remote services."""

import datetime
import json
import os
import subprocess
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = next(parent for parent in HERE.parents if (parent / "Hexalith.Projects.slnx").is_file())
STATE = json.loads((HERE / "initial-state.json").read_text())
TEMP = Path(STATE["temporary_root"])
COMMANDS = HERE / "commands.json"


def run(name, argv, cwd=ROOT, environment=None):
    """Retain actual command output and timing without recording inherited secrets."""
    overrides = environment or {}
    started = datetime.datetime.now(datetime.timezone.utc)
    clock = time.monotonic()
    with (HERE / (name + ".stdout")).open("wb") as stdout:
        with (HERE / (name + ".stderr")).open("wb") as stderr:
            result = subprocess.run(argv, cwd=cwd, env=os.environ | overrides, stdout=stdout, stderr=stderr, check=False)
    record = {
        "name": name,
        "argv": [str(item) for item in argv],
        "cwd": str(cwd),
        "environment": overrides,
        "inherited_environment": "Existing process environment; no credentials archived.",
        "started_utc": started.isoformat(),
        "finished_utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "seconds": round(time.monotonic() - clock, 3),
        "exit_code": result.returncode,
        "stdout": name + ".stdout",
        "stderr": name + ".stderr",
    }
    records = json.loads(COMMANDS.read_text()) if COMMANDS.exists() else []
    assert name not in {item["name"] for item in records}, name
    records.append(record)
    COMMANDS.write_text(json.dumps(records, indent=2) + "\n")
    (HERE / (name + ".command.json")).write_text(json.dumps(record, indent=2) + "\n")
    print(json.dumps({"name": name, "exit_code": result.returncode, "seconds": record["seconds"]}), flush=True)


if __name__ == "__main__":
    conversations = ROOT / "references/Hexalith.Conversations"
    platform = ROOT / "references/Hexalith.Platform"
    manifest = str(ROOT / "module/hexalith.module-manifest.v2.json")
    run("conversations-tool-version", ["dotnet", "tool", "run", "hexalith-module", "--", "--version"], conversations)
    run("platform-tool-version", ["dotnet", "tool", "run", "hexalith-module", "--", "--version"], platform)
    for name, mode, cwd in [
        ("public-source", "source", conversations),
        ("public-package-conversations-tool", "package", conversations),
        ("public-package-platform-tool", "package", platform),
    ]:
        destination = TEMP / name
        assert not destination.exists(), destination
        run(name, ["dotnet", "tool", "run", "hexalith-module", "--", "prepare-build", "--manifest", manifest,
                   "--mode", mode, "--workspace", str(destination), "--output", "json"], cwd)
    run("consumer-package-release-build", ["dotnet", "build", "src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj",
        "-c", "Release", "--no-restore", "-m:1", "-p:NuGetAudit=false", "-p:MinVerVersionOverride=1.0.0", "-v:minimal"],
        environment={"DOTNET_CLI_USE_MSBUILD_SERVER": "0", "MSBUILDDISABLENODEREUSE": "1"})
    run("package-tooling-tests", ["python3", "-m", "unittest", "discover", "-s", "tests/tools", "-p", "test_release_package_tools.py"])
    run("planning-guard", ["python3", "-m", "unittest", "discover", "-s", "tests/tools", "-p", "test_production_authority_guard.py"])
    run("story-authority", ["python3", "tools/planning/validate_production_authority.py", "--story-id", "6.4"])
    run("index-authority", ["python3", "tools/planning/validate_production_authority.py", "--validate-index"])
