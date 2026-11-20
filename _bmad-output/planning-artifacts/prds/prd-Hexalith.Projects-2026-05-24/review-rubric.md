# PRD Quality Review — Hexalith.Projects

## Overall verdict

**Fair.** The PRD gives teams a clear release decision and measurable goals for resuming work across Conversations. Before accepting the affected writers, reconcile who can restore a Project with a missing Folder, what a successful confirmed creation must link, and how Folder creation stays safe under concurrency. The missing authenticated journey at the audited revision is a separate release evidence gap recorded in §2.5.

## Decision-readiness — adequate

The read cutover, blocked first writer, and owners of the final release decision are identifiable in §2.5. The PRD also explains why it limits a Conversation to one Project and uses the Folder as its authorization boundary. One restore case gives different roles to the same operation.

### Findings

- **High — Missing-Folder restore has two authority rules.** FR-23 calls this a “Tenant Project Administrator operation” (prd.md:510). Its acceptance criterion for a Folder that is “invalid or missing” offers a replacement chosen “for a Project User, [with] Selection Evidence” (prd.md:515). A Folder manager and a Tenant Administrator would get different restore permissions depending on which passage a team implements. *Fix:* Split the prior-Folder cases and make the missing-Folder case Administrator-only in both the requirement and its acceptance criteria, as the approved correction specifies. [September 8 decision, line 294](../../sprint-change-proposal-2026-09-08.md)

## Substance over theater — strong

The Project User, Tenant Operator, Tenant Project Administrator, and delegated caller have distinct operations and authority. The export cap, redacted contents, concurrency limit, and recovery budget each give a test team an observable threshold (prd.md:523–535, 543–550).

## Strategic coherence — adequate

SM-7 and SM-8 measure whether people find the right Project and resume an existing Conversation without repairing its context (prd.md:563–572). The automatic-association counter-metric currently classifies some valid confirmations as failures.

### Findings

- **Medium — SM-C1 counts the wrong confirmation for some associations.** SM-C1 requires zero “Inferred Associations bound without FR-14 confirmation” (prd.md:587). FR-14 concerns resolution candidates. FR-1 explicitly allows an inferred Folder binding confirmed “under FR-8/FR-15” (prd.md:213). A correctly confirmed Folder binding can therefore fail the metric. *Fix:* Measure absence of the confirmation required by the governing action, or explicitly limit the denominator to FR-14 candidate bindings. Include an approved FR-8 Folder binding in the measurement fixture.

## Done-ness clarity — thin

The read contracts distinguish native Conversation membership, advisory evidence, and durable audit before descriptive disclosure. Two acceptance cases leave materially different outcomes possible for the same input.

### Findings

- **High — Proposal completion lacks an owner-confirmed Conversation link.** FR-15 binds the “initiating Conversation” and attachments to the Confirmation Artifact, then defines confirmed creation through Folder binding and read-model confirmation (prd.md:405–412). Those criteria could pass with a new Project whose initiating Conversation remains unbound. FR-16 would then return no usable context (prd.md:424–425), even though Story 7.12 explicitly requires linking that Conversation and authorized attachments through the owner. *Fix:* State the required owner link and attachment outcomes before the proposal task reports success. Include a retry after Folder activation but before the Conversation link completes. [Story 7.12, line 2018](../../epics.md)
- **Medium — Policy-excluded references meet two response-state definitions.** When all required components are current and the only optional reference is excluded by Setup policy, §5 allows both “Complete” with “ExcludedBySetupPolicy” and “Partial” when “one or more optional references are excluded” (prd.md:185–186). Clients can present different warnings and report different Partial counts for the same context. *Fix:* State the precedence for policy exclusions and distinguish them from unavailable optional references. Include one example of each outcome.

## Scope honesty — adequate

Section 2.4 explicitly excludes standalone use, cross-Tenant sharing, and full-content retrieval. The seven assumptions name owners and revisit conditions. One fallback is too weak to preserve a binding product guarantee.

### Findings

- **High — A preliminary Folder check cannot guarantee create-only behavior.** A-2 says Projects must “pre-check” if Folders cannot guarantee create-only semantics (prd.md:597). Two requests can both observe an unused name before either creation commits. FR-1 requires create-only same-name creation with a collision failure and forbids binding an existing Folder (prd.md:213). *Fix:* Require an atomic create-only operation or equivalent owner-enforced name reservation. Keep the affected creation path unavailable until that capability is verified, and exercise concurrent same-name requests.

## Downstream usability — adequate

The glossary, stable identifiers, and indexed source decisions make individual requirements easy to find. Two sections need the exceptions that are currently explicit only in another requirement.

### Findings

- **Medium — Relinking after an unlink has conflicting authorization criteria.** FR-7 says “after a confirmed unlink, target and Conversation only,” then requires failure when “either Project” cannot be authorized (prd.md:294, 297). FR-11 expressly says the prior Project’s authorization is unnecessary (prd.md:347). A caller who lost access to the former Project could therefore be denied a valid move. *Fix:* Apply the prior-Project check only while membership is still bound. Add a relink case where the prior Project is hidden and the caller is authorized on both the Conversation and the target.
- **Medium — The sole-candidate acceptance criterion omits the read-only case.** FR-12 specifies “no accept action” for a read-only actor (prd.md:373), while FR-14 requires a sole candidate to have “explicit accept and decline actions” (prd.md:395). The Chatbot companion repeats the unconditional instruction (addendum.md:144). A UI built from the companion alone would offer an action that FR-14 admission refuses. *Fix:* Qualify the sole-candidate UI rule in both locations with the current manage-authority condition. Cover an authorized read-only candidate in the accessible journey tests.

## Shape fit — strong

This is a product contract for an internal platform with a user-facing Chatbot experience and separate operator workflows. Named journeys explain the user consequences. The addendum carries package boundaries, rollback mechanics, test evidence, and dated decisions without making a historical qualification run stand in for full release (addendum.md:13–21, 124–138, 175–194).

## Mechanical notes

- Verified unique, contiguous definitions for 25 FRs, 11 NFRs, five UJs, eight SMs, four counter-metrics, and seven assumptions. All five journeys have a named protagonist.
- All 23 relative Markdown file links in the PRD and addendum resolve. Section 9 lists an owner and revisit condition for each assumption.
- The review covers the complete November 20 working-tree PRD and addendum. It evaluates requirement quality. Candidate-level execution evidence and the terminal release decision remain with their named owners.
