---
title: 'Story 6.4 P1R successor publication decision and qualification package'
type: 'chore'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/spec-6-4-prerequisite-p1r-source-readiness.md'
  - '_bmad-output/implementation-artifacts/spec-6-1-p1r-remediation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The pushed Story 6.4 source-readiness prerequisite is not an exact published successor qualification. EventStore source and Builds revisions have moved, so its proposed version and prior evidence need a fresh release and package-index check before an owner can decide on publication.

**Approach:** Recheck reachable EventStore release history, exact source and Builds identities, and all five public package indexes; record a conditional, exact-source publication decision for the owners and a bounded next published-package qualification work package. Preserve current package pins and P1R acceptance, keep P1R usability and Story 6.4 blocked pending required owner decisions, and retain `rollback=null` with the Projects AD-17 drain, mutation freeze, and forward-recovery policy. Do not dispatch publication or run package qualification in this preparation package.

</frozen-after-approval>

## Implementation Notes

- Rechecked EventStore live/local `main` at `269b20d58236f07228bc2afd54807b94bd96f37b`, latest tag/release `v3.124.0` at `0737c0befce5338e294a789092a0e223e8264a04`, and eleven post-tag commits; the installed analyzer proposes a minor `3.125.0` release. The older source-readiness receipt does not cover the current source.
- Distinguished EventStore's Builds gitlink `4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77`, release execution Builds `22a578b576a515d2af214fe81859447fffc97981`, Projects workspace Builds `b7a9c9ec9f57e098b408d3b940929ef4b160985b` and remote Builds `544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9`.
- Retained five fresh HTTP 200 NuGet index bodies and hashes plus the read-only analyzer result in `evidence/6-4-p1r-successor-decision-20261018/`. Each index ends at 3.124.0 and omits 3.125.0.
- Wrote `6-4-p1r-successor-publication-decision-2026-10-18.md` as a conditional owner request with a HOLD disposition because the exact-source P1R push run failed. Wrote `spec-6-1-p1r-31250-published-qualification-handoff.md` as the next gated, bounded package. No release or qualification was run, and pins, acceptance, rollback, P1R usability and Story 6.4 status were left unchanged.
- During independent review, live EventStore `main` advanced to `ab8fa115c486875be3a22860a273d5d68a26c749` with Builds gitlink `544fd2e907d62ccdc4b8f24168b8ab5dfcc13eb9`. The earlier `269b20d5`/`4f53d9f3` observations above remain historical. Captured the new GitHub source/run/config/commit responses, reran the installed analyzer over 14 post-tag commits (still prospective 3.125.0), and recaptured all five indexes. Exact-head P1R push run 39104648854 failed, so HOLD remains the decision. Updated the packet and handoff to the new observed source and separate CI Builds identity requirement.

## Review Triage Log

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| Live source moved after first capture | high: GitHub `main` became `ab8fa115c486875be3a22860a273d5d68a26c749`; old CI was cancelled and new P1R push proof failed. Replaced the proposed tuple and kept old evidence historical. | patch |
| CI Builds identity omitted | medium: `ci.yml` invokes the reusable Builds workflow at `@main`; the successful run's resolved SHA is a separate gate and is currently unavailable. Added explicit capture before owner authorization. | patch |
| Analyzer inputs not retained | medium: initial result alone omitted literal input history/configuration. Retained fourteen full commit messages, release config text/hash, current analyzer result and reproduction command. | patch |
| Current source-readiness gap | medium: the prior local receipt covers `bb986902`, not later query/governance/merge changes. Required new focused source-readiness evidence or explicit owner disposition. | patch |
| Full publication inventory omitted | medium: release scope is fourteen packages and a container. Required complete immutable inventory, then the five-package P1R subset. | patch |
| Partial publication outcome omitted | medium: NuGet push precedes container publication, so a failed run may leave published bytes. Required fresh inventory and owner decision before retry or selection. | patch |
| Selection tuple incomplete | medium: archive selection alone omits tag/source, NuGet content hashes, Builds identities, rollback and execution profile. Bound all fields in the handoff entry gate. | patch |

