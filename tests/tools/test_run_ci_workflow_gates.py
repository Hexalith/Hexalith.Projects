"""Run the real workflow policy gate against isolated checkout mutations."""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
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

    def test_reviewed_prerequisite_jobs_reject_any_changed_execution(self) -> None:
        path = self.root / ".github/workflows/release.yml"
        original = path.read_text()
        for before, after in (
            ("--profile projects --report prerequisite-qualification.json", "--profile eventstore --report prerequisite-qualification.json"),
            ("--profile projects --report prerequisite-publication.json", "--profile eventstore --report prerequisite-publication.json"),
        ):
            with self.subTest(before=before):
                self.assertIn(before, original)
                path.write_text(original.replace(before, after))
                self.assert_rejected("differs from the exact owner-qualified publisher")
        path.write_text(original)

    def test_unrelated_release_job_remains_forbidden(self) -> None:
        self.replace(".github/workflows/release.yml", "jobs:\n", "jobs:\n  unrelated-publisher:\n    runs-on: ubuntu-latest\n    steps:\n      - run: echo forbidden\n")
        self.assert_rejected("additional jobs are forbidden")

    def test_root_execution_context_and_dispatch_default_cannot_drift(self) -> None:
        path = self.root / ".github/workflows/release.yml"
        original = path.read_text()
        for before, after in (
            ("default: full", "default: contracts-prerequisite"),
            ("jobs:\n", "defaults:\n  run:\n    shell: python\njobs:\n"),
            ("jobs:\n", "defaults:\n  run:\n    working-directory: does-not-exist\njobs:\n"),
        ):
            with self.subTest(after=after):
                self.assertIn(before, original)
                path.write_text(original.replace(before, after))
                self.assert_rejected("root context differs from the approved publication profiles")
        path.write_text(original)

    def test_approved_release_workflow_accepts_crlf(self) -> None:
        path = self.root / ".github/workflows/release.yml"
        original = path.read_text()
        path.write_bytes(original.replace("\r\n", "\n").replace("\n", "\r\n").encode())
        result = self.run_gate()
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_full_release_profile_guard_cannot_be_disabled(self) -> None:
        self.replace(".github/workflows/release.yml",
            "    if: ${{ inputs.release-profile != 'contracts-prerequisite' && inputs.release-profile != 'contracts-prerequisite-verify' }}",
            "    if: ${{ false }}")
        self.assert_rejected("full-release preflight may select only the normal full-release profile")

    def test_unpublished_apphost_helpers_require_the_exact_source_mode_condition(self) -> None:
        path = "src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj"
        target = self.root / path
        original = target.read_text()
        marker = "  <ItemGroup Condition=\"'$(UseHexalithProjectReferences)' == 'true'\">"
        self.assertEqual(1, original.count(marker))
        for condition in ("", " Condition=\"'$(UseHexalithProjectReferences)' != 'true'\""):
            with self.subTest(condition=condition):
                target.write_text(original.replace(marker, "  <ItemGroup" + condition + ">", 1))
                self.assert_rejected("unconditional sibling library ProjectReference")
        target.write_text(original)

    def test_named_package_fixture_executable_edges_are_accepted(self) -> None:
        result = self.run_gate()
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("PASSED", result.stdout)

    def test_package_fixture_exception_does_not_admit_library_references(self) -> None:
        path = "tests/Hexalith.Projects.PackageFixtures.AppHost/Hexalith.Projects.PackageFixtures.AppHost.csproj"
        self.replace(path, "</Project>", """
  <ItemGroup>
    <ProjectReference Include="$(HexalithEventStoreRoot)/src/Hexalith.EventStore.Abstractions/Hexalith.EventStore.Abstractions.csproj" />
  </ItemGroup>
</Project>""")
        self.assert_rejected("unconditional sibling library ProjectReference: $(HexalithEventStoreRoot)/src/Hexalith.EventStore.Abstractions/")

    def test_package_fixture_exception_requires_the_exact_consumer_path(self) -> None:
        original = self.root / "tests/Hexalith.Projects.PackageFixtures.AppHost/Hexalith.Projects.PackageFixtures.AppHost.csproj"
        other = self.root / "tests/another-consumer" / original.name
        other.parent.mkdir()
        shutil.copyfile(original, other)
        self.assert_rejected("tests/another-consumer/Hexalith.Projects.PackageFixtures.AppHost.csproj has an unconditional sibling library ProjectReference")

    def test_package_fixture_exception_requires_an_unconditional_executable_type(self) -> None:
        target = self.root / "tests/Hexalith.Projects.PackageFixtures.AppHost/Hexalith.Projects.PackageFixtures.AppHost.csproj"
        original = target.read_text()
        for output in (
            "<OutputType>Library</OutputType>",
            '<OutputType Condition="false">Exe</OutputType>',
            "<OutputType>Exe</OutputType><OutputType>Library</OutputType>",
        ):
            with self.subTest(output=output):
                target.write_text(original.replace("<OutputType>Exe</OutputType>", output, 1))
                self.assert_rejected("Hexalith.Projects.PackageFixtures.AppHost.csproj has an unconditional sibling library ProjectReference")
        target.write_text(original)

    def test_archived_project_copies_are_ignored_but_active_copies_remain_checked(self) -> None:
        archive = Path("_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/"
                       "isolation-20261103/final-source-20261103/raw-changes")
        projects = (
            (Path("src/Hexalith.Projects.Workers/Hexalith.Projects.Workers.csproj"),
             "has no package-mode Hexalith.EventStore.ServiceDefaults reference"),
            (Path("tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj"),
             "unconditional sibling library ProjectReference"),
        )
        for project, _ in projects:
            target = self.root / archive / project
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / archive / project, target)
        result = self.run_gate()
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("PASSED", result.stdout)
        for project, diagnostic in projects:
            with self.subTest(project=project):
                active = self.root / project
                original = active.read_bytes()
                shutil.copyfile(ROOT / archive / project, active)
                self.assert_rejected(diagnostic)
                active.write_bytes(original)




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









class FrontComposerInspectionTests(unittest.TestCase):
    """Execute the real helper on a fresh checkout with a controlled tool."""

    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory(prefix="frontcomposer fresh gate ")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.helper = self.root / "tests/tools/run-frontcomposer-inspect-gate.ps1"
        self.helper.parent.mkdir(parents=True)
        shutil.copyfile(ROOT / self.helper.relative_to(self.root), self.helper)
        self.contracts = self.root / "src/Hexalith.Projects.UI.Contracts"
        self.contracts.mkdir(parents=True)
        (self.contracts / "Projection.cs").write_text("[Projection]\nclass FixtureProjection {}\n")
        (self.contracts / "Hexalith.Projects.UI.Contracts.csproj").write_text("<Project />\n")
        self.record = self.root / "tool-call.json"
        self.bin = self.root / "tools"
        self.bin.mkdir()
        executable = self.bin / "dotnet"
        executable.write_text(f"#!{sys.executable}\n" + '''import json, os, sys
from pathlib import Path
args = sys.argv[1:]
if args[:2] == ["tool", "restore"]:
    print("restored local FrontComposer tool")
    raise SystemExit(0)
if args[:5] != ["tool", "run", "frontcomposer", "--", "inspect"]:
    print("Unexpected source-tool invocation", file=sys.stderr)
    raise SystemExit(91)
project = Path(args[args.index("--project") + 1])
generated = project.parent / "obj/Release/generated/HexalithFrontComposer/Fixture.g.cs"
record = {"args": args, "cwd": str(Path.cwd()), "generated_before": generated.exists()}
Path(os.environ["FRONTCOMPOSER_STUB_RECORD"]).write_text(json.dumps(record))
if ("--build" not in args or "--fail-on-warning" not in args
        or args[args.index("--configuration") + 1] != "Release"
        or args[args.index("--framework") + 1] != "net11.0"):
    print("Missing fresh Release build or warning failure control", file=sys.stderr)
    raise SystemExit(92)
mode = os.environ["FRONTCOMPOSER_STUB_MODE"]
print("retained Release build output")
if mode == "build-failure":
    print("retained compiler failure", file=sys.stderr)
    raise SystemExit(17)
generated.parent.mkdir(parents=True)
generated.write_text("// fresh generated output\\n")
if mode == "warning":
    print("warning FC0001: retained generator warning", file=sys.stderr)
    raise SystemExit(23)
print("retained successful inspection output")
''')
        executable.chmod(0o755)

    def run_helper(self, mode: str) -> subprocess.CompletedProcess[str]:
        environment = dict(os.environ, FRONTCOMPOSER_STUB_BIN=str(self.bin),
                           FRONTCOMPOSER_STUB_HELPER=str(self.helper),
                           FRONTCOMPOSER_STUB_RECORD=str(self.record), FRONTCOMPOSER_STUB_MODE=mode)
        # Some hosts launch pwsh through dotnet; replace PATH after that host starts.
        command = ("$env:PATH = $env:FRONTCOMPOSER_STUB_BIN + [IO.Path]::PathSeparator + $env:PATH\n"
                   "& $env:FRONTCOMPOSER_STUB_HELPER\n"
                   "$helperSucceeded = $?\n"
                   "if (-not $helperSucceeded) { if ($LASTEXITCODE) { exit $LASTEXITCODE }; exit 1 }\n"
                   "exit 0")
        return subprocess.run(
            ["pwsh", "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command],
            cwd=self.root, env=environment, capture_output=True, text=True, timeout=30,
        )

    def test_fresh_checkout_builds_and_inspects_release_with_the_packaged_tool(self) -> None:
        self.assertFalse((self.contracts / "obj").exists())
        result = self.run_helper("success")
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        call = json.loads(self.record.read_text())
        self.assertFalse(call["generated_before"])
        self.assertEqual(str(self.root), call["cwd"])
        self.assertEqual([
            "tool", "run", "frontcomposer", "--", "inspect", "--project",
            str(self.contracts / "Hexalith.Projects.UI.Contracts.csproj"),
            "--configuration", "Release", "--framework", "net11.0", "--build", "--fail-on-warning",
        ], call["args"])
        self.assertIn("retained successful inspection output", result.stdout)
        self.assertIn("PASSED", result.stdout)

    def test_failed_fresh_build_retains_diagnostics_and_exit_code(self) -> None:
        result = self.run_helper("build-failure")
        self.assertEqual(17, result.returncode, result.stdout + result.stderr)
        self.assertIn("retained Release build output", result.stdout)
        self.assertIn("retained compiler failure", result.stderr)
        self.assertNotIn("PASSED", result.stdout)

    def test_generated_warnings_remain_failures_with_diagnostics(self) -> None:
        result = self.run_helper("warning")
        self.assertEqual(23, result.returncode, result.stdout + result.stderr)
        self.assertIn("warning FC0001: retained generator warning", result.stderr)
        self.assertNotIn("PASSED", result.stdout)

    def test_missing_annotated_inputs_fail_before_tool_execution(self) -> None:
        (self.contracts / "Projection.cs").unlink()
        result = self.run_helper("success")
        self.assertNotEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("no [Projection]/[Command] files found", result.stderr)
        self.assertFalse(self.record.exists())


if __name__ == "__main__":
    unittest.main()
