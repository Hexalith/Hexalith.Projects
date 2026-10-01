#!/usr/bin/env python3
"""Run the approved current G-6 tuple with disposable loopback infrastructure.

The packet stays pending. Source dirtiness, exact gitlinks, failures, preview exclusions,
and cleanup are retained; only a later named decision can grant final acceptance.
"""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import importlib.util
import json
import os
import re
import shlex
import shutil
import signal
import socket
import subprocess
import tarfile
import tempfile
import urllib.request
import uuid
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
BUILDS = ROOT / "references/Hexalith.Builds"
EVENTSTORE = ROOT / "references/Hexalith.EventStore"
DEFAULT_OUTPUT = ROOT / "_bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001"
QUALIFIER = "Hexalith.EventStore.Server.LiveSidecar.Tests.Actors.IdempotencyAdmissionOq8PostgresqlTests.ProductionMatrix_IndependentProcessesPreserveAuthorityReplayExpiryAndLeakageInvariants"


def module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    assert spec is not None and spec.loader is not None
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


API = module("g6_current_validator", BUILDS / "Tools/validate-runtime-toolchain-evidence.py")
CURRENT = API.current_contract()


def write(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def utc() -> str:
    return dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def container_snapshot() -> list[dict]:
    ids = subprocess.check_output(["docker", "ps", "-aq"], text=True).split()
    if not ids:
        return []
    values = json.loads(subprocess.check_output(["docker", "inspect", *ids], text=True))
    return sorted([{"id": item["Id"], "name": item["Name"].lstrip("/"), "image": item["Image"],
                    "running": item["State"]["Running"], "startedAt": item["State"]["StartedAt"],
                    "restartCount": item["RestartCount"]} for item in values], key=lambda item: item["id"])


def shared_snapshot() -> dict:
    paths = [Path(shutil.which("dapr") or "/nonexistent"), Path.home() / ".dapr/bin/daprd"]
    binaries = [{"name": path.name, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()} for path in paths if path.is_file()]
    return {"containers": container_snapshot(), "binaries": binaries}


def install(scratch: Path, baseline: dict) -> None:
    tools = scratch / "tools"
    tools.mkdir()
    url = f"https://github.com/dapr/cli/releases/download/v{baseline['tuple']['daprCli']}/dapr_linux_amd64.tar.gz"
    archive = scratch / "dapr-cli.tar.gz"
    urllib.request.urlretrieve(url, archive)
    with tarfile.open(archive) as stream:
        stream.extractall(tools, filter="data")
    subprocess.run(["dotnet", "tool", "install", "Aspire.Cli", "--tool-path", str(tools), "--version", baseline["tuple"]["aspireCli"]], check=True)
    subprocess.run([str(tools / "dapr"), "init", "--slim", "--runtime-path", str(scratch / "runtime"), "--runtime-version", baseline["tuple"]["daprRuntime"]], check=True)


class Qualification:
    """Retain sanitized command outcomes and remove only resources created by this run."""

    def __init__(self, output: Path, scratch: Path, baseline: dict):
        self.output, self.scratch, self.baseline = output, scratch, baseline
        self.commands = []
        self.owned = []
        self.removed = []
        self.process_groups = []
        self.env = dict(os.environ)
        self.env.update(NUGET_PACKAGES=str(scratch / "nuget"), DOTNET_CLI_HOME=str(scratch / "dotnet-home"),
                        DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER="1", MSBUILDDISABLENODEREUSE="1",
                        PYTHONDONTWRITEBYTECODE="1")
        self.env.pop("CI", None)
        self.before = shared_snapshot()

    def sanitize(self, value: str) -> str:
        for path, alias in [(self.scratch, "[scratch]"), (ROOT, "[workspace]"), (Path.home(), "[user]")]:
            value = value.replace(str(path), alias)
        for pattern in API.FORBIDDEN:
            value = pattern.sub("[redacted]", value)
        return value

    def run(self, purpose: str, command: list[str], cwd: Path = ROOT, timeout: int = 1800) -> int:
        print(f"G6-RUN: {purpose}", flush=True)
        try:
            process = subprocess.Popen(command, cwd=cwd, env=self.env, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, start_new_session=True)
            self.process_groups.append(process.pid)
            stdout, stderr = process.communicate(timeout=timeout)
            code, output = process.returncode, stdout + stderr
        except subprocess.TimeoutExpired:
            os.killpg(process.pid, signal.SIGTERM)
            try:
                stdout, stderr = process.communicate(timeout=10)
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGKILL)
                stdout, stderr = process.communicate()
            code, output = 124, stdout + stderr + "\nCommand exceeded its bounded execution time.\n"
        text = self.sanitize(output)
        path = self.output / "logs" / (re.sub(r"[^a-z0-9]+", "-", purpose.lower()).strip("-") + ".log")
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text)
        outcome = next((line for line in reversed(text.splitlines()) if line.strip()), "No output")
        self.commands.append({"purpose": purpose, "command": self.sanitize(shlex.join(command)), "exitCode": code,
                              "outcome": outcome, "logPath": path.relative_to(ROOT).as_posix(), "logSha256": API.sha256(path)})
        print(f"G6-RESULT: {purpose}: exit {code}; {outcome[:180]}", flush=True)
        return code

    def start_container(self, name: str, image: str, arguments: list[str], port: int, published: int = 0) -> tuple[str, int]:
        identity = subprocess.check_output(["docker", "run", "--rm", "-d", "--name", name,
                                           "--label", "hexalith.g6.isolated=true", "-p", f"127.0.0.1:{published or ''}:{port}", image, *arguments], text=True).strip()
        self.owned.append({"name": name, "id": identity, "image": image})
        binding = subprocess.check_output(["docker", "port", identity, f"{port}/tcp"], text=True).strip()
        return identity, int(binding.rsplit(":", 1)[1])

    def cleanup(self) -> dict:
        for item in reversed(self.owned):
            subprocess.run(["docker", "rm", "-f", item["id"]], capture_output=True, check=False)
            exists = subprocess.run(["docker", "inspect", item["id"]], capture_output=True, check=False).returncode == 0
            if not exists:
                self.removed.append(item["id"])
        # The qualifier owns its application processes and PostgreSQL container. Failure cleanup
        # runs inside its fixture; detect every remaining run-labelled or fixture-owned container.
        groups_stopped = True
        for group in self.process_groups:
            try:
                os.killpg(group, 0)
                os.killpg(group, signal.SIGKILL)
                groups_stopped = False
            except ProcessLookupError:
                pass
        namespace_observations = []
        receipt_path = self.scratch / "fixture-cleanup.json"
        if receipt_path.exists():
            receipt = json.loads(receipt_path.read_text())
            namespace_observations = receipt.get("sidecarNamespaceObservations", [])
            identity = receipt["postgresContainerId"]
            self.owned.append({"name": "fixture-owned-postgresql", "id": identity, "image": "postgres@sha256:a02db8cac496f15b094798a38254f14d6e00741f709360e5e00bb6668ea31636"})
            if subprocess.run(["docker", "inspect", identity], capture_output=True).returncode != 0:
                self.removed.append(identity)
            else:
                subprocess.run(["docker", "rm", "-f", identity], capture_output=True)
                if subprocess.run(["docker", "inspect", identity], capture_output=True).returncode != 0:
                    self.removed.append(identity)
            groups_stopped = groups_stopped and receipt["processesStopped"]
        after = shared_snapshot()
        shutil.rmtree(self.scratch)
        return {"schema": "hexalith.runtime-toolchain-cleanup.v2", "before": self.before, "after": after,
                "ownedContainers": self.owned, "removedContainerIds": self.removed,
                "ownedProcessesStopped": groups_stopped, "scratchRemoved": not self.scratch.exists(), "ownedProcessGroups": self.process_groups, "daprNamespace": self.env.get("HEXALITH_OQ8_NAMESPACE"), "sidecarNamespaceObservations": namespace_observations}


def repositories(baseline: dict, files: list[dict]) -> list[dict]:
    result = []
    for relative in baseline["consumerInventory"]["roots"]:
        path = ROOT / relative
        revision = API.git(path, "rev-parse", "HEAD").stdout.strip()
        entry = API.git(ROOT, "ls-files", "--stage", "--", relative).stdout if relative != "." else ""
        result.append({"name": "Hexalith.Projects" if relative == "." else Path(relative).name,
                       "path": relative, "revision": revision, "rootGitlink": entry.split()[1] if entry else None,
                       "dirty": bool(API.git(path, "status", "--porcelain", "--untracked-files=normal").stdout.strip())})
    result[0]["dirty"] = CURRENT.root_source_dirty(API, ROOT, files, result)
    return result


def resolved(projects: list[str], baseline: dict, scratch: Path) -> list[dict]:
    result = []
    for relative in projects:
        project = ROOT / relative
        candidates = [project.parent / "obj/project.assets.json"] if project.suffix == ".csproj" else list((scratch / "platform-obj").rglob("project.assets.json"))
        packages = []
        for path in candidates:
            if not path.is_file() or (path.is_relative_to(project.parent) and "references" in path.relative_to(project.parent).parts):
                continue
            document = json.loads(path.read_text())
            for identity, value in document.get("libraries", {}).items():
                package, version = identity.rsplit("/", 1)
                field = CURRENT.controlled_field(package)
                if field and value["type"] == "package":
                    excluded = any(item["package"] == package and item["version"] == version
                                   and (item["path"] == "references/Hexalith.Builds/Props/Directory.Packages.props"
                                        or relative.startswith(str(Path(item["path"]).parent) + "/"))
                                   for item in baseline["consumerInventory"]["unqualifiedExclusions"])
                    packages.append({"id": package, "version": version, "qualified": not excluded})
        unique = {(item["id"], item["version"]): item for item in packages}
        result.append({"path": relative, "packages": sorted(unique.values(), key=lambda item: (item["id"], item["version"]))})
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--install-tools", type=Path, help=argparse.SUPPRESS)
    args = parser.parse_args()
    baseline_path = args.baseline.resolve()
    baseline = API.validate_baseline(ROOT, baseline_path)
    if args.install_tools:
        install(args.install_tools, baseline)
        return 0
    output = args.output.resolve()
    API.require(output.is_relative_to(ROOT), "Evidence output must be inside the Projects workspace")
    API.require(not (output / "packet.json").exists(), "Refusing to overwrite a retained qualification attempt")
    output.mkdir(parents=True, exist_ok=True)
    scratch = Path(tempfile.mkdtemp(prefix="hexalith-g6-", dir="/var/tmp"))
    run = Qualification(output, scratch, baseline)
    write(output / "consumer-audit.json", {"schema": "hexalith.runtime-toolchain-consumer-audit.v2", "consumers": CURRENT.inventory(API, ROOT, baseline), "exclusions": baseline["consumerInventory"]["unqualifiedExclusions"]})
    attempts, apphosts, errors = [], [], []
    capture = output / "capture"
    capture.mkdir()
    qualifier_code = 1
    try:
        code = run.run("install isolated tools", ["python3", str(Path(__file__).resolve()), "--baseline", str(baseline_path), "--install-tools", str(scratch)])
        if code:
            raise RuntimeError("The approved isolated tool installation failed")
        tools = scratch / "tools"
        daprd = next((scratch / "runtime").rglob("daprd"))
        run.env["PATH"] = str(tools) + os.pathsep + run.env["PATH"]
        version_command = ["python3", "-c", "import subprocess; [subprocess.run(c,check=True) for c in [['dotnet','--version'],['aspire','--version'],['dapr','--runtime-path'," + repr(str(scratch / "runtime")) + ",'--version']]]"]
        code = run.run("observe exact tool versions", version_command)
        version_log = (output / "logs/observe-exact-tool-versions.log").read_text()
        for field in ["dotnetSdk", "aspireCli", "daprCli", "daprRuntime"]:
            API.require(baseline["tuple"][field] in version_log, f"Observed tool version is not approved: {field}")
        write(output / "observed-versions.json", {"schema": "hexalith.runtime-toolchain-observed-versions.v2", "observedUtc": utc(), "tuple": baseline["tuple"], "toolsLogSha256": API.sha256(output / "logs/observe-exact-tool-versions.log")})
        suffix = uuid.uuid4().hex
        placement_name, scheduler_name = f"g6-oq8-{suffix}-placement", f"g6-oq8-{suffix}-scheduler"
        run.start_container(placement_name, "daprio/dapr:1.18.2", ["./placement", "--port", "50005"], 50005)
        with socket.socket() as connection:
            connection.bind(("127.0.0.1", 0))
            scheduler_port = connection.getsockname()[1]
        run.start_container(scheduler_name, "daprio/dapr:1.18.2", ["./scheduler", "--port", "50006", "--etcd-data-dir", "/tmp/g6-scheduler", "--etcd-client-listen-address", "0.0.0.0", "--override-broadcast-host-port", f"127.0.0.1:{scheduler_port}"], 50006, scheduler_port)
        _, redis_port = run.start_container(f"g6-oq8-{suffix}-redis", "redis:7.4", [], 6379)
        run.env.update(HEXALITH_OQ8_DAPRD_PATH=str(daprd), HEXALITH_OQ8_CONFIGURATION="Debug",
                       HEXALITH_OQ8_PLACEMENT_CONTAINER=placement_name, HEXALITH_OQ8_SCHEDULER_CONTAINER=scheduler_name,
                       HEXALITH_OQ8_NAMESPACE=f"g6-oq8-{suffix}", HEXALITH_OQ8_REDIS_ENDPOINT=f"127.0.0.1:{redis_port}", HEXALITH_OQ8_EVIDENCE_DIRECTORY=str(capture), HEXALITH_OQ8_CLEANUP_PATH=str(scratch / "fixture-cleanup.json"), HEXALITH_OQ8_DIAGNOSTICS_PATH=str(scratch / "fixture-diagnostics.json"))
        build_args = ["--configuration", "Debug", "--no-incremental", "-m:1", "-p:UseHexalithProjectReferences=true", "-v:minimal"]
        live_project = "tests/Hexalith.EventStore.Server.LiveSidecar.Tests/Hexalith.EventStore.Server.LiveSidecar.Tests.csproj"
        support_project = "tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj"
        live_build = run.run("fresh EventStore qualifier build", ["dotnet", "build", live_project, *build_args], EVENTSTORE)
        support_build = run.run("fresh EventStore support build", ["dotnet", "build", support_project, *build_args], EVENTSTORE)
        live_dll = EVENTSTORE / "tests/Hexalith.EventStore.Server.LiveSidecar.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.LiveSidecar.Tests.dll"
        support_dll = EVENTSTORE / "tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll"
        run.run("fixture override tests", ["dotnet", str(live_dll), "-class", "Hexalith.EventStore.Server.LiveSidecar.Tests.Fixtures.Oq8QualificationOverridesTests", "-class", "Hexalith.EventStore.Server.LiveSidecar.Tests.Fixtures.DockerPublishedPortResolverTests", "-noColor"], EVENTSTORE)
        if live_build == 0:
            qualifier_code = run.run("real PostgreSQL two-sidecar stop/restart qualifier", ["dotnet", str(live_dll), "-method", QUALIFIER, "-noColor", "-result-ctrf", str(scratch / "qualifier-ctrf.json")], EVENTSTORE, 900)
        else:
            qualifier_code = run.run("real PostgreSQL two-sidecar stop/restart qualifier", ["python3", "-c", "raise SystemExit('Fresh qualifier build failed; stale binaries were not executed')"])
        attempts.append({"attempt": 1, "exitCode": qualifier_code, "outcome": "passed" if qualifier_code == 0 else "failed", "accepted": False})
        workflow = (EVENTSTORE / ".github/workflows/integration.yml").read_text()
        selectors = re.findall(r"-method (Hexalith\.EventStore\.Server\.Tests\.\S+)", workflow)
        API.require(len(selectors) == len(set(selectors)) == 21, "Integration workflow support selector set drifted")
        support_command = ["dotnet", str(support_dll)] + [argument for selector in selectors for argument in ("-method", selector)] + ["-noColor", "-result-ctrf", str(scratch / "support-ctrf.json")]
        run.run("21 exact deterministic support selectors", support_command if support_build == 0 else ["python3", "-c", "raise SystemExit('Fresh support build failed; stale binaries were not executed')"], EVENTSTORE)
        run.run("strict EventStore capture validation", ["python3", "tools/validate-oq8-platform-evidence.py", "--capture-directory", str(capture), "--ctrf", str(scratch / "qualifier-ctrf.json"), "--support-ctrf", str(scratch / "support-ctrf.json"), "--expected-runtime-version", "1.18.2", "--expected-configuration", "Debug"], EVENTSTORE)
        # Preserve actual counts even when strict capture validation rejects the observations.
        oq8 = module("g6_oq8_result_projection", EVENTSTORE / "tools/validate-oq8-platform-evidence.py")
        if not (capture / "test-results.json").exists() and (scratch / "qualifier-ctrf.json").exists():
            raw = API.read_json(scratch / "qualifier-ctrf.json")["results"]
            test = raw["tests"][0]
            write(capture / "test-results.json", {"schemaVersion": 1, "runner": "xUnit.net v3", "command": oq8.FOCUSED_CURRENT_COMMAND.replace("/Release/", "/Debug/"), "summary": {field: raw["summary"][field] for field in ["tests", "passed", "failed", "skipped"]}, "test": {"name": test["name"], "status": test["status"], "durationMilliseconds": test["duration"], "traits": {name: [value] for name, value in test.get("labels", {}).items()}}})
        if not (capture / "deterministic-support.json").exists() and (scratch / "support-ctrf.json").exists():
            oq8.sanitize_support_ctrf(scratch / "support-ctrf.json", capture / "deterministic-support.json", oq8.SUPPORT_CURRENT_COMMAND.replace("/Release/", "/Debug/"))
        if (scratch / "fixture-diagnostics.json").exists():
            diagnostic = run.sanitize((scratch / "fixture-diagnostics.json").read_text())
            (output / "fixture-diagnostics.json").write_text(diagnostic)
        for item in baseline["pinAudit"]["appHostProjects"]:
            project = ROOT / item["path"]
            purpose = "AppHost build " + project.parent.name
            code = run.run(purpose, ["dotnet", "build", str(project), *build_args], project.parent)
            apphosts.append({"path": item["path"], "purpose": purpose, "exitCode": code, "configuration": "Debug", "dependencyMode": "source"})
        for relative in baseline["consumerInventory"]["fileBasedAppHosts"]:
            project = ROOT / relative
            purpose = "AppHost build Hexalith.Platform"
            code = run.run(purpose, ["dotnet", "build", str(project), "--configuration", "Debug", "--no-incremental", "-v:minimal", f"-p:BaseIntermediateOutputPath={scratch / 'platform-obj'}/"], project.parent)
            apphosts.append({"path": relative, "purpose": purpose, "exitCode": code, "configuration": "Debug", "dependencyMode": "packages"})
        mcp_project = "references/Hexalith.McpCli/src/Hexalith.McpCli/Hexalith.McpCli.csproj"
        run.run("McpCli transitive Dapr consumer", ["dotnet", "build", str(ROOT / mcp_project), *build_args], ROOT / "references/Hexalith.McpCli")
        write(output / "resolved-packages.json", {"schema": "hexalith.runtime-toolchain-resolved-packages.v2", "projects": resolved([item["path"] for item in apphosts] + [mcp_project], baseline, scratch)})
        run.run("G-6 mutation controls", ["python3", str(BUILDS / "Tools/test-runtime-toolchain-evidence-validator.py")])
        run.run("root workflow pin contract", ["pwsh", "-NoProfile", "-File", "tests/tools/run-ci-workflow-gates.ps1"])
    except Exception as error:
        errors.append(run.sanitize(str(error)))
        print("G6-FAILED: " + run.sanitize(str(error)), flush=True)
    finally:
        try:
            cleanup = run.cleanup()
        except Exception as error:
            errors.append("Cleanup failed: " + run.sanitize(str(error)))
            cleanup = {"schema": "hexalith.runtime-toolchain-cleanup.v2", "before": run.before,
                       "after": {"observationFailed": True}, "ownedContainers": run.owned,
                       "removedContainerIds": run.removed, "ownedProcessesStopped": False,
                       "scratchRemoved": not scratch.exists(), "ownedProcessGroups": run.process_groups, "daprNamespace": run.env.get("HEXALITH_OQ8_NAMESPACE"), "sidecarNamespaceObservations": []}
        write(output / "cleanup.json", cleanup)
    write(output / "command-record.json", {"schema": "hexalith.runtime-toolchain-command-record.v2", "commands": run.commands})
    write(output / "apphost-outcomes.json", {"schema": "hexalith.runtime-toolchain-apphost-outcomes.v2", "projects": apphosts})
    write(output / "attempts.json", {"schema": "hexalith.runtime-toolchain-attempts.v2", "attempts": attempts, "errors": errors})
    limitations = ["Pending named acceptance on the exact reviewed packet hash.", "Dirty source requires committed closure and exact root gitlinks before immutable acceptance.", "Debug checkout-source proof does not qualify published EventStore 3.110.0 archives or P1R rollback.", "Test-only hosting startup, deterministic time, intent adapter and boundary counter; Testing environment.", "Run-specific Dapr NAMESPACE isolates self-hosted discovery; application IDs sample/eventstore and the OQ8 matrix are unchanged.", "Works/mTLS Dapr 1.18.3 and catalog preview integrations are unselected or unqualified.", "Platform file-based AppHost has explicit NuGet directives; its Debug compilation is package consumption and does not qualify checkout-source behavior."]
    limitations += [f"Command failed: {item['purpose']} (exit {item['exitCode']})." for item in run.commands if item["exitCode"] != 0]
    limitations += errors
    write(output / "limitations.json", {"schema": "hexalith.runtime-toolchain-limitations.v2", "items": limitations, "publishedEventStoreArchivesQualified": False})
    for relative in ["fixture-diagnostics.json", "observed-versions.json", "resolved-packages.json", "capture/observations.json", "capture/test-results.json", "capture/deterministic-support.json", "capture/capture-validation.json"]:
        if not (output / relative).exists():
            write(output / relative, {"status": "failed", "reason": "Required observation was not produced; this is not passing evidence"})
    files = CURRENT.source_manifest(API, ROOT, baseline)
    bindings = repositories(baseline, files)
    write(output / "source-state.json", {"schema": "hexalith.runtime-toolchain-source-state.v2", "files": files, "repositories": bindings})
    paths = {"fixture-diagnostics": output / "fixture-diagnostics.json", "source-state": output / "source-state.json", "consumer-audit": output / "consumer-audit.json", "observed-versions": output / "observed-versions.json", "command-record": output / "command-record.json", "apphost-outcomes": output / "apphost-outcomes.json", "resolved-packages": output / "resolved-packages.json", "cleanup": output / "cleanup.json", "limitations": output / "limitations.json", "attempts": output / "attempts.json", "eventstore-observations": capture / "observations.json", "eventstore-qualification-results": capture / "test-results.json", "eventstore-support-results": capture / "deterministic-support.json", "eventstore-capture-validation": capture / "capture-validation.json", "baseline-governance": baseline_path, "evidence-schema": BUILDS / "schemas/hexalith.runtime-toolchain-evidence.v2.json", "evidence-validator": BUILDS / "Tools/validate-runtime-toolchain-evidence.py", "current-validator": BUILDS / "Tools/runtime_toolchain_v2.py", "mutation-tests": BUILDS / "Tools/test-runtime-toolchain-evidence-validator.py", "current-mutation-tests": BUILDS / "Tools/test_runtime_toolchain_v2.py", "central-catalog": BUILDS / "Props/Directory.Packages.props", "qualification-runner": Path(__file__).resolve()}
    artifacts = [{"kind": kind, "path": path.relative_to(ROOT).as_posix(), "sha256": API.sha256(path)} for kind, path in sorted(paths.items())]
    passed = not errors and all(item["exitCode"] == 0 for item in run.commands) and len(apphosts) == 10 and cleanup["before"] == cleanup["after"]
    counts = {}
    for label, name, selectors in [("qualification", "test-results.json", 1), ("support", "deterministic-support.json", 21)]:
        document = API.read_json(capture / name)
        summary = document.get("summary", {})
        counts[label] = {"selectors": selectors, "total": summary.get("tests", 0), "passed": summary.get("passed", 0), "failed": summary.get("failed", 1), "skipped": summary.get("skipped", 0)}
    packet = {"schema": "hexalith.runtime-toolchain-evidence.v2", "baseline": {"path": baseline_path.relative_to(ROOT).as_posix(), "sha256": API.sha256(baseline_path)}, "capturedUtc": utc(), "repositories": bindings, "artifacts": artifacts, "observedVersions": baseline["tuple"], "testCounts": counts, "approval": API.baseline_approval(baseline), "status": "pending", "technicalValidity": passed, "usableAsPrerequisite": False, "closure": {"committed": all(not item["dirty"] and (item["path"] == "." or item["revision"] == item["rootGitlink"]) for item in bindings), "sourceManifestSha256": API.manifest_digest(files)}, "acceptance": None, "containment": baseline["containment"], "rollback": baseline["rollback"]}
    write(output / "packet.json", packet)
    try:
        API.validate_packet(ROOT, output / "packet.json", baseline_path, baseline, candidate=True)
        print("G6-EVIDENCE-CANDIDATE-VALID; acceptance pending; usableAsPrerequisite=false")
        return 0
    except API.ValidationError as error:
        print("G6-EVIDENCE-INVALID: " + str(error))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
