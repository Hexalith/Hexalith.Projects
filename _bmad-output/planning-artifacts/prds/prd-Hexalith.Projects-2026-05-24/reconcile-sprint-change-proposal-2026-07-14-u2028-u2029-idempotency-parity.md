# Source Reconciliation: U+2028/U+2029 Idempotency Parity

Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-u2028-u2029-idempotency-parity.md`
Targets: `prd.md` and `addendum.md`

## Verdict

**Substantially captured; three downstream gaps remain.** The revised PRD now owns the observable retry/idempotency contract, and the addendum preserves Unicode canonicalization parity. No new feature, journey, UI behavior, or public contract shape is needed.

## Captured Items

- **Equivalent retry semantics:** `prd.md` §3 (`Idempotency Key`), FR-1, FR-21, NFR-4, and NFR-8 require scoped keys, original-task replay for equivalent requests, conflict for changed requests, non-duplication, and retained idempotency evidence.
- **Idempotent mutation behavior:** FR-1, FR-3, FR-4, FR-6, FR-7, FR-11, and FR-23, plus §6.1, establish durable/idempotent behavior across consequential Project workflows.
- **Unicode parity:** `addendum.md` §2 explicitly requires Unicode-safe request-equivalence canonicalization, including U+2028/U+2029 parity without broadening equivalence.
- **Compatibility and release posture:** `prd.md` NFR-10/NFR-11 and `addendum.md` §5/§7 require compatibility, rollback, deployment, persisted-boundary, duplicate-delivery, and no-false-pass evidence.
- **Technical routing:** `addendum.md` §2 and §8 correctly route exact algorithms, idempotency canonicalization, API detail, tests, migration, and story slicing outside the PRD.
- **No UX/feature change:** The source's no-UX, no-new-FR, no-OpenAPI-shape conclusion is consistent with the revised artifacts.

## Remaining Gaps

1. **Identifier policy:** FR-19 generically rejects control/invisible characters “where unsafe,” but neither artifact explicitly requires U+2028/U+2029 rejection in identifier/envelope fields while permitting deterministic escaping in accepted descriptive metadata.
2. **Parity acceptance evidence:** The addendum does not retain the approved direct real-server/generated-helper proof, byte-level parity, non-collision against LF and literal backslash-u text, stability of unaffected hashes, or the prohibition on hand-editing generated `.g.cs` files.
3. **Deployment-state compatibility gate:** Neither artifact explicitly requires inspection for live persisted legacy fingerprints before changing canonical bytes, nor the source's escalation to a bounded legacy-hash compatibility strategy if affected entries exist.

## Contradictions and Stale References

- The proposal says **NFR-7** owns field-scoped idempotency. In the revised PRD, the governing product requirement is **NFR-4 — Durability and idempotency**; NFR-7 now covers back-pressure and dependency control. This is a stale citation, not a product-decision conflict.
- The proposal's “PRD: no change” conclusion is now functionally true only as **no further PRD change**: the revised PRD has since added the idempotency requirements absent from the earlier draft.
- The proposal's direct-adjustment classification remains conditional; treating the change as unconditionally Minor would contradict its own unresolved deployment-state gate.

## Qualitative Intent to Preserve

- This is a retry-safety and security invariant, not cosmetic Unicode normalization.
- Server and generated SDK must prove the same logical equivalence directly; isolated green tests are insufficient.
- Accepted human-authored text should remain accepted where intended, while identifiers remain structurally safe and single-line.
- The fix must not break unaffected consumers or persisted deduplication history.
- Historical stories and retrospectives remain honest; corrective ownership is added rather than prior completion rewritten.
- Generated artifacts remain generated; fixes belong in source canonicalizers and durable parity tests.

## Disposition

- **PRD:** Captured; no new FR or product-scope change. Correct references to NFR-4, with NFR-10/NFR-11 supporting compatibility and release evidence.
- **Addendum/architecture/test strategy:** Carry the three gaps above into the idempotency mechanism, compatibility gate, and verification design.
- **Epics/sprint/implementation:** Preserve the approved Story 5.12 ownership, code targets, estimate, test commands, and action-item lifecycle in their repository-local artifacts; these are intentionally outside the PRD.
- **Overall:** Accept as reconciled with three non-PRD downstream gaps and no unresolved product contradiction.
