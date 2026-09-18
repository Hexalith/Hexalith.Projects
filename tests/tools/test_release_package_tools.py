#!/usr/bin/env python3
"""Hermetic regression fixtures for the fail-closed release package tools."""

from __future__ import annotations

import importlib.util
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path
from typing import Callable
from unittest import mock


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = REPOSITORY_ROOT / "scripts"
sys.path.insert(0, str(SCRIPTS))

from _nuget import (  # noqa: E402
    ManifestPackage,
    expected_dependencies,
    load_manifest,
    normalize_internal_dependency_ranges,
    validate_packages,
)


VERSION = "9.8.7-ci.1"
SOURCE_SHA = "a" * 40
XmlMutation = Callable[[ET.Element], None]


def load_consumer_module():
    """Load the hyphenated consumer tool as a testable module."""

    spec = importlib.util.spec_from_file_location(
        "validate_consumers",
        SCRIPTS / "validate-consumer-package-references.py",
    )
    if spec is None or spec.loader is None:
        raise AssertionError("Cannot load consumer validation tool")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def create_nuspec(
    package: ManifestPackage,
    version: str,
    source_sha: str,
) -> ET.Element:
    """Create a minimal candidate matching the repository manifest."""

    manifest = load_manifest()
    root = ET.Element("package")
    metadata = ET.SubElement(root, "metadata")
    ET.SubElement(metadata, "id").text = package.package_id
    ET.SubElement(metadata, "version").text = version
    ET.SubElement(metadata, "authors").text = manifest.metadata.authors
    ET.SubElement(metadata, "license", {"type": "expression"}).text = manifest.metadata.license
    ET.SubElement(metadata, "readme").text = manifest.metadata.readme
    ET.SubElement(metadata, "projectUrl").text = manifest.metadata.project_url
    ET.SubElement(
        metadata,
        "repository",
        {
            "type": manifest.metadata.repository_type,
            "url": manifest.metadata.repository_url,
            "commit": source_sha,
        },
    )
    ET.SubElement(metadata, "description").text = manifest.metadata.description
    ET.SubElement(metadata, "tags").text = " ".join(manifest.metadata.tags)
    dependencies = ET.SubElement(metadata, "dependencies")
    dependency_group = ET.SubElement(
        dependencies,
        "group",
        {"targetFramework": manifest.metadata.target_framework},
    )
    for dependency_id, dependency_version in expected_dependencies(package, version).items():
        ET.SubElement(
            dependency_group,
            "dependency",
            {
                "id": dependency_id,
                "version": dependency_version,
                "exclude": "Build,Analyzers",
            },
        )
    if package.framework_references:
        references = ET.SubElement(metadata, "frameworkReferences")
        reference_group = ET.SubElement(
            references,
            "group",
            {"targetFramework": manifest.metadata.target_framework},
        )
        for reference in package.framework_references:
            ET.SubElement(reference_group, "frameworkReference", {"name": reference})
    return root


def write_candidates(
    directory: Path,
    version: str = VERSION,
    source_sha: str = SOURCE_SHA,
) -> None:
    """Write the exact five lightweight candidate archives."""

    manifest = load_manifest()
    for package in manifest.packages:
        path = directory / f"{package.package_id}.{version}.nupkg"
        with zipfile.ZipFile(path, "w") as archive:
            archive.writestr(
                f"{package.package_id}.nuspec",
                ET.tostring(create_nuspec(package, version, source_sha), encoding="utf-8"),
            )
            archive.writestr(
                f"lib/{manifest.metadata.target_framework}/{package.package_id}.dll",
                b"MZfixture-assembly",
            )
            archive.writestr(manifest.metadata.readme, b"fixture readme")


def rewrite_candidate(
    path: Path,
    mutation: XmlMutation | None = None,
    *,
    raw_nuspec: bytes | None = None,
    remove_asset: bool = False,
    extra_entry: tuple[str, bytes] | None = None,
) -> None:
    """Rewrite one archive to model a hostile or drifting candidate."""

    with zipfile.ZipFile(path, "r") as source:
        payloads = {
            entry.filename: source.read(entry)
            for entry in source.infolist()
            if not (remove_asset and entry.filename.casefold().endswith(".dll"))
        }
    nuspec_name = next(name for name in payloads if name.casefold().endswith(".nuspec"))
    if raw_nuspec is not None:
        payloads[nuspec_name] = raw_nuspec
    elif mutation is not None:
        root = ET.fromstring(payloads[nuspec_name])
        mutation(root)
        payloads[nuspec_name] = ET.tostring(root, encoding="utf-8")
    if extra_entry is not None:
        payloads[extra_entry[0]] = extra_entry[1]
    with zipfile.ZipFile(path, "w") as destination:
        for name, payload in payloads.items():
            destination.writestr(name, payload)


def metadata(root: ET.Element) -> ET.Element:
    """Return the fixture metadata element."""

    return root.find("metadata")  # type: ignore[return-value]


class ReleasePackageToolTests(unittest.TestCase):
    """Exercise positive and negative validation paths without network access."""

    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory(prefix="projects-package-fixtures-")
        self.directory = Path(self.temporary.name)
        write_candidates(self.directory)

    def tearDown(self) -> None:
        self.temporary.cleanup()

    def candidate(self, package_id: str, version: str = VERSION) -> Path:
        return self.directory / f"{package_id}.{version}.nupkg"

    def assert_invalid(self, expected: str) -> None:
        with self.assertRaisesRegex(ValueError, expected):
            validate_packages(self.directory, VERSION, SOURCE_SHA)

    def test_exact_candidates_pass(self) -> None:
        packages, version, source = validate_packages(self.directory, VERSION, SOURCE_SHA)
        self.assertEqual(5, len(packages))
        self.assertEqual(VERSION, version)
        self.assertEqual(SOURCE_SHA, source)

    def test_external_dependency_may_equal_candidate_version(self) -> None:
        for candidate in self.directory.glob("*.nupkg"):
            candidate.unlink()
        write_candidates(self.directory, version="1.0.0")
        packages, version, _ = validate_packages(self.directory, "1.0.0", SOURCE_SHA)
        contracts = next(item for item in packages if item.package_id.endswith("Contracts"))
        self.assertEqual("1.0.0", contracts.dependencies["Hexalith.Conversations.Contracts"])
        self.assertEqual("1.0.0", version)

    def test_missing_and_stale_packages_fail(self) -> None:
        self.candidate("Hexalith.Projects.Client").unlink()
        self.assert_invalid("Expected exactly 5 release packages")
        write_candidates(self.directory)
        (self.directory / "Stale.1.0.0.nupkg").write_bytes(b"not a package")
        self.assert_invalid("Expected exactly 5 release packages")

    def test_metadata_and_provenance_drift_fail(self) -> None:
        path = self.candidate("Hexalith.Projects")
        rewrite_candidate(path, lambda root: setattr(metadata(root).find("authors"), "text", "Other"))
        self.assert_invalid("has authors")
        write_candidates(self.directory)
        rewrite_candidate(
            path,
            lambda root: metadata(root).find("repository").set("commit", "b" * 40),
        )
        self.assert_invalid("source commit")
        with self.assertRaisesRegex(ValueError, "Expected source SHA is invalid"):
            validate_packages(self.directory, VERSION, "")

    def test_unsafe_archive_entry_fails(self) -> None:
        rewrite_candidate(
            self.candidate("Hexalith.Projects.Client"),
            extra_entry=("../escape.txt", b"escape"),
        )
        self.assert_invalid("unsafe archive entry")
        write_candidates(self.directory)
        rewrite_candidate(
            self.candidate("Hexalith.Projects.Client"),
            extra_entry=("buildTransitive/Exploit.targets", b"<Project />"),
        )
        self.assert_invalid("forbidden active payload root")

    def test_empty_archive_fails(self) -> None:
        self.candidate("Hexalith.Projects.Client").write_bytes(b"")
        self.assert_invalid("not a valid ZIP archive")

    def test_malformed_and_unsafe_xml_fail(self) -> None:
        path = self.candidate("Hexalith.Projects.Contracts")
        rewrite_candidate(path, raw_nuspec=b"<package><metadata>")
        self.assert_invalid("malformed nuspec XML")
        write_candidates(self.directory)
        rewrite_candidate(
            path,
            raw_nuspec=b'<!DOCTYPE package [<!ENTITY x "boom">]><package />',
        )
        with self.assertRaisesRegex(ValueError, "unsafe XML declarations"):
            normalize_internal_dependency_ranges(self.directory, VERSION)
        self.assert_invalid("unsafe XML declarations")

    def test_case_insensitive_duplicate_dependency_fails(self) -> None:
        def duplicate(root: ET.Element) -> None:
            group = metadata(root).find("dependencies/group")
            ET.SubElement(
                group,  # type: ignore[arg-type]
                "dependency",
                {"id": "fluxor", "version": "6.11.0", "exclude": "Build,Analyzers"},
            )

        rewrite_candidate(self.candidate("Hexalith.Projects.Contracts"), duplicate)
        self.assert_invalid("repeats dependency")

    def test_unexpected_dependency_attributes_fail(self) -> None:
        def add_attribute(root: ET.Element) -> None:
            dependency = next(metadata(root).iter("dependency"))
            dependency.set("include", "Runtime")

        rewrite_candidate(self.candidate("Hexalith.Projects.Contracts"), add_attribute)
        self.assert_invalid("dependency contains unexpected attributes")

    def test_unexpected_dependency_and_wrong_internal_range_fail(self) -> None:
        def unexpected(root: ET.Element) -> None:
            group = metadata(root).find("dependencies/group")
            ET.SubElement(
                group,  # type: ignore[arg-type]
                "dependency",
                {"id": "Unexpected.Package", "version": "1.2.3", "exclude": "Build,Analyzers"},
            )

        path = self.candidate("Hexalith.Projects")
        rewrite_candidate(path, unexpected)
        self.assert_invalid("unexpected=.*Unexpected.Package")
        write_candidates(self.directory)

        def weaken_internal(root: ET.Element) -> None:
            dependency = next(
                item
                for item in metadata(root).iter("dependency")
                if item.get("id") == "Hexalith.Projects.Contracts"
            )
            dependency.set("version", VERSION)

        rewrite_candidate(path, weaken_internal)
        self.assert_invalid("wrong_versions=.*Hexalith.Projects.Contracts")

    def test_missing_compile_asset_fails(self) -> None:
        rewrite_candidate(
            self.candidate("Hexalith.Projects.ServiceDefaults"),
            remove_asset=True,
        )
        self.assert_invalid("compile assets differ")

    def test_normalizer_makes_internal_ranges_exact(self) -> None:
        path = self.candidate("Hexalith.Projects.Testing")

        def weaken_internal(root: ET.Element) -> None:
            for dependency in metadata(root).iter("dependency"):
                if dependency.get("id", "").startswith("Hexalith.Projects"):
                    dependency.set("version", VERSION)

        rewrite_candidate(path, weaken_internal)
        normalize_internal_dependency_ranges(self.directory, VERSION)
        validate_packages(self.directory, VERSION, SOURCE_SHA)

    def test_manifest_probes_are_package_specific_real_api_compilations(self) -> None:
        manifest = load_manifest()
        expected_probes = {
            "Hexalith.Projects.Contracts": (
                "using Hexalith.Projects.Contracts.Identifiers;\n"
                'Console.WriteLine(new ProjectId("01HZ9K8YQ3W6V2N4R7T5P0X1AB").Value);\n'
            ),
            "Hexalith.Projects": (
                "using Hexalith.Projects;\nConsole.WriteLine(ProjectsModule.Name);\n"
            ),
            "Hexalith.Projects.Client": (
                "using Hexalith.Projects.Client;\n"
                "Console.WriteLine(ProjectsClientOptions.DefaultConfigurationSectionName);\n"
            ),
            "Hexalith.Projects.Testing": (
                "using Hexalith.Projects.Testing;\n"
                "Console.WriteLine(ProjectsTestingMarker.Name);\n"
            ),
            "Hexalith.Projects.ServiceDefaults": (
                "using Hexalith.Projects.ServiceDefaults;\n"
                "Console.WriteLine(ProjectsServiceDefaults.Name);\n"
            ),
        }
        for package in manifest.packages:
            self.assertEqual(expected_probes[package.package_id], package.probe)

        module = load_consumer_module()
        for package in manifest.packages:
            rendered = module.render_consumer_project(package, VERSION)
            self.assertIn(f'Version="[{VERSION}]"', rendered)
            self.assertIn(f'Include="{package.package_id}"', rendered)
            self.assertNotIn("ProjectReference", rendered)

    def test_consumer_assets_reject_project_fallback_and_mixed_candidate_versions(self) -> None:
        module = load_consumer_module()
        assets_path = self.directory / "project.assets.json"
        assets_path.write_text(
            json.dumps(
                {
                    "libraries": {
                        f"Hexalith.Projects/{VERSION}": {"type": "package"},
                        f"Hexalith.Projects.Contracts/{VERSION}": {"type": "project"},
                    }
                }
            ),
            encoding="utf-8",
        )
        with self.assertRaisesRegex(ValueError, "restored project inputs"):
            module.assert_package_only_assets(assets_path, "Hexalith.Projects", VERSION)

        assets_path.write_text(
            json.dumps(
                {
                    "libraries": {
                        f"Hexalith.Projects/{VERSION}": {"type": "package"},
                        "Hexalith.Projects.Contracts/1.0.0": {"type": "package"},
                    }
                }
            ),
            encoding="utf-8",
        )
        with self.assertRaisesRegex(ValueError, "mismatched Projects candidates"):
            module.assert_package_only_assets(assets_path, "Hexalith.Projects", VERSION)

    def test_consumer_restore_failure_propagates(self) -> None:
        module = load_consumer_module()
        package = load_manifest().packages[0]
        workspace = self.directory / "consumer-workspace"
        workspace.mkdir()
        restore_failure = subprocess.CalledProcessError(1, ["dotnet", "restore"])

        with mock.patch.object(module.subprocess, "run", side_effect=restore_failure) as run:
            with self.assertRaises(subprocess.CalledProcessError):
                module.run_consumer(package, VERSION, self.directory, workspace)

        run.assert_called_once()
        command = run.call_args.args[0]
        self.assertEqual(["dotnet", "restore"], command[:2])
        self.assertIn("--configfile", command)

    def test_consumer_success_invokes_restore_and_build(self) -> None:
        module = load_consumer_module()
        package = load_manifest().packages[0]
        workspace = self.directory / "consumer-workspace"
        workspace.mkdir()

        with mock.patch.object(module, "assert_package_only_assets") as assets:
            with mock.patch.object(module.subprocess, "run") as run:
                module.run_consumer(package, VERSION, self.directory, workspace)

        self.assertEqual(2, run.call_count)
        restore_command = run.call_args_list[0].args[0]
        build_command = run.call_args_list[1].args[0]
        self.assertEqual(["dotnet", "restore"], restore_command[:2])
        self.assertEqual(["dotnet", "build"], build_command[:2])
        self.assertIn("--no-restore", build_command)
        self.assertIn("-warnaserror", build_command)
        assets.assert_called_once()

    def test_consumer_build_failure_propagates(self) -> None:
        module = load_consumer_module()
        package = load_manifest().packages[0]
        workspace = self.directory / "consumer-workspace"
        workspace.mkdir()
        build_failure = subprocess.CalledProcessError(1, ["dotnet", "build"])

        with mock.patch.object(module, "assert_package_only_assets"):
            with mock.patch.object(
                module.subprocess,
                "run",
                side_effect=[None, build_failure],
            ) as run:
                with self.assertRaises(subprocess.CalledProcessError):
                    module.run_consumer(package, VERSION, self.directory, workspace)

        self.assertEqual(2, run.call_count)
        self.assertEqual(["dotnet", "build"], run.call_args_list[1].args[0][:2])

    def test_release_wrapper_sequences_and_propagates_delegated_failures(self) -> None:
        binary_directory = self.directory / "bin"
        binary_directory.mkdir()
        command_log = self.directory / "commands.log"
        stub = """#!/bin/sh
printf '%s|%s\n' "$(basename "$0")" "$*" >> "$COMMAND_LOG"
if [ -n "${FAIL_MATCH:-}" ]; then
  case "$*" in
    *"$FAIL_MATCH"*) exit 23 ;;
  esac
fi
exit 0
"""
        for command in ("dotnet", "python3"):
            path = binary_directory / command
            path.write_text(stub, encoding="utf-8")
            path.chmod(0o755)

        environment = dict(os.environ)
        environment["PATH"] = f"{binary_directory}{os.pathsep}{environment['PATH']}"
        environment["COMMAND_LOG"] = str(command_log)
        wrapper = REPOSITORY_ROOT / "tests" / "tools" / "run-package-dependency-gate.ps1"
        dotnet_host = shutil.which("dotnet")
        self.assertIsNotNone(dotnet_host)
        pwsh_assembly = subprocess.run(
            [
                "pwsh",
                "-NoProfile",
                "-Command",
                "[System.Reflection.Assembly]::GetEntryAssembly().Location",
            ],
            capture_output=True,
            text=True,
            check=True,
        ).stdout.strip()
        arguments = [
            dotnet_host,
            pwsh_assembly,
            "-NoProfile",
            "-File",
            str(wrapper),
            "-Version",
            VERSION,
            "-PackageDirectory",
            str(self.directory),
        ]

        non_skip = subprocess.run(
            arguments,
            cwd=REPOSITORY_ROOT,
            env=environment,
            capture_output=True,
            text=True,
            check=False,
        )
        self.assertEqual(0, non_skip.returncode, non_skip.stderr)
        non_skip_commands = command_log.read_text(encoding="utf-8").splitlines()
        self.assertEqual(
            5,
            len(non_skip_commands),
            f"commands={non_skip_commands!r}\nstdout={non_skip.stdout}\nstderr={non_skip.stderr}",
        )
        self.assertTrue(non_skip_commands[0].startswith("dotnet|restore "))
        self.assertTrue(non_skip_commands[1].startswith("dotnet|build "))
        self.assertIn("--configuration Release", non_skip_commands[1])
        self.assertIn("-p:UseHexalithProjectReferences=false", non_skip_commands[0])
        self.assertIn("pack-release-packages.py", non_skip_commands[2])
        self.assertNotIn("--normalize-internal-ranges", "\n".join(non_skip_commands))

        command_log.write_text("", encoding="utf-8")
        skip = subprocess.run(
            [*arguments, "-SkipPack"],
            cwd=REPOSITORY_ROOT,
            env=environment,
            capture_output=True,
            text=True,
            check=False,
        )
        self.assertEqual(0, skip.returncode, skip.stderr)
        skip_commands = command_log.read_text(encoding="utf-8").splitlines()
        self.assertEqual(2, len(skip_commands))
        self.assertIn("validate-nuget-packages.py", skip_commands[0])
        self.assertIn("--normalize-internal-ranges", skip_commands[0])
        self.assertIn("validate-consumer-package-references.py", skip_commands[1])

        command_log.write_text("", encoding="utf-8")
        failing_environment = dict(environment)
        failing_environment["FAIL_MATCH"] = "validate-nuget-packages.py"
        failed = subprocess.run(
            [*arguments, "-SkipPack"],
            cwd=REPOSITORY_ROOT,
            env=failing_environment,
            capture_output=True,
            text=True,
            check=False,
        )
        self.assertNotEqual(0, failed.returncode)
        failed_commands = command_log.read_text(encoding="utf-8").splitlines()
        self.assertEqual(1, len(failed_commands))
        self.assertIn("validate-nuget-packages.py", failed_commands[0])


if __name__ == "__main__":
    unittest.main()
