---
title: 'Preserve bound bootstrap when the SDK configures infinite HTTP timeout'
type: 'bugfix'
created: '2026-11-01'
status: 'in-review'
route: 'dispatch'
---

## Intent

The user authorizes fixes to all CI failures, latest-main submodule synchronization and release, without a human validation checkpoint. Nested submodules must remain deinitialized. Work in references/Hexalith.Tenants, read owning guidance and the workspace baseline, and never load a skill under references. Parent owns commits/push and the build workflow; implement only this scoped correction, preserve all other edits, and do not stage, commit, push or initialize dependencies.

## Evidence

Source-reference run41331746531 job124923135003 fails the actual source owner profile with bootstrap-command-timeout. Full logs /tmp/tenants-source-owner-failed.log show TenantBootstrapHostedService logs ArgumentOutOfRangeException during bound-administrator-validation at17:48:23.461. TenantBootstrapBoundCommand.CreateAsync rejects readinessTimeout <=0 while its production caller passes the SDK-managed HttpClient.Timeout, which can be Timeout.InfiniteTimeSpan under resilience handlers. The API accepts this legitimate timeout but the bootstrap never reaches catalog readiness or its owned receipt. Normal source identity, credentials, authority readiness and receipt gates must be preserved.

## Tasks & Acceptance

Inspect the real SDK HTTP defaults and confirm this cause. Correct the existing internal bound-bootstrap timeout handling to accept the legitimate infinite transport timeout while keeping catalog readiness bounded and caller cancellation effective; retain normal positive finite behavior and invalid-timeout refusal. Prefer the default HttpClient finite timeout (100seconds) for the readiness fallback, not an arbitrary long value. Do not disable source profile, relax authentication/catalog rules, edit CI gates, invent receipts or extend fixture timeouts to hide it. Add meaningful regression coverage in the existing bound-bootstrap tests for finite/infinite/invalid inputs and cancellation; exercise the production helper rather than merely mirroring arithmetic. Build Debug/source and run only relevant individual built xUnit classes; record exact evidence. No live topology or shared Docker changes without parent coordination; parent owns final hosted profile verification.

Do not edit the response-file compatibility scripts or CiQualityGateScriptTests; other parent work owns those files. Return changed files, verification and any remaining uncertainty. Leave this spec in-progress for parent completion.

## Review Triage Log

- Blind1 — medium, patch: the infinite-budget scenarios cannot distinguish the required 100-second fallback from an hour. The implementation agent is using the existing TimeProvider seam for a deterministic real CTS timer assertion and expiry while the caller remains uncancelled.
- Blind2 — low, defer: readiness polling responses and RouteCatalogUnavailableException precede this timeout correction; the existing loop handles them, but their sequence lacks focused regression coverage. Record this preexisting coverage gap without changing the production polling policy.
- Blind3 — low, patch: the newly added Builds cold-cache fixture constructs a lowercased but unnormalized cache path; normalize through the same SDK NuGet version semantics used by the writer.
- Blind4 — medium, patch: cache pruning can leave the nuspec while deleting the archive; the cold fixture must construct its own local metadata archive from the validated nuspec rather than require an unrelated retained archive.
- Blind5 — low, patch: accepting any positive count loses the existing completeness guard. Require at least the current twelve compatibility cases while allowing the suite to grow.
- Edge1 — low, patch: same legitimate-version normalization issue as Blind3, retained as its own row.
- Verification1 — medium, patch: pre-verified missing fallback-deadline regression; same timer correction and deterministic uncancelled-caller expiry test as Blind1.

## Implementation verification

Final Debug/source build passed with zero warnings/errors. The 21-case bound-command suite verifies finite and infinite readiness, exact 100-second timer creation and actual CTS callback cancellation with the caller active; prior 120 hosted-bootstrap cases also passed. Scoped formatter/diff checks passed. The compatibility wrapper's actual Release built test passed and requires at least 12 discovered Python cases; all 12 pass. No new packages/public API were introduced. The parent pushed 86244ede82095a0d07c8715dee537bd97cbef0e8 with the compatibility response-file repair and declared Builds/EventStore fixes, while keeping nested checkouts deinitialized. Fresh hosted main and source-profile CI are pending.

## Subsequent source-profile refusal

Hosted run41336228705 job124936526922 now passes source build, catalog commitment and bounded readiness, and dispatches the real owned bootstrap. It fails bootstrap-owned-receipt-invalid because the actual gateway returns400 rather than202. New full logs: /tmp/tenants-source-timer-profile-failed.log. Gateway SubmitCommandHandler logs CommandReceived for BootstrapGlobalAdmin, then the caller records400; no timeout/argument exception remains. Investigate and repair the demonstrated request/producer contract defect at its owner. Preserve real signed workload proof, bound principal, exact internal operation, catalog authority, receipt integrity and status/projected-membership gates. Do not weaken the fixture, accept400, change operation classification, suppress a test, or invent receipt results. Parent owns remote operations and final hosted rerun. No shared/local live backend mutation; use focused actual request/handler tests to establish the defect before changing code. If a support-safe diagnostic is required, report the exact gap rather than logging raw credentials/body/URLs. Keep the completed timer correction/tests intact.

## Response-diagnostic Review Triage Log

- Blind1 — medium, defer: SendAsync already used default full-response buffering before this change. The new8KiB guarantee bounds classification input, not transport allocation; changing completion semantics also requires a body-read timeout/cancellation policy. Record the preexisting transport-buffer gap separately rather than alter transport policy in a diagnostic patch.
- Blind2 — low, patch by explicit regression: buffered oversized409 bodies bypassed the old intended8KiB guard. The corrected bound deliberately treats such a response as unclassified/unexpected rather than scanning beyond the cap. Preserve the normal bounded409 marker rule and test oversized409 refusal; do not reintroduce an unbounded recognition path.
- Blind3 — low, patch: existing SDK authorization reason codes are useful known closed diagnostics; include the relevant emitted literals and exercise them without copying response fields.
- Blind4 — low, patch: existing command-endpoint problem categories currently collapse into other; add the relevant closed category literals with representative SDK-shaped tests.
- Blind5 — low, patch: the new canonical code fallback and reasonCode precedence lack coverage; add code-only and dual-field cases.
- Blind6 — low, patch: accepted/rejected boundary tests are absent; add exact8192/8193 UTF-8 byte cases including multibyte content, asserting the closed outputs.
- Blind7 — low, patch: malformed/oversized assertions do not check ResponseCode; assertnone/unrecognized as appropriate and include empty/whitespace bodies.
- Blind8 — low, patch: current operator guidance omits the closed diagnostic fields; document their fallback meanings and non-authoritative role without duplicating raw response content.
- Edge1 — medium, patch: actual JsonElement.GetString on an unpaired surrogate throws InvalidOperationException outside the JsonException catch, replacing event2003/status with event2002. Catch this malformed scalar case as an unclassified response and assert status-retaining privacy behavior.
- Verification1 — medium, patch: pre-verified missing current SDK503 idempotency-admission failure coverage; use its real type/reasonCode/code shape and assert event2003 category/code/status and protected-data exclusion.
- Verification-other1 — low, patch: real SDK domain rejection URIs append a reason segment, while the new mapping recognizes only the unused bare category. Recognize the emitted URI family with a closed literal output and test a suffixed response. The ordinary already-bootstrapped409 fast path remains unchanged.
