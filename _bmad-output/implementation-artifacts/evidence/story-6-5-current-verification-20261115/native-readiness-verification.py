"""Run the spec-selected current-source native lanes in disposable build output."""
from __future__ import annotations
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import time
import xml.etree.ElementTree as ET

ROOT = Path("/home/administrator/projects/hexalith/projects")
EVIDENCE = Path(__file__).resolve().parent / "native-readiness"
SCRATCH = Path(os.environ["TMPDIR"]) / "story65-native"
EVIDENCE.mkdir(mode=0o700)
SCRATCH.mkdir(mode=0o700)
ENV = os.environ.copy()
for key in ("CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps", "CustomBeforeDirectoryBuildTargets", "CustomAfterDirectoryBuildTargets", "AfterMicrosoftNETSdkTargets", "HEXALITH_SOURCE_MAPPING_HASH"):
    ENV.pop(key, None)
ENV.update(Configuration="Debug", UseHexalithProjectReferences="true", UseNuGetDeps="false",
           EnablePrivateCatalogInvocationProof="true", EnablePrivateOwnerSourceReadProof="true")
for module in ("Builds", "EventStore", "Tenants", "Folders", "Commons", "Conversations", "Memories", "FrontComposer", "Parties", "McpCli", "Platform"):
    ENV["Hexalith" + module + "Root"] = str(ROOT / "references" / ("Hexalith." + module))
ENV["HexalithTenantsBasePath"] = str(ROOT / "references/Hexalith.Tenants/src")
REPORT = {"revision": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(), "commands": [], "lanes": {}, "complete": False}

def invoke(label, args, cwd=ROOT):
    started = time.time()
    result = subprocess.run(args, cwd=cwd, env=ENV, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=900)
    log = result.stdout
    (EVIDENCE / (label + ".log")).write_text(log)
    REPORT["commands"].append({"label": label, "argv": args, "cwd": str(cwd), "exitCode": result.returncode, "elapsedSeconds": time.time()-started, "logSha256": hashlib.sha256(log.encode()).hexdigest()})
    (EVIDENCE / "run.json").write_text(json.dumps(REPORT, indent=2) + "\n")
    print(label + ": " + str(result.returncode), flush=True)
    if result.returncode:
        raise RuntimeError(label + " failed; inspect its retained log")
    return log

try:
    mapping = json.loads(invoke("source-preparation", ["dotnet", "run", "--project", str(ROOT / "references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj"), "--artifacts-path", str(SCRATCH / "tool-artifacts"), "--", "prepare-build", "--manifest", "module/hexalith.module-manifest.v2.json", "--mode", "source", "--workspace", str(SCRATCH / "mapping"), "--output", "json"]))
    selected = mapping["buildPreparation"]["environment"]
    if mapping["status"] != "prepared" or set(selected) != {"CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps", "CustomBeforeDirectoryBuildTargets", "CustomAfterDirectoryBuildTargets", "AfterMicrosoftNETSdkTargets", "HEXALITH_SOURCE_MAPPING_HASH", "Configuration"} or selected["Configuration"] != "Debug":
        raise RuntimeError("Exact source mapping did not prepare")
    ENV.update(selected)
    REPORT["mappingEnvironment"] = selected
    historical = ROOT / "_bmad-output/implementation-artifacts/evidence/story-6-5-canonical-hash-memo-qualified-20261108/native/final"
    lanes = [("integration", "Hexalith.Projects.Integration.Tests", ROOT / "tests/Hexalith.Projects.Integration.Tests", "05-projects-native-4.json")]
    views = {}
    for kind, repository in (("projects", ROOT), ("owner", ROOT / "references/Hexalith.EventStore")):
        view = SCRATCH / (kind + "-source-view")
        view.mkdir()
        for entry in repository.iterdir():
            if entry.name not in ("bin", "obj", ".git"):
                (view / entry.name).symlink_to(entry, target_is_directory=entry.is_dir())
        views[kind] = view
    REPORT["sourceViews"] = {kind: {"view": str(view), "source": str(ROOT if kind == "projects" else ROOT / "references/Hexalith.EventStore")} for kind, view in views.items()}
    for label, name, project, receipt in lanes:
        artifacts = views["owner" if label == "owner" else "projects"] / ".build/artifacts"
        build = ["dotnet", "build", "--configuration", "Debug", "-m:1", "-p:NuGetAudit=false", "--disable-build-servers", "--artifacts-path", str(artifacts)]
        if label == "integration":
            build.append("-p:SupportedProjectReadDistributed=true")
        invoke(label + "-build", build, project)
        candidates = list((artifacts / "bin" / name).glob("debug*/" + name + ".dll"))
        if len(candidates) != 1:
            raise RuntimeError("Fresh native assembly missing or ambiguous: " + label)
        assembly = candidates[0]
        old = json.loads((historical / receipt).read_text())["argv"]
        selectors = old[2:old.index("-xml")]
        command = ["dotnet", str(assembly), *selectors, "-xml", str(EVIDENCE / (label + ".xml"))]
        invoke(label + "-tests", command)
        native = ET.parse(EVIDENCE / (label + ".xml")).getroot().findall("assembly")
        rows = [test for item in native for test in item.findall(".//test")]
        counts = {key: sum(int(item.get(key, "0")) for item in native) for key in ("total", "passed", "failed", "skipped", "errors", "not-run")}
        if not rows or len(rows) != counts["total"] or counts["total"] != counts["passed"] or any(counts[key] for key in ("failed", "skipped", "errors", "not-run")) or any(test.get("result") != "Pass" for test in rows):
            raise RuntimeError("Native lane did not pass without omissions: " + label)
        REPORT["lanes"][label] = {"assemblySha256": hashlib.sha256(assembly.read_bytes()).hexdigest(), "counts": counts, "selectors": selectors}
        print(label + ": " + str(counts["passed"]) + " passed", flush=True)
    REPORT["complete"] = True
except Exception as failure:
    REPORT["failure"] = str(failure)
    raise
finally:
    (EVIDENCE / "run.json").write_text(json.dumps(REPORT, indent=2) + "\n")
