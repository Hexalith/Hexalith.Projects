# Story 6.4 final workflow review and verification — 2026-10-22

All nine final patch groups are resolved. The build spec is done and sprint Story 6.4 is ready for review. Root changes remain uncommitted under the approved scope; no root push was performed. Three independent reviewers reported 11 findings: nine patched, one rejected (preserved user configuration with a running service), and one pre-existing G-6 default-policy mismatch deferred in the workspace ledger. Edge Case Hunter returned no findings. The spec records every individual verdict.

| Final executed lane | Passed | Failed / skipped | Receipt |
| --- | ---: | --- | --- |
| Full Projects.Server, including signed gateway and persisted-store integration | 1,035 | 0 / 0 | `projects-full.xml` |
| Full Projects.Contracts | 202 | 0 / 0 | `contracts-full.xml` |
| Client generation/fingerprint class | 101 | 0 / 0 | `client-generation.xml` |
| Full Conversations.Server | 1,135 | 0 / 0 | `conversations-full.xml` |
| Existing resolution engine/mappers | 107 | 0 / 0 | `engine.xml` |
| Production-authority guard | 29 | 0 / 0 | `planning-guard.log` |

**2,609 unique passing tests.** The 154 focused tests in [focused](focused/results.json) are subsets and are not added. Earlier packets cover different source states and must not be added to this total. Debug builds for all affected test projects and the engine passed with zero warnings/errors. The final package-mode Projects.Server Release build also passed with zero warnings/errors and restored Conversations Client/Contracts 1.3.0 and EventStore Client/Contracts 3.126.1 as packages.

The final corrections repeat exact Project/scope authorization on both legacy REST paths, authorize actual candidate Folder bindings for legacy Conversation matching, validate enumeration receipts without rejecting lag, and propagate caller cancellation after either owner pass. New regressions independently protect read-only admission, valid JSON byte boundaries, authorized Conversation HTTP dispatch and all five DTO wire shapes. Legacy File behavior is now documented in consumer guidance and the OpenAPI description; generated client changes are comments only, with the two required provenance fingerprint constants updated. No response schema, query parameter or scoring-engine rule changed.

`commands.json` records exact commands and results. `results.json` and `executed-tests.json` identify the passing XML cases. `matrix.json` reaudits all seven frozen matrix rows and 116 earlier references against these current runs, and adds 33 references for the final corrections. `source-manifest.json` binds 70 current source/config/evidence files across the feature and publication repairs; the parent independently verified every hash. `parent-audit.json` records this audit, unchanged frozen scope, exact root revision and effective package assets.

The authenticated gateway/store lane used a fresh isolated Dapr/Redis namespace. It retains actual JWT authentication, gateway/SDK dispatch, signed owner transport, owner handlers/authorization and read-model persistence. Existing controlled source-head and tenant-health fixtures remain named in the earlier packet. It verifies exact persisted bytes before/after queries. `fixture.json` records setup, readiness and successful cleanup of only the owned sidecar/container; no shared resources were stopped. Raw runtime/application logs and generated credentials remain outside retained evidence. Runner logs contain discovery/summary lines only.

The first owner-suite launch found its local test binary absent. Its exact diagnostic is retained as `conversations-missing-binary.log` and the failed command remains in `commands.json`; rebuilding that individual project resolved the harness prerequisite. No test failed in the final runs. Both authority validators and the raw whitespace check passed.

[Conversations 1.3.0 publication](../publication-handoff/verified-publication.json) and the [landed consumer pin](../publication-handoff/consumer-pin-landed.json) remain verified. Files are explicitly unsupported/incomplete. This is source acceptance under controlled local fixtures; it grants no deployment, production OIDC or G-4 acceptance, context authority, selection or confirmation authority.
