# Final Reconciliation: FrontComposer Contract Boundary

## Input

- Approved source: `sprint-change-proposal-2026-07-14-frontcomposer-contract-boundary.md` (2026-07-14).
- Compared with: `prd.md` and `addendum.md`, revised 2026-07-15.
- Verdict: **Partially reconciled — product intent is preserved, but the addendum needs correction before this input is fully disposed.**

## Faithfully captured

- The PRD correctly makes no feature, journey, role, FR, MVP-scope, or observable Web/CLI/MCP behavior change; the proposal is a technical package-boundary correction.
- Addendum §4.2 preserves the essential goals of a UI-free reusable contract kernel, public-API and assembly-identity review, cross-surface consumer audit, and generation without runtime MCP/CLI coupling.
- PRD NFR-10 supplies the compatibility gate for any approved breaking change, while NFR-11 and addendum §7 retain compatibility, privacy, accessibility, parity, and release evidence obligations.
- The broader addendum correctly routes package mechanics, migration, and verification detail downstream rather than importing physical assembly layout into the capability-oriented PRD.

## Gaps

1. **Selected boundary and owner are not faithfully named.** The approved decision is a non-packable `Hexalith.Projects.UI.Contracts` descriptor host owned and delivered in the Projects repository by Story 5.13. Addendum §4.2 instead says “Contracts-versus-Contracts.UI” and assigns the exact movable-type inventory and package/version transition to the FrontComposer repository's planning flow.
2. **The concrete package invariant and release gate are missing.** The addendum does not retain `UI.Contracts` → `Contracts`, the prohibition on Contracts dependencies on FrontComposer Shell, Fluxor, Fluent UI, and `Microsoft.AspNetCore.App`, exclusion of `UI.Contracts` from the package inventory, MCP/CLI independence from that host, or Story 5.13 completion as a Contracts package release-readiness condition.
3. **Compatibility disposition is lossy.** The proposal requires a supported-consumer audit, preservation of logical namespaces and FrontComposer contract versions by default, and an approved migration/package-versioning decision when impact is found. The addendum omits those preservation defaults and changes the conditional decision into a categorical “explicitly approved breaking release” statement.
4. **Boundary-specific regression evidence is under-specified.** Generic parity and release testing remain, but the addendum does not bind this split to equivalent descriptor discovery, routes/navigation, views, state names, reason codes, fields, and contract versions; retargeted FrontComposer inspection; descriptor tests in the UI lane; package dependency/non-packability gates; and unchanged tenant-isolation, accessibility, and `NoPayloadLeakage` evidence.

## Contradictions

- There is **no contradiction with the revised PRD's product requirements**; the proposal explicitly preserves them.
- Addendum §4.2 contradicts the approved proposal by routing Projects' movable-type inventory and transition to the FrontComposer repository. FrontComposer provides precedent and tooling constraints; Projects owns this consumer-side assembly split.
- The addendum's unconditional breaking-release wording is stronger than the proposal's audit-driven compatibility path. If the stricter NFR-10 interpretation is intended to supersede the proposal, that supersession should be stated explicitly rather than silently substituted.

## Qualitative intent at risk

- Keep this a surgical ownership correction, not a FrontComposer redesign or reopening of completed functional stories.
- Move only application presentation descriptors; keep shared DTOs, semantic enums, constants, lifecycle vocabulary, and lightweight badge metadata defined once in packable Contracts.
- Preserve logical namespaces and contract versions unless a separate compatibility decision approves cleanup.
- “No MVP delay” does not mean “release-ready”: the Contracts package boundary remains gated on the corrective story and recorded compatibility disposition.
- Preserve historical story files as evidence while correcting architecture, projection catalog, durable guidance, sprint tracking, and current implementation artifacts.

## Disposition

- Leave `prd.md` unchanged.
- Revise addendum §4.2 to name `Hexalith.Projects.UI.Contracts`, correct ownership to the Projects repository/Story 5.13, retain the dependency and package gates, and express the audit-driven compatibility decision without losing the namespace/version preservation default.
- Route exact type inventory, assembly scanning, gate commands, story wording, and artifact edits to architecture, epics, test strategy, and repository-local implementation planning.
- Reconciliation can be marked complete after those addendum corrections; until then, disposition is **revise, then accept**.
