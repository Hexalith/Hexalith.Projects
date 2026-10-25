---
title: 'Story 6.1: Verify asymmetric authority for supported Project reads'
type: 'feature'
created: '2026-10-25'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
baseline_commit: '33b548a786a47e9a3f3ff2b1a6c40fc17a6a77b1'
implementation_gate: 'local-proof-complete-production-blocked'
context:
  - 'AGENTS.md'
  - '_bmad-output/implementation-artifacts/epic-6-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 6.1 is locally implemented, but its gateway/Folder fixture uses Development shared-key assertions. EventStore now supports asymmetric resource-bound authority; Projects lacks integrated proof and still documents the capability as absent.

**Approach:** Verify list/open through the existing broker, resource-binding client-credentials profile, RS256 receivers and real Folder handlers in a controlled local fixture. Correct capability and enrollment documentation.

## Boundaries & Constraints

**Always:** Preserve October 17 local work, platform infrastructure, separate user/workload trust, exact bindings and current Folder authorization. Use ephemeral synthetic credentials, Debug source references and one C# type per file.

**Never:** Enroll/deploy, publish, move pins, rewrite evidence or advance acceptance. Do not stub protected success, forward human credentials or weaken verification/permissions. Amend scope before runtime repair.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Required behavior |
| --- | --- | --- |
| Authorized | Scoped local grants and current evidence | List/open/continuation traverse actual handlers using verified RS256 assertions at both hops |
| Binding changed | Actor, Tenant, target, payload or audience altered after issuance | Safe refusal; no protected metadata |
| Inspection | Caller requests descriptions | Require distinct permission and durable metadata-only audit |
| Owner failure | Folder denied, absent, stale or unavailable | Preserve filtering, safe denial and honest degradation |
| Enrollment absent | Disabled binding, wrong client/grant/issuer | Fail closed without unbound retry |
| Revoked grant | Revoke loaded fixture grant | Stop new issuance; existing JWT lifetime remains separately bounded |
| Trust mismatch | User issuer presented to workload receiver | Reject without inheriting public trust |
| Read effects | Success, refusal, retry | Domain/index/Folder bytes and ETags unchanged; required metadata audit permitted |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectGatewayHttpTests.cs`: reuse real gateway → Projects → owner gateway → Folders persistence; retain Development coverage.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Authentication/WorkloadBindingAuthorityTests.QueryAuthority.cs`: reuse broker/issuer/receiver setup and digest grants; replace its substituted handler with real Projects integration.
- `references/Hexalith.EventStore/docs/security/workload-binding-authority.md`: authoritative configuration, grant, trust and lifetime contracts; consume the existing implementation unchanged.
- `docs/supported-project-reads.md`: stale signing-key-only statements in gateway and advisory sections.

## Tasks & Acceptance

**Execution:**

- [x] `tests/Hexalith.Projects.Server.Tests/Queries/ProjectReadAuthorityFixture.cs`: compose real broker and dedicated trust using temporary credentials/certificate, exact local grants and existing query/owner/storage infrastructure. Put additional helper types in separate named files.
- [x] `tests/Hexalith.Projects.Server.Tests/Queries/SupportedProjectAuthorityHttpTests.cs`: exercise the matrix through real Projects/Folder handlers, production workload verification, list continuation, inspection audit and persisted state assertions. Preserve existing fixture coverage.
- [x] `docs/supported-project-reads.md` and `docs/runbooks/projects-production-identity-contract.md`: correct capability statements and document opt-in configuration categories, observed proof scope and outstanding live enrollment facts; retain dated historical evidence.

**Acceptance Criteria:**

- Given synthetic enrolled grants, when list/open crosses the actual gateway/dispatcher/Folder chain, then current authorization controls results using the existing asymmetric profile.
- Given each matrix fault, when the chain executes, then existing refusal/degradation and disclosure rules hold without fallback.
- Given persisted fixtures, when focused and full Server checks run, then required cases execute without skips and domain-state assertions pass. Local proof does not establish accepted deployed gateway/owner equivalence.

## Implementation Notes

2026-10-25: The user approved this spec and continuing implementation. The approved local-only boundary overrides workflow sprint synchronization: formal Story 6.1 remains blocked and sprint-status.yaml is preserved.

Spec `-7` remains locally done. Formal 6.1 stays blocked behind usable P1R, P0/P2/P3/P4, conformance and independent READY. The advisory exception applies only to 6.4. Footprint: Projects tests/documentation; no irreversible actions or public contract changes.

October 25 baseline: authority/index guards passed; Debug build had zero warnings/errors; 103 focused/HTTP tests passed without skips. Commands/XML/logs: `/tmp/story61-recheck-20261025-07a8pn`. Asymmetric Projects integration is still unproven.


2026-10-25 implementation: all three tasks are complete. Fresh Debug build passed with zero warnings/errors; 24 new asymmetric cases and the full 1,288-case Server suite passed with zero failures/errors/skips. The parent independently read the complete unified diff and checked all eight matrix rows against passing XML cases. Commands, source hashes, XML, shadow comparison and matrix audit are under `/tmp/story61-authority-20261025-rlr9d3tw`. Local shadow comparison is equivalent; accepted gateway/owner fixture remains false.

Aspire baseline was attempted separately: isolated startup passed, but `aspire wait projects --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --timeout 45 --non-interactive` exited 17 while identity stayed Starting and dependents Waiting. The owned AppHost stopped normally; the pre-existing Memories session was untouched. Evidence: `/tmp/story61-recheck-20261025-07a8pn/aspire-summary.json`. The isolated persisted test lane supplies the executed local integration evidence; full-topology health is not claimed.

## Spec Change Log

## Review Triage Log

All three review layers completed before triage. Edge Case Hunter returned no findings. Every filed finding is retained below; patch findings preserve the approved local scope and production restrictions.

| Finding | Verdict | Route | Evidence / decision |
| --- | --- | --- | --- |
| B01 | medium | patch | Verified JwtBearerAuthenticationContract configures 60 seconds of receiver clock skew. New documentation conflates the maximum 120-second issuance lifetime with receiver acceptance. State both bounds and exercise receiver expiration outside its skew. |
| B02 | medium | patch | Verified DisposeAsync exits its host loop after the first stop/dispose failure, leaving later hosts and private material unprocessed. Attempt all owned cleanup and always remove signer/key material; add exceptional-teardown coverage. |
| B03 | medium | patch | AddProjectsServer registers default Data Protection, and this new fixture supplies no isolated provider/key repository. Cursor use can touch the user's persistent ring. Configure fixture-owned ephemeral protection. |
| B04 | medium | patch | RefusedAsync accepts every non-success and only three description fragments, allowing an unexpected validation/internal response to stand in for safe denial. Require each scenario's exact status and closed safe problem shape, and compare denied/absent canonical responses. |
| B05 | medium | patch | New enrollment fault cases cover only the initial gateway-to-Projects acquisition; the separately scoped owner-gateway and Folder acquisitions have no negative controls. Add both downstream acquisition controls and assert their complete attempt sequence. |
| B06 | medium | patch | Audience envelope mutation proves digest integrity, but no new case presents a correctly signed workload assertion with the wrong recipient at either receiving host. Add receiver-level wrong-recipient controls without changing runtime trust. |
| B07 | medium | patch | Distinct-permission and audit-outage refusal cases execute only open; list follows separate inspection/audit branches. Exercise both supported operations with explicit refusal assertions. |
| B08 | false | reject | The approved matrix permits required metadata audit writes. ProjectReadAudit uses platform ReadModelWritePolicy.UpdateAsync even when returning the existing record, so CAS may advance its ETag. Stable audit key/count/bytes plus unchanged domain ETags prove the intended contract; imposing audit ETag invariance would contradict existing permitted behavior. |
| B09 | medium | patch | The current tenant fault names an unseeded tenant. Add populated foreign-tenant controls with overlapping identities and compare both tenants' stored state to strengthen the claimed integration isolation proof. |
| B10 | false | reject | The seven snapshots cover every domain/index/Tenant/Folder model seeded by the current fixture, while additional audit keys are checked separately. Tracing ProjectReadQueryExecutor shows persistence only through ProjectReadAudit; no unobserved domain write or extra domain resource is reachable in these reads. The docs claim concrete model byte/ETag comparisons, not a global datastore census. B09 separately adds the demonstrated missing foreign fixture. |
| V01 | medium | patch | Pre-verified broken-verification finding: both transport observers throw before recording missing profile/assertion attempts. Issuer/invoker exception mapping then lets no-downgrade tests pass despite an attempted unbound retry. Make observers passive, record sanitized attempts before forwarding, and assert all attempted token/domain requests including missing/malformed authority. |

Confirmed patch findings are direct corrections to fixture observation, isolation, negative controls and documentation. They introduce no production/runtime surface and need no intent change. Full corrected-tree verification remains with the parent.


2026-10-25 final local completion: all nine confirmed patch findings are resolved. B01/B06 add real receiver expiry/skew and recipient controls; B02/B03 add exceptional cleanup and ephemeral Data Protection; B04 checks exact safe statuses and canonical closed problems; B05/V01 observe every sanitized attempt across all three acquisitions; B07 covers both inspection branches; B09 seeds and preserves populated foreign-Tenant models. B08/B10 remain rejected on the recorded evidence. Nothing was deferred.

Passive observation established the existing outbound contract: an issuance failure yields one original unsigned dispatch and a real receiver 401, with no ordinary OAuth fallback or retried domain dispatch. The final tests explicitly require that sequence, absent human bearer forwarding and no protected result. This resolves the prior observer gap while preserving the approved fail-closed/no-unbound-retry requirement and runtime behavior.

The parent rebuilt the corrected tree: zero warnings/errors. Full Server.Tests passed 1,306/1,306, including all 42 authority cases, with zero failures/errors/skips. Every approved matrix row maps to actual passing cases. Final shadow comparison is equivalent with no divergences and AcceptedGatewayOwnerFixture=false. Exact commands, source hashes, results, matrix mapping, review summary and cleanup are recorded in `/tmp/story61-authority-20261025-rlr9d3tw/parent-final-verification.json`.

The owned Dapr session stopped with exit 0; its three ports are closed. The exact owned Redis container and its anonymous volume were removed. No private fixture directories remain. The pre-existing Memories AppHost remains running. Formal Story 6.1 and all prerequisite/acceptance records remain unchanged. Changes stay uncommitted under the repository's explicit Git-authorization rule; no staging, commit, push, pin movement or sprint promotion was performed.

## Verification

Build Server.Tests with `dotnet build -c Debug -m:1 --artifacts-path` using a fresh directory. Run built xUnit `-class`/`-method` selections, then the full suite. Required persisted cases need owned isolated Dapr endpoints; missing fixtures block qualification. Record commands/results, independently review, check the diff and clean up only owned resources.
