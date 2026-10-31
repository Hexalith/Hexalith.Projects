"""Publication and source controls use temporary files and mocked reads; never create Git commits."""
import contextlib
import copy
import hashlib
import io
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile

import prepare_builds_artifacts
import run_tuple_ci
from unittest.mock import patch

from builds_tuple import BINDING, ROOT, load_binding, validate_owner_pins, validate_recorded_source, verify_catalog


class BuildsTupleTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.path = self.root / "binding.json"
        # A fully bound historical release is a control, never a proposed future publication.
        self.binding = copy.deepcopy(load_binding(require_publication=False))
        self.binding["producer"]["sourceRevision"] = "d1931b1e803144048d927fff16cb8119afde541e"
        self.binding["producer"]["catalog"].update(
            blob="679a0c342f24aa40905eb84ad274f7e6647c995e",
            sha256="501d72650e3a42afc8fde0345a69208669e7fabdb98b6300f20afca07585d405")
        self.binding["publication"] = {
            "version": "4.38.3", "sourceRevision": self.binding["producer"]["sourceRevision"],
            "releaseTag": "v4.38.3", "catalogSha256": self.binding["producer"]["catalog"]["sha256"],
            "archiveSha256": "6c074186a9bbfb66c558fe5d1090e1e35fdd58577c0396266074bd1cea8919fd",
            "toolingSha256": "4e307577ae4fb02104914951a9905a1c9dd453447a80735443d24fbc83496823"}

    def load(self):
        self.path.write_text(json.dumps(self.binding))
        return load_binding(self.path)

    def test_complete_historical_binding_is_readable(self):
        self.assertEqual(self.load(), self.binding)

    def test_each_missing_publication_field_refuses(self):
        for key in self.binding["publication"]:
            with self.subTest(key=key):
                original = self.binding["publication"][key]
                self.binding["publication"][key] = None
                with self.assertRaisesRegex(ValueError, "publication binding is unavailable"):
                    self.load()
                self.binding["publication"][key] = original

    def test_source_tag_and_catalog_mismatches_refuse(self):
        for key, value in (("sourceRevision", load_binding(require_publication=False)["producer"]["sourceRevision"]),
                           ("releaseTag", "v4.35.2"), ("catalogSha256", load_binding(require_publication=False)["producer"]["catalog"]["sha256"])):
            with self.subTest(key=key):
                original = self.binding["publication"][key]
                self.binding["publication"][key] = value
                with self.assertRaisesRegex(ValueError, "differs"):
                    self.load()
                self.binding["publication"][key] = original

    def test_short_source_and_malformed_hash_refuse(self):
        self.binding["producer"]["sourceRevision"] = "d1931b1"
        with self.assertRaisesRegex(ValueError, "full lowercase Git"):
            self.load()
        self.binding["producer"]["sourceRevision"] = self.binding["publication"]["sourceRevision"]
        self.binding["publication"]["archiveSha256"] = "unavailable"
        with self.assertRaisesRegex(ValueError, "SHA-256"):
            self.load()

    def test_candidate_acceptance_cannot_be_enabled(self):
        self.binding["candidateAcceptance"] = True
        with self.assertRaisesRegex(ValueError, "candidateAcceptance:false"):
            self.load()

    def test_owner_tool_and_floor_must_both_match(self):
        (self.root / ".config").mkdir()
        tools = self.root / ".config/dotnet-tools.json"
        floor = self.root / "hexalith-platform.json"
        tools.write_text(json.dumps({"tools": {"hexalith.builds.module.cli": {"version": "4.38.3"}}}))
        floor.write_text(json.dumps({"toolVersionRange": "[4.38.3, 5.0.0-0)"}))
        validate_owner_pins(self.binding, self.root)
        for path, content in ((tools, {"tools": {"hexalith.builds.module.cli": {"version": "4.35.2"}}}),
                              (floor, {"toolVersionRange": "[4.35.2, 5.0.0-0)"})):
            with self.subTest(path=path.name):
                original = path.read_bytes()
                path.write_text(json.dumps(content))
                with self.assertRaisesRegex(ValueError, "owner tool pin and range floor"):
                    validate_owner_pins(self.binding, self.root)
                path.write_bytes(original)

    def test_dirty_or_wrong_recorded_owner_refuses(self):
        source = self.binding["producer"]["sourceRevision"]
        validate_recorded_source(self.binding, source, False)
        for pin, dirty in ((source, True), (load_binding(require_publication=False)["producer"]["sourceRevision"], False)):
            with self.subTest(pin=pin, dirty=dirty), self.assertRaisesRegex(ValueError, "clean recorded Platform"):
                validate_recorded_source(self.binding, pin, dirty)

    def test_checkout_catalog_source_and_blob_are_checked(self):
        for outputs, message in (([b"wrong\n"], "checkout differs"),
                                 ([self.binding["producer"]["sourceRevision"].encode(), b"wrong\n"], "catalog blob differs"),
                                 ([self.binding["producer"]["sourceRevision"].encode(), self.binding["producer"]["catalog"]["blob"].encode(), b"changed"], "catalog SHA-256 differs")):
            with self.subTest(message=message), patch("builds_tuple.subprocess.check_output", side_effect=outputs), self.assertRaisesRegex(ValueError, message):
                verify_catalog(self.binding, self.root)

    def test_archive_control_checks_executable_source_and_keeps_package_version(self):
        # Synthetic bytes exercise extraction/receipt mechanics; they are never publication evidence.
        archive = self.root / "control.nupkg"
        payload = b"synthetic unit-test tooling"
        with zipfile.ZipFile(archive, "w") as package:
            package.writestr("tools/net10.0/any/Hexalith.Builds.Tooling.dll", payload)
            package.writestr("tools/net10.0/any/Hexalith.Builds.Module.Cli.dll", b"synthetic CLI")
        self.binding["publication"]["archiveSha256"] = hashlib.sha256(archive.read_bytes()).hexdigest()
        self.binding["publication"]["toolingSha256"] = hashlib.sha256(payload).hexdigest()
        source = self.binding["producer"]["sourceRevision"]
        for index, executable in enumerate(("4.38.3+" + source, "4.38.3+wrong-source")):
            output = self.root / ("archive-control-" + str(index))
            with patch.object(sys, "argv", ["prepare", "--archive", str(archive), "--output", str(output)]), patch.object(prepare_builds_artifacts, "load_binding", return_value=self.binding), patch.object(prepare_builds_artifacts, "validate_owner_pins"), patch.object(prepare_builds_artifacts, "source_state", return_value={"dirty": False}), patch.object(prepare_builds_artifacts.subprocess, "check_output", side_effect=[source, executable]), patch.object(prepare_builds_artifacts.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "verified control", "")), contextlib.redirect_stdout(io.StringIO()):
                if index:
                    with self.assertRaisesRegex(ValueError, "executable source/version"):
                        prepare_builds_artifacts.main()
                else:
                    prepare_builds_artifacts.main()
                    receipt = json.loads((output / "artifact-identity.json").read_bytes())
                    self.assertEqual(receipt["version"], "4.38.3")
                    self.assertEqual(receipt["buildsRevision"], source)
                    self.assertIs(receipt["candidateAcceptance"], False)

    def test_preparation_blocks_artifact_download_and_retains_terminal_preflight(self):
        self.binding["publication"]["version"] = None
        self.path.write_text(json.dumps(self.binding))
        incomplete = lambda: load_binding(self.path)
        output = self.root / "artifacts"
        with patch.object(sys, "argv", ["prepare", "--output", str(output), "--require-recorded"]), patch.object(prepare_builds_artifacts, "load_binding", incomplete), self.assertRaisesRegex(ValueError, "publication binding is unavailable"):
            prepare_builds_artifacts.main()
        self.assertFalse(output.exists())
        evidence = self.root / "preflight"
        with patch.object(sys, "argv", ["preflight", "--evidence", str(evidence), "--audit-only"]), patch.object(run_tuple_ci.signal, "signal"), patch.object(run_tuple_ci, "load_binding", incomplete), self.assertRaisesRegex(ValueError, "publication binding is unavailable"):
            run_tuple_ci.main()
        receipt = json.loads((evidence / "tuple-ci-result.json").read_bytes())
        self.assertEqual(receipt["status"], "failed")
        self.assertIs(receipt["candidateAcceptance"], False)
        self.assertIn("publication binding is unavailable", receipt["blocker"])


if __name__ == "__main__":
    unittest.main()
