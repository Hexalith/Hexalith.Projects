---
title: 'Repair FrontComposer session credential isolation'
type: 'bugfix'
created: '2026-11-05'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — authorized in the 2026-11-05 request">

## Intent

Repair the reproduced FrontComposer credential substitution blocking DW-1. Bind owner-managed relayed credentials to the authenticated sign-in session/circuit, stable subject and configured current Tenant. Extend existing authentication and relay infrastructure, with concurrent same-subject/different-Tenant and same-Tenant sessions, distinct users, missing/expired credentials, sign-out and captured SignalR providers covered by focused Debug/source tests.

Preserve existing Projects readiness edits. Retain exact commands/results/source references, update DW-1 and Story 6.5 prerequisites, close DW-1 only with usable prerequisite evidence, then assess readiness. Do not implement Story 6.5, commit, push, change branches, update dependencies or create Projects token plumbing.

</frozen-after-approval>

## Implementation Notes

Investigation: existing OIDC capture, singleton token store, gateway handler, EventStore access-token provider and sign-out endpoint are the complete repair boundary. Reuse FrontComposerClaimExtractor for configured current-Tenant validation and CircuitServicesAccessor for active circuit identity. Generate a fresh opaque local session claim on each sign-in; never trust an incoming session claim. Both gateway and captured providers must require the exact binding; captured providers must recheck revocation/expiry without ambient fallback. Existing non-circuit HTTP authentication remains an owner seam.

No intent gaps or irreversible actions. This is a bounded repair to existing source/test files plus an owner binding type and readiness evidence. Current dirty documentation and main branch are explicitly authorized by the user. No Story 6.5 workflow status synchronization applies to this separate repair.

The owner repair now uses final-ticket capture on the selected OIDC scheme, circuit-first authority shared with IUserContextAccessor, strict configured Tenant validation and opaque stable subjects. Sign-out revokes by local sign-in ID even after Tenant changes; captured SignalR callbacks recheck original binding/state/expiry/revocation. Expired abandoned sessions are purged on writes and expired reads. Real protected-cookie round-trip and reconnect integration tests pass.

Owner Debug build passes with zero warnings/errors and the focused native suite passes 114/114, no failures/errors/skips/not-run. Existing Projects UI source builds successfully. Its old subject-only authentication test fixture requires a prepared, separately permissioned synthetic migration; the test-project failure is retained rather than bypassed. DW-1 closes for the verified source checkout; 6.5 stays blocked for documented planning/consumer-test readiness. See evidence/story-6-5-prerequisite-readiness-20261105/repair/README.md.

## Review Triage Log

- high, patched: tuple-dependent sign-out left old captured sessions valid after Tenant changes; revocation now uses the generated sign-in ID and a changed-Tenant sign-out test proves invalidation.
- medium, patched: unique abandoned sessions retained expired bearer strings; expiry sweep on write and retained-entry test cover cleanup without revisiting the key.
- high, patched: TokenValidated preceded final claim mapping; capture now follows prior final-ticket callbacks, with a late-Tenant test. Framework lifecycle references are retained in repair evidence.
- high, patched: credentials and IUserContextAccessor disagreed under a stale request; both now prefer the active circuit and concurrent tests assert both authorities.
- high, patched: hardcoded default OIDC hook missed custom schemes; named configuration follows the selected challenge scheme and its test passes.
- medium, patched: Tenant identifier restrictions rejected opaque colon-bearing subjects; stable-subject validation is separate and an email-alias compatibility test passes.
- medium verification gap, patched: synthetic principals alone did not prove claim persistence; a real protected-cookie sign-in/authenticate/sign-out round trip now verifies two same-subject/same-Tenant sessions.

All seven findings independently rechecked as addressed, with no material flaw or deferred finding. User instructions override the skill's automatic commit/PR suggestion; no Git mutation or dependency update was performed.
