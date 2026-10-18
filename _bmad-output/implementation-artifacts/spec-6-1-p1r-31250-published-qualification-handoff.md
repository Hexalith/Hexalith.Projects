---
title: '6.1-P1R Bounded published successor qualification handoff for Story 6.4'
type: 'chore'
created: '2026-10-18'
status: 'draft'
route: 'dispatch'
candidate_version_proposed: '3.125.0'
candidate_source_proposed: '39b366bca3dbe508b4cf3c5afa69db1ddadb6dd2'
candidate_package_builds_proposed: '544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9'
candidate_ci_builds_observed: '4629ff2d3bd66376b29a553663c241d3a7b87853'
candidate_release_execution_builds: '22a578b576a515d2af214fe81859447fffc97981'
publication_readiness_observed_at: '2026-10-18T11:49:26.545729Z'
execution_gate: 'blocked-pending-publication-and-owner-selection'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/6-4-p1r-successor-publication-decision-2026-10-18.md'
  - '_bmad-output/implementation-artifacts/spec-6-1-p1r-remediation.md'
---

<frozen-after-approval reason="candidate and execution inputs require a separate exact published-family owner decision">

## Intent

**Problem:** Local source-readiness checks and older signed packages do not prove that the current EventStore source works as a published P1R successor. The proposed `3.125.0` family has not been published or owner-selected.

**Approach:** After exact-source publication and owner selection, qualify only the five selected signed EventStore packages in fresh isolated Release/package consumers against the existing finite P1R contract. Retain `rollback=null`, measure the successor support boundary, independently validate all evidence, and report failures without advancing pins, P1R acceptance/usability or Story 6.4.

## Boundaries & Constraints

**Always:** Require an actual tag equal to the owner-approved source, exact archive SHA-256/signatures/NuGet content hashes/repository commits, the tag Builds gitlink, actual package-build Builds provenance and separate release-execution Builds provenance. Preserve historical packets and failed rows. Use pinned Dapr 1.18.2/PostgreSQL image `postgres@sha256:a02db8cac496f15b094798a38254f14d6e00741f709360e5e00bb6668ea31636`, private owned resources, the adopted `p1r-executed-checks-v1` assertions, and Projects AD-17 drain/freeze/forward recovery. Bind source comparisons to the exact released source, not the older readiness source.

**Never:** Start on a proposed version, guessed tag, unpublished graph, source Debug substitutes or old 3.124.0 receipts. Do not weaken finite enrollment, strict provenance, required cases, cleanup or shared-resource preservation. Do not change production pins, the fixed acceptance record, Stack, sprint/readiness state, rollback policy or Story 6.4 code; do not synthesize persisted state or owner decisions.

</frozen-after-approval>

## Publication readiness observation — 2026-10-18

The [refreshed decision packet](6-4-p1r-successor-publication-decision-2026-10-18.md) observes live source `39b366bca3dbe508b4cf3c5afa69db1ddadb6dd2`, expected package-build Builds `544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9`, resolved CI Builds `4629ff2d3bd66376b29a553663c241d3a7b87853` and release-execution Builds `22a578b576a515d2af214fe81859447fffc97981`. At `2026-10-18T11:49:26.545729Z`, P1R preparation **39123504568** and Commitlint **39123504763** passed; CI **39123504769** remained in progress with no conclusion. No successful CI proof is inferred from a resolved Builds identity. All five indexes still stop at 3.124.0; 3.125.0 remains proposed, with no observed tag/release. The old red sentinel proof is historical; current-source readiness beyond the older Debug/Dapr 1.18.7/Redis receipt still requires fresh evidence or explicit named owner disposition before publication authorization. This observation changes no execution authority.

## Code Map

Paths below belong to `references/Hexalith.EventStore` unless otherwise noted.

- `tools/p1r-published-executor.py`, `tools/p1r_published_executor.py`, `tools/p1r-published-consumers/` — isolated package execution, finite 3.125.0 enrollment, genuine Counter seed/witness and source/package graphs; reuse the pushed readiness tooling.
- `tools/p1r-published-qualification.py`, `tools/p1r_published_qualification.py`, `tools/p1r_check_witnesses.py`, `tools/p1r_isolation.py` — strict packet, support-boundary, witness and owned-cleanup validation; keep existing admission checks.
- `_bmad-output/specs/spec-6-1-p1r-remediation/qualification-contract.md` — 17 canonical scenarios, Reminder recovery and logical-event evolution additions, 163-case baseline and explicit supported/refusal/containment meanings.
- Projects `_bmad-output/implementation-artifacts/evidence/6-1-p1r-31210-publication/qualification-proposal.json` — structural example only; every candidate archive and source/Builds coordinate must be replaced with actual new publication evidence.

## Tasks & Acceptance

**Entry gate:** The [publication decision](6-4-p1r-successor-publication-decision-2026-10-18.md) must be superseded by a dated, named exact-source owner authorization after all three required exact-source release push proofs pass and current-source readiness passes or receives explicit owner disposition. Record the resolved successful CI Builds SHA separately. Inventory the complete fourteen-package/container publication, including any partial failure. A partial release needs a new owner decision before retry or selection. For a complete release, owners must separately select the **actual version/tag/source, five archive SHA-256 and NuGet content hashes, tag/package-build, resolved successful CI and release-execution Builds identities, `rollback=null`, Dapr 1.18.2/PostgreSQL image profile, 3.70.1/3.110.0 comparisons, `p1r-executed-checks-v1` mechanism and both Reminder/evolution additions**. If Semantic Release chooses a version other than 3.125.0 or the tag points elsewhere, re-scope this handoff before execution.

**Execution:**

- [ ] New EventStore `_bmad-output/implementation-artifacts/evidence/6-1-p1r-31250-publication/` — retain full release/tag/build/run provenance, fourteen-package/container inventory and partial-publish disposition if needed; then capture the five qualification indexes, signed archives, content hashes, graphs and exact owner selection. Preserve propagation 404s separately.
- [ ] New EventStore `_bmad-output/implementation-artifacts/evidence/6-1-p1r-31250-published-run/` — execute fresh isolated Release/package lanes for all five selected roles, physical assembly loads, the 17 canonical scenarios and two additions; compare historical 3.70.1/3.110.0 writers and exact released Debug/source separately. Preserve prior 3.121.0 six-case/16-assertion failures as history, never inherit a pass.
- [ ] Same new run — create a genuine published qualified folded snapshot after nine committed Counter events, append three actual events, restore floor five/head twelve into a fresh owned database, append thirteen, restart and check original hashes, version/state and Tenant isolation. Exercise current authority/recovery and bounded refusal cases, source/package equivalence, negative controls, cancellation/repeated cleanup and exact shared-resource preservation.
- [ ] New candidate-specific support table and independent audit — rederive every case, asserted effect/refusal, typed denial before protected access, source/archive/config/command/provider bindings and owned cleanup. Run strict packet preparation/validation without converting a refusal or missing owner disposition to success; record all four later role decisions and same-baseline conformance as pending.
- [ ] Projects decision packet append — link immutable evidence and state actual results only; leave package pins, P1R acceptance/usability, Story 6.4 and sprint status unchanged pending separate owner decisions.

**Acceptance Criteria:**

- Given an owner-selected published family, when each package lane restores and runs, then its signed archive, resolved graph and physically loaded assemblies match that family with no EventStore source dependency.
- Given the actual nine-event snapshot and retained stream, when restore, append and restart run under the pinned profile, then floor five, sequence thirteen, original event hashes and Tenant isolation are measured from persisted state.
- Given any failed required case, drifted shared resource or pending support/role disposition, when the independent audit and strict gate run, then the result stays nonpassing and P1R usability and Story 6.4 remain blocked.

## Implementation Notes

Prepared as an execution handoff only. The refreshed exact-source P1R preparation and Commitlint proofs pass; the CI result and current-source readiness disposition remain explicit publication prerequisites. `3.125.0` is absent from all five indexes, and no owner has selected its published tuple. The older source-readiness receipt, older 3.124.0 signed archives and 3.121.0 qualification failures remain distinct evidence. A new source or version needs reanalysis and an updated work package before a live run.
