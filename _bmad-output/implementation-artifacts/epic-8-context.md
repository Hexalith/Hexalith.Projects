# Epic 8 Context: Operators Run Projects Safely and Release Owners Decide from Evidence

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give Tenant Operators and Tenant Project Administrators a trustworthy account of what happened to an admitted task, what they can safely inspect or recover, and how to tell when a dependency is unavailable. Give Release Owners reproducible evidence from the same authenticated, persisted paths that operators and downstream consumers will use.

## Stories

- Story 8.1: Inspect task, audit, and reconciliation truth
- Story 8.2: Create a bounded Safe Diagnostic Export
- Story 8.3: Integrate conformant Project maintenance in the Web console
- Story 8.4: Operate Projects through a deterministic CLI contract
- Story 8.5: Operate Projects through agent-safe MCP contracts
- Story 8.6: Observe truthful dependency and projection health
- Story 8.7: Consume reproducible packages and supply-chain evidence
- Story 8.8: Integrate authenticated isolation, privacy, parity, and accessibility evidence
- Story 8.9: Meet bounded performance and back-pressure objectives
- Story 8.10: Prove cross-workflow resilience
- Story 8.11: Record the terminal production-release decision

## Requirements & Constraints

- Scope every read to the current Tenant, actor and action. Default operator views expose Safe Metadata. Descriptive inspection requires separate authority and leaves a durable audit record. Use opaque or Tenant-salted identifiers in audit and diagnostics. Generalized denial must protect the existence of inaccessible targets.
- Preserve durable audit metadata for at least 365 days and for any longer applicable event-history obligation. Keep active tasks pollable until terminal. Keep terminal results and scoped idempotency records for at least 30 days or the result lifetime, whichever is longer. Resolution Traces and generated exports exist only for the request.
- Diagnostic export requires its own permission and an audited, synchronous, bounded snapshot. Enforce row, byte and concurrent-lease limits per contract. Preserve component unavailability and truncation in the result.
- Exercise authorization changes, cross-Tenant access, duplicate delivery, concurrent writers, restart, lost responses, and dependency failure and recovery through the supported platform. Prove persisted outcomes and recovery for affected Tenants alongside a healthy control. Size and latency measurements must cover the supported workload shapes.

## Technical Decisions

- Derive the operator timeline from committed domain and security facts. Put polls, retries and intermediate progress in telemetry. Read health from the dependencies a particular host requires and distinguish process liveness from readiness to serve.
- Platform adapters consume versioned Projects contracts and the same authorized read models. Classify each operation as a read, task control, or a confirmation-required action. Reconciliation applies only to the original admitted task, targets and actor authority. A change of intent starts with a new Preview.
- Keep the legacy route reversible through terminal acceptance. Bind verification results to the source, package or deployment revision actually exercised, runner and job, environment, command, hashed artifacts, and accountable owners. An unavailable environment leaves its critical case unverified.

## UX & Interaction Patterns

Show task state, safe reason, expiry and recovery codes appropriate to the role and lifecycle. Carry stable audit and receipt identifiers across Web and CLI. Use the same denial and recovery semantics across eligible surfaces. Public McpCli CLI and MCP accept eligible reads and task control and refuse confirmation or minting Selection Evidence. Human confirmation belongs to the registered confidential Web or Chatbot component for the action and role. Provide keyboard operation, visible focus, explicit status text and restrained announcements for task progress and recovery.

## Cross-Story Dependencies

Epic 8 consumes Epic 6 authenticated reads, Epic 7 durable workflows, and entry fixtures that exercise real persistence, identity and secrets on a qualified runtime. The 21 November sprint reconciliation leaves Story 6.7 in review, Story 7.1 blocked, and Epic 8 and Story 8.1 in backlog. The 22 November scheduling change allows Story 8.1 to enter development ahead of Epic 7 completion. Its existing acceptance gates still apply.

Stories 8.1–8.6 deliver internal operator capabilities when their own gates pass. The Web lifecycle and association journeys rely on accepted presentation and workflow packages. Cross-surface verification consumes independently accepted operator and Chatbot evidence. Release qualification also requires package provenance, scale, resilience and production-equivalent deployment, key-management and rollback results. Production deployment requires dated decisions by both Jerome and John on the complete candidate.
