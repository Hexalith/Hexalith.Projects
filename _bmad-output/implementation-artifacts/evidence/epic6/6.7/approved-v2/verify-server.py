"""Run under run-with-temp; reuse the retained Story 6.6 owned-sidecar verification pattern."""
import json
import os
from pathlib import Path
import socket
import subprocess
import time
import urllib.request

root = Path.cwd()
scratch = Path(os.environ["TMPDIR"])
out = root / "_bmad-output/implementation-artifacts/evidence/epic6/6.7/approved-v2"
commands = []
attempt = os.environ.get("STORY67_ATTEMPT", "")


def run(label, command, environment):
    with (out / (label + attempt + ".log")).open("w") as log:
        result = subprocess.run(command, cwd=root, env=environment, stdout=log, stderr=subprocess.STDOUT)
    commands.append({"purpose": label, "command": command, "exitCode": result.returncode})
    (out / ("server-owned-commands" + attempt + ".json")).write_text(json.dumps(commands, indent=2) + "\n")
    print(label, "exit", result.returncode, flush=True)
    print((out / (label + attempt + ".log")).read_text()[-4000:], flush=True)
    return result.returncode


environment = os.environ.copy()
mapper = root / "references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/bin/Debug/net11.0/Hexalith.Builds.Module.Cli.dll"
prepare = ["dotnet", str(mapper), "prepare-build", "--manifest", "module/hexalith.module-manifest.v2.json", "--mode", "source", "--workspace", str(scratch / "prepared"), "--output", "json"]
if run("server-owned-mapping", prepare, environment):
    raise SystemExit(1)
mapping = json.loads((out / ("server-owned-mapping" + attempt + ".log")).read_text())
environment.update(mapping["buildPreparation"]["environment"])
build = ["dotnet", "build", "tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj", "--configuration", "Debug", "-m:1", "--disable-build-servers", "--artifacts-path", str(scratch / "artifacts")]
if run("Server-owned-build", build, environment):
    raise SystemExit(1)
for entry in root.iterdir():
    target = scratch / entry.name
    if not target.exists():
        target.symlink_to(entry, target_is_directory=entry.is_dir())
resources = scratch / "dapr-resources"
resources.mkdir()
(resources / "statestore.yaml").write_text("apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: statestore\nspec:\n  type: state.in-memory\n  version: v1\n  metadata: []\n")
ports = []
for _ in range(4):
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        ports.append(listener.getsockname()[1])
command = [str(Path.home() / ".dapr/bin/daprd"), "--app-id", "story67-owned-" + str(os.getpid()), "--resources-path", str(resources), "--dapr-http-port", str(ports[0]), "--dapr-grpc-port", str(ports[1]), "--metrics-port", str(ports[2]), "--dapr-internal-grpc-port", str(ports[3]), "--log-level", "warn"]
with (out / ("daprd" + attempt + ".log")).open("w") as log:
    child = subprocess.Popen(command, stdout=log, stderr=subprocess.STDOUT)
    result = 1
    try:
        endpoint = "http://127.0.0.1:" + str(ports[0])
        for _ in range(100):
            try:
                urllib.request.urlopen(endpoint + "/v1.0/healthz/outbound", timeout=1).close()
                break
            except Exception:
                if child.poll() is not None:
                    raise RuntimeError("Owned Dapr sidecar exited before readiness.")
                time.sleep(0.2)
        else:
            raise RuntimeError("Owned Dapr sidecar did not become ready.")
        environment.update(STORY61_DAPR_HTTP_ENDPOINT=endpoint, STORY61_DAPR_GRPC_ENDPOINT="http://127.0.0.1:" + str(ports[1]), STORY67_AUTHENTICATED_OBSERVATIONS=str(out / ("authenticated-observations" + attempt + ".json")))
        assembly = scratch / "artifacts/bin/Hexalith.Projects.Server.Tests/debug/Hexalith.Projects.Server.Tests.dll"
        native_command = ["dotnet", str(assembly), "-xml", str(out / ("Server-owned" + attempt + ".xml"))]
        if environment.get("STORY67_TEST_CLASS"):
            native_command.extend(["-class", environment["STORY67_TEST_CLASS"]])
        result = run("Server-owned-tests", native_command, environment)
    finally:
        child.terminate()
        try:
            child.wait(timeout=15)
        except subprocess.TimeoutExpired:
            child.kill()
            child.wait()
        (out / ("sidecar-cleanup" + attempt + ".json")).write_text(json.dumps({"ownedPid": child.pid, "reaped": child.poll() is not None, "component": "state.in-memory", "productionAcceptance": False}, indent=2) + "\n")
raise SystemExit(result)
