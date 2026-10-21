---
title: 'Simplify Story 6.4 to authorized advisory Project suggestions'
date: '2026-10-21'
status: 'approved'
implementation_approved: true
approval_source: 'User replied yes to replacing strict-current resolution with the advisory model'
approved_date: '2026-10-21'
change_scope: 'moderate product and architecture adjustment'
trigger: 'User reports dozens of iterations without completion and requests a pragmatic simpler solution'
---

# Proposal: finish Project suggestions with a smaller promise

## Decision

Deliver authorized, read-only Project suggestions from ordinary committed metadata. Accept that matching metadata can lag. Check access at each owner during the request, and revalidate the chosen association through the existing confirmation workflow before any change. Suggestions cannot authorize context use or automatic attachment.

This is a change to the product's consistency guarantee. It is not approval to omit authorization, use a guessed File identity, relabel stale evidence as Current, or claim the existing strict Story 6.4 is complete.

## Why the present approach keeps expanding

The [scoring engine](../../src/Hexalith.Projects/Resolution/ProjectResolutionEngine.cs), evidence mappers and both resolution endpoints already exist. The outstanding prerequisite work proves native source identity, complete discovery, projection generations, rebuild completion, all-writer enrollment, current authority and environment qualification across several services. The latest [completion-proof slice](../implementation-artifacts/evidence/6-4-content-free-rebuild-completion-20261021/README.md) passed 1603 tests while the product story remained blocked.

The adopted AD-32 contract also subjects resolution to context-usability requirements for Project, Folder, Setup and first-response evidence. A suggestion does not need to make that stronger promise. Continuing to optimize the same proof protocol preserves the source of the delay.

The existing endpoints are useful starting code, not ready-to-ship security evidence: the attachment endpoint marks presented inputs Included without current owner authorization, and the Conversation endpoint's tenant filter does not by itself establish per-Project access. Those gaps must be fixed.

## Smallest useful implementation

1. Keep the existing engine, weights, deterministic ordering, archive option and reason codes. Finish the existing query composition rather than requiring a host/runtime migration as part of this feature.
2. Obtain metadata through authenticated owner reads. Check Tenant, original actor, workload/delegation and exact resource access before using protected metadata. Filter candidates before passing them to the engine. Return only disclosable IDs, scores and reason codes; names remain omitted.
3. Use ordinary Projects list/reference views to discover candidates. They supply matching hints, never authorization. Reuse the isolated Conversation metadata work where useful. A missing safe owner read may need one narrow metadata-only endpoint; do not extend a generic provenance or rebuild protocol for this feature. Preserve the meaning of existing strict owner contracts; any advisory query is explicit.
4. Retain the selected bounds: at most 32 raw attachments, initially four concurrent dependency calls, cancellation and a two-second dependency deadline. Bound candidate enumeration too. If a limit or dependency failure prevents evaluation, return an explicitly incomplete result, not an exhaustive NoMatch. The first implementation can scan the existing bounded view; add an index only when measurements justify it.
5. Return advisory suggestions with an evaluation time and an explicit incomplete indicator. Evaluation time is not a source-head watermark. NoMatch means no match in successfully evaluated observed metadata. SingleCandidate means one visible suggestion, never exclusive truth or a selection. Preserve indistinguishable denial/nonexistence behavior and omit protected exclusion details.
6. Keep confirmation and context assembly authoritative. Before association, existing confirmation rechecks permissions, lifecycle and relationship state. An advisory result is never accepted as an AD-32 Complete/Partial context certificate or first-response authorization.

Folder/File identity must be real and owner-backed. An unmapped File cannot be guessed from a path, Folder ID or Projects reference ID. If that mapping is still absent, expose the unsupported/incomplete capability honestly and deliver Folder/Conversation support as a named partial scope; do not mark full attachment support complete. Likewise, working authenticated owner authorization remains a real dependency. Eventual consistency does not solve either gap.

## Exact scope changes proposed

| Artifact / section | Existing requirement | Proposed amendment |
| --- | --- | --- |
| PRD FR-12 / FR-13 | Candidates and matching metadata must be proven current; stale resources cannot qualify. | Matching uses the observed committed read models and may lag. Current request authorization remains mandatory. Known denied/deleted inputs never score. Explain advisory coverage and require confirmation-time validation. |
| PRD §5 / FR-17; Architecture AD-7 / AD-32 | Resolution shares the context-usability snapshot and current-only trace contract. | Give advisory resolution an explicit response contract. Explain the observations used in this request, without persisted inference history. Preserve the stronger context/first-response contract for 6.2/6.3 and consumers that need it. |
| Architecture AD-14; selected D3 | A complete current RTI and native owner/generation evidence precede resolution. | Existing committed views plus request-time owner checks suffice for advisory suggestions. Native-head equality, complete-global-empty proof and rebuild certification are not suggestion acceptance gates. |
| Architecture AD-1 / AD-17; Story 6.4 runtime tasks | Supported-host migration and strict shadow tuple precede the new handlers. | Allow a narrow exception to finish the existing read endpoints with the same authenticated platform identity and safe owner contracts. Keep runtime migration and cutover as independent platform work; no new custom runtime or journal. |
| D4 / D5 / D6 and the 6.4 prerequisite spec | Complete result across all current sources, native scoring-label freshness and native enrollment/qualification. | Bounded advisory coverage with explicit omissions. Retain real identity mapping, owner-controlled metadata purpose, safe labels, input limits and authenticated transport. Native proofs become optional implementation details, not new delivery prerequisites. |
| Epic 6 / Story 6.4 / parent build spec | Current-only resolution with prerequisite-first stop gates. | Rename/rewrite as authorized advisory suggestions with the finite acceptance below. Retire obsolete 6.4-specific proof tasks after approval; preserve their history. |
| 6.5 / 6.6 / 8.5 presentation; 7.11 / 7.12 confirmation; UX resolution journey | A shared current resolution snapshot feeds downstream presentation. | Present suggestions and incomplete coverage clearly. Keep candidates unselected. Confirmation must independently validate; API transport and rendering cannot reinterpret advisory output as trusted context. |
| G-4 / traceability / test design | The missing common runner blocks source feature acceptance. | Accept the actual existing test-project runner plus a real authenticated integration case for this bounded feature. Retain platform-runner and production deployment acceptance in their owning work; do not fabricate G-4 completion. |

The local `.config/dotnet-tools.json` and `module/hexalith-projects.module.json` are currently absent. Updating the verification contract is therefore part of the proposal, not pretending the existing command passes. Runtime deployment, security configuration and release acceptance still require real evidence.

## Finite completion criteria

- Conversation and supported Folder/File inputs produce deterministic suggestions through the actual query/owner/store chain.
- Cross-Tenant, missing actor/workload authority, denied resources and revoked access disclose no protected candidates; unknown and denied inputs remain indistinguishable as specified.
- Lag, concurrent changes, dependency timeout, missing mapping and bounded enumeration produce the documented advisory/incomplete behavior. No fabricated global freshness or exhaustive empty result.
- No automatic selection, Project event, query repair, durable trace or sibling mutation; metadata-only responses and logs.
- Focused regression tests and one real authenticated composition test pass. Existing confirmation/context boundaries demonstrably do not consume the advisory result as authorization.

Implementation has three bounded tasks: amend the contract/scope; finish the two query compositions and only missing owner metadata seams; run the above acceptance checks. A new generic platform capability is a scope decision, not permission to launch another prerequisite chain. Start with one real authorized request through the whole chain so unavailable transport/configuration is discovered immediately.

## Options, effort and handling existing work

Recommended: a moderate scope adjustment to consistency and completeness guarantees. It has a smaller, more predictable implementation surface; exact duration still depends on usable owner authorization and File mapping. Retaining all current guarantees leaves the open cross-repository critical path. Reverting the SDK work would add shared-code risk without fixing the requirements.

Keep completed source fixes and tests. Stop extending the strict prerequisite chain for advisory resolution. Do not make all deferred proof machinery mandatory in a new story by default; resume it only for a separately justified product need. Approval authorizes the listed requirement, sprint and code changes. It does not authorize rollback, dependency changes, Git mutations or deployment.

## Approval and handoff

The user decides whether advisory matching with request-time access checks is the intended product promise. After approval, update the authoritative PRD/spine/story/spec/traceability consistently, then the developer implements the bounded tasks above. Keep production release acceptance separate and factual. Existing D1 security and D2 real identity requirements remain; D3-D6 change only as explicitly listed.

Analysis/checklist: trigger and supporting evidence identified; Epic 6 and downstream consumer impacts assessed; PRD/architecture/UX/testing conflicts enumerated; direct adjustment, rollback and MVP reduction compared; implementation boundaries and acceptance specified. User approval is recorded. Authoritative artifact reconciliation and bounded implementation are authorized. No source tests were rerun for this planning-only proposal.
