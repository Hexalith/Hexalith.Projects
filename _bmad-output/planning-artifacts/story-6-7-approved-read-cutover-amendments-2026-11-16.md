---
title: 'Approved Story 6.7 read cutover amendments'
date: '2026-11-16'
status: 'approved'
approval_source: 'User: "approve", in response to the four recommendations in the active Story 6.7 bmad-build session'
approval_recorded_at_utc: '2026-11-16T15:16:26Z'
---

# Approved changes

The user approved all four recommendations after the explanation of operation,
benefits, costs and compatibility implications. This authorizes the bounded owner
and Projects changes below and renegotiates the corresponding frozen specification.
It does not accept a comparison case or authorize deployment, publication or Git work.

1. **Independent positions.** Use normalization revision
   `story67-read-normalization-2026-11-16-v2`. Compare legacy Sequence against its
   original source GlobalPosition and the corresponding supported global position.
   Independently assert supported ProjectVersion against original SequenceNumber.
   Keep exact values and raw evidence; 101 never becomes 2. Missing original source
   evidence fails the historical case. Do not rewrite history or change v1 semantics.
2. **Closed unavailable outcome pairs.** For stale Tenant evidence, accept legacy
   503 paired with supported context/setup/explain 200 Unavailable with no usable
   Setup, or supported advisory 200 empty/incomplete with null result. For unsupported
   File coverage or degraded advisory dependencies, accept legacy 503 paired with
   the explicit supported incomplete result. Assert authority, nondisclosure and
   every AD-32/coverage field independently; retain both statuses and reasons.
   Denied, absent, valid-empty and unavailable remain distinct. No generic error-to-
   empty mapping, ignored difference, timestamp tolerance or array reordering is approved.
3. **Native membership only.** Context/setup/explain name a Conversation. Reuse the
   existing Conversations owner read and platform transport; require current,
   authorized, same-Tenant native Conversation.ProjectId equal to the requested
   Project. Harden/enroll that existing owner use to enforce the original actor
   and delegated authority; a service identity or advisory/hydration metadata is
   never proof. Preserve existing authorized direct owner callers. Project, Folder,
   Setup and Tenant checks remain independent. Missing/unbound/unavailable proof
   yields no usable context. Session/opened-Project fallback is explicitly deferred;
   no new session store/token framework is authorized. Existing Project-only callers,
   including Web context inspection, must supply a Conversation or receive Unavailable
   for these context reads. Preserve existing supported one-string constructors and
   Deconstruct signatures, v1 REST/client signatures and old overloads; use additive
   request metadata/overloads for new input and update actual consumers/tests.
4. **Audited legacy descriptive inspection.** Retain real required names in v1
   list/open. Require explicit inspection opt-in, the existing descriptive-inspection
   permission, and successful durable metadata-only audit before disclosure. Reuse
   existing policy/audit seams. Calls without the required inspection authority fail
   safely. Preserve v1 method signatures and old overloads; update legitimate legacy
   consumers to opt in. Compare authorized descriptive reads with the corresponding
   supported inspection mode, and independently test supported ordinary Safe Metadata.
   Never normalize names to omission or fabricate names. This is an approved behavior
   change for legacy callers that previously obtained names using ordinary read permission.


# Implementation clarification of approved D4

The approved requirement that missing/unbound/unavailable native membership yields
no usable context is realized as one additional closed wire pair, restricted to
context/setup/explain: retained 503 without a setup/context payload versus supported
200 with AD-32 Unavailable, null Setup and no included references. Tests must prove
current Project read authority and the actual missing/unavailable native membership
condition independently, all first-response/nondisclosure fields, and unchanged
persisted domain state. This applies the already approved D4 behavior; it is an
implementation interpretation, not a separate user approval or a generic 503 rule.
Project/Tenant authorization denials keep their indistinguishable safe 404 behavior.
An empty successful ConversationStartSetup DTO must never stand in for unavailable
membership, and tests must not replace the unbound input with a different denied
identity merely to fit another mapping. Raw statuses and bodies remain retained.


# Additional explicit approval: required Folder still materializing

On 2026-11-16 the user answered "Approve this narrow Pending-Folder pair
(Recommended)" to the question describing the observed unsafe legacy Fresh/empty
setup and the proposed fail-closed repair. Normalization revision advances to
`story67-read-normalization-2026-11-16-v3`; prior v1/v2 receipts retain their original
meaning and are not reclassified.

Add exactly one closed cause for context/setup/explain: required Folder
materialization-in-progress, independently proven from original historical
ProjectFolderCreationPending evidence and the current Pending/folderless projection.
Retained 503 without usable context is compatible with supported 200/Unavailable
only when Setup is null, recovery includes PollTask, native Conversation/Project
binding and actor/Tenant authority are independently checked, quarantine remains,
and persisted source history/projections remain unchanged. Do not apply this cause
to arbitrary missing, denied, corrupt, rebuilding or unavailable Folder evidence.
The legacy safety repair must never return a successful empty/Fresh setup as usable
context. This approval does not accept any cell or weaken other source/position,
cursor, disclosure, audit or rollback checks.

# Additional explicit approval: retained v1 reference reason-code spelling

On 2026-11-22, in Story 6.7 code review pass 3, the user chose "snake_case on the
legacy path" (answer: "apply recommended"). Retained v1 open emits
`reference_unavailable` and `owner_read_unverified`, as its schema pattern requires.
Normalization revision advances to `story67-read-normalization-2026-11-22-v4`: v3 plus
exactly one closed mapping for the reference-summary `reasonCode` only,
`reference_unavailable` to `ReferenceUnavailable` and `owner_read_unverified` to
`OwnerReadUnverified`. No other value, field or operation is mapped. Prior v1-v3
receipts retain their original meaning and are not reclassified.


# Execution and evidence

Continue the existing implementation and finish actual authenticated seven-read
comparisons, history/identity/position/cursor/isolation/no-domain-write controls and
executable explicit rollback. All 77 required cells must be observed and pass their
applicable assertions; a structurally complete JSON or passing refusal unit tests
cannot replace executed equivalence evidence. Reject missing/skipped/unapproved cells.

Record the native-only scope correction in current documentation without rewriting
historical completed evidence. Keep all prior blocked packets and native XML immutable;
use a new receipt/runner attempt directory for this approved continuation. Capture the
actual current owner revisions and final source hashes; preserve the original baseline.

Feature acceptance uses the already approved individual Debug source/project-reference
lane. Distinguish source contract parity from unperformed package-mode adoption and
publication; retain the latter as release obligations rather than inventing a new
feature gate. Keep all readiness, Epic 7/8 and AD-25 pins unchanged; DW-63 stays open
for 7.1. No FrontComposer source/Git work or record synchronization is authorized.
