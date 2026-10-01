# Post-capture implementation verification

Retained selected failed packet [attempt-3/packet.json](attempt-3/packet.json) remains SHA-256 `724939520f2d8850073fd0091ffb7f0ada88e87231bb6ff457063bacb562a6bd`. No source bindings were rewritten after capture. It records 1 failed qualifier, 33 passed support cases across 21 selectors, 21 passed fixture controls, zero skips, five failed/five successful AppHosts and a successful separate McpCli consumer build. Shared snapshots are equal, all four owned containers are removed, owned process groups are stopped and scratch is removed. Its live matrix and technical qualification remain incomplete.

The final source-only correction makes root acceptance possible across later metadata-only evidence/acceptance commits: captured root revision must be an ancestor of current HEAD; every bound root source/configuration/build/test byte and submodule gitlink must be committed with identical bytes at both revisions. The working source manifest must still match. Uncommitted or changed bound source fails. Submodules retain exact HEAD/gitlink and Git dirtiness checks. A complete named-acceptance positive control passes across a later metadata-only root commit; an accepted source mutation rejects. Runtime source closure now also includes Razor/web sources, resources and runtime JSON configuration. The final McpCli controls additionally require its actual Debug/source build and resolved packages, and every recorded command must pass. Fully rehashed missing/failed McpCli, missing package entry and additional failed command controls reject. These corrections change four packet-bound implementation artifacts, so the retained packet is additionally stale against final source.

| Artifact | Captured SHA-256 | Final implementation SHA-256 |
| --- | --- | --- |
| current-mutation-tests | `71a09f1e100e67bd619f06f12993095cdddf66f770e14d7da1b710ee14e7319f` | `b429de860cc865f973dfd75051cef5440d36363cc32bb1212be99cee15b8f5a2` |
| current-validator | `0ba87fc0a3d1d36d3f3338d3c9360100867bc4f273ae3f25162329d5b838c9b1` | `7ac63ffb56933e6be71e1ed8efc73aaf2bef22a1d7c01e7670bd5a08f7955bdf` |
| mutation-tests | `026b5d7c4ce072c570461b42c9391d2c7dcb62de0cd3cf91415dc6e40dde5e85` | `fb11aeaad1c052de8835fd1a937144b46143a8f7df54e46b54fba3160166315e` |
| qualification-runner | `162996aa2a1e39f2f6995134588021bc11beaf5a6a1c77e00bae2a075696831a` | `a8f59d286231d686e7915cac56d6375af7ea1649413e251c3c55516f7051d060` |

Final passing source checks:

```text
python3 references/Hexalith.Builds/Tools/test-runtime-toolchain-evidence-validator.py
exit 0: 40 current mutation controls plus accepted metadata-commit positive control;
30 scenarios per historical baseline (3 baselines), 48 baseline-drift controls,
165 authority controls and 2 historical baseline SHA-256 pins.

python3 -m unittest tests/tools/test_run_g6_candidate_gate.py tests/tools/test_check_g6_packet_gitlinks.py tests/tools/test_g6_packet_references.py
exit 0: 24 controls (19 gate/gitlink fixtures plus 5 current document-reference controls).

pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1
exit 0: 5 workflow files; CI Builds execution is the root gitlink 21ce044ab465ccb2adab58b3d66e394ffbecf3c2.

python3 tools/planning/validate_production_authority.py --validate-index
exit 0: production-authority index [6, 7, 8]. Separate exact candidate files were
validated through --sprint-status before each atomic index replacement.

git -C <changed-repository> -c core.whitespace=cr-at-eol diff --check
exit 0 for root and Builds, Conversations, EventStore, Folders, FrontComposer,
Memories, Parties, Platform and Tenants. The CR-at-EOL setting honors Memories'
required CRLF; the ordinary check reported its first-line CR as whitespace.
```

`python3 -m unittest tests/tools/test_production_authority_guard.py` also exits 0 with 21 scheduling/acceptance guard controls.

All 63 sanitized evidence logs are unignored by a narrow local `.gitignore` allowlist, so a normal future commit can carry each hash-bound log. None were staged. All packet artifact differences are the four explicitly listed post-capture implementation changes; observation/result/command/cleanup artifacts remain as captured.

Final failing gates, retained as failures:

```text
python3 references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py --workspace . --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-10-01.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/attempt-3/packet.json --candidate
exit 1: Current artifact hash mismatch: references/Hexalith.Builds/Tools/test_runtime_toolchain_v2.py

python3 tests/tools/run_g6_candidate_gate.py --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-10-01.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/attempt-3/packet.json
exit 1, two checks: current mutation-test artifact hash mismatch, and EventStore
checkout HEAD 96a6041c5d63a933a58f1df09f7fad123c9ada41 differs from root gitlink
6dededdecd62dd6dc6d1f15810108d860ec70c8f. Builds execution SHA check passes.

python3 references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py --workspace . --baseline references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-10-01.json --packet _bmad-output/implementation-artifacts/qualification-evidence/g-6-runtime-toolchain-20261001/attempt-3/packet.json
exit 1: Current packet acceptance is pending.
```

At capture, candidate validation instead rejected stale Memories resolved `Aspire.Hosting` assets after the source guard prevented restoration, and the same EventStore gitlink mismatch. The original namespace-isolated qualifier command, its HTTP 404/500 diagnostics and strict capture failure remain in the packet's bound logs. The separate standalone fixture `dotnet build ... --no-restore` check failed `NETSDK1064` because previous assets named the removed isolated package cache; the required fresh third-attempt restore/build then passed, including all 21 fixture controls.

Remaining work is successful full live qualification and all AppHost/source prerequisites, fresh final committed exact-gitlink capture, then named acceptance on that reviewed hash. P1R, archive/rollback facts and downstream states remain preserved. The spec stays in-progress, with remaining acceptance work unchecked.
