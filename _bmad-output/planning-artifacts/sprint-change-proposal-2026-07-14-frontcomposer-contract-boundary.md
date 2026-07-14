---
title: "Sprint Change Proposal: FrontComposer Contract Boundary"
status: approved
created: 2026-07-14
approved: 2026-07-14
approved_by: Jerome
project: Hexalith.Projects
scope: moderate
mode: incremental
---

# Sprint Change Proposal: FrontComposer Contract Boundary

## 1. Issue Summary

### Trigger

Story 5.3 introduced the FrontComposer projection-hosting pattern into
`Hexalith.Projects.Contracts`. Its Senior Developer Review recorded a follow-up architecture
decision after observing that the packable Contracts project now carried the Blazor/Fluent UI
runtime stack. The Epic 5 retrospective and sprint action ledger retained the decision as open.

### Problem Statement

`Hexalith.Projects.Contracts` is a reusable, packable contract assembly, but it currently also
hosts application-specific `[Projection]` descriptors. Compiling those descriptors requires
`Hexalith.FrontComposer.Shell`, Fluxor, Microsoft Fluent UI Blazor, and the
`Microsoft.AspNetCore.App` framework reference. Those dependencies are consequently exposed by the
Contracts NuGet package to consumers that only require domain/wire contracts.

The assembly is serving two incompatible roles:

1. reusable domain, wire, identifier, DTO, and shared-vocabulary contract boundary; and
2. application presentation descriptor host for generated FrontComposer surfaces.

### Evidence

- `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` has
  `IsPackable=true` while referencing `Hexalith.FrontComposer.Shell`, Fluxor, Fluent UI, and
  `Microsoft.AspNetCore.App`.
- The generated `Hexalith.Projects.Contracts` NuGet package declares those UI/runtime
  dependencies and the ASP.NET Core framework reference in its nuspec.
- Story 5.3's review explicitly records this package-boundary concern and defers the split to an
  architecture decision.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` retains the decision as an open
  Winston/Amelia action item.
- FrontComposer's reusable `Contracts` and `Contracts.UI` projects are packable boundaries, while
  its application-specific Counter projection host is non-packable. This supplies an implemented
  precedent for separating framework contracts from a consumer's descriptor host.
- Projects' MCP and CLI implementations do not require the projection wrapper assembly for their
  shared protocol vocabulary. Their shared constants and enums can remain in the reusable
  Contracts assembly.

### Decision

Move application-specific FrontComposer projection descriptors to a non-packable
`Hexalith.Projects.UI.Contracts` projection-host assembly. Retain reusable domain/wire DTOs,
semantic enums, protocol constants, and lightweight `[ProjectionBadge]` vocabulary in the packable
`Hexalith.Projects.Contracts` assembly.

## 2. Impact Analysis

### Epic Impact

- Epic 5 is the only affected epic.
- Add Story 5.13, **Isolate FrontComposer projection descriptors from packable Contracts**.
- Stories 5.3 through 5.11 retain their completed functional acceptance criteria. Story 5.3's
  assembly-placement assumption is superseded by Story 5.13.
- Existing Story 5.12, **Live AppHost operational-console verification**, remains unchanged and in
  review; its concurrent addition requires this correction to use the next available story number.
- Story 1.2 remains valid: the shared semantic vocabulary stays defined once in the packable
  Contracts assembly.
- No epic is added, removed, reopened, renumbered, or resequenced.
- No planned future epic is invalidated; there are no later planned epics requiring dependency
  changes.
- Story 5.13 should be completed before the Contracts package boundary is treated as release-ready.

### Artifact Conflicts

| Artifact | Impact | Required adjustment |
| --- | --- | --- |
| PRD | None | Preserve the current MVP and functional requirements. |
| UX specification | None | Preserve all Web/MCP/CLI behavior, vocabulary, accessibility, and parity requirements. |
| Architecture | Direct conflict | Replace the `Contracts/Ui` descriptor-host placement with a non-packable `UI.Contracts` boundary and record package invariants. |
| Epics | Missing corrective work | Add Story 5.13 with migration, compatibility, generation, package, and verification criteria. |
| Sprint tracking | Open decision without delivery vehicle | Close the decision action and add Story 5.13 as backlog work. |
| Projection catalog | Incorrect assembly ownership | Distinguish persisted projections from non-persisted FrontComposer descriptor inputs and record the new owner assembly. |
| Durable project guidance | Missing boundary rule | Record where application-specific descriptors and shared cross-surface semantics belong. |
| Historical story files | Historical evidence | Preserve them unchanged; Story 5.3 already records the concern and original context. |

### Technical Impact

The implementation will:

1. create `src/Hexalith.Projects.UI.Contracts/Hexalith.Projects.UI.Contracts.csproj` with
   `IsPackable=false`;
2. reference `Hexalith.Projects.Contracts` inward from the new project;
3. move the application-specific `[Projection]` wrappers, `ProjectsFrontComposerDomain`, and
   presentation-only vocabulary descriptors into the new assembly;
4. retain shared domain/wire DTOs, semantic enums, protocol constants, and lightweight
   `[ProjectionBadge]` annotations in packable Contracts;
5. split the shared maintenance action constants and lifecycle enums out of the file that currently
   co-locates them with `ProjectMaintenanceActionProjection`;
6. update the UI host to reference and scan the new descriptor assembly;
7. keep MCP and CLI independent of the UI descriptor host;
8. retarget the FrontComposer inspect gate and descriptor tests;
9. strengthen the package dependency gate so Contracts cannot reacquire UI runtime dependencies;
10. add the project to solution/build topology without adding it to the NuGet package inventory.

No event schema, OpenAPI schema, generated client wire contract, Dapr topology, persistence model,
tenant-isolation rule, user flow, or visual behavior changes.

### Compatibility Impact

Moving a public type to another assembly changes its assembly-qualified identity even if its logical
namespace is preserved. Before removal from Contracts, implementation must audit supported consumers:

- If no supported consumer references the descriptor types, record that evidence and preserve their
  logical namespaces and FrontComposer contract versions to minimize churn.
- If a supported consumer references them, approve a migration plan and package-versioning decision
  before release.
- Namespace cleanup is not part of this correction unless the compatibility review explicitly
  approves it.

## 3. Recommended Approach

### Selected Path: Direct Adjustment

Add Story 5.13 inside Epic 5 and perform a surgical assembly split. Do not move the entire
`Contracts/Ui` folder: it contains shared semantic types used by domain, server, MCP, CLI, and wire
contracts. Move only application presentation descriptors and presentation-only mappings.

### Estimate and Risk

- **Effort:** Medium — project topology, descriptor files, one co-located shared-type split, test
  ownership, generation gates, package gates, and documentation.
- **Risk:** Medium — assembly identity compatibility and generated surface discovery are the main
  risks; functional behavior is intentionally unchanged.
- **Timeline impact:** One corrective Epic 5 story. No epic resequencing or MVP delay is required,
  but package release readiness depends on its completion.
- **Scope classification:** Moderate — backlog and architecture coordination followed by bounded
  implementation.

### Alternatives Considered

**Rollback Epic 5:** Not viable. It would discard working operational surfaces and would not create
a sustainable contract/package boundary.

**PRD/MVP review:** Not viable as a solution. The issue is architectural and does not alter customer
or operator requirements.

**Leave descriptors in packable Contracts:** Rejected. It preserves accidental coupling, expands
the published dependency surface, and makes every contract consumer inherit application UI runtime
dependencies.

## 4. Detailed Change Proposals

### 4.1 Epic 5 — Add Story 5.13

**OLD**

Epic 5 includes Story 5.12, **Live AppHost operational-console verification**, which is in review.

**NEW**

```markdown
### Story 5.13: Isolate FrontComposer projection descriptors from packable Contracts

As a **package consumer / Projects platform engineer**,
I want **application-specific FrontComposer descriptors isolated in a non-packable UI contracts boundary**,
So that **the reusable Contracts package remains free of Blazor, Fluent UI, Fluxor, Shell, and ASP.NET Core runtime dependencies without changing cross-surface behavior**.

**Acceptance Criteria:**

**Given** `Hexalith.Projects.Contracts` is packable
**When** its produced NuGet package and project graph are inspected
**Then** it has no dependency on `Hexalith.FrontComposer.Shell`, Fluxor, Fluent UI, or `Microsoft.AspNetCore.App`
**And** its domain/wire DTOs, shared semantic enums, constants, and lightweight `[ProjectionBadge]` vocabulary remain reusable.

**Given** application-specific `[Projection]` wrappers
**When** the boundary is corrected
**Then** a non-packable `Hexalith.Projects.UI.Contracts` project owns the projection wrappers, `ProjectsFrontComposerDomain`, and presentation-only vocabulary descriptors
**And** it references the packable Contracts assembly inward.

**Given** maintenance action types currently coexist with a projection descriptor
**When** files are separated
**Then** MCP/shared action names and lifecycle enums remain in packable Contracts
**And** MCP and CLI acquire no dependency on the UI contracts project.

**Given** the UI host
**When** FrontComposer composition runs
**Then** it references and scans the new descriptor assembly
**And** generated navigation, views, state names, reason codes, fields, and contract versions remain equivalent across Web, MCP, and CLI.

**Given** descriptor types were previously emitted from a published assembly
**When** they are moved
**Then** a consumer audit records whether any supported consumer references them directly
**And** any detected compatibility impact has an approved migration and package-versioning decision before release.

**Given** CI and test gates
**When** the correction is complete
**Then** the FrontComposer inspect gate targets the new assembly
**And** descriptor tests run in the UI test lane
**And** the package-dependency gate rejects UI runtime dependencies from Contracts
**And** the new UI contracts project is verified as non-packable
**And** cross-surface parity and payload-leakage tests remain green.

**Given** architectural and operational documentation
**When** the story closes
**Then** architecture, projection catalog, durable project guidance, and sprint tracking record the corrected ownership boundary.
```

**Rationale:** This creates one independently verifiable corrective story without reopening
completed functional stories.

### 4.2 Architecture — Correct Component Ownership

**OLD**

```text
Hexalith.Projects.Contracts
└── Ui/  # shared enums plus [Projection]/[Command] FrontComposer contracts

Hexalith.Projects.UI  # FrontComposer Web host
```

```markdown
FrontComposer surfaces → annotated contracts in `Contracts/Ui/` → emitted into `UI`/`Mcp`/`Cli`.
```

**NEW**

```text
Hexalith.Projects.Contracts/              # packable, reusable contracts
└── Ui/                                   # shared semantic enums and lightweight
                                          # [ProjectionBadge] vocabulary only

Hexalith.Projects.UI.Contracts/           # non-packable FrontComposer descriptor host
├── projection/action wrappers
├── ProjectsFrontComposerDomain
└── presentation-only vocabulary descriptors

Hexalith.Projects.UI/                     # Web host; references and scans UI.Contracts
```

Add the following invariants:

```markdown
- FrontComposer surfaces originate from application-specific annotated descriptors in
  `Hexalith.Projects.UI.Contracts`; generated Web/MCP/CLI metadata continues to use the
  shared semantic vocabulary from `Hexalith.Projects.Contracts`.

- Dependency direction is `UI.Contracts` → `Contracts`. MCP and CLI must not reference
  `UI.Contracts` merely to obtain shared constants, enums, DTOs, or lifecycle vocabulary.

- Package boundary invariant: `Hexalith.Projects.Contracts` must not reference
  `Hexalith.FrontComposer.Shell`, Fluxor, Fluent UI, or `Microsoft.AspNetCore.App`.
  `Hexalith.Projects.UI.Contracts` is `IsPackable=false` and is excluded from the NuGet
  package inventory.

- The FrontComposer inspect gate builds and inspects `UI.Contracts`; the Contracts
  package gate verifies the absence of UI runtime dependencies.
```

**Rationale:** This makes the architecture match FrontComposer's application projection-host
pattern and turns the low-dependency Contracts boundary into a testable invariant.

### 4.3 Sprint Tracking — Close Decision and Track Delivery

**OLD**

```yaml
  5-11-cross-surface-parity-responsive-design-accessibility-hardening: done
  5-12-live-apphost-operational-console-verification: review
  epic-5-retrospective: done
```

```yaml
  - epic: 5
    action: "Decide whether FrontComposer projection descriptors should remain in the packable Contracts assembly or move to a non-packable UI contracts boundary."
    owner: "Winston / Amelia"
    status: open
```

**NEW**

```yaml
  5-11-cross-surface-parity-responsive-design-accessibility-hardening: done
  5-12-live-apphost-operational-console-verification: review
  5-13-isolate-frontcomposer-projection-descriptors-from-packable-contracts: backlog
  epic-5-retrospective: done
```

```yaml
  - epic: 5
    action: "Decide whether FrontComposer projection descriptors should remain in the packable Contracts assembly or move to a non-packable UI contracts boundary."
    owner: "Winston / Amelia"
    status: done
    result: "Move application-specific projection descriptors to a non-packable Hexalith.Projects.UI.Contracts boundary; retain shared domain/wire vocabulary in the packable Contracts assembly."
    evidence: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-14-frontcomposer-contract-boundary.md"
```

Update `last_updated` to `2026-07-14`.

**Rationale:** The decision is complete, while implementation remains visible as backlog work.

### 4.4 Projection Catalog — Distinguish Descriptor Inputs

**OLD**

The catalog describes FrontComposer wrappers as being “in Contracts” and does not establish a
separate descriptor-input category.

**NEW**

Add before the first FrontComposer descriptor:

```markdown
## FrontComposer Descriptor Inputs

The following entries are non-persisted application presentation descriptors. Story 5.13 moves
their assembly ownership to the non-packable `Hexalith.Projects.UI.Contracts` projection host.
They consume reusable DTOs and shared semantic vocabulary from `Hexalith.Projects.Contracts`;
they are not part of the published Projects contract surface.
```

For each shell, inventory, detail, reference-health, resolution-trace, audit/export, warning-queue,
dashboard, and maintenance descriptor, add/update:

```markdown
- **Assembly:** `Hexalith.Projects.UI.Contracts` (`IsPackable=false`).
- **Owner:** Originally delivered by Story 5.x and migrated by Story 5.13 into the
  non-packable FrontComposer descriptor host. It is not a persisted runtime projection.
```

Preserve current contract versions and, initially, logical namespaces unless the compatibility
audit approves a separate namespace migration.

**Rationale:** Assembly ownership, rather than namespace, defines the package boundary. This keeps
the catalog accurate without coupling the correction to a broader naming change.

### 4.5 Durable Project Guidance — Prevent Regression

**OLD**

The project guidance defines FrontComposer generator discipline but not the consumer descriptor
assembly boundary.

**NEW**

```markdown
- Application-specific FrontComposer `[Projection]`/`[Command]` descriptors belong in a
  non-packable projection-host assembly. For Hexalith.Projects, that boundary is
  `Hexalith.Projects.UI.Contracts`.

- A packable Contracts assembly may retain reusable DTOs, shared semantic enums, and lightweight
  `[ProjectionBadge]` metadata, but it must not acquire `Hexalith.FrontComposer.Shell`, Fluxor,
  Fluent UI, or `Microsoft.AspNetCore.App`.

- Shared Web/MCP/CLI constants and lifecycle vocabulary stay in the packable Contracts assembly;
  MCP and CLI must not depend on the UI descriptor host for reusable protocol semantics.
```

**Rationale:** Future story creation and implementation will inherit the corrected boundary.

### 4.6 PRD and UX

No PRD or UX edits are proposed. Product scope, operator workflows, presentation semantics,
accessibility, tenant isolation, and Web/MCP/CLI parity remain unchanged.

## 5. Implementation Handoff

### Scope Classification

**Moderate** — architecture and backlog coordination followed by a bounded Developer implementation.

### Handoff Recipients

- **Architect (Winston):** confirm the final dependency graph and record the architecture boundary.
- **Product Owner:** prioritize Story 5.13 and approve any release/versioning consequence found by
  the consumer audit.
- **Developer (Amelia):** implement the project split, type separation, reference updates, generation
  gate, package gate, tests, and documentation.
- **Quality owner (Murat):** verify package cleanliness, descriptor discovery, generated-surface
  parity, accessibility regression coverage, and payload leakage.

### Implementation Sequence

1. Audit supported consumers of the descriptor types and record the versioning decision.
2. Add the non-packable `Hexalith.Projects.UI.Contracts` project to solution/build topology.
3. Split reusable maintenance action constants/enums from the presentation descriptor.
4. Move application-specific descriptors and the domain marker; preserve contract versions and
   logical namespaces unless the audit approves otherwise.
5. Update the UI host reference and assembly scanning.
6. Retarget FrontComposer inspection and descriptor tests.
7. Tighten the Contracts package-dependency gate and verify the new project is not packed.
8. Run build, focused tests, generation inspection, package inspection, parity, and leakage gates.
9. Update architecture, projection catalog, durable guidance, and sprint evidence.

### Success Criteria

- The produced `Hexalith.Projects.Contracts` nuspec contains no dependency on
  `Hexalith.FrontComposer.Shell`, Fluxor, Microsoft Fluent UI Blazor, or
  `Microsoft.AspNetCore.App`.
- `Hexalith.Projects.UI.Contracts` is non-packable and absent from package inventory.
- The new descriptor assembly references Contracts; Contracts does not reference the new assembly.
- The UI host discovers the same Projects FrontComposer domain and renders the same operational
  routes and metadata.
- MCP and CLI do not reference the UI descriptor assembly and retain their shared vocabulary.
- FrontComposer inspect completes with no warning against the new project.
- Descriptor metadata, parity, accessibility, tenant-isolation, and NoPayloadLeakage tests pass.
- A supported-consumer audit and package-versioning decision are recorded.
- Architecture, projection catalog, project context, and sprint tracking match the implemented
  boundary.

## 6. Checklist Status at Proposal Review

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story | [x] | Story 5.3 review; reaffirmed by Epic 5 retrospective and sprint action. |
| 1.2 Core problem | [x] | Technical limitation and assembly-responsibility conflict documented. |
| 1.3 Evidence | [x] | Project graph, package nuspec, gates, code consumers, and FrontComposer precedent inspected. |
| 2.1 Current epic | [x] | Epic 5 remains viable with one corrective story. |
| 2.2 Epic changes | [x] | Add Story 5.13; preserve existing Story 5.12 and completed functional stories. |
| 2.3 Remaining epics | [x] | No future planned epic requires adjustment. |
| 2.4 Obsolete/new epics | [x] | No obsolete epic and no new epic required. |
| 2.5 Order/priority | [x] | Complete Story 5.13 before package release readiness. |
| 3.1 PRD | [N/A] | MVP and product requirements are unaffected. |
| 3.2 Architecture | [x] | Component ownership and package invariants require correction. |
| 3.3 UI/UX | [N/A] | No behavior, flow, visual, responsive, or accessibility change. |
| 3.4 Other artifacts | [x] | Tests, generation gate, package gate, docs, solution, and sprint tracking affected. |
| 4.1 Direct adjustment | [x] | Viable; medium effort and medium risk. |
| 4.2 Rollback | [N/A] | Not viable; loses working surfaces without resolving the boundary. |
| 4.3 PRD MVP review | [N/A] | Not a product-scope problem. |
| 4.4 Recommended path | [x] | Direct Adjustment approved incrementally. |
| 5.1 Issue summary | [x] | Trigger, problem, and evidence included. |
| 5.2 Impact summary | [x] | Epic, artifacts, and technical effects included. |
| 5.3 Recommended path | [x] | Alternatives, rationale, effort, and risk included. |
| 5.4 MVP/action plan | [x] | No MVP impact; sequenced implementation plan included. |
| 5.5 Handoff | [x] | Architect, Product Owner, Developer, and Quality responsibilities defined. |
| 6.1 Checklist review | [x] | All applicable analysis items completed. |
| 6.2 Proposal accuracy | [x] | Approved incremental edits compiled consistently. |
| 6.3 Explicit final approval | [x] | Approved by Jerome on 2026-07-14. |
| 6.4 Sprint-status update | [x] | Story 5.13 added as backlog; decision action closed with this proposal as evidence. |
| 6.5 Next steps/handoff | [x] | Moderate change routed to Architect, Product Owner, Developer, and Quality owner. |

## Approval

**Status:** Approved by Jerome on 2026-07-14 for implementation and handoff.

## 7. Workflow Execution Log

- **Workflow:** BMad Correct Course.
- **Mode:** Incremental.
- **Analysis:** Trigger, epic impact, artifact impact, path selection, and handoff components were
  reviewed and approved section by section.
- **Detailed proposals:** Epic, architecture, sprint tracking, projection catalog, and durable
  project-guidance edits were each approved.
- **Final approval:** Jerome approved implementation on 2026-07-14.
- **Concurrent-state reconciliation:** An independently added Story 5.12 for live AppHost
  verification was preserved; this correction was assigned Story 5.13.
- **Artifacts modified by this workflow:** This approved Sprint Change Proposal and
  `_bmad-output/implementation-artifacts/sprint-status.yaml`.
- **Implementation routing:** Architect/Product Owner coordination, Developer implementation, and
  Quality verification as defined in Section 5.
