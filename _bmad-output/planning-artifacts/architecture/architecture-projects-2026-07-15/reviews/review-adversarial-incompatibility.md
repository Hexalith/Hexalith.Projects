---
review: adversarial-incompatibility
artifact: ../ARCHITECTURE-SPINE.md
date: 2026-07-16
verdict: changes-required
critical: 1
high: 5
medium: 3
---

# Adversarial Incompatibility Review

## Verdict

**CHANGES REQUIRED.** The ownership model and technical-layer extraction are broadly coherent, and the spine correctly labels absent platform capabilities and runner commands as entry-gated targets rather than current facts. However, one admission rule is internally contradictory under the required lost-response scenario, and five high-risk gaps allow incompatible implementations of task lifecycle, retention, rollback, audit capacity, or Folder-first creation.

## Evidence attacked

- `ARCHITECTURE-SPINE.md`, especially AD-3 through AD-5, AD-8 through AD-13, AD-16 through AD-19, AD-22 through AD-30, the target package graph, runner contract, and G-1 through G-6.
- PRD recovery and retention rules in `prd.md` lines 174, 192, 345, 484, and 490, plus `addendum.md` lines 50–56.
- Current brownfield creation behavior in `src/Hexalith.Projects/Aggregates/Project/ProjectAggregate.cs` lines 60–95, which still commits `ProjectCreated(Active)` followed by `ProjectFolderCreationPending` and therefore makes the migration/rollback conditions material rather than theoretical.
- Current project references, which confirm that the repository still owns AppHost, Aspire, Infrastructure, Workers, ServiceDefaults, UI, MCP, CLI, and sibling-client plumbing; AD-24/AD-25 must therefore remain removal gates, not merely target descriptions.

## Critical finding

### C-1 — Exact lost-response retry is both recoverable and forbidden as replay

**Conflict:** AD-5 requires an equivalent reuse of `(Tenant, actor, operation, key)` to return the original task. AD-13 says confirmation validation, single-use consumption, and task admission are atomic and fail closed on every replay. AD-19 maps consumed confirmation to `409`. After admission succeeds and the response is lost, the exact required retry presents an already-consumed Confirmation Artifact: AD-5 requires `202` plus the original task while AD-13/AD-19 require rejection. Both outcomes cannot be implemented.

**Attack:** Admit a confirmed move, commit confirmation consumption and the task record atomically, then drop the HTTP response. Repeat the byte-equivalent authenticated request with the same artifact and idempotency key. Two compliant adapters can choose opposite results.

**Disposition:** **Autofix before final.** Define one atomic admission ordering: after authentication, an existing idempotency record with identical scope, canonical request hash, confirmation binding/hash, and admitted task returns that same retained task; changed scope/material conflicts. Only when no matching admitted task exists may the platform validate and atomically consume a fresh artifact. A consumed artifact lacking that exact binding remains replay and returns `409`. Amend AD-13 and AD-19 so the matching lost-response recovery case is the explicit exception, not an authorization bypass.

## High findings

### H-1 — The normative state graph forbids cancellation that the rule permits

**Conflict:** AD-4 says cancellation is allowed whenever the irreversible checkpoint has not been crossed, and the transition authority rejects every edge absent from the graph. The graph permits cancellation only from `Pending` and `Running`; it omits `WaitingForDependency → Cancelled` and `NeedsAttention → Cancelled`. Both recoverable states can occur before the irreversible checkpoint.

**Attack:** A task waits on an unavailable owner before any mutation, or enters `NeedsAttention` after a pre-commit validation ambiguity. A user is entitled to cancel under the prose but the transition authority must reject it under the graph.

**Disposition:** **Autofix.** Add conditional cancellation edges from every nonterminal state in which the irreversible checkpoint can still be unset, or narrow the prose and product behavior explicitly. Include persisted-checkpoint evidence in the transition guard.

### H-2 — Terminal task-result retention needed for idempotent recovery is not bound

**Gap:** AD-4 binds only active-task polling. AD-5 says vaguely that “records” remain for at least 30 days or the result lifetime, but does not explicitly bind both the terminal result/task representation and its scoped idempotency record. The PRD requires both. An implementation can retain the idempotency row for 30 days, delete the terminal task immediately, and still claim literal compliance while being unable to return or poll the original task.

**Disposition:** **Autofix.** State that the terminal task result and scoped idempotency record remain resolvable together for at least 30 days and never less than the longer associated result lifetime. Require equivalent retry and task-location polling to remain coherent throughout that period; if storage uses tombstones, the tombstone must preserve the safe terminal contract rather than point to a missing task.

### H-3 — Writer rollback checks replay compatibility but not write compatibility

**Conflict:** AD-17 permits rollback whenever the old writer can replay every new event. The checked-in old writer currently emits `ProjectCreated(Active)` followed by `ProjectFolderCreationPending`, directly violating AD-3, AD-8, and AD-22. It may be able to deserialize/replay the additive `ProjectCreated` shape while still producing forbidden new folderless creation intervals after rollback.

**Attack:** Cut over to the SDK writer, write additive `ProjectCreated` events with Folder binding, then route commands back because the old fold can deserialize them. A new create through the old handler immediately reintroduces the superseded pending-Folder behavior.

**Disposition:** **Autofix.** Command rollback is permitted only when the prior writer both replays all new history and is fenced or upgraded to emit only events/semantics satisfying the current spine, including Folder-first creation and required confirmation/task behavior. Otherwise the only safe path is freeze mutation and roll forward. Add a rollback command-equivalence suite, not replay equivalence alone.

### H-4 — Folder-first creation can leak the hidden ProjectId or expose an orphan owner resource

**Gap:** AD-8 requires Folder creation before Project commit; AD-18 hides the reserved ProjectId until activation; AD-12 forbids automatic destructive rollback. G-2 does not require a Folders provisioning contract that prevents the pre-activation Folder or Project correlation from becoming caller-visible. A Folders implementation can expose the newly created same-name Folder—and any ProjectId metadata—while Projects must still hide the Project, then retain that orphan indefinitely on failure.

**Disposition:** **Discuss, then bind in G-2/AD-8.** Choose and require one compatible owner contract: hidden/reserved Folder provisioning activated after Project commit; creation without externally visible Project correlation; or an explicitly accepted visible orphan with safe naming, authorization, reconciliation, and no ProjectId disclosure. Add live evidence for lost response, Project-commit failure, and reconciliation. Do not allow a general delete compensation to sidestep the owner boundary.

### H-5 — Audit retention and the 100,000-record bound have no coexistence rule

**Conflict:** AD-26 requires every listed audit outcome to be retained for at least 365 days. AD-27 says Projects “enforces” 100,000 retained audit records per Project. A sufficiently active Project can exceed 100,000 required records inside 365 days; deleting the oldest violates AD-26, while refusing to record later required security/mutation outcomes violates FR-21 and AD-26.

**Disposition:** **Discuss, then autofix.** Clarify that 100,000 is a supported query/performance shape rather than a destructive retention cap, or specify a lossless retained archive/tier and bounded online window. Admission back-pressure may protect capacity but must never drop an audit record for an operation already admitted. Add an overflow test that proves all mandatory records remain retained and queryable under the chosen interpretation.

## Medium findings

### M-1 — Editable UI descriptors and generated runtime descriptors overlap authority

AD-2 assigns FrontComposer descriptors to `Projects.UI.Contracts`; AD-16 says platform generators derive runtime descriptors from `Projects.Contracts`. The spine does not say whether UI descriptors are authored presentation metadata, generated operation descriptors, or both. If they can redefine action names, schemas, statuses, recovery codes, or authorization, they become a second editable wire authority.

**Disposition:** **Autofix.** Constrain authored UI descriptors to presentation/composition metadata and stable references to generated contract operations. All operation schemas, status/recovery vocabulary, action identity, and security requirements must be generated from or compile-time checked against `Projects.Contracts`; live-host parity must fail on an unbound descriptor.

### M-2 — “Consequential operations” is not an enumerable contract

AD-5's prose applies confirmation to “consequential operations,” but its `Binds` list covers only selected FRs, while AD-4 and AD-9 use broader consequential-work language. Direct creation, setup update, Conversation linking, and other mutations could be classified differently by Web, CLI, MCP, or platform admission.

**Disposition:** **Autofix or defer to a named contract artifact with a fail-closed default.** Enumerate which action IDs require Preview/Confirmation versus idempotent task admission without confirmation. Make the generated surface schemas consume that same classification so adapters cannot decide independently.

### M-3 — Repository-owned integration fixtures can silently recreate a local technical layer

AD-25 correctly requires local run/test parity, but “manifest-aware fixture” plus repository-owned restart/two-instance/browser/CLI/MCP lanes does not explicitly prohibit the fixture from owning ports, Dapr components, topology, credentials, retries, or lifecycle. That loophole would satisfy local tests while recreating a Projects-specific AppHost in test code and diverging from package-mode CI.

**Disposition:** **Autofix.** State that repository fixtures are thin consumers of the pinned runner/manifest contract: they may select profiles, start/stop through the runner, and assert outcomes, but may not define topology or technical provider behavior. Require the same manifest and runner version in local Debug and CI package-mode lanes, with a guardrail rejecting Projects-owned Aspire/Dapr/topology references outside temporary migration projects.

## Coexistence assessment

The main boundaries can coexist after these changes:

- DomainService/platform ownership and a repository-local runner are compatible because the spine keeps the runner independently consumable and gates removal on clean-checkout and package-mode parity.
- One Project stream, owner-authoritative foreign resources, and forward-recovery tasks are compatible if G-2 includes the missing pre-activation Folder behavior.
- Shadow reads and single-writer cutover are compatible if rollback proves write semantics, not deserialization alone.
- Surface parity and MCP containment are compatible if UI/runtime descriptors cannot redefine generated contract semantics.

Until C-1 and H-1 through H-3 are corrected, two teams can build mutually incompatible admission, cancellation, retention, and rollback behavior while each cites the spine. H-4 and H-5 require explicit product/platform disposition before their affected owner and persistence contracts can be considered ready.

## Recheck

**Verdict: REMAINS BLOCKED by two narrow internal inconsistencies.**

| Original finding | Disposition |
| --- | --- |
| C-1 lost-response retry versus replay | **Partial:** AD-13 now correctly returns the retained task for an exactly equivalent retry, but AD-19 still maps every consumed confirmation to `409`; qualify it as an **unmatched** consumed confirmation so the same request has one transport outcome. |
| H-1 missing cancellation transitions | **Resolved:** the graph now permits guarded cancellation from `WaitingForDependency` and `NeedsAttention` before the irreversible checkpoint. |
| H-2 terminal result retention | **Resolved:** AD-5 now retains the terminal task result and scoped idempotency record together for the required window. |
| H-3 replay-only rollback gate | **Partial:** AD-17 now requires both replay and write-semantic compatibility, but its Mermaid rollback label still says freeze only when the old writer cannot replay; extend the label to include inability to emit spine-compliant events/behavior. |
| H-4 pre-activation Folder/ProjectId visibility | **Resolved:** AD-8 binds hidden-ID provisioning, restricted pre-activation visibility, and reconciliation without automatic deletion. |
| H-5 audit retention versus 100,000 records | **Resolved:** AD-27 makes 100,000 an online/query performance shape and requires lossless queryable archival inside the retention window. |
| M-1 descriptor authority overlap | **Resolved:** AD-16 limits UI descriptors to presentation metadata and fixes generated operation/security authority in `Projects.Contracts`. |
| M-2 undefined consequential-operation set | **Resolved:** AD-5 and the canonical classification enumerate stable action IDs and their admission classes. |
| M-3 fixtures recreating technical layers | **Resolved:** AD-25 and G-4 require thin runner consumers and prohibit Projects-owned topology/provider/lifecycle definitions. |

No new ownership, recovery, extraction, run/test, or surface-semantic contradiction was found beyond those two stale cross-references. After both wording fixes, this lens passes.

## Final recheck

**PASS.** AD-13 and AD-19 now agree that an exactly equivalent authenticated retry returns `202` with the retained original task, while only unmatched consumed-artifact replay returns `409`. AD-17 and its Mermaid rendering now both require freeze-and-roll-forward when the prior writer cannot either replay new history or emit spine-compliant behavior. The neighboring admission, retention, single-writer, and retirement rules introduce no new conflict.
