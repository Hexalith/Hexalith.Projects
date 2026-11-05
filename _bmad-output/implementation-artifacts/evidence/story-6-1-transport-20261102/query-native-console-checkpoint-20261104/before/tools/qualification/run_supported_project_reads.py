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
import shlex
import shutil
import signal
import socket
import sqlite3
import ssl
import stat
import subprocess
import sys
import tempfile
import threading
import time
import urllib.parse
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
APPHOST = ROOT / "src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj"
TEST_PROJECT = ROOT / "tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj"
_command_deadline = None
READ_APPS = {"eventstore", "tenants", "projects", "projects-workers", "folders", "folders-workers"}
MTLS_RESOURCES = ("dapr-sentry", "dapr-placement-mtls", "dapr-scheduler-mtls")


def private_identity(path: Path, *, directory: bool = False, private_parent: Path | None = None) -> dict:
    """Reject links and foreign/permissive files before retaining a cleanup identity."""
    if not path.is_absolute() or any(item.is_symlink() for item in (path, *path.parents)):
        raise RuntimeError("Private infrastructure path is not an absolute link-free path.")
    entry = path.stat()
    if private_parent is not None:
        if private_parent not in path.parents:
            raise RuntimeError("Canonical file is outside its private owner directory.")
        private_identity(private_parent, directory=True)
    if (entry.st_uid != os.getuid() or entry.st_mode & 0o077 and (directory or private_parent is None)
            or not (stat.S_ISDIR(entry.st_mode) if directory else stat.S_ISREG(entry.st_mode))):
        raise RuntimeError("Private infrastructure ownership or permissions differ.")
    identity = {"device": entry.st_dev, "inode": entry.st_ino}
    if not directory:
        if entry.st_size > 1024 * 1024:
            raise RuntimeError("Private infrastructure file exceeds its bound.")
        identity["sha256"] = hashlib.sha256(path.read_bytes()).hexdigest()
    return identity


def read_infrastructure_receipts(private: Path, isolation: str) -> dict:
    """Bind canonical temporary directories to exact owner-private AppHost receipts."""
    private_identity(private, directory=True)
    identities, receipts = {}, {}
    for name in ("mtls-plane.json", "catalog-plane.json"):
        path = private / name
        identities[str(path)] = private_identity(path)
        receipt = json.loads(path.read_text())
        if receipt.get("isolationId") != isolation:
            raise RuntimeError("Infrastructure receipt ownership differs.")
        receipts[name] = receipt
    mtls, catalog = receipts["mtls-plane.json"], receipts["catalog-plane.json"]
    run, certificates = Path(mtls["runDirectory"]), Path(mtls["certificateDirectory"])
    expected = private / "mtls" / ("dapr-credentials-" + hashlib.sha256(str(run).encode()).hexdigest().upper()[:32])
    catalog_directory, custody = Path(catalog["directory"]), Path(catalog["custodyDirectory"])
    if (run.parent != Path(tempfile.gettempdir()) / "hexalith-dapr" or re.fullmatch(r"[0-9a-f]{32}", run.name) is None
            or certificates != expected or custody != private / "catalog-custody"
            or catalog_directory.parent != Path(tempfile.gettempdir()) or not catalog_directory.name.startswith("hexalith-route-catalog-")
            or re.fullmatch(r"[0-9a-f]{64}", catalog.get("rootDigest", "")) is None):
        raise RuntimeError("Canonical infrastructure receipt paths or root digest differ.")
    for path in (run, certificates, catalog_directory, custody):
        identities[str(path)] = private_identity(path, directory=True)
    for file in ("ca.crt", "issuer.crt", "issuer.key"):
        path = certificates / file
        identities[str(path)] = private_identity(path, private_parent=certificates)
    catalog_file = catalog_directory / "catalog.json"
    identities[str(catalog_file)] = private_identity(catalog_file)
    if identities[str(catalog_file)]["sha256"] != catalog["rootDigest"]:
        raise RuntimeError("Canonical catalog root differs from its owner receipt.")
    return {"identities": identities, "runDirectory": str(run), "certificateDirectory": str(certificates),
            "catalogDirectory": str(catalog_directory), "custodyDirectory": str(custody), "rootDigest": catalog["rootDigest"]}


def receiver_configurations(configuration: Path, registry: Path, isolation: str) -> tuple[dict, dict]:
    private = configuration.parent
    receipts = read_infrastructure_receipts(private, isolation)
    configurations = {}
    for app in READ_APPS:
        path = Path(receipts["catalogDirectory"]) / (app + ".configuration.json")
        receipts["identities"][str(path)] = private_identity(path)
        specification = json.loads(path.read_text())["spec"]
        resolver, acl = specification["nameResolution"], specification["accessControl"]
        if (resolver != {"component": "sqlite", "version": "v1", "configuration": {"connectionString": str(registry)}}
                or acl.get("trustDomain") != "public" or acl.get("defaultAction") != "deny"
                or any(policy.get("namespace") != isolation or policy.get("trustDomain") != "public"
                       for policy in acl.get("policies", []))):
            raise RuntimeError("Receiver configuration differs from private discovery or workload identity.")
        configurations[app] = path
    return configurations, receipts


def inspect_mtls_container(identifier: str, name: str, isolation: str, receipts: dict, graph_resource: dict) -> dict:
    if re.fullmatch(r"[0-9a-f]{64}", identifier) is None:
        raise RuntimeError("Canonical container identity is missing.")
    containers = json.loads(execute(["docker", "inspect", identifier]))
    if len(containers) != 1:
        raise RuntimeError("Canonical container inspection is ambiguous.")
    item = containers[0]
    bindings = item.get("NetworkSettings", {}).get("Ports") or {}
    mapped_bindings = {port: entries for port, entries in bindings.items() if entries}
    port_configuration = item.get("HostConfig", {}).get("PortBindings")
    configured_bindings_match = (isinstance(port_configuration, dict) and set(port_configuration) == set(mapped_bindings)
        and all(len(port_configuration[port]) == len(entries)
                and all(any(configured.get("HostIp") in {"", binding["HostIp"]}
                            and configured.get("HostPort") in {"", "0", binding["HostPort"]}
                            for configured in port_configuration[port]) for binding in entries)
                for port, entries in mapped_bindings.items()))
    ports = {binding["HostPort"] for entries in bindings.values() if entries for binding in entries}
    expected_port = str(urllib.parse.urlparse(resource_endpoint({"resources": [graph_resource]}, name, "grpc")).port)
    certificate_mount = any(mount.get("Source") == receipts["certificateDirectory"]
                            and mount.get("Destination") == "/var/run/dapr/credentials" for mount in item.get("Mounts", []))
    arguments = item.get("Config", {}).get("Cmd") or []
    expected_image = "daprio/sentry:" if name == "dapr-sentry" else "daprio/dapr:"
    if (item.get("Id") != identifier or item.get("Config", {}).get("Labels", {}).get("hexalith.supported-reads") != isolation
            or re.fullmatch(r"sha256:[0-9a-f]{64}", item.get("Image", "")) is None
            or not item.get("Config", {}).get("Image", "").startswith(expected_image)
            or not certificate_mount or expected_port not in ports or not mapped_bindings or not configured_bindings_match
            or any(binding["HostIp"] not in {"127.0.0.1", "::1"} for entries in bindings.values() if entries for binding in entries)
            or "--trust-domain=localhost" not in arguments
            or (name != "dapr-sentry" and ("--tls-enabled" not in arguments or "--sentry-address=dapr-sentry:50001" not in arguments))):
        raise RuntimeError("Canonical mTLS container identity, image, listener or credentials differ.")
    if name == "dapr-scheduler-mtls" and not any(mount.get("Source") == str(Path(receipts["runDirectory"]) / "scheduler")
                                                  and mount.get("Destination") == "/var/lock" for mount in item.get("Mounts", [])):
        raise RuntimeError("Canonical scheduler data is not owned by this run.")
    return {"name": name, "id": identifier, "imageId": item["Image"], "bindings": bindings,
            "portConfiguration": port_configuration, "canonical": True}


def capture_infrastructure(private: Path, isolation: str, graph: dict) -> dict:
    """Capture immutable model/container identities before any fixture request or teardown."""
    _, receipts = receiver_configurations(private / "discovery.json", private / "discovery.sqlite", isolation)
    resources = {}
    for name in MTLS_RESOURCES:
        matches = [item for item in graph.get("resources", []) if item.get("displayName") == name]
        if len(matches) != 1:
            raise RuntimeError("Canonical mTLS model resource is missing or ambiguous.")
        if matches[0].get("state") != "Running" or matches[0].get("healthStatus") != "Healthy":
            raise RuntimeError("Canonical mTLS model resource is not healthy.")
        resources[name] = matches[0]
    containers = [inspect_mtls_container(resources[name].get("properties", {}).get("container.id", ""), name,
                                       isolation, receipts, resources[name]) for name in MTLS_RESOURCES]
    if len({item["id"] for item in containers}) != len(MTLS_RESOURCES):
        raise RuntimeError("Canonical mTLS resource identities overlap.")
    receipts.update({"containers": containers, "resources": resources, "registryPath": str(private / "discovery.sqlite")})
    return receipts


def validate_infrastructure_capture(capture: dict, isolation: str, *, stopped: bool = False) -> None:
    catalog = Path(capture["catalogDirectory"])
    if capture.get("registryIdentity") is not None:
        registry = Path(capture["registryPath"])
        identity = private_identity(registry, private_parent=registry.parent)
        if capture["registryIdentity"] != {key: identity[key] for key in ("device", "inode")}:
            raise RuntimeError("Captured private discovery registry was replaced.")
    for value, identity in capture["identities"].items():
        path = Path(value)
        # The SDK disposes its generated catalog directory after draining its children.
        if stopped and (path == catalog or catalog in path.parents) and not catalog.exists() and not catalog.is_symlink():
            continue
        certificates = Path(capture["certificateDirectory"])
        if private_identity(path, directory="sha256" not in identity,
                            private_parent=certificates if certificates in path.parents else None) != identity:
            raise RuntimeError("Captured infrastructure ownership or bytes changed; recovery material retained.")
    if not stopped:
        for item in capture["containers"]:
            if inspect_mtls_container(item["id"], item["name"], isolation, capture, capture["resources"][item["name"]]) != item:
                raise RuntimeError("Captured mTLS container image or listeners changed.")


def cleanup_private_material(private: Path | None, isolation: str, capture: dict | None,
                             start_attempted: bool, report: dict) -> None:
    """Delete credentials only after exact process/container and receipt proof succeeds."""
    try:
        if private is None:
            return
        private_identity(private, directory=True)
        if (any(not item.get("absenceVerified") for item in report["cleanup"])
                or report.get("infrastructureOwnershipChanged") or report.get("infrastructureOwnershipUnproven")
                or start_attempted and (capture is None or not report.get("ownedSidecarsAbsent")
                                        or not any(item["resource"] == "exact-apphost" and item.get("absenceVerified") for item in report["cleanup"]))):
            raise RuntimeError("Infrastructure absence is not proven.")
        if start_attempted:
            remaining = execute(["docker", "ps", "--all", "--no-trunc", "--filter",
                                 "label=hexalith.supported-reads=" + isolation, "--format", "{{.ID}}"])
            if remaining.strip():
                raise RuntimeError("Unexpected owned containers remain.")
            validate_infrastructure_capture(capture, isolation, stopped=True)
            certificate = Path(capture["certificateDirectory"])
            if set(certificate.parent.iterdir()) != {certificate}:
                raise RuntimeError("Unexpected private issuer directories remain.")
            for path in (Path(capture["catalogDirectory"]), Path(capture["runDirectory"])):
                if path.exists():
                    shutil.rmtree(path)
                if path.exists() or path.is_symlink():
                    raise RuntimeError("Canonical private directory removal failed.")
        for current, directories, _ in os.walk(private):
            for directory in directories:
                path = Path(current, directory)
                if not path.is_symlink():
                    path.chmod(0o700)
        shutil.rmtree(private)
    except BaseException:
        report["result"] = "failed"
        report["recoveryMaterialRetained"] = True
        if private is not None:
            report["recoveryDirectory"] = str(private)
    finally:
        report["privateMaterialRemoved"] = private is None or not private.exists()


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
    required = READ_APPS
    configurations, receipts = receiver_configurations(configuration, registry, isolation)
    registry_identity = private_identity(registry, private_parent=configuration.parent)
    retained_path = configuration.parent / "infrastructure-observed.json"
    retained = json.loads(retained_path.read_text())
    private_identity(retained_path)
    if any(receipts["identities"].get(path) != identity for path, identity in retained["identities"].items()):
        raise RuntimeError("Infrastructure receipt or receiver configuration changed after capture.")
    if retained.get("registryIdentity") is not None and retained["registryIdentity"] != {key: registry_identity[key] for key in ("device", "inode")}:
        raise RuntimeError("Private discovery registry was replaced.")
    processes = {}
    for parts in sidecar_processes():
        if len(parts) < 3 or not parts[1].endswith("/daprd"):
            continue
        selected_config = parts[parts.index("--config") + 1] if "--config" in parts else ""
        if selected_config not in {str(path) for path in configurations.values()}:
            environment = Path("/proc", parts[0], "environ").read_bytes().split(b"\0")
            if ("NAMESPACE=" + isolation).encode() in environment:
                raise RuntimeError("Unexpected sidecar configuration in the owned namespace.")
            continue
        app = parts[parts.index("--app-id") + 1]
        if app in processes or app not in required:
            raise RuntimeError("Unexpected or duplicate sidecar in private discovery scope.")
        environment = dict(item.split(b"=", 1) for item in Path("/proc", parts[0], "environ").read_bytes().split(b"\0") if b"=" in item)
        if selected_config != str(configurations[app]) or environment.get(b"NAMESPACE") != isolation.encode():
            raise RuntimeError("Owned sidecar configuration or actor namespace differs.")
        for key, expected in (("DAPR_CONTROLPLANE_TRUST_DOMAIN", b"localhost"), ("DAPR_CONTROLPLANE_NAMESPACE", b"default")):
            if environment.get(key.encode()) != expected:
                raise RuntimeError("Native sidecar control-plane identity differs.")
        for key, file in (("DAPR_TRUST_ANCHORS", "ca.crt"), ("DAPR_CERT_CHAIN", "issuer.crt"), ("DAPR_CERT_KEY", "issuer.key")):
            credential = Path(receipts["certificateDirectory"]) / file
            private_identity(credential, private_parent=Path(receipts["certificateDirectory"]))
            if environment.get(key.encode()) != credential.read_bytes():
                raise RuntimeError("Native sidecar issuer identity differs from its owned Sentry.")
        if "--enable-mtls" not in parts:
            raise RuntimeError("Native sidecar has no mTLS admission.")
        for flag, resource in (("--sentry-address", "dapr-sentry"), ("--placement-host-address", "dapr-placement-mtls"),
                               ("--scheduler-host-address", "dapr-scheduler-mtls")):
            actual = parts[parts.index(flag) + 1] if flag in parts else ""
            expected = resource_endpoint({"resources": [retained["resources"][resource]]}, resource, "grpc")
            parsed = urllib.parse.urlparse(expected)
            if actual not in {f"localhost:{parsed.port}", f"127.0.0.1:{parsed.port}"}:
                raise RuntimeError("Native sidecar uses a different canonical control-plane listener.")
        processes[app] = {"pid": int(parts[0]), "port": int(parts[parts.index("--dapr-internal-grpc-port") + 1]),
                          "startTicks": process_start(int(parts[0])),
                          "binarySha256": hashlib.sha256(Path(parts[1]).read_bytes()).hexdigest()}
        if processes[app]["startTicks"] is None:
            raise RuntimeError("Owned native sidecar process identity is missing.")
    if set(processes) != required or not registry.is_file():
        raise RuntimeError("Private discovery lacks the complete owned sidecar set.")
    with contextlib.closing(sqlite3.connect("file:" + str(registry) + "?mode=ro", uri=True)) as database:
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
        if process_start(processes[app]["pid"]) != processes[app]["startTicks"]:
            raise RuntimeError("Owned native sidecar process identity changed during discovery.")
    current_registry = private_identity(registry, private_parent=configuration.parent)
    if any(current_registry[key] != registry_identity[key] for key in ("device", "inode")):
        raise RuntimeError("Private discovery registry changed during observation.")
    retained["registryIdentity"] = {key: registry_identity[key] for key in ("device", "inode")}
    retained_path.write_text(json.dumps(retained))
    return {"resolver": "sqlite", "version": "v1", "alphaTestInfrastructureOnly": True,
            "daprdSha256": sorted({item["binarySha256"] for item in processes.values()}),
            "configurationSha256": {app: receipts["identities"][str(path)]["sha256"] for app, path in sorted(configurations.items())},
            "registryIdentitySha256": hashlib.sha256(str(registry).encode()).hexdigest(),
            "ownedSidecars": [{"appId": app, "pid": processes[app]["pid"], "namespaceVerified": True,
                              "startTicks": processes[app]["startTicks"],
                              "registryAddressVerified": True, "mtlsIdentityVerified": True} for app in sorted(required)]}


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
            if resource.get("canonical"):
                current = json.loads(execute(["docker", "inspect", container_id]))
                if (len(current) != 1 or current[0].get("Id") != container_id or current[0].get("Image") != resource["imageId"]
                        or current[0].get("HostConfig", {}).get("PortBindings") != resource["portConfiguration"]):
                    raise RuntimeError("Canonical container identity changed; refusing removal.")
            execute(["docker", "rm", "-f", "-v", container_id])
            if container_id in container_listing():
                raise RuntimeError("Owned container remains after removal")
            report["cleanup"].append({"resource": name, "containerId": container_id, "removed": True, "absenceVerified": True})
        except BaseException as failure:
            # Aspire may finish removing an exact captured container between listing and
            # inspection/removal. Only Docker's exact missing-ID response plus a fresh
            # successful complete listing can prove that race; all other failures retain recovery.
            missing = (isinstance(container_id, str) and re.fullmatch(r"[0-9a-f]{64}", container_id)
                and re.fullmatch(r"(?:\[\]\s*)?(?:Error(?: response from daemon)?: No such (?:object|container): |error: no such object: )"
                                 + re.escape(container_id) + r"\s*", getattr(failure, "command_output", "")))
            try:
                absent = bool(missing) and container_id not in container_listing()
            except BaseException:
                absent = False
            if absent:
                report["cleanup"].append({"resource": name, "containerId": container_id, "removed": True,
                                          "alreadyAbsent": True, "absenceVerified": True, "disappearedDuringCleanup": True})
            else:
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
                with contextlib.closing(sqlite3.connect("file:" + registry + "?mode=ro", uri=True)) as database:
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
    native = set()

    def inspect_native(value):
        # These are observations of the selected native runtime, never authority or readiness.
        if (value.get("scope") != "dapr.runtime" or value.get("type") != "log"
                or not isinstance(value.get("msg"), str)):
            return
        message = value["msg"]
        if value.get("level") == "info" and message == "Initialized name resolution to sqlite":
            native.add("SqliteNameResolutionInitialized")
        if (value.get("level") not in {"error", "fatal"} or "[INIT_COMPONENT_FAILURE]" not in message
                or "nr (sqlite/v1)" not in message):
            return
        native.add("SqliteNameResolutionInitializationFailed")
        if "failed to perform migrations" in message:
            native.add("SqliteNameResolutionMigrationsFailed")
        if re.search(r"\bSQLITE_BUSY\b", message):
            native.add("SqliteBusy")
        if re.search(r"\bSQLITE_LOCKED\b", message):
            native.add("SqliteLocked")

    def inspect(value):
        if isinstance(value, dict):
            inspect_native(value)
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
            if value.startswith("time="):
                try:
                    fields = dict(item.split("=", 1) for item in shlex.split(value) if "=" in item)
                    inspect_native(fields)
                except ValueError:
                    pass
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
        for line in raw.splitlines():
            inspect(line)
    return {"templateSha256": sorted(templates), "closedFacts": sorted(facts), "missingAssemblies": sorted(assemblies),
            "nativeStartupFacts": sorted(native)}


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
        source_projects = {}
        for name, item in assets["libraries"].items():
            module = name.rsplit("/", 1)[0]
            if module not in selected:
                continue
            coordinate = item.get("msbuildProject") or item.get("path")
            path = (project / coordinate).resolve() if isinstance(coordinate, str) else None
            if path is None or not path.is_file() or path.suffix != ".csproj" or ROOT not in path.parents:
                raise RuntimeError("An actual source project coordinate is missing or outside the owned workspace.")
            if module in source_projects and source_projects[module] != str(path):
                raise RuntimeError("A source dependency coordinate is ambiguous.")
            source_projects[module] = str(path)
        observed[project.name] = {"rootAvailableReferences": len(selected), "allSourceProjects": True, "sourceProjects": source_projects,
                                  "assetsSha256": hashlib.sha256((project / "obj/project.assets.json").read_bytes()).hexdigest(),
                                  "runtimeFiles": len(runtime_files),
                                  "runtimeFilesSha256": hashlib.sha256(json.dumps(runtime_files, sort_keys=True).encode()).hexdigest()}
    return observed


def require_test_results(assemblies) -> list[dict]:
    results = []
    for item in assemblies:
        try:
            counts = {key: int(item.attrib.get(key, "-1")) for key in ("total", "passed", "failed", "skipped")}
            unexecuted = int(item.attrib.get("not-run", "0"))
            errors = int(item.attrib.get("errors", "0"))
        except (ValueError, TypeError) as failure:
            raise RuntimeError("Native test counts are malformed.") from failure
        if counts["total"] < 1 or counts["passed"] != counts["total"] or counts["failed"] != 0 or counts["skipped"] != 0 or unexecuted != 0 or errors != 0:
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
    "duplicate-project-delivery-retains-supported-reads-and-state",
    "full-project-replay-retains-truth-and-invalidates-old-continuation",
    "authorized-list-continuation-open",
    "public-user-and-workload-trust-remain-separate",
    "populated-foreign-tenant-and-absent-safe-denial",
    "owned-projects-process-restart-retains-reads-continuation-and-state",
    "inspection-permission-and-durable-metadata-only-audit",
    "audit-storage-unavailable-blocks-disclosure",
    "revocation-observes-real-head-and-invalidates-old-continuation",
    "owned-folder-outage-honest-unavailability-and-recovery",
    "authenticated-command-and-projection-compatibility",
)


SCENARIO_GROUPS = ("duplicate", "full-replay", "main")
SCENARIO_CASES = {"duplicate": MATRIX_CASES[:1], "full-replay": MATRIX_CASES[1:2], "main": MATRIX_CASES[2:]}
SCENARIO_METHODS = {
    "duplicate": "DuplicateDeliveryUsesActualPersistedOwnersAsync",
    "full-replay": "FullReplayUsesActualPersistedOwnersAsync",
    "main": "MainReadsUseActualPersistedOwnersAsync",
}
SCENARIO_TEST_TYPE = "Hexalith.Projects.Integration.Tests.SupportedProjectReadDistributedTests"


def require_scenario_matrix(matrix: dict, scenario: str, provider_isolation_sha: str | None = None) -> None:
    """Require the finite exact native scenario receipt; it never represents another provider."""
    flags = {"readOnlyDomainBytesAndEtagsUnchanged", "projectsProcessRestartVerified", "duplicateDeliveryVerified", "fullReplayVerified",
             "preRestartContinuationAcceptedAfterRestart", "freshPagingAgrees", "retainedCursorAcceptedImmediatelyBeforeRevocation",
             "cursorRejectionWithinLifetime", "postRestartForeignAndAbsentDenialsVerified", "bothTenantDomainWatermarksUnchanged",
             "actualDaprDiscovery", "actualOwnerActorHeads", "acceptedEquivalence", "productionReadiness"}
    fields = flags | {"version", "qualificationGroup", "providerIsolationSha256", "allGroupCasesExecuted", "cases", "providerScope"}
    if (scenario not in SCENARIO_GROUPS or not isinstance(matrix, dict) or set(matrix) != fields
            or matrix.get("version") != 2 or matrix.get("qualificationGroup") != scenario
            or not isinstance(matrix.get("providerIsolationSha256"), str) or not re.fullmatch(r"[0-9A-F]{64}", matrix["providerIsolationSha256"])
            or provider_isolation_sha is not None and matrix["providerIsolationSha256"] != provider_isolation_sha
            or matrix.get("providerScope") != "one-fresh-owned-runtime" or matrix.get("allGroupCasesExecuted") is not True
            or matrix.get("cases") != list(SCENARIO_CASES[scenario])):
        raise RuntimeError("The exact native scenario cases did not qualify.")
    always = {"readOnlyDomainBytesAndEtagsUnchanged", "freshPagingAgrees", "bothTenantDomainWatermarksUnchanged", "actualDaprDiscovery", "actualOwnerActorHeads"}
    main = {"projectsProcessRestartVerified", "preRestartContinuationAcceptedAfterRestart", "retainedCursorAcceptedImmediatelyBeforeRevocation",
            "cursorRejectionWithinLifetime", "postRestartForeignAndAbsentDenialsVerified"}
    expected = always | (main if scenario == "main" else {"duplicateDeliveryVerified"} if scenario == "duplicate" else {"fullReplayVerified"})
    if any(matrix.get(field) is not (field in expected) for field in flags):
        raise RuntimeError("A native scenario claimed an unexecuted proof or omitted its own result.")


def require_complete_native_results(assemblies) -> list[dict]:
    results = require_test_results(assemblies)
    for assembly in assemblies:
        tests = assembly.findall(".//test")
        if len(tests) != int(assembly.get("total")) or any(test.get("result") != "Pass" for test in tests):
            raise RuntimeError("Native test records are missing, skipped, failed or substituted with counters.")
    return results


def require_native_scenario(assemblies, scenario: str, provider_isolation_sha: str, assembly_path: str, assembly_sha: str) -> list[dict]:
    """Bind a nonempty all-passing native XML result to its one exact selected method."""
    if scenario not in SCENARIO_GROUPS:
        raise RuntimeError("Unknown native qualification scenario.")
    if (not isinstance(provider_isolation_sha, str) or not re.fullmatch(r"[0-9A-F]{64}", provider_isolation_sha)
            or not isinstance(assembly_path, str) or not Path(assembly_path).is_absolute()
            or not isinstance(assembly_sha, str) or not re.fullmatch(r"[0-9a-f]{64}", assembly_sha)):
        raise RuntimeError("Native scenario provider/assembly bindings are missing.")
    results = require_test_results(assemblies)
    tests = [test for assembly in assemblies for test in assembly.findall(".//test")]
    if (len(assemblies) != 1 or len(tests) != 1 or any(assemblies[0].get(key) != value for key, value in
            {"total": "1", "passed": "1", "failed": "0", "skipped": "0", "not-run": "0"}.items())
            or tests[0].get("result") != "Pass" or tests[0].get("type") != SCENARIO_TEST_TYPE
            or tests[0].get("method") != SCENARIO_METHODS[scenario] or assemblies[0].get("name") != assembly_path):
        raise RuntimeError("Native output does not prove the exact selected scenario without omissions.")
    marker = "supported-read-scenario-v2|" + scenario + "|" + provider_isolation_sha + "|" + assembly_sha
    markers = [line.strip() for line in (tests[0].findtext("output") or "").splitlines() if line.strip().startswith("supported-read-scenario-v2|")]
    if markers != [marker]:
        raise RuntimeError("Native scenario output belongs to another provider, assembly or invocation.")
    return results


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


def require_distributed_replay_evidence(matrix: dict, replay: dict, qualification_group: str | None = None) -> dict:
    """Eleven exact rows and complete source/HTTP/state evidence are mandatory; summaries alone cannot pass."""
    def require(value):
        if not value:
            raise RuntimeError("The complete distributed replay evidence did not qualify.")
    require(qualification_group in (None, "duplicate", "full-replay"))
    duplicate = qualification_group in (None, "duplicate")
    full = qualification_group in (None, "full-replay")
    if qualification_group is None:
        require(isinstance(matrix, dict) and matrix.get("cases") == list(MATRIX_CASES))
        for flag in ("allCasesExecuted", "projectsProcessRestartVerified", "duplicateDeliveryVerified", "fullReplayVerified", "readOnlyDomainBytesAndEtagsUnchanged"):
            require(matrix.get(flag) is True)
        require(matrix.get("acceptedEquivalence") is False and matrix.get("productionReadiness") is False)
        require(isinstance(replay, dict) and replay.get("version") == 1)
    else:
        require_scenario_matrix(matrix, qualification_group)
        require(isinstance(replay, dict) and set(replay) == {"version", "qualificationGroup", "providerIsolationSha256", "sourceBaselineVerified", "duplicateVerified", "replayVerified", "ownedDomainKeyCensus", "observations"}
                and replay.get("version") == 2 and replay.get("qualificationGroup") == qualification_group
                and replay.get("sourceBaselineVerified") is True and replay.get("providerIsolationSha256") == matrix["providerIsolationSha256"])
    require(replay.get("duplicateVerified") is duplicate and replay.get("replayVerified") is full)
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
        if kind == "duplicate" and not duplicate or kind in ("invalid", "rebuild") and not full:
            scenarios = set()
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
    require(len(authentication) == (8 if full else 0))
    required_auth = {(route, scenario) for route in ("project/v2", "project/rebuild/v1")
                     for scenario in ("anonymous", "human-bearer", "public-user-workload", "wrong-operation")}
    if not full:
        required_auth = set()
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
    phases = {"duplicate", "duplicate-reads"} if duplicate else set()
    if full:
        phases.update({"invalid", "authentication", "refusal-reads", "full-rebuild", "post-rebuild-reads"})
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
    original = states["duplicate" if duplicate else "invalid"]["comparison"]["before"]
    for phase in ({"duplicate", "duplicate-reads", "invalid", "authentication", "refusal-reads"} & states.keys()):
        comparison = states[phase]["comparison"]
        require(comparison["before"] == original and comparison["after"] == original)
    if full:
        previous = original
        for tenant, project in sources:
            comparison = states["rebuild:" + tenant + ":" + project]["comparison"]
            require(comparison["before"] == previous)
            previous = comparison["after"]
        require(states["full-rebuild"]["comparison"]["before"] == original and states["full-rebuild"]["comparison"]["after"] == previous)
        require(states["post-rebuild-reads"]["comparison"]["before"] == previous and states["post-rebuild-reads"]["comparison"]["after"] == previous)
    read_phases = ({"duplicate"} if duplicate else set()) | ({"invalid", "authentication", "rebuild"} if full else set())
    require(len(kinds["reads"]) == len(read_phases) and {value.get("phase") for value in kinds["reads"]} == read_phases)
    for read in kinds["reads"]:
        require(read.get("readCount") == 8 and all(read.get(field) is True for field in
                ("bothTenantsCompared", "stableMetadataAgrees", "freshPagingWithoutGapsOrDuplicates", "listOpenAgree", "foreignAndAbsentDenials")))
        if read["phase"] != "rebuild":
            require(read.get("retainedContinuationsAccepted") is True)
    if not full:
        require(not kinds["cursor-control"] and not kinds["cursor-probe"] and not kinds["cursor"])
        ordered = []
        for scope, source in sources.items():
            ordered.extend((source, attempts[("duplicate", scope, "retained")], attempts[("duplicate", scope, "fresh")]))
        ordered.extend((states["duplicate"], kinds["reads"][0], states["duplicate-reads"]))
        require(observations == ordered)
        return {"sources": 4, "duplicateAttempts": 8, "rebuildAttempts": 0, "invalidReplayAttempts": 0, "authenticationRefusals": 0, "persistedComparisons": len(states)}
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
    reads = {value["phase"]: value for value in kinds["reads"]}
    if duplicate:
        for scope, source in sources.items():
            ordered.extend((source, attempts[("duplicate", scope, "retained")], attempts[("duplicate", scope, "fresh")]))
        ordered.extend((states["duplicate"], reads["duplicate"], states["duplicate-reads"]))
    for scope, source in sources.items():
        if not duplicate:
            ordered.append(source)
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

COMPILATION_PROJECTS = (
    "src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj",
    "src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj",
    "src/Hexalith.Projects.Workers/Hexalith.Projects.Workers.csproj",
    "references/Hexalith.EventStore/src/Hexalith.EventStore/Hexalith.EventStore.csproj",
    "references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Hexalith.EventStore.ServiceDefaults.csproj",
    "references/Hexalith.EventStore/src/Hexalith.EventStore.Gateway/Hexalith.EventStore.Gateway.csproj",
    "references/Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj",
    "references/Hexalith.Folders/src/Hexalith.Folders.Server/Hexalith.Folders.Server.csproj",
    "references/Hexalith.Folders/src/Hexalith.Folders.Workers/Hexalith.Folders.Workers.csproj",
    "tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj",
)
COMPILATION_PROPERTIES = ("Configuration", "Optimize", "DefineConstants", "DebugType", "UseHexalithProjectReferences", "UseNuGetDeps", "EnablePrivateCatalogInvocationProof")
RUNTIME_PROJECTS = dict(zip(("eventstore", "tenants", "projects", "projects-workers", "folders", "folders-workers"), (
    "references/Hexalith.EventStore/src/Hexalith.EventStore/Hexalith.EventStore.csproj",
    "references/Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj",
    "src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj",
    "src/Hexalith.Projects.Workers/Hexalith.Projects.Workers.csproj",
    "references/Hexalith.Folders/src/Hexalith.Folders.Server/Hexalith.Folders.Server.csproj",
    "references/Hexalith.Folders/src/Hexalith.Folders.Workers/Hexalith.Folders.Workers.csproj",
)))
RUNTIME_FLAGS = {"EventStore__RouteCatalog__ScopedDecodeReuse": "true", "EventStore__RouteCatalog__SerializeLocalLeaseRelease": "false",
                 "EventStore__RouteCatalog__RetainValidatedCatalogBytes": "true"}


def require_compilation_profile(profile: dict) -> None:
    """Verify actual evaluated ordinary Debug properties, including the reusable gated SDK assemblies."""
    if not isinstance(profile, dict) or set(profile) != set(COMPILATION_PROJECTS):
        raise RuntimeError("The complete evaluated Debug/source compilation profile is missing.")
    for values in profile.values():
        if (not isinstance(values, dict) or set(values) != set(COMPILATION_PROPERTIES)
                or values["Configuration"] != "Debug" or values["DebugType"] != "portable"
                or any(not isinstance(value, str) for value in values.values())
                or values["Optimize"].lower() != "false" or values["UseHexalithProjectReferences"].lower() != "true"
                or values["UseNuGetDeps"].lower() != "false" or values["EnablePrivateCatalogInvocationProof"].lower() != "true"
                or "DEBUG" not in values["DefineConstants"].split(";")):
            raise RuntimeError("A scenario is outside the actual ordinary Debug/source proof mode.")


def observe_compilation_profile(run) -> dict:
    profile = {}
    for project in COMPILATION_PROJECTS:
        result = json.loads(run(["dotnet", "msbuild", str(ROOT / project), "-nologo", "-p:Configuration=Debug",
                                 "-p:SupportedProjectReadDistributed=true", "-getProperty:" + ",".join(COMPILATION_PROPERTIES)]))
        profile[project] = result.get("Properties")
    require_compilation_profile(profile)
    return profile


def source_dependency_graph(observations: dict) -> dict:
    """Compare actual source project coordinates across fresh mappings without equating emitted paths/hashes."""
    expected_projects = {"Hexalith.Projects.Server", "Hexalith.Projects.Workers", "Hexalith.Projects.AppHost", "Hexalith.Projects.ReadAuthority",
                         "Hexalith.EventStore", "Hexalith.Tenants", "Hexalith.Folders", "Hexalith.Folders.Server", "Hexalith.Folders.Workers"}
    if not isinstance(observations, dict) or set(observations) != expected_projects:
        raise RuntimeError("The complete actual source dependency observations are missing.")
    result = {}
    for project, observation in observations.items():
        graph = observation.get("sourceProjects") if isinstance(observation, dict) else None
        if (not isinstance(observation, dict) or observation.get("allSourceProjects") is not True or not isinstance(graph, dict) or not graph
                or len(graph) != observation.get("rootAvailableReferences")
                or any(not isinstance(name, str) or not isinstance(path, str) or not Path(path).is_absolute() or not path.endswith(".csproj") for name, path in graph.items())):
            raise RuntimeError("A live host source graph is incomplete or uses package fallback.")
        result[project] = graph
    return result


def observe_runtime_modes(graph: dict, owned: dict) -> dict:
    """Read finite options only from each exact owned native server, preserving its process identity."""
    result = {}
    for app, relative in RUNTIME_PROJECTS.items():
        resource = [value for value in graph["resources"] if value.get("displayName") == app]
        if len(resource) != 1:
            raise RuntimeError("A native scenario peer is missing or ambiguous.")
        properties = resource[0].get("properties", {})
        wrapper = properties.get("executable.pid")
        project = ROOT / relative
        if type(wrapper) is not int or properties.get("project.path") != str(project) or process_start(wrapper) is None:
            raise RuntimeError("A native scenario peer is not its actual source project.")
        before = process_start(wrapper)
        binary = project.parent / "bin/Debug/net10.0" / project.stem
        pending, candidates, seen = [wrapper], [], set()
        while pending:
            pid = pending.pop()
            if pid in seen:
                raise RuntimeError("A native scenario peer ancestry is ambiguous.")
            seen.add(pid)
            process = Path("/proc", str(pid))
            executable = process.joinpath("exe").readlink()
            arguments = process.joinpath("cmdline").read_bytes().split(b"\0")
            if executable == binary or executable.name == "dotnet" and str(Path(str(binary) + ".dll")).encode() in arguments:
                candidates.append(pid)
            pending.extend(int(child) for child in process.joinpath("task", str(pid), "children").read_text().split())
        if len(candidates) != 1:
            raise RuntimeError("A native scenario server is missing or ambiguous.")
        pid = candidates[0]
        started = process_start(pid)
        environment = dict(value.split(b"=", 1) for value in Path("/proc", str(pid), "environ").read_bytes().split(b"\0") if b"=" in value)
        expected = {"NAMESPACE": owned["_isolation"], "XDG_DATA_HOME": str(Path(owned["_configuration"]).parent / ("data-" + app)),
                    "ASPNETCORE_ENVIRONMENT": "Development", "EventStore__DomainService__AppId": app, **RUNTIME_FLAGS,
                    "EventStore__RouteCatalog__ReuseHeldMetadataDispatch": "true" if app == "folders" else "false"}
        if any(environment.get(key.encode()) != value.encode() for key, value in expected.items()):
            raise RuntimeError("A native scenario peer has changed namespace, source ownership or private options.")
        if started is None or process_start(wrapper) != before or process_start(pid) != started:
            raise RuntimeError("A native scenario peer changed during option observation.")
        result[app] = {"wrapperPid": wrapper, "wrapperStartTicks": before, "pid": pid, "startTicks": started,
                       "environment": "Development", "options": {key: expected[key] for key in (*RUNTIME_FLAGS, "EventStore__RouteCatalog__ReuseHeldMetadataDispatch")},
                       "assemblySha256": hashlib.sha256(Path(str(binary) + ".dll").read_bytes()).hexdigest()}
    return result


def require_runtime_modes(observations: dict) -> dict:
    if not isinstance(observations, dict) or set(observations) != READ_APPS:
        raise RuntimeError("The complete actual native runtime option census is missing.")
    modes = {}
    for app, value in observations.items():
        options = {**RUNTIME_FLAGS, "EventStore__RouteCatalog__ReuseHeldMetadataDispatch": "true" if app == "folders" else "false"}
        if (not isinstance(value, dict) or value.get("environment") != "Development" or value.get("options") != options
                or any(type(value.get(name)) is not int or value[name] <= 0 for name in ("pid", "wrapperPid"))
                or any(not isinstance(value.get(name), str) or not value[name].isdigit() for name in ("startTicks", "wrapperStartTicks"))
                or not re.fullmatch(r"[0-9a-f]{64}", value.get("assemblySha256", ""))):
            raise RuntimeError("An actual native option/process observation is incomplete or changed.")
        modes[app] = {"environment": value["environment"], "options": value["options"]}
    return modes


def require_mapping_environment(values: dict) -> None:
    keys = {"CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps", "CustomBeforeDirectoryBuildTargets",
            "CustomAfterDirectoryBuildTargets", "AfterMicrosoftNETSdkTargets", "HEXALITH_SOURCE_MAPPING_HASH", "Configuration"}
    if (not isinstance(values, dict) or set(values) != keys or values.get("Configuration") != "Debug"
            or any(not isinstance(value, str) or not value or any(ord(c) < 32 for c in value) for value in values.values())
            or not re.fullmatch(r"[0-9a-f]{64}", values.get("HEXALITH_SOURCE_MAPPING_HASH", ""))
            or any(not Path(values[key]).is_absolute() for key in keys - {"HEXALITH_SOURCE_MAPPING_HASH", "Configuration"})):
        raise RuntimeError("A scenario did not retain the exact seven emitted Debug mapping values.")


def require_discovery_receipt(value: dict) -> dict:
    """Validate the actual complete six-peer SQLite/socket/mTLS census, rather than a summary flag."""
    if (not isinstance(value, dict) or value.get("resolver") != "sqlite" or value.get("version") != "v1"
            or value.get("alphaTestInfrastructureOnly") is not True
            or type(value.get("observationsRequired")) is not int or not 1 <= value["observationsRequired"] <= 30
            or not isinstance(value.get("configurationSha256"), dict) or set(value["configurationSha256"]) != READ_APPS
            or any(not isinstance(x, str) or not re.fullmatch(r"[0-9a-f]{64}", x) for x in value["configurationSha256"].values())
            or not isinstance(value.get("daprdSha256"), list) or not value["daprdSha256"]
            or len(value["daprdSha256"]) != len(set(value["daprdSha256"]))
            or any(not isinstance(x, str) or not re.fullmatch(r"[0-9a-f]{64}", x) for x in value["daprdSha256"])
            or not isinstance(value.get("registryIdentitySha256"), str) or not re.fullmatch(r"[0-9a-f]{64}", value["registryIdentitySha256"])):
        raise RuntimeError("The actual SQLite/native-binary/configuration census is incomplete or changed.")
    rows = value.get("ownedSidecars")
    if (not isinstance(rows, list) or len(rows) != 6 or any(not isinstance(row, dict) for row in rows)
            or {row.get("appId") for row in rows} != READ_APPS
            or any(type(row.get("pid")) is not int or row["pid"] <= 0 or not isinstance(row.get("startTicks"), str) or not row["startTicks"].isdigit()
                   or any(row.get(key) is not True for key in ("namespaceVerified", "registryAddressVerified", "mtlsIdentityVerified")) for row in rows)
            or len({row["pid"] for row in rows}) != 6):
        raise RuntimeError("The exact six unique owned native sidecar identities are not proved.")
    return {row["appId"]: (row["pid"], row["startTicks"]) for row in rows}


def require_same_discovery_binding(before: dict, after: dict) -> None:
    if any(before[key] != after[key] for key in ("configurationSha256", "registryIdentitySha256", "daprdSha256")):
        raise RuntimeError("The scenario's private registry/configuration or pinned native binary changed.")


def require_projects_restart_receipt(value: dict, session: dict, initial_mode: dict) -> None:
    if (not isinstance(value, dict) or value.get("resource") != "projects" or value.get("action") != "restart"
            or value.get("completed") is not True or value.get("stage") != "completed" or value.get("oldProcessExited") is not True
            or value.get("endpointsRediscovered") is not True
            or any(value.get(key) != session.get(key) for key in ("appHostPid", "appHostStartTicks", "configurationSha256"))):
        raise RuntimeError("The main scenario lacks its exact completed owned Projects restart receipt.")
    before, after = value.get("before"), value.get("after")
    for item in (before, after):
        if (not isinstance(item, dict) or not isinstance(item.get("resourceName"), str) or not item["resourceName"]
                or any(type(item.get(key)) is not int or item[key] <= 0 for key in ("pid", "launcherPid"))
                or any(not isinstance(item.get(key), str) or not item[key].isdigit() for key in ("startTicks", "launcherStartTicks"))
                or any(not re.fullmatch(r"[0-9a-f]{64}", item.get(key, "")) for key in ("binarySha256", "assemblySha256"))):
            raise RuntimeError("The actual Projects restart process/binary observations are incomplete.")
    if (any(before[key] != after[key] for key in ("resourceName", "binarySha256", "assemblySha256"))
            or any((before[pid], before[start]) == (after[pid], after[start]) for pid, start in (("pid", "startTicks"), ("launcherPid", "launcherStartTicks")))
            or any(before[key] != initial_mode.get(other) for key, other in (("pid", "pid"), ("startTicks", "startTicks"), ("launcherPid", "wrapperPid"), ("launcherStartTicks", "wrapperStartTicks"), ("assemblySha256", "assemblySha256")))):
        raise RuntimeError("Projects restart reused/replaced a process, source binary or original native identity.")


def require_restart_state_evidence(value: dict) -> None:
    """Reuse the complete fingerprint/watermark invariant over the actual restart observations."""
    if (not isinstance(value, dict) or value.get("bothTenantsCompared") is not True or value.get("domainKeysBytesEtagsAndWatermarksUnchanged") is not True
            or type(value.get("domainKeyCount")) is not int or value["domainKeyCount"] <= 6 or value.get("afterObservation") != "observed"
            or value.get("failureType") is not None or value.get("missingKeys") != []):
        raise RuntimeError("The actual restart state observation is incomplete or changed.")
    before = value.get("before")
    if not isinstance(before, list) or len(before) != value["domainKeyCount"]:
        raise RuntimeError("Restart state keys are incomplete.")
    watermarks = value.get("beforeWatermarks")
    if not isinstance(watermarks, list):
        raise RuntimeError("Restart watermarks are missing.")
    expected = {row.get("keySha256"): (row.get("tenantSha256"), row.get("globalPosition")) for row in watermarks if isinstance(row, dict)}
    comparison = {"valid": True, "bothTenantsCompared": True, "domainKeysAndBytesUnchanged": True, "exactWatermarksUnchanged": True,
                  "onlyExpectedETagsChanged": True, "expectedChangedETagKeys": [], "changedETagKeys": [],
                  **{key: value.get(key) for key in ("before", "after", "beforeWatermarks", "afterWatermarks")}}
    require_replay_comparison(comparison, set(), {row.get("keySha256") for row in before if isinstance(row, dict)}, expected)


def require_owned_group_report(report: dict, scenario: str) -> str:
    """Require completed exact owned teardown and real runtime observations for one fresh provider."""
    if (not isinstance(report, dict) or report.get("result") != "passed" or report.get("qualificationGroup") != scenario or report.get("matrixCompleted") is not True
            or any(report.get(key) is not True for key in ("ownedSidecarsAbsent", "preexistingResourcesPreserved", "privateMaterialRemoved"))
            or any(report.get(key) for key in ("recoveryMaterialRetained", "infrastructureOwnershipChanged", "infrastructureOwnershipUnproven", "partialSessionOwnershipUnproven"))
            or report.get("ownedSidecarsObservedBeforeStop") != 6):
        raise RuntimeError("A scenario did not finish every exact owned cleanup and external-preservation gate.")
    cleanup = report.get("cleanup")
    if (not isinstance(cleanup, list) or not cleanup or any(row.get("absenceVerified") is not True or row.get("removed") is not True for row in cleanup)
            or len([row for row in cleanup if row.get("resource") == "exact-apphost"]) != 1):
        raise RuntimeError("A scenario lacks its exact owned cleanup receipts.")
    containers = [row for row in cleanup if "containerId" in row]
    if len(containers) != 4 or len({row["containerId"] for row in containers}) != 4 or any(not re.fullmatch(r"[0-9a-f]{64}", row["containerId"]) for row in containers):
        raise RuntimeError("A scenario lacks four immutable owned container absence receipts.")
    infrastructure = report.get("mtlsInfrastructure")
    by_resource = {row.get("resource"): row["containerId"] for row in containers}
    if (not isinstance(infrastructure, list) or len(infrastructure) != 3 or {row.get("name") for row in infrastructure} != set(MTLS_RESOURCES)
            or len(by_resource) != 4 or len({row.get("id") for row in infrastructure}) != 3
            or {row.get("id") for row in infrastructure} != {row["containerId"] for row in containers if not row.get("resource", "").endswith("-redishost")}
            or any(row.get("canonical") is not True or row.get("id") != by_resource.get(row.get("name"))
                   or not re.fullmatch(r"sha256:[0-9a-f]{64}", row.get("imageId", "")) for row in infrastructure)):
        raise RuntimeError("A scenario lacks unique canonical mTLS names joined to their exact owned cleanup identities.")
    providers = [row for row in containers if row.get("resource", "").endswith("-redishost")]
    if len(providers) != 1 or not re.fullmatch(r"reads-[0-9a-f]{24}-redishost", providers[0]["resource"]):
        raise RuntimeError("The actual scenario provider identity is missing or ambiguous.")
    namespace = providers[0]["resource"].removesuffix("-redishost")
    if report.get("providerIsolationSha256") != hashlib.sha256(namespace.encode()).hexdigest().upper():
        raise RuntimeError("The scenario namespace hash does not bind its actual owned provider.")
    session = report.get("ownedAppHostSession")
    if (not isinstance(session, dict) or any(type(session.get(key)) is not int or session[key] <= 0 for key in ("appHostPid", "cliPid"))
            or any(not isinstance(session.get(key), str) or not session[key].isdigit() for key in ("appHostStartTicks", "cliStartTicks"))
            or any(not re.fullmatch(r"[0-9a-f]{64}", session.get(key, "")) for key in ("configurationSha256", "configurationPathSha256"))):
        raise RuntimeError("A scenario lacks exact actual AppHost process/configuration bindings.")
    before_discovery, after_discovery = report.get("discovery"), report.get("discoveryAfterReads")
    before_peers = require_discovery_receipt(before_discovery)
    after_peers = require_discovery_receipt(after_discovery)
    require_same_discovery_binding(before_discovery, after_discovery)
    changed_peers = {app for app in READ_APPS if before_peers[app] != after_peers[app]}
    if changed_peers:
        restart = report.get("projectsRestart", {})
        if (scenario != "main" or changed_peers != {"projects"} or restart.get("completed") is not True
                or restart.get("resource") != "projects" or restart.get("action") != "restart"
                or restart.get("appHostPid") != session["appHostPid"] or restart.get("appHostStartTicks") != session["appHostStartTicks"]
                or restart.get("configurationSha256") != session["configurationSha256"]):
            raise RuntimeError("A native sidecar identity changed without its exact owned restart receipt.")
        restarted = restart.get("discovery")
        restart_peers = require_discovery_receipt(restarted)
        require_same_discovery_binding(before_discovery, restarted)
        if restart_peers != after_peers:
            raise RuntimeError("Final native identities do not match the actual owned restart census.")
    if scenario == "main":
        restart = report.get("projectsRestart")
        require_projects_restart_receipt(restart, session, report.get("runtimeModes", {}).get("projects", {}))
        restart_discovery = restart.get("discovery")
        restart_peers = require_discovery_receipt(restart_discovery)
        require_same_discovery_binding(before_discovery, restart_discovery)
        if restart_peers != after_peers:
            raise RuntimeError("Final sidecar identities differ from the actual restart/current namespace census.")
    require_mapping_environment(report.get("sourceMappingEnvironment"))
    if report.get("sourceMappingHash") != report["sourceMappingEnvironment"]["HEXALITH_SOURCE_MAPPING_HASH"]:
        raise RuntimeError("The scenario mapping binding changed.")
    require_compilation_profile(report.get("compilationProfile"))
    require_runtime_modes(report.get("runtimeModes"))
    source_dependency_graph(report.get("liveHostSourceReferences"))
    return providers[0]["containerId"]


def read_qualified_scenario(directory: Path, scenario: str, frozen: dict, regressions: bool) -> dict:
    """Validate native files and receipts from their one exact new child destination, never a historical failed row."""
    if scenario not in SCENARIO_GROUPS or directory.name != scenario or directory.is_symlink() or not directory.is_dir():
        raise RuntimeError("A scenario destination is unknown or substituted.")
    required = ["run.json", "source-manifest.json", "matrix.json", "distributed.xml", "provider-observer-control.json"]
    if scenario != "main":
        required.append("projection-replay.json")
    else:
        required.extend(("projects-restart-process.json", "projects-restart-state.json"))
    if regressions:
        required.extend(("integration.xml", "server.xml"))
    for name in required:
        path = directory / name
        entry = path.lstat()
        if not stat.S_ISREG(entry.st_mode) or entry.st_uid != os.getuid() or entry.st_nlink != 1 or entry.st_mode & 0o022 or path.is_symlink():
            raise RuntimeError("A scenario evidence file is missing, replaced or writable by another owner.")
    report = json.loads((directory / "run.json").read_text())
    provider = require_owned_group_report(report, scenario)
    manifest = json.loads((directory / "source-manifest.json").read_text())
    digest = hashlib.sha256(json.dumps(manifest, sort_keys=True).encode()).hexdigest()
    if manifest != frozen or report.get("sourceManifestSha256") != digest:
        raise RuntimeError("Source inputs changed between scenario groups.")
    assemblies = ET.parse(directory / "distributed.xml").getroot().findall("assembly")
    command = report.get("nativeTestCommand")
    assembly_path = report.get("nativeTestAssemblyPath")
    expected_command = ["dotnet", assembly_path, "-method", SCENARIO_TEST_TYPE + "." + SCENARIO_METHODS[scenario], "-xml", str(directory / "distributed.xml")]
    if (command != expected_command or report.get("commands", []).count(command) != 1 or report.get("testAssemblyUnchanged") is not True):
        raise RuntimeError("The native assembly is not its exact recorded selected command and unchanged binary receipt.")
    native = require_native_scenario(assemblies, scenario, report["providerIsolationSha256"], assembly_path, report.get("testAssemblySha256"))
    if report.get("tests") != native:
        raise RuntimeError("The scenario report disagrees with its actual native XML.")
    matrix = json.loads((directory / "matrix.json").read_text())
    require_scenario_matrix(matrix, scenario, report["providerIsolationSha256"])
    require_provider_observer_control(json.loads((directory / "provider-observer-control.json").read_text()))
    if scenario != "main":
        replay = json.loads((directory / "projection-replay.json").read_text())
        derived = require_distributed_replay_evidence(matrix, replay, scenario)
        if report.get("projectionReplay") != derived:
            raise RuntimeError("The scenario replay summary differs from its exact actual ledger.")
    else:
        if json.loads((directory / "projects-restart-process.json").read_text()) != report.get("projectsRestart"):
            raise RuntimeError("The actual restart file differs from its child report.")
        require_restart_state_evidence(json.loads((directory / "projects-restart-state.json").read_text()))
    if regressions:
        if set(report.get("regressions", {})) != {"Integration", "Server"}:
            raise RuntimeError("The complete main Integration/Server regressions are missing.")
        for name in ("Integration", "Server"):
            results = require_complete_native_results(ET.parse(directory / (name.lower() + ".xml")).getroot().findall("assembly"))
            if report["regressions"][name] != results:
                raise RuntimeError("A full regression summary differs from its actual native XML.")
    return {"group": scenario, "directory": scenario, "cases": matrix["cases"], "providerId": provider,
            "sessionIdentity": [report["ownedAppHostSession"]["appHostPid"], report["ownedAppHostSession"]["appHostStartTicks"]],
            "containerIds": sorted(row["containerId"] for row in report["cleanup"] if "containerId" in row),
            "sourceManifestSha256": digest, "revisions": report.get("revisions"),
            "sourceDependencyGraph": source_dependency_graph(report["liveHostSourceReferences"]),
            "compilationProfile": report["compilationProfile"], "runtimeModes": require_runtime_modes(report["runtimeModes"]),
            "nativeMethod": SCENARIO_METHODS[scenario], "native": native,
            "files": {name: hashlib.sha256((directory / name).read_bytes()).hexdigest() for name in required},
            "fullRegressionsCompleted": regressions}


def require_aggregate_scenarios(groups: list[dict], regressions: bool) -> dict:
    """Aggregate exact independent proofs without composing any provider snapshot or cursor."""
    if not isinstance(groups, list) or [group.get("group") for group in groups] != list(SCENARIO_GROUPS):
        raise RuntimeError("All three unique scenario groups must pass in their exact finite order.")
    first = groups[0]
    required_fields = {"group", "directory", "cases", "providerId", "sessionIdentity", "containerIds", "sourceManifestSha256", "revisions",
                       "sourceDependencyGraph", "compilationProfile", "runtimeModes", "nativeMethod", "native", "files", "fullRegressionsCompleted"}
    for group in groups:
        files = {"run.json", "source-manifest.json", "matrix.json", "distributed.xml", "provider-observer-control.json"}
        if group["group"] != "main":
            files.add("projection-replay.json")
        else:
            files.update(("projects-restart-process.json", "projects-restart-state.json"))
            if regressions:
                files.update(("integration.xml", "server.xml"))
        require_compilation_profile(group.get("compilationProfile"))
        if (set(group) != required_fields or not re.fullmatch(r"[0-9a-f]{64}", group.get("providerId", ""))
                or not re.fullmatch(r"[0-9a-f]{64}", group.get("sourceManifestSha256", ""))
                or not isinstance(group.get("revisions"), dict) or not {".", "references/Hexalith.EventStore"}.issubset(group["revisions"])
                or any(not re.fullmatch(r"[0-9a-f]{40}", value) for value in group["revisions"].values())
                or group.get("native") != [{"total": "1", "passed": "1", "failed": "0", "skipped": "0"}]
                or not isinstance(group.get("files"), dict) or set(group["files"]) != files
                or any(not re.fullmatch(r"[0-9a-f]{64}", value) for value in group["files"].values())
                or not isinstance(group.get("containerIds"), list) or len(group["containerIds"]) != 4
                or group["providerId"] not in group["containerIds"] or any(not re.fullmatch(r"[0-9a-f]{64}", value) for value in group["containerIds"])):
            raise RuntimeError("A native scenario receipt is incomplete or replaced with summaries.")
        if (group.get("directory") != group["group"] or group.get("cases") != list(SCENARIO_CASES[group["group"]])
                or group.get("nativeMethod") != SCENARIO_METHODS[group["group"]]
                or any(group.get(key) != first.get(key) for key in ("sourceManifestSha256", "revisions", "sourceDependencyGraph", "compilationProfile", "runtimeModes"))
                or not group.get("native") or not group.get("files")):
            raise RuntimeError("A scenario proof, source graph or actual compilation/private mode changed.")
    if (len({group["providerId"] for group in groups}) != 3 or len({tuple(group["sessionIdentity"]) for group in groups}) != 3
            or len({identifier for group in groups for identifier in group["containerIds"]}) != 12):
        raise RuntimeError("Scenario fixtures reused a provider or AppHost process identity.")
    cases = [case for group in groups for case in group["cases"]]
    if cases != list(MATRIX_CASES) or groups[-1].get("fullRegressionsCompleted") is not regressions or any(group.get("fullRegressionsCompleted") for group in groups[:-1]):
        raise RuntimeError("The eleven cases or complete main regressions are missing or substituted.")
    return {"version": 2, "qualification": "three-independent-fresh-owned-scenarios", "allCasesExecuted": True, "cases": cases,
            "providerScope": "independent-per-group-no-composed-state", "groups": groups, "fullRegressionsCompleted": regressions,
            "acceptedEquivalence": False, "productionReadiness": False}


def capture_launcher_identity(child, command: list[str]) -> dict:
    """Capture this exact spawned launcher after exec; never adopt another process or an argv-only identity."""
    deadline = time.monotonic() + 1
    while time.monotonic() < deadline:
        if child.poll() is not None:
            raise RuntimeError("The scenario launcher exited before its active identity could be verified.")
        started = process_start(child.pid)
        try:
            process = Path("/proc", str(child.pid))
            arguments = [item.decode() for item in process.joinpath("cmdline").read_bytes().split(b"\0") if item]
            if started is not None and process.stat().st_uid == os.getuid() and arguments == command and process_start(child.pid) == started:
                return {"pid": child.pid, "startTicks": started, "command": command}
        except FileNotFoundError:
            pass
        time.sleep(.01)
    raise RuntimeError("The active scenario launcher identity is unknown; no other process is adopted.")


def owned_launcher_identity_matches(child, identity: dict) -> bool:
    if child.poll() is not None:
        return False
    try:
        arguments = [item.decode() for item in Path("/proc", str(child.pid), "cmdline").read_bytes().split(b"\0") if item]
        return (identity.get("pid") == child.pid and process_start(child.pid) == identity.get("startTicks")
                and arguments == identity.get("command"))
    except FileNotFoundError:
        return False


def forward_owned_launcher_signal(child, identity: dict, signum: int) -> None:
    """Signal only the still-exact active child and leave all of its owned teardown to that launcher."""
    if child.poll() is not None:
        return
    if not owned_launcher_identity_matches(child, identity):
        raise RuntimeError("The active child identity changed; interruption is not forwarded to an unknown process.")
    child.send_signal(signum)


def owned_launcher_exit_started(child, identity: dict) -> bool:
    """Observe only the captured child's kernel exit transition; this never authorizes a signal."""
    if identity.get("pid") != child.pid or identity.get("command") != child.args:
        return False
    try:
        process = Path("/proc", str(child.pid))
        before = process.joinpath("stat").read_text().rsplit(")", 1)[1].split()
        arguments = process.joinpath("cmdline").read_bytes()
        after = process.joinpath("stat").read_text().rsplit(")", 1)[1].split()
        # Linux PF_EXITING is set before exit_mm clears cmdline, before waitpid can
        # reap the child. Empty argv alone never establishes this transition.
        return (process.stat().st_uid == os.getuid() and not arguments
                and before[19] == after[19] == identity.get("startTicks")
                and int(before[6]) & 0x00000004 != 0 and int(after[6]) & 0x00000004 != 0)
    except (OSError, ValueError, IndexError):
        return False


def wait_owned_launcher(child, identity: dict, interrupted: dict, *, cleanup_wait_seconds: float = 900) -> int:
    deadline = None
    while child.poll() is None:
        exiting = False
        if not owned_launcher_identity_matches(child, identity):
            if child.poll() is not None:
                break
            exiting = owned_launcher_exit_started(child, identity)
            if not exiting:
                if child.poll() is not None:
                    break
                raise RuntimeError("Active child completion/cleanup is unknown; preserved material must be recovered by exact ownership.")
        if interrupted.get("requested") or exiting:
            deadline = deadline or time.monotonic() + cleanup_wait_seconds
            if time.monotonic() >= deadline:
                raise RuntimeError("Interrupted or exiting child cleanup completion is unproved; no later group may start.")
        time.sleep(.05)
    return child.wait()


def observe_child_cleanup(directory: Path, launch: dict) -> None:
    """Retain actual failed/partial child cleanup facts without converting process exit into absence proof."""
    try:
        path = directory / "run.json"
        if path.is_symlink():
            raise RuntimeError("Replaced child report")
        raw = path.read_bytes()
        report = json.loads(raw)
        launch["childReportSha256"] = hashlib.sha256(raw).hexdigest()
        launch["childCleanup"] = {key: report.get(key) for key in ("ownedSidecarsAbsent", "preexistingResourcesPreserved", "privateMaterialRemoved", "recoveryMaterialRetained")}
        launch["cleanupCompletionVerified"] = (report.get("privateMaterialRemoved") is True and not report.get("recoveryMaterialRetained")
            and report.get("ownedSidecarsAbsent") is True and all(row.get("absenceVerified") is True for row in report.get("cleanup", [])))
    except (OSError, ValueError, TypeError, RuntimeError, AttributeError):
        launch["cleanupCompletionVerified"] = False
        launch["childReportUnproved"] = True


def coordinate_scenarios(args, evidence: Path) -> int:
    """Run finite fresh groups sequentially; a failed/unknown child or cleanup blocks every later start."""
    report = {"qualification": "independent-scenario-coordinator", "result": "failed", "matrixCompleted": False,
              "acceptedEquivalence": False, "productionReadiness": False, "groups": [], "launches": []}
    active, identity = None, None
    interrupted = {"requested": False}
    previous = {signum: signal.getsignal(signum) for signum in (signal.SIGINT, signal.SIGTERM)}
    def handle(signum, _frame):
        if interrupted["requested"]:
            return
        interrupted.update(requested=True, signal=signal.Signals(signum).name)
        if active is not None and identity is not None:
            try:
                forward_owned_launcher_signal(active, identity, signum)
                interrupted["forwardedToExactChild"] = True
            except (RuntimeError, OSError):
                interrupted["forwardedToExactChild"] = False
    try:
        for signum in previous:
            signal.signal(signum, handle)
        report["launcherControls"] = run_launcher_controls(evidence)
        frozen = source_manifest()
        (evidence / "source-manifest.json").write_text(json.dumps(frozen, sort_keys=True, indent=2) + "\n")
        report["sourceManifestSha256"] = hashlib.sha256(json.dumps(frozen, sort_keys=True).encode()).hexdigest()
        for scenario in SCENARIO_GROUPS:
            if interrupted["requested"] or source_manifest() != frozen:
                raise RuntimeError("Qualification was interrupted or sources changed before a later group.")
            command = [sys.executable, str(Path(__file__).resolve()), "--scenario", scenario, "--timeout", str(args.timeout), "--evidence", str(evidence / scenario)]
            if args.artifacts:
                command.extend(("--artifacts", str(args.artifacts.resolve() / scenario)))
            if args.regressions and scenario == "main":
                command.append("--regressions")
            logfile = evidence / (scenario + ".launcher.log")
            descriptor = os.open(logfile, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
            with os.fdopen(descriptor, "w") as output:
                active = subprocess.Popen(command, cwd=ROOT, env=os.environ.copy(), stdout=output, stderr=subprocess.STDOUT, start_new_session=True)
                identity = capture_launcher_identity(active, command)
                if interrupted["requested"] and not interrupted.get("forwardedToExactChild"):
                    forward_owned_launcher_signal(active, identity, signal.Signals[interrupted["signal"]])
                    interrupted["forwardedToExactChild"] = True
                launch = {"group": scenario, "identity": identity, "evidenceDirectory": scenario, "complete": False}
                report["launches"].append(launch)
                (evidence / "active-group.json").write_text(json.dumps({"active": True, **launch}, indent=2) + "\n")
                code = wait_owned_launcher(active, identity, interrupted, cleanup_wait_seconds=args.timeout)
                launch.update(complete=True, exitCode=code)
                observe_child_cleanup(evidence / scenario, launch)
            if interrupted["requested"] or code != 0:
                raise RuntimeError("An interrupted/failed scenario cannot qualify and no later group was started.")
            group = read_qualified_scenario(evidence / scenario, scenario, frozen, args.regressions and scenario == "main")
            report["groups"].append(group)
            active, identity = None, None
        if interrupted["requested"] or source_manifest() != frozen:
            raise RuntimeError("Source inputs changed or qualification was interrupted after scenario verification.")
        verified = [read_qualified_scenario(evidence / group, group, frozen, args.regressions and group == "main") for group in SCENARIO_GROUPS]
        if verified != report["groups"]:
            raise RuntimeError("Scenario evidence changed after its child completed.")
        aggregate = require_aggregate_scenarios(verified, args.regressions)
        (evidence / "matrix.json").write_text(json.dumps(aggregate, indent=2) + "\n")
        report.update(result="passed", matrixCompleted=True, fullRegressionsCompleted=args.regressions)
    except BaseException as failure:
        report["failure"] = str(failure) if isinstance(failure, RuntimeError) else type(failure).__name__
        if active is not None and active.poll() is None:
            try:
                if identity is None:
                    raise RuntimeError("No verified active child identity exists.")
                forward_owned_launcher_signal(active, identity, signal.SIGTERM)
                cleanup_interruption = {"requested": True}
                code = wait_owned_launcher(active, identity, cleanup_interruption, cleanup_wait_seconds=args.timeout)
                if report["launches"]:
                    launch = report["launches"][-1]
                    launch.update(complete=True, exitCode=code, exactInterruptionAfterCoordinatorFailure=True)
                    observe_child_cleanup(evidence / launch["group"], launch)
            except (RuntimeError, OSError, subprocess.SubprocessError):
                report["activeChildCompletionUnproved"] = True
                report["recoveryMaterialRetainedByChild"] = True
    finally:
        report["interruption"] = interrupted
        for signum, handler in previous.items():
            signal.signal(signum, handler)
        still_running = active is not None and active.poll() is None
        (evidence / "active-group.json").write_text(json.dumps({"active": still_running, "identity": identity if still_running else None,
            "result": report["result"], "completionUnproved": report.get("activeChildCompletionUnproved", False)}, indent=2) + "\n")
        (evidence / "run.json").write_text(json.dumps(report, indent=2) + "\n")
    print("Supported Project reads coordinator: " + report["result"] + "; evidence: " + str(evidence))
    return 0 if report["result"] == "passed" else 1


def issuer_server(directory: Path, issuer: str, bootstrap_password: str) -> tuple[http.server.HTTPServer, threading.Thread]:
    modulus = execute(["openssl", "rsa", "-in", str(directory / "user.pem"), "-modulus", "-noout"])
    number = int(modulus.strip().split("=", 1)[1], 16)
    public = {"keys": [{"kty": "RSA", "use": "sig", "alg": "RS256", "kid": "synthetic-user",
                        "n": b64url(number.to_bytes((number.bit_length() + 7) // 8, "big")), "e": "AQAB"}]}

    class MetadataHandler(http.server.BaseHTTPRequestHandler):
        timeout = 5

        def respond(self, status, payload):
            body = json.dumps(payload).encode()
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            self.wfile.write(body)

        def do_GET(self) -> None:
            payload = ({"issuer": issuer, "jwks_uri": issuer + "/jwks", "token_endpoint": issuer + "/token",
                        "id_token_signing_alg_values_supported": ["RS256"]}
                       if self.path == "/users/.well-known/openid-configuration" else public if self.path == "/users/jwks" else None)
            self.respond(200 if payload else 404, payload)

        def do_POST(self) -> None:
            if self.path != "/users/token":
                self.respond(404, {"error": "not_found"})
                return
            try:
                length = int(self.headers.get("Content-Length", "0"))
                if (not 0 < length <= 4096 or self.headers.get("Content-Type", "").split(";")[0] != "application/x-www-form-urlencoded"
                        or self.headers.get("Transfer-Encoding") is not None):
                    raise ValueError()
                form = urllib.parse.parse_qs(self.rfile.read(length).decode(), strict_parsing=True, max_num_fields=4)
                valid = (set(form) == {"grant_type", "client_id", "username", "password"}
                         and form["grant_type"] == ["password"] and form["client_id"] == ["supported-read-bootstrap"]
                         and form["username"] == ["actor-1"] and len(form["password"]) == 1
                         and secrets.compare_digest(form["password"][0], bootstrap_password))
            except (ValueError, UnicodeDecodeError, TypeError):
                valid = False
            if not valid:
                self.respond(400, {"error": "invalid_grant"})
                return
            now = int(time.time())
            header = b64url(json.dumps({"alg": "RS256", "typ": "JWT", "kid": "synthetic-user"}).encode())
            payload = b64url(json.dumps({"iss": issuer, "aud": "supported-read-users", "sub": "actor-1",
                "iat": now, "nbf": now - 5, "exp": now + 120, "jti": secrets.token_hex(16),
                "tenant_id": "system", "eventstore:tenant": "system", "global_admin": "true", "azp": "supported-read-bootstrap",
                "eventstore:surface": "ui",
                "client_id": "supported-read-bootstrap", "eventstore:permission": ["command:submit", "replay:read"]}).encode())
            unsigned = (header + "." + payload).encode()
            try:
                signed = subprocess.run(["openssl", "dgst", "-sha256", "-sign", str(directory / "user.pem")],
                                        input=unsigned, capture_output=True, timeout=5, check=True).stdout
            except (subprocess.SubprocessError, OSError):
                self.respond(503, {"error": "temporarily_unavailable"})
                return
            self.respond(200, {"access_token": unsigned.decode() + "." + b64url(signed), "token_type": "Bearer", "expires_in": 120})

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
    parser.add_argument("--scenario", choices=SCENARIO_GROUPS, help=argparse.SUPPRESS)
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
    evidence = (args.evidence or Path(tempfile.gettempdir()) / ("supported-project-reads-evidence-" + secrets.token_hex(8))).resolve()
    evidence.mkdir(mode=0o700, parents=True, exist_ok=False)
    if args.scenario is None:
        return coordinate_scenarios(args, evidence)
    private = None
    isolation = "reads-" + secrets.token_hex(12)
    owned: list[dict] = []
    owned_apphost = None
    start_attempted = False
    config_path = None
    server = None
    infrastructure = None
    preexisting = None
    report: dict = {"qualification": "local-distributed-proof", "acceptedEquivalence": False,
                    "productionReadiness": False, "commands": [], "cleanup": [], "matrixCompleted": False,
                    "qualificationGroup": args.scenario,
                    "providerIsolationSha256": hashlib.sha256(isolation.encode()).hexdigest().upper()}
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
    env["EnablePrivateCatalogInvocationProof"] = "true"
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
        # The AppHost compiles source-referenced sibling projects. Bind the complete Projects
        # graph to the Platform-selected mapping before Aspire builds any of those sources.
        mapping_keys = {"CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps",
                        "CustomBeforeDirectoryBuildTargets", "CustomAfterDirectoryBuildTargets",
                        "AfterMicrosoftNETSdkTargets", "HEXALITH_SOURCE_MAPPING_HASH", "Configuration"}
        for key in mapping_keys:
            env.pop(key, None)
        env["HexalithBuildsRoot"] = str(ROOT / "references/Hexalith.Builds")
        mapping = json.loads(run(["dotnet", "run", "--project",
            str(ROOT / "references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj"),
            "--", "prepare-build", "--manifest", "module/hexalith.module-manifest.v2.json",
            "--mode", "source", "--workspace", str(private / "source-mapping"), "--output", "json"]))
        selected = mapping.get("buildPreparation", {}).get("environment")
        if (mapping.get("status") != "prepared" or not isinstance(selected, dict) or set(selected) != mapping_keys
                or selected.get("Configuration") != "Debug"
                or any(not isinstance(value, str) or any(ord(character) < 32 for character in value) for value in selected.values())):
            raise RuntimeError("The Platform source mapping did not prepare the exact Debug build graph.")
        env.update(selected)
        report["sourceMappingHash"] = selected["HEXALITH_SOURCE_MAPPING_HASH"]
        report["sourceMappingEnvironment"] = selected.copy()
        report["compilationProfile"] = observe_compilation_profile(run)
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
        execute(["openssl", "genpkey", "-algorithm", "RSA", "-pkeyopt", "rsa_keygen_bits:2048", "-out", str(private / "tenant-owner.pem")])
        execute(["openssl", "pkcs8", "-topk8", "-nocrypt", "-in", str(private / "tenant-owner.pem"), "-outform", "DER", "-out", str(private / "tenant-owner.der")])
        execute(["openssl", "pkey", "-in", str(private / "tenant-owner.pem"), "-pubout", "-outform", "DER", "-out", str(private / "tenant-owner-public.der")])
        execute(["openssl", "genpkey", "-algorithm", "RSA", "-pkeyopt", "rsa_keygen_bits:2048", "-out", str(private / "folder-owner.pem")])
        execute(["openssl", "pkcs8", "-topk8", "-nocrypt", "-in", str(private / "folder-owner.pem"), "-outform", "DER", "-out", str(private / "folder-owner.der")])
        execute(["openssl", "pkey", "-in", str(private / "folder-owner.pem"), "-pubout", "-outform", "DER", "-out", str(private / "folder-owner-public.der")])
        for path in private.iterdir():
            os.chmod(path, 0o700 if path.is_dir() else 0o600)
        for name in ("eventstore", "tenants", "projects", "projects-workers", "folders", "folders-workers"):
            (private / ("data-" + name)).mkdir(mode=0o700)
        user_issuer = f"https://127.0.0.1:{free_port()}/users"
        bootstrap_password = secrets.token_hex(32)
        server, _ = issuer_server(private, user_issuer, bootstrap_password)
        dependencies = {}
        for role, port, image, command in (
            ("RedisHost", 6379, "redis:6", []),
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
        # Initial sidecar syntax only: canonical Platform composition allocates and replaces these.
        dependencies.update({"PlacementHostAddress": f"127.0.0.1:{free_port()}", "SchedulerHostAddress": f"127.0.0.1:{free_port()}"})
        (private / "catalog-invocations").mkdir(mode=0o700)
        fixture = {"IsolationId": isolation, "UserIssuer": user_issuer, "UserKeyPath": str(private / "user.pem"),
                   "CertificatePath": str(private / "certificate.pfx"), "CertificatePassword": password,
                   "TrustBundle": str(private / "certificate.pem"), "EventStoreSecret": secrets.token_hex(32),
                   "ProjectsSecret": secrets.token_hex(32), "ChannelToken": secrets.token_hex(32),
                   "TenantsSecret": secrets.token_hex(32), "FoldersSecret": secrets.token_hex(32), "BootstrapPassword": bootstrap_password,
                   "CatalogDigestKey": base64.b64encode(secrets.token_bytes(32)).decode(),
                   "InvocationDirectory": str(private / "catalog-invocations"),
                   "ReceiptKey": base64.b64encode(secrets.token_bytes(32)).decode(),
                   "OwnerPrivateKey": base64.b64encode((private / "owner.der").read_bytes()).decode(),
                   "OwnerPublicKey": base64.b64encode((private / "owner-public.der").read_bytes()).decode(),
                   "TenantOwnerPrivateKey": base64.b64encode((private / "tenant-owner.der").read_bytes()).decode(),
                   "TenantOwnerPublicKey": base64.b64encode((private / "tenant-owner-public.der").read_bytes()).decode(),
                   "FolderOwnerPrivateKey": base64.b64encode((private / "folder-owner.der").read_bytes()).decode(),
                   "FolderOwnerPublicKey": base64.b64encode((private / "folder-owner-public.der").read_bytes()).decode(),
                   "Tenant": isolation + "-tenant", "ForeignTenant": isolation + "-foreign",
                   "WriterProtocolSourceCommit": revisions["references/Hexalith.EventStore"],
                   "WriterProtocolSourceManifestSha256": hashlib.sha256((evidence / "source-manifest.json").read_bytes()).hexdigest()}
        discovery_configuration = private / "discovery.json"
        discovery_registry = private / "discovery.sqlite"
        discovery_configuration.write_text(json.dumps({"apiVersion": "dapr.io/v1alpha1", "kind": "Configuration",
            "metadata": {"name": isolation}, "spec": {"accessControl": {"defaultAction": "allow", "trustDomain": "public"},
            "nameResolution": {"component": "sqlite", "version": "v1", "configuration": {"connectionString": str(discovery_registry)}}}}))
        os.chmod(discovery_configuration, 0o600)
        fixture["DiscoveryConfiguration"] = str(discovery_configuration)
        fixture["Scenario"] = args.scenario
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
        report["projectsStateResiliencySha256"] = hashlib.sha256((private / "components-projects/projects-resiliency.yaml").read_bytes()).hexdigest()
        for resource in ("supported-read-authority", "tenants", "eventstore", "projects", "projects-workers", "folders", "folders-workers",
                         "tenants-dapr-cli", "eventstore-dapr-cli", "projects-dapr-cli", "projects-workers-dapr-cli", "folders-dapr-cli", "folders-workers-dapr-cli", *MTLS_RESOURCES):
            run(["aspire", "wait", resource, "--apphost", str(APPHOST), "--timeout", str(args.timeout), "--non-interactive"])
        graph = json.loads(run(["aspire", "describe", "--apphost", str(APPHOST), "--format", "Json", "--non-interactive"]))
        infrastructure = capture_infrastructure(private, isolation, graph)
        owned.extend(infrastructure["containers"])
        capture_path = private / "infrastructure-observed.json"
        capture_path.write_text(json.dumps(infrastructure))
        os.chmod(capture_path, 0o600)
        report["mtlsInfrastructure"] = infrastructure["containers"]
        report["discovery"] = verify_discovery(discovery_configuration, discovery_registry, isolation)
        infrastructure = json.loads(capture_path.read_text())
        report["runtimeConfigurationSha256"] = hashlib.sha256(json.dumps(graph, sort_keys=True).encode()).hexdigest()
        report["runtimeModes"] = observe_runtime_modes(graph, owned_apphost)
        report["resourceStates"] = [{"name": r.get("displayName"), "state": r.get("state"), "health": r.get("healthStatus")}
                                    for r in graph["resources"] if not r.get("displayName", "").startswith("supported-reads-")]
        report["endpointNames"] = {r.get("displayName"): [url.get("name") for url in r.get("urls", [])]
                                   for r in graph["resources"] if not r.get("displayName", "").startswith("supported-reads-")}
        endpoints = {name: endpoint(graph, name) for name in ("eventstore", "projects", "folders", "eventstore-dapr-cli", "projects-dapr-cli", "folders-dapr-cli")}
        endpoints["broker"] = endpoint(graph, "supported-read-authority", "https") + "/workload-authority"
        endpoints["state-grpc"] = endpoint(graph, "eventstore-dapr-cli", "grpc")
        endpoints["ownership"] = str(ownership_path)
        endpoints["redis-container-id"] = next(item["id"] for item in owned if item["name"].endswith("-redishost"))
        (private / "endpoints.json").write_text(json.dumps(endpoints))
        env["SUPPORTED_PROJECT_READ_ENDPOINTS"] = str(private / "endpoints.json")
        artifacts = args.artifacts.resolve() if args.artifacts else private / "artifacts"
        if args.artifacts:
            artifacts.mkdir(mode=0o700, parents=True, exist_ok=True)
        report["freshArtifacts"] = args.artifacts is None
        run(["dotnet", "build", str(TEST_PROJECT), "-c", "Debug", "-m:1", "-p:SupportedProjectReadDistributed=true", "--artifacts-path", str(artifacts)])
        candidates = list((artifacts / "bin/Hexalith.Projects.Integration.Tests").glob("debug*/Hexalith.Projects.Integration.Tests.dll"))
        if len(candidates) != 1:
            raise RuntimeError("The fresh Debug test assembly is missing or ambiguous.")
        assembly = candidates[0]
        test_command = ["dotnet", str(assembly), "-method", SCENARIO_TEST_TYPE + "." + SCENARIO_METHODS[args.scenario], "-xml", str(evidence / "distributed.xml")]
        report["commands"].append(test_command)
        report["nativeTestCommand"] = test_command.copy()
        report["nativeTestAssemblyPath"] = str(assembly)
        report["testAssemblySha256"] = hashlib.sha256(assembly.read_bytes()).hexdigest()
        result = run_test_process(test_command, env=env, timeout=args.timeout, cleanup=report.setdefault("testProcessCleanup", []))
        # xUnit success/failure diagnostics use assertions containing statuses and closed reason codes only.
        (evidence / "distributed.log").write_text(result.stdout)
        assemblies = ET.parse(evidence / "distributed.xml").getroot().findall("assembly")
        report["tests"] = require_native_scenario(assemblies, args.scenario, report["providerIsolationSha256"], str(assembly), report["testAssemblySha256"])
        if result.returncode:
            raise RuntimeError("Distributed assertions failed; inspect distributed.xml and the sanitized test log.")
        matrix = json.loads((evidence / "matrix.json").read_text())
        require_scenario_matrix(matrix, args.scenario, report["providerIsolationSha256"])
        observer_control = json.loads((evidence / "provider-observer-control.json").read_text())
        require_provider_observer_control(observer_control)
        report["providerObserverControl"] = {key: observer_control[key] for key in ("verified", "domainKeyCount", "controlKeyCount", "binaryAndEscapingBoundariesExact")}
        if args.scenario == "main":
            report["projectsRestart"] = json.loads((evidence / "projects-restart-process.json").read_text())
            if not report["projectsRestart"].get("completed"):
                raise RuntimeError("The main scenario did not complete its exact owned Projects restart.")
        else:
            replay = json.loads((evidence / "projection-replay.json").read_text())
            report["projectionReplay"] = require_distributed_replay_evidence(matrix, replay, args.scenario)
        report["discoveryAfterReads"] = verify_discovery(discovery_configuration, discovery_registry, isolation)
        if args.regressions and args.scenario != "main":
            raise RuntimeError("Complete regressions run only under the main scenario's owned runtime.")
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
                report["regressions"][lane] = require_complete_native_results(lane_results)
                if regression.returncode or not lane_results or any(int(a.attrib.get("failed", "0")) or int(a.attrib.get("skipped", "0")) for a in lane_results):
                    raise RuntimeError("The complete " + lane + " regression project did not pass without skips.")
        if hashlib.sha256(assembly.read_bytes()).hexdigest() != report["testAssemblySha256"]:
            raise RuntimeError("The selected native test assembly changed during verification.")
        report["testAssemblyUnchanged"] = True
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
                             "tenants-dapr-cli", "eventstore-dapr-cli", "projects-dapr-cli", "projects-workers-dapr-cli",
                             "folders-dapr-cli", "folders-workers-dapr-cli"):
                try:
                    logs = execute(["aspire", "logs", resource, "--apphost", str(APPHOST), "--format", "Json", "--tail", "1000", "--include-hidden", "--non-interactive"], env=env, timeout=15)
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
            if start_attempted and infrastructure is None and owned_apphost is not None:
                try:
                    infrastructure = capture_infrastructure(private, isolation, resource_graph(run))
                    owned.extend(infrastructure["containers"])
                except BaseException:
                    report["infrastructureOwnershipUnproven"] = True
            if infrastructure is not None:
                try:
                    validate_infrastructure_capture(infrastructure, isolation)
                except BaseException:
                    report["infrastructureOwnershipChanged"] = True
                    report["result"] = "failed"
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
            cleanup_private_material(private, isolation, infrastructure, start_attempted, report)
            (evidence / "run.json").write_text(json.dumps(report, indent=2) + "\n")
        finally:
            signal.signal(signal.SIGTERM, previous_sigterm)
    print(f"Supported Project reads: {report['result']}; evidence: {evidence}")
    return 0 if report["result"] == "passed" else 1


if __name__ == "__main__":
    sys.exit(main())
