"""The owning source-only authorization follows the real published producer catalog."""

from __future__ import annotations

import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/qualification"))
from check_conversations_toolkit import CONVERSATIONS_APPHOST, TOOLKIT_PACKAGE, toolkit_issue

BUILDS = ROOT / "references/Hexalith.Builds"
PRODUCER = "b80f3e85fc803f71bf7b2809965db0aceafdc3c5"
RELATIVE = "tools/qualification/g6-current-policy-2026-11-23-toolkit-preview.json"
DAPR_ACTION = "references/Hexalith.Builds/Github/dapr-init/action.yml"
SPEC = importlib.util.spec_from_file_location("owning_g6_policy_contract", BUILDS / "Tools/g6_current.py")
assert SPEC is not None and SPEC.loader is not None
CURRENT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(CURRENT)


class OwningCurrentPolicyTests(unittest.TestCase):
    def test_exact_package_tuple_comes_from_the_unchanged_published_catalog(self) -> None:
        policy = CURRENT.policy_document(ROOT / RELATIVE)
        catalog = ET.fromstring(subprocess.check_output(
            ["git", "-C", str(BUILDS), "show", PRODUCER + ":Props/Directory.Packages.props"]))
        selected = {item.get("Include"): item.get("Version") for item in catalog.iter("PackageVersion")}
        packages = ("Dapr.Client", "Dapr.AspNetCore", "Dapr.Actors.AspNetCore",
                    "Dapr.Actors.Generators", "Dapr.Actors", "Dapr.Workflow")
        for package in packages:
            with self.subTest(package=package):
                self.assertEqual("1.19.1", selected[package])
                self.assertEqual(selected[package], policy["tuple"]["daprDotnetPackages"])
        toolkit_version = catalog.findtext(".//HexalithAspireHostingDaprVersion")
        self.assertEqual(toolkit_version, policy["tuple"]["communityToolkitAspireDapr"])
        self.assertEqual("Current user authorization", policy["approval"]["approvedBy"])
        self.assertTrue(policy["exceptions"]["sourceModeOnly"])
        self.assertFalse(policy["reuseEnabled"])

    def test_old_producer_policies_remain_exact_and_behavior_scope_cannot_shrink(self) -> None:
        for path, expected in (
            ("Tools/g6-current-policy-2026-10-20.json", "580c381e62996e2e59e1b670604396e17fa0f2627cff26695c17288aff8127cf"),
            ("Tools/g6-current-policy-2026-11-12.json", "8f56f843f5476d7e3a6a003b5839836218a6dee7e55593716e476910163c67cc"),
            ("Tools/g6-current-policy.json", "b322645318505aecbed54c14675c0d0117c4ec268c4c62547812414664ce7aa7"),
        ):
            original = subprocess.check_output(["git", "-C", str(BUILDS), "show", PRODUCER + ":" + path])
            self.assertEqual(expected, hashlib.sha256(original).hexdigest())
            self.assertEqual(original, (BUILDS / path).read_bytes())
        original = json.loads((BUILDS / "Tools/g6-current-policy-2026-11-12.json").read_text())
        policy = CURRENT.policy_document(ROOT / RELATIVE)
        self.assertEqual(original["resolvedProjects"], policy["resolvedProjects"])
        self.assertEqual(original["materialPrefixes"], policy["materialPrefixes"])
        self.assertEqual(original["materialFiles"] + ["tools/qualification/g6-current-policy-2026-11-12-dapr-net1191.json", RELATIVE, DAPR_ACTION], policy["materialFiles"])
        self.assertEqual(21, CURRENT.SUPPORT_SELECTORS)
        self.assertEqual(33, CURRENT.SUPPORT_CASES)
        expected = copy.deepcopy(original["tuple"])
        expected["aspireSdk"] = "17.0.0"
        expected["aspireCli"] = "17.0.0"
        expected["communityToolkitAspireDapr"] = "17.0.0-preview.1.261120-0454"
        expected["daprDotnetPackages"] = "1.19.1"
        expected["fluentUi"] = "5.1.0"
        self.assertEqual(expected, policy["tuple"])

    def test_conversations_apphost_restore_uses_catalog_toolkit_version(self) -> None:
        project = CONVERSATIONS_APPHOST
        toolkit = TOOLKIT_PACKAGE
        policy = CURRENT.policy_document(ROOT / RELATIVE)
        self.assertIn(project, policy["resolvedProjects"])
        version = policy["tuple"]["communityToolkitAspireDapr"]

        with tempfile.TemporaryDirectory() as directory:
            workspace = Path(directory)
            assets = (workspace / project).parent / "obj/project.assets.json"
            assets.parent.mkdir(parents=True)
            for resolved, accepted in ((version, True), ("13.6.0-beta.910", False)):
                with self.subTest(resolved=resolved):
                    assets.write_text(json.dumps({"libraries": {f"{toolkit}/{resolved}": {"type": "package"}}}))
                    graph, issues = CURRENT.resolved_graph(workspace, {"resolvedProjects": [project]}, policy["tuple"])
                    self.assertEqual([{"path": project, "packages": [{"id": toolkit, "version": resolved}]}], graph)
                    expected = [] if accepted else [
                        f"resolved package differs from effective communityToolkitAspireDapr: {project}::{toolkit} {resolved}"
                    ]
                    self.assertEqual(expected, issues)
                    audit = {"resolvedPackages": graph, "effectiveTuple": policy["tuple"]}
                    self.assertEqual(accepted, toolkit_issue(audit) is None)

            missing_toolkit = {"resolvedPackages": [{"path": project, "packages": []}],
                               "effectiveTuple": policy["tuple"]}
            self.assertIn("must restore", toolkit_issue(missing_toolkit))

    def test_dapr_action_defaults_are_material_to_current_qualification(self) -> None:
        policy = CURRENT.policy_document(ROOT / RELATIVE)
        self.assertIn(DAPR_ACTION, policy["materialFiles"])
        with tempfile.TemporaryDirectory() as directory:
            workspace = Path(directory)
            builds = workspace / "references/Hexalith.Builds"
            builds.mkdir(parents=True)
            for repository in (workspace, builds):
                subprocess.run(["git", "init", "-q", str(repository)], check=True, capture_output=True)
            (workspace / ".gitmodules").write_text('[submodule "Builds"]\n    path = references/Hexalith.Builds\n')
            action = workspace / DAPR_ACTION
            action.parent.mkdir(parents=True)
            action.write_bytes((ROOT / DAPR_ACTION).read_bytes())
            fixture = {"materialFiles": [DAPR_ACTION], "materialPrefixes": []}
            before = CURRENT.tracked_material(workspace, fixture)
            self.assertEqual(1, before["fileCount"])
            with action.open("ab") as stream:
                stream.write(b"\n# changed Dapr setup action\n")
            self.assertNotEqual(before["fingerprint"], CURRENT.tracked_material(workspace, fixture)["fingerprint"])
            action.unlink()
            with self.assertRaisesRegex(CURRENT.G6Error, "required material files are missing"):
                CURRENT.tracked_material(workspace, fixture)

    def test_changed_tuple_without_exact_new_authorization_is_refused(self) -> None:
        policy = CURRENT.policy_document(ROOT / RELATIVE)
        self.assertEqual(CURRENT.digest(policy["tuple"]), policy["approval"]["tupleSha256"])
        changed = copy.deepcopy(policy["tuple"])
        changed["daprDotnetPackages"] = "1.19.0"
        self.assertNotEqual(CURRENT.digest(changed), policy["approval"]["tupleSha256"])
        policy["tuple"] = changed
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "changed-policy.json"
            path.write_text(json.dumps(policy))
            with self.assertRaisesRegex(CURRENT.G6Error, "approval is not bound"):
                CURRENT.policy_document(path)
        self.assertIn("no Builds signature", policy["approval"]["reference"])
        self.assertIn("native/image acceptance", policy["approval"]["reference"])


if __name__ == "__main__":
    unittest.main()
