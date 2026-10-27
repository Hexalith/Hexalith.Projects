---
title: 'Story 6.1: Verify distributed duplicate delivery and full replay'
type: 'feature'
created: '2026-10-26'
status: 'done'
implementation_gate: 'verified-local-distributed-proof-formal-story-blocked'
route: 'dispatch'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
baseline_commit: '02d0dbd088095068cb40729191d352b05d89b55b'
context:
  - 'AGENTS.md'
  - '_bmad-output/implementation-artifacts/epic-6-context.md'
  - 'docs/supported-project-reads.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The managed distributed lane never redelivers Project events or invokes the authenticated rebuild endpoint. Existing persisted replay tests call the SDK dispatcher inside an in-process host.

**Approach:** Extend the existing isolated Development lane to prove duplicate delivery and full replay preserve supported list/open truth through actual HTTP routes, workload authentication, owner handlers and persisted storage.

## Boundaries & Constraints

**Always:** Preserve all nine existing rows. Use Debug source references, owned resources, actual persisted streams and current catalogs. Seed once. Observe both tenants; separate intentional projection writes from subsequent read effects. Keep exact event sequences, GlobalPositions and timestamps. One C# type per file.

**Never:** Delete or patch stored state, invent events/heads, substitute authority, call the dispatcher directly, or change runtime behavior, pins, defaults, cursor policy or public routes. No commits, publication, deployment, sprint transitions or prerequisite acceptance. Local proof establishes no G-4, approved shadow equivalence, P4 or formal Story 6.1 acceptance. Record runtime/dependency defects as blockers for separate scope.

## I/O & Edge-Case Matrix

| Scenario | Input / state | Required behavior |
| --- | --- | --- |
| Duplicate | Original request, then same events with a fresh dispatch ID | Both return `AlreadyCompleted`; domain keys, bytes, ETags and watermarks stay identical; existing cursor works |
| Full replay | Complete authenticated source prefix for each of four Projects | Fresh rebuild IDs return `Completed`; six projection values and watermarks agree; projection ETags change; other domain state stays identical |
| Paging after rebuild | Cursor accepted immediately before rebuild | Index ETag change invalidates it within its lifetime; fresh paging has no gaps/duplicates and open metadata agrees |
| Invalid replay | Truncated history with the observed full head, or mismatched head | Rejected without domain writes or loss of healthy reads |
| Authentication | Anonymous, human bearer, public-user issuer masquerading as workload, or wrong-operation workload assertion | Both projection routes refuse; domain state stays identical |
| Failure | Missing/inconsistent stream, failed dispatch or unreadable state | Fail qualification; retain sanitized observations and existing owned cleanup |

</frozen-after-approval>

## Code Map

- `SupportedProjectReadDistributedFixture` already supplies `StreamAsync`, `MetadataAsync`, `InvokeWorkloadAsync`, snapshots and stable read comparison. Extract request mapping from `ProjectPersistedStreamAsync`; retain seed requests. Validate complete identity/sequence/head using the SDK stream contract; set `RebuildSourceHeadSequence` from observed metadata.
- `ProjectReadPlatformEndpoints` maps both protected routes to the SDK; `ProjectReadProjectionHandler` rebuilds detail/index. These runtime files remain unchanged. Index ETags participate in cursor scope.
- Each rebuild may change only its target detail and that tenant's index ETags within observed domain families. Require identical projection bytes and exact watermarks; preserve the other tenant's state. SDK receipts remain operational evidence. Take a new baseline before testing read-only effects.

## Tasks & Acceptance

**Execution:**

- [x] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs`: add complete-stream validation, retained-request redelivery, protected rebuild/refusal helpers and sanitized comparison evidence.
- [x] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedTests.cs`: append duplicate/rebuild rows after existing scenarios; verify both tenants, invalid/authentication controls, cursor causality, fresh reads and zero subsequent writes.
- [x] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadEvidenceTests.cs`: mutation controls reject missing/truncated source, altered metadata/watermarks, forbidden state changes and incomplete evidence.
- [x] `tools/qualification/run_supported_project_reads.py` and `tests/tools/test_supported_project_reads_launcher.py`: require eleven exact matrix rows and complete replay evidence; test missing/failed observations. Preserve ownership, deadlines and teardown.
- [x] `docs/supported-project-reads.md`: record a reproducible fresh-destination command, actual results and limits; retain historical runs.

**Acceptance Criteria:**

- Given the owned fixture, when qualification completes, then all eleven rows execute without skips and retained evidence binds actual source streams, HTTP outcomes and persisted comparisons.
- Given successful rebuilds, when list/open and denial controls run, then metadata, ordering and exact watermarks agree with the prior observations, and the new domain baseline remains unchanged.
- Given any outcome, when teardown completes, then owned cleanup and external preservation are observed without credentials or protected payloads in evidence.

## Implementation Notes

2026-10-26: User approved this local proof and implementation. Frozen intent, runtime behavior, dependency pins, defaults, sprint status and formal prerequisite acceptance remain unchanged.

2026-10-26: Parent inspected the complete implementation diff and actual retained evidence. Fresh qualification at `/tmp/supported-project-reads-evidence-9f3273f502ed918a` passed all eleven exact rows (1/1, 158.067 seconds), Integration 119/119, Server 1306/1306 and launcher controls 53/53, with zero errors/failures/skips. Focused evidence controls passed 34/34. Actual source streams, eight duplicate outcomes, eight invalid replay outcomes, eight authentication refusals, four rebuilds and eleven persisted comparisons qualify. Both tenants retain 124 domain values and six exact watermarks; only six expected projection ETags change, with zero later domain writes. The old cursor is accepted immediately before rebuild and receives HTTP 400 on its first post-rebuild probe within lifetime. Both authority validators and whitespace checks pass.

2026-10-26: Batched read-only provider observations were verified against the original reader for 100 actual domain hashes and four fresh non-domain binary controls. Bytes and versions agree exactly, including all 256 byte values and escaping boundaries. Nine source graphs and 14,535 inputs stayed stable. All three test process groups, the exact AppHost, six sidecars, three containers and private material were verified absent; five pre-existing containers were preserved.

2026-10-26: Pre-change baseline `/tmp/supported-project-reads-evidence-d104f205df9b2665` passed all nine original rows. Three extended attempts remain failed and are documented separately: `dc24d72ccd4651b4` lacked a pre-rebuild cursor; `01abeb6bad6664fd` returned HTTP 200 for the old cursor without retained admission payload and observed unrelated McpCli/sidecar disappearance; `fabec33e799095bb` failed a post-authentication page check near 302 seconds. Source inspection identified the existing five-minute Folders Tenant-evidence limit versus the seven-day Projects development budget. Specific earlier HTTP 200 payloads cannot be retrospectively classified. The fixture now retains sanitized list admission/cursor observations and avoids repeated Docker overhead without changing freshness, authority, timestamps or seeding.

2026-10-26: Matrix audit maps duplicate, full replay, paging, invalid replay and authentication rows to the executed distributed test; missing/inconsistent source, failed evidence, unreadable/changed state and cursor cancellation/deadline/lifetime cases map to the executed C# evidence controls and Python launcher controls. All five execution tasks are verified complete. Independent review follows.

2026-10-26: All eight review patch groups are implemented. Focused corrected-tree validation passed 43/43 C# evidence controls, 60/60 launcher controls and a zero-warning/error Debug build. Parent independently reproduced and rejected the original incomplete-key, watermark, ownership, timestamp, request-binding, ordering, matrix-claim and deadline corruptions. First reviewed-tree live attempt `/tmp/story61-replay-reviewed-20261026-mvpjeaoe` failed on the initial bootstrap HTTP timeout before replay; its original sidecar/external-preservation failures remain recorded. A separate guarded cleanup recheck confirmed no owned sidecars, AppHost sessions or containers remained. Fresh full verification is still required.

2026-10-26: Reviewed-tree run `/tmp/story61-replay-reviewed-20261026-mvpjvh0p` passed all eleven distributed rows (169.044 seconds), Integration 128/128, Server 1306/1306 and launcher controls 60/60 without failures/skips. Parent independently validated all 50 replay observations and the complete 124-key census, source-bound watermark tuples and exact invalid-cursor Problem Details. Overall qualification remains failed: the immediate post-stop observation saw six owned sidecars still exiting, and a pre-existing Tenants sidecar retained its PID but changed configuration hash. The unrelated AppHost and all seven pre-existing containers remained. Subsequent guarded cleanup confirmed zero owned sidecars/AppHost sessions/containers. The original run result is unchanged.

2026-10-26: Corrected teardown observation with a ten-second bounded wait preserving the namespace census and every original PID/start identity. No additional stop signals or external-preservation changes were introduced. Focused launcher controls pass 63/63, both production-authority validators pass and whitespace checks are clean. C# fixture/runtime sources remain unchanged from the full functional run. A stable external-session window has been requested; no further live run starts without that required answer. Keep this spec in-review and formal Story 6.1 blocked until the final qualification or an explicit human scope decision resolves the gate.

2026-10-27: User reported that the other sessions are stopped, resolving the requested stable-session window. Read-only Aspire discovery still lists a Tenants AppHost; preserve that exact existing session and measure its stability through a fresh full qualification. No authorization to stop it is inferred.

2026-10-27: First stable-window attempt `/tmp/story61-replay-final-20261027-mvqicf65` failed before tests because private SQLite discovery retained five of six required registrations (missing Folders worker). Exact AppHost, all owned sidecars, three containers and private material were removed; external preservation passed. Launcher controls passed 63/63. The final teardown fix is exercised successfully; a fresh retry keeps source/runtime policy and the complete-discovery requirement unchanged.

2026-10-27: Final corrected-tree qualification `/tmp/story61-replay-final-20261027-mvqiiwxv` passed all eleven exact distributed rows (1/1 test, 156.047 seconds), Integration 128/128, Server 1306/1306 and launcher controls 63/63, with zero errors/failures/skips. Both fresh Debug builds reported zero warnings/errors. Parent independently inspected actual XML and revalidated all 50 replay observations, the complete 124-key census, eleven comparisons, source-bound watermark tuples and exact invalid-cursor Problem Details. Eight duplicates returned AlreadyCompleted, four rebuilds returned Completed, eight invalid replays failed and eight authentication refusals qualified. All 124 domain byte values and exact watermarks `[16, 11, 16, 31, 26, 31]` stayed identical; only the six expected projection ETags changed, and subsequent reads performed zero domain writes. The old cursor was accepted immediately before rebuild and rejected about 1.38 seconds later within both observer and original lifetime bounds.

2026-10-27: Final source binding matched all 14,535 inputs and nine Debug source-reference graphs. The live provider control matched 100 actual domain hashes and four binary controls. All three test process groups, the exact AppHost, six sidecars, three owned containers and private material were verified absent; the pre-existing AppHost, eight containers and seven sidecars were preserved. Both production-authority validators and `git diff --check` passed. Independent review is complete: twelve findings fixed in eight groups, two rejected with evidence, none deferred. This local spec is done. Frozen intent overrides generic completion instructions: no commit or sprint transition is performed; formal Story 6.1 and all prerequisite acceptance states remain unchanged and blocked.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patch: complete key census | Parent reproduced removal of all 118 non-projection fingerprints from every real comparison while the gate still passed. Bind comparisons to a separately retained complete owned domain-key census. |
| B2 | medium | patch: source-bound watermarks | Parent changed a later before/after watermark pair to 999 and the gate accepted it. Every phase must retain the source-bound key, tenant and exact position tuple. |
| B3 | medium | patch: source-bound watermarks | Parent swapped tenant labels across all real watermark observations without rejection. Counts do not establish each key's source ownership. |
| B4 | medium | patch: specific cursor refusal | The observer returns on any HTTP 400, and its positive control supplies an empty 400 response. Require the platform invalid-cursor Problem Details classification, preserving only closed safe metadata. |
| B5 | medium | patch: complete observer budget | Parent moved the final probe/rejection ten minutes later and the artifact still qualified inside the separate fifteen-minute cursor lifetime. Retain and validate the observer start/deadline. |
| B6 | medium | patch: source timestamps | Parent replaced a real source timestamp with `not-a-timestamp` and the gate passed. Validate timezone-aware, non-default timestamp syntax. |
| B7 | low | reject | Bulk replacement of opaque diagnostic event/request digests is accepted, but these hashes cannot be independently recomputed without the intentionally excluded payloads. Actual source/request equality is asserted by the bound executing fixture; these observations are not an authenticated standalone manifest. A new canonical manifest/proof protocol exceeds a direct correction for this unlikely diagnostic corruption case. |
| B8 | medium | patch: refusal request bindings | Parent removed authentication observations' request/event/catalog/dispatch/head/count fields and the gate still passed. Validate the fields already emitted by the writer against their retained source and fresh identities. |
| B9 | medium | patch: causal observation order | Parent moved all persisted comparisons ahead of all attempts and the gate accepted the artifact. Require each comparison/read observation to follow its associated actions and preserve the actual baseline sequence. |
| B10 | low | patch: matrix claims | Parent supplied a false read-only invariant and true formal-equivalence/readiness claims and the helper still qualified them. The outer report stays local/false, but contradictory matrix evidence must be rejected by direct flag checks. |
| E1 | medium | patch: source-bound watermarks | Independently reproduced the same later-watermark mismatch as B2; initial-source checks do not constrain later tables. Grouped only after its individual verdict. |
| E2 | medium | patch: source timestamps | Independently reproduced the malformed-timestamp case; the nonempty-string guard does not validate the source timestamp contract. Grouped with B6 after its individual verdict. |
| E3 | false | reject | The managed caller is `QueriesController.Submit`, which constructs its HTTP 200 `SubmitQueryResponse` with Success=true after `SubmitQueryHandler` throws on failed or absent router results. No actual caller of this fixture observer emits the hypothesized failed HTTP 200 envelope. The general DTO permits false elsewhere, but that does not establish this claimed path. |
| E4 | medium | patch: complete observer budget | The evidence callback is awaited without the operation/caller budget and cancellation is not rechecked before accepting HTTP 400. A delayed file write can outlast cancellation/deadline; bound recording and reject late completion. |

Review disposition: twelve findings survive in eight patch groups; one low diagnostic-manifest suggestion and one unreachable managed-envelope claim are rejected. The verification-gap layer found no grounded gaps and independently passed all 53 launcher controls. No findings are deferred. Frozen intent remains unchanged.

## Verification

- Before code changes, load the Aspire orchestration skill and capture the owned AppHost baseline; preserve unrelated sessions.
- Run `python3 -m unittest discover -s tests/tools -p test_supported_project_reads_launcher.py` and focused evidence controls from the built Integration assembly.
- Run `python3 tools/qualification/run_supported_project_reads.py --regressions --timeout 900` with a fresh automatic destination. Inspect actual XML, all eleven rows, replay comparisons, source binding and cleanup. The launcher builds Debug and runs individual test assemblies.
- Run `python3 tools/planning/validate_production_authority.py --story-id 6.1`, `python3 tools/planning/validate_production_authority.py --validate-index` and `git diff --check`. Record exact blockers; missing evidence cannot pass.
