#!/usr/bin/env python3
"""Shared fail-closed helpers for the Projects package-candidate tools."""

from __future__ import annotations

import json
import os
import re
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile
from dataclasses import dataclass
from pathlib import Path, PurePosixPath, PureWindowsPath
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "tools" / "release-packages.json"
PACKAGE_IDS = {
    "Hexalith.Projects",
    "Hexalith.Projects.Client",
    "Hexalith.Projects.Contracts",
    "Hexalith.Projects.ServiceDefaults",
    "Hexalith.Projects.Testing",
}
PACKAGE_ID_PATTERN = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]*$")
VERSION_PATTERN = re.compile(r"^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$")
SOURCE_SHA_PATTERN = re.compile(r"^[0-9a-f]{40}$")
MAX_ENTRY_COUNT = 2048
MAX_ENTRY_SIZE = 64 * 1024 * 1024
MAX_TOTAL_SIZE = 128 * 1024 * 1024
ACTIVE_PAYLOAD_ROOTS = {
    "analyzers",
    "build",
    "buildmultitargeting",
    "buildtransitive",
    "content",
    "contentfiles",
    "native",
    "runtimes",
    "tools",
}


@dataclass(frozen=True)
class ReleaseMetadata:
    """Exact metadata shared by every release package."""

    authors: str
    license: str
    project_url: str
    repository_type: str
    repository_url: str
    readme: str
    description: str
    tags: tuple[str, ...]
    target_framework: str


@dataclass(frozen=True)
class ManifestPackage:
    """One manifest-owned package and its complete candidate contract."""

    package_id: str
    project: str
    project_path: Path
    assembly: str
    framework_references: tuple[str, ...]
    dependencies: dict[str, str]
    probe: str


@dataclass(frozen=True)
class ReleaseManifest:
    """Validated package manifest."""

    metadata: ReleaseMetadata
    packages: tuple[ManifestPackage, ...]


@dataclass(frozen=True)
class PackageMetadata:
    """Validated metadata read from a package candidate."""

    path: Path
    package_id: str
    version: str
    source_sha: str
    dependencies: dict[str, str]
    framework_references: tuple[str, ...]


def reject_duplicate_json_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    """Reject duplicate JSON keys instead of silently accepting the last value."""

    result: dict[str, Any] = {}
    normalized: set[str] = set()
    for key, value in pairs:
        folded = key.casefold()
        if folded in normalized:
            raise ValueError(f"Duplicate JSON key: {key!r}")
        normalized.add(folded)
        result[key] = value
    return result


def _require_string(value: Any, field: str) -> str:
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"{field} must be a non-empty string")
    return value.strip()


def _require_string_list(value: Any, field: str) -> tuple[str, ...]:
    if not isinstance(value, list):
        raise ValueError(f"{field} must be an array")
    result = tuple(_require_string(item, f"{field}[]") for item in value)
    if len({item.casefold() for item in result}) != len(result):
        raise ValueError(f"{field} contains duplicate values")
    return result


def load_manifest(path: Path = MANIFEST) -> ReleaseManifest:
    """Load and strictly validate the authoritative five-package manifest."""

    resolved = path.resolve()
    try:
        with resolved.open("r", encoding="utf-8") as handle:
            data = json.load(handle, object_pairs_hook=reject_duplicate_json_keys)
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise ValueError(f"Cannot read package manifest {resolved}: {exc}") from exc

    if not isinstance(data, dict) or data.get("schemaVersion") != 1:
        raise ValueError("Package manifest schemaVersion must be exactly 1")
    raw_metadata = data.get("metadata")
    raw_packages = data.get("packages")
    if not isinstance(raw_metadata, dict) or not isinstance(raw_packages, list):
        raise ValueError("Package manifest must contain metadata and packages")

    metadata = ReleaseMetadata(
        authors=_require_string(raw_metadata.get("authors"), "metadata.authors"),
        license=_require_string(raw_metadata.get("license"), "metadata.license"),
        project_url=_require_string(raw_metadata.get("projectUrl"), "metadata.projectUrl"),
        repository_type=_require_string(raw_metadata.get("repositoryType"), "metadata.repositoryType"),
        repository_url=_require_string(raw_metadata.get("repositoryUrl"), "metadata.repositoryUrl"),
        readme=_require_string(raw_metadata.get("readme"), "metadata.readme"),
        description=_require_string(raw_metadata.get("description"), "metadata.description"),
        tags=_require_string_list(raw_metadata.get("tags"), "metadata.tags"),
        target_framework=_require_string(raw_metadata.get("targetFramework"), "metadata.targetFramework"),
    )

    packages: list[ManifestPackage] = []
    ids: set[str] = set()
    projects: set[str] = set()
    for index, value in enumerate(raw_packages):
        if not isinstance(value, dict):
            raise ValueError(f"packages[{index}] must be an object")
        package_id = _require_string(value.get("id"), f"packages[{index}].id")
        project = _require_string(value.get("project"), f"packages[{index}].project")
        assembly = _require_string(value.get("assembly"), f"packages[{index}].assembly")
        probe = _require_string(value.get("probe"), f"packages[{index}].probe") + "\n"
        if not PACKAGE_ID_PATTERN.fullmatch(package_id):
            raise ValueError(f"Invalid package id: {package_id!r}")
        project_parts = PurePosixPath(project.replace("\\", "/"))
        if project_parts.is_absolute() or ".." in project_parts.parts or project_parts.suffix != ".csproj":
            raise ValueError(f"Unsafe project path for {package_id}: {project!r}")
        project_path = (ROOT / project_parts).resolve()
        try:
            project_path.relative_to(ROOT)
        except ValueError as exc:
            raise ValueError(f"Project escapes repository root: {project!r}") from exc
        if not project_path.is_file():
            raise ValueError(f"Manifest project does not exist: {project}")

        raw_dependencies = value.get("dependencies")
        if not isinstance(raw_dependencies, dict):
            raise ValueError(f"{package_id}.dependencies must be an object")
        dependencies: dict[str, str] = {}
        dependency_ids: set[str] = set()
        for dependency_id, dependency_version in raw_dependencies.items():
            normalized_id = _require_string(dependency_id, f"{package_id}.dependency id")
            normalized_version = _require_string(
                dependency_version,
                f"{package_id}.dependencies[{normalized_id}]",
            )
            folded = normalized_id.casefold()
            if folded in dependency_ids:
                raise ValueError(f"{package_id} has duplicate dependency {normalized_id}")
            if normalized_version != "{version}" and not VERSION_PATTERN.fullmatch(normalized_version):
                raise ValueError(
                    f"{package_id} dependency {normalized_id} has unsupported version {normalized_version!r}"
                )
            dependency_ids.add(folded)
            dependencies[normalized_id] = normalized_version

        folded_id = package_id.casefold()
        folded_project = project_parts.as_posix().casefold()
        if folded_id in ids or folded_project in projects:
            raise ValueError(f"Duplicate manifest package id or project: {package_id}")
        ids.add(folded_id)
        projects.add(folded_project)
        packages.append(
            ManifestPackage(
                package_id=package_id,
                project=project_parts.as_posix(),
                project_path=project_path,
                assembly=assembly,
                framework_references=_require_string_list(
                    value.get("frameworkReferences"),
                    f"{package_id}.frameworkReferences",
                ),
                dependencies=dependencies,
                probe=probe,
            )
        )

    if {package.package_id for package in packages} != PACKAGE_IDS:
        raise ValueError(
            "Package manifest inventory differs from the frozen five-package set: "
            f"{sorted(package.package_id for package in packages)}"
        )
    return ReleaseManifest(metadata=metadata, packages=tuple(packages))


def evaluate_project_properties(project: Path) -> dict[str, str]:
    """Evaluate identity properties for a manifest project without building it."""

    command = [
        "dotnet",
        "msbuild",
        str(project),
        "-nologo",
        "-getProperty:PackageId,AssemblyName,TargetFramework,IsPackable",
        "-p:Configuration=Release",
        "-p:UseHexalithProjectReferences=false",
    ]
    completed = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, check=False)
    if completed.returncode != 0:
        detail = completed.stderr.strip() or completed.stdout.strip()
        raise ValueError(f"Cannot evaluate {project.relative_to(ROOT)}: {detail}")
    try:
        values = json.loads(completed.stdout)["Properties"]
    except (json.JSONDecodeError, KeyError, TypeError) as exc:
        raise ValueError(f"Invalid MSBuild property output for {project.relative_to(ROOT)}") from exc
    return {name: str(values.get(name, "")) for name in ("PackageId", "AssemblyName", "TargetFramework", "IsPackable")}


def validate_manifest_projects(manifest: ReleaseManifest) -> None:
    """Prove every manifest entry still identifies the intended packable project."""

    for package in manifest.packages:
        properties = evaluate_project_properties(package.project_path)
        if properties["PackageId"] != package.package_id:
            raise ValueError(
                f"{package.project} evaluates PackageId={properties['PackageId']!r}, expected {package.package_id!r}"
            )
        if properties["AssemblyName"] != package.assembly:
            raise ValueError(
                f"{package.project} evaluates AssemblyName={properties['AssemblyName']!r}, expected {package.assembly!r}"
            )
        if properties["TargetFramework"] != manifest.metadata.target_framework:
            raise ValueError(
                f"{package.project} targets {properties['TargetFramework']!r}, expected {manifest.metadata.target_framework!r}"
            )
        if properties["IsPackable"].casefold() != "true":
            raise ValueError(f"Manifest project is not packable: {package.project}")


def current_source_sha() -> str:
    """Return the exact current repository commit used as package provenance."""

    completed = subprocess.run(
        ["git", "rev-parse", "HEAD"],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    value = completed.stdout.strip()
    if completed.returncode != 0 or not SOURCE_SHA_PATTERN.fullmatch(value):
        raise ValueError("Cannot resolve an exact lowercase source commit for package provenance")
    return value


def _safe_entries(archive: zipfile.ZipFile, path: Path) -> tuple[zipfile.ZipInfo, ...]:
    entries = tuple(archive.infolist())
    if not entries or len(entries) > MAX_ENTRY_COUNT:
        raise ValueError(f"Package {path.name} has an unsafe entry count: {len(entries)}")
    names: set[str] = set()
    total_size = 0
    for entry in entries:
        name = entry.filename
        normalized = name.replace("\\", "/")
        parts = PurePosixPath(normalized).parts
        windows = PureWindowsPath(name)
        if (
            not name
            or "\x00" in name
            or "\\" in name
            or normalized.startswith("/")
            or windows.is_absolute()
            or windows.drive
            or any(part in ("", ".", "..") for part in parts)
        ):
            raise ValueError(f"Package {path.name} contains unsafe archive entry {name!r}")
        if parts[0].casefold() in ACTIVE_PAYLOAD_ROOTS:
            raise ValueError(
                f"Package {path.name} contains forbidden active payload root {parts[0]!r}"
            )
        folded = normalized.casefold()
        if folded in names:
            raise ValueError(f"Package {path.name} contains duplicate archive entry {name!r}")
        names.add(folded)
        if entry.flag_bits & 0x1:
            raise ValueError(f"Package {path.name} contains encrypted entry {name!r}")
        unix_file_type = (entry.external_attr >> 16) & 0o170000
        if unix_file_type not in (0, 0o040000, 0o100000):
            raise ValueError(f"Package {path.name} contains a special archive entry {name!r}")
        if entry.file_size > MAX_ENTRY_SIZE:
            raise ValueError(f"Package {path.name} entry is too large: {name!r}")
        total_size += entry.file_size
        if total_size > MAX_TOTAL_SIZE:
            raise ValueError(f"Package {path.name} expands beyond the safe size limit")
    return entries


def _local_name(element: ET.Element) -> str:
    return element.tag.rsplit("}", 1)[-1]


def _children(element: ET.Element, name: str) -> list[ET.Element]:
    return [child for child in element if _local_name(child) == name]


def _required_child_text(element: ET.Element, name: str, package_name: str) -> str:
    matches = _children(element, name)
    if len(matches) != 1 or not matches[0].text or not matches[0].text.strip():
        raise ValueError(f"Package {package_name} must contain exactly one non-empty {name}")
    return matches[0].text.strip()


def _read_package(path: Path, manifest: ReleaseManifest) -> tuple[PackageMetadata, dict[str, Any]]:
    try:
        archive = zipfile.ZipFile(path)
    except (OSError, zipfile.BadZipFile) as exc:
        raise ValueError(f"Package is not a valid ZIP archive: {path.name}") from exc
    with archive:
        entries = _safe_entries(archive, path)
        nuspecs = [entry for entry in entries if entry.filename.casefold().endswith(".nuspec")]
        if len(nuspecs) != 1 or "/" in nuspecs[0].filename:
            raise ValueError(f"Package {path.name} must contain exactly one root nuspec")
        nuspec_bytes = archive.read(nuspecs[0])
        upper = nuspec_bytes.upper()
        if b"<!DOCTYPE" in upper or b"<!ENTITY" in upper:
            raise ValueError(f"Package {path.name} contains unsafe XML declarations")
        try:
            root = ET.fromstring(nuspec_bytes)
        except ET.ParseError as exc:
            raise ValueError(f"Package {path.name} contains malformed nuspec XML") from exc
        if _local_name(root) != "package":
            raise ValueError(f"Package {path.name} nuspec must have a package root")
        metadata_matches = _children(root, "metadata")
        if len(metadata_matches) != 1:
            raise ValueError(f"Package {path.name} must contain exactly one metadata element")
        metadata = metadata_matches[0]
        allowed_metadata = {
            "id",
            "version",
            "authors",
            "license",
            "licenseUrl",
            "readme",
            "projectUrl",
            "description",
            "tags",
            "repository",
            "dependencies",
            "frameworkReferences",
        }
        unexpected_metadata = sorted(
            {_local_name(element) for element in metadata} - allowed_metadata
        )
        if unexpected_metadata:
            raise ValueError(
                f"Package {path.name} contains unexpected metadata: {unexpected_metadata}"
            )
        license_urls = _children(metadata, "licenseUrl")
        if len(license_urls) > 1 or (
            license_urls
            and (license_urls[0].text or "").strip() != "https://licenses.nuget.org/MIT"
        ):
            raise ValueError(f"Package {path.name} contains unexpected license URL metadata")
        package_id = _required_child_text(metadata, "id", path.name)
        version = _required_child_text(metadata, "version", path.name)
        if not PACKAGE_ID_PATTERN.fullmatch(package_id) or not VERSION_PATTERN.fullmatch(version):
            raise ValueError(f"Package {path.name} has invalid identity metadata")

        repository_nodes = _children(metadata, "repository")
        if len(repository_nodes) != 1:
            raise ValueError(f"Package {path.name} must contain exactly one repository element")
        source_sha = repository_nodes[0].attrib.get("commit", "").strip()

        dependency_containers = _children(metadata, "dependencies")
        if len(dependency_containers) != 1:
            raise ValueError(f"Package {path.name} must contain exactly one dependencies element")
        dependency_groups = _children(dependency_containers[0], "group")
        if len(dependency_groups) != 1:
            raise ValueError(f"Package {path.name} must contain exactly one dependency group")
        dependency_group = dependency_groups[0]
        if dependency_group.attrib.get("targetFramework") != manifest.metadata.target_framework:
            raise ValueError(f"Package {path.name} dependency group targets the wrong framework")
        dependency_elements = _children(dependency_group, "dependency")
        if len(dependency_elements) != len(
            [element for element in metadata.iter() if _local_name(element) == "dependency"]
        ):
            raise ValueError(f"Package {path.name} contains dependencies outside its framework group")

        dependencies: dict[str, str] = {}
        normalized_dependencies: set[str] = set()
        for dependency in dependency_elements:
            if set(dependency.attrib) != {"id", "version", "exclude"}:
                raise ValueError(
                    f"Package {path.name} dependency contains unexpected attributes"
                )
            dependency_id = dependency.attrib.get("id", "").strip()
            dependency_version = dependency.attrib.get("version", "").strip()
            folded = dependency_id.casefold()
            if not PACKAGE_ID_PATTERN.fullmatch(dependency_id) or not dependency_version:
                raise ValueError(f"Package {path.name} has malformed dependency metadata")
            if folded in normalized_dependencies:
                raise ValueError(f"Package {path.name} repeats dependency {dependency_id!r}")
            excludes = {value.strip().casefold() for value in dependency.attrib.get("exclude", "").split(",") if value.strip()}
            if excludes != {"build", "analyzers"}:
                raise ValueError(f"Package {path.name} dependency {dependency_id} has unsafe exclude metadata")
            normalized_dependencies.add(folded)
            dependencies[dependency_id] = dependency_version

        framework_references: list[str] = []
        reference_elements = [
            element for element in metadata.iter() if _local_name(element) == "frameworkReference"
        ]
        reference_containers = _children(metadata, "frameworkReferences")
        if reference_elements:
            if len(reference_containers) != 1:
                raise ValueError(f"Package {path.name} must contain exactly one frameworkReferences element")
            reference_groups = _children(reference_containers[0], "group")
            if (
                len(reference_groups) != 1
                or reference_groups[0].attrib.get("targetFramework") != manifest.metadata.target_framework
                or len(_children(reference_groups[0], "frameworkReference")) != len(reference_elements)
            ):
                raise ValueError(f"Package {path.name} has malformed framework reference grouping")
        elif reference_containers:
            raise ValueError(f"Package {path.name} contains an empty frameworkReferences element")
        for reference in reference_elements:
            name = reference.attrib.get("name", "").strip()
            if not name or name.casefold() in {item.casefold() for item in framework_references}:
                raise ValueError(f"Package {path.name} has malformed framework references")
            framework_references.append(name)

        compile_assets = [
            entry
            for entry in entries
            if not entry.is_dir()
            and entry.filename.casefold().endswith(".dll")
            and PurePosixPath(entry.filename).parts[0].casefold() in {"lib", "ref"}
        ]
        expected_asset = f"lib/{manifest.metadata.target_framework}/{package_id}.dll"
        if [entry.filename for entry in compile_assets] != [expected_asset]:
            raise ValueError(
                f"Package {path.name} compile assets differ from the exact contract; expected {expected_asset!r}"
            )
        if not archive.read(compile_assets[0]).startswith(b"MZ"):
            raise ValueError(f"Package {path.name} compile asset is empty or not a PE assembly")

        details: dict[str, Any] = {
            "authors": _required_child_text(metadata, "authors", path.name),
            "license": _required_child_text(metadata, "license", path.name),
            "license_type": _children(metadata, "license")[0].attrib.get("type", ""),
            "readme": _required_child_text(metadata, "readme", path.name),
            "project_url": _required_child_text(metadata, "projectUrl", path.name),
            "description": _required_child_text(metadata, "description", path.name),
            "tags": tuple(_required_child_text(metadata, "tags", path.name).replace(";", " ").split()),
            "repository_type": repository_nodes[0].attrib.get("type", "").strip(),
            "repository_url": repository_nodes[0].attrib.get("url", "").strip(),
            "entries": {entry.filename for entry in entries},
        }
        return (
            PackageMetadata(
                path=path,
                package_id=package_id,
                version=version,
                source_sha=source_sha,
                dependencies=dependencies,
                framework_references=tuple(framework_references),
            ),
            details,
        )


def expected_dependencies(package: ManifestPackage, version: str) -> dict[str, str]:
    """Resolve the candidate-version placeholder to exact internal ranges."""

    return {
        dependency_id: f"[{version}]" if dependency_version == "{version}" else dependency_version
        for dependency_id, dependency_version in package.dependencies.items()
    }


def validate_packages(
    package_directory: Path,
    expected_version: str | None = None,
    expected_source_sha: str | None = None,
    manifest_path: Path = MANIFEST,
) -> tuple[tuple[PackageMetadata, ...], str, str]:
    """Validate exact inventory, metadata, provenance, dependencies, and assets."""

    manifest = load_manifest(manifest_path)
    directory = package_directory.resolve()
    if not directory.is_dir():
        raise ValueError(f"Package directory does not exist: {directory}")
    archives = sorted(
        path for path in directory.glob("*.nupkg") if ".symbols." not in path.name.casefold()
    )
    if len(archives) != len(manifest.packages):
        raise ValueError(
            f"Expected exactly {len(manifest.packages)} release packages, found {len(archives)}"
        )

    source_sha = current_source_sha() if expected_source_sha is None else expected_source_sha
    if not SOURCE_SHA_PATTERN.fullmatch(source_sha):
        raise ValueError(f"Expected source SHA is invalid: {source_sha!r}")

    by_manifest_id = {package.package_id: package for package in manifest.packages}
    actual: dict[str, PackageMetadata] = {}
    versions: set[str] = set()
    for archive in archives:
        package, details = _read_package(archive, manifest)
        if package.package_id not in by_manifest_id or package.package_id in actual:
            raise ValueError(f"Unexpected or duplicate release package id: {package.package_id}")
        expected_name = f"{package.package_id}.{package.version}.nupkg"
        if archive.name != expected_name:
            raise ValueError(f"Package filename {archive.name!r} must be exactly {expected_name!r}")
        common = manifest.metadata
        expected_detail = {
            "authors": common.authors,
            "license": common.license,
            "license_type": "expression",
            "readme": common.readme,
            "project_url": common.project_url,
            "description": common.description,
            "tags": common.tags,
            "repository_type": common.repository_type,
            "repository_url": common.repository_url,
        }
        for key, expected in expected_detail.items():
            if details[key] != expected:
                raise ValueError(
                    f"Package {package.package_id} has {key}={details[key]!r}, expected {expected!r}"
                )
        if common.readme not in details["entries"]:
            raise ValueError(f"Package {package.package_id} declares a missing readme")
        if package.source_sha != source_sha:
            raise ValueError(
                f"Package {package.package_id} source commit {package.source_sha!r} does not match {source_sha!r}"
            )
        actual[package.package_id] = package
        versions.add(package.version)

    if set(actual) != set(by_manifest_id):
        raise ValueError(f"Package IDs differ from manifest: {sorted(actual)}")
    if len(versions) != 1:
        raise ValueError(f"All Projects packages must share one version; found {sorted(versions)}")
    version = versions.pop()
    if expected_version is not None and version != expected_version:
        raise ValueError(f"Packed version {version!r} does not match expected version {expected_version!r}")

    for package_id, package in actual.items():
        manifest_package = by_manifest_id[package_id]
        expected = expected_dependencies(manifest_package, version)
        if package.dependencies != expected:
            missing = sorted(set(expected) - set(package.dependencies))
            unexpected = sorted(set(package.dependencies) - set(expected))
            wrong = sorted(
                dependency_id
                for dependency_id in set(expected) & set(package.dependencies)
                if expected[dependency_id] != package.dependencies[dependency_id]
            )
            raise ValueError(
                f"Package {package_id} dependency graph differs from the manifest; "
                f"missing={missing}, unexpected={unexpected}, wrong_versions={wrong}"
            )
        if package.framework_references != manifest_package.framework_references:
            raise ValueError(
                f"Package {package_id} framework references differ from the manifest: "
                f"{package.framework_references!r}"
            )

    return tuple(actual[package.package_id] for package in manifest.packages), version, source_sha


def normalize_internal_dependency_ranges(
    package_directory: Path,
    version: str,
    manifest_path: Path = MANIFEST,
) -> None:
    """Rewrite Projects-to-Projects dependency ranges to exact candidate ranges."""

    manifest = load_manifest(manifest_path)
    internal_ids = {package.package_id for package in manifest.packages}
    for path in sorted(package_directory.resolve().glob("*.nupkg")):
        if ".symbols." in path.name.casefold():
            continue
        with zipfile.ZipFile(path, "r") as source:
            entries = _safe_entries(source, path)
            nuspecs = [entry for entry in entries if entry.filename.casefold().endswith(".nuspec")]
            if len(nuspecs) != 1:
                raise ValueError(f"Package {path.name} must contain exactly one nuspec")
            payloads = {entry.filename: source.read(entry) for entry in entries}
            nuspec_bytes = payloads[nuspecs[0].filename]
            upper = nuspec_bytes.upper()
            if b"<!DOCTYPE" in upper or b"<!ENTITY" in upper:
                raise ValueError(f"Package {path.name} contains unsafe XML declarations")
            try:
                root = ET.fromstring(nuspec_bytes)
            except ET.ParseError as exc:
                raise ValueError(f"Package {path.name} contains malformed nuspec XML") from exc
            if _local_name(root) != "package" or len(_children(root, "metadata")) != 1:
                raise ValueError(f"Package {path.name} has an invalid nuspec structure")
            for dependency in (element for element in root.iter() if _local_name(element) == "dependency"):
                if dependency.attrib.get("id") in internal_ids:
                    dependency.set("version", f"[{version}]")
            if root.tag.startswith("{"):
                ET.register_namespace("", root.tag[1:].split("}", 1)[0])
            payloads[nuspecs[0].filename] = ET.tostring(root, encoding="utf-8", xml_declaration=True)

            file_descriptor, temporary_name = tempfile.mkstemp(
                prefix=f".{path.name}.", suffix=".tmp", dir=path.parent
            )
            os.close(file_descriptor)
            temporary = Path(temporary_name)
            try:
                with zipfile.ZipFile(temporary, "w") as destination:
                    for entry in entries:
                        destination.writestr(entry, payloads[entry.filename])
                os.replace(temporary, path)
            finally:
                temporary.unlink(missing_ok=True)
