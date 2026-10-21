# Story 6.4 native stream-head inspection prerequisite

Status: **reviewed source slice complete; prerequisite in progress; Story 6.4 blocked**.

EventStore now answers authenticated native workload `0..0` stream requests with an empty page after two identical complete native inspections, before any event-range or payload processing. `GatewayNativeSourceMetadataReader` implements the existing reader contract over the existing gateway transport, validates exact response/proof identity and metadata, and applies one two-second admitted deadline across invocation, response validation and observation. Caller cancellation takes precedence; the reader performs no retries. Native proof JSON must explicitly include `erased`; valid v1 payloads and the existing constructor remain unchanged.

The reader is additive infrastructure and is not registered into Conversation resolution. A current source head cannot certify an earlier projection generation. Cold actor activation may still run existing recovery; no installation-wide persisted-zero-write result is claimed.

## Independent verification

| Final Debug source check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Reader, admission, existing authoritative reader and gateway transport | 117 | 0 | 0 |
| Controller, warm actor, native metadata and native prefix | 137 | 0 | 0 |
| Total | 254 | 0 | 0 |

Both independent test-project builds succeeded with zero warnings/errors. Every final command preserved all seven changed source/test hashes. The [command records](command-records.json), [native result summary](verification-summary.json), [source and binary hashes](source-manifest.json), and [reviewed patch](reviewed-owner.patch) bind the checks to the exact owner revision and worktree. Actual logs/XML, the recorder, review-baseline diff, initial preservation snapshot and superseded implementation attempts are retained in the [verification archive](verification-artifacts.tar.xz). The initial parent run was 240 tests before review corrections; final results above replace it for this source slice.

Coverage includes initialized-empty versus missing, nonempty and fully retained-away heads, changed incarnation/prefix/head/retention/erasure, malformed and omitted-field JSON, exact JWT/signed stream bindings, real workload middleware and HTTP transport, cancellation precedence, synchronous/asynchronous/validation blocking, and admission saturation/late completion/recovery. Existing native-prefix/actor tests exercise real in-memory persisted state and warm read-only behavior. These source fixtures are not deployed owner or G-4 acceptance evidence.

## Review

Three independent reviewers produced eleven findings: six source findings in five correction groups were patched, one persisted-coverage claim was refuted by executed existing actor/prefix tests, and four pre-existing G-6/P0 handoff issues were appended to the deferred ledger. The [prerequisite review log](../../spec-6-4-prerequisite-current-authorized-resolution-evidence.md#native-metadata-inspection-review--2026-10-21) preserves every claim and disposition. Corrections require explicit erasure status and add signed middleware, advanced-retention, synchronous deadline and isolated admission coverage. The parent inspected the corrected diff and independently rebuilt and ran the final checks.

Superseded logs retain stale package-mode assets/duplicate Commons, a named-argument expression-tree compile error, a missing test namespace and an ambiguous test-only Tenant-validator registration. Explicit Debug source restore/build and the targeted corrections resolved them. No dependency version was changed, and none of those attempts is promoted as a pass.

## Runtime and remaining gates

The [sanitized runtime baseline](runtime-baseline.json) records the exact start/stop commands. Startup succeeded with source references; security was Unhealthy and dependent services remained Waiting. The exact Projects AppHost was stopped normally before source edits. No healthy live topology, production authority or runtime acceptance is claimed.

Original-generation native-prefix certification, legacy-compatible guarded Conversation enrollment, content-free rebuild promotion, native Folder/Organization generation binding, committed File identity/Projects reference binding, production metadata-mutation adoption, independent installation/all-writer coverage, deployed owner authority and exact-current qualification remain incomplete. Accepted Projects G-4 consumer adoption/profile evidence is absent. RTI and parent Story 6.4 handlers remain gated, and no parent or sprint acceptance is recorded.

The [preservation record](preservation.json) verifies unchanged parent story/spec, sprint tracking, frozen intent, dependency configuration, repository heads/indices and unrelated tracked files. Only the active prerequisite notes, append-only deferred entries, this evidence bundle and the seven EventStore source/test files change. No commit, push, dependency update or deployment was performed.
