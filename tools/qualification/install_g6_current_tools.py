"""Install exact current G6 tools from the pinned producer's verified archive identities."""
from __future__ import annotations

import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
import tarfile
import urllib.request


VERSION = re.compile(r"[0-9]+\.[0-9]+\.[0-9]+")
SHA256 = re.compile(r"[0-9a-f]{64}")


def install_current_tools(scratch: Path, policy: dict, builds: Path, environment: dict[str, str]) -> None:
    """Verify selected archives before writing only their sole regular executable."""
    pins = policy["tuple"]
    if (pins["daprCli"], pins["daprRuntime"]) != ("1.19.1", "1.19.1"):
        raise RuntimeError("Current G6 installer requires the explicitly qualified Dapr1.19.1 pair")
    identity = json.loads((builds / "Props/dapr-toolchain-sha256.json").read_text(encoding="utf-8"))
    if identity.get("schema") != "hexalith.builds.dapr-toolchain-sha256.v1":
        raise RuntimeError("Unsupported producer Dapr archive identity schema")
    tools = scratch / "tools"
    runtime = scratch / "runtime"
    for kind, repository, binary, field, destination in (
        ("cli", "dapr/cli", "dapr", "daprCli", tools),
        ("runtime", "dapr/dapr", "daprd", "daprRuntime", runtime),
    ):
        version = pins[field]
        if not VERSION.fullmatch(version):
            raise RuntimeError("Current G6 Dapr version must be exact and stable")
        name = binary + "_linux_amd64.tar.gz"
        expected = identity.get(kind, {}).get(version, {}).get(name)
        if not isinstance(expected, str) or not SHA256.fullmatch(expected):
            raise RuntimeError("Pinned producer has no exact archive identity for " + name)
        url = f"https://github.com/{repository}/releases/download/v{version}/{name}"
        with urllib.request.urlopen(url, timeout=60) as response:
            data = response.read(100 * 1024 * 1024 + 1)
        if len(data) > 100 * 1024 * 1024 or hashlib.sha256(data).hexdigest() != expected:
            raise RuntimeError("Official Dapr archive identity mismatch for " + name)
        with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as archive:
            files = [member for member in archive.getmembers() if Path(member.name).name == binary]
            if len(files) != 1 or not files[0].isfile() or files[0].size > 256 * 1024 * 1024:
                raise RuntimeError("Official Dapr archive must contain exactly one regular " + binary)
            executable = archive.extractfile(files[0])
            if executable is None:
                raise RuntimeError("Official Dapr archive executable is unavailable")
            content = executable.read()
        destination.mkdir(parents=True, exist_ok=True)
        target = destination / binary
        if target.exists():
            raise RuntimeError("Refusing to overwrite an existing isolated tool: " + binary)
        target.write_bytes(content)
        target.chmod(0o755)
    observed_runtime = subprocess.run([str(runtime / "daprd"), "--version"], check=True,
                                      capture_output=True, text=True, env=environment, timeout=30).stdout.strip()
    observed_cli = subprocess.run([str(tools / "dapr"), "--version"], check=True,
                                  capture_output=True, text=True, env=environment, timeout=30).stdout
    if observed_runtime != pins["daprRuntime"] or not re.search(
            r"(?m)^CLI version:\s*" + re.escape(pins["daprCli"]) + r"\s*$", observed_cli):
        raise RuntimeError("Installed current G6 executables do not report the exact selected versions")
    subprocess.run(["dotnet", "tool", "install", "Aspire.Cli", "--tool-path", str(tools),
                    "--version", pins["aspireCli"]], check=True, env=environment)
