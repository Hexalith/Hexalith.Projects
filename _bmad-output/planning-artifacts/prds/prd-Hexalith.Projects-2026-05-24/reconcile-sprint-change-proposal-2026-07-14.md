# Final Reconciliation: CreateProject `metadataClass` Enforcement

**Input:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14.md`  
**Reconciled against:** `prd.md`, `addendum.md`, and `.memlog.md`  
**Input status:** Approved 2026-07-14; implementation was explicitly not complete when the proposal was approved.

## Extracted Requirements and Decisions

### Product requirement content

- Project name remains the only required **user-authored** creation input. Metadata Classification is safety metadata supplied by an authenticated integration, not a new user field or UX requirement.
- A canonical, versioned Create Project request that contains `projectMetadata` must contain a valid `metadataClass`. The allowed wire values are `public_metadata`, `tenant_sensitive`, `credential_sensitive`, and `secret`.
- Only the historical unversioned name-only request receives the v1 compatibility treatment. It remains accepted throughout v1 and can be retired only through an explicitly approved major version with migration, usage, compatibility, and rollback evidence.
- Invalid canonical classification must be rejected before command submission, identify only the rejected field, echo no submitted value, and cause no durable effect. The `secret` token is a sensitivity label and never authorizes storage or disclosure of secret content.
- The correction does not add a lifecycle, domain, event, projection, persistence, UX, or generated-client capability and does not change the existing OpenAPI schema.

### Implementation and verification how

- Authorization precedes protected-body parsing; classification validation occurs after parsing and before command construction or submission.
- The approved implementation factors the existing four-value check into one server-owned `SensitiveMetadataTierValidator`, reused by direct Create Project and proposal confirmation without changing proposal-confirmation behavior.
- Invalid input returns metadata-only `400 ValidationFailure`, with proposal-approved `details.rejectedField = "projectMetadata.metadataClass"`, and never invokes `IProjectCommandSubmitter`.
- Verification comprises the OpenAPI required-field/reference/exact-enum assertions; the legacy-valid, canonical-valid, canonical-missing, and canonical-unsupported endpoint matrix; leakage/no-command assertions; the OpenAPI fingerprint gate; focused contract/server tests; and a warning-free Release build.
- The implementation action remains open until those gates are green and their exact commands/results are recorded; proposal approval alone is not implementation or release evidence.

## Coverage in the Current PRD Workspace

- `prd.md` §2.1, FR-1, FR-19, and NFR-10 preserve the user-input boundary, strict canonical classification, deliberate legacy compatibility, pre-submission rejection, safe errors, and major-version retirement gate.
- `addendum.md` §4.1 preserves the four-value vocabulary, canonical-versus-legacy shape distinction, authenticated integration ownership, malformed-value cases, exact `projectMetadata.metadataClass` rejected-field path, no-value echo, no-command behavior, shared-validator reuse, and compatibility retirement conditions.
- `addendum.md` §7.1 preserves the OpenAPI/endpoint/leakage/shared-validator/fingerprint/build verification categories and the open-until-green sprint-action rule.
- `addendum.md` E-9 cites the approved proposal and records its implementation/evidence status without treating planning approval as completion.
- `.memlog.md` records the canonical-versus-legacy product decision, classifies detailed wire/error/test material as addendum content, and explicitly corrects its earlier overstatement after the three reconciliation fixes.

## Remaining Gaps

None remain. The product contract is captured in `prd.md`; the API-boundary mechanism, exact error contract, compatibility treatment, implementation decision, verification matrix, proposal provenance, and open-until-green status are captured in `addendum.md` without promoting technical how into the PRD.

## Memlog Conflict Audit

- No current conflict remains. The canonical-classification and historical-compatibility decisions agree with the proposal, PRD, and addendum.
- The latest memlog change explicitly corrects the earlier closure overstatement and matches the repaired rejected-field path, shared-validator decision, and E-9 traceability.
- No memlog entry says the implementation or verification gates passed. The proposal's “not yet implemented” state remains consistent and cannot be upgraded to completion by inference.

## Disposition

**Verdict: Fully reconciled; no gaps or memlog conflicts remain.** Keep the PRD product requirements unchanged and continue to treat implementation completion as contingent on recorded green evidence.
