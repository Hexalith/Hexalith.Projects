# Final Reconciliation: CreateProject `metadataClass` Enforcement

**Input:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14.md`
**Reconciled against:** `prd.md` and `addendum.md`

## Captured Items

- FR-1 and FR-19 preserve Project name as the only user-authored creation field while requiring valid system-supplied Metadata Classification on canonical requests.
- Historical unversioned name-only creation remains accepted throughout v1; NFR-10 and addendum section 4.1 require a major-version decision, migration evidence, compatibility tests, and rollback criteria before retirement.
- Invalid classification is rejected before command submission, with safe field/reason codes that do not echo sensitive values.
- The classification remains an integration/API safety contract rather than a new UX field, feature, lifecycle change, or schema-driven product expansion.
- Addendum section 4.1 correctly routes classification source, compatibility adapter, rejection behavior, and contract evidence to downstream contract planning.

## Gaps

1. **Exact wire vocabulary and invalid-input semantics are absent.** The final artifacts do not retain the four allowed values (`public_metadata`, `tenant_sensitive`, `credential_sensitive`, `secret`) or explicitly cover missing, blank, null, non-string, case/whitespace, duplicate-property, and unknown-value behavior.
2. **Classification ownership and use remain unresolved.** “System-supplied” does not identify which adapter derives the tier, by what policy, how malformed canonical requests are distinguished from the legacy name-only shape, or whether a valid tier is propagated beyond boundary validation.
3. **The precise boundary/error contract is deferred.** The proposal's authorization-before-parsing order, `400 ValidationFailure`, exact `details.rejectedField`, no-value echo, and guarantee that no command submitter is invoked are only captured at a higher-level intent.
4. **Implementation acceptance is not preserved.** The OpenAPI required-field/enum test, four-case endpoint matrix, leakage assertion, fingerprint gate, warning-free Release build, and open-until-green sprint-action state are not recorded in the final workspace.

## Contradictions

- There is **no direct product-contract contradiction**: strict canonical classification and deliberate legacy acceptance coexist, and classification is not user-authored.
- FR-19's statement that validation rejects “secrets” is potentially ambiguous beside the allowed classification token `secret`. The final interpretation must be that secret content/payloads are rejected; the `secret` sensitivity label itself remains valid and authorizes no payload storage or disclosure.
- Proposal approval does not mean implementation completion. The input explicitly says the correction was not yet implemented; the final PRD's product readiness must not be read as evidence that its server/test gates passed.

## Qualitative Intent at Risk

The critical intent is “strict canonical path, tolerant only on the intentional legacy path.” Classification must remain integration-supplied rather than becoming user burden; direct creation and proposal confirmation must share one vocabulary; invalid input must cause no durable effect and no metadata leakage; and the bounded fix must not trigger schema, generated-client, UX, domain, infrastructure, or MVP churn.

## Disposition

**Verdict: Substantially captured; four contract/implementation gaps remain.** Keep the PRD product contract unchanged. Preserve the proposal as the canonical technical source and carry gaps 1–4 into the API contract, compatibility adapter, server implementation, and focused test/release evidence. Treat the sprint action as open until those gates are green and recorded.
