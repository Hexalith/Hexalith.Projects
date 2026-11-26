#!/usr/bin/env python3
"""Hermetic regression fixtures for the fail-closed release package tools."""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
import urllib.error
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path
from typing import Callable
from unittest import mock


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = REPOSITORY_ROOT / "scripts"
sys.path.insert(0, str(SCRIPTS))

import _nuget
import _pack_release_packages as packer  # noqa: E402
from _nuget import (  # noqa: E402
    EXTENSION_PACKAGE_ID,
    MODULE_MANIFEST,
    PACKAGED_MODULE_MANIFEST,
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
        SCRIPTS / "_consumer_packages.py",
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
    """Write the exact lightweight candidate archives."""

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
            if package.package_id == EXTENSION_PACKAGE_ID:
                archive.writestr(PACKAGED_MODULE_MANIFEST, MODULE_MANIFEST.read_bytes())


def rewrite_candidate(
    path: Path,
    mutation: XmlMutation | None = None,
    *,
    raw_nuspec: bytes | None = None,
    remove_asset: bool = False,
    remove_entry: str | None = None,
    extra_entry: tuple[str, bytes] | None = None,
) -> None:
    """Rewrite one archive to model a hostile or drifting candidate."""

    with zipfile.ZipFile(path, "r") as source:
        payloads = {
            entry.filename: source.read(entry)
            for entry in source.infolist()
            if entry.filename != remove_entry and not (remove_asset and entry.filename.casefold().endswith(".dll"))
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


class ReleaseAvailabilityTests(unittest.TestCase):
    """Check the real publication gate with NuGet index response fixtures."""

    def setUp(self) -> None:
        spec = importlib.util.spec_from_file_location("availability", SCRIPTS / "check-release-version-available.py")
        self.assertIsNotNone(spec)
        self.assertIsNotNone(spec.loader)
        self.gate = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.gate)
        self.ids = {package.package_id.lower() for package in load_manifest().packages}
        self.observed: set[str] = set()

    def require(self, overrides: dict[str, bytes | int]) -> None:
        def respond(request, *, timeout):
            self.assertEqual(20, timeout)
            root, package, name = request.full_url.rsplit("/", 2)
            self.assertEqual(self.gate.PACKAGE_BASE_ADDRESS, root)
            self.assertEqual("index.json", name)
            self.observed.add(package)
            result = overrides.get(package, b'{"versions":["1.2.3"]}')
            if isinstance(result, int):
                raise urllib.error.HTTPError(request.full_url, result, "fixture error", {}, None)
            response = mock.MagicMock()
            response.__enter__.return_value = response
            response.status = 200
            response.geturl.return_value = request.full_url
            response.read.return_value = result
            return response

        with mock.patch.object(self.gate.urllib.request, "urlopen", side_effect=respond):
            self.gate.require_available(VERSION)

    def test_absence_in_all_indexes_or_an_unknown_id_allows_release(self) -> None:
        self.require({"hexalith.projects": 404})
        self.assertEqual(self.ids, self.observed)

    def test_one_occupied_coordinate_blocks_the_whole_release(self) -> None:
        with self.assertRaisesRegex(ValueError, r"already occupied for: Hexalith.Projects.Client"):
            self.require({"hexalith.projects.client": json.dumps({"versions": [VERSION.upper()]}).encode()})
        self.assertEqual(self.ids, self.observed)

    def test_malformed_index_is_not_evidence_of_absence(self) -> None:
        for body in (b"{", b"{}", b'{"versions":[]}', b'{"versions":[null]}', b'{"versions":["bogus"]}'):
            with self.subTest(body=body), self.assertRaises(ValueError):
                self.require({"hexalith.projects": body})

    def test_transport_failure_is_not_evidence_of_absence(self) -> None:
        for status in (401, 403, 429, 500):
            with self.subTest(status=status), self.assertRaises(urllib.error.HTTPError):
                self.require({"hexalith.projects": status})


class ReleasePublicationTests(unittest.TestCase):
    """Verify public identities and packages without using real credentials or registries."""

    def setUp(self) -> None:
        spec = importlib.util.spec_from_file_location("verify_publication", SCRIPTS / "verify-release-publication.py")
        self.assertIsNotNone(spec)
        self.assertIsNotNone(spec.loader)
        self.verifier = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.verifier)
        temporary = tempfile.TemporaryDirectory(prefix="projects-publication-fixtures-")
        self.addCleanup(temporary.cleanup)
        self.directory = Path(temporary.name)
        self.fixtures = self.directory / "source-assets"
        self.fixtures.mkdir()
        self.evidence = self.directory / "verification"
        write_candidates(self.fixtures)
        self.tag = f"v{VERSION}"
        self.names = sorted(path.name for path in self.fixtures.iterdir())
        self.run = {
            "id": 42, "head_sha": SOURCE_SHA, "head_branch": "main",
            "path": ".github/workflows/release.yml", "event": "workflow_dispatch",
            "status": "completed", "conclusion": "success", "run_attempt": 1,
        }
        self.step = {
            "name": "Semantic Release", "conclusion": "success",
            "started_at": "2026-11-24T09:00:00Z", "completed_at": "2026-11-24T09:05:00Z",
        }
        self.ref = {"ref": f"refs/tags/{self.tag}", "object": {"type": "commit", "sha": SOURCE_SHA}}
        self.release = {
            "tag_name": self.tag, "draft": False, "published_at": "2026-11-24T09:03:00Z",
            "assets": [
                {"name": name, "state": "uploaded", "size": (self.fixtures / name).stat().st_size,
                 "digest": "sha256:" + hashlib.sha256((self.fixtures / name).read_bytes()).hexdigest()}
                for name in self.names
            ],
        }

    def verify(
        self, mutation: tuple[str, bytes] | None = None, invalid_signature: bool = False,
        signature: str = "Signature type: Repository\nhttps://api.nuget.org/v3/index.json",
        committed_manifest: bytes | None = None,
    ) -> None:
        responses = {
            "actions/runs/42": self.run,
            "actions/runs/42/attempts/1/jobs": {
                "jobs": [{"name": "Release packages", "conclusion": "success", "steps": [self.step]}],
            },
            f"git/ref/tags/{self.tag}": self.ref,
            f"releases/tags/{self.tag}": self.release,
        }

        def fetch_package(url: str, output: Path) -> None:
            self.assertTrue(url.endswith("/" + output.name.lower()))
            shutil.copyfile(self.fixtures / output.name, output)
            rewrite_candidate(output, extra_entry=(".signature.p7s", b"fixture signature"))
            if mutation is not None:
                rewrite_candidate(output, extra_entry=mutation)

        def execute(command: list[str], **kwargs) -> subprocess.CompletedProcess:
            self.assertTrue(kwargs["check"])
            if command[:3] == ["gh", "release", "download"]:
                for path in self.fixtures.iterdir():
                    shutil.copyfile(path, Path(command[-1]) / path.name)
                return subprocess.CompletedProcess(command, 0)
            self.assertEqual(["dotnet", "nuget", "verify", "--all"], command[:4])
            if invalid_signature:
                raise subprocess.CalledProcessError(1, command)
            return subprocess.CompletedProcess(command, 0, signature)

        with (
            mock.patch.object(self.verifier, "github", side_effect=responses.__getitem__),
            mock.patch.object(self.verifier, "download_package", side_effect=fetch_package),
            mock.patch.object(self.verifier, "read_committed_module_manifest",
                              return_value=MODULE_MANIFEST.read_bytes() if committed_manifest is None else committed_manifest) as read_manifest,
            mock.patch.object(self.verifier.subprocess, "run", side_effect=execute),
        ):
            self.verifier.verify_publication(VERSION, SOURCE_SHA, 42, self.evidence)
        read_manifest.assert_called_once_with(SOURCE_SHA)

    def test_exact_release_emits_complete_receipt(self) -> None:
        self.verify()
        receipt = json.loads((self.evidence / "publication-verified.json").read_text())
        self.assertEqual(self.names, sorted(receipt["packages"]))
        for package in receipt["packages"].values():
            self.assertNotEqual(package["githubSha256"], package["nugetSha256"])

    def test_wrong_tag_commit_is_rejected(self) -> None:
        self.ref["object"]["sha"] = "b" * 40
        with self.assertRaisesRegex(ValueError, "does not resolve"):
            self.verify()

    def test_wrong_workflow_is_rejected(self) -> None:
        self.run["path"] = ".github/workflows/ci.yml"
        with self.assertRaisesRegex(ValueError, "Workflow run"):
            self.verify()

    def test_frozen_run_cannot_claim_an_existing_release(self) -> None:
        self.step["conclusion"] = "skipped"
        with self.assertRaisesRegex(ValueError, "did not successfully execute"):
            self.verify()

    def test_same_source_release_from_another_attempt_is_rejected(self) -> None:
        self.release["published_at"] = "2026-11-24T08:59:59Z"
        with self.assertRaisesRegex(ValueError, "outside the selected workflow attempt"):
            self.verify()

    def test_extra_release_asset_is_rejected(self) -> None:
        self.release["assets"].append({"name": "unrelated.nupkg"})
        with self.assertRaisesRegex(ValueError, "unexpected asset inventory"):
            self.verify()

    def test_mismatched_github_asset_digest_is_rejected(self) -> None:
        self.release["assets"][0]["digest"] = "sha256:" + "b" * 64
        with self.assertRaisesRegex(ValueError, "differs from the published asset metadata"):
            self.verify()

    def test_author_signature_is_insufficient(self) -> None:
        with self.assertRaisesRegex(ValueError, "verified NuGet.org repository signature"):
            self.verify(signature="Signature type: Author")

    def test_changed_payload_is_rejected(self) -> None:
        with self.assertRaisesRegex(ValueError, "payloads differ"):
            self.verify(mutation=("README.md", b"Different public readme"))

    def test_rejects_package_that_matches_worktree_instead_of_commit(self) -> None:
        with self.assertRaisesRegex(ValueError, "must contain the exact Projects module manifest"):
            self.verify(committed_manifest=b"committed bytes that differ from the checkout")

    def test_reads_manifest_from_requested_commit_even_when_worktree_is_dirty(self) -> None:
        worktree = self.directory / "committed"
        worktree.mkdir()
        manifest_path = worktree / "module/hexalith.module-manifest.v2.json"
        manifest_path.parent.mkdir()
        manifest_path.write_bytes(b"committed declaration")
        git = ["git", "-C", str(worktree)]
        subprocess.run([*git, "init", "-q"], check=True)
        subprocess.run([*git, "add", str(manifest_path)], check=True)
        subprocess.run([*git, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.com",
                        "commit", "-qm", "fixture"], check=True)
        revision = subprocess.check_output([*git, "rev-parse", "HEAD"], text=True).strip()
        manifest_path.write_bytes(b"modified worktree")
        with mock.patch.object(_nuget, "ROOT", worktree), mock.patch.object(_nuget, "MODULE_MANIFEST", manifest_path):
            self.assertEqual(b"committed declaration", _nuget.read_committed_module_manifest(revision))

    def test_signature_failure_never_creates_success_receipt(self) -> None:
        with self.assertRaises(subprocess.CalledProcessError):
            self.verify(invalid_signature=True)
        self.assertFalse((self.evidence / "publication-verified.json").exists())


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

    def test_all_pack_commands_inherit_nuget_audit_policy(self) -> None:
        output = self.directory / "pack"
        arguments = argparse.Namespace(output_directory=output, version=VERSION)

        def pack_fixture(command: list[str], *, check: bool) -> None:
            self.assertEqual(["dotnet", "pack"], command[:2])
            self.assertTrue(check)
            self.assertIn("-warnaserror", command)
            self.assertFalse(any("nugetaudit=false" in arg.casefold() for arg in command))
            shutil.copy(self.candidate(Path(command[2]).stem), output)

        with (
            mock.patch.object(packer, "validate_manifest_projects"),
            mock.patch.object(packer, "current_source_sha", return_value=SOURCE_SHA),
            mock.patch.object(packer.subprocess, "run", side_effect=pack_fixture) as run,
        ):
            self.assertEqual(0, packer.main(arguments))
        self.assertEqual(len(load_manifest().packages), run.call_count)

    def test_pack_stops_on_first_command_failure(self) -> None:
        arguments = argparse.Namespace(output_directory=self.directory / "pack", version=VERSION)
        failure = subprocess.CalledProcessError(1, ["dotnet", "pack"])
        with (
            mock.patch.object(packer, "validate_manifest_projects"),
            mock.patch.object(packer, "current_source_sha", return_value=SOURCE_SHA),
            mock.patch.object(packer.subprocess, "run", side_effect=failure) as run,
            self.assertRaises(subprocess.CalledProcessError) as raised,
        ):
            packer.main(arguments)
        self.assertIs(failure, raised.exception)
        run.assert_called_once()

    def test_publication_pushes_extension_first_and_stops_after_refusal(self) -> None:
        binary = self.directory / "bin"
        binary.mkdir()
        fake_dotnet = binary / "dotnet"
        fake_dotnet.write_text(
            '#!/bin/sh\nprintf "%s\\n" "$3" >> "$PUSH_LOG"\nexit 23\n', encoding="utf-8",
        )
        fake_dotnet.chmod(0o755)
        fake_python = binary / "python3"
        fake_python.write_text("#!/bin/sh\nexit 0\n", encoding="utf-8")
        fake_python.chmod(0o755)
        self.directory.joinpath("nupkgs").mkdir()
        for archive in self.directory.glob("*.nupkg"):
            shutil.copyfile(archive, self.directory / "nupkgs" / archive.name)
        command = subprocess.check_output(
            ["node", "-e", 'process.stdout.write(require("./release.config.cjs").plugins[2][1].publishCmd)'],
            cwd=REPOSITORY_ROOT, text=True,
        ).replace("${nextRelease.version}", VERSION)
        push_log = self.directory / "push.log"
        result = subprocess.run(["bash", "-c", command], cwd=self.directory, check=False, env={
            **os.environ, "PATH": f"{binary}{os.pathsep}{os.environ['PATH']}", "PUSH_LOG": str(push_log),
            "NUGET_API_KEY": "fixture-key",
        })
        self.assertEqual(23, result.returncode)
        self.assertEqual([f"./nupkgs/{EXTENSION_PACKAGE_ID}.{VERSION}.nupkg"], push_log.read_text().splitlines())

    def test_exact_candidates_pass(self) -> None:
        packages, version, source = validate_packages(self.directory, VERSION, SOURCE_SHA)
        self.assertEqual(6, len(packages))
        self.assertEqual(VERSION, version)
        self.assertEqual(SOURCE_SHA, source)

    def test_extension_rejects_manifest_with_different_content(self) -> None:
        rewrite_candidate(
            self.candidate(EXTENSION_PACKAGE_ID),
            extra_entry=(PACKAGED_MODULE_MANIFEST, b"{}"),
        )
        self.assert_invalid("must contain the exact Projects module manifest")

    def test_extension_rejects_stripped_manifest(self) -> None:
        rewrite_candidate(self.candidate(EXTENSION_PACKAGE_ID), remove_entry=PACKAGED_MODULE_MANIFEST)
        self.assert_invalid("must contain the exact Projects module manifest")

    def test_other_packages_reject_a_module_manifest(self) -> None:
        rewrite_candidate(
            self.candidate("Hexalith.Projects.Contracts"),
            extra_entry=(PACKAGED_MODULE_MANIFEST, MODULE_MANIFEST.read_bytes()),
        )
        self.assert_invalid("unexpected Projects module manifest")

    def test_service_defaults_rejects_stale_opentelemetry_dependency_versions(self) -> None:
        package = next(item for item in load_manifest().packages
                       if item.package_id == "Hexalith.Projects.ServiceDefaults")
        telemetry = {name: version for name, version in package.dependencies.items()
                     if name.startswith("OpenTelemetry")}
        self.assertEqual(6, len(telemetry))
        for name, current in telemetry.items():
            with self.subTest(dependency=name):
                self.assertNotEqual("1.19.0", current)
                write_candidates(self.directory)

                def stale_version(root: ET.Element) -> None:
                    dependency = next(item for item in root.iter("dependency") if item.get("id") == name)
                    dependency.set("version", "1.19.0")

                rewrite_candidate(self.candidate(package.package_id), stale_version)
                self.assert_invalid("dependency graph differs")

    def test_external_dependency_may_equal_candidate_version(self) -> None:
        for candidate in self.directory.glob("*.nupkg"):
            candidate.unlink()
        manifest = load_manifest()
        contracts_manifest = next(
            item for item in manifest.packages if item.package_id.endswith("Contracts")
        )
        external_id = "Hexalith.EventStore.Contracts"
        self.assertNotIn(external_id, {item.package_id for item in manifest.packages})
        external_version = contracts_manifest.dependencies[external_id]
        self.assertNotIn("{", external_version)
        write_candidates(self.directory, version=external_version)
        normalize_internal_dependency_ranges(self.directory, external_version)
        packages, version, _ = validate_packages(self.directory, external_version, SOURCE_SHA)
        contracts = next(item for item in packages if item.package_id.endswith("Contracts"))
        self.assertEqual(
            external_version,
            contracts.dependencies[external_id],
        )
        self.assertEqual(external_version, version)

    def test_missing_and_stale_packages_fail(self) -> None:
        self.candidate("Hexalith.Projects.Client").unlink()
        self.assert_invalid("Expected exactly 6 release packages")
        write_candidates(self.directory)
        (self.directory / "Stale.1.0.0.nupkg").write_bytes(b"not a package")
        self.assert_invalid("Expected exactly 6 release packages")

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
                {"id": "hexalith.eventstore.contracts", "version": "5.1.0", "exclude": "Build,Analyzers"},
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
            EXTENSION_PACKAGE_ID: (
                "using Hexalith.Projects.EventStoreExtensions;\n"
                "Console.WriteLine(typeof(ProjectsIntentAdapters).Assembly.GetName().Name);\n"
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

    def test_extension_only_closure_rejects_transitive_provider(self) -> None:
        module = load_consumer_module()
        path = self.directory / "extension-assets.json"
        contracts = next(package for package in load_manifest().packages if package.package_id == "Hexalith.Projects.Contracts")
        libraries = {
            f"{identifier}/{VERSION}": {"type": "package"}
            for identifier in (EXTENSION_PACKAGE_ID, contracts.package_id, *contracts.dependencies)
        }
        path.write_text(json.dumps({"libraries": libraries}), encoding="utf-8")
        module.assert_package_only_assets(path, EXTENSION_PACKAGE_ID, VERSION, extension_only=True)
        libraries["Dapr.Client/1.19.1"] = {"type": "package"}
        path.write_text(json.dumps({"libraries": libraries}), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "unexpected=.*dapr.client"):
            module.assert_package_only_assets(path, EXTENSION_PACKAGE_ID, VERSION, extension_only=True)

    def test_packed_extension_consumer_builds_and_executes_eventstore_host(self) -> None:
        module = load_consumer_module()
        package = next(package for package in load_manifest().packages if package.package_id == EXTENSION_PACKAGE_ID)
        with mock.patch.object(module, "assert_package_only_assets") as assets, mock.patch.object(module.subprocess, "run") as run:
            module.run_consumer(package, VERSION, self.directory, self.directory, eventstore_host=True)
        host = self.directory / "eventstore-extension-host"
        self.assertIn('Include="Hexalith.EventStore.Server" Version="[5.6.0]"', (host / "Consumer.csproj").read_text())
        assets.assert_called_once_with(host / "obj/project.assets.json", EXTENSION_PACKAGE_ID, VERSION, extension_only=False)
        self.assertEqual(3, run.call_count)
        runtime_command = run.call_args.args[0]
        self.assertEqual(["dotnet", str(host / "bin/Release/net11.0/Consumer.dll"),
                          str(host / ".packages" / EXTENSION_PACKAGE_ID.lower() / VERSION / PACKAGED_MODULE_MANIFEST)], runtime_command)

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
            3,
            len(non_skip_commands),
            f"commands={non_skip_commands!r}\nstdout={non_skip.stdout}\nstderr={non_skip.stderr}",
        )
        self.assertTrue(all(command.startswith("python3|") for command in non_skip_commands))
        self.assertIn("pack-release-packages.py", non_skip_commands[0])
        self.assertIn("validate-nuget-packages.py", non_skip_commands[1])
        self.assertIn("validate-consumer-package-references.py", non_skip_commands[2])
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

    def test_wrapper_rejects_frontcomposer_reference_in_contracts(self) -> None:
        sandbox = self.directory / "repository"
        contracts = Path("src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj")
        for relative in (
            Path("tests/tools/run-package-dependency-gate.ps1"),
            contracts,
            Path("src/Hexalith.Projects.UI.Contracts/Hexalith.Projects.UI.Contracts.csproj"),
        ):
            target = sandbox / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(REPOSITORY_ROOT / relative, target)
        binary_directory = self.directory / "bin"
        binary_directory.mkdir()
        command_log = self.directory / "commands.log"
        for command in ("dotnet", "python3"):
            path = binary_directory / command
            path.write_text(
                "#!/bin/sh\nprintf '%s|%s\\n' \"$(basename \"$0\")\" \"$*\" >> \"$COMMAND_LOG\"\nexit 0\n",
                encoding="utf-8",
            )
            path.chmod(0o755)
        environment = dict(os.environ)
        environment["PATH"] = f"{binary_directory}{os.pathsep}{environment['PATH']}"
        environment["COMMAND_LOG"] = str(command_log)
        dotnet_host = shutil.which("dotnet")
        self.assertIsNotNone(dotnet_host)
        pwsh_assembly = subprocess.run(
            ["pwsh", "-NoProfile", "-Command", "[System.Reflection.Assembly]::GetEntryAssembly().Location"],
            capture_output=True,
            text=True,
            check=True,
        ).stdout.strip()
        original = (REPOSITORY_ROOT / contracts).read_text(encoding="utf-8")
        self.assertNotIn("Hexalith.FrontComposer", original)
        package_mode_marker = """  <ItemGroup Condition="'$(UseHexalithProjectReferences)' != 'true'">\n"""
        source_mode_marker = """  <ItemGroup Condition="'$(UseHexalithProjectReferences)' == 'true'">\n"""
        injections = {
            "package": (
                package_mode_marker,
                '    <PackageReference Include="Hexalith.FrontComposer.Contracts" />\n',
            ),
            "source": (
                source_mode_marker,
                "    <ProjectReference Include=\"$(HexalithFrontComposerRoot)\\src\\Hexalith.FrontComposer.Shell"
                "\\Hexalith.FrontComposer.Shell.csproj\" />\n",
            ),
        }
        for mode, (marker, reference) in injections.items():
            with self.subTest(mode=mode):
                self.assertEqual(1, original.count(marker))
                (sandbox / contracts).write_text(original.replace(marker, marker + reference), encoding="utf-8")
                command_log.write_text("", encoding="utf-8")
                result = subprocess.run(
                    [
                        dotnet_host,
                        pwsh_assembly,
                        "-NoProfile",
                        "-File",
                        str(sandbox / "tests" / "tools" / "run-package-dependency-gate.ps1"),
                        "-Version",
                        VERSION,
                        "-PackageDirectory",
                        str(self.directory),
                        "-SkipPack",
                    ],
                    cwd=sandbox,
                    env=environment,
                    capture_output=True,
                    text=True,
                    check=False,
                )
                self.assertNotEqual(0, result.returncode, result.stdout)
                self.assertIn("must not reference Hexalith.FrontComposer", result.stdout + result.stderr)
                self.assertNotIn("PASSED", result.stdout)
                self.assertEqual("", command_log.read_text(encoding="utf-8"))


class PlatformMtlsInputTests(unittest.TestCase):
    """Exercise the imported MSBuild target with all three real consumer projects."""

    def test_direct_mapped_and_sibling_only_inputs(self) -> None:
        consumers = {
            "src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj": "true",
            "tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj": "false",
            "tests/Hexalith.Projects.PackageFixtures.AppHost/Hexalith.Projects.PackageFixtures.AppHost.csproj": "false",
        }
        for project, source_mode in consumers.items():
            with self.subTest(project=project), tempfile.TemporaryDirectory() as private:
                scratch = Path(private)
                root = scratch / "active"
                target = root / project
                target.parent.mkdir(parents=True)
                shutil.copyfile(REPOSITORY_ROOT / project, target)
                imported = root / "src/PlatformMtlsHelpers.targets"
                imported.parent.mkdir(exist_ok=True)
                shutil.copyfile(REPOSITORY_ROOT / "src/PlatformMtlsHelpers.targets", imported)
                direct = root / "references/Hexalith.Platform"
                mapped = scratch / "mapped"
                sibling = scratch / "Hexalith.Platform"
                helpers = ("DaprSelfHostedMtls.cs", "DaprSelfHostedMtlsSubscriber.cs")
                for available in (sibling, mapped):
                    available.mkdir(parents=True)
                    for helper in helpers:
                        (available / helper).touch()
                args = [
                    "dotnet", "msbuild", str(target), "-nologo", "-t:ValidateProjectsPlatformMtlsInput",
                    f"-p:UseHexalithProjectReferences={source_mode}",
                    f"-p:HexalithTenantsRoot={scratch / 'tenants'}", f"-p:HexalithFoldersRoot={scratch / 'folders'}",
                ]

                def build(*variables: str) -> subprocess.CompletedProcess:
                    return subprocess.run(
                        [*args, *variables], cwd=root, capture_output=True, text=True,
                        timeout=30, check=False,
                    )

                refused = build("-p:HexalithPlatformRoot=")
                self.assertNotEqual(0, refused.returncode)
                self.assertIn(f"Projects Platform input is missing at '{direct}'", refused.stdout + refused.stderr)
                selected = build(f"-p:HexalithPlatformRoot={mapped}")
                self.assertEqual(0, selected.returncode, selected.stdout + selected.stderr)
                shutil.copytree(mapped, direct)
                selected = build("-p:HexalithPlatformRoot=")
                self.assertEqual(0, selected.returncode, selected.stdout + selected.stderr)
                missing = scratch / "missing-mapped-input"
                refused = build(f"-p:HexalithPlatformRoot={missing}")
                self.assertNotEqual(0, refused.returncode)
                self.assertIn(f"Projects Platform input is missing at '{missing}'", refused.stdout + refused.stderr)


if __name__ == "__main__":
    unittest.main()
