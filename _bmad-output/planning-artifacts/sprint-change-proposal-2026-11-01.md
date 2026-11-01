---
title: 'Simplify the Epic 6 entry gate for authenticated Project list/open'
date: '2026-11-01'
status: 'proposed-awaiting-approval'
change_scope: 'moderate: bounded development exception, backlog routing, and scheduling guard/tests'
mode: 'batch'
approval: 'pending; this document does not apply the proposed changes'
related_proposal: 'sprint-change-proposal-2026-11-01-close-story-6-4.md'
---

# Sprint Change Proposal: let authenticated list/open finish on its own acceptance

## 1. Issue summary

**Yes: the entry gate is over-engineered for Story 6.1.** It combines necessary read/security capabilities with platform distribution, historical package qualification, writer rollback, and whole-plan approval. Those obligations have valid owners, but most do not determine whether authenticated list/open works. Story 6.5 inherits the entire chain through DW-1.

The correction is to accept the feature at a recorded source commit using its actual authenticated and persisted tests, following the approved November 1 story completion rule. Here, shipping Story 6.1 means delivering and accepting the supported read capability. Production deployment, publication, public-route cutover, and terminal release acceptance retain their separate gates.

### Evidence and trigger

| Repository evidence | Finding |
| --- | --- |
| [Epic 6 entry gate and work packages](epics.md#epic-6-chatbot-and-operators-retrieve-authorized-project-truth) | P1R → P0/P2 → P3 → same-baseline architect signature → P4 clean-checkout/full-run acceptance → specification readiness → independent READY precedes feature readiness. P0 also requires published tooling and consumer adoption. |
| [Sprint index](../implementation-artifacts/sprint-status.yaml) | Story 6.1 is blocked with the comment `wait for eventstore 6.2`; P0/P2/P3/P4 and P1R remediation remain open. |
| `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/sprint-status.yaml` | `6-2-folded-snapshot-implementation: done`. The comment is stale even on its own terms. This owner status is not proof of every Projects read/security requirement. |
| [Story 6.4 closure proposal](sprint-change-proposal-2026-11-01-close-story-6-4.md) and Epic 6's November 1 completion rule | Publication, adoption, producer security, platform qualification, and deployment must not extend a finished feature's acceptance. |
| `src/Hexalith.Projects.Server/Queries/ListProjectsQueryHandler.cs`, `GetProjectQueryHandler.cs`, and [supported-read documentation](../../docs/supported-project-reads.md) | Supported SDK handlers, authenticated gateway/owner composition, scoped paging, projection persistence, audit, and a disposable distributed verification lane already exist. This is not a proposal to start by rebuilding a platform. |
| Supported-read documentation, final October 27 replay qualification | Records eleven distributed matrix rows, Integration 128/128, Server 1306/1306, and launcher controls 63/63 with no failures/skips. It records a real Projects process restart, Folder revocation, duplicate delivery, and full replay. These are recorded historical results, not tests rerun for this proposal. |
| Same document's shadow-comparison and authority limitations | Local shadow comparison is not accepted full gateway/owner equivalence; live enrollment and package-mode qualification are unclaimed. Those limitations must remain explicit. |
| [DW-1](../implementation-artifacts/deferred-work.md#dw-1-deliver-and-accept-story-65-supported-read-prerequisites) and [6.5 spec](../implementation-artifacts/spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md) | Require a separate immutable/hash-pinned prerequisite record and executable G-4 reads/web-reads profiles in addition to delivered 6.1–6.4 contracts and FrontComposer identity. |
| `tools/planning/validate_production_authority.py` | `EPIC6_DEVELOPMENT_STATUSES` pins 6.1 and 6.5 to `blocked`. Changing prose or YAML alone cannot unblock either. |

**Category:** process/dependency modeling correction. No product-scope reduction, requirement renumbering, or acceptance of an incompatible runtime is proposed.

## 2. Impact analysis

### Epic and story impact

- **Story 6.1:** replace the P0–P4 development chain with the two capability prerequisites below. Finish/review existing implementation against feature acceptance. Do not mark it done from this proposal or from historical pass counts alone.
- **Stories 6.2/6.3/6.4:** preserve their completed records and accepted scopes. This correction does not retroactively certify their runtime or release environment.
- **Story 6.5 / DW-1:** retain dependence on the actual 6.1–6.4 read contracts it renders and working FrontComposer authentication/token isolation. Remove inherited publication, common-runner, and duplicate prerequisite-record requirements. It remains blocked until its real dependencies are delivered and its revised spec is ready.
- **Story 6.6 / 6.8:** preserve the Contracts split and McpCli ownership. No scheduling advance is proposed.
- **Story 6.7:** owns supported/public read-route cutover, end-to-end legacy equivalence, consumer compatibility, and reversible read routing. Move the exhaustive pre-cutover comparison gate here; retain useful local comparators as feature regressions.
- **Epic 7:** preserve Durable Task/Confirmation, sibling write contracts, single-writer fencing, and safe writer recovery. Read completion cannot authorize an incapable historical writer.
- **Epic 8:** 8.7 owns package/tool publication and consumption evidence; 8.8 owns deployed authentication/isolation/privacy integration; 8.9 owns scale/back-pressure; 8.10 owns platform resilience; 8.11-P1/P2/P3 and 8.11 own deployment, KMS/enrollment, rollback drills, and terminal acceptance. No Epic 8 story advances.

### Conflicting artifacts

This is broader than deleting one paragraph. The global freeze also appears in `epics.md` frontmatter, Requirements Inventory, Corrective Production Plan containment, PRD §2.3/§2.5 and addendum containment, Architecture AD-25/AD-30 and Stack notes, project context, the traceability matrix, and the active 6.1/6.5 specs. E1–E7 below reconcile these operational instructions together. Dated reports and sealed evidence remain historical; they are not rewritten into READY.

PRD FR-2/FR-5, NFR-1, pagination, AD-19/20/32/33, and UX response semantics remain binding. UX screens, flows, wireframes, and accessibility requirements need no redesign. The 6.5 browser journey still needs its own authentication, accessibility, isolation, and no-write evidence.

### Technical impact and limits

The implementation change for this course correction is limited to the Projects scheduling guard and its tests. No dependency bump, submodule update, runtime change, CI bypass, publication, enrollment, or deployment is authorized. Existing source-mode evidence cannot certify published binaries. If a required read capability actually fails in the selected implementation, fix that specific defect in its owning repository; do not waive it because a larger package was moved out of the feature gate.

The current dirty deferred-work ledger, release catalog, and EventStore checkout must be preserved. DW-76 remains an independent current-CI repair; the implementation must inspect its current entry rather than overwrite it with the older 6.4 proposal text.

## 3. Recommended approach

**Direct adjustment, moderate scope.** Apply one bounded development exception for 6.1 and its 6.5 consumer. Do not replace six work packages with six newly named gates or another approval framework.

### The only two capability prerequisites for 6.1

1. **A usable supported read substrate.** At the source revision under test, the existing EventStore SDK must execute list/open through its authenticated query adapter, `IDomainQueryHandler`, incremental persisted read models, and platform cursor codec. Required projections must preserve their actual event sequence/global-position evidence, recover/replay correctly, and report missing or invalid evidence honestly. Record the tested Projects and directly used dependency commits with the test result; no new package publication, tool distribution, all-platform qualification, or old-writer rollback ceremony is required to start or accept the feature.
2. **A usable fail-closed authority path.** Authenticated Tenant, original actor, workload/delegation, audience, scope, and action must survive the actual gateway → Projects → Folders path. Chatbot visibility is current Folder-read-derived and filtered before paging; Tenant-role callers use the permitted surface and Safe Metadata default. Descriptive inspection requires distinct permission and durable metadata-only audit. Required authentication configuration must fail closed. A service-only grant, synthetic permission model, or handler-only allow stub cannot prove this prerequisite. Production signer custody, live grants, and enrollment remain release work.

These capabilities can be demonstrated using the existing controlled Development composition with real JWT validation, workload assertions, owner handlers, and persisted storage. Controlled fixture credentials are not production enrollment. No new owner signature is needed to restate an already implemented API; changes to an external API or trust policy still require that repository's normal authorization.

### What proves the feature is done

Use the ordinary revised story specification and the November 1 completion rule:

- Actual authorized list/open traverses the supported adapter and SDK, with current Folder authorization before pagination, default 50/cap 200, deterministic ordering and scoped/tamper-resistant cursors.
- Denied, absent, and cross-Tenant targets have indistinguishable safe denial; invalid structural input follows the existing authorized `400` policy. Missing delegation, wrong audience/scope, stale authority, and permission revocation fail closed.
- AD-32 state is truthful: current required Project/Folder/Setup/authority evidence for usable open; authorized/current enumeration plus per-entry state for list; optional omissions explicit; unavailable or Archived context is never admitted. Unknown evidence never becomes Complete. Legacy Active folderless data stays quarantined under the PRD; authorized Archived recovery inspection does not authorize context use.
- Names, Setup, and other descriptive fields follow the existing allowlist and inspection policy; denial/security/inspection audit stays metadata-only and disclosure fails if required audit cannot persist.
- Queries append no domain events and mutate no Project, sibling, task, resolution, or repair state. Required security/inspection audit is the explicit exception, observed separately.
- Focused regressions plus a real persisted authenticated run pass, including required projection replay/duplicate behavior, cursor behavior, Projects restart, and authority-change controls. Reuse the existing lane and fixtures; do not build a replacement runner.
- Record the accepted commit, exact commands, pass/fail/skip totals and actual XML plus the relevant result report in the existing story. Retain required state assertions in tests. Full unchanged-file hash inventories, new evidence schemas, and proof refresh after unrelated submodule movement are not completion requirements.

Story readiness checks these concrete inputs and executable commands. It does not wait for a new whole-plan independent READY verdict. Existing green evidence may be reused at its recorded revision if it covers the revised acceptance and its artifacts remain available; missing artifacts or an uncovered criterion require only the relevant verification. Exhaustive legacy/public-route equivalence remains mandatory before 6.7 cutover, rather than before completing the parallel supported read capability.

### Effort, risk, and schedule

Planning/guard alignment: approximately one engineering day, including focused tests. Final 6.1 acceptance: inspect existing receipts first, then run only uncovered or unavailable evidence; allow another half to one day if the existing lane needs a fresh run, excluding actual defects. These are estimates, not commitments. This removes publication and multi-owner platform lead time from the feature's critical path.

Risk is moderate because several current instructions enforce the old freeze. Reconcile them atomically and preserve fail-closed negative controls. Security behavior is retained. Current scale limitations (full authorization sweep per page and bounded batch/index size) remain disclosed and owned by 8.9; no 10,000-Project performance claim follows from functional completion.

**Alternatives:** rollback has no useful target because the feature implementation exists; cutting the MVP would remove required security/value without addressing the dependency error. Keeping P4 but shortening its paperwork leaves the same wrong critical path.

## 4. Detailed change proposals

### E1. Epic 6 entry and completion boundaries

**Files:** `epics.md` and `implementation-artifacts/epic-6-context.md`.

**OLD:** every shared read consumer requires accepted 6.1-P0/P1/P1R/P2/P3/P4; G-6 precedes any affected passing build/evidence claim; the executable critical path ends in whole-plan READY. Story 6.1 AC requires exhaustive shadow equivalence before completion.

**NEW:** use the two capability prerequisites and feature acceptance in §3. Replace the executable critical path with:

```text
working SDK read path + working authenticated actor/Folder authority
  -> ordinary focused story-spec readiness
  -> finish/review 6.1 and accept its recorded feature evidence
  -> 6.5 readiness against delivered contracts + its FrontComposer auth seam
```

Replace the shared mandatory G-4 fixture with the existing individual test-project/distributed Development lane. G-6 remains a platform/runtime qualification and exact-source release requirement: report actual focused story outcomes without claiming G-6 passed. A relevant runtime failure still fails the affected story test; an unrelated platform qualification failure is not a feature prerequisite. Keep CI/release gates intact.

Retain the existing November 1 completion rule. Move exhaustive shadow equivalence, canonical key/watermark/cursor comparison, approved normalization, and reversible public routing to 6.7; its acceptance must explicitly consume the supported list/open output and existing comparator. Preserve the legacy route while 6.1 is accepted. Do not move production cutover into 6.1 under the word “ship.”

Update the active implementation-block metadata, Requirements Inventory, Corrective Production Plan containment, and Epic 6 shared attributes to cite this bounded exception. Broader NOT_READY and release containment continue outside its named scope.

**Rationale:** align delivery with FR-2/FR-5 while retaining migration and production obligations where they are exercised.

### E2. Story 6.1 and its active specification

**Files:** `implementation-artifacts/6-1-list-and-open-projects-through-supported-authenticated-paths.md` and `spec-6-1-list-and-open-projects-through-supported-authenticated-paths.md`.

**OLD:** `status: blocked`, `blocked_by: [6.1-P0, 6.1-P1, 6.1-P1R, 6.1-P2, 6.1-P3, 6.1-P4]`; P4 accepts all pins/approvals before work; AC4 exhaustive equivalence and AC7 the published G-4 runner are completion gates.

**NEW:** replace those dependency/entry/task/Dev Notes/verification instructions with §3; adopt `blocked_by: []` once the focused readiness check confirms the existing supported/security seams and executable tests. Set `ready-for-dev` to finish/review the existing implementation, not `done`. If that check identifies an actual missing capability, retain `blocked` with that precise defect and owner, rather than reinstating P0–P4.

Retain AC1–3/5–6 security, response, disclosure and audit semantics with the current PRD's Folder-before-pagination, quarantine, and inspection rules. Replace AC4 with additive compatibility and retained legacy routing; require exhaustive equivalence in 6.7. Replace AC7's runner requirement with the existing persisted authenticated lane and individual test-project results. Update the story beneficiary to Project User on Chatbot or Tenant-role actor on permitted surfaces, matching the epic.

Append a dated supersession note to the old Auto Run Result. Preserve dated local sub-specs, including completed restart/replay work, and their frozen intent/history; do not restart them or rewrite their claims into production acceptance. `docs/supported-project-reads.md` receives one current acceptance-boundary note pointing here once approved.

**Rationale:** the present “cannot implement” wording contradicts substantial implemented local capability. A corrected spec must be executable without a fresh platform project.

### E3. Route the work packages without inventing completion

**Files:** `epics.md` work-package table and Epic 8 ownership notes; `sprint-status.yaml::action_items`; append routing notes to the existing P0/P1R-remediation/P2/P3 handoffs where their active instructions still claim to block 6.1.

Preserve stable IDs and historical evidence. Change owning epic for the remaining qualification bundles; do not duplicate them under new IDs.

| Existing ID | OLD | NEW owner/placement and disposition |
| --- | --- | --- |
| 6.1-P0 | Epic 6, open; prerequisite for 6.1 | Epic 8, open; Builds/Platform own generic runner, publication and consumer adoption, consumed by 8.7/8.11. Stage 1 remains done; stages 2–7 remain open. AD-25 still gates retirement of the old runtime projects. |
| 6.1-P1 | Epic 6, done | Preserve as historical accepted normalization; remove it from 6.1's active dependency list. |
| 6.1-P1R | Epic 6, done, current usability false | Preserve exact accepted JSON, coordinates, role decisions and false current usability as historical package qualification. No new tuple acceptance or rollover. |
| 6.1-P1R-remediation | Epic 6, open; development prerequisite | Epic 8, open; EventStore/Builds own package compatibility and capable rollback/forward recovery. Consumers: 8.7/8.10/8.11-P3 and Epic 7's applicable writer boundary. Actual defects on the selected read path still block that path. |
| 6.1-P2 | Epic 6, open, blocked by P1R | Epic 8, open for the full platform qualification bundle; EventStore/Identity own it. Existing query identity, denial and trustworthy position capabilities remain mandatory in 6.1's tests, without awaiting acceptance of the entire historical package/runner bundle. |
| 6.1-P3 | Epic 6, open, blocked by P2 | Epic 8, open for production identity configuration/enrollment/qualification; Identity/Platform own it, consumed by 8.8 and 8.11-P1/P2. Fail-closed host behavior and real authenticated fixture proof remain mandatory in 6.1. |
| 6.1-P4 | Epic 6, open aggregate entry approval | Epic 6, `superseded`; replace the development ceremony with ordinary story readiness/acceptance. Do not mark it delivered or accepted. Its release-evidence links are consumed by existing Epic 8 packages. |

Add Action Item status `superseded`: a process requirement replaced by an approved correction, never a delivered-capability pass. For the moved actions, retain current technical statuses and meaningful owner dependencies, but remove `required by Story 6.1`/`unblocks Story 6.1` language and route to the consumers above. Remove P4 and Story 6.1 from active `unblocks` lists; preserve original dispositions as dated history. Approval of this proposal is not approval to publish or edit the owning repositories.

**Rationale:** work remains owned and visible without making the read feature wait for platform delivery. No P0/P2/P3 package is falsely closed.

### E4. Sprint scheduling and DW-1

**Files:** `implementation-artifacts/sprint-status.yaml`, `deferred-work.md`, and `spec-6-5-inspect-projects-through-an-authenticated-frontcomposer-read-surface.md`.

**OLD:** `story_6_1_prerequisite_chain` is P1R → P0/P2 → P3 → sign-off → P4 → independent READY; 6.1 blocked; `# wait for eventstore 6.2`. DW-1 demands an additional committed/hash-pinned `prerequisite_record` and G-4 executable profiles before 6.5 dispatch.

**NEW:** replace the chain with E1 and remove the stale comment. After focused readiness, set 6.1 to `ready-for-dev`; leave 6.5 `blocked` until DW-1's revised condition is met. Preserve Epic 6 `in-progress`, 6.2–6.4 `done`, 6.6–6.8 and every Epic 7/8 story status. `last_updated` remains the actual edit date. Add a short bounded-development-exception provenance note; retain the historical `readiness_provenance.current_result: NOT_READY` rather than manufacturing READY.

For DW-1 and the 6.5 frozen Prerequisite Gate, explicitly renegotiate only the gate:

> Story 6.5 consumes accepted 6.1–6.4 contracts and supported handlers plus the FrontComposer server-authentication/current-Tenant/per-circuit gateway-credential seam it needs. Record the accepting commit and test references in this entry and the story's ordinary prerequisites. Close DW-1 only when those inputs exist and are usable; then run focused 6.5 spec readiness. Common-runner publication, package/rollback qualification, live production enrollment and a second immutable hash inventory are not dispatch requirements. Anonymous UI, token bleed, absent read contracts, substituted legacy REST, or missing owner authority still block dispatch.

Remove `prerequisite_record: null` and its separate schema/hash ceremony from the active spec; replace references in its matrix, AC1 and Auto Run Result with the ordinary prerequisite/evidence references. Keep DW-1 open in this application; Story 6.1 has not been accepted by this proposal. Correct its obsolete sentence that 6.4 is blocked to the current completed status. Preserve the rest of the ledger and concurrent changes.

**Never run `sprint_plan.py` to write or regenerate `sprint-status.yaml`.** Prepare targeted manual edits and validate the candidate artifact set with the updated guard before replacing active files.

**Rationale:** remove the inherited process chain without admitting a Web adapter before its real dependencies.

### E5. Matching production-authority guard and regression tests

**Files:** `tools/planning/validate_production_authority.py` and `tests/tools/test_production_authority_guard.py`. Implement in the same change as E3/E4.

**OLD:** `EPIC6_DEVELOPMENT_STATUSES` fixes 6.1 and 6.5 to `blocked`; `ACTION_STATUSES` fixes every 6.1-P* action to Epic 6 and P4 to `open`; tests require 6.1/6.5 status advancement to fail.

**NEW, precise scheduling policy:**

1. Remove only the 6.1 and 6.5 hard pins from `EPIC6_DEVELOPMENT_STATUSES`. Add a named `STORY_6_5_KEY`. Validate each exact key against `{blocked, ready-for-dev, in-progress, review, done}`, then include its validated value in the exact production inventory comparison. Missing, renamed, duplicate, `backlog`, or invented status fails. Preserve the existing separate 6.4 exception and its status vocabulary.
2. Add a small dependency check in the existing parsed-snapshot validation path: when DW-1 is `done`, or 6.5 advances beyond `blocked`, 6.1/6.2/6.3/6.4 must all be `done`. Advancing 6.5 additionally requires DW-1 `done`. Read DW-1 with the existing deferred-status parser. A plain authority check remains an authorization/scheduling check, not feature acceptance or deployment proof.
3. Update exactly these `ACTION_STATUSES` entries, retaining ID order: `6.1-P0`, `6.1-P1R-remediation`, `6.1-P2`, `6.1-P3` become `(8, "open")`; `6.1-P4` becomes `(6, "superseded")`. Historical P1/P1R and all other action boundaries remain as configured. Existing action-inventory and closure validation still apply.
4. Preserve authority `[6, 7, 8]`, the immutable Epic 1–5 inventory/digest, exact non-excepted Epic 6–8 statuses, historical NOT_READY, selected/rollback tuple validation, strict four-role acceptance schema, P1R/P0-stage-1/DW-35/DW-68 atomicity, current-usability false checks, and P0 stages 2–7/capability boundary. Moving a gate must not turn it green.

**Required test changes, not a blanket expectation flip:**

| Case | Expected result |
| --- | --- |
| 6.1 progresses through each approved status while P0/P2/P3 remain open, P4 superseded, P1R current usability false, historical NOT_READY | Pass; no package/release acceptance inferred. |
| 6.1 key missing/renamed/duplicated or given `released`, `accepted`, `ready`, `backlog`, null | Fail closed. |
| 6.5 advanced with DW-1 open, or any 6.1–6.4 dependency not done | Fail closed, naming the unmet dependency. |
| DW-1 closed with 6.1 incomplete | Fail closed. |
| Accepted 6.1–6.4 plus DW-1 done; 6.5 ordinary progression while qualification actions remain open | Pass. |
| 6.5 missing/renamed/invalid status; other stories or Epic 7/8 status advance | Fail closed. |
| Reparented action has wrong epic/status; P4 falsely marked done; P0 stages/capabilities or false usability advances | Fail closed. |
| Existing malformed JSON, tuple/role mismatch, historical-status tampering and unauthorized P1R closure cases | Continue failing exactly as before. |

Update `test_unauthorized_closure_fails_closed` to mutate a still-pinned story such as 6.6, and replace the old 6.5 unconditional rejection cases with dependency-aware cases. Keep the existing advisory-only test, and add combined 6.1/6.5 scenarios through full `validate_index`, including open and accepted historical P1R fixtures. Assert the active repository index passes after targeted edits.

**Rationale:** allow precisely the approved development scope while retaining the protection against false qualification, lost history, and unintended Epic 7/8 scheduling.

### E6. PRD, Architecture, and project-context consistency

**OLD:** PRD §2.3/§2.5 and addendum containment, Architecture AD-25/AD-30, and project context treat the complete P0–P4/READY chain as a universal prerequisite to feature development.

**NEW:** add one dated bounded exception referencing this proposal and replace active contradictory instructions: Story 6.1 and, after its real prerequisites, Story 6.5 use focused spec readiness and recorded feature evidence. Historical NOT_READY is retained for broader planning/release qualification. AD-25/G-4 still gates old-runtime removal and platform-runner rollout; AD-30 still rejects false passes and owns full terminal release evidence. Stack/P1R notes describe their dated package scope, not a current feature scheduling veto. Amend active P2/P3/production-identity runbook notes to distinguish local capability proof from release enrollment without changing the security configuration contract.

Update the PRD addendum's supersession trace with this proposal upon approval. No FR/NFR body, numbering, scope, UX response meaning, or Epic 7/8 execution permission changes. No architect signature or production trust approval is inferred.

**Rationale:** editing only Epic 6 would leave downstream agents obeying contradictory global freezes.

### E7. Traceability and test-plan alignment

**Files:** `implementation-readiness-traceability-matrix.yaml` and its Markdown view; `test-artifacts/test-design-epic-6.md`.

**OLD:** `fr-2` and `fr-5` inherit a mandatory G-4 fixture and list all P0–P4 as entry gates; their blocker says implementation cannot start. Global containment repeats the full chain. Draft test risks make G-4 a pre-6.1 requirement.

**NEW:** override the two rows' entry gates/environment/command with the supported read and authority capabilities plus the existing Development lane. Change those rows to `pending` when readiness passes, never `passed` from proposal approval. Record subsequent actual feature results separately from release qualification. Update global containment with the bounded exception and link the moved qualification requirements to their existing 6.7/Epic 7/8 consumers. Review supporting NFR-1/5/10 rows so the old chain does not remain an indirect 6.1 blocker; preserve their broader release dependencies and nonpassing status.

Add a short dated scope note to the draft test design: reuse existing list/open and distributed matrices; common-runner delivery and full platform certification are separate. Preserve its historical risk/results. No generated evidence or sealed packet changes.

**Rationale:** honest traceability needs both a feature development path and the full, still-unaccepted production path.

## 5. Implementation handoff and validation

**Scope: moderate.** Product Owner/Projects maintainer aligns the backlog and active instructions; Developer implements the small guard/test change and targeted manual tracking edits; Test Architect verifies coverage and actual feature receipts. Builds, EventStore, Platform/Identity and Release Engineering retain their routed qualification work. This is a responsibility assignment, not an external notification or new repository authorization.

### Application order after approval

1. Prepare E1–E7 together, preserving concurrent edits. Renegotiate the named 6.5 frozen gate explicitly under this approval; retain historical frozen local proof specs.
2. Update guard and tests before testing candidate statuses. Run the focused Python suite against the proposed policy, including invalid candidate controls.
3. In a temporary candidate directory, prepare manually edited sprint YAML, the latest deferred ledger, unchanged guarded P0 fields, and the unchanged fixed P1R acceptance record at its relative path. Validate that complete candidate set; never mix candidate/default companion files.
4. Apply the validated targeted set together, then rerun active-index and story authority checks. Do not use `sprint_plan.py` in this operation.
5. Finish/review 6.1 under its revised acceptance. Reuse valid recorded receipts or run missing evidence. Only actual accepted feature evidence moves 6.1 to done; then assess DW-1 and 6.5 readiness. No status shortcut follows from the exception.

### Narrow checks

```bash
python3 -m unittest discover -s tests/tools -p test_production_authority_guard.py
python3 tools/planning/validate_production_authority.py --validate-index
python3 tools/planning/validate_production_authority.py --story-id 6.1
python3 tools/planning/validate_production_authority.py --story-id 6.5
git diff --check
```

For the candidate check use the existing `--sprint-status`, `--deferred-work`, `--p0-artifact`, and `--workspace-root` options together, naming all paths in that candidate directory. There is no `--acceptance-record` argument. Preserve and supply the fixed record under the candidate workspace.

If fresh feature evidence is needed, use the documented lane, which builds Debug source references, executes individual test assemblies and retains actual results:

```bash
MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 \
  python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900
```

Do not supply a reused evidence directory. Inspect actual XML, matrix and cleanup outcomes. These commands are a future feature-verification handoff; no live qualification or broad .NET test was run to write this proposal. CI retains Release/package references and its current G-6/release controls.

### Success criteria

- No active instruction makes 6.1 depend on P0 publication/adoption, historical P1R usability, production enrollment, P4, global READY, or unrelated platform qualification.
- All mandatory SDK, actor/Folder authorization, disclosure/audit, freshness, cursor and persisted no-domain-write behavior remains testable and required.
- 6.1 can progress under the updated guard; 6.5 stays blocked until its actual dependencies and DW-1 condition are met. Other stories and Epic 7/8 statuses stay pinned.
- Routed qualification actions remain open under their owning epic/repository; P4 is superseded, not accepted; historical P1/P1R decisions and guarded fields remain intact.
- Candidate and active guard validation plus focused positive/negative tests pass. Sprint YAML was edited manually, never generated by `sprint_plan.py`.
- Feature completion records its tested commit and evidence once; later unrelated movement does not reopen it. Release, cutover, and published package claims remain separately earned.

## 6. Workflow checklist and current disposition

| Checklist items | Status and evidence |
| --- | --- |
| 1.1–1.3 Trigger, core problem, evidence | [x] 6.1 entry chain and DW-1 compared with the approved 6.4 precedent, owner tracker and implementation evidence. |
| 2.1–2.5 Epic impact, future epics, order | [x] Bounded 6.1/6.5 exception; 6.7 migration and Epic 7/8 obligations retained; no new epic or renumbering. |
| 3.1–3.4 PRD, Architecture, UX, other artifacts | [x] E1–E7 identify conflicting operational instructions. [N/A] UX redesign; product semantics unchanged. |
| 4.1–4.4 Options and recommendation | [x] Direct adjustment selected; rollback and MVP cuts rejected with reasons. |
| 5.1–5.5 Proposal components and ownership | [x] Issue, impact, before/after edits, effort/risk and handoff documented. |
| 6.1–6.2 Checklist and proposal consistency | [x] Qualification is moved without granting acceptance; actual security and persisted proof retained. |
| 6.3 Approval | [!] Pending Jerome's explicit approval of E1–E7. |
| 6.4 Tracking updates | [!] Proposed only; apply manually with matching guard/tests after approval. |
| 6.5 Handoff | [x] Responsibilities and success criteria documented; implementation dispatch awaits approval. |

**Validation performed for this proposal:** current `python3 tools/planning/validate_production_authority.py --validate-index` passed; `python3 -m unittest discover -s tests/tools -p test_production_authority_guard.py` passed all 37 tests. This confirms the existing boundary, not the proposed one.

**Workflow execution log:** customization resolved successfully with empty prepend/append/facts/on-complete; repository baseline and required PRD/Epics plus Architecture/UX/context inspected; trigger supplied by Jerome; batch used pending any preference correction. Only this proposal is written. E1–E7, runtime verification and status transitions remain unapplied pending approval.
