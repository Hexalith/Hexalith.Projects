# PRD Quality Review — Hexalith.Projects

## Overall verdict

This PRD holds up as a chain-top product contract: §2.1 names real trade-offs, §2.4 makes omissions explicit, and most FRs carry testable consequences under a shared §5 recovery contract. What is at risk is silent product invention — UX, architecture, and stories must still decide what makes a match exclusive enough to skip confirmation, which evidence is required for `Complete` versus `Partial`, and how to treat brownfield folderless Projects that the addendum admits and the PRD's present-tense invariant denies. `status: final` is earned for direction; it is not earned as a claim that no product questions remain for downstream work.

## Decision-readiness — strong

A decision-maker can act on this document. §2.1 states choices as choices and names what was given up: one Conversation per Project trades “simultaneous cross-Project context for an unambiguous workspace boundary”; one Folder trades “multi-root workspaces for a stable authorization anchor”; current recomputation trades “retrospective inference replay for current-authorization safety and lower diagnostic retention.” §2.3 is equally blunt: a core-only build is “internal evidence, not an authorized production release,” and “there is no smaller safe production release.”

Pushback is acknowledged rather than smoothed. Multi-Project conversations, multi-root workspaces, persisted inference history, generic project management, standalone UI, autonomous MCP confirmation, cross-Tenant sharing, customer-managed keys, and cross-region DR are excluded with a revisit condition or an enduring-boundary rationale. Vision already decides the confirmation shape: Chatbot may “determine which Project applies,” “ask for confirmation when intent is ambiguous,” and “propose a Project when none fits.” That is a real decision, not a balanced shrug.

The remaining weakness is over-clearance, not missing trade-offs. §2.5 asserts “No phase-blocking product questions remain for UX, architecture, or epic decomposition” while several product rules those workflows must source are still implicit (match exclusivity, required evidence, brownfield folderless handling). Those holes are scored below; they do not erase the honesty of the decisions that *are* written.

### Findings

_None._

## Substance over theater — strong

The furniture test mostly fails to find furniture. There is no standalone persona section; five named journeys in §3.4 earn distinct confirmation, recovery, isolation, and restore behavior. The Vision cannot be swapped into a generic PM or chatbot PRD: the module “is not a generic project-management system” and exists to give Chatbot “a durable workspace boundary.” NFRs are product-specific envelopes (99.9% availability, RTO 15 minutes, RPO 0, 10,000 Projects/Tenant, p95 500 ms, 1 MiB / 500 / 100 export caps, WCAG 2.2 AA, 15-minute Confirmation Artifact expiry, 365-day audit retention), not “must be scalable / secure / reliable.”

Roles in §3.2 are authority tables, not value statements. The addendum correctly parks mechanism, package-boundary, and evidence-index depth so the PRD stays a product contract.

The one theatrical patch is the success-metric set: SM-1, SM-4, SM-5, and SM-6 restate NFR-3, NFR-5, NFR-9, and NFR-11 as “metrics.” SM-2, SM-3, SM-7, SM-8, and SM-C4 still do independent work.

### Findings

- **medium** Success metrics that only echo NFRs (§8 SM-1, SM-4, SM-5, SM-6) — SM-1 repeats “at least 99.9% monthly availability” from NFR-3; SM-4 says list/open/resolution/context/admission “meet NFR-5”; SM-5 restates NFR-9’s automated-plus-manual accessibility bar; SM-6 restates NFR-11’s “zero failed critical cases and zero unexplained critical skips.” For a platform module, operational gates are legitimate, but four of eight primaries add no thesis signal beyond the NFR section. *Fix:* Keep SM-2, SM-3, SM-7, and SM-8 as primaries; treat the NFR restatements as release-acceptance checks under NFR-11, or give each an independent measurement definition that can fail when the NFR text is met.

## Strategic coherence — strong

The thesis is stable from Vision through metrics: Chatbot should resume the correct Tenant-scoped Project Context without reconstruction, silent attachment, or inferred completion. §6 groups serve that arc — workspace identity, cross-context references, resolution, context assembly, setup quality, then audit/operations as the production-safety half — rather than reading as a backlog with headings.

§2.3’s scope kind is platform: core user value (FR-1–FR-20, FR-23) may be sequenced first, but “no approved v1 FR or NFR is deferrable from production release.” That matches a Chatbot-platform module rather than a revenue or experience MVP cut. SM-7 and SM-8 validate the user-facing half of the thesis (resolution usefulness; continuity without reconstruction). SM-C1 through SM-C4 name the Goodhart failures — automatic attachment, context volume, acknowledgement-as-completion, and relabeling unavailable outcomes as SM-8 successes.

FR-22 and FR-24, added in the rebaseline, are argued in §2.3 as “the supported alternatives to unbounded troubleshooting access,” so they sit on the thesis rather than beside it.

### Findings

_None._

## Done-ness clarity — adequate

Most FRs would let an engineer write an acceptance test. FR-1, FR-4, FR-7, FR-8, FR-11, FR-14, FR-16, FR-21, FR-23, and FR-24 specify admission versus completion, confirmation binding, fail-closed cases, and read-model confirmation. §5 gives binding meanings for `Complete`, `Partial`, `Unavailable`, and `Denied`, plus refresh and recovery transitions (“Expired or stale confirmation returns `RenewPreview` and admits no task”). NFR-3 through NFR-11 are bounds, not adjectives.

The dimension is not strong because the shared contracts that every story will hang on still contain product-shaped blanks. “Other required evidence,” what counts as a match, and mutation FRs that omit the Durable Task / recovery pattern will be invented differently by UX, architecture, and epics.

### Findings

- **high** Required versus optional evidence for `Complete` / `Partial` is not enumerable (§5; FR-2, FR-16, FR-20) — `Complete` requires “the authorized Project, Project Folder, Project Setup, and other required evidence” to be `Current`. `Partial` says “authorization evidence required for first-response admission” is `Current` while “optional references” may be excluded, stale, rebuilding, or unavailable. The PRD never lists the required set versus optional set, nor whether that set changes by purpose (open, list, resolution, context, Conversation-start). FR-20 then lets Chatbot admit a first response on `Complete` or `Partial`. Stories will disagree on whether a missing Memory, File Reference, or Conversation link blocks admission or is a disclosed omission. *Fix:* Per response purpose, name the required components and state that File References and Memories are optional unless Project Setup marks them required.

- **high** `SingleCandidate` exclusivity is unspecified, so confirmation can be skipped on a weak match (§1; FR-12, FR-13, FR-14; SM-C1) — Vision allows Chatbot to “determine which Project applies” and reserves confirmation for ambiguous intent. FR-12 only returns `NoMatch`, `SingleCandidate`, or `MultipleCandidates` “with current Resolution Reason Codes.” FR-14 applies only “When resolution returns multiple candidates.” `MetadataMatched` can therefore produce a sole `SingleCandidate` and skip Preview. SM-C1 forbids optimizing “automatic attachment rate at the expense of explicit intent,” but no rule says a metadata-only exclusive hit is or is not enough to determine. That is the common path, not an edge. *Fix:* State the exclusivity rule in §2.1 / FR-12 (for example: `SingleCandidate` requires `ConversationLinked` or `ProjectFolderMatched`, or any sole `MetadataMatched` / `FileReferenceMatched` / `MemoryMatched` hit must use FR-14 confirmation).

- **medium** File and Memory link FRs omit the recovery contract used everywhere else (FR-9, FR-10 vs FR-6, FR-8, FR-11) — FR-9 and FR-10 require actor-selected versus inferred confirmation and metadata-only storage, but they do not require a Durable Task, Read-Model-Confirmed Completion, lost-response recovery, or fail-closed stale evidence. FR-6 and FR-8 do. Cross-context association is exactly where §2.2 promises “Durable, idempotent, recoverable” workflows. *Fix:* Give FR-9 and FR-10 the same task, idempotency, completion, and recovery consequences as FR-6.

- **medium** “Relink” and “manual reconciliation” are authorized and audited without a mutation FR (§3.2; FR-21, FR-22) — The role table grants Project Users “relink, and unlink” and Tenant Project Administrators “relink/unlink.” FR-21 audits “move, relink, Folder replacement, unlink” as distinct events and also “manual reconciliation.” FR-22 says Tenant Project Administrators “may perform authorized reconciliation.” No FR specifies preview, confirmation, targets, or completion for relink or reconciliation. FR-7 (move) and FR-8 (Folder replacement) are already named separately from relink. *Fix:* Either define FR-level relink and reconciliation (actor, targets, Preview binding, completion) or delete those words from §3.2 and FR-21 and map the permission onto FR-6/FR-7/FR-8/FR-11/FR-23.

- **medium** SM-7’s unit of measure is not defined (§8 SM-7) — SM-8 defines “eligible resumption” and “continuity success” with inclusion, exclusion, and denominator rules. SM-7 measures “authorized Project Resolution episodes” without saying what starts or ends an episode, how repeated resolve calls in one Conversation count, or what a “Project correction” is (SM-8 defines “context correction,” not Project correction). The 90% / 15-minute bar cannot be implemented from the PRD alone. *Fix:* Define an episode as one Conversation-start or resolve request with no explicit Project, and reuse SM-8’s context-correction definition or define Project correction explicitly.

- **low** Residual adjectives on otherwise testable FRs (FR-15, FR-19) — FR-15: “The proposal may suggest a Project name and setup metadata.” FR-19 rejects “control/invisible characters where unsafe.” “May” and “where unsafe” are the hedges the rubric flags. *Fix:* Require a suggested name when attachments or Conversation metadata can supply one; point “unsafe” characters at the addendum’s U+2028/U+2029 / control-character rule.

## Scope honesty — adequate

§2.4 does real work. Enduring boundaries (Projects is not the system of record for Conversations, Folders, or Memories; no payloads, secrets, or transcript storage; no persisted Resolution Traces) are separated from v1 exclusions (indexing, Memory synthesis, autonomous MCP confirmation, cross-Tenant sharing, CMEK, cross-region DR). §2.3 forbids silent FR/NFR removal. Document Purpose routes “Technical mechanisms, migration detail, and proposal-specific implementation evidence” to the addendum, and the addendum is candid that E-17 is `NOT READY` and that live evidence was “19 passes and 56 failures.”

Honesty fails where the PRD states an invariant as already true while the addendum treats the opposite as a v1 migration obligation, and where load-bearing cross-system inferences carry no `[ASSUMPTION]` tag on a `status: final` chain-top PRD.

### Findings

- **medium** Present-tense folderless invariant contradicts acknowledged brownfield (NFR-4; FR-1; addendum §5) — NFR-4: “Active Projects are never folderless.” FR-1: dependency failure “never exposes an Active folderless Project.” Addendum §5 requires “Inventory and reconciliation of legacy Active folderless Projects and in-flight Folder work before those Projects can appear in lists or participate in resolution or context.” A PRD-only reader will treat the invariant as already universal and skip migration stories; a pair-reader gets two incompatible tenses. *Fix:* In §2.4 or NFR-4, state that legacy Active folderless Projects may exist and must be reconciled before list/resolution/context admission; keep “never folderless” as the post-reconciliation / new-work invariant.

- **medium** Zero tagged assumptions on a green-light chain-top PRD (§2.5; §8 measurement contract; FR-1) — There are no `[ASSUMPTION]`, `[NOTE FOR PM]`, or Open Questions. §2.5 then claims no phase-blocking product questions remain. The PRD still depends on untagged inferences: SM-7/SM-8 require “Chatbot’s metadata-only companion outcomes”; FR-1 depends on `Hexalith.Folders` performing “same-name Folder creation”; addendum §5 says this PRD “does not authorize sibling-repository changes by implication.” Addendum E-17 still records “four UX-alignment findings.” Open-item density of zero is not honesty when those dependencies can block UX and stories. *Fix:* Tag the Chatbot measurement-companion and Folders same-name-create dependencies as `[ASSUMPTION]` with owners, and downgrade §2.5 to “no open product *direction* questions; specification holes listed in Open Questions.”

## Downstream usability — adequate

This dimension is load-bearing (chain-top). The extractable spine is good: §4 glossary is large and mostly stable; FR-1–FR-24, NFR-1–NFR-11, UJ-1–UJ-5, SM-1–SM-8, and SM-C1–SM-C4 are contiguous and unique; FRs cite UJs and §5 by ID/section rather than “see above”; each UJ has a named protagonist (Priya, Jules, Sam, Alex, Morgan). The addendum’s routing paragraph and E-1–E-18 index tell architecture, UX, API, test, and epic owners where mechanism lives.

Extractability breaks where the same noun means two things, or where a count noun used in an NFR is not a glossary term. Those collisions will fork stories.

### Findings

- **medium** Domain nouns used in FRs/NFRs/SMs do not round-trip through the glossary (§4; §6.2; FR-17; NFR-5; §8) — **Resolution Trace** is “diagnostic evidence for the current Project Resolution computation,” but FR-17 uses “current Resolution Traces” to explain why a *context reference* was included or excluded. **Context Reference** is the §6.2 heading, FR-11’s object, and NFR-5’s “5,000 Context References per Project,” but it is not a glossary term. SM-7’s “Project correction” is not SM-8’s defined “context correction.” Downstream will emit two trace types, an undefined reference counter, and two correction metrics. *Fix:* Add Context Reference to §4; rename FR-17’s artifact (or widen Resolution Trace); use one correction term with one definition.

- **low** SM-7’s source pointer does not resolve (§8 SM-7) — “The source is the metadata-only Resolution/Chatbot outcome feed defined above” never names the “Outcome measurement contract” block. *Fix:* Cite “§8 Outcome measurement contract.”

## Shape fit — strong

The hybrid is the right shape. This is an internal Chatbot platform module with meaningful confirmation UX, not a single-operator CRUD tool and not a public launch. Capability-grouped FRs plus five compact journeys is proportionate: UJs carry Chatbot confirmation, recovery, isolation, and restore; the §3.2 role matrix carries operator/admin/service authority without persona theater. Success metrics mix operational release gates with user-outcome SM-7/SM-8, which fits a platform module that also feeds UX.

Chain-top concerns are placed correctly: product contract in `prd.md`, mechanism/migration/package/evidence in `addendum.md`. Brownfield siblings (`Hexalith.Conversations`, `Hexalith.Folders`, `Hexalith.Memories`, EventStore, Dapr) are named as systems of record. The document is long because the FR/NFR set is long, not because it was forced into a consumer-PRD template.

### Findings

_None._

## Mechanical notes

- Frontmatter is `status: final` with `updated: 2026-07-15`. The addendum’s controlling readiness row is E-17 / E-18 (2026-08-02 / 2026-08-03). The product contract date and the evidence-index date have drifted; that does not by itself invalidate §2.1.
- FR-1–FR-24, NFR-1–NFR-11, UJ-1–UJ-5, SM-1–SM-8, and SM-C1–SM-C4 are contiguous and unique. FR-23/FR-24 are append-only IDs from the rebaseline; the addendum records that history.
- Cross-references that resolve: FR-4 ↔ FR-23, FR-6 → FR-7, FR-15 → FR-1, FR-22 vs FR-24, UJ realizations on FRs, SM validates-lists. Cross-references that do not: SM-7 “defined above”; “relink” in §3.2/FR-21 (no FR); “Context Reference” (no glossary entry).
- No inline `[ASSUMPTION]` or `[NOTE FOR PM]` markers, so Assumptions Index roundtrip is not applicable — and is a scope-honesty gap, not a clean bill of health.
- Every UJ has a named protagonist carrying context inline. UJ-4 (Alex) is a negative isolation constraint written as a journey; it still has a protagonist.
- Recovery Action Code `RequestPreview` is defined in §4 and never assigned a binding trigger; `RenewPreview` is. Harmless if architecture owns it; worth one glossary sentence.
- Glossary case/plural is mostly stable (`Project Folder` vs `Hexalith.Folders`, `Active`/`Archived`). Drift to watch: “read-model-confirmed” / “Read-Model-Confirmed Completion” / “read model confirms”; “relink” vs “link” vs “move.”
