#!/usr/bin/env python3
"""Build isolated real-API consumers for every Projects release candidate."""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

from _nuget import ROOT, ManifestPackage, load_manifest, validate_packages


def parse_arguments() -> argparse.Namespace:
    """Parse the package candidate directory."""

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_directory", type=Path)
    return parser.parse_args()


def render_consumer_project(package: ManifestPackage, version: str) -> str:
    """Render one exact-version executable package consumer."""

    project = ET.Element("Project", {"Sdk": "Microsoft.NET.Sdk"})
    properties = ET.SubElement(project, "PropertyGroup")
    ET.SubElement(properties, "OutputType").text = "Exe"
    ET.SubElement(properties, "TargetFramework").text = "net10.0"
    ET.SubElement(properties, "ImplicitUsings").text = "enable"
    ET.SubElement(properties, "Nullable").text = "enable"
    dependencies = ET.SubElement(project, "ItemGroup")
    ET.SubElement(
        dependencies,
        "PackageReference",
        {"Include": package.package_id, "Version": f"[{version}]"},
    )
    ET.indent(project)
    return ET.tostring(project, encoding="unicode") + "\n"


def render_nuget_config(package_directory: Path) -> str:
    """Map Projects IDs exclusively to the candidate directory."""

    configuration = ET.Element("configuration")
    sources = ET.SubElement(configuration, "packageSources")
    ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", {"key": "candidates", "value": str(package_directory)})
    ET.SubElement(
        sources,
        "add",
        {"key": "nuget.org", "value": "https://api.nuget.org/v3/index.json"},
    )
    mapping = ET.SubElement(configuration, "packageSourceMapping")
    candidates = ET.SubElement(mapping, "packageSource", {"key": "candidates"})
    ET.SubElement(candidates, "package", {"pattern": "Hexalith.Projects*"})
    nuget = ET.SubElement(mapping, "packageSource", {"key": "nuget.org"})
    ET.SubElement(nuget, "package", {"pattern": "*"})
    ET.indent(configuration)
    return ET.tostring(configuration, encoding="unicode") + "\n"


def assert_package_only_assets(assets_path: Path, package_id: str, version: str) -> None:
    """Reject project fallbacks and unexpected Projects package versions."""

    with assets_path.open("r", encoding="utf-8") as handle:
        assets = json.load(handle)
    libraries = assets.get("libraries")
    if not isinstance(libraries, dict):
        raise ValueError(f"Consumer assets for {package_id} contain no libraries")
    project_libraries = sorted(
        name for name, details in libraries.items() if details.get("type") == "project"
    )
    if project_libraries:
        raise ValueError(f"Consumer {package_id} restored project inputs: {project_libraries}")
    expected_direct = f"{package_id}/{version}".casefold()
    if expected_direct not in {name.casefold() for name in libraries}:
        raise ValueError(f"Consumer did not restore exact direct package {package_id}/{version}")
    unexpected = sorted(
        name
        for name in libraries
        if name.casefold().startswith("hexalith.projects/")
        or name.casefold().startswith("hexalith.projects.")
        if name.rsplit("/", 1)[-1] != version
    )
    if unexpected:
        raise ValueError(f"Consumer {package_id} restored mismatched Projects candidates: {unexpected}")


def run_consumer(
    package: ManifestPackage,
    version: str,
    package_directory: Path,
    workspace: Path,
) -> None:
    """Restore and compile one isolated candidate consumer."""

    consumer = workspace / package.package_id
    consumer.mkdir()
    project = consumer / "Consumer.csproj"
    project.write_text(render_consumer_project(package, version), encoding="utf-8")
    (consumer / "Program.cs").write_text(package.probe, encoding="utf-8")
    config = workspace / "NuGet.Config"
    completed_environment = dict(os.environ)
    completed_environment["NUGET_PACKAGES"] = str(consumer / ".packages")
    subprocess.run(
        ["dotnet", "restore", str(project), "--configfile", str(config), "/nr:false"],
        cwd=ROOT,
        env=completed_environment,
        check=True,
    )
    assert_package_only_assets(consumer / "obj" / "project.assets.json", package.package_id, version)
    subprocess.run(
        [
            "dotnet",
            "build",
            str(project),
            "--configuration",
            "Release",
            "--no-restore",
            "-warnaserror",
            "/nr:false",
        ],
        cwd=ROOT,
        env=completed_environment,
        check=True,
    )
    print(f"Consumer compiled real API from {package.package_id} {version}.")


def main() -> int:
    """Validate candidates, then prove each works as an isolated package."""

    arguments = parse_arguments()
    package_directory = arguments.package_directory.resolve()
    packages, version, _ = validate_packages(package_directory)
    manifest = load_manifest()
    validated_ids = {package.package_id for package in packages}
    with tempfile.TemporaryDirectory(prefix="hexalith-projects-consumers-") as temporary:
        workspace = Path(temporary)
        (workspace / "NuGet.Config").write_text(
            render_nuget_config(package_directory),
            encoding="utf-8",
        )
        for package in manifest.packages:
            if package.package_id not in validated_ids:
                raise ValueError(f"Candidate was not validated before consumption: {package.package_id}")
            run_consumer(package, version, package_directory, workspace)
    print(f"Validated {len(manifest.packages)} isolated package consumers.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, subprocess.CalledProcessError, ValueError) as error:
        print(f"Consumer validation failed: {error}", file=sys.stderr)
        sys.exit(1)
