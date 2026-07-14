# Hexalith.Projects Codebase Audit Prompt

Use the prompt below with a coding agent that has read access to the complete
repository. It requests a read-only, evidence-based audit and an implementation
backlog; it does not authorize the agent to change production code.

## Prompt

```text
Act as the lead reviewer for a production-readiness and best-practices audit of
the Hexalith.Projects repository.

Repository root:
/home/administrator/projects/hexalith/projects

Mission:
Analyze the entire first-party codebase and identify every evidence-backed
change needed to:

1. comply with the repository's current mandatory architecture and engineering
   rules;
2. be safe, predictable, efficient, and easy to use from LLM chatbots and
   autonomous or human-supervised LLM agents;
3. meet current .NET, C#, Dapr, Aspire, MCP, OpenAPI, security, privacy,
   reliability, observability, performance, accessibility, testing, packaging,
   and maintainability best practices; and
4. preserve the intended Hexalith.Projects bounded context.

Produce the audit report and a prioritized, implementation-ready change
backlog. Do not modify application code, generated code, tests, configuration,
dependencies, submodule pointers, or documentation during this audit. If the
environment supports writing a report, save the final report as
`_bmad-output/analysis/hexalith-projects-codebase-audit-YYYY-MM-DD.md`; otherwise
return the complete report in the response. Creating that one report is the
only permitted repository write.

Definition of done:

- Every in-scope project, major workflow, trust boundary, public contract, and
  test lane has been inspected or explicitly recorded as not inspectable.
- Every finding is supported by repository evidence and, where applicable, an
  authoritative rule or external source.
- Every finding states the concrete change, affected locations, acceptance
  criteria, and verification required.
- Required compliance work is clearly separated from recommended optimization
  and optional polish.
- Repeated occurrences are enumerated, while one root-cause change is used to
  avoid duplicate backlog items.
- Unknowns and dynamically unverified behavior are labeled as unknown, not
  reported as facts.
- The report includes areas reviewed with no finding, so absence of a finding
  is distinguishable from absence of review.

Operating rules

1. Before doing anything else, read `AGENTS.md` and then read
   `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` completely. Read
   every topical instruction that it makes relevant to this audit, including
   the state, UX, and Git instructions. Follow them throughout the audit.
2. Treat the latest repository instructions as mandatory. Older PRDs,
   architecture documents, ADRs, story artifacts, generated output, and tests
   are evidence of intent or behavior, but they may be stale. Report conflicts
   rather than silently treating an older artifact as authoritative.
3. Respect repository boundaries. If a root-declared submodule is missing and
   is necessary for analysis, initialize only that exact `references/...` path
   declared by the root `.gitmodules`. Never use recursive submodule commands,
   never initialize nested submodules, and never update a submodule to another
   revision.
4. Preserve all user changes. Do not format, regenerate, restore, clean, or
   otherwise rewrite tracked files. Before using any Git command, follow the
   repository Git instructions. Do not stage, commit, branch, push, or change
   submodule pointers.
5. The audit is evidence-first. Search broadly, then inspect the implementation
   and tests behind each candidate issue. Do not infer a defect merely from a
   filename, TODO, code metric, package age, or preferred personal style.
6. Cite repository evidence as `relative/path:line` and name the relevant type,
   member, endpoint, tool, or configuration key. Cite the exact repository rule
   or current external standard that makes a change necessary.
7. When current external guidance is needed, use current primary or
   authoritative sources only: official .NET/C# documentation, Dapr docs,
   Aspire docs, the official MCP and OpenAPI specifications, OpenTelemetry
   specifications, OWASP publications, NIST publications, and W3C/WAI guidance.
   Include source links and access dates. Distinguish a mandatory specification
   requirement from advice. Do not propose a dependency upgrade only because a
   newer version exists; establish compatibility, support, security, and value.
8. Do not weaken warnings, analyzers, nullable checks, tenant isolation,
   authorization, persistence rules, tests, or build gates to make a result
   pass.
9. Use `.slnx`, never a legacy `.sln`. Restore/build the applicable solution in
   Release when feasible, but execute test projects individually as required by
   repository instructions. Record exact commands, results, and environmental
   blockers. Do not claim a runtime property from static inspection alone.
10. You may delegate independent read-only review lenses to subagents if that
    capability exists. The lead reviewer must reconcile disagreements,
    personally verify cited evidence, deduplicate findings, and own the final
    report.
11. Continue through the whole audit without asking broad preference questions.
    Make conservative, stated assumptions. Ask for input only when a missing
    choice would materially change the audit's scope or safety.

Product boundary that recommendations must preserve

- The primary consumer is a chatbot or LLM agent that starts, resumes, resolves,
  and maintains tenant-scoped project workspaces.
- Hexalith.Projects owns Project identity, setup, lifecycle, reference
  associations, resolution metadata, context-selection metadata, and
  metadata-only audit/diagnostic surfaces.
- Conversations owns transcripts; Folders owns folders/files and their access
  control; Memories owns memory payloads; Tenants owns tenant lifecycle and
  access truth; EventStore supplies domain persistence and domain-service
  plumbing; FrontComposer supplies module UI and MCP composition.
- Projects must use references and metadata. It must not absorb transcripts,
  file contents, memory payloads, secrets, raw prompts, unrestricted local
  paths, model-provider orchestration, generic retrieval, or another bounded
  context's authorization responsibility.
- Do not recommend generic project-management features such as tasks, boards,
  schedules, milestones, or resource planning; those are outside the v1
  bounded context.
- Project resolution and context assembly must remain tenant-scoped,
  authorization-filtered, deterministic, explainable, conservative, and
  fail-closed. Ambiguous matches require explicit user confirmation.
- Archived projects are retained for history but excluded from automatic active
  context selection unless explicitly requested.
- The service target documented in the PRD is p95 below 500 ms for listing,
  opening, resolution, and context retrieval when dependency metadata is
  available. Validate whether measurement and budgets exist; do not invent
  benchmark results.

Source-of-truth and scope inventory

Inspect at minimum:

- root instructions, `.editorconfig`, `global.json`, NuGet/MSBuild configuration,
  `.gitmodules`, `.slnx`, CI/release configuration, package metadata, and README;
- current PRD, architecture, ADRs, event/projection catalogs, payload taxonomy,
  context-assembly decision matrix, resolution scoring rules, runbooks, and
  cross-surface parity documentation;
- every first-party project under `src/`, including Contracts, domain,
  Infrastructure, Server, Workers, Client and its generator, MCP, CLI, UI,
  Testing, orchestration, and service-default projects;
- every current test project and E2E workspace under `tests/`;
- the pinned public surfaces of referenced Hexalith modules when needed to
  validate an integration assumption.

Exclude `bin/`, `obj/`, package caches, and archived test backups from code
quality counts. Still report repository-hygiene issues if generated/build output
is tracked or can affect tooling. Treat checked-in generated clients as derived
artifacts: review their public behavior and generation reproducibility, but put
fixes in their source schema/generator rather than hand-editing generated files.

Required review lenses

A. Requirements and source-of-truth consistency

- Build a traceability map from current requirements and repository rules to
  code and tests. Identify implemented, partial, missing, contradictory,
  obsolete, and undocumented behavior.
- Reconcile PRD, architecture, ADRs, OpenAPI, event/projection catalogs, MCP
  descriptors, generated client, CLI, UI, tests, and runtime wiring.
- Detect duplicate sources of truth, magic strings, schema drift, dead features,
  stale docs, misleading README/package metadata, and tests that preserve an
  obsolete architecture.
- Do not treat planning-artifact claims such as “complete” or “validated” as
  evidence that current code complies.

B. Hexalith architecture and package boundaries

- Validate the current domain-module rules in the repository instructions,
  especially use of `Hexalith.EventStore.DomainService`, the two-line host,
  domain-centric package contents, `IDomainQueryHandler`,
  `IDomainProjectionHandler`, `IReadModelStore` with `ReadModelWritePolicy`, and
  `IQueryCursorCodec` with `QueryCursorScope`.
- Locate custom AppHost, Aspire, ServiceDefaults, Dapr persistence, projection,
  query, actor, event-subscription, health, telemetry, or hosting plumbing that
  current rules assign to technical modules. For each occurrence, recommend a
  migration destination and sequence; do not merely say “refactor.”
- Validate dependency direction and ensure Contracts/domain packages remain
  independent of infrastructure, UI, MCP, CLI, generated-client, and hosting
  concerns. Detect cycles and inappropriate transitive dependencies.
- Check that common Hexalith capabilities are reused rather than duplicated in
  Projects. If the platform lacks a needed seam, identify the technical module
  that should receive it before Projects consumes it.
- Validate one C# type per `.cs` file, file-scoped namespaces, documentation,
  naming, nullability, immutability, public API shape, and current .NET 10/C# 14
  conventions without recommending style-only churn.

C. Domain model, CQRS, and event sourcing

- Trace every command through validation, pure aggregate handling, success or
  rejection events, persistence, replay, projections, queries, and returned
  status. Confirm state is reconstructed from events rather than directly
  mutated or independently persisted.
- Validate aggregate invariants for create/setup/folder/link/unlink/archive/
  restore/resolution-confirmation flows, including duplicates, no-ops,
  conflicting retries, archived state, replacement semantics, and maximum
  payload/cardinality limits.
- Check event compatibility, tolerant additive evolution, unknown-event
  handling, deterministic replay, projection rebuilds, ordering, duplicates,
  optimistic concurrency, atomicity, and persist-before-publish behavior.
- Verify expected domain failures use structured rejection outcomes while
  infrastructure failures remain exceptions/retry/dead-letter paths.
- Validate canonical EventStore identity and ULID rules for message,
  correlation, causation, aggregate, task, event, and idempotency identifiers.
  Enumerate inappropriate GUID validation or generation where a ULID or
  framework-owned identifier is required.
- Examine cross-bounded-context workflows, especially folder creation and
  reference validation, for durable sagas/state, retry safety, compensation,
  timeout behavior, and crash recovery. Flag process-local ledgers or state
  that becomes incorrect after restart or horizontal scaling.

D. LLM chatbot and agent readiness

Review the module as an API used by a fallible, retrying, sometimes autonomous
machine caller. Optimize for deterministic machine behavior, not prose that
only a human can interpret.

- Inventory the complete chatbot/agent job surface: create/open/list/update,
  resolve from conversation/attachments, confirm ambiguity, propose/create,
  link/unlink/replace references, archive/restore, get/refresh/explain context,
  and operator diagnostics. State which jobs are intentionally available via
  HTTP client, MCP tools/resources, CLI, or UI and whether omissions match the
  product boundary.
- Assess tool/resource names, descriptions, schemas, enums, required fields,
  defaults, nullability, bounds, examples, discoverability, and semantic
  precision. A model should be able to choose the correct operation without
  guessing or relying on undocumented magic strings.
- Verify read-only resources are separated from mutations. Mutations must have
  accurate read-only/destructive/idempotent semantics where the protocol
  supports them, bounded input, explicit intent, safe retries, and clear
  post-acceptance status retrieval.
- Review confirmation for ambiguous or consequential actions. A caller-supplied
  Boolean is not by itself proof of informed user consent. Determine whether
  previews/dry runs and confirmation artifacts are server-issued, opaque,
  expiring, single-use when appropriate, and bound to tenant, actor, action,
  target, normalized payload/hash, and current state. Prevent an agent from
  confirming a materially different action than the user reviewed.
- Verify idempotency is durable across retries, restarts, instances, timeouts,
  and “accepted but response lost” scenarios. Define behavior for the same key
  with the same request and the same key with a different request. Do not expose
  idempotency secrets in results or logs.
- Require stable structured success, rejection, error, progress, and eventual-
  consistency states with machine-readable reason codes, correlation/task IDs,
  retryability, retry-after guidance where relevant, and a way to poll or
  reconcile accepted commands. Avoid agents getting stuck in retry loops.
- Compare OpenAPI, server endpoints, generated client, idempotency helpers, MCP,
  CLI, UI, and docs for behavioral and vocabulary parity. Validate schema
  generation is deterministic and drift is blocked in CI.
- Evaluate call economy: prevent unnecessary round trips, N+1 metadata calls,
  unbounded result sets, over-fetching, and oversized tool schemas/results.
  Recommend batching, cursor pagination, conditional requests, compact
  projections, or workflow-shaped operations only when they preserve
  authorization, atomicity, and bounded-context ownership.
- Evaluate context economy: explicit budgets and cardinality limits, stable
  ordering and tie-breakers, deduplication, provenance, trust/freshness state,
  inclusion/exclusion reasons, and compact reference metadata. Projects should
  help the chatbot select context but must not fetch or store sibling-owned
  payloads or become a prompt builder.
- Treat all human-authored names, descriptions, setup guidance, labels, and all
  sibling-resource metadata as untrusted data. Check length/character bounds,
  output encoding, log forging, markup injection, control characters, Unicode
  ambiguity, and downstream contracts that could accidentally elevate data to
  system/developer instructions. Do not claim that sanitization alone solves
  prompt injection; preserve provenance and instruction/data separation.
- Review whether the module exposes enough trust metadata for the downstream
  orchestrator to distinguish authoritative policy, user preferences,
  untrusted referenced content, stale evidence, and unavailable evidence—while
  keeping model-provider prompt construction outside Projects.

E. Security, privacy, and abuse resistance

- Draw trust boundaries for chatbot/agent, browser/operator, MCP/FrontComposer,
  CLI, API, workers, Dapr sidecars, EventStore, Tenants, Conversations, Folders,
  and Memories.
- Trace identity and tenant scope end to end. Tenant and caller identity must be
  authenticated and server-derived, propagated safely, and enforced on command,
  query, projection, cache, state key, pub/sub, SignalR, diagnostic, export, and
  log paths. Test cross-tenant identifiers, forged claims, mixed-tenant batches,
  stale membership, and platform-operator access.
- Check command-side authorization and query/result filtering independently.
  Review service-to-service authorization and confused-deputy risks when
  Projects validates references with sibling modules or forwards caller tokens.
- Validate fail-closed behavior for timeout, dependency outage, unknown ACL,
  stale ACL, deleted/archive references, partial evidence, and time-of-check to
  time-of-use races.
- Review mass assignment, over-posting, identifier injection, path traversal,
  unrestricted file paths, SSRF-capable configuration/input, header injection,
  replay, CSRF where browser credentials apply, CORS, request smuggling
  assumptions, unsafe deserialization, denial of service, rate limiting, quotas,
  payload size, collection cardinality, and decompression/amplification risks.
- Validate least privilege and explicit authorization for every MCP tool,
  endpoint, Dapr component, state store, pub/sub topic, secret, container, and
  diagnostic/export surface.
- Enforce data minimization: no transcript, file content, memory content, raw
  prompt, token, secret, full command payload, unrestricted path, or sensitive
  user data in Projects state/events/projections/logs/traces/metrics/errors/
  audit/MCP/CLI/UI/snapshots/test fixtures. Check exception paths and source-
  generated logging too.
- Review retention, deletion/archive semantics, audit integrity, redaction,
  safe diagnostic export, encryption assumptions, secret management, package
  and CI supply-chain controls, dependency vulnerabilities, and reproducible
  builds.
- Threat-model prompt injection and tool abuse only at boundaries Projects owns:
  malicious setup metadata, poisoned labels, agent-generated identifiers,
  forged confirmation, tool-result injection, excessive retries, and attempts
  to use Projects as a payload exfiltration channel.

F. Reliability, concurrency, and distributed-systems behavior

- Analyze cancellation, deadlines, timeout budgets, bounded retries with
  jitter, circuit breaking, bulkheads, backpressure, rate limits, and propagation
  of dependency failures. Avoid stacked retry storms between client, API, Dapr,
  and workers.
- Check eventual-consistency contracts, projection readiness/watermarks,
  read-your-writes expectations, stale results, accepted-command lifecycle,
  recovery after crash, poison events, dead letters, replay, and zero-downtime
  version skew.
- Exercise duplicate, reordered, concurrent, and partially completed operations,
  including two agents acting on the same project or reference.
- Validate cache keys include every security and result-shaping dimension;
  invalidation does not leak stale cross-tenant data; and in-memory fallbacks are
  not mistaken for production durability.
- Review startup/config validation, health/readiness/liveness semantics,
  graceful shutdown, worker ownership/leases, clock assumptions, and UTC/
  `DateTimeOffset` usage.

G. Performance and resource efficiency

- Establish evidence before recommending optimization. Inspect algorithmic
  complexity, allocation hot paths, repeated serialization, reflection, LINQ on
  hot paths, large object creation, synchronous blocking, thread-pool risks,
  connection reuse, Dapr/service calls, projection scans, and read-model access.
- Detect unbounded list/context/audit queries and N+1 or serial independent
  dependency calls. Consider bounded parallelism, batching, cursor pagination,
  caching, ETags/conditional reads, precomputed projections, streaming, and
  pooled/source-generated serialization where measurements justify them.
- Verify cancellation tokens and async calls flow end to end. Do not recommend
  `Task.Run`, parallelism, caching, or pooling without checking ordering,
  tenant-isolation, consistency, and lifecycle implications.
- Review startup size, package/transitive dependency weight, container behavior,
  non-root execution, trimming/AOT claims if any, and generated-client overhead.
- Locate load tests, benchmarks, p95 measurement, service-level indicators, and
  regression thresholds for the documented 500 ms target. Propose a repeatable
  benchmark/load-test design for gaps; never fabricate performance numbers.

H. API and contract quality

- Validate HTTP method/status semantics, route consistency, RFC 9457
  ProblemDetails, content types, cancellation, pagination, filtering, sorting,
  conditional requests, rate-limit metadata, and versioning/deprecation.
- Validate OpenAPI completeness and correctness: operation IDs, schemas,
  required/nullable/default semantics, enums, examples, formats, bounds,
  ProblemDetails, auth, idempotency/correlation headers, 202 workflows, and all
  actual response codes.
- Check contracts for serialization tolerance, deterministic JSON, enum/version
  evolution, unknown fields/values, temporal formats, ULIDs, validation parity,
  and no sibling-owned payload fields.
- Check generated-client reproducibility, disposal/HttpClientFactory usage,
  timeouts, cancellation, error mapping, idempotency hashing/canonicalization,
  sensitive-data handling, and compatibility tests.
- Validate MCP protocol URIs, schemas, manifest versioning, authorization
  policies, resource pagination/filtering, tool annotations if supported, safe
  structured content, and protocol-level error behavior against the pinned MCP
  integration and current official MCP specification.

I. Observability and operations

- Trace W3C/OpenTelemetry context across HTTP, Dapr, events, workers, sibling
  clients, projections, MCP, and CLI. Check correlation, causation, message/task,
  project, tenant, and actor metadata without leaking payloads or high-cardinality
  sensitive values.
- Require source-generated structured logging where mandated, consistent event
  IDs/levels, safe exceptions, useful metrics, traces around dependency calls,
  and actionable but payload-free diagnostics.
- Check SLIs/SLOs, dashboards, alertable failure modes, projection lag,
  dead-letter/retry visibility, safe diagnostic export, runbooks, and operator
  remediation paths.
- Verify health endpoints represent their intended semantics and do not leak
  topology/configuration details.

J. UI, CLI, and operator experience

- For Blazor UI, apply the repository UX rules: FrontComposer and Fluent UI v5,
  component reuse, current Fluent 2 tokens, accordion rules, responsive behavior,
  localization, keyboard/focus behavior, WCAG 2.2 AA, reduced motion, zoom,
  screen-reader names, validation, loading/empty/error/stale states, and safe
  rendering of untrusted metadata.
- Review operator confirmation and maintenance previews for clarity, current-
  state binding, reversibility, stale state, and prevention of accidental or
  agent-driven destructive actions.
- Ensure CLI output and exit codes are deterministic, script-friendly, safely
  redacted, and semantically aligned with API/MCP/UI behavior. Human prose may
  supplement but must not replace structured/machine-detectable states.

K. Tests, CI/CD, packaging, and developer experience

- Map every requirement, trust boundary, command, event, projection, endpoint,
  MCP resource/tool, generated client operation, CLI command, and UI workflow to
  test evidence. Identify assertion-free, implementation-coupled, duplicate,
  flaky, skipped, quarantined, or obsolete tests.
- Require focused pure tests for aggregate Handle/Apply, validators, scoring,
  context decisions, mappers, projections, and serialization. Include boundary,
  property-based/fuzz, malicious-input, concurrency, replay, duplicate,
  cancellation, and negative authorization tests where valuable.
- Integration tests must validate actual persisted state-store end-state and
  relevant emitted/read-model outcomes, not only status codes, mocks, or method
  calls. Validate Dapr/EventStore integration, restart durability, and tenant
  isolation at real boundaries.
- Check OpenAPI fingerprints, generated-client drift, contract compatibility,
  event replay, projection rebuild, MCP schema/authorization, CLI parity,
  accessibility, E2E, load, resilience, and privacy/no-payload-leakage gates.
- Verify `.slnx` and CI lanes include or intentionally invoke every required test
  project, including integration and E2E lanes. Confirm local and CI dependency
  modes follow current Hexalith instructions.
- Review analyzers, warning policy, formatting, central package management,
  deterministic builds, NuGet audit, secret scanning, dependency review, SBOM/
  provenance where appropriate, container publishing, semantic release,
  Conventional Commits, package contents, XML docs, README/onboarding, and
  reproducible generation.
- Run safe applicable gates. If a broad gate is blocked, follow the repository's
  focused validation ladder and report broad blockers separately from focused
  evidence.

Mandatory end-to-end workflow traces

For each workflow below, trace request/tool input -> authentication -> tenant
derivation -> validation -> authorization/ACL evidence -> application/domain
handling -> event persistence -> projection/read model -> response/status ->
logs/traces/tests. Record missing links and unsafe assumptions.

1. Create a project, including optional project-folder creation.
2. Open/list a project and retrieve conversation-start setup.
3. Resolve from a conversation.
4. Resolve from attachments.
5. Handle multiple candidates and bind explicit confirmation.
6. Handle no match, propose a project, and confirm creation.
7. Get, explain, and refresh project context with authorized, unauthorized,
   stale, unavailable, archived, deleted, and mixed evidence.
8. Link, replace, move, and unlink conversation/folder/file/memory references.
9. Archive, restore, relink, unlink, and reevaluate through every exposed API,
   generated client, MCP, CLI, and UI path.
10. Read operator inventory/detail/reference health/resolution trace/audit/
    warning/dashboard/safe-export surfaces.
11. Repeat accepted requests after timeout/lost response, restart the service,
    rebuild projections, replay events, and run two concurrent callers.

Audit method

Phase 1 — Establish the baseline

- Record repository structure, project/reference graph, public surfaces,
  generated artifacts, submodule state needed for analysis, target SDK/package
  versions, and relevant instruction versions.
- Record working assumptions and the precedence used for conflicting documents.
- Run or inspect restore/build/test/CI gates as safely feasible. Keep exact
  command/output summaries.

Phase 2 — Static and contract analysis

- Search the entire in-scope tree for each review lens, then inspect the full
  implementation context and corresponding tests before creating a finding.
- Build architecture, data-flow, trust-boundary, event/projection, endpoint,
  MCP, client, and dependency maps.
- Compare code, contracts, schemas, docs, and tests mechanically where possible.

Phase 3 — Focused dynamic verification

- Run focused unit/contract/integration/E2E checks that can safely validate
  candidate findings. Use test projects individually.
- Exercise runtime/Aspire topology only if it can be done safely and adds
  material evidence. Do not mutate external data or use production credentials.
- Use static-analysis, package-audit, coverage, or benchmark tools only when
  already available or safely runnable. Record tool/version/config and avoid
  treating heuristic output as a confirmed defect.

Phase 4 — Synthesis and adversarial challenge

- Deduplicate findings by root cause and enumerate all verified occurrences.
- Challenge each finding: Is the rule current? Is the evidence sufficient? Is
  the recommendation compatible with EventStore, Dapr, tenant isolation, the
  product boundary, and existing public contracts? Could it introduce a new
  security, consistency, compatibility, or latency defect?
- Perform a final omission pass against every review lens, project, workflow,
  and public surface.

Finding standard

Assign each finding:

- ID: stable category prefix plus number, such as `ARCH-001`, `SEC-004`, or
  `AGENT-003`.
- Priority:
  - P0: credible cross-tenant exposure, secret/payload disclosure, destructive
    autonomous action, integrity/data-loss path, or immediately exploitable
    critical vulnerability.
  - P1: mandatory architecture/security/privacy/reliability violation, broken
    public workflow, non-durable correctness, or likely production incident.
  - P2: meaningful performance, operability, compatibility, test, or
    maintainability gap with bounded impact.
  - P3: low-risk optimization, documentation, consistency, or developer-
    experience improvement.
- Obligation: `Required compliance`, `Required correctness/security`,
  `Recommended optimization`, or `Optional polish`.
- Confidence: High, Medium, or Low, with the reason for anything below High.
- Effort: XS, S, M, L, or XL, and whether a public-contract/data migration is
  involved.

Every detailed finding must contain:

1. concise title and outcome/impact;
2. verified current behavior;
3. evidence with all affected `path:line` occurrences or an attached occurrence
   list;
4. violated repository rule, requirement, specification, or measured target;
5. why this matters specifically for chatbot/agent callers where relevant;
6. concrete recommended design/change, including the correct owning package;
7. compatibility, rollout, event/schema/data migration, and rollback concerns;
8. implementation dependencies and sequencing;
9. acceptance criteria in testable language;
10. exact tests and operational evidence needed to close it;
11. priority, obligation, effort, risk, and confidence; and
12. alternatives considered when the recommendation has a meaningful tradeoff.

Do not write vague findings such as “improve security,” “add validation,”
“optimize performance,” “increase coverage,” or “refactor architecture.” Name
the precise boundary, data, operation, limit, ownership move, expected behavior,
and verification.

Required report structure

1. Executive verdict
   - readiness for production chatbot/agent use;
   - counts by priority and obligation;
   - top risks and top high-leverage changes;
   - explicit statement if no P0/P1 finding was verified.
2. Scope, assumptions, source precedence, and evidence limitations.
3. Repository/system map
   - project and dependency map;
   - data-flow/trust-boundary diagram;
   - command/event/projection map;
   - HTTP/client/MCP/CLI/UI surface map.
4. Baseline evidence
   - commands run and concise results;
   - build/test/static/package/runtime blockers;
   - current test-lane inventory.
5. Compliance and coverage matrix
   - one row per mandatory rule/review lens/public workflow;
   - status: Pass, Partial, Fail, Unknown, or Not applicable;
   - evidence and related finding IDs.
6. Prioritized findings summary table.
7. Detailed findings using the finding standard above.
8. LLM/agent contract scorecard
   - discoverability;
   - deterministic behavior;
   - confirmation and least agency;
   - idempotency/recovery;
   - structured errors/status;
   - context/token economy;
   - provenance/trust/freshness;
   - prompt-injection/tool-abuse boundary;
   - HTTP/MCP/client/CLI/UI parity.
9. Security/privacy threat model and negative-test gaps.
10. Performance/reliability/observability assessment, separating measured facts
    from hypotheses needing benchmarks or runtime proof.
11. Test and CI gap matrix.
12. Phased implementation roadmap
    - Phase 0: contain P0 risks;
    - Phase 1: mandatory architecture/security/correctness compliance;
    - Phase 2: agent contract, reliability, and test hardening;
    - Phase 3: measured performance and developer-experience improvements;
    - dependencies, safe migration order, parallelizable groups, and exit gates.
13. Quick wins (only items truly XS/S and low risk).
14. Reviewed with no finding, false positives rejected, unknowns, and deferred
    decisions.
15. Machine-readable backlog appendix in YAML with this shape:

    findings:
      - id: ARCH-001
        title: "..."
        priority: P1
        obligation: required_compliance
        category: architecture
        confidence: high
        effort: L
        affected_files:
          - path: "src/..."
            lines: [1, 2]
            symbol: "Type.Member"
        current_behavior: "..."
        required_change: "..."
        acceptance_criteria:
          - "..."
        verification:
          - "..."
        dependencies: []
        breaking_change: false
        migration_required: false
        source_rules:
          - "repository instruction or authoritative URL"

Final quality bar

The report is a decision and implementation aid, not a list of opinions. Be
exhaustive within the declared scope, concise where the code complies, and
detailed where change is needed. Prefer a smaller number of proven root-cause
findings with complete occurrence lists over a large number of speculative or
duplicated comments. Do not implement any finding until a separate request
explicitly authorizes changes.
```
