---
title: '6.1: Restore local Aspire startup through Docker Desktop WSL integration'
type: 'bugfix'
created: '2026-10-25'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The local Projects Aspire topology cannot create its Keycloak security container, leaving dependent services waiting. Both the retained Story 6.1 attempt and a fresh reproduction report an unhealthy Docker runtime. Windows Docker Desktop works, but Ubuntu's Docker CLI integration mount is missing and its socket does not respond.

**Approach:** Restore the existing Docker Desktop WSL integration, then verify the unchanged authenticated Projects topology through Aspire. Preserve existing container identities and data, and document the diagnosed failure and recovery in the local topology runbook. This is a local environment repair associated with Story 6.1; it does not complete the blocked story or change any production acceptance record.

</frozen-after-approval>

## Implementation Notes

- Baseline: clean Projects main at `f02481c2aa65ccc6d13f7fa71eb3a884503337ba`. The previous 6.1 work is committed. This task does not authorize staging, committing, pushing, dependency updates, or sprint promotion.
- Investigation reproduced security Starting with no container ID, and all dependent services Waiting. `docker version` exits 1 with the Docker Desktop WSL integration message; `aspire doctor --format Json --non-interactive` exits 1 with No container runtime detected. `docker.exe version` succeeds; fresh WSL sessions have the same missing integration.
- Docker Desktop already enables Ubuntu-26.04 integration. Snapshot the five existing containers before recovery; all have restart policies. Stop only the owned Projects AppHost, restart Docker Desktop through its supported CLI, verify existing container identities and health, and restart the Projects AppHost. No container removal or WSL shutdown.
- Footprint: environment recovery, `docs/runbooks/projects-topology.md`, and this spec. No application or public API changes are indicated. No unresolved product intent gaps or data migrations.
- Evidence directory: `/tmp/story61-aspire-fix-20261025-iu5iw444`. Exact startup command, original resource snapshot, and pre-existing container inventory are retained there. Diagnostic logs contain local endpoints and remain outside version control.

- Recovery: Docker Desktop restart completed in 21.88 seconds; Linux Docker now reports client/server 29.8.2, and all five original containers are running with unchanged IDs. Aspire doctor has zero failed checks.
- Follow-on startup exposed the current platform workload-issuer requirement: EventStore exited 134 for missing client ID/secret after Keycloak became healthy. This remains the same bounded startup goal: the Folders and Tenants AppHosts already implement the exact required local configuration. Apply that established composition to Projects without changing the runtime graph, libraries, production trust or story gates.
- Additional footprint: AppHost Program.cs, the local realm JSON, and KeycloakWorkloadRealmTests.cs. Reuse WithEventStoreWorkloadClientCredentials, one generated secret shared with Keycloak, and optional single-grant scopes for projects/tenants and the five domain-service operations. Validate realm invariants, actual scoped/unscoped Keycloak issuance, durable service health, and cleanup.

- Verification: Debug Integration.Tests build passed with zero warnings/errors; 33 focused topology/realm cases passed with zero failures/errors/skips. Live checks found all ten services/sidecars Running and Healthy, validated all ten audience/operation token pairs (300-second lifetime, no human grants), empty authority on unscoped issuance, wrong-secret 401 and unknown-scope 400. Projects/Workers alive/ready return 200; anonymous Projects list returns its documented safe 404. Tokens and generated secrets were not persisted. Commands/results: build-integration-result.json, topology-tests-command.json, fixed-readiness-results.json and live-validation.json in the evidence directory.
- Live probe adjustments: the Python client initially lacked the development CA directory; rerunning with SSL_CERT_DIR pointing to the existing trust store preserved TLS verification. The unauthenticated Projects read uses the documented indistinguishable 404 contract, not a generic 401. These probe corrections changed no application security.

- Concurrent source update: the user committed root gitlink synchronization as `6d327ca427ada949892aa70bbba1498d43284a71` at 2026-10-25T09:17:47Z. The original reproduction baseline above remains historical; implementation and the 09:24 Debug build use this later checkout. EventStore is `5be0c309f457d0af1f169bc591609fb4be6473ce` and Builds is `1783c0a377615063f3222c1d28f3ab513af4e72f`. All root-declared dependency revisions are retained in validation-revisions.json. No gitlinks were changed by this task.

## Review Triage Log

- B1 — high, patch: verified persistent container configuration changes can recreate Keycloak without a data volume. Corrected the runbook before its first startup command: require preserved/exported data and verified restoration, distinguish container reuse from recreation, and use a separate isolated session with persistence explicitly false for validation. No existing persistent identity container was present or modified during this task.
- B2 — medium, patch: realm tests alone did not detect disconnected credential wiring. Added actual AppHost model tests for the same secret object at both consumers, secret classification, client ID, persistence defaults in both modes, and absence when security is disabled. The test project consumes the centrally pinned Aspire.Hosting.Testing package; no version or runtime dependency was changed.
- B3 — low, reject: a permanent live Keycloak suite would add a new runtime harness beyond this bounded startup fix. The retained executable validate-live.py and live-validation.json provide repeatable real issuance checks for every local audience/operation pair, unscoped tokens, bad secrets and unknown scopes; application-model and realm tests provide committed regression coverage. No claim of production receiver acceptance is made.
- B4 — low, patch: documented and executed seven resource snapshots across 30 seconds, requiring ten services/sidecars to remain Running/Healthy with identical names and start timestamps. No exits or restarts occurred. This is bounded startup evidence only.
- B5 — medium, patch: preserved the original reproduction revision and recorded the later implementation/validation HEAD plus every root-declared dependency revision in validation-revisions.json. The user-owned gitlink synchronization is outside this patch.

## Completion

2026-10-25: local Aspire startup repair complete. Final Debug build passed with zero warnings/errors; all 36 focused topology, realm and actual AppHost-model tests passed without errors/failures/skips. Review patches are verified; no findings were deferred. The bounded live proof covered ten healthy services/sidecars, thirteen token-issuance controls, four health endpoints and the safe unauthenticated read response. Seven snapshots over 30 seconds found no service exit or restart. The test AppHost stopped normally and its Keycloak container was removed by Aspire; all five original shared container IDs remain running. A separately started FrontComposer AppHost was observed at final cleanup and left untouched.

Final evidence: review-corrected-build.log, final-topology-tests-command.json, final-topology-tests.xml, live-validation.json, stability-check.json, cleanup.json, validation-revisions.json and final-source-hashes.json in the recorded evidence directory. Earlier failed analyzer builds are retained; the corrected model tests use cancellation and await conventions required by this repository. No production contract, acceptance record, sprint status or submodule pointer was changed by this task. Changes remain uncommitted under the user-provided Git rule.

2026-10-25 publication authorization: the user subsequently requested "push all changes", authorizing staging, a Conventional Commit and push of this completed repair. The earlier uncommitted-state statements describe the state before that request.
