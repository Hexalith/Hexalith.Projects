"""Hermetic fixture tests for the shared BMAD skill renderer."""

from __future__ import annotations

import hashlib
import importlib.util
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import unittest
from dataclasses import dataclass
from pathlib import Path
from types import ModuleType
from unittest import mock


PROJECT_ROOT = Path(__file__).resolve().parents[2]
RENDERER_PATH = PROJECT_ROOT / "_bmad" / "scripts" / "render_skill.py"
CI_PATH = PROJECT_ROOT / ".github" / "workflows" / "ci.yml"


@dataclass(frozen=True)
class RendererFixture:
    """Paths owned by one temporary renderer fixture."""

    project_root: Path
    skill_root: Path
    caller_root: Path


def _write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def _sha256(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def _canonical_json(value: object) -> bytes:
    return json.dumps(
        value, ensure_ascii=False, sort_keys=True, separators=(",", ":")
    ).encode("utf-8")


def _tree_bytes(root: Path) -> dict[str, bytes]:
    return {
        path.relative_to(root).as_posix(): path.read_bytes()
        for path in root.rglob("*")
        if path.is_file()
    }


def _load_renderer_module() -> ModuleType:
    module_name = "fixture_render_skill"
    specification = importlib.util.spec_from_file_location(module_name, RENDERER_PATH)
    if specification is None or specification.loader is None:
        raise RuntimeError(f"Unable to load renderer module from {RENDERER_PATH}")
    module = importlib.util.module_from_spec(specification)
    scripts_root = str(RENDERER_PATH.parent)
    sys.path.insert(0, scripts_root)
    try:
        specification.loader.exec_module(module)
    finally:
        sys.path.remove(scripts_root)
    return module


class RenderSkillFixtureTests(unittest.TestCase):
    """Exercise the checked-in renderer with dependency-free temporary fixtures."""

    def make_fixture(
        self,
        root: Path,
        *,
        workflow: str = "Fixture workflow.\n",
        guide: str = "Fixture guide.\n",
        central_config: str = '[fixture]\nvalue = "base"\n',
        customization: str | None = None,
    ) -> RendererFixture:
        project_root = root / "project fixture with spaces"
        skill_root = root / "skill fixtures" / "fixture-skill"
        caller_root = root / "caller cwd with spaces"
        caller_root.mkdir(parents=True)
        _write_text(project_root / "_bmad" / "config.toml", central_config)
        _write_text(skill_root / "SKILL.md", "---\nname: fixture-skill\n---\n")
        _write_text(skill_root / "workflow.md", workflow)
        _write_text(skill_root / "guide.md", guide)
        if customization is not None:
            _write_text(skill_root / "customize.toml", customization)
        return RendererFixture(project_root, skill_root, caller_root)

    def run_renderer(
        self, fixture: RendererFixture
    ) -> subprocess.CompletedProcess[str]:
        environment = os.environ.copy()
        environment["PYTHONDONTWRITEBYTECODE"] = "1"
        return subprocess.run(
            [
                sys.executable,
                str(RENDERER_PATH),
                "--project-root",
                str(fixture.project_root),
                "--skill",
                str(fixture.skill_root),
            ],
            cwd=fixture.caller_root,
            env=environment,
            capture_output=True,
            text=True,
            encoding="utf-8",
            check=False,
            timeout=30,
        )

    def assert_success(
        self,
        completed: subprocess.CompletedProcess[str],
    ) -> Path:
        self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
        self.assertEqual("", completed.stderr)
        prefix = "read and follow "
        self.assertTrue(completed.stdout.startswith(prefix), completed.stdout)
        self.assertTrue(completed.stdout.endswith("\n"), completed.stdout)
        entry = Path(completed.stdout[len(prefix) :].rstrip("\n"))
        self.assertEqual(f"read and follow {entry}\n", completed.stdout)
        self.assertTrue(entry.is_file(), entry)
        return entry

    def assert_halt(
        self,
        completed: subprocess.CompletedProcess[str],
        expected_prefix: str,
    ) -> None:
        self.assertEqual(1, completed.returncode, completed.stdout + completed.stderr)
        self.assertEqual("", completed.stderr)
        self.assertTrue(
            completed.stdout.startswith(expected_prefix),
            f"Expected prefix {expected_prefix!r}, got {completed.stdout!r}",
        )
        self.assertTrue(completed.stdout.endswith("\n"), completed.stdout)
        self.assertEqual(1, completed.stdout.count("\n"), completed.stdout)

    def assert_manifest_integrity(
        self,
        fixture: RendererFixture,
        entry: Path,
        expected_outputs: set[str],
    ) -> dict[str, object]:
        destination = entry.parent
        manifest = json.loads((destination / "manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(1, manifest["schema_version"])
        self.assertEqual(fixture.skill_root.name, manifest["skill"])
        self.assertEqual(str(fixture.project_root), manifest["project_root"])
        self.assertEqual("project-fixture-with-spaces", manifest["project_slug"])

        root_hash = _sha256(str(fixture.project_root).encode("utf-8"))[:12]
        self.assertEqual(root_hash, manifest["root_hash"])
        self.assertEqual(
            f"{manifest['project_slug']}-{root_hash}", destination.parent.name
        )
        self.assertEqual(fixture.skill_root.name, destination.parent.parent.name)

        inputs = manifest["inputs"]
        self.assertIsInstance(inputs, dict)
        generation_hash = _sha256(_canonical_json(inputs))[:20]
        self.assertEqual(generation_hash, manifest["generation_hash"])
        self.assertEqual(generation_hash, destination.name)
        self.assertEqual(str(fixture.project_root), inputs["project_root"])
        self.assertEqual(_sha256(RENDERER_PATH.read_bytes()), inputs["renderer_sha256"])

        sources = {
            path.relative_to(fixture.skill_root).as_posix(): _sha256(
                path.read_text(encoding="utf-8").encode("utf-8")
            )
            for path in fixture.skill_root.rglob("*.md")
            if path.name != "SKILL.md"
        }
        self.assertEqual(sources, inputs["source_sha256"])
        self.assertEqual(expected_outputs, set(manifest["outputs"]))
        self.assertEqual(
            expected_outputs | {"manifest.json"}, set(_tree_bytes(destination))
        )
        for name, expected_hash in manifest["outputs"].items():
            self.assertEqual(expected_hash, _sha256((destination / name).read_bytes()))
        return manifest

    def test_layer_precedence_lists_and_keyed_review_replacement(self) -> None:
        workflow = """Central winner: {{config.settings.winner}}
Base only: {{config.settings.base_only}}
User only: {{config.settings.user_only}}
Team only: {{config.settings.team_only}}
Final only: {{config.settings.final_only}}
Artifact root: {{.artifact_root}}
Headline: {workflow.headline}
Prepend:
{workflow.prepend_steps}
Append:
{workflow.append_steps}
Review:
{workflow.review_layers}
"""
        defaults = """[workflow]
headline = "default headline"
prepend_steps = ["default prepend"]
append_steps = ["default append"]

[[workflow.review_layers]]
id = "security"
name = "Default Security"
instruction = "default security instruction"
when = "always"

[[workflow.review_layers]]
id = "structure"
name = "Default Structure"
instruction = "default structure instruction"
"""
        with tempfile.TemporaryDirectory(prefix="render skill fixtures ") as directory:
            fixture = self.make_fixture(
                Path(directory),
                workflow=workflow,
                central_config="""[settings]
winner = "base"
base_only = "base value"
artifact_root = "{project-root}/artifacts"
""",
                customization=defaults,
            )
            custom_root = fixture.project_root / "_bmad" / "custom"
            _write_text(
                fixture.project_root / "_bmad" / "config.user.toml",
                '[settings]\nwinner = "central user"\nuser_only = "user value"\n',
            )
            _write_text(
                custom_root / "config.toml",
                '[settings]\nwinner = "team"\nteam_only = "team value"\n',
            )
            _write_text(
                custom_root / "config.user.toml",
                '[settings]\nwinner = "custom user"\nfinal_only = "final value"\n',
            )
            _write_text(
                custom_root / f"{fixture.skill_root.name}.toml",
                """[workflow]
headline = "team headline"
prepend_steps = ["team prepend"]
append_steps = ["team append"]

[[workflow.review_layers]]
id = "security"
instruction = "team security instruction"

[[workflow.review_layers]]
id = "performance"
name = "Team Performance"
instruction = "team performance instruction"
""",
            )
            _write_text(
                custom_root / f"{fixture.skill_root.name}.user.toml",
                """[workflow]
headline = "user headline"
prepend_steps = ["user prepend"]
append_steps = ["user append"]

[[workflow.review_layers]]
id = "performance"
name = "User Performance"
instruction = ""

[[workflow.review_layers]]
id = "final"
name = "User Final"
instruction = "user final instruction"
""",
            )

            entry = self.assert_success(self.run_renderer(fixture))
            rendered = entry.read_text(encoding="utf-8")
            expected_scalars = (
                "Central winner: custom user",
                "Base only: base value",
                "User only: user value",
                "Team only: team value",
                "Final only: final value",
                f"Artifact root: {fixture.project_root}/artifacts",
                "Headline: user headline",
            )
            for expected in expected_scalars:
                self.assertIn(expected, rendered)
            self.assertIn(
                "Prepend:\n- default prepend\n- team prepend\n- user prepend",
                rendered,
            )
            self.assertIn(
                "Append:\n- default append\n- team append\n- user append",
                rendered,
            )
            self.assertIn("#### security (`security`)", rendered)
            self.assertIn("team security instruction", rendered)
            self.assertIn("#### Default Structure (`structure`)", rendered)
            self.assertIn("#### User Final (`final`)", rendered)
            self.assertNotIn("Default Security", rendered)
            self.assertNotIn("Run only when: always", rendered)
            self.assertNotIn("Performance", rendered)
            self.assertLess(rendered.index("(`security`)"), rendered.index("(`structure`)"))
            self.assertLess(rendered.index("(`structure`)"), rendered.index("(`final`)"))

            manifest = self.assert_manifest_integrity(
                fixture, entry, {"workflow.md", "guide.md"}
            )
            resolved = manifest["inputs"]["resolved_values"]
            self.assertEqual(
                ["default prepend", "team prepend", "user prepend"],
                resolved["customization.workflow.prepend_steps"],
            )
            review_layers = resolved["customization.workflow.review_layers"]
            self.assertEqual(
                ["security", "structure", "performance", "final"],
                [layer["id"] for layer in review_layers],
            )
            self.assertEqual(
                {
                    "id": "security",
                    "name": "security",
                    "instruction": "team security instruction",
                },
                review_layers[0],
            )

    def test_inserted_tokens_are_opaque_and_source_snapshots_bind_to_generation(self) -> None:
        opaque = (
            "config={{config.fixture.value}}; short={{.value}}; "
            "workflow={workflow.other}; runtime={project-root}; "
            "snapshot=[[bmad-snapshot:missing.md]]; skill={skill-root}/guide.md"
        )
        workflow = """Opaque: {workflow.opaque}
Source config: {{config.fixture.value}}
Source snapshot: [[bmad-snapshot:guide.md]]
"""
        customization = f'[workflow]\nopaque = "{opaque}"\n'
        with tempfile.TemporaryDirectory(prefix="render skill fixtures ") as directory:
            fixture = self.make_fixture(
                Path(directory), workflow=workflow, customization=customization
            )
            nested_source = fixture.skill_root / "nested" / "details.md"
            nested_target = fixture.skill_root / "nested" / "target.md"
            _write_text(
                nested_source,
                """Nested opaque: {workflow.opaque}
Nested config: {{config.fixture.value}}
Nested snapshot: [[bmad-snapshot:nested/target.md]]
""",
            )
            _write_text(nested_target, "Nested target.\n")
            entry = self.assert_success(self.run_renderer(fixture))
            destination = entry.parent
            expected = (
                f"Opaque: {opaque.replace('{skill-root}', str(destination))}\n"
                "Source config: base\n"
                f"Source snapshot: {destination / 'guide.md'}\n"
            )
            self.assertEqual(expected, entry.read_text(encoding="utf-8"))
            expected_nested = (
                f"Nested opaque: {opaque.replace('{skill-root}', str(destination))}\n"
                "Nested config: base\n"
                f"Nested snapshot: {destination / 'nested' / 'target.md'}\n"
            )
            self.assertEqual(
                expected_nested,
                (destination / "nested" / "details.md").read_text(encoding="utf-8"),
            )
            self.assertEqual(
                "Nested target.\n",
                (destination / "nested" / "target.md").read_text(encoding="utf-8"),
            )
            self.assert_manifest_integrity(
                fixture,
                entry,
                {
                    "workflow.md",
                    "guide.md",
                    "nested/details.md",
                    "nested/target.md",
                },
            )

    def test_identical_render_reuses_bytes_and_changed_input_is_immutable(self) -> None:
        with tempfile.TemporaryDirectory(prefix="render skill fixtures ") as directory:
            fixture = self.make_fixture(
                Path(directory), workflow="Value: {{config.fixture.value}}\n"
            )
            first_entry = self.assert_success(self.run_renderer(fixture))
            first_destination = first_entry.parent
            first_tree = _tree_bytes(first_destination)
            sentinel = 1_600_000_000_000_000_000
            for path in first_destination.rglob("*"):
                if path.is_file():
                    os.utime(path, ns=(sentinel, sentinel))
            preserved_mtimes = {
                path.relative_to(first_destination).as_posix(): path.stat().st_mtime_ns
                for path in first_destination.rglob("*")
                if path.is_file()
            }

            second_entry = self.assert_success(self.run_renderer(fixture))
            self.assertEqual(first_entry, second_entry)
            self.assertEqual(
                preserved_mtimes,
                {
                    path.relative_to(first_destination).as_posix(): path.stat().st_mtime_ns
                    for path in first_destination.rglob("*")
                    if path.is_file()
                },
            )
            self.assertEqual(first_tree, _tree_bytes(first_destination))

            _write_text(
                fixture.project_root / "_bmad" / "config.user.toml",
                '[fixture]\nvalue = "changed"\n',
            )
            third_entry = self.assert_success(self.run_renderer(fixture))
            self.assertNotEqual(first_entry, third_entry)
            self.assertEqual("Value: changed\n", third_entry.read_text(encoding="utf-8"))
            self.assertEqual(first_tree, _tree_bytes(first_destination))
            generations = {
                path.parent
                for path in (fixture.project_root / "_bmad" / "render").rglob(
                    "manifest.json"
                )
            }
            self.assertEqual({first_destination, third_entry.parent}, generations)

    def test_rename_failure_removes_populated_staging_and_destination(self) -> None:
        renderer = _load_renderer_module()
        with tempfile.TemporaryDirectory(prefix="render skill fixtures ") as directory:
            parent = Path(directory) / "publication with spaces"
            destination = parent / "generation"
            outputs = {"nested/workflow.md": b"fixture output\n"}
            manifest = {
                "outputs": {"nested/workflow.md": _sha256(outputs["nested/workflow.md"])}
            }

            def fail_rename(source: Path, target: Path) -> None:
                staging = Path(source)
                self.assertEqual(destination, Path(target))
                self.assertEqual(
                    outputs["nested/workflow.md"],
                    (staging / "nested" / "workflow.md").read_bytes(),
                )
                self.assertTrue((staging / "manifest.json").is_file())
                raise OSError("forced rename failure")

            with mock.patch.object(renderer.os, "rename", side_effect=fail_rename):
                with self.assertRaisesRegex(OSError, "forced rename failure"):
                    renderer._publish(destination, outputs, manifest)

            self.assertFalse(destination.exists())
            self.assertEqual([], list(parent.glob(".staging-*")))

    def test_invalid_inputs_halt_and_publish_nothing(self) -> None:
        valid_customization = '[workflow]\nmessage = "configured"\n'
        duplicate_reviews = """[workflow]
[[workflow.review_layers]]
id = "duplicate"
instruction = "first"
[[workflow.review_layers]]
id = "duplicate"
instruction = "second"
"""
        cases = (
            ("missing workflow", "Fixture.\n", None, "missing-workflow"),
            ("malformed source", "Fixture.\n", None, "malformed-source"),
            ("missing central config", "Fixture.\n", None, "missing-central"),
            ("malformed central config", "Fixture.\n", None, "malformed-central"),
            (
                "missing customization",
                "Configured: {workflow.message}\n",
                valid_customization,
                "missing-customization",
            ),
            (
                "malformed customization",
                "Configured: {workflow.message}\n",
                valid_customization,
                "malformed-customization",
            ),
            (
                "invalid review layers",
                "{workflow.review_layers}\n",
                duplicate_reviews,
                "invalid-reviews",
            ),
            (
                "undeclared snapshot",
                "[[bmad-snapshot:missing.md]]\n",
                None,
                "undeclared-snapshot",
            ),
        )
        with tempfile.TemporaryDirectory(prefix="render skill fixtures ") as directory:
            root = Path(directory)
            for index, (name, workflow, customization, mutation) in enumerate(cases):
                with self.subTest(name=name):
                    fixture = self.make_fixture(
                        root / f"case {index} with spaces",
                        workflow=workflow,
                        customization=customization,
                    )
                    central_path = fixture.project_root / "_bmad" / "config.toml"
                    customization_path = fixture.skill_root / "customize.toml"
                    workflow_path = fixture.skill_root / "workflow.md"
                    if mutation == "missing-workflow":
                        workflow_path.unlink()
                        expected = f"HALT: render entry is missing: {workflow_path}"
                    elif mutation == "malformed-source":
                        workflow_path.write_bytes(b"\xff")
                        expected = f"HALT: failed to read render source {workflow_path}:"
                    elif mutation == "missing-central":
                        central_path.unlink()
                        expected = f"HALT: required TOML file not found: {central_path}"
                    elif mutation == "malformed-central":
                        central_path.write_text("[invalid", encoding="utf-8")
                        expected = f"HALT: failed to parse {central_path}:"
                    elif mutation == "missing-customization":
                        customization_path.unlink()
                        expected = f"HALT: required TOML file not found: {customization_path}"
                    elif mutation == "malformed-customization":
                        customization_path.write_text("[invalid", encoding="utf-8")
                        expected = f"HALT: failed to parse {customization_path}:"
                    elif mutation == "invalid-reviews":
                        expected = "HALT: duplicate review layer id `duplicate`"
                    else:
                        expected = "HALT: snapshot reference targets undeclared source: missing.md"

                    self.assert_halt(self.run_renderer(fixture), expected)
                    render_root = fixture.project_root / "_bmad" / "render"
                    self.assertEqual(
                        [], list(render_root.rglob("*")) if render_root.exists() else []
                    )

    def test_missing_bmad_directory_halts_before_publishing(self) -> None:
        with tempfile.TemporaryDirectory(prefix="render skill fixtures ") as directory:
            fixture = self.make_fixture(Path(directory))
            shutil.rmtree(fixture.project_root / "_bmad")
            expected = f"HALT: project root does not contain _bmad/: {fixture.project_root}"
            self.assert_halt(self.run_renderer(fixture), expected)
            self.assertFalse((fixture.project_root / "_bmad").exists())

    def test_corrupt_existing_generations_halt_without_repair(self) -> None:
        def corrupt_manifest(destination: Path) -> str:
            (destination / "manifest.json").write_text("{", encoding="utf-8")
            return "HALT: corrupt existing generation"

        def collide_manifest(destination: Path) -> str:
            path = destination / "manifest.json"
            manifest = json.loads(path.read_text(encoding="utf-8"))
            manifest["skill"] = "colliding-skill"
            path.write_text(json.dumps(manifest), encoding="utf-8")
            return "HALT: generation collision or corruption at"

        def damage_output(destination: Path) -> str:
            (destination / "workflow.md").write_text("damaged\n", encoding="utf-8")
            return "HALT: generation output hash mismatch:"

        def remove_output(destination: Path) -> str:
            (destination / "guide.md").unlink()
            return "HALT: generation contains unexpected or missing files:"

        def add_output(destination: Path) -> str:
            (destination / "unexpected.txt").write_text("unexpected\n", encoding="utf-8")
            return "HALT: generation contains unexpected or missing files:"

        cases = (
            ("manifest syntax", corrupt_manifest),
            ("manifest collision", collide_manifest),
            ("output hash", damage_output),
            ("missing file", remove_output),
            ("unexpected file", add_output),
        )
        with tempfile.TemporaryDirectory(prefix="render skill fixtures ") as directory:
            root = Path(directory)
            for index, (name, corrupt) in enumerate(cases):
                with self.subTest(name=name):
                    fixture = self.make_fixture(root / f"case {index} with spaces")
                    entry = self.assert_success(self.run_renderer(fixture))
                    destination = entry.parent
                    expected = corrupt(destination)
                    damaged_evidence = _tree_bytes(destination)

                    self.assert_halt(self.run_renderer(fixture), expected)
                    self.assertEqual(damaged_evidence, _tree_bytes(destination))
                    self.assertEqual([], list(destination.parent.glob(".staging-*")))

    def test_ci_runs_this_suite_as_a_blocking_no_bytecode_gate(self) -> None:
        workflow = CI_PATH.read_text(encoding="utf-8")
        job_match = re.search(
            r"(?ms)^  workflow-gates:\r?\n.*?(?=^  [A-Za-z0-9_-]+:\r?\n|\Z)",
            workflow,
        )
        if job_match is None:
            self.fail("CI workflow does not define the workflow-gates job")
        workflow_gates = job_match.group(0)
        renderer_headers = list(
            re.finditer(
                r"(?m)^      - name: Validate shared skill renderer\r?$",
                workflow_gates,
            )
        )
        self.assertEqual(1, len(renderer_headers), renderer_headers)
        step_start = renderer_headers[0].start()
        next_step = re.search(r"(?m)^      - ", workflow_gates[step_start + 1 :])
        step_end = (
            step_start + 1 + next_step.start()
            if next_step is not None
            else len(workflow_gates)
        )
        renderer_step = workflow_gates[step_start:step_end]
        renderer_step = renderer_step.replace("\r\n", "\n").rstrip("\n")
        expected_step = """      - name: Validate shared skill renderer
        env:
          PYTHONDONTWRITEBYTECODE: '1'
        run: python3 -m unittest tests/tools/test_render_skill.py -v"""
        self.assertEqual(expected_step, renderer_step)
        self.assertNotIn("continue-on-error", workflow_gates[step_start:step_end])


if __name__ == "__main__":
    unittest.main()
