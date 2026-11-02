---
title: 'Align the Tenants gateway invocation budget with native authority verification'
type: 'fix'
created: '2026-11-01'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '6b034a2d3b70533024343e721c47ffdfd1830a10'
context: []
---

<frozen-after-approval reason="user authorized demonstrated CI repairs and release without human validation">

## Intent

Repair actual source profile41362685140: bootstrap and current native authority now pass, but CreateTenant failsHTTP500 because its gateway domain invocation inherits a5second SDK default that cancels the required final native head recheck. Explicitly configure the Tenants owning gateway with a bounded default30seconds, preserving operator overrides. The existing authority reader permits10seconds and subsequent independent metadata observation2seconds; outer transport still has its existing bounds.

## Boundaries & Constraints

Change only the Tenants owning AppHost configuration and its meaningful composition regression/documentation if needed. Do not change the shared SDK5second default, native proof/final recheck/authorization rules, reader budgets, browser waits, source-profile deadlines or acceptance assertions. Do not relax endpoint binding or clear retained leases. Do not initialize nested checkouts or start a local topology/backend. Preserve all existing work. Parent commits/pushes and qualifies hosted exact sources. Existing parent bmad-build step03 handoff: do not invoke/render/orchestrate it again.

</frozen-after-approval>

## Code Map and proven evidence

- Tenants src/Hexalith.Tenants.AppHost/Program.cs70-90 composes its EventStore gateway and explicit tenant/global-administrator registrations. Existing queue-limit override pattern preserves builder.Configuration before a local default; use the same pattern for EventStore:DomainServices:InvocationTimeoutSeconds / EventStore__DomainServices__InvocationTimeoutSeconds.
- Tenants tests/Hexalith.Tenants.AppHost.Tests/AppHostSecurityResourceGraphTests.cs uses actual resource composition without launching a topology. Inspect existing fixtures and assert the default leaves room for existing10s+2s authority work, while an explicit operator value retains precedence. Avoid a test that merely mirrors a literal without checking composition/override behavior.
- Existing EventStore DomainServices/DaprDomainServiceInvoker.cs149-154 owns a TimeProvider-backed configured timeout and linked caller token; existing focused tests cover configured expiry and cancellation. Reuse those checks as appropriate; no SDK code changes required.
- Actual raw log /tmp/projects-release-maintenance/tenants-6b03-source-failed.log: invocation starts23:46:19.858; first native HTTP read20.344→21.815(200); second21.838→23.756(200); third23.761 has no response. InvokerEvent3004 at24.863 reports ConfiguredTimeoutElapsed=true, TimeoutSeconds=5. Third receiver cancels24.864; actor DomainServiceException24.930; outer actor RPC finishes24.957 with caller cancellation false. The deterministic authoritative head→event-page→final-head sequence identifies this as the final head recheck, before separate metadata observation. No native proof corruption/guard refusal/CAS exhaustion occurs on this command trace.
- Earlier API query catalog CAS32attempt exhaustion is a separate transient observation, not proven cause of this command. Do not expand into catalog algorithm changes.
- Local Dapr policy has app targets tenants-api5s and eventstore30s, no tenants target; the actual domain call5s cancellation is the SDK caller timer. Do not alter Dapr policy speculatively.

## Tasks & Acceptance

- [x] Configure the owning gateway bounded default30seconds with explicit operator override precedence.
- [x] Verify actual resource composition/default versus override; retain existing configured-timeout and caller-cancellation behavior. Keep test/resource startup offline.
- [x] Run the narrowest owning serial Debug/source build and affected tests, report exact commands/counts/logs/changed files, then freeze for parent review. Do not commit/push.

Given the normal Tenants host configuration, the gateway supplies a budget accommodating its existing bounded native authority operations. Given an explicit operator value, composition preserves it. Given configured expiry or caller cancellation, the unchanged invoker still cancels the actual call. Hosted source qualification remains required; local composition alone does not establish runtime success.

## Verification

The source AppHost.Tests build passes with zero warnings/errors. Three offline production publish-model cases pass without skips: default30, explicit5 and explicit45. The model mode is asserted; no topology is started. The budget registration is outside run/publish guards. Initial run-model attempts correctly refused missing retained source-catalog custody; no guard or custody was changed. The final tests use the existing publish-model pattern, and do not claim hosted runtime verification. Five existing SDK controls pass: default, configured invocation expiry, registration expiry, early pipeline cancellation and caller cancellation. Shared SDK code is unchanged. Exact logs: /tmp/tenant-domain-invocation-budget-final-build.log, /tmp/tenant-domain-invocation-budget-final-composition-tests.log, /tmp/tenant-domain-invocation-budget-invoker-controls.log. Hosted exact-source qualification remains pending.

## Review Triage Log

- Blind1 — medium, defer: the standalone source-authority example omits an invocation setting and production Dapr tenants target uses5seconds; both configurations already read that way at baseline6b and this AppHost change does not alter their behavior. A deployment using the same10s+2s verifier can interrupt valid work under those existing ceilings. This is preexisting standalone deployment-policy alignment, separate from the demonstrated AppHost source qualification; record it without changing production Dapr policy or the shared default during this repair.
- Blind2 — false: actual behavior is already covered by the mandatory SourceCatalogOwnerProfileTests profile, which sends a real command, requires202/terminal completion, and asserts persisted tenant/REST effects and zero outstanding leases. The new offline composition cases verify the changed default/override boundary; existing invoker controls verify real configured expiry/caller cancellation. Hosted execution remains pending, explicitly not claimed by those unit cases. Adding a duplicate test is unnecessary; an earlier deadline still fails the existing actual source gate.
- Edge reviewer — no findings.
- Verification-gap reviewer — no gaps.

Parent read both final hunks. All layers were launched before results were triaged. No code patch follows review; hosted exact-source qualification remains required.

## Exact hosted qualification closure

EventStorea82555477689e3e021c3c74bcab0ccfefd4ff307 completes all seven exact-source workflows successfully, including mainCI41360994832 and both actual published compatibility/live-sidecar Integration41360994325 jobs. Tenants3a32b8d74f954832dd8426f5b0151205a6be4e47 actual source41372568646 passes the complete cohort/bootstrap/command/persistence prefix, six REST and conditional reads, actual Admin UI proxy/owner read, refused descendant, persisted Redis tenant and zero outstanding leases. Its successful artifact is preserved in release-evidence/tenant-a825-owner-source-qualification-2026-11-02.json. Earlier failed evidence is retained; a later successful run does not erase the separately demonstrated premature10s native-query/AdminApi race, tracked in its own transport-budget spec. This closes this scoped functional repair only; final maintenance release remains pending its final source cut/publication. The standalone deployment-policy budget follow-up is resolved by preserved upstreamca93 and its tests.
