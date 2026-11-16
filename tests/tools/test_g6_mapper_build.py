"""Exercise the workflow's mapper build/execute boundary and diagnostic retention."""

import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import textwrap
import unittest


ROOT = Path(__file__).resolve().parents[2]


class MapperBuildBoundaryTests(unittest.TestCase):
    def run_prefix(self, build_failure=False, prepare_failure=False):
        source = (ROOT / ".github/workflows/ci.yml").read_text()
        source = source.split("      - name: Restore effective G-6 package graphs\n", 1)[1]
        source = source.split("          python3 - \"$mapping_json\"", 1)[0]
        script = textwrap.dedent(source.split("        run: |\n", 1)[1])
        with tempfile.TemporaryDirectory(prefix="projects-mapper-boundary-") as temporary:
            root = Path(temporary)
            for name, sdk in (("Hexalith.Builds", "11.0.100"), ("Hexalith.Conversations", "10.0.403")):
                directory = root / "references" / name
                directory.mkdir(parents=True)
                (directory / "global.json").write_text(json.dumps({"sdk": {"version": sdk}}))
            tools = root / "tools"
            tools.mkdir()
            stub = tools / "dotnet"
            stub.write_text(f"#!{sys.executable}\n" + textwrap.dedent('''\
                import json, os, pathlib, sys
                cwd = pathlib.Path.cwd()
                args = sys.argv[1:]
                sdk = json.loads((cwd / "global.json").read_text())["sdk"]["version"]
                if args[0] == "build":
                    assert sdk == "11.0.100", "mapper compilation selected the consumer SDK"
                    print("MSBuild output must remain outside the mapping JSON")
                    print("retained compiler diagnostic", file=sys.stderr)
                    if os.environ["BUILD_FAILURE"] == "1": sys.exit(17)
                    output = cwd / "src/libraries/Hexalith.Builds.Module.Cli/bin/Release/net11.0/Hexalith.Builds.Module.Cli.dll"
                    output.parent.mkdir(parents=True)
                    output.write_text("synthetic executable marker")
                else:
                    assert sdk == "10.0.403", "mapper execution lost the consumer context"
                    assert pathlib.Path(args[0]).is_file()
                    assert args[1] == "prepare-build"
                    if os.environ["PREPARE_FAILURE"] == "1":
                        print("retained prerequisite refusal", file=sys.stderr)
                        sys.exit(23)
                    print(json.dumps({"status": "prepared", "buildPreparation": {"environment": {}}}))
                '''))
            stub.chmod(0o755)
            environment = dict(os.environ, PATH=str(tools) + os.pathsep + os.environ["PATH"],
                               RUNNER_TEMP=str(root / "runner"), G6_OUTPUT=str(root / "evidence"),
                               BUILD_FAILURE=str(int(build_failure)), PREPARE_FAILURE=str(int(prepare_failure)))
            result = subprocess.run(["bash", "-c", script], cwd=root, env=environment,
                                    capture_output=True, text=True, timeout=20)
            retained = {p.name: p.read_text() for p in (root / "evidence/source-mapping").iterdir()}
            return result, retained

    def test_build_uses_owner_sdk_and_execution_keeps_consumer_context(self):
        result, retained = self.run_prefix()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("prepared", json.loads(retained["conversations.json"])["status"])
        self.assertIn("MSBuild output", retained["build.stdout.log"])
        self.assertNotIn("MSBuild", retained["conversations.json"])

    def test_build_failure_retains_diagnostics_and_prevents_execution(self):
        result, retained = self.run_prefix(build_failure=True)
        self.assertEqual(17, result.returncode)
        self.assertIn("retained compiler diagnostic", retained["build.stderr.log"])
        self.assertNotIn("conversations.json", retained)

    def test_prerequisite_refusal_preserves_its_exit_code_and_diagnostics(self):
        result, retained = self.run_prefix(prepare_failure=True)
        self.assertEqual(23, result.returncode)
        self.assertIn("retained prerequisite refusal", retained["prepare.stderr.log"])

class LiveRunnerMapperBuildBoundaryTests(MapperBuildBoundaryTests):
    """Execute the actual live-runner function against distinct owner/consumer SDKs."""

    def run_prefix(self, build_failure=False, prepare_failure=False):
        import ast
        import __future__
        import types

        source_path = ROOT / "tools/qualification/run_g6_current.py"
        tree = ast.parse(source_path.read_text())
        function = next(node for node in tree.body if isinstance(node, ast.FunctionDef)
                        and node.name == "conversations_source_mapping")
        with tempfile.TemporaryDirectory(prefix="projects-live-mapper-boundary-") as temporary:
            root = Path(temporary)
            builds = root / "references/Hexalith.Builds"
            consumer = root / "references/Hexalith.Conversations"
            for directory, sdk in ((builds, "11.0.100"), (consumer, "10.0.403")):
                directory.mkdir(parents=True)
                (directory / "global.json").write_text(json.dumps({"sdk": {"version": sdk}}))
            cli = builds / "src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj"
            cli.parent.mkdir(parents=True)
            cli.write_text("<Project />")
            assembly = cli.parent / "bin/Release/net11.0/Hexalith.Builds.Module.Cli.dll"
            assembly.parent.mkdir(parents=True)
            assembly.write_text("stale marker; must never execute after a compiler failure")
            tools = root / "tools"
            tools.mkdir()
            stub = tools / "dotnet"
            stub.write_text(f"#!{sys.executable}\n" + textwrap.dedent('''\
                import json, os, pathlib, sys
                cwd = pathlib.Path.cwd()
                args = sys.argv[1:]
                sdk = json.loads((cwd / "global.json").read_text())["sdk"]["version"]
                if args[0] == "build":
                    assert sdk == "11.0.100", "mapper compilation selected the consumer SDK"
                    assert pathlib.Path(args[1]).is_file()
                    assert args[2:4] == ["--configuration", "Release"]
                    print("MSBuild output must remain outside the mapping JSON")
                    print("retained compiler diagnostic", file=sys.stderr)
                    if os.environ["BUILD_FAILURE"] == "1": sys.exit(17)
                else:
                    assert sdk == "10.0.403", "mapper execution lost the consumer context"
                    assert pathlib.Path(args[0]).is_file() and args[1] == "prepare-build"
                    if os.environ["PREPARE_FAILURE"] == "1":
                        print("retained prerequisite refusal", file=sys.stderr)
                        sys.exit(23)
                    print(json.dumps({"status": "prepared", "buildPreparation": {"environment": {}}}))
                '''))
            stub.chmod(0o755)
            environment = dict(os.environ, PATH=str(tools) + os.pathsep + os.environ["PATH"],
                               BUILD_FAILURE=str(int(build_failure)), PREPARE_FAILURE=str(int(prepare_failure)))
            calls = []
            retained = {}
            scratch = root / "scratch"
            scratch.mkdir()

            def execute(purpose, command, cwd):
                calls.append(purpose)
                result = subprocess.run(command, cwd=cwd, env=environment, capture_output=True,
                                        text=True, timeout=20)
                key = "build" if purpose.startswith("build ") else "prepare"
                retained[key + ".stdout.log"] = result.stdout
                retained[key + ".stderr.log"] = result.stderr
                return result.returncode

            namespace = {"ROOT": root, "BUILDS": builds, "MODULE_CLI": cli,
                         "CONVERSATIONS_PREFIX": "references/Hexalith.Conversations/", "json": json}
            executable = compile(ast.Module(body=[function], type_ignores=[]), str(source_path), "exec",
                                 flags=__future__.annotations.compiler_flag)
            exec(executable, namespace)
            try:
                assignments = namespace["conversations_source_mapping"](types.SimpleNamespace(run=execute), scratch)
                self.assertEqual([], assignments)
                result = subprocess.CompletedProcess([], 0, "", "")
            except RuntimeError as error:
                expected_code = 17 if build_failure else 23
                self.assertIn(f"exit {expected_code}", str(error))
                result = subprocess.CompletedProcess([], expected_code, "", str(error))
            mapping = scratch / "conversations-source-mapping.json"
            if mapping.exists():
                retained["conversations.json"] = mapping.read_text()
            if build_failure:
                self.assertEqual(["build Conversations source mapper"], calls)
            else:
                self.assertEqual(["build Conversations source mapper", "prepare Conversations source mapping"], calls)
            return result, retained


if __name__ == "__main__":
    unittest.main()
