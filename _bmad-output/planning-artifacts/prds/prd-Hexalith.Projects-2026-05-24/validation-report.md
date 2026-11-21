# Validation report — Hexalith.Projects

**Grade: Fair.** 6 high · 5 medium · 0 critical · 0 low. Eleven distinct findings across three reviews.

Reviewed 2026-11-20T15:57:38+01:00. [PRD](./prd.md) · [Addendum](./addendum.md)

## Overall verdict

The PRD gives teams a clear release decision and measurable goals for resuming work across Conversations. Before accepting the affected writers, reconcile who can restore a Project with a missing Folder, what a successful confirmed creation must link, and how Folder creation stays safe under concurrency. The missing authenticated journey at the audited revision is a separate release evidence gap recorded in §2.5.

Three high findings concern the product requirements. Three more concern downstream instructions for Conversation membership, McpCli confirmation, and release evidence tied to the candidate revision. The authority review independently corroborated four rubric findings. The totals count each issue once.

Priority for the next edit is to resolve the three writer requirements before accepting the affected stories, then align the active epics and evidence matrix with the November 15, 16 and 20 decisions. The release owners still require a passing authenticated journey on the exact candidate.

## Dimension verdicts

| Dimension | Verdict | Assessment |
| --- | --- | --- |
| Decision-readiness | Adequate | The read cutover, blocked first writer, and owners of the final release decision are identifiable in §2.5. The PRD also explains why it limits a Conversation to one Project and uses the Folder as its authorization boundary. One restore case gives different roles to the same operation. |
| Substance over theater | Strong | The Project User, Tenant Operator, Tenant Project Administrator, and delegated caller have distinct operations and authority. The export cap, redacted contents, concurrency limit, and recovery budget each give a test team an observable threshold (prd.md:523–535, 543–550). |
| Strategic coherence | Adequate | SM-7 and SM-8 measure whether people find the right Project and resume an existing Conversation without repairing its context (prd.md:563–572). The automatic-association counter-metric currently classifies some valid confirmations as failures. |
| Done-ness clarity | Thin | The read contracts distinguish native Conversation membership, advisory evidence, and durable audit before descriptive disclosure. Two acceptance cases leave materially different outcomes possible for the same input. |
| Scope honesty | Adequate | Section 2.4 explicitly excludes standalone use, cross-Tenant sharing, and full-content retrieval. The seven assumptions name owners and revisit conditions. One fallback is too weak to preserve a binding product guarantee. |
| Downstream usability | Adequate | The glossary, stable identifiers, and indexed source decisions make individual requirements easy to find. Two sections need the exceptions that are currently explicit only in another requirement. |
| Shape fit | Strong | This is a product contract for an internal platform with a user-facing Chatbot experience and separate operator workflows. Named journeys explain the user consequences. The addendum carries package boundaries, rollback mechanics, test evidence, and dated decisions without making a historical qualification run stand in for full release (addendum.md:13–21, 124–138, 175–194). |

## Findings by severity

### Critical (0)

None.

### High (6)

**V01 · Missing-Folder restore has two authority rules**

_Product requirements · Decision-readiness._ FR-23 calls this a “Tenant Project Administrator operation” (prd.md:510). Its acceptance criterion for a Folder that is “invalid or missing” offers a replacement chosen “for a Project User, [with] Selection Evidence” (prd.md:515). A Folder manager and a Tenant Administrator would get different restore permissions depending on which passage a team implements.

**Proposed correction.** Split the prior-Folder cases and make the missing-Folder case Administrator-only in both the requirement and its acceptance criteria, as the approved correction specifies. [September 8 decision, line 294](../../sprint-change-proposal-2026-09-08.md)

**V03 · Proposal completion lacks an owner-confirmed Conversation link**

_Product requirements · Done-ness clarity._ FR-15 binds the “initiating Conversation” and attachments to the Confirmation Artifact, then defines confirmed creation through Folder binding and read-model confirmation (prd.md:405–412). Those criteria could pass with a new Project whose initiating Conversation remains unbound. FR-16 would then return no usable context (prd.md:424–425), even though Story 7.12 explicitly requires linking that Conversation and authorized attachments through the owner.

**Proposed correction.** State the required owner link and attachment outcomes before the proposal task reports success. Include a retry after Folder activation but before the Conversation link completes. [Story 7.12, line 2018](../../epics.md)

**V05 · A preliminary Folder check cannot guarantee create-only behavior**

_Product requirements · Scope honesty._ A-2 says Projects must “pre-check” if Folders cannot guarantee create-only semantics (prd.md:597). Two requests can both observe an unused name before either creation commits. FR-1 requires create-only same-name creation with a collision failure and forbids binding an existing Folder (prd.md:213).

**Proposed correction.** Require an atomic create-only operation or equivalent owner-enforced name reservation. Keep the affected creation path unavailable until that capability is verified, and exercise concurrent same-name requests.

**V08 · An opened Project or a projected link can be mistaken for Conversation membership**

_Downstream planning._ The current FR-16 summary permits a session-open fallback, FR-20 inherits it, and the Chatbot companion acceptance text asks for a `ConversationLinked` “exemption” (epics.md lines 115, 122 and 2756). The canonical FR-16, FR-17 and FR-20 rows point only to earlier stories (implementation-readiness-traceability-matrix.yaml lines 311–335 and 364–375). A consumer following these entries could admit a first response with another Project’s context. PRD lines 372, 424–425, 436 and 476 and the approved November 16 read amendments lines 30–42 require a current, authorized native membership.

**Proposed correction.** Correct the active summary and companion acceptance case, then link these rows to Story 6.7’s native-membership evidence. Exercise an unbound Conversation after an explicit open, and a stale advisory link, as separate negative cases.

**V09 · Terminal release paperwork still offers machine confirmation**

_Downstream planning._ The canonical containment rule and terminal disposition make human MCP confirmation, Selection Evidence or even autonomous mutation conditional on a passed release gate (implementation-readiness-traceability-matrix.yaml lines 61 and 1038). Story 8.11 repeats the conditional grant (epics.md line 2855). PRD lines 36 and 91 and the November 15 scheduling decision lines 220 and 273 bar these actions on McpCli. A release owner could record a wider authorization than the product allows.

**Proposed correction.** Make the terminal disposition authorize the production release only and preserve Web/Chatbot-only confirmation. Extend the existing DW-84 wording reconciliation to the terminal matrix and dependent writer/selection criteria in epics.md lines 2032, 2046 and 2138.

**V10 · The canonical smoke row can be satisfied with evidence from the wrong revision**

_Downstream planning._ Its acceptance language requires a passing authenticated run and a test result file, without the candidate SHA, release profile, run/job identity, persisted state and recovery required by PRD line 98, epics.md lines 2732 and 2838, and sprint-status.yaml line 643 (implementation-readiness-traceability-matrix.yaml lines 994–1008). A successful earlier run could therefore appear sufficient to someone evaluating the matrix alone.

**Proposed correction.** Bind the full-release row to the candidate-specific Story 8.7/8.11-P1 evidence and its negative cases. Keep the historical 19/56 result and the separate Contracts profiles distinct.

### Medium (5)

**V02 · SM-C1 counts the wrong confirmation for some associations**

_Product requirements · Strategic coherence._ SM-C1 requires zero “Inferred Associations bound without FR-14 confirmation” (prd.md:587). FR-14 concerns resolution candidates. FR-1 explicitly allows an inferred Folder binding confirmed “under FR-8/FR-15” (prd.md:213). A correctly confirmed Folder binding can therefore fail the metric.

**Proposed correction.** Measure absence of the confirmation required by the governing action, or explicitly limit the denominator to FR-14 candidate bindings. Include an approved FR-8 Folder binding in the measurement fixture.

**V04 · Policy-excluded references meet two response-state definitions**

_Product requirements · Done-ness clarity._ When all required components are current and the only optional reference is excluded by Setup policy, §5 allows both “Complete” with “ExcludedBySetupPolicy” and “Partial” when “one or more optional references are excluded” (prd.md:185–186). Clients can present different warnings and report different Partial counts for the same context.

**Proposed correction.** State the precedence for policy exclusions and distinguish them from unavailable optional references. Include one example of each outcome.

**V06 · Relinking after an unlink has conflicting authorization criteria**

_Product requirements · Downstream usability._ FR-7 says “after a confirmed unlink, target and Conversation only,” then requires failure when “either Project” cannot be authorized (prd.md:294, 297). FR-11 expressly says the prior Project’s authorization is unnecessary (prd.md:347). A caller who lost access to the former Project could therefore be denied a valid move.

**Proposed correction.** Apply the prior-Project check only while membership is still bound. Add a relink case where the prior Project is hidden and the caller is authorized on both the Conversation and the target.

**V07 · The sole-candidate acceptance criterion omits the read-only case**

_Product requirements · Downstream usability._ FR-12 specifies “no accept action” for a read-only actor (prd.md:373), while FR-14 requires a sole candidate to have “explicit accept and decline actions” (prd.md:395). The Chatbot companion repeats the unconditional instruction (addendum.md:144). A UI built from the companion alone would offer an action that FR-14 admission refuses.

**Proposed correction.** Qualify the sole-candidate UI rule in both locations with the current manage-authority condition. Cover an authorized read-only candidate in the accessible journey tests.

**V11 · Retained list/open name disclosure lacks a trace to its specific controls**

_Downstream planning._ The FR-5 epic summary says Project Users receive names, and the canonical FR-2/5 descriptions refer to an inspection gate without identifying the retained route’s explicit opt-in and successful audit prerequisites (epics.md line 94 and implementation-readiness-traceability-matrix.yaml lines 105 and 153). The November 16 amendments lines 43–51 and PRD line 500 require both controls for successful retained v1 responses, including those issued by callers who formerly received names with ordinary read permission. Readers could apply the supported route’s rules to retained traffic.

**Proposed correction.** Label the supported and retained contracts in the summary and attach Story 6.7’s D5 evidence to the FR-2/5/22 rows, including missing opt-in, denied permission and audit-write failure.

### Low (0)

None.

## Mechanical checks and scope

- Verified unique, contiguous definitions for 25 FRs, 11 NFRs, five UJs, eight SMs, four counter-metrics, and seven assumptions. All five journeys have a named protagonist.
- All 23 relative Markdown file links in the PRD and addendum resolve. Section 9 lists an owner and revisit condition for each assumption.
- The review covers the complete November 20 working-tree PRD and addendum. It evaluates requirement quality. Candidate-level execution evidence and the terminal release decision remain with their named owners.
- The traceability matrix contains one row for each of the 25 FRs and 11 NFRs. The production-authority index check passed for Epics 6, 7, and 8. These checks establish coverage by ID and the permitted planning scope.
- Validation used the working tree at repository commit `0f40053e`. The PRD and addendum include local changes. The prior dated review evaluated the November update only.

## Reviewer files

- [Full PRD quality rubric](./review-rubric.md)
- [Authority, isolation, and recovery](./review-adversarial-isolation.md)
- [Downstream planning and traceability](./review-planning-drift.md)

The authority review corroborates V05, V01, V06, V07.

Rubric: `.agents/skills/bmad-prd/assets/prd-validation-checklist.md`.

- `prd.md` SHA-256: `57400fe8889deb38b47b6e9fb080a0dee2fcbb544772bbe6dcb1d5fe69ae8e34`
- `addendum.md` SHA-256: `5a4aff9731cff36fab819b66ea6e8c13e3a155e2f01f2cf6ebbed293bf9c7979`
