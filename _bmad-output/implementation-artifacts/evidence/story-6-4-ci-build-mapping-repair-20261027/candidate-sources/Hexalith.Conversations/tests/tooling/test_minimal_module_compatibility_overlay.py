"""Verify the external compatibility graph and successor lock boundary."""
from pathlib import Path
import json
import os
import subprocess
import shutil
import tempfile
from xml.etree import ElementTree
import unittest

ROOT = Path(__file__).resolve().parents[2]
OVERLAY = ROOT / "tests/compatibility/MinimalModule/RegisteredCommand.targets"
FIXTURE = ROOT / "tests/fixtures/Hexalith.Conversations.MinimalModule"


class MinimalModuleCompatibilityOverlayTests(unittest.TestCase):
    @staticmethod
    def isolated_environment():
        return {name: value for name, value in os.environ.items() if name.lower() not in {
            "custombeforedirectorybuildprops", "customaftermicrosoftcommonprops",
            "custombeforedirectorybuildtargets", "customafterdirectorybuildtargets",
            "aftermicrosoftnetsdktargets", "hexalith_source_mapping_hash", "configuration"}}

    def catalog_versions(self):
        result = subprocess.run(
            ["dotnet", "msbuild", str(OVERLAY.parent / "CatalogVersion.proj"), "-nologo",
             "-t:ReadMinimalModuleCompatibilityCatalog", "-getItem:_MinimalModuleCompatibilityCatalogPackage"],
            cwd=ROOT, env=self.isolated_environment(), text=True, capture_output=True, check=True)
        packages = json.loads(result.stdout)["Items"]["_MinimalModuleCompatibilityCatalogPackage"]
        self.assertTrue(packages)
        return {package["Identity"].lower(): package["Version"] for package in packages}

    def test_actual_overlay_selects_packages_and_external_successor_locks(self):
        environment = self.isolated_environment()
        catalog = self.catalog_versions()
        for project in sorted(FIXTURE.rglob("*.csproj")):
            command = ["dotnet", "msbuild", str(project), "-nologo", "-t:MinimalModuleCompatibilitySelectPackages",
                       "-p:Configuration=Release", "-p:MinimalModuleCompatibilityMode=package",
                       "-p:MinimalModuleCompatibilityUseSuccessorLocks=true", f"-p:CustomAfterMicrosoftCommonTargets={OVERLAY}",
                       "-getProperty:NuGetLockFilePath,Configuration", "-getItem:ProjectReference,PackageReference"]
            result = subprocess.run(command, cwd=ROOT, env=environment, text=True, capture_output=True, check=True)
            output = json.loads(result.stdout)
            lock = OVERLAY.parent / "locks/package" / (project.stem + ".packages.lock.json")
            self.assertEqual(str(lock), output["Properties"]["NuGetLockFilePath"])
            self.assertEqual("Release", output["Properties"]["Configuration"])
            self.assertTrue(lock.is_file())
            for reference in output["Items"]["ProjectReference"]:
                self.assertNotIn("Hexalith.EventStore.", reference["Identity"])
            selected = [reference for reference in output["Items"]["PackageReference"] if reference["Identity"].startswith("Hexalith.EventStore.")]
            if not project.stem.endswith(".Tests"):
                self.assertTrue(selected)
            for reference in selected:
                self.assertEqual(catalog[reference["Identity"].lower()], reference["Version"])
            graph = json.loads(lock.read_text())["dependencies"]["net10.0"]
            for identity, package in graph.items():
                if identity.lower().startswith("hexalith.eventstore."):
                    self.assertNotEqual("Project", package["type"])
                    self.assertEqual(catalog[identity.lower()], package["resolved"])

    def test_compatibility_job_initializes_only_the_direct_catalog(self):
        workflow = (ROOT / ".github/workflows/ci.yml").read_text()
        job = workflow.split("\n  minimal-module-compatibility:\n", 1)[1].split("\n  testsupport-windows:\n", 1)[0]
        self.assertIn("git -c submodule.recurse=false submodule update --init --checkout -- references/Hexalith.Builds", job)
        self.assertNotIn("references/Hexalith.EventStore", job)
        self.assertIn("--locked-mode -p:Configuration=Release", job)
        self.assertIn("-p:MinimalModuleCompatibilityMode=package", job)
        self.assertIn("-automated sync -failSkips", job)
        self.assertIn("verify_results.py", job)

    def test_catalog_drift_refuses_before_using_successor_locks(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            overlay = root / "tests/compatibility/MinimalModule"
            overlay.mkdir(parents=True)
            for name in ("RegisteredCommand.targets", "CatalogVersion.proj"):
                shutil.copy2(OVERLAY.parent / name, overlay / name)
            catalog = ElementTree.parse(ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props")
            version = catalog.find(".//HexalithEventStoreVersion")
            self.assertIsNotNone(version)
            version.text = "0.0.0-review-drift"
            catalog_path = root / "references/Hexalith.Builds/Props/Directory.Packages.props"
            catalog_path.parent.mkdir(parents=True)
            catalog.write(catalog_path, encoding="utf-8")
            project = root / "Fixture.csproj"
            project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>')
            result = subprocess.run(
                ["dotnet", "msbuild", str(project), "-nologo", "-t:MinimalModuleCompatibilitySelectPackages",
                 "-p:Configuration=Release", "-p:MinimalModuleCompatibilityMode=package",
                 "-p:MinimalModuleCompatibilityUseSuccessorLocks=true",
                 f"-p:CustomAfterMicrosoftCommonTargets={overlay / 'RegisteredCommand.targets'}"],
                cwd=root, env=self.isolated_environment(), text=True, capture_output=True, check=False)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("differs from the direct pinned Builds catalog", result.stdout + result.stderr)



if __name__ == "__main__":
    unittest.main()
