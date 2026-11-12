"""Prove current G6 archive refusal boundaries without executing synthetic tools."""
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import tarfile
import tempfile
import subprocess
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("install_g6_current_tools", ROOT / "tools/qualification/install_g6_current_tools.py")
INSTALL = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(INSTALL)


def archive(binary, symlink=False):
    output = io.BytesIO()
    with tarfile.open(fileobj=output, mode="w:gz") as tar:
        member = tarfile.TarInfo(binary)
        if symlink:
            member.type = tarfile.SYMTYPE
            member.linkname = "/untrusted/target"
            tar.addfile(member)
        else:
            data = ("synthetic " + binary).encode()
            member.size = len(data)
            tar.addfile(member, io.BytesIO(data))
        other = tarfile.TarInfo("../../escape")
        other.size = 1
        tar.addfile(other, io.BytesIO(b"x"))
    return output.getvalue()


class CurrentToolArchiveTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="g6-current-install-controls-")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.builds = self.root / "builds"
        (self.builds / "Props").mkdir(parents=True)
        self.scratch = self.root / "scratch"
        self.scratch.mkdir()
        self.policy = {"tuple": {"daprCli": "1.19.1", "daprRuntime": "1.19.1", "aspireCli": "13.6.5"}}
        self.archives = {"dapr": archive("dapr"), "daprd": archive("daprd")}
        self.identity = {"schema": "hexalith.builds.dapr-toolchain-sha256.v1", "cli": {}, "runtime": {}}
        self.write_identity()

    def write_identity(self):
        for kind, binary in (("cli", "dapr"), ("runtime", "daprd")):
            self.identity[kind]["1.19.1"] = {binary + "_linux_amd64.tar.gz": hashlib.sha256(self.archives[binary]).hexdigest()}
        (self.builds / "Props/dapr-toolchain-sha256.json").write_text(json.dumps(self.identity))

    def run_install(self, runtime_version="1.19.1", cli_version="1.19.1"):
        def download(url, timeout):
            self.assertEqual(60, timeout)
            binary = "daprd" if "/daprd_" in url else "dapr"
            repository = "dapr/dapr" if binary == "daprd" else "dapr/cli"
            self.assertEqual(f"https://github.com/{repository}/releases/download/v1.19.1/{binary}_linux_amd64.tar.gz", url)
            return io.BytesIO(self.archives[binary])
        def execute(args, **kwargs):
            if Path(args[0]).name == "daprd":
                return subprocess.CompletedProcess(args, 0, runtime_version + "\n", "")
            if Path(args[0]).name == "dapr":
                return subprocess.CompletedProcess(args, 0, "CLI version: " + cli_version + "\nRuntime version: n/a\n", "")
            return subprocess.CompletedProcess(args, 0, "", "")
        with patch.object(INSTALL.urllib.request, "urlopen", side_effect=download) as network, \
             patch.object(INSTALL.subprocess, "run", side_effect=execute) as command:
            INSTALL.install_current_tools(self.scratch, self.policy, self.builds, {"PATH": "/controlled-tools"})
        return network, command

    def test_verified_archives_write_only_exact_executables_and_pin_aspire(self):
        network, command = self.run_install()
        self.assertEqual(2, network.call_count)
        for folder, binary in (("tools", "dapr"), ("runtime", "daprd")):
            self.assertEqual(("synthetic " + binary).encode(), (self.scratch / folder / binary).read_bytes())
        self.assertFalse((self.root / "escape").exists())
        self.assertFalse((self.scratch / "escape").exists())
        self.assertEqual(["dotnet", "tool", "install", "Aspire.Cli", "--tool-path", str(self.scratch / "tools"), "--version", "13.6.5"], command.call_args.args[0])
        self.assertTrue(command.call_args.kwargs["check"])
        self.assertEqual({"PATH": "/controlled-tools"}, command.call_args.kwargs["env"])

    def test_actual_runtime_version_drift_is_refused(self):
        with self.assertRaisesRegex(RuntimeError, "exact selected versions"):
            self.run_install(runtime_version="1.19.2")

    def test_actual_cli_version_drift_is_refused(self):
        with self.assertRaisesRegex(RuntimeError, "exact selected versions"):
            self.run_install(cli_version="1.19.2")

    def test_changed_archive_refuses_before_any_executable_is_written(self):
        self.archives["dapr"] += b"changed"
        with self.assertRaisesRegex(RuntimeError, "archive identity mismatch"):
            self.run_install()
        self.assertFalse((self.scratch / "tools/dapr").exists())

    def test_missing_identity_refuses_before_network_or_tool_execution(self):
        self.identity["cli"] = {}
        (self.builds / "Props/dapr-toolchain-sha256.json").write_text(json.dumps(self.identity))
        with patch.object(INSTALL.urllib.request, "urlopen") as network, \
             patch.object(INSTALL.subprocess, "run") as command, \
             self.assertRaisesRegex(RuntimeError, "no exact archive identity"):
            INSTALL.install_current_tools(self.scratch, self.policy, self.builds, {})
        network.assert_not_called()
        command.assert_not_called()

    def test_even_checksum_matching_symlink_is_refused(self):
        self.archives["dapr"] = archive("dapr", symlink=True)
        self.write_identity()
        with self.assertRaisesRegex(RuntimeError, "exactly one regular dapr"):
            self.run_install()
        self.assertFalse((self.scratch / "tools/dapr").exists())

    def test_unknown_pair_has_no_unverified_download_fallback(self):
        self.policy["tuple"]["daprRuntime"] = "1.19.2"
        with patch.object(INSTALL.urllib.request, "urlopen") as network, \
             patch.object(INSTALL.subprocess, "run") as command, \
             self.assertRaisesRegex(RuntimeError, "explicitly qualified Dapr1.19.1 pair"):
            INSTALL.install_current_tools(self.scratch, self.policy, self.builds, {})
        network.assert_not_called()
        command.assert_not_called()

    def test_existing_owned_path_is_preserved(self):
        tools = self.scratch / "tools"
        tools.mkdir()
        (tools / "dapr").write_bytes(b"previous owned bytes")
        with self.assertRaisesRegex(RuntimeError, "Refusing to overwrite"):
            self.run_install()
        self.assertEqual(b"previous owned bytes", (tools / "dapr").read_bytes())


if __name__ == "__main__":
    unittest.main()
