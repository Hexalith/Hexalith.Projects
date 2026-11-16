#!/usr/bin/env python3
"""Replay qualification under run-with-temp into a fresh HEXALITH_QUALIFICATION_OUTPUT."""
import datetime
import hashlib
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

PACKET = Path(__file__).resolve().parent
ROOT = PACKET.parents[3]
SOURCE = "0be0c922883d4ea61b18e843e24472162261d934"
REMOTE = "107e50e39cf2545cbc6546e60e02252251aca274"
BUILDS = "e2bbbd0893f586e4d447ef8358ff747ea3d34435"
INSPECT_VERSION = "0.27.0"
INSPECT = ["dnx", "dotnet-inspect@" + INSPECT_VERSION, "-y", "--"]


def sha(data):
    return hashlib.sha256(data).hexdigest()


def test_counts(xml, expected_total=None):
    assemblies = ET.parse(xml).getroot().findall("assembly")
    if not assemblies:
        raise ValueError("Missing xUnit assembly results: " + str(xml))
    counts = {name: sum(int(item.attrib[name]) for item in assemblies)
              for name in ("total", "passed", "failed", "skipped", "errors", "not-run")}
    if (counts["total"] < 1 or counts["passed"] != counts["total"]
            or any(counts[name] for name in ("failed", "skipped", "errors", "not-run"))
            or (expected_total is not None and counts["total"] != expected_total)):
        raise ValueError("Incomplete or failing test results: " + str(counts))
    return {**counts, "xml": xml.name, "sha256": sha(xml.read_bytes())}


def main():
    out = Path(os.environ.get("HEXALITH_QUALIFICATION_OUTPUT", str(PACKET))).resolve()
    # Refuse before scratch creation, package reads or command/log writes.
    if out.exists():
        raise FileExistsError("Refusing existing qualification output: " + str(out))
    scratch = Path(os.environ["TMPDIR"]).resolve()
    if out.is_relative_to(scratch):
        raise ValueError("Qualification output must survive TMPDIR cleanup")
    out.mkdir(parents=True, exist_ok=False)
    work = scratch / "eventstore-contracts"
    env = os.environ.copy()
    env.update(NUGET_PACKAGES=str(scratch / "packages"), DOTNET_CLI_USE_MSBUILD_SERVER="0",
               DOTNET_CLI_HOME=str(scratch / "dotnet-home"))
    config = scratch / "NuGet.qualification.Config"
    config.write_text('''<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" /></packageSources>
  <packageSourceMapping><clear /><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping>
</configuration>
''')
    env["RestoreConfigFile"] = str(config)
    result = {"recorded_at": datetime.datetime.now(datetime.timezone.utc).isoformat(),
              "source": SOURCE, "builds_source": BUILDS, "version": "5.3.0",
              "package": "Hexalith.EventStore.Contracts", "published": False,
              "inspection_tool": {"package": "dotnet-inspect", "version": INSPECT_VERSION},
              "restore_config": config.read_text(), "private_nuget_cache": "${TMPDIR}/packages",
              "steps": [], "status": "running"}

    def save():
        (out / "eventstore-contracts-candidate-qualification.json").write_text(json.dumps(result, indent=2) + "\n")

    def run(label, command, cwd=None, required=True, suffix=".log"):
        log = out / (label + suffix)
        with log.open("xb") as stream:
            process = subprocess.run(command, cwd=cwd, env=env, stdout=stream, stderr=subprocess.STDOUT)
        entry = {"label": label, "command": command, "exit": process.returncode,
                 "required": required, "log": log.name, "sha256": sha(log.read_bytes())}
        result["steps"].append(entry)
        save()
        print(label, process.returncode, flush=True)
        if required and process.returncode:
            raise RuntimeError(label + " failed; inspect " + str(log))
        return entry

    def git(*arguments):
        return subprocess.check_output(["git", "-C", str(work), *arguments], env=env, text=True).strip()

    try:
        endpoint = "https://api.nuget.org/v3-flatcontainer/hexalith.eventstore.contracts/5.3.0/hexalith.eventstore.contracts.5.3.0.nupkg"
        try:
            with urllib.request.urlopen(urllib.request.Request(endpoint, method="HEAD"), timeout=60) as response:
                status = response.status
        except urllib.error.HTTPError as error:
            status = error.code
            if status != 404:
                raise
        result["coordinate_availability"] = {
            "recorded_at": datetime.datetime.now(datetime.timezone.utc).isoformat(),
            "approved_feed": "https://api.nuget.org/v3/index.json", "endpoint": endpoint,
            "coordinate": "Hexalith.EventStore.Contracts/5.3.0", "status": status, "present": status == 200}
        save()
        if status != 404:
            raise RuntimeError("Candidate coordinate is already published; a new owner decision is required")
        repo = ROOT / "references/Hexalith.EventStore"
        run("eventstore-candidate-clone", ["git", "clone", "--shared", "--no-checkout", str(repo), str(work)])
        run("eventstore-candidate-remote", ["git", "remote", "set-url", "origin", "https://github.com/Hexalith/Hexalith.EventStore.git"], work)
        run("eventstore-candidate-current-source", ["git", "-c", "submodule.recurse=false", "fetch", "--no-recurse-submodules", "--no-tags", "origin", REMOTE], work)
        run("eventstore-candidate-checkout", ["git", "-c", "submodule.recurse=false", "checkout", "--detach", SOURCE], work)
        result["source_tree"] = git("rev-parse", "HEAD^{tree}")
        result["remote_query_diff"] = git("diff", SOURCE, REMOTE, "--", "src/Hexalith.EventStore.Contracts/Queries", "src/Hexalith.EventStore.RestApi.Generators")
        result["remote_complete_tree_diff"] = git("diff", "--name-status", SOURCE, REMOTE)
        if git("rev-parse", SOURCE + ":references/Hexalith.Builds") != BUILDS:
            raise ValueError("Producer Builds gitlink differs from the pinned catalog")
        target = work / "references/Hexalith.Builds"
        target.mkdir(parents=True, exist_ok=True)
        builds_repo = ROOT / "references/Hexalith.Builds"
        archive = subprocess.check_output(["git", "-C", str(builds_repo), "archive", BUILDS])
        with tarfile.open(fileobj=io.BytesIO(archive)) as tar:
            tar.extractall(target, filter="data")
        result["builds_materialization"] = "ordinary exact Git archive, no submodule initialization"
        result["builds_package_catalog"] = {
            "source": BUILDS, "path": "Props/Directory.Packages.props",
            "sha256": sha((target / "Props/Directory.Packages.props").read_bytes()),
            "git_blob": subprocess.check_output(["git", "-C", str(builds_repo), "rev-parse", BUILDS + ":Props/Directory.Packages.props"], text=True).strip(),
            "archive_sha256": sha(archive)}
        project = "src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj"
        common = ["--configuration", "Release", "-p:UseHexalithProjectReferences=false", "-p:Version=5.3.0",
                  "-p:GeneratePackageOnBuild=false", "-p:RestoreConfigFile=" + str(config), "-m:1", "--disable-build-servers"]
        run("eventstore-contracts-candidate-pack", ["dotnet", "pack", project, *common, "--output", str(work / "candidate-feed"), "-p:RepositoryCommit=" + SOURCE], work)
        artifact = work / "candidate-feed/Hexalith.EventStore.Contracts.5.3.0.nupkg"
        retained = out / "unpublished-candidates" / artifact.name
        retained.parent.mkdir()
        shutil.copy2(artifact, retained)
        with zipfile.ZipFile(artifact) as package:
            names = package.namelist()
            if len(names) != len(set(names)):
                raise ValueError("Duplicate candidate ZIP entries")
            nuspec = package.read("Hexalith.EventStore.Contracts.nuspec")
            (out / "eventstore-contracts-candidate.nuspec").write_bytes(nuspec)
            dll = package.read("lib/net11.0/Hexalith.EventStore.Contracts.dll")
            extracted = work / "candidate-api/Hexalith.EventStore.Contracts.dll"
            extracted.parent.mkdir()
            extracted.write_bytes(dll)
        repository = ET.fromstring(nuspec).find("{*}metadata/{*}repository")
        if repository.attrib.get("commit") != SOURCE:
            raise ValueError("Candidate nuspec source binding differs")
        result["artifact"] = {
            "path": str(retained.relative_to(out)), "sha256": sha(retained.read_bytes()), "bytes": retained.stat().st_size,
            "files": names, "signed": ".signature.p7s" in names, "dll_sha256": sha(dll),
            "source_commit_embedded_utf8": SOURCE.encode() in dll,
            "repository_commit_in_nuspec": repository.attrib["commit"], "repository_url": repository.attrib["url"],
            "signing_status": "unsigned local qualification candidate; owner publication must use approved signing/provenance process"}
        save()
        run("eventstore-contracts-candidate-signature", ["dotnet", "nuget", "verify", str(retained), "--all"], work, required=False)
        version = run("dotnet-inspect-version", [*INSPECT, "--version"], work)
        result["inspection_tool"]["actual_version_output"] = (out / version["log"]).read_text().strip()
        if INSPECT_VERSION not in result["inspection_tool"]["actual_version_output"]:
            raise ValueError("Inspection tool version differs from the pin")
        run("eventstore-contracts-candidate-api", [*INSPECT, "member", "QueryResultContractAttribute", "--library", str(extracted), "--json"], work, suffix=".json")
        library = run("eventstore-contracts-candidate-library", [*INSPECT, "library", str(extracted), "--json"], work, suffix=".json")
        library_data = json.loads((out / library["log"]).read_text())
        information = library_data["assembly_info"]["informational_version"]
        if information != "5.3.0+" + SOURCE:
            raise ValueError("Candidate informational version differs from the exact source")
        result["artifact"].update({"informational_version": information, "metadata_log": library["log"],
                                   "metadata_exit": library["exit"], "metadata_sha256": library["sha256"]})
        public_url = "https://api.nuget.org/v3-flatcontainer/hexalith.eventstore.contracts/5.1.1/hexalith.eventstore.contracts.5.1.1.nupkg"
        public_package = work / "published-5.1.1.nupkg"
        with urllib.request.urlopen(public_url, timeout=60) as response:
            public_package.write_bytes(response.read())
        public_dll = work / "published-5.1.1.dll"
        with zipfile.ZipFile(public_package) as package:
            public_dll.write_bytes(package.read("lib/net11.0/Hexalith.EventStore.Contracts.dll"))
        comparison = run("eventstore-contracts-published-to-candidate-api-diff", [*INSPECT, "diff", "--library", str(public_dll) + ".." + str(extracted), "--json"], work, suffix=".json")
        diff = json.loads((out / comparison["log"]).read_text())
        summary = diff["document"]["summary"]
        if diff["outcome"] != "available" or summary["breakingCount"] or summary["potentiallyBreakingCount"]:
            raise ValueError("Published-to-candidate API comparison is unavailable or breaking")
        result["binary_compatibility"] = {**comparison, "summary": summary,
            "public_package_url": public_url, "public_archive_sha256": sha(public_package.read_bytes()),
            "public_dll_sha256": sha(public_dll.read_bytes())}
        result["tests"] = {}
        cases = [
            ("eventstore-contracts-candidate", "Hexalith.EventStore.Contracts.Tests", [], None),
            ("eventstore-query-generator-candidate", "Hexalith.EventStore.RestApi.Generators.Tests", ["-class", "*TypedGatewayQueryGenerationTests"], 2),
        ]
        for label, name, selectors, expected_total in cases:
            run(label + "-test-build", ["dotnet", "build", f"tests/{name}/{name}.csproj", *common], work)
            xml = out / (label + "-tests.xml")
            run(label + "-test-run", ["dotnet", str(work / f"tests/{name}/bin/Release/net11.0/{name}.dll"), *selectors, "-xml", str(xml)], work)
            result["tests"][name] = test_counts(xml, expected_total)
            save()
        identities = {}
        for assets in work.rglob("project.assets.json"):
            if "obj" in assets.parts and "references" not in assets.relative_to(work).parts:
                data = json.loads(assets.read_text())
                identities[str(assets.relative_to(work))] = sorted(k for k, v in data["libraries"].items() if v["type"] == "package")
        if not identities:
            raise ValueError("No resolved package identities were retained")
        result["resolved_package_identities"] = identities
        result["publication_readiness"] = "qualification-only: unsigned candidate; owner approval and approved publication/signature process still required"
        result["status"] = "passed"
        return 0
    except Exception as error:
        result["status"] = "failed"
        result["error"] = str(error)
        return 1
    finally:
        save()


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError) as error:
        print(str(error), file=sys.stderr)
        sys.exit(1)
