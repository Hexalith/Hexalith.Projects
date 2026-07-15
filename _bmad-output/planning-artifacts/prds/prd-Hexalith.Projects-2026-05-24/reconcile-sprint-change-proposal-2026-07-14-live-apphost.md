# Final Reconciliation: Live AppHost Route and Epic 5 Playwright Closure

## Input

- Approved and implemented source: `sprint-change-proposal-2026-07-14-live-apphost.md` (2026-07-14).
- Compared with: revised `prd.md` and `addendum.md` (2026-07-15).
- Verdict: **Substantially reconciled, with four residual addendum gaps; verification enablement is complete, release acceptance is not.**

## Faithfully captured

- The PRD correctly makes no feature, role, journey, FR, MVP-scope, or product-behavior change. The proposal is operational verification work for existing operator, security, accessibility, and release obligations.
- PRD NFR-9 retains authenticated accessibility evidence, and NFR-11 explicitly blocks release on failed critical cases or unexplained critical skips.
- Addendum §7 faithfully states that the live-AppHost proposal established a runnable mechanism but not release acceptance, and that reported failures must be classified, owned, corrected, and rerun.
- Addendum §8 correctly routes live-AppHost and no-skip evidence into test strategy and keeps topology/version mechanics outside the capability-oriented PRD.
- PRD NFR-1 and the addendum's metadata-only evidence posture preserve the source's prohibition on exposing credentials, tokens, payloads, setup text, or private content.

## Gaps

1. **The live topology invariant is too abstract.** The addendum does not retain dynamic `projects-ui` readiness and endpoint discovery, prohibition of guessed ports, Aspire-managed teardown, root-declared sibling use without nested-submodule initialization, owned AppHost/hosting/toolchain alignment, or the prohibition on editing generated tool-cache binaries.
2. **The dual-lane Playwright contract is missing.** It does not preserve explicit live opt-in, fail-fast URL validation, independently runnable deterministic no-AppHost checks, conversion of permanent `test.fixme` declarations to conditional live tests, or the rule that every retained skip names a concrete route, fixture, seed, or product prerequisite.
3. **The evidence is summarized without its decisive facts.** The addendum omits the focused result of 13 live failures, the full result of 19 passed/56 failed across 75 Chromium cases, and the two known blocker classes: missing deterministic `tenant-a` access-projection seeding and missing warning-console UI/static-asset prerequisites.
4. **Acceptance traceability and ownership remain implicit.** The revised documents do not state that safe-denial `404` results are valid negative evidence but not proof of authorized FR-22 behavior, map the failed cases to FR-21/FR-22/NFR-9/NFR-11, or retain the separate Hexalith.Conversations sibling-root correction and remediation owners.

## Contradictions

- There is **no direct contradiction with the revised PRD or addendum**; both preserve the unchanged product contract and distinguish execution from acceptance.
- The source permits Story 5.12 to be marked done when the live lane runs and records results. That is compatible only when `done` means verification enablement. Treating Story 5.12 completion as Epic 5 or release acceptance would contradict PRD NFR-11 and addendum §7 while critical live failures remain.

## Qualitative intent at risk

- Dynamic discovery is the invariant; a fixed or guessed port is not an acceptable substitute.
- Live mode must be explicit and fail fast, while the no-AppHost contract lane remains deterministic and independently useful.
- Safe denial is valuable negative-path evidence, but it never substitutes for an authorized positive operator path.
- “Verification completed” means failures became observable and reproducible; it does not mean the product passed.
- Preserve root-only submodule topology, fix version incompatibility at an owned boundary, sanitize evidence, and include teardown in the verification lifecycle.
- Keep Story 5.11's historical offline evidence intact without presenting it as live proof.

## Disposition

- Leave `prd.md` unchanged.
- Extend addendum §7 with the live topology and dual-lane invariants, concrete result/blocker summary, requirement traceability, and remediation/upstream ownership.
- Treat route discovery and live-test enablement as completed infrastructure; keep Epic 5/release acceptance open until failures are classified, corrected, and rerun with deterministic authorized fixtures and required UI assets.
- Route exact commands, environment variables, version pins, fixture recipes, case inventory, and raw evidence to architecture, runbooks, test strategy, Story 5.12, and the test summary.
- Final disposition: **revise addendum, then accept; do not reopen PRD requirements.**
