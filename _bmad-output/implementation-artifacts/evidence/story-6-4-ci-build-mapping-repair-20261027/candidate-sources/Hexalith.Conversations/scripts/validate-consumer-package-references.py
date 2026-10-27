#!/usr/bin/env python3
"""Restore, build, and test package-only Conversations consumers."""

from __future__ import annotations

import argparse
import os
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path
from xml.sax.saxutils import quoteattr
from xml.etree import ElementTree

from release_contract import EXACT_PACKAGE_IDS, ROOT


def package_version(package_directory: Path) -> str:
    """Read and prove the shared version from the exact local package inventory."""
    versions: dict[str, str] = {}
    package_paths = sorted(package_directory.glob("*.nupkg"))
    if len(package_paths) != len(EXACT_PACKAGE_IDS):
        raise ValueError(
            f"local consumer feed must contain exactly {len(EXACT_PACKAGE_IDS)} packages; "
            f"found {len(package_paths)}"
        )
    for package_path in package_paths:
        with zipfile.ZipFile(package_path) as archive:
            nuspecs = [name for name in archive.namelist() if name.endswith(".nuspec")]
            if len(nuspecs) != 1:
                raise ValueError(f"{package_path.name}: expected exactly one nuspec")
            root = ElementTree.fromstring(archive.read(nuspecs[0]))
            namespace = {"n": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}
            id_path = ".//n:metadata/n:id" if namespace else ".//metadata/id"
            version_path = ".//n:metadata/n:version" if namespace else ".//metadata/version"
            id_element = root.find(id_path, namespace)
            version_element = root.find(version_path, namespace)
            if id_element is None or version_element is None or not id_element.text or not version_element.text:
                raise ValueError(f"{package_path.name}: id and version are required")
            package_id = id_element.text.strip()
            if package_id in versions:
                raise ValueError(f"local consumer feed contains duplicate package id: {package_id}")
            versions[package_id] = version_element.text.strip()
    if set(versions) != set(EXACT_PACKAGE_IDS):
        raise ValueError(f"local consumer feed does not equal the exact inventory: {sorted(versions)}")
    distinct = set(versions.values())
    if len(distinct) != 1:
        raise ValueError(f"local packages do not share one version: {sorted(distinct)}")
    return next(iter(distinct))


def catalog_versions() -> dict[str, str]:
    """Read test-tool versions from the pinned Builds catalog."""
    catalog = ROOT / "references" / "Hexalith.Builds" / "Props" / "Directory.Packages.props"
    root = ElementTree.parse(catalog).getroot()
    versions = {
        element.attrib["Include"]: element.attrib["Version"]
        for element in root.findall(".//PackageVersion")
        if "Include" in element.attrib and "Version" in element.attrib
    }
    required = ("Microsoft.NET.Test.Sdk", "xunit.v3", "xunit.runner.visualstudio")
    missing = [package_id for package_id in required if package_id not in versions]
    if missing:
        raise ValueError(f"Builds catalog is missing test-tool versions: {missing}")
    return versions


def write_text(path: Path, content: str) -> None:
    """Write normalized temporary consumer input."""
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def write_nuget_config(root: Path, package_directory: Path) -> Path:
    config = root / "NuGet.Config"
    package_source = quoteattr(str(package_directory))
    write_text(
        config,
        f"""<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-conversations" value={package_source} />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
""",
    )
    return config


def write_public_consumer(root: Path, version: str) -> Path:
    project = root / "public-consumer" / "PublicConsumer.csproj"
    write_text(
        project,
        f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Hexalith.Conversations" Version="{version}" />
    <PackageReference Include="Hexalith.Conversations.Client" Version="{version}" />
    <PackageReference Include="Hexalith.Conversations.Contracts" Version="{version}" />
  </ItemGroup>
</Project>
""",
    )
    write_text(
        project.parent / "Program.cs",
        """using Hexalith.Conversations;
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts;

Type[] publicPackageMarkers =
[
    typeof(ConversationsAssemblyMarker),
    typeof(ClientAssemblyMarker),
    typeof(ContractsAssemblyMarker),
];

if (publicPackageMarkers.Any(marker => !marker.Assembly.GetName().Name!.StartsWith("Hexalith.Conversations", StringComparison.Ordinal)))
{
    throw new InvalidOperationException("The package-only public surface could not be loaded.");
}
""",
    )
    return project


def write_testing_consumer(root: Path, version: str, versions: dict[str, str]) -> Path:
    project = root / "testing-consumer" / "TestingConsumer.csproj"
    write_text(
        project,
        f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Hexalith.Conversations.Testing" Version="{version}" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="{versions['Microsoft.NET.Test.Sdk']}" />
    <PackageReference Include="xunit.v3" Version="{versions['xunit.v3']}" />
    <PackageReference Include="xunit.runner.visualstudio" Version="{versions['xunit.runner.visualstudio']}" PrivateAssets="all" />
  </ItemGroup>
</Project>
""",
    )
    write_text(
        project.parent / "PackageSmokeTests.cs",
        """using Hexalith.Conversations.Testing;

using Xunit;

public sealed class PackageSmokeTests
{
    [Fact]
    public void Testing_package_loads_without_source_projects()
    {
        Assert.Equal("Hexalith.Conversations.Testing", typeof(TestingAssemblyMarker).Assembly.GetName().Name);
    }
}
""",
    )
    return project


def assert_package_only(project: Path) -> None:
    text = project.read_text(encoding="utf-8")
    if "ProjectReference" in text:
        raise ValueError(f"temporary consumer contains a source reference: {project}")


MAPPING_ENVIRONMENT = {
    "custombeforedirectorybuildprops", "customaftermicrosoftcommonprops",
    "custombeforedirectorybuildtargets", "customafterdirectorybuildtargets",
    "aftermicrosoftnetsdktargets", "hexalith_source_mapping_hash", "configuration",
}


def isolated_consumer_environment(parent: dict[str, str]) -> dict[str, str]:
    """Keep standalone package consumers independent of the caller's generated imports."""
    return {name: value for name, value in parent.items() if name.lower() not in MAPPING_ENVIRONMENT}


def run_dotnet(arguments: list[str], working_directory: Path, environment: dict[str, str]) -> None:
    subprocess.run(["dotnet", *arguments], cwd=working_directory, env=environment, check=True)


def validate_consumer(project: Path, config: Path, environment: dict[str, str], is_test_project: bool) -> None:
    assert_package_only(project)
    run_dotnet(
        [
            "restore",
            str(project),
            "-p:Configuration=Release",
            "--configfile",
            str(config),
            "-p:NuGetAudit=true",
            "-p:NuGetAuditMode=all",
        ],
        project.parent,
        environment,
    )
    run_dotnet(
        ["build", str(project), "--no-restore", "--configuration", "Release", "-warnaserror"],
        project.parent,
        environment,
    )
    assembly = project.parent / "bin" / "Release" / "net10.0" / f"{project.stem}.dll"
    runner_arguments = ["-parallelMode", "none", "-noLogo"] if is_test_project else []
    run_dotnet([str(assembly), *runner_arguments], project.parent, environment)


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate package-only consumer restore and build.")
    parser.add_argument("package_directory", type=Path)
    parser.add_argument("--work-directory", type=Path)
    args = parser.parse_args()

    package_directory = args.package_directory.resolve()
    version = package_version(package_directory)
    versions = catalog_versions()

    temporary: tempfile.TemporaryDirectory[str] | None = None
    if args.work_directory is None:
        temporary = tempfile.TemporaryDirectory(prefix="hexalith-conversations-consumer-")
        work_directory = Path(temporary.name)
    else:
        work_directory = args.work_directory.resolve()
        if work_directory == Path(work_directory.anchor) or work_directory == ROOT:
            raise ValueError(f"unsafe consumer work directory: {work_directory}")
        if work_directory.exists():
            raise ValueError(f"consumer work directory must not already exist: {work_directory}")
        work_directory.mkdir(parents=True)

    try:
        config = write_nuget_config(work_directory, package_directory)
        environment = isolated_consumer_environment(os.environ.copy())
        environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1"
        environment["MSBUILDDISABLENODEREUSE"] = "1"
        environment["NUGET_PACKAGES"] = str(work_directory / ".nuget" / "packages")
        validate_consumer(write_public_consumer(work_directory, version), config, environment, False)
        validate_consumer(write_testing_consumer(work_directory, version, versions), config, environment, True)
    finally:
        if temporary is not None:
            temporary.cleanup()

    print(f"Validated package-only public and testing consumers at {version}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as exc:
        print(f"Consumer validation failed with exit code {exc.returncode}.", file=sys.stderr)
        raise SystemExit(exc.returncode)
    except Exception as exc:  # noqa: BLE001 - command-line validation must report one concise failure.
        print(f"Consumer validation failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
