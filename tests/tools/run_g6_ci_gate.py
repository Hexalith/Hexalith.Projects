#!/usr/bin/env python3
"""Audit G-6 on every CI checkout; run OQ8 when selected runtime inputs changed."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
POLICY = Path("references/Hexalith.Builds/Tools/g6-current-policy.json")
AUDITOR = Path("references/Hexalith.Builds/Tools/g6_current.py")
RUNNER = Path("tools/qualification/run_g6_current.py")
import re

SHA = re.compile(r"[0-9a-f]{40}")
BUILDS_REFERENCE = re.compile(r"uses:\s*Hexalith/Hexalith\.Builds/[^\s@]+@(?P<sha>[^\s#]+)")


def git(*args: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(["git", "-C", str(ROOT), *args], capture_output=True, text=True, check=False)


def builds_execution_pins() -> list[str]:
    active = "\n".join(line for line in (ROOT / ".github/workflows/ci.yml").read_text().splitlines()
                       if not line.lstrip().startswith("#"))
    pins = sorted({entry["sha"] for entry in BUILDS_REFERENCE.finditer(active)})
    if len(pins) != 1 or not SHA.fullmatch(pins[0]):
        raise RuntimeError("CI must use one immutable Builds execution SHA")
    return pins


def selection(event: str, base: str, policy: dict) -> tuple[bool, str]:
    if event == "schedule":
        return True, "scheduled qualification"
    if event not in {"push", "pull_request"} or not SHA.fullmatch(base) or base == "0" * 40:
        return True, "change base unavailable"
    probe = git("cat-file", "-e", f"{base}^{{commit}}")
    if probe.returncode:
        return True, "change base is not available locally"
    changed = git("diff", "--name-only", "--no-ext-diff", base, "HEAD")
    if changed.returncode:
        return True, "changed paths could not be determined"
    paths = changed.stdout.splitlines()
    material = set(policy["materialFiles"])
    prefixes = tuple(policy["materialPrefixes"])
    for path in paths:
        if path.startswith("references/"):
            root = path.rstrip("/") + "/"
            if any(item.startswith(root) for item in material) or any(item.startswith(root) for item in prefixes):
                return True, f"runtime-owning submodule changed: {path}"
            continue
        if path == ".gitmodules" or path in material or path.startswith(prefixes):
            return True, f"material or submodule change: {path}"
    return False, "not required by this change"


def main(arguments: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--event", required=True)
    parser.add_argument("--base-sha", default="")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--policy", type=Path, default=POLICY)
    options = parser.parse_args(arguments)
    output = options.output.resolve()
    if not output.is_relative_to(ROOT):
        parser.error("G-6 CI output must be under the Projects workspace")
    output.mkdir(parents=True, exist_ok=True)
    policy_path = options.policy if options.policy.is_absolute() else ROOT / options.policy
    policy = json.loads(policy_path.read_text(encoding="utf-8"))
    try:
        execution_pins = builds_execution_pins()
        audit_path = output / "current-audit.json"
        command = [sys.executable, str(ROOT / AUDITOR), "audit", "--workspace", str(ROOT),
                   "--policy", str(policy_path), "--out", str(audit_path)]
        subprocess.run(command, check=True)
        audit = json.loads(audit_path.read_text(encoding="utf-8"))
        required, reason = selection(options.event, options.base_sha, policy)
        (output / "selection.json").write_text(json.dumps({"status": "required" if required else "not required by this change",
            "reason": reason, "source": audit["source"], "materialFingerprint": audit["materialInputs"]["fingerprint"],
            "tupleApproved": audit["tupleApproved"], "issues": audit["issues"],
            "buildsExecutionPins": execution_pins}, indent=2) + "\n", encoding="utf-8")
        if not required:
            print("G6-CURRENT-NOT-REQUIRED-BY-THIS-CHANGE: " + reason
                  + f"; current tuple approved={audit['tupleApproved']}; issues={len(audit['issues'])}")
            return 0
        print("G6-CURRENT-REQUIRED: " + reason, flush=True)
        subprocess.run([sys.executable, str(ROOT / RUNNER), "--policy", str(policy_path),
                        "--output", str(output), "--execution-scope", "ci"], check=True)
        subprocess.run([sys.executable, str(ROOT / AUDITOR), "validate", "--workspace", str(ROOT),
                        "--policy", str(policy_path), "--evidence", str(output / "result.json")], check=True)
        return 0
    except (OSError, subprocess.CalledProcessError, RuntimeError, KeyError, ValueError) as error:
        print(f"G6-CURRENT-CI-FAILED: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
