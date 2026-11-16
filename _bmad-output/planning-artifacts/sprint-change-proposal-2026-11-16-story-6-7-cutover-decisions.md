---
title: 'Story 6.7 transport, comparison and SDK-host migration decisions'
date: '2026-11-16'
status: 'approved'
approval_source: 'Jerome: "apply recommended", after the detailed options discussion in the Story 6.7 bmad-build session'
baseline_commit: 'c6541a305a420078b16dceaba47f36468022850b'
change_scope: 'Planning and specification only; no product implementation or status promotion'
---

# Approved Story 6.7 decisions

The user approved the three recommendations after considering their operation,
benefits and costs. This record resolves the draft's intent questions; the complete
revised specification approval is recorded below.

## Transport

Use the existing EventStore Gateway for typed supported consumption and switch
primary Web entry routing through one explicit configuration choice, applied by
restart/redeployment. Preserve v1 REST and existing generated-client signatures.
Maintain an inventory of consumers actually migrated; local Web selection does
not establish external Chatbot adoption. Implement only the missing typed export
needed by these reads in EventStore tooling. A substantial generator redesign
becomes a separate prerequisite; Projects must not introduce a replacement
generator. No new public REST version is selected.

## Comparison and rollback

Approve `story67-read-normalization-2026-11-16-v1` as a finite comparison contract:

- Ignore JSON object-property order; map lifecycle spelling to the same enum.
- Map legacy folder fields to the identical authorized supported reference.
- Map Sequence to ProjectVersion only when both identify the same source sequence;
  continue comparing exact global positions independently.
- Map ObservedAt to AsOf only when both describe the same observation.
- Map null Setup to empty only when independent current evidence proves a valid
  empty Setup. Missing or unavailable evidence remains distinct.
- Map physical keys to logical AggregateIdentity while retaining raw keys,
  missing-record and collision checks in evidence.
- Compare independently decoded cursor boundary and scope, with actual
  continuation, expiry and scope-rejection tests.

Do not accept timestamp tolerance, array reordering, discarded differences or
inferred authority. Assert supported-only snapshots/components against independent
evidence. Known stale-Tenant admission, Conversation-binding and advisory safety
differences require repair or an explicitly approved behavioral change.

Approval establishes the comparison rules, not passing equivalence evidence.
An operator explicitly selects rollback only to a security-verified legacy path;
otherwise the affected read remains unavailable. Supported errors do not trigger
per-request fallback to legacy.

## DW-63 ownership and gate

DW-63 remains **open**, with Projects Owner responsible for integration and
EventStore Owner responsible for SDK capability. Move its gate from Story 6.7 to
Story 7.1, where whole-host integration accompanies the first Epic 7 writer slice.
This is a sequencing decision: SDK-host migration does not inherently replace a
writer, but DW-63 spans `/process`, `/project` and `/query` and needs evidence for
all three families.

Before Story 7.1 writer cutover, require SDK host registration/route/catalog,
authorization, projection dispatch and supported-read compatibility proof;
AD-17 historical replay, command equivalence, single-writer fencing and executable
rollback gates also apply. The move does not close DW-63 or authorize Epic 7
implementation. Keep the current query composition for Story 6.7.

Apply the gate assignment to the deferred-work ledger, Epic 7 and Story 7.1
entry text, and a sprint-status comment. Do not create another work-package ID,
change guard constants, or promote any development/action/readiness status.
Historical records remain unchanged; AD-25 and Epic 8 release gates still apply.

## Validation

Validate the production-authority index and Story 6.7, run the existing guard
tests using `run-with-temp`, parse the edited YAML/frontmatter, and verify the
development/action statuses are unchanged. Check both tracked and newly created
documents for whitespace and preserve each existing file's line endings.

Applied validation: Story 6.7 authority and active-index checks passed; the existing
production-authority suite passed all 50 tests. YAML/frontmatter parsed, the sprint
YAML data remains identical to HEAD, DW-63 remains open under the 7.1 gate, and
whitespace checks passed. Existing line endings were preserved, including the
ledger's mixed endings. Jerome approved the revised spec at Checkpoint 1 on 2026-11-16 with "approve and stop"; its status is ready-for-dev and implementation has not started.
