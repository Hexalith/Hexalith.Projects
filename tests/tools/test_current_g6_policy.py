"""The owning source-only authorization follows the real published producer catalog."""

from __future__ import annotations

import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[2]
BUILDS = ROOT / "references/Hexalith.Builds"
PRODUCER = "6ad5854aa14a9950e1fbfc32d7e27953a1c4b82b"
RELATIVE = "tools/qualification/g6-current-policy-2026-11-12-dapr-net1191.json"
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
        self.assertEqual(original["materialFiles"] + [RELATIVE], policy["materialFiles"])
        self.assertEqual(21, CURRENT.SUPPORT_SELECTORS)
        self.assertEqual(33, CURRENT.SUPPORT_CASES)
        expected = copy.deepcopy(original["tuple"])
        expected["daprDotnetPackages"] = "1.19.1"
        self.assertEqual(expected, policy["tuple"])

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
