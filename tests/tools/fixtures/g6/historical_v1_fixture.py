#!/usr/bin/env python3
"""Historical baseline-writing helpers for Projects-owned G-6 regression tests.

The baseline bytes remain pinned independently of the producer validator, and
literal version pins retain the workflow context of their original role.
"""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path


if not __debug__:
    raise SystemExit("G6-EVIDENCE-TESTS-INVALID: optimized Python disables required assertions")


SCRIPT = Path(__file__).resolve().parents[4] / "references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py"
BASELINES = (
    # Accepted 2026-09-06 G-6 tuple; bound by the historical accepted packet.
    SCRIPT.with_name("runtime-toolchain-baseline.json"),
    # Superseded 2026-09-27 EventStore 3.108.1 candidate tuple.
    SCRIPT.with_name("runtime-toolchain-baseline-2026-09-27.json"),
    # Pending 2026-09-29 EventStore 3.109.0 candidate tuple.
    SCRIPT.with_name("runtime-toolchain-baseline-2026-09-29.json"),
)
# Bound baselines must keep the exact bytes their packets hash (normalized-LF SHA-256,
# identical to the raw hash of the LF checkout): the accepted 2026-09-06 packet and the
# superseded 2026-09-27 packet. Any later edit to these files is drift, not an update.
HISTORICAL_BASELINE_SHA256 = {
    "runtime-toolchain-baseline.json": "525615c65ada8cabf5a6911a374bb22b62f6a3c1bfa6c91f7245312da9720265",
    "runtime-toolchain-baseline-2026-09-27.json": "b9aa6791effe78cbb9bf405cd8384b81a6d750f996fabf8f79b7cd1c3bedad01",
}
# The Dapr CLI setup action the fixture writes around every `version:` pin.
FIXTURE_DAPR_SETUP_ACTION = "dapr/setup-dapr@" + "0" * 40
DEFAULT_INPUT_BY_FIELD = {"daprCli": "dapr-version", "daprRuntime": "dapr-runtime-version"}
SPEC = importlib.util.spec_from_file_location("g6_validator", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
VALIDATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VALIDATOR)
# Retained Projects test support from Hexalith.Builds 6ad5854aa14a9950e1fbfc32d7e27953a1c4b82b.
# Only helpers used by test_g6_deferred_contracts.py are retained; validators stay producer-owned.


def write_json(path: Path, value: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def write_text(path: Path, value: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(value, encoding="utf-8")


def assert_historical_baseline_bytes() -> None:
    for name, expected in HISTORICAL_BASELINE_SHA256.items():
        actual = VALIDATOR.sha256(SCRIPT.with_name(name))
        assert actual == expected, f"bound historical baseline drifted: {name} hashes {actual}, packets bind {expected}"


def fixture_pin_contexts(literal_pins: list[dict[str, str]], tuple_values: dict[str, str]) -> list[tuple[str, str] | None]:
    """The YAML context each original pin is written in: its workflow input, its setup step, or none."""
    contexts: list[tuple[str, str] | None] = []
    for item in literal_pins:
        value = item["value"].strip()
        if value.startswith("default:"):
            fields = [field for field, name in DEFAULT_INPUT_BY_FIELD.items() if f"'{tuple_values[field]}'" in value]
            assert len(fields) == 1, f"default: pin sets no single fixture input: {value}"
            contexts.append(("input", DEFAULT_INPUT_BY_FIELD[fields[0]]))
        elif value.startswith("version:"):
            contexts.append(("action", FIXTURE_DAPR_SETUP_ACTION))
        else:
            contexts.append(None)
    return contexts


def write_literal_pins(
    workspace: Path, literal_pins: list[dict[str, str]], contexts: list[tuple[str, str] | None],
) -> None:
    """Write each audited literal pin as an active line of its fixture file, inside its YAML context."""
    files: dict[str, dict] = {}
    for item, context in zip(literal_pins, contexts, strict=True):
        entry = files.setdefault(item["path"], {"lines": [], "inputs": {}, "steps": []})
        value = item["value"].strip()
        if context is None:
            entry["lines"].append(value)
        elif context[0] == "input":
            entry["inputs"].setdefault(context[1], []).append(value)
        else:
            entry["steps"].append((context[1], value))
    for relative, entry in files.items():
        lines = list(entry["lines"])
        if entry["inputs"]:
            lines += ["on:", "  workflow_call:", "    inputs:"]
            for name, values in entry["inputs"].items():
                lines += [f"      {name}:", "        type: string", *(f"        {value}" for value in values)]
        if entry["steps"]:
            lines.append("steps:")
            for action, value in entry["steps"]:
                lines += ["  - name: Install Dapr CLI", f"    uses: {action}", "    with:", f"      {value}"]
        write_text(workspace / relative, "\n".join(lines) + "\n")
