"""Exercise qualification manifest verification through a real Git repository."""

import hashlib
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


TOOL = Path(__file__).resolve().parents[2] / "tools/qualification/verify_retained_manifests.py"
ARCHIVE = Path("_bmad-output/implementation-artifacts/qualification-evidence")
MANIFEST = "artifact-manifest.sha256"


class RetainedManifestTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.repo = Path(temporary.name)
        self.env = {k: v for k, v in os.environ.items() if not k.startswith("GIT_")}
        self.env.update(GIT_CONFIG_NOSYSTEM="1", GIT_CONFIG_GLOBAL=os.devnull)
        self.git("init", "--quiet")
        self.bundle = self.repo / ARCHIVE / "attempt"
        self.bundle.mkdir(parents=True)
        self.payload = b"\x00evidence\r\n"
        self.digest = hashlib.sha256(self.payload).hexdigest()
        (self.bundle / "trace data.bin").write_bytes(self.payload)

    def git(self, *args, data=None):
        return subprocess.run(
            ["git", *args], cwd=self.repo, env=self.env, check=True, input=data, capture_output=True
        ).stdout

    def stage_blob(self, path, mode, content):
        oid = self.git("hash-object", "-w", "--stdin", data=content).decode("ascii").strip()
        self.git("update-index", "--add", "--cacheinfo", mode, oid, f"{ARCHIVE.as_posix()}/{path}")

    def commit(self):
        self.git("-c", "user.name=Test", "-c", "user.email=test@example.invalid", "commit", "-qm", "fixture")

    def verify(self):
        return subprocess.run(
            [sys.executable, str(TOOL)], cwd=self.bundle, env=self.env, text=True, capture_output=True
        )

    def test_checks_committed_bytes_in_each_bundle(self):
        (self.bundle / MANIFEST).write_bytes(f"{self.digest} *./trace data.bin\r\n".encode())
        second = self.bundle.parent / "another"
        second.mkdir()
        (second / "trace data.bin").write_bytes(b"other")
        (second / MANIFEST).write_text(f"{hashlib.sha256(b'other').hexdigest()}  trace data.bin\n")
        self.git("add", ".")
        self.commit()
        (self.bundle / "trace data.bin").unlink()
        (second / MANIFEST).write_text("local edit\n")
        result = self.verify()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(2, result.stdout.count("1/1 entries verified"))

    def test_reports_each_failure_and_continues_to_other_manifests(self):
        (self.bundle / MANIFEST).write_text(
            f"{self.digest}  untracked.log\n"
            f"{'0' * 64}  trace data.bin\n"
            f"{self.digest}  ./trace data.bin\n"
            f"{self.digest}  ../other.bin\n"
            f"{self.digest}  /absolute.bin\n"
            f"{self.digest}  trace data.bin/\n"
            f"{self.digest}  trace data.bin/.\n"
            f"\\{self.digest}  invalid\\qescape\n"
            "malformed row\n"
        )
        empty = self.bundle.parent / "empty"
        empty.mkdir()
        (empty / MANIFEST).touch()
        self.git("add", ".")
        self.commit()
        (self.bundle / "untracked.log").write_bytes(self.payload)
        result = self.verify()
        self.assertEqual(1, result.returncode)
        for message in ("no tracked regular file", "expected " + "0" * 64, "duplicate entry",
                        "invalid bundle-relative path '../other.bin'", "invalid bundle-relative path '/absolute.bin'",
                        "invalid bundle-relative path 'trace data.bin/'", "invalid bundle-relative path 'trace data.bin/.'",
                        "invalid sha256sum filename escape",
                        "expected a sha256sum entry", "empty manifest"):
            self.assertIn(message, result.stderr)

    def test_reads_literal_unicode_separator_and_gnu_escaped_filenames(self):
        rows = []
        for actual, recorded in (
            ("line\u2028separator", f"{self.digest}  line\u2028separator"),
            ("two\\parts", f"\\{self.digest}  two\\\\parts"),
            ("two\nlines", f"\\{self.digest}  two\\nlines"),
            ("two\rparts", f"\\{self.digest}  two\\rparts"),
        ):
            self.stage_blob(f"attempt/{actual}", "100644", self.payload)
            rows.append(recorded)
        self.stage_blob(f"attempt/{MANIFEST}", "100644", "\n".join(rows).encode("utf-8"))
        self.commit()
        result = self.verify()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("4/4 entries verified", result.stdout)

    def test_rejects_symlinks_even_if_their_blob_matches(self):
        link_hash = hashlib.sha256(b"trace data.bin").hexdigest()
        (self.bundle / MANIFEST).write_text(f"{link_hash}  link\n")
        self.git("add", ".")
        self.stage_blob("attempt/link", "120000", b"trace data.bin")
        self.stage_blob(f"second/{MANIFEST}", "120000", f"../attempt/{MANIFEST}".encode("ascii"))
        self.commit()
        result = self.verify()
        self.assertEqual(1, result.returncode)
        self.assertIn("no tracked regular file for 'link'", result.stderr)
        self.assertIn("manifest must be a tracked regular file", result.stderr)

    def test_requires_a_committed_manifest(self):
        self.git("add", ".")
        self.commit()
        (self.bundle / MANIFEST).write_text(f"{self.digest}  trace data.bin\n")
        result = self.verify()
        self.assertEqual(1, result.returncode)
        self.assertIn(f"no {MANIFEST}", result.stderr)


if __name__ == "__main__":
    unittest.main()
