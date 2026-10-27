#!/usr/bin/env python3
"""Own one disposable Aspire supported-read proof; retain only metadata and actual test results."""
from __future__ import annotations

import argparse
import base64
import contextlib
import hashlib
import http.server
import ipaddress
import json
import os
from pathlib import Path
import re
import secrets
import shutil
import signal
import socket
import sqlite3
import ssl
import subprocess
import sys
import tempfile
import threading
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
APPHOST = ROOT / "src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj"
TEST_PROJECT = ROOT / "tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj"
_command_deadline = None


@contextlib.contextmanager
def restart_budget(seconds: float = 180):
    """One wall-clock budget covers guards, discovery and resource commands in this Linux-only lane."""
    global _command_deadline
    previous_deadline = _command_deadline
    deadline = min(time.monotonic() + seconds, float(os.environ.get("SUPPORTED_PROJECT_READ_DEADLINE", "inf")))
    _command_deadline = min(deadline, previous_deadline or float("inf"))
    previous_handler = signal.getsignal(signal.SIGALRM)
    previous_timer = signal.getitimer(signal.ITIMER_REAL)
    def expired(_signal, _frame):
        raise TimeoutError("Owned restart exceeded its complete operation budget.")
    signal.signal(signal.SIGALRM, expired)
    signal.setitimer(signal.ITIMER_REAL, max(.001, _command_deadline - time.monotonic()))
    try:
        yield
    finally:
        signal.setitimer(signal.ITIMER_REAL, 0)
        signal.signal(signal.SIGALRM, previous_handler)
        if previous_timer[0]:
            signal.setitimer(signal.ITIMER_REAL, *previous_timer)
        _command_deadline = previous_deadline


def helper_descendants(pid: int) -> dict[int, str]:
    result, pending = {}, [pid]
    while pending:
        current = pending.pop()
        started = process_start(current)
        if started is None:
            continue
        result[current] = started
        try:
            pending.extend(int(child) for child in Path("/proc", str(current), "task", str(current), "children").read_text().split())
        except FileNotFoundError:
            pass
    return result


def stop_helper_tree(process) -> None:
    observed = helper_descendants(process.pid)
    for pid, started in reversed(list(observed.items())):
        if process_start(pid) == started:
            try:
                os.kill(pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
    process.wait(timeout=10)
    deadline = time.monotonic() + 10
    while any(process_start(pid) == started for pid, started in observed.items()):
        if time.monotonic() >= deadline:
            raise RuntimeError("Owned helper descendants remain after termination.")
        time.sleep(.01)


def test_group_members(group: int) -> dict[int, str]:
    members = {}
    for path in Path("/proc").iterdir():
        if not path.name.isdigit():
            continue
        try:
            fields = (path / "stat").read_text().rsplit(")", 1)[1].split()
        except FileNotFoundError:
            continue
        if int(fields[2]) == group and fields[0] != "Z":
            members[int(path.name)] = fields[19]
    return members


def run_test_process(command, *, env, timeout, cleanup) -> subprocess.CompletedProcess:
    """A test runner owns a fresh process group; reap its helpers before any topology teardown."""
    environment = env.copy()
    environment["SUPPORTED_PROJECT_READ_DEADLINE"] = str(time.monotonic() + timeout)
    process = subprocess.Popen(command, cwd=ROOT, env=environment, text=True, stdout=subprocess.PIPE,
                               stderr=subprocess.STDOUT, start_new_session=True)
    try:
        output, _ = process.communicate(timeout=timeout)
        return subprocess.CompletedProcess(command, process.returncode, output)
    finally:
        # The new session/group was created for this runner alone. Runtime services were
        # started before it, so they and unrelated processes are never members.
        observed = test_group_members(process.pid)
        if observed:
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
        process.wait(timeout=10)
        deadline = time.monotonic() + 10
        while test_group_members(process.pid):
            if time.monotonic() >= deadline:
                cleanup.append({"groupId": process.pid, "descendantsAbsent": False})
                raise RuntimeError("Owned test helpers remain; resource teardown is not yet safe.")
            time.sleep(.01)
        cleanup.append({"groupId": process.pid, "descendantsAbsent": True})
        if process.stdout:
            process.stdout.close()


def regression_environment(env: dict, endpoints_path: Path) -> dict:
    endpoints = json.loads(endpoints_path.read_text())
    result = env.copy()
    result["Projects__SupportedReads__Enabled"] = "false"
    result["STORY61_DAPR_HTTP_ENDPOINT"] = endpoints["eventstore-dapr-cli"]
    result["STORY61_DAPR_GRPC_ENDPOINT"] = endpoints["state-grpc"]
    return result


def free_port() -> int:
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        return listener.getsockname()[1]


def execute(command: list[str], *, env: dict[str, str] | None = None, timeout: int = 600) -> str:
    if _command_deadline is not None:
        timeout = min(timeout, _command_deadline - time.monotonic())
        if timeout <= 0:
            raise TimeoutError("Owned operation deadline elapsed before command dispatch.")
    process = subprocess.Popen(command, cwd=ROOT, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    try:
        stdout, stderr = process.communicate(timeout=timeout)
    except BaseException:
        stop_helper_tree(process)
        process.communicate(timeout=10)
        raise
    result = subprocess.CompletedProcess(command, process.returncode, stdout, stderr)
    if result.returncode:
        # Output can contain credentials, request bodies or dashboard tokens. Keep it private.
        failure = RuntimeError(f"Command failed ({result.returncode}): {' '.join(command[:3])}")
        failure.command_output = result.stdout + result.stderr
        raise failure
    return result.stdout


def b64url(value: bytes) -> str:
    return base64.urlsafe_b64encode(value).decode().rstrip("=")


def sanitized_environment(source) -> dict[str, str]:
    prefixes = ("authentication__", "folders__", "projects__", "tenants__", "eventstore__", "dapr__", "dapr_", "parameters__")
    return {key: value for key, value in source.items() if not key.casefold().replace(":", "__").startswith(prefixes)}


def source_manifest() -> dict[str, str]:
    """Bind tracked and untracked build inputs, including dirty source dependencies, without copying payloads."""
    manifest = {}
    repositories = [ROOT, *(path for path in (ROOT / "references").iterdir() if (path / ".git").exists())]
    for repository in repositories:
        paths = execute(["git", "-C", str(repository), "ls-files", "--cached", "--others", "--exclude-standard", "-z", "--",
                         "src", "tests", "Props", "tools/qualification", "*.props", "*.targets", "*.slnx", "global.json", "nuget.config", "AGENTS.md", ".editorconfig", ".gitattributes"]).split("\0")
        for relative in sorted(set(paths)):
            path = repository / relative
            if relative and path.is_file():
                manifest[str(path.relative_to(ROOT))] = hashlib.sha256(path.read_bytes()).hexdigest()
    return manifest


def sidecar_processes():
    for line in execute(["ps", "-eo", "pid=,comm="], timeout=10).splitlines():
        pid, name = line.strip().split(None, 1)
        if name != "daprd":
            continue
        try:
            arguments = [item.decode() for item in Path("/proc", pid, "cmdline").read_bytes().split(b"\0") if item]
        except FileNotFoundError:
            continue
        yield [pid, *arguments]


def verify_discovery(configuration: Path, registry: Path, isolation: str) -> dict:
    """Wait for asynchronous registrations, then require a fresh complete owned resolver observation."""
    for attempt in range(30):
        try:
            observation = observe_discovery(configuration, registry, isolation)
            observation["observationsRequired"] = attempt + 1
            return observation
        except RuntimeError:
            if attempt == 29:
                raise
            time.sleep(1)


def observe_discovery(configuration: Path, registry: Path, isolation: str) -> dict:
    """Compare real private resolver rows with the exact owned daprd processes and their namespace."""
    required = {"eventstore", "tenants", "projects", "projects-workers", "folders", "folders-workers"}
    processes = {}
    for parts in sidecar_processes():
        if len(parts) < 3 or not parts[1].endswith("/daprd") or "--config" not in parts:
            continue
        if parts[parts.index("--config") + 1] != str(configuration):
            continue
        app = parts[parts.index("--app-id") + 1]
        if app in processes or app not in required:
            raise RuntimeError("Unexpected or duplicate sidecar in private discovery scope.")
        namespace = dict(item.split(b"=", 1) for item in Path("/proc", parts[0], "environ").read_bytes().split(b"\0") if b"=" in item).get(b"NAMESPACE")
        if namespace != isolation.encode():
            raise RuntimeError("Owned sidecar actor namespace differs from its selected fixture namespace.")
        processes[app] = {"pid": int(parts[0]), "port": int(parts[parts.index("--dapr-internal-grpc-port") + 1]),
                          "binarySha256": hashlib.sha256(Path(parts[1]).read_bytes()).hexdigest()}
    if set(processes) != required or not registry.is_file():
        raise RuntimeError("Private discovery lacks the complete owned sidecar set.")
    with sqlite3.connect("file:" + str(registry) + "?mode=ro", uri=True) as database:
        tables = [row[0] for row in database.execute("SELECT name FROM sqlite_master WHERE type='table'")]
        rows = None
        for table in tables:
            quoted = '"' + table.replace('"', '""') + '"'
            columns = {row[1] for row in database.execute("PRAGMA table_info(" + quoted + ")")}
            if {"app_id", "address", "registration_id"}.issubset(columns):
                if rows is not None:
                    raise RuntimeError("Private discovery registry has ambiguous host tables.")
                rows = list(database.execute("SELECT app_id, address FROM " + quoted))
    if rows is None or len(rows) != len(required) or {row[0] for row in rows} != required:
        known = sorted({row[0] for row in rows or []} & required)
        raise RuntimeError("Private SQLite discovery membership differs from owned resources; rows="
                           + str(len(rows or [])) + ", expectedApps=" + ",".join(known))
    for app, address in rows:
        if not sidecar_listens_at(processes[app]["pid"], processes[app]["port"], address):
            raise RuntimeError("Private discovery resolved an endpoint outside its owned sidecar process.")
    return {"resolver": "sqlite", "version": "v1", "alphaTestInfrastructureOnly": True,
            "daprdSha256": sorted({item["binarySha256"] for item in processes.values()}),
            "configurationSha256": hashlib.sha256(configuration.read_bytes()).hexdigest(),
            "registryIdentitySha256": hashlib.sha256(str(registry).encode()).hexdigest(),
            "ownedSidecars": [{"appId": app, "pid": processes[app]["pid"], "namespaceVerified": True,
                              "registryAddressVerified": True} for app in sorted(required)]}


def sidecar_listens_at(pid: int, port: int, address: str) -> bool:
    host, raw_port = address.rsplit(":", 1)
    target = ipaddress.ip_address(host.strip("[]"))
    if int(raw_port) != port or target.is_unspecified or target.is_multicast:
        return False
    local = {ipaddress.ip_address(item["local"]) for interface in json.loads(execute(["ip", "-json", "address", "show"], timeout=10))
             for item in interface.get("addr_info", [])}
    if target not in local and not target.is_loopback:
        return False
    inodes = set()
    for entry in Path("/proc", str(pid), "fd").iterdir():
        try:
            link = os.readlink(entry)
        except FileNotFoundError:
            continue
        if link.startswith("socket:["):
            inodes.add(link[8:-1])
    for family in ("tcp", "tcp6"):
        for line in Path("/proc", str(pid), "net", family).read_text().splitlines()[1:]:
            fields = line.split()
            encoded, raw = fields[1].split(":")
            if fields[3] != "0A" or int(raw, 16) != port or fields[9] not in inodes:
                continue
            packed = bytes.fromhex(encoded)
            bound = ipaddress.ip_address(packed[::-1] if family == "tcp" else b"".join(packed[i:i+4][::-1] for i in range(0, 16, 4)))
            if bound == target or bound.is_unspecified and (bound.version == target.version or bound.version == 6):
                return True
    return False


def apphost_running(description: str) -> bool:
    try:
        value = json.loads(description)
    except ValueError as failure:
        raise RuntimeError("Unrecognized Aspire description; refusing to infer that no AppHost is running.") from failure
    if not isinstance(value, dict) or not isinstance(value.get("resources"), list):
        raise RuntimeError("Unrecognized Aspire description; refusing to infer that no AppHost is running.")
    return bool(value["resources"])


def exact_apphost_sessions() -> list[dict]:
    sessions = json.loads(execute(["aspire", "ps", "--format", "Json", "--non-interactive"], timeout=15))
    if not isinstance(sessions, list) or any(not isinstance(item, dict) or not isinstance(item.get("appHostPath"), str) for item in sessions):
        raise RuntimeError("Unrecognized Aspire sessions; ownership is not proven.")
    return [item for item in sessions if Path(item["appHostPath"]).resolve() == APPHOST]


def process_start(pid: int) -> str | None:
    try:
        fields = Path("/proc", str(pid), "stat").read_text().rsplit(")", 1)[1].split()
        return None if fields[0] == "Z" else fields[19]
    except FileNotFoundError:
        return None


def session_identity(session: dict, configuration: Path, isolation: str) -> dict | None:
    identity = {"_configuration": str(configuration), "_isolation": isolation,
                "configurationSha256": hashlib.sha256(configuration.read_bytes()).hexdigest(),
                "configurationPathSha256": hashlib.sha256(str(configuration).encode()).hexdigest()}
    for role in ("appHost", "cli"):
        pid = session.get(role + "Pid")
        if type(pid) is not int or pid <= 0:
            return None
        try:
            environment = dict(item.split(b"=", 1) for item in Path("/proc", str(pid), "environ").read_bytes().split(b"\0") if b"=" in item)
        except FileNotFoundError:
            return None
        if environment.get(b"Projects__SupportedReads__ConfigurationFile") != str(configuration).encode() or environment.get(b"NAMESPACE") != isolation.encode():
            return None
        started = process_start(pid)
        if started is None:
            return None
        identity[role + "Pid"] = pid
        identity[role + "StartTicks"] = started
    return identity


def find_owned_apphost(configuration: Path, isolation: str) -> dict | None:
    owned = [identity for session in exact_apphost_sessions()
             if (identity := session_identity(session, configuration, isolation)) is not None]
    if len(owned) > 1:
        raise RuntimeError("Multiple AppHost sessions use this fixture identity; refusing ambiguous cleanup.")
    return owned[0] if owned else None


def owns_current_apphost(owned: dict) -> bool:
    sessions = exact_apphost_sessions()
    return len(sessions) == 1 and session_identity(sessions[0], Path(owned["_configuration"]), owned["_isolation"]) == owned


def owned_apphost_gone(owned: dict) -> bool:
    return all(process_start(owned[role + "Pid"]) != owned[role + "StartTicks"] for role in ("appHost", "cli"))


def start_owned_apphost(run, configuration: Path, isolation: str) -> dict:
    raw = run(["aspire", "start", "--apphost", str(APPHOST), "--isolated", "--format", "Json", "--non-interactive"])
    try:
        started = json.loads(raw)
    except ValueError as failure:
        raise RuntimeError("Unrecognized Aspire start response; refusing session adoption.") from failure
    if not isinstance(started, dict) or started.get("alreadyRunning") is True or started.get("status") in ("alreadyRunning", "already-running", "existing"):
        raise RuntimeError("Aspire did not report a new owned session; refusing session adoption.")
    owned = find_owned_apphost(configuration, isolation)
    if owned is None or started.get("appHostPath") != str(APPHOST) or any(started.get(role + "Pid") != owned[role + "Pid"] for role in ("appHost", "cli")) or not owns_current_apphost(owned):
        raise RuntimeError("Started AppHost does not match this run's process and private configuration identity.")
    return owned


def stop_owned_apphost(owned: dict, run) -> None:
    if not owned_apphost_gone(owned):
        if not owns_current_apphost(owned):
            raise RuntimeError("AppHost ownership changed; refusing to stop a mismatched or replacement session.")
        run(["aspire", "stop", "--apphost", str(APPHOST), "--non-interactive"], timeout=60)
    for _ in range(50):
        if owned_apphost_gone(owned):
            return
        time.sleep(.1)
    raise RuntimeError("Owned AppHost process/session remains after stop")


def mutate_owned_folder(owned: dict, action: str, run) -> None:
    if action not in ("stop", "start") or not owns_current_apphost(owned):
        raise RuntimeError("Folder mutation requires this run's current AppHost process and private configuration identity.")
    run(["aspire", "resource", "folders", action, "--apphost", str(APPHOST), "--non-interactive"])
    if action == "start":
        run(["aspire", "wait", "folders", "--apphost", str(APPHOST), "--timeout", "120", "--non-interactive"])


def resource_graph(run) -> dict:
    graph = json.loads(run(["aspire", "describe", "--apphost", str(APPHOST), "--format", "Json", "--non-interactive"], timeout=30))
    if not isinstance(graph, dict) or not isinstance(graph.get("resources"), list):
        raise RuntimeError("Resource identity is unobservable.")
    return graph


def resource_endpoint(graph: dict, resource: str, name: str = "http") -> str:
    matches = [url["url"] for item in graph["resources"] if item.get("displayName") == resource
               for url in item.get("urls", []) if url.get("name") == name]
    if len(matches) != 1:
        raise RuntimeError("Required owned endpoint is missing or ambiguous.")
    return matches[0]


def projects_process_identity(owned: dict, graph: dict) -> dict:
    """Bind the actual Debug server, not merely Aspire's dotnet run wrapper, to this fixture."""
    matches = [item for item in graph["resources"] if item.get("displayName") == "projects"]
    project = ROOT / "src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj"
    binary = project.parent / "bin/Debug/net10.0/Hexalith.Projects.Server"
    if len(matches) != 1 or matches[0].get("resourceType") != "Project":
        raise RuntimeError("Projects resource identity is missing or ambiguous.")
    resource = matches[0]
    properties = resource.get("properties", {})
    wrapper = properties.get("executable.pid")
    if properties.get("project.path") != str(project) or type(wrapper) is not int or wrapper <= 0:
        raise RuntimeError("Projects resource is not the owned source project.")
    wrapper_start = process_start(wrapper)
    if wrapper_start is None:
        raise RuntimeError("Projects resource launcher is absent.")
    pending, candidates, seen = [wrapper], [], set()
    while pending:
        pid = pending.pop()
        if pid in seen:
            raise RuntimeError("Projects process ancestry is ambiguous.")
        seen.add(pid)
        process = Path("/proc", str(pid))
        executable = process.joinpath("exe").readlink()
        arguments = process.joinpath("cmdline").read_bytes().split(b"\0")
        if executable == binary or executable.name == "dotnet" and str(Path(str(binary) + ".dll")).encode() in arguments:
            candidates.append(pid)
        pending.extend(int(child) for child in process.joinpath("task", str(pid), "children").read_text().split())
    if len(candidates) != 1:
        raise RuntimeError("The actual Projects server process is missing or ambiguous.")
    pid = candidates[0]
    started = process_start(pid)
    environment = dict(item.split(b"=", 1) for item in Path("/proc", str(pid), "environ").read_bytes().split(b"\0") if b"=" in item)
    expected = {b"NAMESPACE": owned["_isolation"].encode(), b"EventStore__DomainService__AppId": b"projects",
                b"XDG_DATA_HOME": str(Path(owned["_configuration"]).parent / "data-projects").encode(),
                b"ASPNETCORE_ENVIRONMENT": b"Development"}
    if started is None or any(environment.get(key) != value for key, value in expected.items()):
        raise RuntimeError("Projects server does not belong to this isolated Development fixture.")
    if process_start(wrapper) != wrapper_start or process_start(pid) != started:
        raise RuntimeError("Projects process identity changed during observation.")
    return {"resourceName": resource["name"], "launcherPid": wrapper, "launcherStartTicks": wrapper_start,
            "pid": pid, "startTicks": started,
            "binarySha256": hashlib.sha256(binary.read_bytes()).hexdigest(),
            "assemblySha256": hashlib.sha256(Path(str(binary) + ".dll").read_bytes()).hexdigest()}


def projects_processes_gone(identity: dict) -> bool:
    return all(process_start(identity[pid]) != identity[start] for pid, start in
               (("pid", "startTicks"), ("launcherPid", "launcherStartTicks")))


def refresh_owned_endpoints(owned: dict, graph: dict) -> dict:
    private = Path(owned["_configuration"]).parent
    endpoints_path = private / "endpoints.json"
    endpoints = json.loads(endpoints_path.read_text())
    for name in ("eventstore", "projects", "folders", "eventstore-dapr-cli", "projects-dapr-cli", "folders-dapr-cli"):
        endpoints[name] = resource_endpoint(graph, name)
    endpoints["broker"] = resource_endpoint(graph, "supported-read-authority", "https") + "/workload-authority"
    endpoints["state-grpc"] = resource_endpoint(graph, "eventstore-dapr-cli", "grpc")
    discovery = verify_discovery(private / "discovery.json", private / "discovery.sqlite", owned["_isolation"])
    endpoints_path.write_text(json.dumps(endpoints))
    os.chmod(endpoints_path, 0o600)
    return discovery


def mutate_owned_resource(owned: dict, resource: str, action: str, run, observation: dict, *, budget_seconds: float = 180) -> None:
    with restart_budget(budget_seconds):
        _mutate_owned_resource(owned, resource, action, run, observation)


def _mutate_owned_resource(owned: dict, resource: str, action: str, run, observation: dict) -> None:
    """The only additional mutation is an owned Projects stop/start without build or seed."""
    if resource == "folders" and action in ("stop", "start"):
        mutate_owned_folder(owned, action, run)
        return
    if resource != "projects" or action != "restart":
        raise RuntimeError("Only owned Folder stop/start or Projects restart is allowed.")
    observation.update({"resource": resource, "action": action, "completed": False, "stage": "ownership"})
    def guard():
        if not owns_current_apphost(owned):
            raise RuntimeError("Projects restart requires the original owned AppHost session.")
    guard()
    before = projects_process_identity(owned, resource_graph(run))
    observation["before"] = before
    observation["appHostPid"] = owned["appHostPid"]
    observation["appHostStartTicks"] = owned["appHostStartTicks"]
    observation["configurationSha256"] = owned["configurationSha256"]
    observation["stage"] = "stop"
    guard()
    # Re-observe immediately before mutation, refusing an independently replaced server.
    if projects_process_identity(owned, resource_graph(run)) != before:
        raise RuntimeError("Projects process changed before the owned stop.")
    guard()
    run(["aspire", "resource", "projects", "stop", "--apphost", str(APPHOST), "--non-interactive"], timeout=60)
    for _ in range(100):
        if projects_processes_gone(before):
            break
        time.sleep(.1)
    else:
        raise RuntimeError("The original Projects process did not exit; restart is unproven.")
    observation["oldProcessExited"] = True
    observation["stage"] = "start"
    guard()
    run(["aspire", "resource", "projects", "start", "--apphost", str(APPHOST), "--non-interactive"], timeout=60)
    run(["aspire", "wait", "projects", "--apphost", str(APPHOST), "--timeout", "120", "--non-interactive"], timeout=135)
    guard()
    graph = resource_graph(run)
    after = projects_process_identity(owned, graph)
    observation["after"] = after
    if any((before[pid], before[start]) == (after[pid], after[start]) for pid, start in
           (("pid", "startTicks"), ("launcherPid", "launcherStartTicks"))):
        raise RuntimeError("Projects start retained an original process identity.")
    if any(before[key] != after[key] for key in ("resourceName", "binarySha256", "assemblySha256")):
        raise RuntimeError("Projects resource or Debug binary changed during restart.")
    observation["stage"] = "rediscovery"
    guard()
    observation["discovery"] = refresh_owned_endpoints(owned, graph)
    guard()
    if projects_process_identity(owned, resource_graph(run)) != after:
        raise RuntimeError("Projects process changed during endpoint rediscovery.")
    observation.update({"endpointsRediscovered": True, "completed": True, "stage": "completed"})


def container_listing() -> dict[str, set[str]]:
    rows = execute(["docker", "ps", "--all", "--no-trunc", "--format", '{"id":{{json .ID}},"names":{{json .Names}}}']).splitlines()
    observed = {}
    for line in rows:
        row = json.loads(line)
        identity, names = row.get("id"), row.get("names")
        if not isinstance(identity, str) or re.fullmatch(r"[0-9a-f]{64}", identity) is None or identity in observed or not isinstance(names, str) or not names:
            raise RuntimeError("Unrecognized Docker listing; container absence is not proven.")
        observed[identity] = {name.strip() for name in names.split(',')}
    return observed


def owned_sidecar_census(isolation: str) -> dict[str, dict]:
    """Fresh process/start identities; only confirmed process disappearance is absence."""
    owned = {}
    for parts in sidecar_processes():
        try:
            environment = Path("/proc", parts[0], "environ").read_bytes().split(b"\0")
            started = process_start(int(parts[0]))
        except FileNotFoundError:
            continue
        if started is not None and ("NAMESPACE=" + isolation).encode() in environment:
            owned[parts[0]] = {"start": started, "arguments": parts[1:],
                              "appId": parts[parts.index("--app-id") + 1]}
    return owned


def wait_for_owned_sidecar_absence(isolation: str, before: dict, *, seconds: float = 10) -> tuple[dict, bool]:
    """Bound all observations while awaiting namespace and original PID/start disappearance."""
    remaining, observation_ok = {}, False
    try:
        with restart_budget(seconds):
            while True:
                remaining, observation_ok = {}, False
                try:
                    remaining = owned_sidecar_census(isolation)
                    for pid, previous in before.items():
                        try:
                            Path("/proc", pid, "cmdline").read_bytes()
                            if process_start(int(pid)) == previous["start"]:
                                remaining[pid] = previous
                        except FileNotFoundError:
                            pass
                    observation_ok = True
                except TimeoutError:
                    raise
                except OSError:
                    pass
                if observation_ok and not remaining:
                    return remaining, True
                time.sleep(.1)
    except TimeoutError:
        return remaining, observation_ok
    except BaseException:
        return remaining, False


def cleanup_resources(owned_apphost: dict | None, owned: list[dict], isolation: str, run, report: dict, *, sidecar_wait_seconds: float = 10) -> None:
    """Attempt every owned cleanup, verifying immutable IDs and fresh process absence."""
    if owned_apphost:
        before, remaining = {}, {}
        observation_ok = True
        try:
            before = owned_sidecar_census(isolation)
        except BaseException:
            observation_ok = False
        try:
            stop_owned_apphost(owned_apphost, run)
            report["cleanup"].append({"resource": "exact-apphost", "removed": True, "absenceVerified": True})
        except BaseException:
            report["cleanup"].append({"resource": "exact-apphost", "removed": False})
            report["result"] = "failed"
        remaining, after_observation_ok = wait_for_owned_sidecar_absence(isolation, before, seconds=sidecar_wait_seconds)
        observation_ok = observation_ok and after_observation_ok
        report["ownedSidecarsObservedBeforeStop"] = len(before)
        report["ownedSidecarsAbsent"] = observation_ok and not remaining
        if not observation_ok:
            report["sidecarObservationFailed"] = True
        if remaining:
            report["remainingOwnedSidecars"] = sorted(item["appId"] for item in remaining.values())
        if not report["ownedSidecarsAbsent"]:
            report["result"] = "failed"
    for resource in reversed(owned):
        name, container_id = resource["name"], resource.get("id")
        try:
            if container_id is None and resource.get("cidfile"):
                try:
                    container_id = Path(resource["cidfile"]).read_text().strip()
                except FileNotFoundError:
                    pass
            if container_id is None:
                # A failed create has no known identity only if no container carries this unique owner.
                unknown = execute(["docker", "ps", "--all", "--no-trunc", "--filter", "label=hexalith.supported-reads=" + isolation, "--format", "{{.ID}}"])
                if unknown.strip():
                    raise RuntimeError("Container creation identity is unproven")
                report["cleanup"].append({"resource": name, "removed": True, "alreadyAbsent": True, "absenceVerified": True})
                continue
            if re.fullmatch(r"[0-9a-f]{64}", container_id) is None:
                raise RuntimeError("Invalid creation identity")
            if container_id not in container_listing():
                report["cleanup"].append({"resource": name, "containerId": container_id, "removed": True, "alreadyAbsent": True, "absenceVerified": True})
                continue
            observed = json.loads(execute(["docker", "inspect", "--format", '{"id":{{json .Id}},"owner":{{json (index .Config.Labels "hexalith.supported-reads")}}}', container_id]))
            if observed.get("id") != container_id or observed.get("owner") != isolation:
                raise RuntimeError("Ownership label mismatch")
            execute(["docker", "rm", "-f", "-v", container_id])
            if container_id in container_listing():
                raise RuntimeError("Owned container remains after removal")
            report["cleanup"].append({"resource": name, "containerId": container_id, "removed": True, "absenceVerified": True})
        except BaseException:
            report["cleanup"].append({"resource": name, "removed": False})
            report["result"] = "failed"


def preexisting_resources() -> dict:
    """Observe external process/container identities without retaining URLs, credentials or payloads."""
    apphosts = json.loads(execute(["aspire", "ps", "--format", "Json", "--non-interactive"], timeout=15))
    containers = set(execute(["docker", "ps", "--format", "{{.ID}}"]).splitlines())
    sidecars = {}
    for parts in sidecar_processes():
        if len(parts) < 3 or not parts[1].endswith("/daprd"):
            continue
        app = parts[parts.index("--app-id") + 1] if "--app-id" in parts else "unknown"
        configuration = Path(parts[parts.index("--config") + 1]) if "--config" in parts else None
        item = {"appId": app, "configurationSha256": None, "resolver": "mdns-default"}
        if configuration and configuration.is_file():
            body = configuration.read_text()
            item["configurationSha256"] = hashlib.sha256(body.encode()).hexdigest()
            registry = None
            if body.lstrip().startswith("{"):
                resolver = json.loads(body).get("spec", {}).get("nameResolution", {})
                if resolver.get("component") == "sqlite":
                    registry = resolver.get("configuration", {}).get("connectionString")
            elif re.search(r"component:\s*sqlite\s*(?:\n|$)", body):
                connection = re.search(r"connectionString:\s*(.+)", body)
                if connection:
                    raw = connection[1].strip()
                    registry = json.loads(raw) if raw.startswith('"') else raw
            if registry:
                item["resolver"] = "sqlite"
                item["registryIdentitySha256"] = hashlib.sha256(registry.encode()).hexdigest()
                with sqlite3.connect("file:" + registry + "?mode=ro", uri=True) as database:
                    memberships = []
                    for (table,) in database.execute("SELECT name FROM sqlite_master WHERE type='table'"):
                        quoted = '"' + table.replace('"', '""') + '"'
                        columns = {row[1] for row in database.execute("PRAGMA table_info(" + quoted + ")")}
                        if {"app_id", "address", "registration_id"}.issubset(columns):
                            memberships.extend(database.execute("SELECT app_id, address FROM " + quoted))
                    item["registryMembershipSha256"] = hashlib.sha256(json.dumps(sorted(memberships)).encode()).hexdigest()
        sidecars[str(parts[0])] = item
    return {"apphosts": {str(item["appHostPid"]): item["appHostPath"] for item in apphosts},
            "containers": sorted(containers), "sidecars": sidecars}


def safe_log_facts(raw: str) -> dict:
    templates = set()
    facts = set()
    assemblies = set()

    def inspect(value):
        if isinstance(value, dict):
            template = value.get("{OriginalFormat}")
            if isinstance(template, str):
                templates.add(hashlib.sha256(template.encode()).hexdigest())
            for key, item in value.items():
                if key in ("ComponentName", "Reason", "ProcessingResult", "ExceptionType") and isinstance(item, str) and re.fullmatch(r"[A-Za-z0-9_.-]{1,128}", item):
                    facts.add(key + "Sha256=" + hashlib.sha256(item.encode()).hexdigest())
                inspect(item)
        elif isinstance(value, list):
            for item in value:
                inspect(item)
        elif isinstance(value, str):
            for assembly in re.findall(r"Could not load file or assembly '([^']+)'", value):
                if re.fullmatch(r"[A-Za-z0-9_., =-]{1,256}", assembly):
                    assemblies.add(assembly)
            if value.lstrip().startswith("{"):
                try:
                    inspect(json.loads(value))
                except ValueError:
                    pass

    try:
        inspect(json.loads(raw))
    except ValueError:
        pass
    return {"templateSha256": sorted(templates), "closedFacts": sorted(facts), "missingAssemblies": sorted(assemblies)}


def verify_source_references() -> dict:
    available = {path.stem for repository in [ROOT, *(ROOT / "references").iterdir()] if (repository / "src").is_dir()
                 for path in (repository / "src").rglob("*.csproj")}
    projects = [ROOT / "src/Hexalith.Projects.Server", ROOT / "src/Hexalith.Projects.Workers",
                ROOT / "src/Hexalith.Projects.AppHost", ROOT / "tests/Hexalith.Projects.ReadAuthority",
                ROOT / "references/Hexalith.EventStore/src/Hexalith.EventStore", ROOT / "references/Hexalith.Tenants/src/Hexalith.Tenants",
                *(ROOT / ("references/Hexalith.Folders/src/" + name) for name in ("Hexalith.Folders", "Hexalith.Folders.Server", "Hexalith.Folders.Workers"))]
    observed = {}
    for project in projects:
        assets = json.loads((project / "obj/project.assets.json").read_text())
        selected = {name.rsplit("/", 1)[0]: item["type"] for name, item in assets["libraries"].items()
                    if name.rsplit("/", 1)[0] in available}
        if not selected or any(kind != "project" for kind in selected.values()):
            raise RuntimeError("A live host contains a root-available Hexalith package fallback: " + project.name)
        output = project / "bin/Debug/net10.0"
        if not (output / (project.name + ".dll")).is_file():
            raise RuntimeError("A live Debug output is missing: " + project.name)
        runtime_files = {str(path.relative_to(output)): hashlib.sha256(path.read_bytes()).hexdigest()
                         for path in output.rglob("*") if path.is_file()
                         and (path.suffix == ".dll" or path.name.endswith((".deps.json", ".runtimeconfig.json")))}
        observed[project.name] = {"rootAvailableReferences": len(selected), "allSourceProjects": True,
                                  "assetsSha256": hashlib.sha256((project / "obj/project.assets.json").read_bytes()).hexdigest(),
                                  "runtimeFiles": len(runtime_files),
                                  "runtimeFilesSha256": hashlib.sha256(json.dumps(runtime_files, sort_keys=True).encode()).hexdigest()}
    return observed


def require_test_results(assemblies) -> list[dict]:
    results = []
    for item in assemblies:
        counts = {key: int(item.attrib.get(key, "-1")) for key in ("total", "passed", "failed", "skipped")}
        if counts["total"] < 1 or counts["passed"] != counts["total"] or counts["failed"] != 0 or counts["skipped"] != 0:
            raise RuntimeError("Qualification requires actual nonempty passing test results without skips.")
        results.append({key: item.attrib[key] for key in counts})
    if not results:
        raise RuntimeError("Qualification requires an executed test assembly.")
    return results


def run_launcher_controls(evidence: Path) -> dict:
    # Main-path unit controls mock this function; no bypass is accepted from inherited configuration.
    command = [sys.executable, "-m", "unittest", "discover", "-s", "tests/tools", "-p", "test_supported_project_reads_launcher.py", "-v"]
    environment = os.environ.copy()
    result = subprocess.run(command, cwd=ROOT, env=environment, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=60)
    (evidence / "launcher-controls.log").write_text(result.stdout)
    summary = re.search(r"Ran ([0-9]+) tests? in ", result.stdout)
    if result.returncode or summary is None or int(summary[1]) < 1 or not re.search(r"^OK$", result.stdout, re.MULTILINE):
        raise RuntimeError("Launcher ownership controls did not execute successfully.")
    return {"command": command, "total": int(summary[1]), "passed": int(summary[1]), "failed": 0, "skipped": 0}


MATRIX_CASES = (
    "authorized-list-continuation-open",
    "public-user-and-workload-trust-remain-separate",
    "populated-foreign-tenant-and-absent-safe-denial",
    "owned-projects-process-restart-retains-reads-continuation-and-state",
    "inspection-permission-and-durable-metadata-only-audit",
    "audit-storage-unavailable-blocks-disclosure",
    "revocation-observes-real-head-and-invalidates-old-continuation",
    "owned-folder-outage-honest-unavailability-and-recovery",
    "authenticated-command-and-projection-compatibility",
    "duplicate-project-delivery-retains-supported-reads-and-state",
    "full-project-replay-retains-truth-and-invalidates-old-continuation",
)


def require_provider_observer_control(evidence: dict) -> None:
    if not isinstance(evidence, dict) or evidence.get("verified") is not True or evidence.get("binaryAndEscapingBoundariesExact") is not True:
        raise RuntimeError("The provider observer lacks its live byte/version comparison control.")
    count = evidence.get("domainKeyCount")
    if type(count) is not int or count < 1 or evidence.get("controlKeyCount") != 4:
        raise RuntimeError("The provider observer controls are incomplete.")
    individual, batched = evidence.get("individual"), evidence.get("batched")
    if not isinstance(individual, list) or len(individual) != count + 4 or individual != batched:
        raise RuntimeError("The provider batch differs from individual observations.")
    keys = set()
    for observation in individual:
        if not isinstance(observation, dict) or any(not isinstance(observation.get(field), str) or not re.fullmatch(r"[0-9A-F]{64}", observation[field])
                for field in ("keySha256", "bytesSha256", "etagSha256")) or observation["keySha256"] in keys:
            raise RuntimeError("The provider comparison contains missing or malformed fingerprints.")
        keys.add(observation["keySha256"])


def require_replay_comparison(comparison: dict, expected_changed: set[str], domain_keys: set[str], expected_watermarks: dict) -> None:
    """Recompute the invariant from sanitized physical observations, including every exact watermark."""
    def require(value):
        if not value:
            raise RuntimeError("Incomplete or inconsistent persisted replay comparison.")
    require(isinstance(comparison, dict))
    for field in ("valid", "bothTenantsCompared", "domainKeysAndBytesUnchanged", "exactWatermarksUnchanged", "onlyExpectedETagsChanged"):
        require(comparison.get(field) is True)
    def fingerprint_map(name):
        values = comparison.get(name)
        require(isinstance(values, list) and len(values) >= 6)
        result = {}
        for value in values:
            require(isinstance(value, dict) and value.get("status") == "observed")
            require(all(isinstance(value.get(field), str) and re.fullmatch(r"[0-9A-F]{64}", value[field]) for field in ("keySha256", "bytesSha256", "etagSha256")))
            require(value["keySha256"] not in result)
            result[value["keySha256"]] = value
        return result
    before, after = fingerprint_map("before"), fingerprint_map("after")
    require(before.keys() == after.keys() == domain_keys)
    require(all(before[key]["bytesSha256"] == after[key]["bytesSha256"] for key in before))
    changed = {key for key in before if before[key]["etagSha256"] != after[key]["etagSha256"]}
    require(changed == expected_changed)
    require(comparison.get("expectedChangedETagKeys") == sorted(expected_changed) and comparison.get("changedETagKeys") == sorted(changed))
    def watermarks(name):
        values = comparison.get(name)
        require(isinstance(values, list) and len(values) == 6)
        result, tenants = {}, {}
        for value in values:
            require(isinstance(value, dict) and value.get("status") == "observed")
            key, tenant = value.get("keySha256"), value.get("tenantSha256")
            require(key in before and key not in result and isinstance(tenant, str) and re.fullmatch(r"[0-9A-F]{64}", tenant))
            require(type(value.get("globalPosition")) is int and value["globalPosition"] > 0)
            result[key] = (tenant, value["globalPosition"])
            tenants[tenant] = tenants.get(tenant, 0) + 1
        require(len(tenants) == 2 and set(tenants.values()) == {3})
        return result
    require(watermarks("beforeWatermarks") == watermarks("afterWatermarks") == expected_watermarks)


def observed_timestamp(value):
    """Require a real timezone-aware non-default observation, rather than arbitrary text."""
    from datetime import datetime, timezone
    try:
        if not isinstance(value, str):
            raise ValueError("Missing timestamp")
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
        if parsed.tzinfo is None or parsed.utcoffset() is None or parsed.astimezone(timezone.utc) <= datetime.min.replace(tzinfo=timezone.utc):
            raise ValueError("Unbounded or default timestamp")
        return parsed
    except (TypeError, ValueError, OverflowError) as failure:
        raise RuntimeError("Evidence requires an aware non-default timestamp.") from failure


def require_distributed_replay_evidence(matrix: dict, replay: dict) -> dict:
    """Eleven exact rows and complete source/HTTP/state evidence are mandatory; summaries alone cannot pass."""
    def require(value):
        if not value:
            raise RuntimeError("The complete distributed replay evidence did not qualify.")
    require(isinstance(matrix, dict) and matrix.get("cases") == list(MATRIX_CASES))
    for flag in ("allCasesExecuted", "projectsProcessRestartVerified", "duplicateDeliveryVerified", "fullReplayVerified", "readOnlyDomainBytesAndEtagsUnchanged"):
        require(matrix.get(flag) is True)
    require(matrix.get("acceptedEquivalence") is False and matrix.get("productionReadiness") is False)
    require(isinstance(replay, dict) and replay.get("version") == 1 and replay.get("duplicateVerified") is True and replay.get("replayVerified") is True)
    census = replay.get("ownedDomainKeyCensus")
    require(isinstance(census, list) and len(census) > 6 and all(isinstance(key, str) and re.fullmatch(r"[0-9A-F]{64}", key) for key in census)
            and census == sorted(set(census)))
    domain_keys = set(census)
    observations = replay.get("observations")
    require(isinstance(observations, list) and all(isinstance(value, dict) for value in observations))
    kinds = {kind: [value for value in observations if value.get("kind") == kind]
             for kind in ("source", "duplicate", "invalid", "authentication", "rebuild", "state", "reads", "cursor", "cursor-control", "cursor-probe")}
    require(len(observations) == sum(len(values) for values in kinds.values()))
    sources = {}
    for source in kinds["source"]:
        for field in ("tenantSha256", "projectSha256", "sourceSha256", "requestEventsSha256", "catalogSha256", "retainedDispatchIdSha256", "detailKeySha256", "indexKeySha256"):
            require(isinstance(source.get(field), str) and re.fullmatch(r"[0-9A-F]{64}", source[field]))
        scope = (source["tenantSha256"], source["projectSha256"])
        require(scope not in sources and source.get("complete") is True)
        head = source.get("observedHead")
        require(type(head) is int and head >= 2 and source.get("eventCount") == head)
        events = source.get("events")
        require(isinstance(events, list) and len(events) == head)
        previous_position = 0
        for sequence, event in enumerate(events, 1):
            require(event.get("sequence") == sequence and type(event.get("globalPosition")) is int and event["globalPosition"] > previous_position)
            previous_position = event["globalPosition"]
            observed_timestamp(event.get("timestamp"))
            for field in ("sourceEventSha256", "projectionEventSha256"):
                require(isinstance(event.get(field), str) and re.fullmatch(r"[0-9A-F]{64}", event[field]))
        sources[scope] = source
    require(len(sources) == 4 and len({scope[0] for scope in sources}) == 2 and len({scope[1] for scope in sources}) == 2)
    require(len({source["detailKeySha256"] for source in sources.values()}) == 4 and len({source["indexKeySha256"] for source in sources.values()}) == 2)
    retained_ids = {source["retainedDispatchIdSha256"] for source in sources.values()}
    require(len(retained_ids) == 4)
    attempts, ids = {}, set(retained_ids)
    for kind, scenarios in (("duplicate", {"retained", "fresh"}), ("invalid", {"truncated", "mismatched-head"}), ("rebuild", {"full-prefix"})):
        require(len(kinds[kind]) == len(sources) * len(scenarios))
        seen = set()
        for attempt in kinds[kind]:
            scope = (attempt.get("tenantSha256"), attempt.get("projectSha256"))
            require(scope in sources)
            source = sources[scope]
            scenario = attempt.get("scenario")
            require(scenario in scenarios and (scope, scenario) not in seen)
            seen.add((scope, scenario))
            for field in ("sourceSha256", "catalogSha256", "retainedDispatchIdSha256", "observedHead"):
                require(attempt.get(field) == source[field])
            for field in ("dispatchIdSha256", "requestSha256", "requestEventsSha256"):
                require(isinstance(attempt.get(field), str) and re.fullmatch(r"[0-9A-F]{64}", attempt[field]))
            require(attempt.get("failureType") is None and attempt.get("httpStatus") == 200 and attempt.get("protocolVersion") == 2)
            status = {"duplicate": "AlreadyCompleted", "invalid": "Failed", "rebuild": "Completed"}[kind]
            require(attempt.get("outcomes") == [{"projectionType": "project-reads", "status": status}])
            require(attempt.get("route") == ("project/v2" if kind == "duplicate" else "project/rebuild/v1"))
            if scenario == "truncated":
                require(attempt.get("eventCount") == source["observedHead"] - 1 and attempt.get("requestHead") == source["observedHead"]
                        and attempt["requestEventsSha256"] != source["requestEventsSha256"])
            else:
                require(attempt.get("eventCount") == source["eventCount"] and attempt["requestEventsSha256"] == source["requestEventsSha256"])
                require(attempt.get("requestHead") == (None if kind == "duplicate" else source["observedHead"] - 1 if kind == "invalid" else source["observedHead"]))
            if kind == "duplicate" and scenario == "retained":
                require(attempt["dispatchIdSha256"] == source["retainedDispatchIdSha256"])
            else:
                require(attempt["dispatchIdSha256"] != source["retainedDispatchIdSha256"] and attempt["dispatchIdSha256"] not in ids)
                ids.add(attempt["dispatchIdSha256"])
            attempts[(kind, scope, scenario)] = attempt
    authentication = kinds["authentication"]
    require(len(authentication) == 8)
    required_auth = {(route, scenario) for route in ("project/v2", "project/rebuild/v1")
                     for scenario in ("anonymous", "human-bearer", "public-user-workload", "wrong-operation")}
    require({(value.get("route"), value.get("scenario")) for value in authentication} == required_auth)
    for attempt in authentication:
        require(attempt.get("failureType") is None and attempt.get("httpStatus") == (403 if attempt["scenario"] == "wrong-operation" else 401)
                and attempt.get("outcomes") is None)
        scope = (attempt.get("tenantSha256"), attempt.get("projectSha256"))
        require(scope in sources)
        source = sources[scope]
        for field in ("sourceSha256", "requestEventsSha256", "catalogSha256", "retainedDispatchIdSha256", "observedHead", "eventCount"):
            require(attempt.get(field) == source[field])
        require(attempt.get("requestHead") == source["observedHead"] and attempt.get("protocolVersion") is None)
        for field in ("requestSha256", "dispatchIdSha256"):
            require(isinstance(attempt.get(field), str) and re.fullmatch(r"[0-9A-F]{64}", attempt[field]))
        require(attempt["dispatchIdSha256"] not in ids)
        ids.add(attempt["dispatchIdSha256"])
    states = {value.get("phase"): value for value in kinds["state"]}
    phases = {"duplicate", "duplicate-reads", "invalid", "authentication", "refusal-reads", "full-rebuild", "post-rebuild-reads"}
    phases.update("rebuild:" + tenant + ":" + project for tenant, project in sources)
    require(len(states) == len(kinds["state"]) == len(phases) and states.keys() == phases)
    projection_keys = {source[field] for source in sources.values() for field in ("detailKeySha256", "indexKeySha256")}
    expected_watermarks = {}
    for scope, source in sources.items():
        tenant = scope[0]
        detail, index = source["detailKeySha256"], source["indexKeySha256"]
        require(detail != index and detail not in expected_watermarks)
        expected_watermarks[detail] = (tenant, source["events"][-1]["globalPosition"])
        tenant_max = max(value["events"][-1]["globalPosition"] for key, value in sources.items() if key[0] == tenant)
        require(index not in expected_watermarks or expected_watermarks[index] == (tenant, tenant_max))
        expected_watermarks[index] = (tenant, tenant_max)
    require(len(expected_watermarks) == 6 and projection_keys < domain_keys)
    for phase, state in states.items():
        require(state.get("failureType") is None)
        expected = projection_keys if phase == "full-rebuild" else set()
        if phase.startswith("rebuild:"):
            _, tenant, project = phase.split(":")
            expected = {sources[(tenant, project)][field] for field in ("detailKeySha256", "indexKeySha256")}
        require_replay_comparison(state.get("comparison"), expected, domain_keys, expected_watermarks)
    original = states["duplicate"]["comparison"]["before"]
    for phase in ("duplicate", "duplicate-reads", "invalid", "authentication", "refusal-reads"):
        comparison = states[phase]["comparison"]
        require(comparison["before"] == original and comparison["after"] == original)
    previous = original
    for tenant, project in sources:
        comparison = states["rebuild:" + tenant + ":" + project]["comparison"]
        require(comparison["before"] == previous)
        previous = comparison["after"]
    require(states["full-rebuild"]["comparison"]["before"] == original and states["full-rebuild"]["comparison"]["after"] == previous)
    require(states["post-rebuild-reads"]["comparison"]["before"] == previous and states["post-rebuild-reads"]["comparison"]["after"] == previous)
    require(len(kinds["reads"]) == 4 and {value.get("phase") for value in kinds["reads"]} == {"duplicate", "invalid", "authentication", "rebuild"})
    for read in kinds["reads"]:
        require(read.get("readCount") == 8 and all(read.get(field) is True for field in
                ("bothTenantsCompared", "stableMetadataAgrees", "freshPagingWithoutGapsOrDuplicates", "listOpenAgree", "foreignAndAbsentDenials")))
        if read["phase"] != "rebuild":
            require(read.get("retainedContinuationsAccepted") is True)
    require(len(kinds["cursor-control"]) == 1)
    control = kinds["cursor-control"][0].get("observation")
    require(isinstance(control, dict) and control.get("httpStatus") == 200 and control.get("responseState") == "Complete"
            and control.get("itemCount") == 1 and control.get("hasMore") is True and control.get("cursorPresent") is True)
    probes = kinds["cursor-probe"]
    require(len(probes) >= 1)
    for number, probe in enumerate(probes, 1):
        observed = probe.get("observation")
        require(isinstance(observed, dict) and observed.get("attempt") == number and observed.get("failureType") is None and observed.get("withinLifetime") is True)
        if number == len(probes):
            require(observed.get("httpStatus") == 400 and observed.get("problem") == {"status": 400, "reasonCode": "query_invalid_page", "reason": "invalid-cursor"})
        else:
            admission = observed.get("admission")
            require(observed.get("httpStatus") == 200 and observed.get("honestEmptyUnavailable") is True and isinstance(admission, dict))
            require(admission.get("responseState") == "Unavailable" and admission.get("itemCount") == 0 and admission.get("hasMore") is False
                    and admission.get("cursorPresent") is False and admission.get("asOfPresent") is True and admission.get("projectVersion") == 0)
            require(any(component.get("included") is False and component.get("freshness") in ("Unavailable", "Stale") for component in admission.get("components", [])))
    require(len(kinds["cursor"]) == 1)
    cursor = kinds["cursor"][0]
    require(cursor.get("acceptedHttpStatus") == 200 and cursor.get("rejectedHttpStatus") == 400 and cursor.get("withinLifetime") is True
            and cursor.get("beforeIndexETagSha256") != cursor.get("afterIndexETagSha256")
            and cursor.get("rejectionProblem") == {"status": 400, "reasonCode": "query_invalid_page", "reason": "invalid-cursor"})
    index_key = cursor.get("indexKeySha256")
    require(any(source["tenantSha256"] == cursor.get("tenantSha256") and source["indexKeySha256"] == index_key for source in sources.values()))
    initial_fingerprints = {value["keySha256"]: value for value in original}
    final_fingerprints = {value["keySha256"]: value for value in previous}
    require(cursor.get("beforeIndexETagSha256") == initial_fingerprints[index_key]["etagSha256"]
            and cursor.get("afterIndexETagSha256") == final_fingerprints[index_key]["etagSha256"])
    from datetime import timedelta
    try:
        requested, accepted, rejected = (observed_timestamp(cursor[name]) for name in ("requestedAt", "acceptedAt", "rejectedAt"))
        started, deadline = (observed_timestamp(cursor[name]) for name in ("observerStartedAt", "observerDeadline"))
        require(requested <= accepted <= started < rejected < requested + timedelta(minutes=15))
        require(started < deadline <= started + timedelta(seconds=90) and rejected < deadline)
        previous_probe = started
        for probe in probes:
            value = probe["observation"]
            require(observed_timestamp(value["observerStartedAt"]) == started and observed_timestamp(value["observerDeadline"]) == deadline)
            at = observed_timestamp(value["observedAt"])
            require(previous_probe <= at <= rejected and at < deadline and at < requested + timedelta(minutes=15))
            previous_probe = at
    except (KeyError, TypeError, ValueError) as failure:
        raise RuntimeError("Cursor rejection lacks separate observer and lifetime bounds.") from failure
    # Keep the causal order emitted by the fixture: summaries cannot precede the operations they prove.
    ordered = []
    for scope, source in sources.items():
        ordered.extend((source, attempts[("duplicate", scope, "retained")], attempts[("duplicate", scope, "fresh")]))
    reads = {value["phase"]: value for value in kinds["reads"]}
    ordered.extend((states["duplicate"], reads["duplicate"], states["duplicate-reads"]))
    for scope in sources:
        ordered.extend((attempts[("invalid", scope, "truncated")], attempts[("invalid", scope, "mismatched-head")]))
    ordered.extend((states["invalid"], reads["invalid"]))
    ordered.extend(authentication)
    ordered.extend((states["authentication"], reads["authentication"], states["refusal-reads"], kinds["cursor-control"][0]))
    for scope in sources:
        ordered.extend((attempts[("rebuild", scope, "full-prefix")], states["rebuild:" + scope[0] + ":" + scope[1]]))
    ordered.extend((states["full-rebuild"], *probes, cursor, reads["rebuild"], states["post-rebuild-reads"]))
    require(observations == ordered)
    return {"sources": len(sources), "duplicateAttempts": len(kinds["duplicate"]), "rebuildAttempts": len(kinds["rebuild"]),
            "invalidReplayAttempts": len(kinds["invalid"]), "authenticationRefusals": len(authentication), "persistedComparisons": len(states)}


def issuer_server(directory: Path, issuer: str) -> tuple[http.server.HTTPServer, threading.Thread]:
    modulus = execute(["openssl", "rsa", "-in", str(directory / "user.pem"), "-modulus", "-noout"])
    number = int(modulus.strip().split("=", 1)[1], 16)
    public = {"keys": [{"kty": "RSA", "use": "sig", "alg": "RS256", "kid": "synthetic-user",
                        "n": b64url(number.to_bytes((number.bit_length() + 7) // 8, "big")), "e": "AQAB"}]}

    class MetadataHandler(http.server.BaseHTTPRequestHandler):
        def do_GET(self) -> None:
            payload = ({"issuer": issuer, "jwks_uri": issuer + "/jwks", "id_token_signing_alg_values_supported": ["RS256"]}
                       if self.path == "/users/.well-known/openid-configuration" else public if self.path == "/users/jwks" else None)
            self.send_response(200 if payload else 404)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(json.dumps(payload).encode())

        def log_message(self, *_args: object) -> None:
            pass

    server = http.server.HTTPServer(("127.0.0.1", int(issuer.split(":")[2].split("/")[0])), MetadataHandler)
    context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
    context.load_cert_chain(directory / "certificate.pem", directory / "certificate.key")
    server.socket = context.wrap_socket(server.socket, server_side=True)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    return server, thread


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence", type=Path, help="New directory for sanitized evidence (must not exist).")
    parser.add_argument("--timeout", type=int, default=600)
    parser.add_argument("--artifacts", type=Path, help="Private reusable Debug artifacts for iterative triage; omitted for fresh final verification.")
    parser.add_argument("--regressions", action="store_true", help="Also run the complete Projects Integration and Server test projects before teardown.")
    parser.add_argument("--resource-action", choices=("stop", "start", "restart"), help=argparse.SUPPRESS)
    parser.add_argument("--resource", choices=("folders", "projects"), default="folders", help=argparse.SUPPRESS)
    parser.add_argument("--ownership", type=Path, help=argparse.SUPPRESS)
    args = parser.parse_args()
    if args.resource_action is not None:
        if args.ownership is None:
            parser.error("A private ownership identity is required for resource mutation.")
        observation, observation_path = {}, None
        try:
            if args.resource == "projects":
                evidence_path = Path(os.environ["SUPPORTED_PROJECT_READ_EVIDENCE"])
                if not evidence_path.is_dir():
                    raise RuntimeError("Restart evidence destination is missing.")
                observation_path = evidence_path / "projects-restart-process.json"
            if args.ownership.stat().st_mode & 0o077:
                raise RuntimeError("Ownership identity must be private.")
            owned = json.loads(args.ownership.read_text())
            if args.ownership.parent != Path(owned["_configuration"]).parent:
                raise RuntimeError("Ownership identity must belong to the private fixture directory.")
            mutate_owned_resource(owned, args.resource, args.resource_action, execute, observation)
            return 0
        except BaseException as failure:
            observation.update({"completed": False, "failureType": type(failure).__name__})
            print("Owned resource mutation refused: " + type(failure).__name__)
            return 1
        finally:
            if observation_path is not None:
                observation_path.write_text(json.dumps(observation, indent=2) + "\n")
    for program in ("aspire", "docker", "dotnet", "openssl", "ip"):
        if not shutil.which(program):
            parser.error(f"{program} is required")
    if args.timeout < 1:
        parser.error("--timeout must be positive")
    evidence = args.evidence or Path(tempfile.gettempdir()) / ("supported-project-reads-evidence-" + secrets.token_hex(8))
    evidence.mkdir(mode=0o700, parents=True, exist_ok=False)
    private = None
    isolation = "reads-" + secrets.token_hex(12)
    owned: list[dict] = []
    owned_apphost = None
    start_attempted = False
    config_path = None
    server = None
    preexisting = None
    report: dict = {"qualification": "local-distributed-proof", "acceptedEquivalence": False,
                    "productionReadiness": False, "commands": [], "cleanup": [], "matrixCompleted": False}
    cleanup_started = False
    interrupted = False
    previous_sigterm = signal.getsignal(signal.SIGTERM)

    def handle_sigterm(signum, _frame):
        nonlocal interrupted
        report["result"] = "failed"
        report["interruptedSignal"] = signal.Signals(signum).name
        if not cleanup_started and not interrupted:
            interrupted = True
            raise KeyboardInterrupt
    env = sanitized_environment(os.environ)
    env.update({"ASPNETCORE_ENVIRONMENT": "Development", "DOTNET_ENVIRONMENT": "Development",
                "EnableKeycloak": "false", "KeycloakPersistent": "false", "NAMESPACE": isolation,
                "Projects__SupportedReads__Enabled": "true", "Projects__E2E__LiveFixtures": "false",
                "HexalithCommonsRoot": str(ROOT / "references/Hexalith.Commons"),
                "HexalithFoldersRoot": str(ROOT / "references/Hexalith.Folders")})
    env["UseHexalithProjectReferences"] = "true"
    env["UseNuGetDeps"] = "false"
    for module in ("EventStore", "Tenants", "Folders", "Commons", "Conversations", "Memories", "FrontComposer", "Parties", "McpCli"):
        env["Hexalith" + module + "Root"] = str(ROOT / "references" / ("Hexalith." + module))
    env["HexalithTenantsBasePath"] = str(ROOT / "references/Hexalith.Tenants/src")

    def run(command: list[str], *, timeout: int | None = None) -> str:
        # Commands contain paths/options only. Private material is always delivered by file/configuration.
        report["commands"].append(command)
        try:
            output = execute(command, env=env, timeout=timeout or args.timeout)
        except RuntimeError as failure:
            if command[:2] == ["dotnet", "build"]:
                (evidence / f"build-{len(report['commands'])}.log").write_text(getattr(failure, "command_output", ""))
            raise
        if command[:2] == ["dotnet", "build"]:
            (evidence / f"build-{len(report['commands'])}.log").write_text(output)
        return output

    def endpoint(graph: dict, resource: str, name: str = "http") -> str:
        matches = [url["url"] for item in graph.get("resources", []) if item.get("displayName") == resource
                   for url in item.get("urls", []) if url.get("name") == name]
        if len(matches) != 1:
            raise RuntimeError(f"Required endpoint missing or ambiguous: {resource}/{name}")
        return matches[0]

    signal.signal(signal.SIGTERM, handle_sigterm)
    try:
        report["launcherControls"] = run_launcher_controls(evidence)
        previous_mask = signal.pthread_sigmask(signal.SIG_BLOCK, {signal.SIGTERM, signal.SIGINT})
        try:
            private = Path(tempfile.mkdtemp(prefix="supported-project-reads-private-"))
            os.chmod(private, 0o700)
        finally:
            signal.pthread_sigmask(signal.SIG_SETMASK, previous_mask)
        existing = run(["aspire", "describe", "--apphost", str(APPHOST), "--format", "Json", "--non-interactive"])
        # This CLI emits no describe payload when no AppHost API exists. Empty output
        # alone proves nothing; the independent structured session list must be empty.
        if existing.strip() and apphost_running(existing):
            raise RuntimeError("The exact Projects AppHost is already running; this launcher cannot adopt it.")
        report["commands"].append(["aspire", "ps", "--format", "Json", "--non-interactive"])
        if exact_apphost_sessions():
            raise RuntimeError("The exact Projects AppHost is already running; this launcher cannot adopt it.")
        preexisting = preexisting_resources()
        report["preexistingResources"] = preexisting
        report["versions"] = {"dotnet": execute(["dotnet", "--version"]).strip(),
                              "aspire": execute(["aspire", "--version"]).strip(),
                              "dockerServer": execute(["docker", "version", "--format", "{{.Server.Version}}"]).strip()}
        revisions = {}
        for path in (ROOT, *(ROOT / "references").iterdir()):
            if path == ROOT or (path / ".git").exists():
                revisions[str(path.relative_to(ROOT))] = execute(["git", "-C", str(path), "rev-parse", "HEAD"]).strip()
        report["revisions"] = revisions
        report["sourceDiffSha256"] = hashlib.sha256(execute(["git", "diff", "--binary"]).encode()).hexdigest()
        inputs = source_manifest()
        (evidence / "source-manifest.json").write_text(json.dumps(inputs, sort_keys=True, indent=2) + "\n")
        report["sourceManifestSha256"] = hashlib.sha256(json.dumps(inputs, sort_keys=True).encode()).hexdigest()
        password = secrets.token_hex(32)
        (private / "password").write_text(password)
        execute(["openssl", "req", "-x509", "-newkey", "rsa:2048", "-nodes", "-days", "1", "-subj", "/CN=localhost",
                 "-addext", "subjectAltName=DNS:localhost,IP:127.0.0.1", "-addext", "keyUsage=digitalSignature,keyCertSign",
                 "-keyout", str(private / "certificate.key"), "-out", str(private / "certificate.pem")])
        execute(["openssl", "pkcs12", "-export", "-in", str(private / "certificate.pem"), "-inkey", str(private / "certificate.key"),
                 "-out", str(private / "certificate.pfx"), "-passout", "file:" + str(private / "password")])
        execute(["openssl", "genpkey", "-algorithm", "RSA", "-pkeyopt", "rsa_keygen_bits:2048", "-out", str(private / "user.pem")])
        execute(["openssl", "genpkey", "-algorithm", "RSA", "-pkeyopt", "rsa_keygen_bits:2048", "-out", str(private / "owner.pem")])
        execute(["openssl", "pkcs8", "-topk8", "-nocrypt", "-in", str(private / "owner.pem"), "-outform", "DER", "-out", str(private / "owner.der")])
        execute(["openssl", "pkey", "-in", str(private / "owner.pem"), "-pubout", "-outform", "DER", "-out", str(private / "owner-public.der")])
        for path in private.iterdir():
            os.chmod(path, 0o600)
        for name in ("eventstore", "tenants", "projects", "projects-workers", "folders", "folders-workers"):
            (private / ("data-" + name)).mkdir(mode=0o700)
        user_issuer = f"https://127.0.0.1:{free_port()}/users"
        server, _ = issuer_server(private, user_issuer)
        dependencies = {}
        for role, port, image, command in (
            ("RedisHost", 6379, "redis:6", []),
            ("PlacementHostAddress", 50005, "daprio/dapr:1.18.7", ["./placement", "--port", "50005"]),
            ("SchedulerHostAddress", 50006, "daprio/dapr:1.18.7", ["./scheduler", "--port", "50006", "--etcd-data-dir", "/tmp/data"]),
        ):
            name = isolation + "-" + role.lower()
            resource = {"name": name, "id": None, "cidfile": str(private / (role + ".cid"))}
            owned.append(resource)
            previous_mask = signal.pthread_sigmask(signal.SIG_BLOCK, {signal.SIGTERM, signal.SIGINT})
            try:
                created = run(["docker", "create", "--cidfile", resource["cidfile"], "--name", name, "--label", "hexalith.supported-reads=" + isolation,
                               "-p", f"127.0.0.1::{port}", image, *command]).strip()
            finally:
                try:
                    try:
                        resource["id"] = Path(resource["cidfile"]).read_text().strip()
                    except FileNotFoundError:
                        pass
                finally:
                    signal.pthread_sigmask(signal.SIG_SETMASK, previous_mask)
            if re.fullmatch(r"[0-9a-f]{64}", created) is None or resource["id"] != created:
                raise RuntimeError("Container creation identity is unproven")
            run(["docker", "start", created])
            dependencies[role] = run(["docker", "port", created, str(port) + "/tcp"]).strip()
            report.setdefault("containerImages", {})[role] = execute(["docker", "inspect", "--format", "{{.Image}}", created]).strip()
        fixture = {"IsolationId": isolation, "UserIssuer": user_issuer, "UserKeyPath": str(private / "user.pem"),
                   "CertificatePath": str(private / "certificate.pfx"), "CertificatePassword": password,
                   "TrustBundle": str(private / "certificate.pem"), "EventStoreSecret": secrets.token_hex(32),
                   "ProjectsSecret": secrets.token_hex(32), "ChannelToken": secrets.token_hex(32),
                   "ReceiptKey": base64.b64encode(secrets.token_bytes(32)).decode(),
                   "OwnerPrivateKey": base64.b64encode((private / "owner.der").read_bytes()).decode(),
                   "OwnerPublicKey": base64.b64encode((private / "owner-public.der").read_bytes()).decode(),
                   "Tenant": isolation + "-tenant", "ForeignTenant": isolation + "-foreign"}
        discovery_configuration = private / "discovery.json"
        discovery_registry = private / "discovery.sqlite"
        discovery_configuration.write_text(json.dumps({"apiVersion": "dapr.io/v1alpha1", "kind": "Configuration",
            "metadata": {"name": isolation}, "spec": {"accessControl": {"defaultAction": "allow", "trustDomain": "public"},
            "nameResolution": {"component": "sqlite", "version": "v1", "configuration": {"connectionString": str(discovery_registry)}}}}))
        os.chmod(discovery_configuration, 0o600)
        fixture["DiscoveryConfiguration"] = str(discovery_configuration)
        configuration = {"Projects": {"SupportedReads": fixture}, "Dapr": dependencies, "EnableKeycloak": False, "KeycloakPersistent": False}
        config_path = private / "fixture.json"
        config_path.write_text(json.dumps(configuration))
        os.chmod(config_path, 0o600)
        env["Projects__SupportedReads__ConfigurationFile"] = str(config_path)
        env["SUPPORTED_PROJECT_READ_CONFIG"] = str(config_path)
        env["SSL_CERT_FILE"] = str(private / "certificate.pem")
        env["SUPPORTED_PROJECT_READ_EVIDENCE"] = str(evidence)
        report["configurationFingerprint"] = hashlib.sha256(json.dumps({"namespace": isolation, "dependencies": dependencies,
            "userTrust": hashlib.sha256(execute(["openssl", "rsa", "-in", str(private / "user.pem"), "-modulus", "-noout"]).encode()).hexdigest(),
            "profile": "Development-Debug-source"}, sort_keys=True).encode()).hexdigest()
        start_attempted = True
        owned_apphost = start_owned_apphost(run, config_path, isolation)
        report["ownedAppHostSession"] = {key: value for key, value in owned_apphost.items() if not key.startswith("_")}
        ownership_path = private / "apphost-session.json"
        ownership_path.write_text(json.dumps(owned_apphost))
        os.chmod(ownership_path, 0o600)
        report["liveHostSourceReferences"] = verify_source_references()
        report["nestedOwnerResiliencySha256"] = hashlib.sha256((private / "components/owner-resiliency.yaml").read_bytes()).hexdigest()
        for resource in ("supported-read-authority", "tenants", "eventstore", "projects", "projects-workers", "folders", "folders-workers",
                         "tenants-dapr-cli", "eventstore-dapr-cli", "projects-dapr-cli", "projects-workers-dapr-cli", "folders-dapr-cli", "folders-workers-dapr-cli"):
            run(["aspire", "wait", resource, "--apphost", str(APPHOST), "--timeout", str(args.timeout), "--non-interactive"])
        graph = json.loads(run(["aspire", "describe", "--apphost", str(APPHOST), "--format", "Json", "--non-interactive"]))
        report["discovery"] = verify_discovery(discovery_configuration, discovery_registry, isolation)
        report["runtimeConfigurationSha256"] = hashlib.sha256(json.dumps(graph, sort_keys=True).encode()).hexdigest()
        endpoints = {name: endpoint(graph, name) for name in ("eventstore", "projects", "folders", "eventstore-dapr-cli", "projects-dapr-cli", "folders-dapr-cli")}
        endpoints["broker"] = endpoint(graph, "supported-read-authority", "https") + "/workload-authority"
        endpoints["state-grpc"] = endpoint(graph, "eventstore-dapr-cli", "grpc")
        endpoints["ownership"] = str(ownership_path)
        endpoints["redis-container-id"] = next(item["id"] for item in owned if item["name"].endswith("-redishost"))
        (private / "endpoints.json").write_text(json.dumps(endpoints))
        env["SUPPORTED_PROJECT_READ_ENDPOINTS"] = str(private / "endpoints.json")
        report["resourceStates"] = [{"name": r.get("displayName"), "state": r.get("state"), "health": r.get("healthStatus")}
                                    for r in graph["resources"] if not r.get("displayName", "").startswith("supported-reads-")]
        artifacts = args.artifacts.resolve() if args.artifacts else private / "artifacts"
        if args.artifacts:
            artifacts.mkdir(mode=0o700, parents=True, exist_ok=True)
        report["freshArtifacts"] = args.artifacts is None
        run(["dotnet", "build", str(TEST_PROJECT), "-c", "Debug", "-m:1", "-p:SupportedProjectReadDistributed=true", "--artifacts-path", str(artifacts)])
        candidates = list((artifacts / "bin/Hexalith.Projects.Integration.Tests").glob("debug*/Hexalith.Projects.Integration.Tests.dll"))
        if len(candidates) != 1:
            raise RuntimeError("The fresh Debug test assembly is missing or ambiguous.")
        assembly = candidates[0]
        test_command = ["dotnet", str(assembly), "-class", "*SupportedProjectReadDistributedTests", "-xml", str(evidence / "distributed.xml")]
        report["commands"].append(test_command)
        report["testAssemblySha256"] = hashlib.sha256(assembly.read_bytes()).hexdigest()
        result = run_test_process(test_command, env=env, timeout=args.timeout, cleanup=report.setdefault("testProcessCleanup", []))
        # xUnit success/failure diagnostics use assertions containing statuses and closed reason codes only.
        (evidence / "distributed.log").write_text(result.stdout)
        assemblies = ET.parse(evidence / "distributed.xml").getroot().findall("assembly")
        report["tests"] = require_test_results(assemblies)
        if result.returncode:
            raise RuntimeError("Distributed assertions failed; inspect distributed.xml and the sanitized test log.")
        matrix = json.loads((evidence / "matrix.json").read_text())
        report["projectsRestart"] = json.loads((evidence / "projects-restart-process.json").read_text())
        observer_control = json.loads((evidence / "provider-observer-control.json").read_text())
        require_provider_observer_control(observer_control)
        report["providerObserverControl"] = {key: observer_control[key] for key in ("verified", "domainKeyCount", "controlKeyCount", "binaryAndEscapingBoundariesExact")}
        replay = json.loads((evidence / "projection-replay.json").read_text())
        report["projectionReplay"] = require_distributed_replay_evidence(matrix, replay)
        if not report["projectsRestart"].get("completed"):
            raise RuntimeError("The full distributed matrix did not execute.")
        report["discoveryAfterReads"] = verify_discovery(discovery_configuration, discovery_registry, isolation)
        if args.regressions:
            regression_env = regression_environment(env, private / "endpoints.json")
            report["regressions"] = {}
            for lane in ("Integration", "Server"):
                if lane == "Server":
                    server_project = ROOT / "tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj"
                    run(["dotnet", "build", str(server_project), "-c", "Debug", "-m:1", "--artifacts-path", str(artifacts)])
                lane_assembly = next((artifacts / ("bin/Hexalith.Projects." + lane + ".Tests")).glob("debug*/Hexalith.Projects." + lane + ".Tests.dll"))
                regression_command = ["dotnet", str(lane_assembly), "-xml", str(evidence / (lane.lower() + ".xml"))]
                if lane == "Integration":
                    regression_command.extend(["-class-", "*SupportedProjectReadDistributedTests"])
                report["commands"].append(regression_command)
                regression = run_test_process(regression_command, env=regression_env, timeout=args.timeout,
                                              cleanup=report.setdefault("testProcessCleanup", []))
                (evidence / (lane.lower() + ".log")).write_text(regression.stdout)
                lane_results = ET.parse(evidence / (lane.lower() + ".xml")).getroot().findall("assembly")
                report["regressions"][lane] = require_test_results(lane_results)
                if regression.returncode or not lane_results or any(int(a.attrib.get("failed", "0")) or int(a.attrib.get("skipped", "0")) for a in lane_results):
                    raise RuntimeError("The complete " + lane + " regression project did not pass without skips.")
        if source_manifest() != inputs:
            raise RuntimeError("Source inputs changed during verification; run again against stable sources.")
        if verify_source_references() != report["liveHostSourceReferences"]:
            raise RuntimeError("Live source-reference assets or runtime binaries changed during verification.")
        report["matrixCompleted"] = True
        report["result"] = "passed"
    except BaseException as failure:
        report["result"] = "failed"
        report["failure"] = str(failure) if isinstance(failure, RuntimeError) else type(failure).__name__
        if start_attempted and owned_apphost is None and config_path is not None:
            try:
                owned_apphost = find_owned_apphost(config_path, isolation)
                if owned_apphost is not None:
                    report["ownedAppHostSession"] = {key: value for key, value in owned_apphost.items() if not key.startswith("_")}
            except BaseException:
                report["partialSessionOwnershipUnproven"] = True
        try:
            diagnostic_ownership = owned_apphost is not None and owns_current_apphost(owned_apphost)
        except BaseException:
            diagnostic_ownership = False
        if diagnostic_ownership:
            report["runtimeFailureTypes"] = {}
            for resource in ("supported-read-authority", "eventstore", "tenants", "projects", "projects-workers", "folders", "folders-workers",
                             "eventstore-dapr-cli", "projects-dapr-cli", "folders-dapr-cli"):
                try:
                    logs = execute(["aspire", "logs", resource, "--apphost", str(APPHOST), "--format", "Json", "--tail", "1000", "--non-interactive"], env=env, timeout=15)
                    report.setdefault("runtimeLogFacts", {})[resource] = safe_log_facts(logs)
                    report["runtimeFailureTypes"][resource] = sorted(set(re.findall(r"(?:[A-Za-z_][A-Za-z0-9_]*\.)*[A-Za-z_][A-Za-z0-9_]*Exception", logs)))
                    report.setdefault("runtimeDenialReasons", {})[resource] = sorted({hashlib.sha256(value.encode()).hexdigest() for value in re.findall(r'(?:Reason=|"Reason":")(\w[\w-]*)', logs)})
                    report.setdefault("runtimeTransportStatuses", {})[resource] = sorted(set(re.findall(r'Response status code does not indicate success: ([1-5][0-9]{2})', logs)))
                    report.setdefault("runtimeStartupFailures", {})[resource] = (["inotify-instance-limit"]
                        if "configured user limit" in logs and "inotify instances" in logs else [])
                    report.setdefault("runtimeInvalidIdentityComponents", {})[resource] = sorted(set(re.findall(r'invalid identity component ([A-Za-z]+)', logs)))
                    # Private diagnostics are available only during this owned run, and are removed below.
                    (private / (resource + ".log")).write_text(logs)
                except BaseException:
                    pass
    finally:
        cleanup_started = True
        try:
            cleanup_resources(owned_apphost, owned, isolation, run, report)
            if preexisting is not None:
                try:
                    after = preexisting_resources()
                    preserved = (all(after["apphosts"].get(pid) == path for pid, path in preexisting["apphosts"].items())
                        and set(preexisting["containers"]).issubset(after["containers"])
                        and all(after["sidecars"].get(pid) == observation for pid, observation in preexisting["sidecars"].items()))
                    report["preexistingResourcesPreserved"] = preserved
                    if not preserved:
                        report["externalObservationsChanged"] = {
                            "apphosts": sorted(pid for pid, path in preexisting["apphosts"].items() if after["apphosts"].get(pid) != path),
                            "containers": sorted(set(preexisting["containers"]) - set(after["containers"])),
                            "sidecars": sorted(pid for pid, item in preexisting["sidecars"].items() if after["sidecars"].get(pid) != item),
                        }
                        report["result"] = "failed"
                except BaseException:
                    report["preexistingResourcesPreserved"] = False
                    report["result"] = "failed"
            if server:
                try:
                    server.shutdown()
                    server.server_close()
                except BaseException:
                    report["cleanup"].append({"resource": "synthetic-user-issuer", "removed": False})
                    report["result"] = "failed"
            try:
                if private is not None:
                    shutil.rmtree(private)
            except BaseException:
                report["result"] = "failed"
            report["privateMaterialRemoved"] = private is None or not private.exists()
            (evidence / "run.json").write_text(json.dumps(report, indent=2) + "\n")
        finally:
            signal.signal(signal.SIGTERM, previous_sigterm)
    print(f"Supported Project reads: {report['result']}; evidence: {evidence}")
    return 0 if report["result"] == "passed" else 1


if __name__ == "__main__":
    sys.exit(main())
