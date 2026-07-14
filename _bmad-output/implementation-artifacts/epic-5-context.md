# Epic 5 Context: Operational Console & Audit (CLI / MCP / Web)

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver one metadata-only operational product across Web, MCP, and CLI. Authorized administrators, operators, support users, and MCP-assisted agents must be able to inspect project state, reference health, resolution evidence, warnings, and audit history and perform tightly controlled maintenance while all three surfaces expose equivalent facts, preserve tenant isolation, and fail closed when evidence is unavailable.

## Stories

- Story 5.1: Audit timeline projection & metadata-only audit events
- Story 5.2: Operator read access
- Story 5.3: FrontComposer console shell & shared rendering
- Story 5.4: Project inventory & detail views
- Story 5.5: Reference inventory & health view
- Story 5.6: Resolution Trace Workbench
- Story 5.7: Audit timeline view & Safe Diagnostic Export
- Story 5.8: Warnings queue & operational dashboard
- Story 5.9: Audit-first maintenance actions
- Story 5.10: MCP & CLI parity surfaces
- Story 5.11: Cross-surface parity, responsive design & accessibility hardening

## Requirements & Constraints

- Operational reads and exports expose safe metadata only: tenant and project identifiers, lifecycle state, reference identifiers and health, reason codes, timestamps, warnings, correlation and audit identifiers. They never expose transcripts, file or memory contents, prompts, setup content, tokens, secrets, or sibling payloads.
- Every query and mutation is tenant-scoped and authorization-gated. Unauthorized, cross-tenant, stale, missing, malformed, or unverifiable evidence fails closed without revealing resource existence.
- Web, MCP, and CLI use one shared vocabulary and expose equivalent state names, reasons, timestamps, warnings, and audit identifiers. Machine-readable schemas and stable automation identifiers make parity testable.
- Mutating maintenance flows are previewed, validated, dry-run where required, explicitly confirmed, and audited. Command-async behavior distinguishes acknowledgement from eventual confirmation or rejection.
- Accessibility targets WCAG 2.2 AA: keyboard operation, visible focus, semantic landmarks, tables and timelines, dialog focus containment, non-color-only state, sufficient contrast, reduced-motion safety, and no hover-only critical actions.
- Responsive layouts preserve identity, tenant, lifecycle, warnings, reason codes, consequences, and accessible full identifier values from mobile through wide desktop viewports.
- Automated evidence must include cross-tenant negative tests and payload-leakage checks for every new surface and DTO.

## Technical Decisions

- FrontComposer-generated contracts and rendering are the default Web path. Custom views use the lowest sufficient customization level and retain generated lifecycle, authorization, telemetry, and accessibility behavior.
- The operational console is hosted as `projects-ui` in the Aspire AppHost topology alongside Projects API/workers and shared security dependencies. Service endpoints are assigned by Aspire and must be discovered rather than hard-coded.
- Dapr remains the infrastructure boundary for state, pub/sub, actors, and service invocation. UI, CLI, MCP, Contracts, and domain code do not access infrastructure stores directly.
- REST queries use the existing tenant-safe server endpoints; writes use the EventStore command pipeline and return accepted-command evidence before projection confirmation.
- Playwright with axe-core is the live browser verification mechanism and requires Node.js 24 or newer. Pure selector/contract checks must remain runnable without the AppHost, while live cases use a separately explicit topology gate.

## UX & Interaction Patterns

- Every screen uses a common diagnostic header, state/reason badges with accessible text, explicit empty/denied/unavailable/filter states, and distinct loading/success/warning/error/fail-closed feedback.
- Inventory, detail, reference health, resolution trace, audit timeline, warnings queue, dashboard, and maintenance panels share stable identifiers and terminology across surfaces.
- Long identifiers, timestamps, and safe diagnostic metadata remain copyable and available to assistive technology; critical information is never communicated by color alone.

## Cross-Story Dependencies

- Stories 5.3–5.11 consume the metadata-only audit projection and operator-read authorization established by Stories 5.1–5.2.
- Maintenance actions depend on the existing project lifecycle, reference, resolution, and EventStore command flows from earlier epics.
- Web parity depends on a runnable `projects-ui` Aspire resource and authenticated Projects topology; MCP and CLI parity depend on the same server contracts and safe-denial semantics.
- Story 5.11 supplies the cross-surface, responsive, accessibility, tenant-isolation, and leakage acceptance boundary for the completed Epic 5 feature set.
