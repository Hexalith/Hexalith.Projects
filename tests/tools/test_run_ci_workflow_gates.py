"""Run the real workflow policy gate against isolated checkout mutations."""

from __future__ import annotations

import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
GATE = Path("tests/tools/run-ci-workflow-gates.ps1")
VERSION_PROPERTIES = ("HexalithConversationsVersion", "HexalithFoldersVersion")
PACKAGE_IDS = (
    "Hexalith.Conversations.Client", "Hexalith.Conversations.Contracts",
    "Hexalith.Folders.Client", "Hexalith.Folders.Contracts",
)


class WorkflowGateTests(unittest.TestCase):
    """Require success on the checkout and rejection of each controlled drift."""

    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory(prefix="projects-workflow-gate-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        inputs = (
            GATE, Path("Hexalith.Projects.CI.slnx"), Path("release.config.cjs"),
            Path("Directory.Build.props"), Path("Directory.Packages.props"),
            Path("tools/release-packages.json"),
            Path("docs/runbooks/nuget-trusted-publishing.md"),
            Path("tests/tools/run-frontcomposer-inspect-gate.ps1"),
            Path("tests/tools/run-openapi-fingerprint-gate.ps1"),
            Path("tests/e2e/run-live-apphost.sh"),
            Path("tests/e2e/playwright.config.ts"),
            Path("tests/e2e/reporters/zero-live-skip-reporter.ts"),
        )
        projects = [path for directory in ("src", "tests")
                    for path in (ROOT / directory).rglob("*.csproj")]
        paths = [ROOT / path for path in inputs]
        paths.extend((ROOT / ".github/workflows").glob("*.y*ml"))
        paths.extend(projects)
        for source in paths:
            target = self.root / source.relative_to(ROOT)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, target)

    def replace(self, path: str, before: str, after: str) -> None:
        target = self.root / path
        text = target.read_text(encoding="utf-8")
        self.assertIn(before, text)
        target.write_text(text.replace(before, after), encoding="utf-8")

    def run_gate(self, *, environment: dict[str, str] | None = None) -> subprocess.CompletedProcess[str]:
        clean_environment = {key: value for key, value in os.environ.items()
                             if key.casefold() not in {name.casefold() for name in VERSION_PROPERTIES}}
        clean_environment.update(environment or {})
        return subprocess.run(
            ["pwsh", "-NoLogo", "-NoProfile", "-NonInteractive", "-File", str(self.root / GATE)],
            cwd=self.root, env=clean_environment, capture_output=True, text=True, timeout=30,
        )

    def assert_rejected(self, diagnostic: str, **kwargs) -> None:
        result = self.run_gate(**kwargs)
        self.assertEqual(1, result.returncode, result.stdout + result.stderr)
        self.assertIn(diagnostic, result.stderr)
        self.assertNotIn("PASSED", result.stdout)

    def test_checkout_passes_without_submodules(self) -> None:
        self.assertFalse((self.root / "references").exists())
        result = self.run_gate()
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("PASSED", result.stdout)

    def test_catalog_package_versions_cannot_be_redeclared(self) -> None:
        for operation in ("Include", "Update", "Remove"):
            for package_id in PACKAGE_IDS:
                with self.subTest(operation=operation, package_id=package_id):
                    path = self.root / "Directory.Packages.props"
                    original = path.read_text()
                    entry = f'<ItemGroup><PackageVersion {operation}="{package_id}" Version="9.9.9" /></ItemGroup>'
                    path.write_text(original.replace("</Project>", entry + "</Project>"))
                    self.assert_rejected(f"{package_id} version must be owned")
                    path.write_text(original)

    def test_case_variant_semicolon_and_choose_entries_are_rejected(self) -> None:
        self.replace("Directory.Packages.props", "</Project>", """
  <Choose><When Condition="'true' == 'true'"><ItemGroup>
    <PackageVersion Update="Unrelated; hexalith.folders.client" Version="9.9.9" />
  </ItemGroup></When></Choose>
</Project>""")
        self.assert_rejected("Hexalith.Folders.Client version must be owned")

    def test_catalog_import_must_be_present_unique_and_root_owned(self) -> None:
        path = self.root / "Directory.Packages.props"
        original = path.read_text()
        import_line = next(line for line in original.splitlines()
                           if '<Import Project="$(Hexalith1BuildPackageProps)"' in line)
        mutations = (
            original.replace(import_line, ""),
            original.replace(import_line, import_line + "\n" + import_line),
            original.replace("references/Hexalith.Builds/Props/", "elsewhere/"),
            original.replace("</Project>", "<PropertyGroup><Hexalith1BuildPackageProps>other</Hexalith1BuildPackageProps></PropertyGroup></Project>"),
        )
        for index, text in enumerate(mutations):
            with self.subTest(index=index):
                path.write_text(text)
                self.assert_rejected("must import the root-declared Hexalith.Builds package catalog")

    def test_harmless_comments_and_unrelated_properties_and_packages_pass(self) -> None:
        self.replace("Directory.Packages.props", "</Project>", """
  <!-- <PropertyGroup><HexalithFoldersVersion>9.9.9</HexalithFoldersVersion></PropertyGroup> -->
  <!-- <ItemGroup><PackageVersion Include="Hexalith.Folders.Client" Version="9.9.9" /></ItemGroup> -->
  <PropertyGroup><UnrelatedVersion>1.0.0</UnrelatedVersion></PropertyGroup>
  <ItemGroup><PackageVersion Include="Unrelated" Version="1.0.0" /></ItemGroup>
</Project>""")
        self.replace(".github/workflows/ci.yml", "name: CI", "name: CI\n# HexalithFoldersVersion: 9.9.9")
        result = self.run_gate(environment={"HexalithFoldersVersion": ""})
        self.assertEqual(0, result.returncode, result.stderr)

    def test_root_props_cannot_override_version_set_properties(self) -> None:
        for filename in ("Directory.Build.props", "Directory.Packages.props"):
            for property_name in VERSION_PROPERTIES:
                with self.subTest(filename=filename, property_name=property_name):
                    path = self.root / filename
                    original = path.read_text()
                    name = property_name.lower()
                    entry = f'<Choose><When Condition="false"><PropertyGroup><{name}>9.9.9</{name}></PropertyGroup></When></Choose>'
                    path.write_text(original.replace("</Project>", entry + "</Project>"))
                    self.assert_rejected(f"{property_name} must remain owned by the Hexalith.Builds package catalog")
                    path.write_text(original)

    def test_workflow_environment_and_msbuild_switch_overrides_are_rejected(self) -> None:
        path = self.root / ".github/workflows/ci.yml"
        original = path.read_text()
        for property_name in VERSION_PROPERTIES:
            for assignment in (f"  {property_name}: '9.9.9'", f'  "{property_name}": 9.9.9',
                               f"  '{property_name}': 9.9.9", f'  ARGS: -p:{property_name.lower()}=9.9.9'):
                with self.subTest(assignment=assignment):
                    path.write_text(original.replace("name: CI", "name: CI\nenv:\n" + assignment))
                    self.assert_rejected(f"{property_name} must remain owned by the Hexalith.Builds package catalog")

    def test_inherited_version_set_overrides_are_rejected_case_insensitively(self) -> None:
        for property_name in VERSION_PROPERTIES:
            with self.subTest(property_name=property_name):
                self.assert_rejected(
                    f"{property_name} must remain owned by the Hexalith.Builds package catalog",
                    environment={property_name.lower(): "9.9.9"},
                )

    def test_namespaced_msbuild_properties_cannot_override_catalog(self) -> None:
        self.replace("Directory.Build.props", "<Project>", '<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">')
        self.replace("Directory.Build.props", "</Project>", """
  <PropertyGroup><HexalithFoldersVersion>9.9.9</HexalithFoldersVersion></PropertyGroup>
</Project>""")
        self.assert_rejected("HexalithFoldersVersion must remain owned by the Hexalith.Builds package catalog")

    def test_shell_environment_writes_cannot_override_catalog(self) -> None:
        path = self.root / ".github/workflows/probe.yml"
        for script in (
            "export HexalithFoldersVersion=9.9.9",
            "env HexalithFoldersVersion=9.9.9 dotnet restore",
            "$env:HexalithFoldersVersion = '9.9.9'",
            "Set-Item Env:HexalithFoldersVersion 9.9.9",
            "[Environment]::SetEnvironmentVariable('HexalithFoldersVersion', '9.9.9')",
            "echo 'HexalithFoldersVersion=9.9.9' >> \"$GITHUB_ENV\"",
            "echo 'HexalithFoldersVersion<<EOF' >> \"$GITHUB_ENV\"\necho 9.9.9 >> \"$GITHUB_ENV\"\necho EOF >> \"$GITHUB_ENV\"",
        ):
            with self.subTest(script=script):
                path.write_text("name: Probe\non: workflow_dispatch\njobs:\n  probe:\n    runs-on: ubuntu-latest\n"
                                "    steps:\n      - run: |\n" + "\n".join("          " + line for line in script.splitlines()) + "\n")
                self.assert_rejected("HexalithFoldersVersion must remain owned by the Hexalith.Builds package catalog")

    def test_inline_comment_does_not_assign_property(self) -> None:
        self.replace(".github/workflows/ci.yml", "name: CI", "name: CI # HexalithFoldersVersion=9.9.9")
        result = self.run_gate()
        self.assertEqual(0, result.returncode, result.stderr)

    def test_echoed_assignment_does_not_override_environment(self) -> None:
        (self.root / ".github/workflows/probe.yml").write_text(
            "name: Probe\non: workflow_dispatch\njobs:\n  probe:\n    runs-on: ubuntu-latest\n"
            '    steps:\n      - run: echo "HexalithFoldersVersion=9.9.9"\n')
        result = self.run_gate()
        self.assertEqual(0, result.returncode, result.stderr)

    def test_all_collected_failures_are_reported(self) -> None:
        entries = "".join(f'<PackageVersion Include="{name}" Version="9.9.9" />' for name in PACKAGE_IDS)
        self.replace("Directory.Packages.props", "</Project>", f"<ItemGroup>{entries}</ItemGroup></Project>")
        result = self.run_gate()
        self.assertEqual(1, result.returncode, result.stderr)
        for package_id in PACKAGE_IDS:
            self.assertIn(f"{package_id} version must be owned", result.stderr)

    def test_mismatched_direct_builds_sha_names_its_scope(self) -> None:
        self.replace(".github/workflows/ci.yml", "domain-ci.yml@51af786cf156d2a3396dbd49f5e4898222e55c12",
                     "domain-ci.yml@" + "a" * 40)
        self.assert_rejected("Direct ci.yml reference executes Hexalith.Builds")

    def test_regression_fixture_step_cannot_be_removed_or_made_nonblocking(self) -> None:
        path = self.root / ".github/workflows/ci.yml"
        original = path.read_text()
        step = """      - name: Validate workflow policy regression fixtures
        env:
          PYTHONDONTWRITEBYTECODE: '1'
        run: python3 -m unittest tests/tools/test_run_ci_workflow_gates.py -v
"""
        self.assertIn(step, original)
        for replacement, diagnostic in (
            ("", "must contain exactly one 'Validate workflow policy regression fixtures' step"),
            (step + step, "must contain exactly one 'Validate workflow policy regression fixtures' step"),
            (step.replace("        env:", "        if: false\n        env:"), "must be an exact blocking"),
            (step.replace("        env:", "        continue-on-error: true\n        env:"), "must be an exact blocking"),
        ):
            with self.subTest(replacement=replacement):
                path.write_text(original.replace(step, replacement))
                self.assert_rejected(diagnostic)

    def test_existing_workflow_safety_invariants_reject_mutations(self) -> None:
        path = self.root / ".github/workflows/ci.yml"
        original = path.read_text()
        for before, after, diagnostic in (
            ("submodules: false", "submodules: recursive", "Recursive or implicit recursive submodule checkout is forbidden"),
            ("solution: Hexalith.Projects.CI.slnx", "solution: Hexalith.Projects.CI.slnx -p:Unsafe=true", "must be the bare 'Hexalith.Projects.CI.slnx'"),
            ("npm --prefix tests/e2e ci --ignore-scripts", "npm --prefix tests/e2e install", "Workflow dependency installation must not use npm install"),
        ):
            with self.subTest(after=after):
                self.assertIn(before, original)
                path.write_text(original.replace(before, after))
                self.assert_rejected(diagnostic)


if __name__ == "__main__":
    unittest.main()
