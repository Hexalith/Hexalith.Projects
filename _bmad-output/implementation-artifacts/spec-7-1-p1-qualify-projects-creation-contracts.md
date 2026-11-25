---
title: 'Qualify 7.1-P1 Projects creation contracts'
type: 'feature'
created: '2026-11-24'
status: 'done'
route: 'dispatch'
baseline_commit: '280b1eedfb7b5e5b2b51d3fc52770c7bd2857080'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-24-readiness-blockers.md'
  - '{project-root}/_bmad-output/planning-artifacts/prds/prd-Hexalith.Projects-2026-05-24/addendum.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 7.1 cannot enter until package 7.1-P1 is qualified and accepted against the accepted Story 6.7 creation-contract baseline. No pin, fixtures, fingerprint or exercised rollback exist. Three canonical inputs that addendum §4.1 calls invalid are accepted or misreported at the baseline.

**Approach:** Close those three gaps in the create handler, then pin the result. Add creation fixtures and focused tests, run them from a clean checkout of an immutable commit, record the compatibility fingerprint and an exercised rollback, and publish the pin for owner acceptance. This is prerequisite evidence only.

**Decisions (Jerome, 2026-11-24, "apply recommendation"):**
- **Baseline.** `9dadd65c350145c651bf1d1defc5fc83f255bae5` is the commit that recorded Story 6.7 `done`. Its 13-file creation fingerprint is `90feea73…`, equal to HEAD `280b1eed`. The 6.7 `baseline_commit` `c6541a30` is only the start of that story.
- **Pin.** `evidence/epic7/7.1-P1-creation-contract-pin.json`, with native results under `evidence/epic7/7.1-P1/`. Both live under `_bmad-output/implementation-artifacts/`.
- **Q1=A: create-local fix.**
  - Create parsing rejects duplicate properties.
  - A parse error whose path is `$.projectMetadata.metadataClass` (compared case-insensitively) reports `projectMetadata.metadataClass`. This covers duplicate (D1) and non-string (D2) values.
  - A versioned request without `projectMetadata` returns 400 `projectMetadata` (D3).
  - The shared `RequestJsonOptions`, the spine and the client stay unchanged.
  - Accepted delta: a legacy body with duplicate keys moves from 202 to 400.
- **Q2=A: proposal confirmation.** Confirm is covered for validator parity only. Its parse-before-authorize order goes to a DW entry routed to Story 7.12.
- **Q3=A: build modes.** Contracts, Client and domain suites build with `CI=true` in package mode. Server.Tests builds in Debug source mode because package mode hits DW-63's `CS0246`. The Server evidence covers only the affected classes, so no Dapr sidecar is needed.
- **Q4=A: commits.**
  - Make local, path-staged commits on `main`: (1) the fix plus the two flipped tests, which is the rollback unit; (2) fixtures plus tests tagged `Package=7.1-P1`, whose SHA is the pin; (3) after acceptance, evidence, pin and tracker.
  - Commits 1 and 2 are pushed before owner acceptance, with the push confirmed with Jerome at that point.
- **Spec size.** About 3,500 tokens, kept whole at Jerome's choice.

## Boundaries & Constraints

**Always:**
- The existing `ProjectsDomainProcessor` stays the single writer.
- Legacy unversioned requests keep their behavior, except for the duplicate-key delta.
- Authorization precedes body parsing on `POST /api/v1/projects`. The Idempotency-Key header check still runs first.
- Invalid authorized input returns 400 `validation_error` with a metadata-only `details.rejectedField`. Nothing is echoed, no accepted-command fields appear, and the submitter is never called.
- `SensitiveMetadataTierValidator` stays the only classification predicate.
- Recheck `git status` and `git reflog` before each edit or commit.
- Run evidence only in clean local clones, with root submodules only and an isolated `NUGET_PACKAGES`.
- Edit tracker files on a scratchpad candidate and validate them with `--validate-index --sprint-status --deferred-work --p0-artifact --workspace-root`. Then run the active-index check and `test_production_authority_guard.py`.
- Edit CRLF artifacts as bytes.

**Never:**
- Deliver FR-1, expose a Project, or change event shapes.
- Admit tasks or Folders.
- Open or schedule Story 7.1; it stays `blocked`.
- Cut over a writer or change routing.
- Change the spine or generated client.
- Edit DW-63 files: `Program.cs`, `ProjectsServerServiceCollectionExtensions.cs`, `ProjectsDomainProcessor.cs` or the EventStore submodule.
- Stage other sessions' files or run `sprint_plan.py`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Legacy | Authorized `{name}`, optionally with `projectId`, `description` and `setupMetadata` | 202 and one `CreateProject`. The processor emits `ProjectCreated` (Active) and `ProjectFolderCreationPending` with a pinned `IdempotencyFingerprint`. | N/A |
| Canonical | Authorized `v1` plus `projectMetadata{displayName, metadataClass}`, one case per tier | The same events. The classification is not persisted. | N/A |
| Bad class | Missing, null, blank, whitespace or case variant, unknown, non-string, or duplicate | 400 `projectMetadata.metadataClass` | No command and no events |
| Versioned, no metadata | `{requestSchemaVersion:"v1", name}` | 400 `projectMetadata` | No command |
| Other invalid | Malformed JSON, unknown or duplicate property, wrong version, blank name or displayName, bad `projectId` | 400 `body` or the named field | No command |
| Unauthorized | Each denial mode × valid, malformed or invalid body | 404 safe denial with no validation feedback; zero body bytes read | The same body from an authorized caller gets 400 |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`:
  - `CreateProjectAsync` (539–666): the body read and `catch (JsonException)` are at about 570–585, and the version and metadata checks at 587–620.
  - `RequestJsonOptions` (61) is shared, so do not change it.
  - `Accepted` (1890), `ValidationProblem` (2011) and `CreateProjectHttpRequest` (2480).
- `src/Hexalith.Projects.Server/SensitiveMetadataTierValidator.cs`: the four-value predicate. Confirm reuses it at `Queries/ProposeNewProjectEndpoint.cs` ~599 with `rejectedField` `projectMetadata`. Confirm reads the body at ~186 and authorizes at ~204; leave both as they are.
- `src/Hexalith.Projects.Server/EventStoreProjectCommandSubmitter.cs` `SubmitCreateProjectAsync`: the production mapping into `SubmitCommandRequest`. Reuse it with a capturing `IEventStoreGatewayClient`, then feed `ProjectsDomainProcessor.ProcessAsync` (`ProcessCreate` 325–378, read-only). The envelope pattern is in `ProjectsDomainProcessorTests.cs` 387–405.
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs`:
  - The flipped tests are `PostProject_NonStringCanonicalMetadataClass_…` (313) and `PostProject_DuplicateCanonicalMetadataClass_…` (350).
  - The host is `StartAppAsync` (2681), the submitter fake is `FakeProjectCommandSubmitter` (2804) and the raw-body helper is `RawCreateRequest` (2385).
  - Denial knobs: null or `"system"` tenant, `seedTenantAccess`, `tenantEnabled`, `membershipPrincipalId`, stale timestamp.
- `tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs`: the confirm harness (`BuildProviderAsync` 818).
- `tests/Hexalith.Projects.Tests/Hexalith.Projects.Tests.csproj:12`: the fixture copy pattern.
- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` (2756–2800): the enum and required fields.
- `tools/planning/validate_production_authority.py:146` pins `"7.1-P1": (7, "open")`, and `_validate_closure_boundary` is at 602–616.
- `tests/tools/test_production_authority_guard.py:1056,1058`.
- `sprint-status.yaml`: the 7.1-P1 entry is at 541–551.
- Precedents: `evidence/epic6/6.7/approved-v2/verify-server.py` (the source-mapping environment) and `6-1-p1r-acceptance.json` (role-keyed `decisions`).

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`:
  - Add a create-only options copy with `AllowDuplicateProperties = false`.
  - Map the JsonException path as decided; add the D3 guard.
  - Flip the two existing tests and add a D3 test.
  - This is commit 1.
- [x] `tests/Hexalith.Projects.Server.Tests/Fixtures/CreationContract/creation-contract-fixtures.v1.json` plus a csproj copy item: matrix cases with expected status, `rejectedField`, command fields, event types and `IdempotencyFingerprint`.
- [x] `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs`: mark the class `partial` and give `StartAppAsync` an optional pipeline hook, so a request-body read counter can be installed.
- [x] `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.CreationContract.cs` (trait `Package=7.1-P1`):
  - Run each fixture through HTTP, the production submitter mapping and the processor.
  - Add paired auth-before-parse controls with zero body reads, and a no-accepted-fields check on 400.
  - Check that the OpenAPI enum matches the validator decision table.
- [x] `tests/Hexalith.Projects.Server.Tests/Queries/ProposeNewProjectEndpointTests.cs` (same trait): drive the same decision table through confirm. Four values are accepted, the variants get 400 `projectMetadata`, and nothing is submitted. This is commit 2.
- [x] `_bmad-output/implementation-artifacts/evidence/epic7/7.1-P1/run-qualification.sh`, mode `pin <sha>`:
  - Make a clean local clone and initialize the root submodules from local stores, without recursion.
  - Run `CI=true` package builds of Contracts.Tests (full), Client.Tests (`ClientGenerationTests`) and Hexalith.Projects.Tests (full).
  - Run a source-mode build of Server.Tests with `CreateProjectEndpointTests`, `ProposeNewProjectEndpointTests` and `ProjectsDomainProcessorTests`.
  - Use `-m:1` and `-warnaserror`, and write XML plus `commands.json`.
- [x] The same script, mode `rollback <sha>`: in a fresh clone, `git revert --no-edit <commit 1>`. Run the existing create suites with `-notrait Package=7.1-P1` and recompute the fingerprint.
- [x] `evidence/epic7/7.1-P1-creation-contract-pin.json` with these fields: `schema`, `package`, `baselineCommit`, `sourceRevision`, `fingerprint`, `behaviorDelta`, `fixtures`, `nativeReceipts`, `commands`, `rollbackProof`, `limits` (the DW-63 package blocker; task admission means 202 plus a submitter call today), and `decisions`, which stays pending.
- [x] Ask Jerome to confirm the push of commits 1–2, then record owner decisions in the pin.
- [x] After the decisions are recorded:
  - Set 7.1-P1 to `done` with `projects_evidence_pin`.
  - Change guard `ACTION_STATUSES["7.1-P1"]` to `done` and update the guard tests.
  - Add a DW entry for the confirm order, routed to 7.12.
  - This is commit 3.

**Acceptance Criteria:**
- Given the pin SHA in a clean clone, when `run-qualification.sh pin` runs, then every listed suite passes with 0 failed and 0 skipped, and the XML hashes match the pin.
- Given `git ls-tree <rev> -- <13 fingerprint files> | sha256sum`, when run at the baseline, the pin and the rollback tree, then all three equal `90feea73…`, and the create-path behavior delta is listed in the pin.
- Given commit 1 reverted in a clean clone, when the existing create suites run, then they pass with 6.7 behavior.
- Given the recorded decisions from Projects Owner, Product Owner and Test Architect against the pinned SHA after it is pushed, when the tracker moves, then Story 7.1 stays `blocked` and both the guard index and the guard tests pass.

## Implementation Notes

- Commit 1 (rollback unit, part 1): `a101e7cdae6918602b9d771c4fe4a18a96ebe551`. Commit 2 (fixtures and tagged tests): `31b3563780fc4e42a04e6982780a2a38c71a7acb`. Commit 2b (rollback unit, part 2, and the pin): `b738fc5b1709a65982f8632534561ade459a3290`, added after review when Jerome chose to report syntax errors inside the classification value as `body` (G1). It adds one commit to the Q4 plan. All three are on `origin/main`: `a101e7cd` and `31b35637` reached it on 2026-11-24 at 22:08 +0100, and `b738fc5b` was pushed on 2026-11-25 with Jerome's approval.
- The D1/D2 mapping holds for case-variant keys (`MetadataClass`) because the JsonException path keeps the wire spelling.
- xUnit v3 excludes a trait with `-trait- "Package=7.1-P1"`; the script uses that form for `-notrait`.
- Pin run at `b738fc5b`: Contracts 208, Client `ClientGenerationTests` 101, Domain 693 (package mode, Release) and Server 273 (source mode, Debug, 80 tagged), all 0 failed and 0 skipped, every build 0 warnings. Fingerprint `90feea73…` at baseline, pin and rollback tree.
- Rollback run: after reverting `b738fc5b` and `a101e7cd`, the existing create suites pass 192/192. The script's negative control then shows exactly the 15 delta fixture cases failing (77 tagged tests on that tree, 62 passed).
- Commit 3: Jerome accepted the pin at `b738fc5b` as Projects Owner, Product Owner and Test Architect on 2026-11-25. 7.1-P1 is `done` with `projects_evidence_pin`, guard `ACTION_STATUSES["7.1-P1"]` is `done` with a test that keeps Story 7.1 `blocked`, and DW-96 routes the confirm parse-before-authorize order to Story 7.12.

## Spec Change Log

## Review Triage Log

Review 2026-11-25, iteration 0: blind hunter (BH), edge-case hunter (EC) and verification gap (VG) layers ran on commits a101e7cd and 31b35637 plus the uncommitted evidence.

| Finding | Claim | Verdict | Route | Evidence |
|---|---|---|---|---|
| G1: VG-2, BH-1, EC-1 | JSON syntax errors at the metadataClass value (unquoted, truncated, bad escape) carry Path `$.projectMetadata.metadataClass`, so they report `projectMetadata.metadataClass` instead of `body` | low | patch | Two reviewers reproduced it on SDK 11.0.100. The code follows the literal Q1 path rule, but the matrix row says "Malformed JSON → 400 `body`" and the code comment says every other parse failure stays `body`. The frozen intent is ambiguous, so this goes to Jerome. Any code fix creates a new commit after the pushed pin. Jerome chose to map these to `body` (2026-11-25). Fixed in `b738fc5b`: the field is reported only when the JsonException has no inner reader exception, and three fixture cases pin it. |
| VG-1 | Package=7.1-P1 and flipped create tests never run in CI, because Server.Tests cannot build in package mode (CS0246, DW-63) | medium | defer | Pre-verified gap. Pre-existing DW-63 blocker; spec Q3=A accepts source-mode evidence. Logged as DW-86. |
| G2: VG-3, BH-3 | Rollback "15 delta cases fail" claim has no receipt in evidence | low | patch | The pin labels it an observation outside the script. The native receipt exists only at scratchpad `q-rollback/negative-control.xml`. Fix: rollback mode produces and records that receipt. Applied: rollback mode writes `rollback/negative-control.xml` and fails unless exactly the 15 delta cases fail. |
| G3: BH-6b, EC-11 | `rm -rf "$out_dir"` deletes the recorded receipts when anyone reruns the script to check AC1 | medium | patch | run-qualification.sh line 68 runs before any step. The receipts are untracked until commit 3, so a rerun loses them and the pin hashes match no file. Applied: the script exits 2 when the output folder already exists. |
| EC-10 | Rollback default reverts the last commit that touched the endpoint file, not the recorded rollback unit | low | patch | Correct at the pin SHA (resolves to a101e7cd), but wrong for any later SHA. Direct correction: default to the recorded commit. Applied: `rollback_commits` lists `a101e7cd` and `b738fc5b`. |
| EC-13 | Pin `buildFlags` says every build used -warnaserror; the Builds CLI build did not | low | patch | builds-cli-build.log reports 0 warnings and 0 errors, but the flag claim is wrong. Correct the pin wording. Applied in the pin. |
| BH-14 | Pin does not state that the server create contract is wider than the spine `CreateProjectRequest` (legacy fields, projectId) | low | patch | Legacy behavior is preserved by intent. One `limits` line. Applied in the pin. |
| G4: BH-2, EC-5 | Confirm still accepts duplicate and non-string classifications (shared RequestJsonOptions, last value wins) | medium | defer | Confirm is unchanged by Q2=A (validator parity only). Pre-existing. Logged as DW-87 and routed to 7.12. |
| G5: BH-16, EC-2 | Missing or non-JSON Content-Type makes ReadFromJsonAsync throw InvalidOperationException; only JsonException is caught, so the result is 500 | medium | defer | Pre-existing at the 6.7 baseline. Logged as DW-88. |
| BH-9 | String projectId is never validated: the projectId 400 branch is unreachable and any non-whitespace value reaches the processor | maybe-false | defer | ProjectId accepts any non-whitespace string. Unverified whether the EventStore gateway rejects such ids. Would be medium; pre-existing. Logged as DW-89. |
| BH-15 | Create idempotency fingerprint covers only the name, so a replay with a different description is not a 409 | medium | defer | All 10 accepted fixtures share sha256:7d9b6ec5…, including legacy-full. ProjectCommandValidator.cs is unchanged and fingerprinted; pre-existing. Logged as DW-90. |
| BH-4 | Fingerprint excludes the endpoint file; script hard-codes the expected value | false | reject | The frozen Decisions fix the 13-file set, and the fixtures plus behaviorDelta pin endpoint behavior. Recomputed independently: 9dadd65c, 280b1eed and 31b35637 all give 90feea73…. |
| G6: BH-5, EC-12 | XML hashes cannot be reproduced on a rerun; XML carries host and user details | false | reject | AC1 compares the run's receipts with the pin: all 5 sha256 values match. Committed xUnit XML with host attributes is established practice (evidence/6-4-advisory-resolution-20261021). |
| G7: BH-6a, EC-8 | mapfile ignores the mapping python exit code | low | reject | Needs prepare-build to fail, and an unmapped Server build then fails loudly with DW-63 CS0246. Fix adds a guard. |
| EC-9 | grep -q with pipefail can SIGPIPE the nested-submodule check | low | reject | Output is a few lines from a non-recursive update; unlikely. Fix adds a guard. |
| BH-7 | summary.json hard-codes nestedSubmodulesInitialized and isolatedNugetPackages; build logs not kept | false | reject | The script exits before the summary if a nested submodule is found, and it exports NUGET_PACKAGES unconditionally. Logs are deliberately not committed (no bulk diagnostics). |
| G8: BH-8, EC-3 | Blank requestSchemaVersion with name takes the legacy 202 path | low | reject | Version check unchanged from 6.7; real clients send v1 or nothing. Fix adds a guard. |
| BH-10 | No fixture reaches a domain rejection (ProjectCreationRejected) | low | reject | Covered in the pinned runs by ProjectsDomainProcessorTests, ProjectCommandValidatorTests and ProjectAggregateHandleTests. |
| BH-11 | Event and envelope fields (OccurredAt, actor, correlation) not asserted; payload subset check would pass without name | low | reject | created.Name is asserted, and the envelope fields are covered by ProjectsDomainProcessorTests in the pinned run. |
| BH-12 | 202 body never checked for an echoed classification | false | reject | Accepted() (ProjectsDomainServiceEndpoints.cs:1912) builds the body from correlationId, taskId and replay only. |
| BH-13 | Denials compared only within one denial mode | false | reject | Every mode returns SafeDenial(correlationId, taskId), and the authorization overload ignores its argument (line 2030). |
| BH-17 | Fixture `request` block unused; IDs and scenario counts not self-checked | low | reject | .Single() lookup already fails on duplicate case IDs. Developer-only; fix adds code. |
| EC-4 | Non-string class with a bad version reports metadataClass, not requestSchemaVersion | low | reject | Both are safe metadata-only 400s. Fix needs a pre-parse. |
| EC-6 | Decision-table row could drift from its createCase body | low | reject | Developer-only; fix adds assertions. |
| EC-7 | A validator that accepts an extra tier would pass enum parity | low | reject | SensitiveMetadataTierValidator.cs is fingerprinted, so any change moves the pin fingerprint. |

## Design Notes

The fingerprint covers 13 files: the generated client and helpers, `CreateProject`, the three creation events, the spine, `CreateProjectPayload`, `EventStoreProjectCommandSubmitter`, `ProposeNewProjectEndpoint`, `SensitiveMetadataTierValidator`, `ProjectAggregate` and `ProjectCommandValidator`. The shared endpoint and processor files change for unrelated reads, so they are excluded. The fixtures and the declared behavior delta pin their create behavior instead. The source-mode environment comes from `prepare-build` `buildPreparation.environment`. A probe at `280b1eed` built with 0 warnings and passed `CreateProjectEndpointTests` 98/98.

## Verification

**Commands:**
- `bash _bmad-output/implementation-artifacts/evidence/epic7/7.1-P1/run-qualification.sh pin <sha>`: all suites are green, and XML and `commands.json` are written.
- `bash …/run-qualification.sh rollback <sha>`: the baseline suites are green, and the fingerprint is `90feea73…`.
- `python3 tools/planning/validate_production_authority.py --validate-index` (active and candidate) and `python3 -m pytest tests/tools/test_production_authority_guard.py -q`: PASS.
