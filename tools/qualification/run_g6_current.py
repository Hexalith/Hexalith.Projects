#!/usr/bin/env python3
"""Run the current G-6 proof and retain one compact, source-bound result.

Historical v2 packet creation stays in run_g6_qualification.py. This runner reuses
its isolated process/container ownership controls and writes v3 evidence only.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import socket
import subprocess
import sys
import tempfile
import uuid
from pathlib import Path

import run_g6_qualification as historical


ROOT = historical.ROOT
EVENTSTORE = historical.EVENTSTORE
BUILDS = historical.BUILDS
POLICY = BUILDS / "Tools/g6-current-policy.json"
AUDITOR = BUILDS / "Tools/g6_current.py"
DEFAULT_OUTPUT = ROOT / ".g6-current-evidence/current"
CURRENT_API = historical.module("g6_compact_validator", AUDITOR)


def audit(output: Path, policy: Path) -> dict:
    command = [sys.executable, str(AUDITOR), "audit", "--workspace", str(ROOT),
               "--policy", str(policy), "--out", str(output)]
    result = subprocess.run(command, text=True, capture_output=True, check=False)
    if result.returncode != 0 or not output.is_file():
        raise RuntimeError(f"Current G-6 audit failed (exit {result.returncode}): {result.stdout}{result.stderr}")
    return json.loads(output.read_text(encoding="utf-8"))


def counts(path: Path) -> dict:
    if not path.is_file():
        return {"total": 0, "passed": 0, "failed": 1, "skipped": 0}
    summary = json.loads(path.read_text(encoding="utf-8")).get("results", {}).get("summary", {})
    return {"total": summary.get("tests", 0), "passed": summary.get("passed", 0),
            "failed": summary.get("failed", 1), "skipped": summary.get("skipped", 0)}


def passed(summary: dict, minimum: int) -> bool:
    return (summary["total"] >= minimum and summary["passed"] == summary["total"]
            and summary["failed"] == 0 and summary["skipped"] == 0)


def digest(value: dict) -> str:
    source = {key: item for key, item in value.items() if key != "artifactSha256"}
    return hashlib.sha256(json.dumps(source, sort_keys=True, separators=(",", ":"),
                                     ensure_ascii=False).encode("utf-8")).hexdigest()


def main(arguments: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--policy", type=Path, default=POLICY)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--execution-scope", choices=("ci", "release"), default="ci")
    options = parser.parse_args(arguments)
    policy_path = options.policy.resolve()
    output = options.output.resolve()
    if not output.is_relative_to(ROOT):
        parser.error("Output must be under the Projects workspace for sanitized command logs")
    if (output / "result.json").exists():
        parser.error("Refusing to overwrite a retained G-6 result")
    policy = json.loads(policy_path.read_text(encoding="utf-8"))
    output.mkdir(parents=True, exist_ok=True)
    before = audit(output / "audit-before.json", policy_path)
    execution_sha = CURRENT_API.builds_execution_sha(ROOT, options.execution_scope)
    pending_candidate = (policy.get("approval", {}).get("decision") == "pending"
                         and before.get("effectiveTuple") == policy.get("tuple"))
    fatal_prefixes = ("controlled tuple changed:", "current checkout differs from root gitlink:")
    if not pending_candidate:
        fatal_prefixes += ("Platform file host pin differs from effective tuple:",
                           "direct controlled package differs from effective tuple:")
    preflight_issues = [issue for issue in before.get("issues", []) if issue.startswith(fatal_prefixes)]
    if preflight_issues:
        historical.write(output / "cleanup.json", {"status": "not run", "reason": "preflight rejected source or tuple"})
        cleanup_receipt = {"path": (output / "cleanup.json").relative_to(ROOT).as_posix(),
                           "sha256": historical.API.sha256(output / "cleanup.json")}
        result = {"schema": "hexalith.g6-current-evidence.v1", "audit": before,
            "qualification": {"status": "not verified", "runner": "tools/qualification/run_g6_current.py",
                "fixture": historical.QUALIFIER, "commands": [],
                "tests": {"qualifier": {"total": 0, "passed": 0, "failed": 1, "skipped": 0},
                          "support": {"total": 0, "passed": 0, "failed": 1, "skipped": 0}},
                "receipts": {"captureDirectory": "", "files": [], "logs": [], "cleanup": cleanup_receipt},
                "cleanup": {"ownedProcessesStopped": False, "scratchRemoved": False,
                    "fixtureScratchRemoved": False, "sharedResourcesUnchanged": False,
                    "ownedContainersRemoved": False},
                "environment": {"configuration": "Debug", "dependencyMode": "source",
                    "executionScope": options.execution_scope, "buildsExecutionSha": execution_sha},
                "limitations": preflight_issues}, "approval": policy.get("approval", {})}
        result["artifactSha256"] = digest(result)
        historical.write(output / "result.json", result)
        print("G6-CURRENT-NOT-VERIFIED: " + "; ".join(preflight_issues), file=sys.stderr)
        return 1
    scratch = Path(tempfile.mkdtemp(prefix="hexalith-g6-current-", dir="/var/tmp"))
    run = historical.Qualification(output, scratch, policy)
    errors: list[str] = []
    apphosts: list[dict] = []
    live_count = {"total": 0, "passed": 0, "failed": 1, "skipped": 0}
    support_count = dict(live_count)
    cleanup: dict = {}
    try:
        historical.install(scratch, policy)
        tools = scratch / "tools"
        daprd = next((scratch / "runtime").rglob("daprd"))
        run.env["PATH"] = str(tools) + os.pathsep + run.env["PATH"]
        version_log = run.run("observe exact tool versions", ["python3", "-c",
            "import subprocess; [subprocess.run(c,check=True) for c in "
            "[['dotnet','--version'],['aspire','--version'],['dapr','--runtime-path',"
            + repr(str(scratch / "runtime")) + ",'--version']]]"])
        if version_log != 0:
            errors.append("Selected SDK, Aspire, or Dapr version could not be observed")
        else:
            log = (output / "logs/observe-exact-tool-versions.log").read_text()
            for field in ("dotnetSdk", "aspireCli", "daprCli", "daprRuntime"):
                if policy["tuple"][field] not in log:
                    errors.append(f"Observed {field} differs from the policy")
        suffix = uuid.uuid4().hex
        placement = f"g6-oq8-{suffix}-placement"
        scheduler = f"g6-oq8-{suffix}-scheduler"
        runtime_image = f"daprio/dapr:{policy['tuple']['daprRuntime']}"
        run.start_container(placement, runtime_image, ["./placement", "--port", "50005"], 50005)
        with socket.socket() as connection:
            connection.bind(("127.0.0.1", 0))
            scheduler_port = connection.getsockname()[1]
        run.start_container(scheduler, runtime_image, ["./scheduler", "--port", "50006",
            "--etcd-data-dir", "/tmp/g6-scheduler", "--etcd-client-listen-address", "0.0.0.0",
            "--override-broadcast-host-port", f"127.0.0.1:{scheduler_port}"], 50006, scheduler_port)
        _, redis_port = run.start_container(f"g6-oq8-{suffix}-redis", "redis:7.4", [], 6379)
        capture = output / "capture"
        capture.mkdir()
        run.env.update(HEXALITH_OQ8_DAPRD_PATH=str(daprd), HEXALITH_OQ8_CONFIGURATION="Debug",
            HEXALITH_OQ8_PLACEMENT_CONTAINER=placement, HEXALITH_OQ8_SCHEDULER_CONTAINER=scheduler,
            HEXALITH_OQ8_NAMESPACE=f"g6-oq8-{suffix}", HEXALITH_OQ8_REDIS_ENDPOINT=f"127.0.0.1:{redis_port}",
            HEXALITH_OQ8_EVIDENCE_DIRECTORY=str(capture),
            HEXALITH_OQ8_CLEANUP_PATH=str(scratch / "fixture-cleanup.json"),
            HEXALITH_OQ8_DIAGNOSTICS_PATH=str(scratch / "fixture-diagnostics.json"))
        build = ["--configuration", "Debug", "--no-incremental", "-m:1",
                 "-p:UseHexalithProjectReferences=true", "-v:minimal"]
        live_project = "tests/Hexalith.EventStore.Server.LiveSidecar.Tests/Hexalith.EventStore.Server.LiveSidecar.Tests.csproj"
        support_project = "tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj"
        live_build = run.run("fresh EventStore qualifier build", ["dotnet", "build", live_project, *build], EVENTSTORE)
        support_build = run.run("fresh EventStore support build", ["dotnet", "build", support_project, *build], EVENTSTORE)
        live_dll = EVENTSTORE / "tests/Hexalith.EventStore.Server.LiveSidecar.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.LiveSidecar.Tests.dll"
        support_dll = EVENTSTORE / "tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll"
        historical.run_fixture_controls(run, live_build, live_dll)
        if live_build == 0:
            run.run("real PostgreSQL two-sidecar stop/restart qualifier", ["dotnet", str(live_dll),
                "-method", historical.QUALIFIER, "-noColor", "-result-ctrf", str(scratch / "qualifier-ctrf.json")], EVENTSTORE, 900)
        else:
            errors.append("Fresh qualifier build failed; old binaries were not executed")
        workflow = (EVENTSTORE / ".github/workflows/integration.yml").read_text(encoding="utf-8")
        selectors = re.findall(r"-method (Hexalith\.EventStore\.Server\.Tests\.\S+)", workflow)
        if len(selectors) != len(set(selectors)) or len(selectors) != 21:
            errors.append("Deterministic support selector inventory changed")
        elif support_build == 0:
            run.run("21 exact deterministic support selectors", ["dotnet", str(support_dll),
                *[argument for selector in selectors for argument in ("-method", selector)], "-noColor",
                "-result-ctrf", str(scratch / "support-ctrf.json")], EVENTSTORE)
        else:
            errors.append("Fresh support build failed; old binaries were not executed")
        if (scratch / "qualifier-ctrf.json").is_file() and (scratch / "support-ctrf.json").is_file():
            run.run("strict EventStore capture validation", ["python3", "tools/validate-oq8-platform-evidence.py",
                "--capture-directory", str(capture), "--ctrf", str(scratch / "qualifier-ctrf.json"),
                "--support-ctrf", str(scratch / "support-ctrf.json"), "--expected-runtime-version",
                policy["tuple"]["daprRuntime"], "--expected-configuration", "Debug"], EVENTSTORE)
        live_count = counts(scratch / "qualifier-ctrf.json")
        support_count = counts(scratch / "support-ctrf.json")
        # Compile every selected AppHost and a transitive consumer. The file-based Platform
        # AppHost consumes packages; the remaining projects use source references in Debug.
        baseline = json.loads((BUILDS / "Tools/runtime-toolchain-baseline-2026-10-01.json").read_text())
        for item in baseline["pinAudit"]["appHostProjects"]:
            project = ROOT / item["path"]
            code = run.run("AppHost build " + project.parent.name, ["dotnet", "build", str(project), *build], project.parent)
            apphosts.append({"path": item["path"], "exitCode": code, "dependencyMode": "source"})
        platform = ROOT / "references/Hexalith.Platform/apphost.cs"
        code = run.run("AppHost build Hexalith.Platform", ["dotnet", "build", str(platform),
            "--configuration", "Debug", "--no-incremental", "-v:minimal",
            f"-p:BaseIntermediateOutputPath={scratch / 'platform-obj'}/"], platform.parent)
        apphosts.append({"path": platform.relative_to(ROOT).as_posix(), "exitCode": code, "dependencyMode": "packages"})
        mcp = ROOT / "references/Hexalith.McpCli/src/Hexalith.McpCli/Hexalith.McpCli.csproj"
        run.run("McpCli transitive Dapr consumer", ["dotnet", "build", str(mcp), *build], mcp.parent)
    except Exception as error:
        errors.append(run.sanitize(str(error)))
        print("G6-CURRENT-FAILED: " + errors[-1], file=sys.stderr, flush=True)
    finally:
        try:
            cleanup = run.cleanup()
            historical.retain_cleanup_failure(cleanup, errors)
        except Exception as error:
            errors.append("Cleanup failed: " + run.sanitize(str(error)))
            cleanup = {}
    historical.write(output / "cleanup.json", cleanup)
    after = audit(output / "audit-after.json", policy_path)
    if before.get("materialInputs", {}).get("fingerprint") != after.get("materialInputs", {}).get("fingerprint"):
        errors.append("Material runtime inputs changed during qualification")
    if before.get("source") != after.get("source"):
        errors.append("Tested source changed during qualification")
    if any(item["exitCode"] != 0 for item in run.commands):
        errors.append("A required qualifier, support, consumer, or capture command failed")
    if not (live_count == {"total": 1, "passed": 1, "failed": 0, "skipped": 0}
            and support_count == {"total": 33, "passed": 33, "failed": 0, "skipped": 0}):
        errors.append("Critical runtime or support tests failed, skipped, or were unavailable")
    if len(apphosts) != 10 or any(item["exitCode"] != 0 for item in apphosts):
        errors.append("Selected AppHosts did not all compile")
    capture = output / "capture"
    receipt_files = []
    for name in ("observations.json", "test-results.json", "deterministic-support.json", "capture-validation.json"):
        path = capture / name
        if path.is_file():
            receipt_files.append({"name": name, "sha256": historical.API.sha256(path)})
    if len(receipt_files) != 4:
        errors.append("The four required EventStore capture documents were not retained")
    qualified = not errors and after.get("tupleApproved") is True and not after.get("issues")
    status = "qualified" if qualified else "failed" if errors else "not verified"
    receipt_logs = [{"purpose": item["purpose"], "path": item["logPath"], "sha256": item["logSha256"]}
                    for item in run.commands]
    cleanup_receipt = {"path": (output / "cleanup.json").relative_to(ROOT).as_posix(),
                       "sha256": historical.API.sha256(output / "cleanup.json")}
    receipt = {
        "schema": "hexalith.g6-current-evidence.v1",
        "audit": after,
        "qualification": {
            "status": status,
            "runner": "tools/qualification/run_g6_current.py",
            "fixture": historical.QUALIFIER,
            "commands": [{key: item[key] for key in ("purpose", "command", "exitCode")} for item in run.commands],
            "tests": {"qualifier": live_count, "support": support_count},
            "receipts": {"captureDirectory": capture.relative_to(ROOT).as_posix(),
                         "files": receipt_files, "logs": receipt_logs, "cleanup": cleanup_receipt},
            "cleanup": {"ownedProcessesStopped": cleanup.get("ownedProcessesStopped") is True,
                "scratchRemoved": cleanup.get("scratchRemoved") is True,
                "fixtureScratchRemoved": cleanup.get("fixtureScratchRemoved") is True,
                "sharedResourcesUnchanged": cleanup.get("before") == cleanup.get("after") if cleanup else False,
                "ownedContainersRemoved": (sorted(item["id"] for item in cleanup.get("ownedContainers", []))
                    == sorted(cleanup.get("removedContainerIds", []))) if cleanup else False},
            "environment": {"configuration": "Debug", "dependencyMode": "source",
                "daprRuntime": policy["tuple"]["daprRuntime"], "platformAppHostDependencyMode": "packages",
                "executionScope": options.execution_scope, "buildsExecutionSha": execution_sha},
            "limitations": ["Checkout-source proof; published EventStore archives, G-4/G-5, P1R and release readiness are separate.", *errors],
        },
        "approval": policy.get("approval", {}),
    }
    receipt["artifactSha256"] = digest(receipt)
    historical.write(output / "result.json", receipt)
    print(f"G6-CURRENT-{status.upper().replace(' ', '-')}: {output / 'result.json'}")
    return 0 if qualified else 1


if __name__ == "__main__":
    raise SystemExit(main())
