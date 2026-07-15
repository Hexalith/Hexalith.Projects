# Final Reconciliation: U+2028/U+2029 Idempotency Canonicalizer Parity

## Input

- Approved source: `sprint-change-proposal-2026-07-14-u2028-u2029-idempotency-parity.md` (approved 2026-07-14).
- Compared with: the current `prd.md`, `addendum.md`, and `.memlog.md` in this PRD workspace (audited 2026-07-15).
- Verdict: **Fully reconciled — no current gap remains.**

## Extracted Requirements and Decisions

### Product Requirement Content

- Equivalent retries must remain equivalent across supported server and generated-client surfaces: reuse of the same scoped Idempotency Key with equivalent accepted input returns the original Durable Task, while materially different reuse conflicts and cannot duplicate effects.
- Accepted descriptive metadata containing U+2028 or U+2029 must not cause cross-surface fingerprint drift. Identifier and envelope contexts must reject those separators where their line-breaking behavior is unsafe.
- The correction must preserve compatibility: unaffected canonical hashes remain stable, and a canonical-byte change must not invalidate live persisted legacy fingerprints without an approved bounded compatibility treatment.
- Release evidence must prove the persisted-boundary, duplicate-delivery, compatibility, deployment, smoke, and rollback obligations. An unavailable deployment environment or unresolved critical case is not passing evidence.
- This is a retry-safety correction, not a new feature, journey, visual behavior, or public API shape. Historical completion records remain historical evidence rather than being rewritten.

### Implementation, Verification, and Delivery How

- Escape accepted U+2028/U+2029 values in the server canonicalizer exactly as literal backslash-`u` sequences while keeping the generated-client production implementation unchanged; align identifier validation and do not hand-edit generated `.g.cs` files.
- Invoke the real server validator and a real generated request helper in direct parity tests. Prove byte-identical canonicalization/fingerprints, non-collision with LF and literal backslash-`u` text, and stability of hashes for unaffected input.
- Inspect deployed durable idempotency state before changing canonical bytes. If affected legacy fingerprints exist, stop the direct change and define a bounded legacy-hash fallback or equivalent compatibility strategy for the retention window.
- The proposed Epic 5 Story 5.12 slot, architecture wording, sprint-status edit, code/test targets, estimate, build commands, and action-item lifecycle are delivery mechanics. They belong in repository-local architecture, story, sprint, test, and release artifacts rather than the capability-oriented PRD.

## Current Coverage

- `prd.md` FR-1 and the Idempotency Key glossary define equivalent retry/original-task behavior and changed-request conflict; the mutation FRs extend durable/idempotent behavior across consequential workflows.
- `prd.md` FR-19 requires rejection of control/invisible characters where unsafe and safe field-only diagnostics. NFR-4 owns durability and idempotency; NFR-10 owns compatibility and historical readability; NFR-11 owns persisted-boundary, duplicate-delivery, deployment, smoke, rollback, and no-false-pass evidence.
- Addendum §2 explicitly requires Unicode-safe request-equivalence canonicalization, including U+2028/U+2029 parity without broadening equivalence, and routes exact wire fields and algorithms to downstream design.
- Addendum §7.2 explicitly requires rejection in identifier/envelope fields, deterministic escaping in accepted descriptive metadata, real-server/generated-helper byte parity, LF and literal backslash-`u` non-collision, unaffected-hash stability, generated-file integrity, deployed-state inspection, and a bounded legacy-hash strategy when affected persisted fingerprints exist.

## Gap Summary

**None remain.** The current PRD/addendum set captures the observable idempotency, validation, compatibility, and release-evidence obligations and preserves the separator algorithm, regression vectors, generated-artifact rule, and deployment-state gate at the correct implementation-facing level.

## Conflict Summary

- **No unresolved conflict with `.memlog.md` decisions.** The reliability and retention/replay decisions require idempotent retries, original-task recovery, changed-payload conflict, non-duplication, and retained scoped idempotency evidence. The later memlog changes explicitly route idempotency mechanisms to the addendum and record Unicode idempotency compatibility as a closed reconciliation gap.
- The proposal's reference to **NFR-7** as the idempotency owner is stale numbering, not a decision conflict. In the current PRD, **NFR-4** owns durability/idempotency; NFR-7 owns back-pressure and dependency control.
- The proposal's Epic 5 Story 5.12/direct-implementation route is superseded by the later memlog-recorded readiness freeze and sequencing reflected in the addendum: Epics 1–5 remain implementation history, and corrective development is not authorized until the current readiness and release gates pass. This supersedes the delivery vehicle without weakening the approved Unicode parity invariant.

## Disposition

- Leave `prd.md`, `addendum.md`, and `.memlog.md` unchanged.
- Accept this input as fully reconciled.
- Preserve the Unicode parity correction as a downstream implementation and verification obligation subject to the current readiness and deployment-state gates.
