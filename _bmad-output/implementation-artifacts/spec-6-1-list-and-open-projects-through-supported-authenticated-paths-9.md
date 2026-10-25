---
title: 'Story 6.1: Verify supported Project reads in an isolated distributed topology'
type: 'feature'
created: '2026-10-25'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '6-1-list-and-open-projects-through-supported-authenticated-paths'
baseline_commit: '94f6ad1f6854df62c8cd15e47d65dc72985f4e79'
implementation_gate: 'approved-local-distributed-proof-formal-story-blocked'
context:
  - 'AGENTS.md'
  - '_bmad-output/implementation-artifacts/epic-6-context.md'
  - 'docs/supported-project-reads.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** List/open and asymmetric authority work locally, but their fixture substitutes discovery and Folder stream heads. Healthy normal AppHost startup proves neither supported query nor owner-read behavior.

**Approach:** Add an opt-in Development profile to the existing AppHost. Verify gateway → Projects → owner gateway → Folders through actual Dapr discovery, persisted projections and owner stream-head reads.

## Boundaries & Constraints

**Always:** Use Debug source references, platform seams and real Folder handlers. Separate user/workload trust; preserve bindings. Use ephemeral synthetic credentials, isolated actor/discovery namespaces and disposable storage. Seed only owned fixtures; compare domain bytes/ETags during reads. One C# type per file.

**Never:** Change defaults, public routes, pins, shared resources, historical evidence or sprint status. No production enrollment/deployment, gate acceptance, replacement G-4 runner, duplicated runtime, success stubs, TLS bypass or human-token forwarding. Preserve authenticated command/projection operations.

## I/O & Edge-Case Matrix

| Scenario | Input / state | Required behavior |
| --- | --- | --- |
| Authorized reads | Seeded Project/Folder and current grants | List, continuation and open traverse actual distributed handlers and return authorized metadata |
| Hidden target | Denied, populated foreign Tenant or absent Project | Existing indistinguishable safe denial; no protected metadata |
| Authority changes | Revoke Folder permission after a successful control | New reads observe current owner stream heads; old continuation cannot retain access |
| Inspection | Distinct permission present/absent; audit unavailable | Metadata disclosure requires permission and durable metadata-only audit |
| Dependency fault | Owned Folder/sidecar unavailable | Honest unavailable evidence; no false Complete empty result or authority downgrade |
| Compatibility | Existing command and projection operations | Fixture authentication admits their properly scoped workload assertions |
| Teardown | Success, assertion failure or partial startup | Clean all owned resources/private material and preserve pre-existing resources |

**Scope decision (2026-10-25):** The user selected the recommended local distributed proof and authorized continuing this workflow. Formal entry-gate completion remains outside this implementation.

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.AppHost/ProjectsLiveE2EFixtureProfile.cs`: opt-in precedent; compatibility fixtures cannot prove owner authorization.
- `tests/Hexalith.Projects.Server.Tests/Queries/ProjectReadAuthorityFixture.cs`: reuse trust/grant/audit assertions; replace loopback routing and `GatewayFixtureOwnerHeads` in the new lane.
- Folders' Server/Workers and `DaprOrganizationStreamSequenceReader`: existing owner hosts/projections/actor reads. EventStore already maps the opt-in binding broker.
- `tests/e2e/run-live-apphost.sh`: lifecycle/discovery precedent.
- `tests/Hexalith.Projects.Server.Tests/DelegatedProjectDirectoryRegistrationTests.cs`, `Queries/DelegatedProjectAdvisoryConversationDirectoryTests.cs` and `Queries/ProjectReadAuthorityFixture.cs`: assert the existing exact SDK failure wire strings without obsolete aliases, preserving source and retained package compatibility.
- `tests/Hexalith.Projects.ReadAuthority/Program.cs`: minimal fixture host over the existing platform broker registration/mapping APIs; avoids the gateway's metadata-startup dependency on its own not-yet-listening broker endpoint.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Authentication/WorkloadAuthorityBindingConfiguration.cs` and `WorkloadBindingAuthorityExtensions.cs`: preserve exact protocol-permitted, server-configured binding families and unambiguous grant identities through real configuration providers.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Authentication/WorkloadAuthorityConfigurationTests.cs`: real-provider binding, unknown-name refusal and reload regression coverage.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Authentication/WorkloadBindingAuthorityTests.Configuration.cs`: configured custom exact-value families retain the existing real issuer's approval/denial and mandatory Tenant/domain policy.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DelegatedOwnerQueryClient.cs`: optional whole-owner operation budget across acquisition, transport resilience, body reading and owned content disposal; absent configuration preserves the existing caller-owned budget.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.DomainService.Tests/QueryAuthorityCorrectionTests.OwnerDeadlines.cs`, `OwnerStatusResponseHandler.cs` and `DelayedOwnerResponseContent.cs`: deadline, default, invalid configuration, caller cancellation and late-disposal controls over the real signed owner client.

## Tasks & Acceptance

**Execution:**

- [x] Correct the existing EventStore broker configuration seam for protocol-permitted colon-delimited platform binding families and sparse grant identities; reproduce the failures and verify exact configured allowlists, invalid-prefix/ambiguity refusal and reload without changing issuer matching semantics or pins.
- [x] Add the optional platform-owned whole-owner operation budget required by the live outage proof, preserving absent/default behavior and genuine caller cancellation; reproduce the missing budget and verify acquisition, transport, body, disposal and invalid configuration.
- [x] `src/Hexalith.Projects.AppHost/SupportedProjectReadFixtureProfile.cs`, `Program.cs`, `Hexalith.Projects.AppHost.csproj`: compose real Folder Server/Workers, query catalogs and broker/receiver grants using root-resolved Debug dependencies. Reject conflicting profiles/incomplete isolation; preserve defaults.
- [x] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadTopologyTests.cs`: verify defaults, references, secret classification, grants, trust separation and invalid configuration.
- [x] `tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedTests.cs` and separate helpers: execute the matrix, seed through supported command/projection paths, observe actual heads and compare persisted state. Missing resources/positive controls fail qualification.
- [x] `tools/qualification/run_supported_project_reads.py`: launch through Aspire, wait/discover endpoints, run tests and clean owned resources on every outcome. Record full canonical revisions, configuration fingerprints, commands and actual XML/results without secrets/protected payloads.
- [x] `docs/supported-project-reads.md`: document the reproducible command, observed coverage/evidence and remaining gates. Historical normalization remains local proof.

**Acceptance Criteria:**

- Given an isolated fixture, when the launcher runs, then every matrix case executes without skips using actual discovery and owner evidence.
- Given successful/refused reads, when persisted state is compared, then domain/index/Folder bytes and ETags remain unchanged; required metadata audit is separately verified.
- Given success or failure, when teardown ends, then owned resources/credentials are removed and evidence distinguishes local proof from accepted equivalence/readiness.

## Implementation Notes

2026-10-25: Approved local scope preserves sprint status and formal acceptance records, overriding workflow sprint synchronization. Baseline remains the full canonical revision recorded above. No staging, commit, push or dependency update is part of this implementation.

2026-10-25 implementation dependency: real IConfiguration binding collapsed the broker's mandatory `eventstore:bound-tenant` and `eventstore:bound-domain` names into a single `eventstore` dictionary key; the prior fixture configured options directly. A platform-owned post-configuration adapter reconstructs exact server-configured `eventstore:bound-*` families within the existing protocol bounds and rejects malformed paths/values. The initial focused red reproduction failed 1/1; its first correction passed 90/90 existing/new cases. Parent audit then reproduced sparse indexes 0/2/3 assigning grant 2's allowlist to grant 3 (red 1/1) and the initial fixed-family whitelist rejecting two valid configured families (red 2/2). The corrected adapter maps by unambiguous configured grant identity, preserves custom exact-value families, and passes 104/104 binding/authority cases with zero skips (`/tmp/supported-reads-sparse-red.xml`, `/tmp/supported-reads-family-red.xml`, `/tmp/supported-reads-sparse-green-final.xml`). The broker decoder and exact request/grant matching remain unchanged. No package pin, gitlink, enrollment or public security rule changes.

The fixture uses a minimal technical broker host over the unchanged platform APIs because the gateway loads protected operational catalogs during StartAsync, before its own HTTP listener exists. Real Folder executable resources use generated Aspire project metadata and explicit dynamic HTTP endpoints; string-path resources triggered extra runtime restore/build attempts and an environment-specific NuGet-Migrations mutex failure at `/run/user/1000/.dotnet`. Actual persisted Tenant events are replayed into Folders' existing worker callback because its retained subscription topic differs from Projects'. None of these fixture composition choices changes runtime implementations, public routes, default topics or security policy.

The fixture generates a private Dapr resiliency resource scoped only to `eventstore` calls to `folders`, with a two-second attempt deadline and zero retries. Projects alone receives the optional three-second whole-owner budget, including assertion acquisition, every HTTP resilience attempt, body reading/deserialization and request/response disposal. Two owner operations fit below the retained ten-second enclosing Projects deadline. Topology assertions verify the exact scope/target, retry count and aggregate deadline ordering. Failing default and per-attempt-only policy evidence remains in `/tmp/supported-reads-run-25` and `/tmp/supported-reads-run-28`. The optional platform dependency reproduced red 2/2, then passed all 24 existing/new authority correction cases without skips (`/tmp/supported-reads-owner-deadline-red.xml`, `/tmp/supported-reads-owner-deadline-green.xml`). Absent/default behavior and genuine caller cancellation remain unchanged; only this local fixture enables the option. Restored faults require a real authorized-read recovery control before the next authority mutation. Actor provider observations preserve exact bytes and concurrency versions, with bounded concurrency over the complete owned key set; each read check also verifies that key set remains unchanged.

Launcher controls in `tests/tools/test_supported_project_reads_launcher.py` cover strict description/start parsing, process/configuration ownership, start races and replacements, partial startup, independent cleanup failures, case-insensitive environment filtering and a real subprocess SIGTERM cleanup/private-file/handler-restoration control. The first complete distributed matrix passed in `/tmp/supported-reads-run-29`; fresh full verification follows the launcher hardening.

Post-audit amendments route both Folder resource mutations through the same live process/private-configuration identity proof. Container deletion targets the observed immutable owned ID; successful structured Docker listings prove absence, including resources never created during partial startup. Observation failure cannot qualify cleanup. Focused launcher controls pass 20/20; actual created/uncreated-container cleanup is recorded in `/tmp/supported-reads-cleanup-id-control-2.json`. The full regression selector uses this runner's verified `-class-` exclusion; the rejected `-notclass` attempt remains preserved in `/tmp/supported-reads-final-20261025-2`.

The bounded full-regression diagnostic `/tmp/supported-reads-regression-diagnostic-1` passed all eight distributed rows and 85/85 Integration tests, then exposed five obsolete-alias compile errors in the existing Server tests. The three test files listed above now assert the exact existing `actor-exception` and `actor-response-mismatch` wire values, avoiding deprecated source aliases while retaining compatibility with the pinned package API. No assertion, warning policy or runtime behavior was weakened. The prior fresh run's LF-sensitive source assertion remains unchanged; tracked files retain their original endings, and direct full Integration verification passed 85/85 (`/tmp/supported-reads-integration-diagnostic.xml`).

2026-10-25 Parent task/matrix audit: read the complete Projects/EventStore unified diff and subsequent amendments, then independently validated fresh run `/tmp/supported-reads-final-20261025-4`: all eight distributed rows (1/1), Integration 85/85 and Server 1306/1306; zero failures/errors/skips. Fresh artifacts, source/runtime stability, six private discovery members, owned process/container/sidecar cleanup, private material removal and preservation of five pre-existing containers and one unrelated sidecar passed. Independently read platform XML (104/104 binding/authority, 24/24 owner-budget cases) and executed all 20 launcher controls. All execution tasks and acceptance criteria are satisfied for local scope. Formal acceptance/tracking remains blocked and unchanged.

## Spec Change Log

## Review Triage Log

All three layers completed before triage. Every filed finding is retained below. Entries B02/E01/V02 share one process-observation cause; B04/E03 share one endpoint-validation cause. Other findings remain separate. These are direct corrections to demonstrated guards, assertions or documentation, with no new public runtime surface or intent ambiguity.

| Finding | Verdict | Route | Evidence / decision | Resolution |
| --- | --- | --- | --- | --- |
| B01 | medium | patch | Verified cleanup discovers ownership only by the original name; a renamed owned container is reported already absent without removing its ID. Capture creation identity and clean by that retained ID. | Captured container IDs at creation; cleanup uses retained IDs through renames and verifies absence. Launcher controls and actual teardown passed. |
| B02 | medium | patch | Verified per-process environ/cmdline OSError is swallowed and can produce ownedSidecarsAbsent=true. Confirm disappearance separately; inaccessible observations must fail proof. | Unexpected process-read failures now fail observation; only confirmed disappearance counts as absence. Focused failure controls and real cleanup passed. |
| B03 | medium | patch | Verified post-stop logic checks only the pre-stop PID list; a new matching sidecar is not censused. Fresh ownership-scoped observation must detect remaining replacements. | Added a fresh post-stop census with process start identities; replacement and inaccessible-sidecar controls passed. |
| B04 | medium | patch | Verified registry validation compares only numeric ports, so a foreign address with the same port is marked owned. Validate complete local process/listen endpoint ownership. | Bound complete registry addresses to owned kernel listeners and local addresses. Both six-member discovery observations passed. |
| B05 | medium | patch | Verified projection keys are a fixed list, making projection-key-set equality tautological. Enumerate owned domain key families while retaining required-key and permitted-audit distinctions. | Enumerated owned projection and actor families across both tenants, retaining required-key assertions. Full matrix proved unchanged key sets, bytes and versions. |
| B06 | medium | patch | Verified the audit assertion accepts any prior Inspection record and does not bind evidence to both requests. Require the durable audit delta independently for inspected list and open. | Required a distinct durable audit delta for each inspected list/open, with exact disclosure class and count. Both live operations passed. |
| B07 | medium | patch | Verified approved audit field names plus a Synthetic substring check do not constrain field values or reject all seeded identifiers/credentials. Validate closed values/types, count, time and surrogate plus sensitive-value exclusions. | Validated closed audit values/types, hash, time/count, TTL and protected-value exclusions. Negative controls and live audits passed. |
| B08 | medium | patch | Verified missing-inspection-permission and audit-failure branches assert only HTTP status. Apply metadata-free safe problem checks to each list/open refusal. | Applied the closed metadata-free Problem Details check to both list/open permission and audit refusals. All live refusals passed. |
| B09 | medium | patch | Verified foreign-Tenant checks exercise only singleton open. Add populated foreign list and continuation refusal controls through the same coarse transport grant. | Added populated foreign list and old-continuation refusals through coarse transport admission. All match the absent-target safe problem. |
| B10 | medium | patch | Verified the fault denies every new Redis key, so a 404 alone does not identify audit persistence failure. Require healthy ordinary reads during the fault and an explicitly denied new audit write. | Restricted the ACL to new audit keys; existing audits and all non-audit keys remain available. Real Redis boundary controls and populated Complete controls immediately before both inspected refusals passed. |
| B11 | low | patch | Verified the 100ms budget test can finish synchronous disposal after 250ms. The implementation refuses a late result but cannot preempt synchronous custom disposal; clarify the documented cooperative guarantee without changing working runtime behavior. | Documented cooperative cancellation and rejection after synchronous disposal, without asserting wall-clock preemption. Existing 24 owner-budget controls remain applicable. |
| B12 | medium | patch | Verified complete regression XML validation does not require positive totals. Require actual nonempty executed results in each lane and cover empty summaries. | Required positive executed totals and passed=total with zero failures/skips in every lane; empty-result controls and all three real XML lanes passed. |
| E01 | medium | patch | Duplicate of B02; verified both inner process-read exception paths can falsely establish absence. | Resolved by the strict process-observation correction recorded for B02; retained as a separate finding. |
| E02 | high | patch | Verified RedisAsync checks a label then executes mutable ACL commands through the reusable name; replacement between operations can target foreign storage. Execute against the inspected immutable owned ID, including raw observer calls. | Bound every Redis observation/ACL action to its creation ID and verified owner label, then executed that same immutable ID. Replacement controls and real fault/cleanup passed. |
| E03 | medium | patch | Duplicate of B04; the complete address rather than just its port must be proved owned. | Resolved by complete local listener ownership recorded for B04; retained as a separate finding. |
| E04 | medium | patch | Verified safe_log_facts copies arbitrary alphabetic Reason values verbatim. Hash or strictly allowlist dynamic diagnostic values so protected text cannot become a retained closed fact. | Hashed dynamic diagnostic fact values and denial reasons. Sensitive-value controls across accepted fields passed. |
| V01 | medium | patch | Accepted the filed pre-verified gap: the documented launcher/CI path does not execute its ownership controls. Make the local qualification command execute nonempty passing controls before external allocation. | The documented launcher now requires all 31 ownership controls before allocating resources and retains their actual result. |
| V02 | medium | patch | Duplicate of B02/E01; source confirms the demonstrated PermissionError false-positive cleanup proof. | Resolved by the strict inner process-read handling recorded for B02; retained as a separate finding. |

## Verification

Authority story/index guards passed during planning. Before AppHost edits, capture Aspire baseline and verify APIs in Aspire docs. Build test projects individually in Debug/fresh artifacts; run built xUnit single-dash selectors, then relevant full suites. Run the launcher; inspect XML counts, state/cleanup reports and `git diff --check`. Record exact blockers.

2026-10-25 reviewed-tree verification: all 18 findings were resolved in 15 patch entries, with no deferrals, intent changes or review loopback. The parent read the complete original diff, review patches and final fault-control amendments. Failed parent runs `supported-reads-parent-review-final-20261025-1` through `-4` remain preserved: they exposed the audited Chatbot control, the overly broad recursive component-name check, the ACL-blocked EXISTS observer, and the blanket ACL's denial of missing non-audit catalog reads. The final fixture checks actual contract paths and uses a verified exact audit-prefix complement. All 5,888 real Redis byte-boundary controls passed, including missing/new non-audit reads/writes, existing audits, denied new audits and restoration (`/tmp/supported-reads-review-audit-prefix-control-2.json`). Default runtime policies, authorization and the 90-second recovery budget remain unchanged. The focused managed run `/tmp/supported-reads-review-audit-prefix-matrix-1` then passed all eight rows, stability, cleanup and external preservation.

The parent independently ran:

```sh
python3 tools/qualification/run_supported_project_reads.py --evidence /tmp/supported-reads-parent-review-final-20261025-5 --regressions --timeout 900
```

The final report passed with fresh artifacts: distributed 1/1 (all eight rows), Integration 89/89, Server 1306/1306, and launcher controls 31/31; zero errors, failures or skips. Both inspected refusals had actual populated Complete non-descriptive controls while the audit fault remained active. Actual authorized responses confirmed storage and Folder recovery. The parent read each XML and matrix result, verified nine live source graphs and all 14,205 source inputs, and checked all six private registry endpoints against owned listeners before and after reads. Source/runtime stability passed. The exact AppHost, six sidecars, three immutable container IDs and private material were removed; one pre-existing unrelated AppHost and five containers were preserved. The source manifest digest is `533cc410f7e4c4878e6b9d06dcfa931758d159b0ef8d8506a571d81f03fb078a`. All 4,038 EventStore source inputs match the already verified 104/104 broker and 24/24 owner-budget suites. Formal Story 6.1, acceptance gates and sprint tracking remain blocked and unchanged.
