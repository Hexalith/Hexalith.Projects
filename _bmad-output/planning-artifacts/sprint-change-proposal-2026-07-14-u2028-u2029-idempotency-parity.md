# Sprint Change Proposal: U+2028/U+2029 Idempotency Canonicalizer Parity

**Date:** 2026-07-14
**Project:** Hexalith.Projects
**Prepared for:** Jerome
**Status:** Approved for implementation
**Change scope:** Minor, contingent on the deployment-state gate below

## 1. Issue Summary

Hexalith.Projects has a confirmed cross-surface idempotency defect for Unicode LINE SEPARATOR (`U+2028`) and PARAGRAPH SEPARATOR (`U+2029`). The generated-client canonicalizer escapes these accepted string values as the six-character sequences `\u2028` and `\u2029`, while the server-side `ProjectCommandValidator.Escape` method leaves the code points literal. The two paths therefore hash different UTF-8 canonical bytes for the same spine-declared equivalence input.

The defect was first recorded by the Epic 2 retrospective, carried through the read-only Epic 3, and marked **Not demonstrated** by the Epic 4 retrospective after new mutation surfaces shipped. It remains an open Epic 5 action item in `sprint-status.yaml`.

### Evidence

- `src/Hexalith.Projects.Client/Idempotency/HexalithIdempotencyHasher.cs` explicitly maps `U+2028` to `\u2028` and `U+2029` to `\u2029`.
- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs` escapes backslash, tab, CR, LF, semicolon, equals, and `char.IsControl` values, but `U+2028`/`U+2029` are Unicode separator categories rather than control characters and fall through unchanged.
- `ProjectCommandValidator.IsSafeMetadata` accepts embedded `U+2028`/`U+2029`, so the divergent server fingerprint is reachable through mutation metadata.
- Existing server and client tests cover ordinary canonical hashes, order, duplicates, and control characters, but contain no `U+2028`/`U+2029` server/generated-helper parity vector.

### Consequence

For an affected accepted value, the generated SDK's retry key and the server's canonical payload fingerprint disagree. This weakens the load-bearing NFR-7 guarantee and can turn an equivalent retry into a conflict or allow inconsistent deduplication behavior across surfaces.

## 2. Impact Analysis

### Epic Impact

- **Epic 1:** Story 1.3 established the canonical server/generated-client fingerprint contract. Its historical completion remains valid; this proposal closes a later-discovered edge-case defect rather than rewriting that story.
- **Epic 2:** The retrospective correctly identified the divergence. No historical edit is needed.
- **Epic 3:** No impact. Its query surfaces reject `Idempotency-Key` and do not compute mutation fingerprints.
- **Epic 4:** Stories 4.4 and 4.5 exposed new mutation surfaces and strengthened ordinary lockstep tests, but did not prove separator parity. Their completed records remain unchanged.
- **Epic 5:** Add a bounded corrective Story 5.12 because the open action item is already assigned to Epic 5 and `epic-5` remains `in-progress` in sprint status. No new epic is required.

### Story Impact

- Add Story 5.12, **Harden U+2028/U+2029 idempotency canonicalizer parity**.
- Do not reopen or rewrite completed Stories 1.3, 4.4, 4.5, or their retrospectives.
- Preserve all existing canonical hashes for inputs that do not contain the two separator code points.

### Artifact Conflicts

- **PRD:** No change. NFR-7 already requires field-scoped idempotency under at-least-once delivery.
- **Epics:** Add Story 5.12 with explicit byte-level parity acceptance criteria.
- **Architecture:** Strengthen the idempotency format rule with an explicit separator policy and mandatory server/generated-helper regression vectors.
- **UX specification:** No change. The correction has no visual, interaction, accessibility, or operational-console effect.
- **Sprint status:** Add Story 5.12 as `backlog`; keep the existing action item open until verified implementation completes.
- **Historical implementation and retrospective artifacts:** No change; they remain accurate evidence of when the gap was discovered and deferred.

### Technical Impact

Expected implementation scope:

1. Update the server canonicalizer to escape accepted string-value `U+2028`/`U+2029` exactly as the generated-client hasher does.
2. Align server envelope/identifier validation so the two separators are rejected in identifier contexts rather than admitted as line-breaking characters.
3. Add direct parity coverage that invokes the real server validator and a real generated request's `ComputeIdempotencyHash()` for both code points.
4. Pin non-collision behavior against LF and literal backslash-u text.
5. Retain the existing client production implementation; it already has the intended escaping. Do not hand-edit generated `.g.cs` files.

No OpenAPI shape, generated-client output, package, database schema, Dapr topology, deployment manifest, or submodule pointer change is expected.

### Deployment-State Gate

Changing the server hash for these inputs can invalidate an already-persisted fingerprint created by the legacy behavior. Before implementation is classified as unconditionally Minor, confirm that no live durable idempotency ledger contains accepted mutations with embedded `U+2028`/`U+2029`. The current release handoff records production deployment as unverified and stakeholder acceptance as not granted, so no migration is presently indicated.

If live affected fingerprints are found, stop and reclassify the change as **Moderate**. Define a bounded compatibility strategy—such as legacy-hash fallback during the ledger retention window—before changing runtime behavior.

## 3. Recommended Approach

### Chosen Path: Direct Adjustment

Add one corrective story to the existing Epic 5 plan, make the server canonical bytes match the already-correct generated-client policy, and add direct cross-assembly regression vectors.

This is preferred because:

- The PRD and MVP goals already require the behavior; no scope decision is changing.
- Reopening completed stories would rewrite history without improving implementation clarity.
- Deferring again leaves a known idempotency/security invariant broken on accepted input.
- Rollback is unnecessary; the existing canonicalizer design is sound apart from two missing switch cases and verification.

### Estimate and Risk

- **Effort:** approximately 0.5–1 engineering day, including focused and full-solution verification.
- **Schedule impact:** one small corrective backlog slot; no MVP feature displacement.
- **Implementation risk:** low if the deployment-state gate confirms no affected durable fingerprints.
- **Invariant risk if deferred:** moderate, because equivalent retry behavior can diverge across server and SDK surfaces.
- **Compatibility risk:** bounded to historical commands containing embedded `U+2028`/`U+2029`; use the deployment-state gate above.

## 4. Detailed Change Proposals

### 4.1 Epics — Add Story 5.12

**Artifact:** `_bmad-output/planning-artifacts/epics.md`
**Section:** Epic 5, after Story 5.11

**OLD:**

Epic 5 ends with Story 5.11, **Cross-surface parity, responsive design & accessibility hardening**.

**NEW:**

### Story 5.12: Harden U+2028/U+2029 idempotency canonicalizer parity

As a **Projects API and SDK consumer**,
I want **server and generated-client fingerprinting to canonicalize Unicode line and paragraph separators identically**,
So that **retries cannot bypass deduplication because the two surfaces hash equivalent input differently**.

**Acceptance Criteria:**

- Embedded U+2028 and U+2029 in accepted mutation metadata are escaped as literal `\u2028` and `\u2029` canonical bytes before hashing.
- Server validation and generated `ComputeIdempotencyHash()` produce identical `sha256:` fingerprints for both separator vectors.
- Separator characters remain distinct from LF and from literal backslash-u text.
- Identifier contexts reject U+2028/U+2029 consistently.
- Tests exercise the real server validator and generated request helper; generated `.g.cs` files are not hand-edited.
- Existing mutation hashes without these characters remain unchanged.
- The deployment-state gate confirms no affected live fingerprints, or an approved compatibility strategy is implemented before the server hash changes.

**Justification:** Converts the long-running retrospective action item into an owned, testable implementation slice without changing feature scope.

### 4.2 Architecture — Make Separator Parity Explicit

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`
**Section:** Implementation Patterns & Consistency Rules → Format Patterns → Idempotency

**OLD:**

> Stable `Idempotency-Key` per logical attempt (reused on retry); field-scoped equivalence via the shared hasher; required on mutations, rejected on queries.

**NEW:**

> Stable `Idempotency-Key` per logical attempt (reused on retry); field-scoped equivalence via the shared hasher; required on mutations, rejected on queries. Server and generated-client canonicalization must produce byte-identical canonical bytes and `sha256:` fingerprints. Accepted string values escape U+2028 and U+2029 as literal `\u2028` and `\u2029`; identifier contexts reject them. Every canonicalizer change requires direct server/generated-helper parity vectors, including non-collision against LF and literal backslash-u text.

**Justification:** Makes the byte-level invariant durable so future canonicalizer or serializer work cannot repeat the drift.

### 4.3 Sprint Status — Track the Corrective Story

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`
**Section:** Epic 5 story list

**OLD:**

```yaml
5-11-cross-surface-parity-responsive-design-accessibility-hardening: done
epic-5-retrospective: done
```

**NEW:**

```yaml
5-11-cross-surface-parity-responsive-design-accessibility-hardening: done
5-12-harden-u2028-u2029-idempotency-canonicalizer-parity: backlog
epic-5-retrospective: done
```

Keep the existing Epic 5 action item open until implementation and verification succeed.

**Justification:** Gives the open action item normal story ownership and preserves historical completion records.

### 4.4 No-Change Decisions

- **PRD:** Existing NFR-7 is sufficient.
- **UX:** No user-interface behavior changes.
- **OpenAPI and generated artifacts:** No contract shape changes; generated files remain untouched.
- **Completed stories and retrospectives:** Preserve as historical evidence.

## 5. Implementation Handoff

### Scope Classification

**Minor — direct implementation by the Developer agent**, provided the deployment-state gate passes. Escalate to **Moderate** only if live affected fingerprints require a compatibility window or migration behavior.

### Recipients

- **Amelia / Developer:** implement the server escape and identifier-validation alignment; add direct parity tests; preserve unrelated hashes and generated artifacts.
- **Murat / Test Architect:** verify the separator vectors, non-collision cases, and regression scope; confirm focused and full lanes are green.
- **Jerome / Project Lead:** confirm deployment-state evidence and approve any compatibility strategy if affected durable fingerprints exist.

### Suggested Implementation Targets

- `src/Hexalith.Projects/Aggregates/Project/ProjectCommandValidator.cs`
- `tests/Hexalith.Projects.Tests/Aggregates/Project/ProjectCommandValidatorTests.cs`
- `tests/Hexalith.Projects.Client.Tests/ClientGenerationTests.cs` or a focused parity test in that project
- A test-only project reference if needed to invoke both real implementations in one assertion; no production dependency inversion

### Verification

1. For `Synthetic\u2028Project` and `Synthetic\u2029Project`, the accepted server command fingerprint equals the generated `CreateProjectRequest.ComputeIdempotencyHash()` result.
2. Expected canonical bytes contain literal backslash-u escapes, not raw separator code points.
3. U+2028/U+2029, LF, and literal `\\u2028`/`\\u2029` inputs do not collide.
4. Identifier/envelope contexts reject both separator code points with safe field-name-only diagnostics.
5. Existing canonical hash fixtures remain unchanged.
6. Focused `Hexalith.Projects.Tests` and `Hexalith.Projects.Client.Tests` lanes pass.
7. `dotnet build Hexalith.Projects.slnx -warnaserror` and the full solution test lane pass with zero warnings, failures, and skips attributable to the change.
8. OpenAPI fingerprint and generated-artifact staleness gates remain green with no `.g.cs` diff.
9. `git diff --check` is clean and no root submodule pointer changes.

## Checklist Disposition

- [x] Trigger and evidence confirmed.
- [x] Epic and story impact assessed.
- [x] PRD conflict assessed — no edit required.
- [x] Architecture conflict assessed — explicit invariant approved.
- [N/A] UX impact — no visual or interaction change.
- [x] Technical scope and deployment compatibility risk assessed.
- [x] Direct-adjustment path selected over rollback, deferral, or MVP change.
- [x] Incremental edits approved for Epics, Architecture, and Sprint Status.
- [x] Final proposal approved and routed for implementation.

## 6. Approval and Routing Record

- **Approved by:** Jerome
- **Approval date:** 2026-07-14
- **Approval signal:** Repeated Continue (`c`) response after the final approval prompt; recorded as approval to proceed with the proposal's implementation handoff.
- **Final classification:** Minor, contingent on the deployment-state gate.
- **Routed to:** Amelia / Developer for implementation; Murat / Test Architect for separator-vector and regression verification; Jerome / Project Lead for deployment-state confirmation.
- **Implementation status:** Not started by this workflow. The Epic 5 action item remains open until the implementation and verification criteria pass.
