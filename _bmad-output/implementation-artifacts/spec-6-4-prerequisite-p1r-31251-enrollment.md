---
title: 'Story 6.4 prerequisite: finite P1R 3.125.1 enrollment'
type: 'chore'
created: '2026-10-18'
status: 'done'
route: 'dispatch'
owner_repository: 'references/Hexalith.EventStore'
baseline_commit: '25f9fdab6106f5bcbf06aaabe8ca351f543bb52e'
owner_baseline_commit: '47e234ccb535a71e7dd4a039fd0e2281083083ae'
candidate_version: '3.125.1'
candidate_source: '55432c50d4b6f1666b4bd6d4a60bfc263b2d0e0b'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/evidence/6-4-p1r-31251-enrollment-20261018/README.md'
---

<frozen-after-approval reason="user selected option 2; implementation scope awaits this prerequisite's approval">

## Intent

**Problem:** Published EventStore 3.125.1 is outside the finite P1R qualification fixture enrollment. Its consumers omit required compiler paths; strict snapshot and isolation controls do not recognize it.

**Approach:** Add only exact 3.125.1 to the existing tooling, test strict positive/refusal paths, and prepare a truthful successor handoff. This prepares later qualification; it does not execute it.

**Decision:** The user chose option 2, targeting 3.125.1. Preserve the prerequisites-first hold on Story 6.4.

## Boundaries & Constraints

**Always:** Preserve 3.125.0, historical/source behavior, exact selected-writer binding, genuine nine-event manual snapshot plus three actual appends, strict witness/isolation/cleanup checks, rollback=null and AD-17 drain/freeze/forward recovery. Unknown successors refuse before state reset or writer start. Keep source/tag/HEAD equality enforced; the new handoff must block operational execution until a separately approved tooling/source binding strategy exists.

**Never:** Expand production snapshot policy or supported runtime semantics; synthesize snapshot/provider evidence; admit arbitrary future versions; edit generated output; update dependencies, Git refs/checkouts/gitlinks, acceptance, Stack or sprint state; publish, qualify packages or implement Story 6.4. Planning target is not exact-tuple owner selection.

## I/O & Edge-Case Matrix

| Given | When | Then |
| --- | --- | --- |
| 3.125.1 selected, matching package/source writer | Seed and witness validation run | Existing genuine snapshot and strict checks apply |
| 3.125.0 and 3.125.1 candidate/writer mismatch, either direction | Seed is requested | Refuse before provider or writer I/O |
| Unenrolled 3.125.2 or 3.126.0 | Projects evaluate or genuine seed is requested | No successor compiler paths; seed refuses |
| Missing/substituted snapshot, physical identity or isolation/cleanup evidence | Strict import runs | Fail; never convert incomplete proof to success |
| Edited tooling HEAD differs from released tag | Execution inputs validate | Existing immutable guard refuses; handoff remains blocked |

</frozen-after-approval>

## Code Map

Paths below are EventStore-relative unless marked Projects. All 52 local tooling/test files match released immutable blobs; this does not bind the edited checkout to the tag.

- `tools/p1r_published_executor.py` — seed/case allowlists; preserve `validate_execution_inputs` and `observe_inputs` equality guards.
- `tools/p1r_check_witnesses.py` — strict snapshot requirement and writer admission.
- `tools/p1r_published_qualification.py` — final isolation attachment and lane/recovery/import binding.
- `tools/p1r-published-consumers/` — feature constants plus Host/Domain includes; Probe inherits constants.

## Tasks & Acceptance

**Execution:**

- [x] `tools/p1r-published-consumers/Directory.Build.props`, `host/Host.csproj`, `domain/Domain.csproj` — add exact 3.125.1 to existing positive/negative enrollment conditions; preserve every older branch.
- [x] `tools/p1r_published_executor.py`, `tools/p1r_check_witnesses.py`, `tools/p1r_published_qualification.py` — enroll 3.125.1 in seed, fixture-source retention, witness and final isolation checks; preserve selected-writer equality and immutable execution guards.
- [x] `tools/tests/test_p1r_published_executor.py` — exercise the existing strict witness/isolation corpus for 3.125.1; extend seed/restore/fixture-source coverage; retain both cross-version mismatch refusals and use 3.125.2 for unenrolled candidate controls.
- [x] `tools/tests/test_p1r_snapshot_registration.py` — evaluate Host/Domain/Probe for both enrolled versions and unenrolled 3.125.2/3.126.0; retain real HostBuilt registration/negative controls. Add 3.125.1 import regression in `tools/tests/test_p1r_published_qualification.py` only where inherited coverage leaves a gap.
- [x] `tools/p1r-published-consumers/README.md` — append enrollment/verification scope and immutable-binding limitation; preserve historical observations.
- [x] Projects `_bmad-output/implementation-artifacts/spec-6-1-p1r-31251-published-qualification-handoff.md` — prepare a draft bound to actual source, five archive identities, separate Builds coordinates and pending ownership/graph/binding gates; preserve the 3.125.0 handoff.
- [x] New EventStore `_bmad-output/implementation-artifacts/evidence/6-4-p1r-31251-enrollment/` — retain exact changes, commands/results, negative controls and preservation proof; distinguish tooling checks from package qualification.

**Acceptance:** Given the bounded enrollment changes, when focused suites run, then the entire matrix passes and each changed admission boundary is exercised. Given the new handoff, when inspected, then it records actual immutable publication coordinates and blocks execution on unresolved owner selection, restored graph agreement and tooling/source binding without implying P1R acceptance.

## Implementation Notes

- Approved by the user with "approve and continue". Implemented nine EventStore tooling/project/test/README paths and the separate 3.125.1 handoff. All five required suites pass: 151 + 62 + 2 + 31 + 24 = 270 tests. Retained initial ENOSPC failures and passing retries using TMPDIR on /dev/shm. The AppHost baseline built but startup is blocked on absent nested Tenants hosts.
- Primary audit verified every task, matrix coverage, evidence manifest, frozen scope and protected state. Production/runtime code, dependencies, Git identities, acceptance and Story 6.4 remain unchanged. Qualification remains blocked on its exact owner/graph/tooling-source gates.


- Review patches completed and verified: required evidence is Git-discoverable; original publication/native Aspire bytes and reproducible post-run preservation are retained; observed package-build attribution and contract links are corrected. The changed registration suite passes 2/2 with Content-copy assertions. Primary post-review audit passes for both manifests and all protected files. No findings were deferred.
- The workflow local-commit step is superseded by this approved frozen scope, which prohibits updating Git refs/checkouts/gitlinks. Changes remain uncommitted; no staging, dependency update, qualification or owner acceptance was performed.

## Spec Change Log

## Review Triage Log

### Review pass 1

- B1 — medium / patch: EventStore coverage.json matches coverage*.json ignores; the manifest would be incomplete in a fresh checkout. Add a scoped exception.
- B2 — medium / patch: Three retained Projects release-smoke logs are ignored; add scoped evidence exceptions so their existing manifest remains reproducible.
- B3 — medium / patch: Publication coordinates preserve derived data but the referenced original source-receipt bytes/hash live only in temporary storage. Retain those exact bytes with an explicit link and manifest binding.
- B4 — medium / patch: Aspire baseline records a summary/external log path without retained native diagnostic bytes or original command timing. Retain available native evidence and honest timing limitations.
- B5 — medium / patch: Full preservation verification predates the last passing suites; repeat it after verification and retain the post-run result.
- B6 — low / patch: Protected-path selection and admission normalization are not reproducible from a retained procedure. Record the exact read-only procedure and its invocation.
- B7 — low / reject: Project evaluation omits PackageVersion assertions, but unchanged central version binding reads EventStoreVersion directly and restored graph agreement is expressly unmeasured/pending. Additional assertions would target an unrelated hypothetical change, rather than correct a stale binding in this diff.
- B8 — low / patch: The changed Host ItemGroup also controls copying the evolution JSON, while evaluation asserts only Compile. Assert that actual Content item and output-copy metadata alongside existing checks.
- B9 — false / reject: Standard unfiltered unittest discovery ran 151 cases with OK and no skips; independent discovery finds exactly 151 unskipped cases including the 22 manifest-listed 3.125.1 methods in the hash-bound test source. The corpus is exercised despite aggregate output.
- B10 — low / patch: The handoff lacks clickable authoritative contract/inventory references. Add links with historical-versus-candidate applicability stated.
- E1 — medium / patch: The edge reviewer independently confirms B1/B2's ignored manifest members. Same packaging root cause; retain this separate verdict and group the correction.
- P1 — medium / patch: Prior retained release-job observations record the actual package-build checkout 4629ff2d3bd66376b29a553663c241d3a7b87853. The handoff labels package construction wholly unbound; include the observed checkout while leaving archive/restore graph verification explicitly unmeasured.
- Verification-gap reviewer — no gaps found.

Groups: packaging (B1/B2/E1); retained publication evidence (B3/P1); baseline diagnostics (B4); reproducible post-run preservation (B5/B6); changed Content coverage (B8); handoff references (B10). All surviving fixes are bounded patches; no intent or specification loopback is needed.

## Design Notes

No intent gaps or irreversible actions. Footprint: six tooling/project files, two test files with a conditional third, README, one handoff and evidence. Strict admission/attribution changes merit the dispatch route. Runtime/tooling binding remains a separately gated execution prerequisite, outside this enrollment repair.

## Verification

From EventStore, run `python3 -m unittest discover -s tools/tests -p NAME` individually for `test_p1r_published_executor.py`, `test_p1r_published_qualification.py`, `test_p1r_snapshot_registration.py`, `test_p1r_isolation.py`, and `test_p1r_isolation_hardening.py`; expect all pass. The registration suite evaluates published projects and separately starts source hosts at HostBuilt; it supplies no published operational qualification.

Run `git diff --check` in both repositories and compare protected hashes, history, gitlinks, pins and sprint/acceptance state. Before code edits, follow the owner repository's Aspire baseline requirement and retain any exact blocker separately from tooling verification. Approval covers this enrollment package only.
