# Architecture Spine Review — Rubric Walker

**Reviewed:** `ARCHITECTURE-SPINE.md` (draft updated 2026-07-16)  
**Lens:** BMad Good Spine checklist, template completeness, deterministic lint intent, and finalizability  
**Verdict:** **CHANGES REQUIRED.** The spine is unusually complete for an initiative-level brownfield rebaseline and covers the operational envelope, but two source-contract contradictions and three enforceability gaps must be fixed before `status: final`.

## Gate Evidence

- `lint_spine.py` result: **PASS**, zero findings.
- AD identifiers are unique and ascending (`AD-1` through `AD-33`).
- Every AD contains `Binds`, `Prevents`, and `Rule`.
- Stack entries are syntactically pinned; prerelease/runtime skew is exposed through G-6 rather than hidden.
- No template comments, assumptions, placeholders, empty sections, or empty Mermaid graphs remain.
- Required initiative dimensions are present: paradigm, dependency/ownership shape, mutation and durable state, data authority, contract evolution, migration, security, operations/environments, local execution, test/release evidence, capability mapping, and bounded deferrals.

## Critical Findings

### RW-1 — AD-5 contradicts the approved confirmation boundary

**Disposition:** Autofix before finalization.

AD-5 says all consequential operations bound there require a Confirmation Artifact. That conflicts with the governing PRD: an explicitly actor-selected additive Conversation link (FR-6), initial Folder binding (FR-8), File link (FR-9), and Memory link (FR-10) use an idempotent Durable Task **without a second confirmation**; inferred links require confirmation. AD-5 omits FR-6/9/10 from `Binds`, includes FR-8 without distinguishing initial actor-selected from inferred/replacement behavior, and can cause adapters to demand an unauthorized extra interaction.

**Required correction:** Make AD-5 govern “operations for which the contract requires confirmation,” explicitly preserve the actor-selected additive exceptions, and state that inferred links, moves, unlinks, archive/restore, proposal confirmation, and Folder replacement follow Preview/Confirmation. Make AD-13 conditional on that same boundary. Keep every admitted mutation idempotent even when confirmation is not required.

### RW-2 — AD-3 makes required Archived/folder-recovery records undiscoverable

**Disposition:** Autofix before finalization.

AD-3 forbids any Project from being “visible, listed, opened” until a Folder is bound. That is stronger than the governing contract: FR-5 lists authorized Active and Archived Projects with Folder availability, and FR-23/UJ-5 must expose enough authorized Archived state to preview recovery when Folder evidence is missing or invalid. The current rule can make the restore path impossible for the very historical records AD-17 and AD-23 are designed to repair.

**Required correction:** Bind the invariant to `Active`, resolution eligibility, and context usability. Keep pre-activation creation tasks hidden from Project reads, but allow authorized Archived legacy records to appear in safe list/operator/restore views as `Unavailable` with no protected leakage and only the recovery actions needed by AD-23. Rename the AD title accordingly.

## High Findings

### RW-3 — The “exact” task state graph rejects required recovery/cancellation transitions

**Disposition:** Autofix before finalization.

AD-4 declares that transitions absent from its graph are rejected, but the graph has no path from `WaitingForDependency` to `NeedsAttention` when retry/dependency recovery is exhausted, and no cancellation path from `WaitingForDependency` or `NeedsAttention` even when the irreversible checkpoint has not been crossed. This conflicts with AD-4's own global cancellation rule and AD-27's “waiting/intervention” outcome.

**Required correction:** Add guarded transitions `WaitingForDependency -> NeedsAttention`, `WaitingForDependency -> Cancelled`, and `NeedsAttention -> Cancelled` (the latter two only before the irreversible checkpoint). If recovery can produce a known terminal owner/domain outcome directly, either add those guarded edges or state that recovery must first reacquire `Running`; avoid leaving that choice to individual workflows.

### RW-4 — NFR-9 is mapped but not architecturally governed

**Disposition:** Autofix before finalization.

The capability map assigns NFR-9 to AD-2, AD-29, and AD-30, but none establishes its WCAG 2.2 AA invariant. AD-30 only requires an accessibility evidence category. Independent FrontComposer, Chatbot, Web, and operator implementations could therefore choose incompatible accessibility behavior while still claiming conformance to the spine.

**Required correction:** Add an accessibility convention or AD binding NFR-9: WCAG 2.2 AA, keyboard operation, visible focus, assistive-technology announcements, no color/timing-only meaning, 200% zoom, 320 CSS-pixel reflow, and both automated and authenticated manual keyboard/screen-reader evidence. Update the capability map to cite it. Exact interaction and visual design can remain deferred.

### RW-5 — AD-25 permits two runner delivery forms while the structural seed requires one

**Disposition:** Discuss with the platform owner, then make the spine internally singular before finalization.

AD-25 and G-4 permit either a pinned package/tool **or** a root-declared submodule entry point, while the repository contract mandates `dotnet tool run hexalith-module ...` after `dotnet tool restore`. A submodule-only entry point does not necessarily implement that clean-checkout command contract, so two platform features could satisfy different halves of the rule and still be incompatible.

**Required correction:** Choose the independently consumable tool as the stable repository contract, with source-project/submodule use only as a Debug development implementation behind the same tool command; or defer the delivery mechanism and avoid presenting the four tool commands as fixed until G-4 selects it. Preserve the user's non-negotiable outcome: this repository must run, tear down, debug, and test the module without owning an AppHost.

## Medium Findings

### RW-6 — AD-27 turns capacity targets into ambiguous destructive caps

**Disposition:** Autofix wording.

“Projects enforces ... 100,000 retained audit records per Project” can be read as permission to stop recording or delete audit data at 100,000, conflicting with AD-26/NFR-8's at-least-365-day durable audit obligation. The same sentence converts PRD “supports” scale figures into hard admission limits without defining the safe outcome at the limit.

**Required correction:** Distinguish supported capacity/admission bounds from retention truth. State explicitly that the 100,000 figure never authorizes dropping, refusing, or prematurely deleting required audit; retention obligations win and capacity pressure must fail safe/scale out. Define safe structured behavior for Project/reference admission limits if they are intended as hard product caps.

### RW-7 — The dependency diagram points domain code at sibling implementations

**Disposition:** Autofix diagram labels/nodes.

The diagram's `Domain -> Conversations/Folders/Memories` arrows are labelled “versioned owner contract,” but their targets are bounded-context implementation boxes. This weakens the otherwise clear domain-only boundary and can be read as permission for direct implementation/package dependencies.

**Required correction:** Introduce explicit owner-contract/port nodes (or label the targets as contract surfaces) and show runtime invocation through the platform workflow transport. The diagram should make compile-time dependency and runtime invocation direction unambiguous.

## Good-Spine Checklist

| Check | Result | Judgment |
| --- | --- | --- |
| Fixes the real divergence points at initiative altitude | **Partial** | Strong breadth; RW-1, RW-2, and RW-5 remain incompatible choices. |
| Every Rule is enforceable and prevents its stated divergence | **Partial** | Most are testable; RW-3's graph and RW-6's capacity wording are not yet safe. |
| Deferred contains no load-bearing unbounded choice | **Pass with condition** | Deferrals preserve behavioral invariants; runner delivery must be made singular or explicitly deferred. |
| Named technology is pinned and reality risk is visible | **Pass, lens-dependent** | Exact local pins and prerelease/runtime skew are stated; current-version verification remains the dedicated technology reviewer's concern. |
| Ratifies brownfield reality rather than pretending greenfield | **Pass** | Immutable history, legacy adapters, shadow reads, writer fencing, compensating tasks, and retirement gates are explicit. |
| Covers governing PRD capabilities | **Partial** | FR/NFR coverage table is complete, but RW-1/RW-2 conflict with capability semantics and NFR-9 lacks a binding convention. |
| Parent-spine inheritance is preserved | **N/A** | No parent spine is declared. |
| Every owned structural dimension is decided/deferred/open | **Pass** | Operational/environmental, security, deployment, migration, test, and cross-repository dimensions are all addressed. |
| Dependency diagram conveys a binding direction | **Partial** | Overall ownership is strong; sibling contract/implementation direction needs RW-7. |
| Stack is pinned seed rather than an unbounded technology list | **Pass** | Versions are exact and the risky prerelease/skew entries are gated. |
| Structural seed is minimal and runnable/testable | **Pass with condition** | Package graph and lane coverage are strong; runner delivery contradiction is RW-5. |
| Capability map is complete and auditable | **Pass with correction** | All FR/NFR ranges appear; NFR-9's governing reference needs an enforceable target. |
| No placeholders/comments/template residue | **Pass** | Confirmed by inspection and deterministic lint. |

## Decision Density and Finalizability

The 33 ADs and 481 lines are dense for a spine, but the density is defensible for a high-stakes initiative rebaseline spanning three epics, six repositories/platform boundaries, migration, durable workflow, and release containment. AD-17, AD-30, and AD-32 are compound; do not expand them further. Favor precise corrections inside existing stable AD IDs and conventions over adding new sections. A new accessibility AD is justified only if the existing convention table cannot carry NFR-9 enforceably.

After RW-1 through RW-5 are resolved, apply the medium wording/diagram fixes, rerun deterministic lint, confirm no reviewer-introduced placeholder or Mermaid error, then the spine is finalizable by changing frontmatter to `status: final` and recording the required memlog finalization event.

## Recheck

**Disposition: PASS — no remaining rubric blocker.**

- **RW-1 resolved:** AD-5 and the canonical admission table now preserve task-only actor-selected additive actions and restrict confirmation to the enumerated action IDs.
- **RW-2 resolved:** AD-3 now gates Active/context usability while permitting safe `Unavailable` Archived recovery views.
- **RW-3 resolved:** AD-4 now includes guarded waiting-to-attention and pre-irreversible cancellation transitions.
- **RW-4 resolved:** AD-34 makes WCAG 2.2 AA behavior and automated/authenticated manual evidence binding, and the capability map cites it.
- **RW-5 resolved:** AD-25, G-4, and the structural seed consistently require one pinned repository-manifest .NET tool contract.
- **RW-6 resolved:** AD-27 distinguishes supported audit query shape from retention, requires lossless queryable archival, and forbids audit loss under pressure.
- **RW-7 resolved:** the ownership diagram now separates compile-time owner contracts from platform-mediated runtime calls and defines arrow semantics.

The original gate criteria remain satisfied: deterministic lint reports zero findings; AD IDs are unique and complete through AD-34; every AD has enforceable `Binds`/`Prevents`/`Rule`; stack and compatibility risks are pinned/gated; the package, runner, topology, capability, migration, security, operational, and deferred dimensions are covered; and no placeholder, assumption tag, template comment, or empty section remains. The draft is finalizable after the normal frontmatter and memlog close steps.
