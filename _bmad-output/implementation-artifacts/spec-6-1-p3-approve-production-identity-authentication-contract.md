---
title: 'Approve the production identity and authentication contract'
type: 'feature'
created: '2026-07-19'
status: 'in-review'
local_implementation: 'complete'
owner_acceptance: 'blocked'
baseline_commit: 'd4a69ad9a640294e849444a60d7ddfbd0468f91a'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md'
  - '{project-root}/references/Hexalith.EventStore/_bmad-output/project-context.md'
  - '{project-root}/src/Hexalith.Projects.Server/Authentication/ProjectsClaimsTransformation.cs'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `Hexalith.Projects.Server` currently enables bearer authentication only when an
authority happens to be configured. Missing production identity configuration therefore leaves an
implicit anonymous host path, and the local claim shim does not document or prove the P2 dual-principal
identity contract.

**Approach:** Add a validated Projects authentication contract that requires OIDC authority, issuer,
audience, and secure metadata in Production, while allowing only an explicit Development bypass.
Adopt the platform-owned P2 identity mapping and provide deterministic configuration/token fixtures,
ownership documentation, and rollback evidence without duplicating EventStore query-envelope logic.

## Boundaries & Constraints

**Always:** Bind `Authentication:JwtBearer` through options validation before serving endpoints; derive
actor, workload, delegation, scopes, and audience only from validated token claims and the accepted P2
platform contract; keep authorization fail-closed and metadata-only; keep production secrets outside
source control; preserve the existing local Keycloak fixture and safe-denial behavior.

**Ask First:** Stop if the accepted P2 public mapping or revision changes, if an Identity/Security Owner
or Solution Architect rejects the claim/configuration contract, or if production deployment ownership,
secret references, or rollback pins are not available. Do not select a new identity provider or audience
without owner approval.

**Never:** Do not permit anonymous or symmetric-key authentication in Production; do not infer identity
from headers, query/body fields, Dapr metadata, or client-supplied tenant values; do not copy the P2
query envelope/helper into Projects; do not commit production credentials or switch the public read route.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Production startup | Missing/blank authority, issuer, audience, or insecure metadata setting | Host fails validation before serving protected endpoints | Deterministic startup/configuration failure; no anonymous fallback |
| Development bypass | `Development` environment and explicit bypass flag | Host may start without OIDC only for local diagnostics | Same flag in Production is rejected |
| Valid token | Trusted issuer/audience, lifetime, signature, and required Projects permission | Existing authorization gate receives normalized identity; accepted P2 envelope keeps actor/workload/delegation distinct and preserves scopes as routing metadata | Missing required permission fails closed with safe denial |
| Invalid identity | Wrong issuer/audience, expired token, malformed delegation, or absent required permission | No protected Project data is returned; malformed optional delegation remains unknown | Authentication failure or safe authorization denial; no claim detail disclosure |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Projects.Server/Program.cs` -- current conditional JWT registration and middleware pipeline.
- `src/Hexalith.Projects.Server/Authentication/ProjectsClaimsTransformation.cs` -- existing tenant, principal, and permission normalization.
- `src/Hexalith.Projects.AppHost/Program.cs` -- local security resource wiring and Projects environment injection.
- `src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json` -- development-only identity fixtures.
- `src/Hexalith.Projects.AppHost/DaprComponents/accesscontrol.yaml` -- local versus production Dapr access-control boundary.
- `tests/Hexalith.Projects.Server.Tests/ProjectsClaimsTransformationTests.cs` -- claim normalization tests.
- `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` -- AppHost security wiring contract tests.
- `tests/Hexalith.Projects.Integration.Tests/DaprConfigurationTests.cs` -- local/production access-control assertions.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Projects.Server/Authentication/ProjectsAuthenticationOptions.cs` -- add environment-aware required OIDC settings and startup validation -- prevent implicit anonymous production startup.
- [x] `src/Hexalith.Projects.Server/Authentication/ProjectsAuthenticationServiceCollectionExtensions.cs` and `src/Hexalith.Projects.Server/Program.cs` -- register validated bearer authentication/authorization and an explicit Development-only bypass -- preserve the existing endpoint gate and safe denial.
- [x] `src/Hexalith.Projects.Server/Authentication/ProjectsClaimsTransformation.cs` -- align normalized claims with the accepted P2 mapping without synthesizing missing actor, workload, delegation, scope, or audience evidence -- keep identity provenance authoritative.
- [x] `src/Hexalith.Projects.AppHost/Program.cs` and `src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json` -- make local fixture wiring explicit and keep production secret/config ownership external -- prevent dev credentials from becoming deployment defaults.
- [x] `tests/Hexalith.Projects.Server.Tests/Authentication/ProjectsAuthenticationContractTests.cs` and related existing tests -- cover startup/configuration, valid/invalid token claims, delegated/non-delegated mapping, scope preservation, missing permission, and safe-denial behavior -- create the P3 verification fixture set.
- [x] `docs/runbooks/projects-production-identity-contract.md` -- record configuration keys, owner responsibilities, fixture commands, accepted P2 pin, and rollback procedure -- make the approval and deployment boundary reviewable.

**Acceptance Criteria:**
- Given a Production host, when required OIDC configuration is absent or insecure, then startup fails before protected endpoints can serve anonymously.
- Given an explicit Development bypass, when the environment is not Development, then validation rejects it and no bypass is activated.
- Given valid non-delegated or delegated token claims, when Projects forwards the authenticated request, then the P2 envelope preserves original actor, workload, delegation, scopes, and audience separately; malformed optional delegation remains unknown.
- Given invalid issuer/audience, expired credentials, absent required permission, or cross-Tenant access, when a protected read is attempted, then no protected metadata is disclosed and the existing safe-denial contract remains intact.
- Given local and production configuration review, when the owner-approved fixture and rollback checks run, then no production secret is stored in the repository, the local Keycloak fixture is clearly development-only, and the accepted P2 revision/configuration can be reverted deterministically.

### Review Findings

- [x] [Review][Decision] HIGH — P3 advances while its required P2 dependency remains unaccepted — Resolved 2026-07-20: the EventStore-owned P2 spec is `done`; its implementation commits `58236cf3` and `b904322b` are contained by the current root-pinned EventStore revision `5c123ccb`. The remaining Projects-root P2 evidence bookkeeping is stale cross-repository planning, not an active EventStore dependency or a P3 code-review blocker.
- [x] [Review][Decision] HIGH — The required-scope authorization policy contradicts the current P2 contract — Resolved 2026-07-20: `eventstore:permission` remains the Projects authorization input. Validated `scope`/`scp` claims are preserved as routing metadata in the accepted P2 envelope, but their absence does not independently authorize or deny a request.
- [x] [Review][Decision] HIGH — Deterministic rollback inputs and approvals are unavailable — Resolved 2026-07-20: exact deployment release/package/configuration pins, approval evidence, and executable rollback selection belong to P4 release acceptance. P3 documents and verifies the authentication contract and rollback invariants without blocking its code review on artifacts that do not exist until release assembly.
- [x] [Review][Patch] HIGH — Default launch profile keeps anonymous bypass active when Aspire injects OIDC [src/Hexalith.Projects.Server/Properties/launchSettings.json:11]
- [x] [Review][Patch] MEDIUM — Disabling bundled Keycloak also forces anonymous mode instead of honoring external Development OIDC [src/Hexalith.Projects.AppHost/Program.cs:57]
- [x] [Review][Patch] MEDIUM — Options binding and manual configuration parsing can select different authentication modes [src/Hexalith.Projects.Server/Authentication/ProjectsAuthenticationServiceCollectionExtensions.cs:32]
- [x] [Review][Patch] MEDIUM — Claim normalization can read authorization evidence from a different identity than the authenticated identity [src/Hexalith.Projects.Server/Authentication/ProjectsClaimsTransformation.cs:29]
- [x] [Review][Patch] HIGH — Startup fail-closed verification never starts the host and does not isolate all required-field and HTTPS guards [tests/Hexalith.Projects.Server.Tests/Authentication/ProjectsAuthenticationContractTests.cs:31]
- [x] [Review][Patch] HIGH — JWT middleware, protected safe-denial scenarios, and reproducible fixture commands are not exercised [tests/Hexalith.Projects.Server.Tests/Authentication/ProjectsAuthenticationContractTests.cs:149]
- [x] [Review][Patch] HIGH — P2 actor/workload/delegation mapping and token forwarding are not verified at the EventStore envelope boundary [tests/Hexalith.Projects.Server.Tests/ProjectsClaimsTransformationTests.cs:65]
- [x] [Review][Patch] MEDIUM — Development OIDC validation accepts unusable authority and metadata combinations [src/Hexalith.Projects.Server/Authentication/ValidateProjectsAuthenticationOptions.cs:40]
- [x] [Review][Patch] MEDIUM — Runbook misidentifies the root-pinned EventStore revision [docs/runbooks/projects-production-identity-contract.md:36]
- [ ] [Review][Patch] MEDIUM — Unrelated FrontComposer submodule pointer bump is included in the P3 change [references/Hexalith.FrontComposer:1] — Not applied: removing it now requires rewriting committed P3 history or moving the user-owned FrontComposer checkout from `6a00bd95` back to `b0254994`; this remediation preserves both states.

## Design Notes

The Projects host owns adoption and fail-closed startup policy; EventStore owns the dual-principal
query-envelope implementation. The AppHost may inject authority/issuer/audience and clear any signing-key
override for local OIDC, but production values must come from deployment-managed configuration or secret
references. `scope`/`scp` values remain forwarded routing metadata; action-specific
`eventstore:permission` evidence is the fail-closed Projects authorization input.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: zero warnings/errors with current sibling source dependencies.
- `dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -class Hexalith.Projects.Server.Tests.Authentication.ProjectsAuthenticationContractTests -class Hexalith.Projects.Server.Tests.ProjectsClaimsTransformationTests` -- expected: focused startup, middleware, identity, forwarding, and safe-denial fixtures pass.
- `dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll` -- expected: all server tests pass.
- `dotnet build tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --configuration Debug --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: zero warnings/errors.
- `dotnet tests/Hexalith.Projects.Integration.Tests/bin/Debug/net10.0/Hexalith.Projects.Integration.Tests.dll` -- expected: all AppHost/access-control and existing integration contracts pass.

**Results (2026-07-20):** 582/582 server tests and 20/20 integration tests passed in Release configuration with `--no-restore`.

**Manual checks (if no CLI):**
- Inspect the production configuration/secret references and rollback record; they must contain no committed credential and must reject the Development bypass outside Development.

### Historical gate check before P1R acceptance (2026-10-01)

**Disposition:** blocked pending owner resolution of the frozen P2 mapping/revision boundary.
The completed execution checkboxes and July test results above are historical implementation
evidence; they do not establish acceptance or passing verification of the current baseline.
This run changed only this verification note; it did not alter runtime source, tests, dependency
pins, submodule checkouts, or sprint statuses. Concurrent EventStore working-tree edits were preserved.
The existing `baseline_commit` and frozen intent are preserved.

**Observed coordinates:** Projects HEAD is `4d8dcf65803792f7def3b10ed21227329536154b`.
The runbook records EventStore `5c123ccbce2515a618134382d6181c2ec1a5cbbf`, while the current
root gitlinks select EventStore `6dededdecd62dd6dc6d1f15810108d860ec70c8f` and Builds
`21ce044ab465ccb2adab58b3d66e394ffbecf3c2`; the Builds catalog pins EventStore `3.110.0`.
The [P2 owner handoff](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/6-1-p2-query-security-projection-capability-acceptance-record.md)
explicitly remains draft and unaccepted, adds `DelegationId` derived from bounded `act.sub`,
and leaves source/package/runner/consumer pins and executable rollback selection pending.
At this pre-acceptance checkpoint, the [historical P1R record](evidence/6-1-p1r-acceptance-20260922-3.106.0.json)
selected EventStore `3.106.0` / Builds `ad52f350a2f0bc47849179ae17b4594dafff5363`.
These records did not authorize P3 to adopt or approve the current P2 contract.

**Subsequent P1R acceptance (2026-10-01T06:17:01Z):** the [fixed P1R record](6-1-p1r-acceptance.json)
now accepts EventStore `3.110.0` / `v3.110.0` / `27279fe6431925a6ea046c3f89af61487185c7de`
and Builds `21ce044ab465ccb2adab58b3d66e394ffbecf3c2`. G-6 remains stale and unusable for
the current toolchain. This dependency-coordinate acceptance does not accept the current P2
contract or P3 production identity, deployment, or rollback inputs; P3 owner acceptance remains blocked.

**Checks executed:**

| Command | Result |
| --- | --- |
| `python3 tools/planning/validate_production_authority.py --story-id 6.1` | Exit 0: `PASS: Story 6.1 is within production authority`. This verifies scheduling scope, not prerequisite acceptance. |
| `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug --no-restore -m:1` | Exit 1: `CS1704` in Projects Contracts because FrontComposer Shell and Contracts are imported from both package and project references. Two warnings and two errors; no tests ran. |
| `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release --no-restore -m:1` | Serialized fallback exited 1: `FoldersProjectFolderDirectory.cs:66`, `CS1501`, no five-argument `GetEffectivePermissionsAsync` overload. Zero warnings and one error; no tests ran. |
| `git diff --check` | Exit 0 after the gate-note update. |

**Read-only audit observations, requiring implementation and fresh verification after the gate is resolved:**

- Production JWT middleware fixtures inject a symmetric key and create HS256 tokens, while
  authentication registration has no explicit asymmetric-key/algorithm restriction. These fixtures
  do not prove the frozen prohibition on symmetric-key authentication in Production.
- Wrong-issuer and invalid-signature middleware negatives are absent; blank required settings and
  individual Production HTTPS guards are not independently exercised by host-startup fixtures.
- The runbook uses project-level `--filter`, while `global.json` now selects Microsoft.Testing.Platform.
  Use built-assembly xUnit `-class` filtering for focused verification once the project builds.

**Resume requirement:** the EventStore Owner, Identity/Security Owner, Projects Owner, and Solution
Architect must resolve the accepted P2 mapping/revision and configuration/rollback inputs before
runtime implementation resumes. Preserve the open P3/P4 gates and blocked Story 6.1 state.

### Authorized local continuation (2026-10-01)

The user explicitly approved continuing local P3 implementation and verification on the current
EventStore revision `6dededdecd62dd6dc6d1f15810108d860ec70c8f` while keeping owner acceptance
blocked. This supersedes the runtime-resume restriction in the preceding gate note for local
work only. It does not accept P1R/P2/P3/P4, authorize dependency-pin changes or a release, select
a production identity provider/audience, or supply deployment secrets and rollback pins.
The frozen requirements remain intact. Preserve concurrent workspace changes.

Local completion must enforce the existing prohibition on symmetric-key authentication in
Production, exercise wrong-issuer/invalid-signature and independent startup guards, and update
the fixture commands and current-baseline disposition without claiming owner acceptance.

### Local implementation and verification (2026-10-01)

**Disposition:** local implementation, independent review, and review repairs are complete; owner acceptance remains blocked. The overall P3 approval remains `in-review` because accepted immutable coordinates and owner approval have not been supplied. No staging or commit is authorized by this local continuation.
This continuation supersedes the earlier local-runtime stop and stale-assets blocker. The
historical baseline, frozen intent, existing workspace changes, and prerequisite statuses are
preserved. The earlier July review decision cannot establish acceptance for the current P2 pin.

**Changes:** authentication now requires signed asymmetric tokens outside Development, accepts
only the exact configured issuer even when discovery advertises another issuer, and suppresses
challenge error details outside Development. Declared nonblank `act` must resolve a
`DelegationId` through the existing platform helper, using the validated `sub`, or bearer
authentication fails generically. Absent and blank optional `act` retain platform unknown semantics.
Positive middleware fixtures cover all nine permitted RSA, RSA-PSS, and ECDSA algorithms with
public-only discovery keys. Production and Staging negative fixtures prove rejection of HMAC,
unsigned, wrong-issuer/audience, expired, invalid-signature, and malformed-delegation tokens
without challenge/body disclosure. Every negative protected read has an accessible seeded
Project control with otherwise-valid authorization claims.
The existing forwarding helper was moved to its own documented
`tests/Hexalith.Projects.Server.Tests/Authentication/CapturingHttpMessageHandler.cs` file.
The runbook records the current unaccepted platform coordinates, source-mode fixture commands,
ownership, and release/rollback evidence still required from owners.

**Results:** both Debug test-project builds passed with zero warnings/errors. The focused
authentication/claims lane passed **89/89**, the full Server suite passed **764/764**, and the
full Integration suite passed **27/27**, with zero errors, failures, skips, or tests not run.
The exact final commands are listed above. A targeted Debug restore cleared the earlier mixed
package/project assets; no source adapter, dependency version, or gitlink was changed.
`NuGetAudit=false` and `MinVerVersionOverride=1.0.0` were local build environment pins.
Raw run logs are in `/tmp/hexalith-projects-p3-z4npGi/`: `review-final-server-build.log`,
`review-final-focused-server.log`, `review-final-all-server.log`,
`review-final-integration-build.log`, and `review-final-all-integration.log`. These final runs
occurred after all review repairs. Temporary logs are local evidence, not a durable
owner-acceptance packet.

**Matrix audit:** every behavioral row ran in the passing focused lane:

| Matrix row | Executed covering fixtures |
| --- | --- |
| Production startup | `HostStartup_MissingOrBlankRequiredProductionConfiguration_FailsBeforeServing` (9 cases); `HostStartup_EachInsecureProductionSetting_FailsBeforeServing` (3 cases) |
| Development bypass | `HostStartup_DevelopmentBypassOutsideDevelopment_FailsBeforeServing` (Production and Staging); `HostStartup_ExplicitDevelopmentBypassWithoutOidc_Starts`; Development OIDC startup guards; `JwtMiddleware_DevelopmentOidc_RequiresAndAcceptsSignedTokensWithoutAnonymousBypass` (RS256/HS256 controls with unsigned and anonymous rejection) |
| Valid token | `JwtMiddleware_EachPermittedAsymmetricAlgorithm_AuthenticatesUsingPublicDiscoveryKeys` (9 algorithms); `JwtMiddleware_ConfiguredIssuer_RemainsValidWhenDiscoveryIssuerDiffers` (Production/Staging); `JwtMiddleware_ValidTokenAuthenticatesAndPreservesDualPrincipalClaims` (delegated/non-delegated and scope/scp); absent/blank optional-evidence controls; `TransformAndP2Envelope_ShouldPreserveDualPrincipalEvidenceAtEventStoreBoundary`; unchanged-token forwarding fixture |
| Invalid identity | `JwtMiddleware_InvalidCredentials_AreRejectedWithoutDisclosingProtectedMetadata` (12 Production/Staging cases); symmetric-signature negatives (3 algorithms in both environments); `JwtMiddleware_DeclaredMalformedDelegation_FailsAuthenticationAndDeniesProtectedRead` (10 Production/Staging cases with direct-helper unknown evidence); missing-permission and cross-Tenant safe-denial fixtures; each has a seeded accessible Project control |

**Remaining acceptance evidence:** exact owner-approved P2/P3/P4 revisions and package pins,
deployment-managed configuration/secret references, and executable rollback selection remain
external. Tests used current source working trees, including preserved concurrent EventStore
edits; they do not establish immutable or Release package acceptance. The isolated Aspire
baseline command `aspire start --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --non-interactive --isolated --format Json`
exited 2 after 120 seconds during restore. `aspire stop` and `aspire describe` confirmed that no
AppHost remained running. No live Keycloak/G-4 proof was produced. The historical FrontComposer
pointer finding remains preserved; no branch, staging, commit, push, or pin change was performed.

**Local review scope:** the authorized continuation starts from Projects HEAD
`4d8dcf65803792f7def3b10ed21227329536154b`. The original `baseline_commit` remains
unchanged; its complete historical diff (including untracked workspace files) was archived at
`/tmp/p3-historical-baseline-e36ezx7c.diff`. That historical diff spans months of unrelated committed work. Independent reviewers examined the five current P3 source/test/documentation files changed by
this continuation, with CRLF-only differences normalized. The reviewed diff at
`/tmp/p3-local-review-iigbn29y.diff` was rewritten after repairs to reflect the final tree.
Concurrent P0/P1R artifacts and EventStore edits are excluded from the local change review and
preserved. Review does not certify the historical implementation or immutable owner acceptance.

## Review Triage Log

Three independent review lenses examined the authorized local continuation. The edge-case lens
reported no findings. Each submitted substantive finding is recorded separately before grouping.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind B1: other permitted asymmetric algorithms have no positive middleware fixture | medium | The current success fixture signs only RS256. A missing RSA-PSS/ECDSA allowlist entry would silently break a documented supported caller while these tests remain green. Patch group R1. |
| Blind B2: environment branches are unexercised in Staging/normal Development OIDC | medium | The private middleware host always selects Production; the Staging startup test checks only bypass rejection. Branch regressions can weaken Staging protections without detection. Patch group R2. |
| Blind B3: configured issuer against differing metadata has no successful control | medium | The differing-metadata case currently checks rejection only, so accidental blanket rejection under metadata drift is undetected. The exact configured issuer is the accepted host contract. Patch group R3. |
| Blind B4: scope/scp precedence has no corresponding fixture | false | The platform-owned `DualPrincipalClaimsHelperTests.Extract_ScopeAndScpBothPresent_PrefersScope` and `Extract_ScopeAbsentScpPresent_FallsBackToScp` cover this contract; its helper explicitly prefers nonblank scope and falls back to scp. P3 changes no scope mapping. Reject; no platform test duplication or mapping change is required. |
| Blind B5: malformed/absent optional identity evidence lacks JWT-pipeline coverage | medium | Pure principal coverage ran, but the updated middleware identity fixture exercises only well-formed or absent act with azp/scopes present. A token-decoding integration regression could distort unknown optional evidence. Patch group R4, preserving the platform's mandatory-audience workload fallback. |
| Blind B6: rejected-token protected reads have no available authorized-data control | medium | Several invalid tokens contain only sub, and the default fixture does not seed an accessible Project. Its redacted 404 alone cannot distinguish authentication rejection from later lack of authorization/data. Patch group R5: otherwise-valid claims and seeded authorized read. |
| Blind B7: the spec omits the restoring command before no-restore builds | low | The runtime runbook includes the restoring Debug build and environment properties, but the spec lists only final incremental builds. Reject as a finding whose proposed fix edits this build's spec; retain the initial restoring command as ordinary verification evidence. |
| Verification V1: permitted signing families lack positive verification | medium | Pre-verified gap: removing RSA-PSS/ECDSA policy entries would pass the checked positive and negative fixtures. Same root cause as B1; patch group R1. |
| Verification V2: Staging signing/challenge restrictions lack verification | medium | Pre-verified gap: replacing IsDevelopment with IsProduction would keep current tests green while weakening Staging. Same root cause as B2; patch group R2. |

Groups R1–R5 required fixture-only patches with no new public surface or production policy
change. Root R6 then exposed a protected-data disclosure for malformed declared delegation;
its authentication guard enforces the existing frozen denial requirement using the platform
parser. Owner acceptance remains blocked; no review finding authorizes a release or platform
pin/mapping change.

**Restoring Debug commands used before incremental verification:**

- `dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` cleared stale assets; its first compile exposed fixture attribute errors that were corrected before the passing builds.
- `dotnet build tests/Hexalith.Projects.Integration.Tests/Hexalith.Projects.Integration.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` restored and built the integration project successfully.

**Root R6 — high; patch:** the seeded `JwtMiddleware_UnknownOptionalEvidence_PreservesPlatformFallbacks` fixture passed a protected Project read for nonblank malformed `act` declarations, returning the seeded name/setup metadata. This demonstrated a violation of the frozen Invalid identity matrix. The repaired bearer validation rejects declared nonblank delegation whose platform-parsed `DelegationId` is unknown, preserving the platform parser, optional absent/blank semantics, and the public read route. Ten Production/Staging fixtures now prove rejection without protected-data or claim disclosure.

**Repair results:** R1–R5 added positive coverage for all permitted algorithms, environment branches,
configured-issuer discovery drift, absent optional evidence, and accessible Project controls.
R6 added the minimal bearer identity guard. The implementation agent ran only the authentication
class after each repair; the final class passed 81/81. The primary agent then rebuilt both test
projects and reran the complete verification set recorded above. All local review findings are
patched or rejected with evidence; none was added to the deferred-work ledger. The pre-existing
historical FrontComposer pointer finding and external acceptance/live-run blockers remain as
recorded. `git diff --check`, frozen-intent/baseline comparisons, documentation links, and required
CRLF checks for all five changed files passed.


### Resumed review (2026-10-01)

All three review lenses reported before triage. The full original-baseline diff,
including untracked files, is `/tmp/hexalith-p3-resume-f_z6yb9n/baseline.diff`;
its 40,422,720 bytes include months of unrelated work. Review scope remains P3.
Each finding is recorded independently before grouping.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Resume Blind B1: audience trailing-slash alias is accepted | high | Installed IdentityModel 8.23.0 documents the default slash-insensitive audience match. The configured audience is an exact identity boundary. Patch group S1; add protected-read negatives before setting the explicit strict flag. |
| Resume Blind B2: missing/blank/multiple nondelegated subjects can authenticate | high | The bearer identity guard requires a subject only for declared delegation. A raw name-identifier alias can supply the seeded authorized actor without a valid canonical subject. Patch group S2; require one nonblank subject for every bearer identity. |
| Resume Blind B3: subject/name-identifier conflict changes the authorized actor | high | The transformer and REST accessor prefer NameIdentifier, while the real QueriesController uses sub only. A signed conflicting alias can select the seeded actor for REST. Patch group S2; reject conflicting aliases without changing the platform mapping. |
| Resume Blind B4: secondary identities can contribute accessor authority | maybe-false | The accessor aggregates claims; however, this host's bearer handler produces one identity and no current producer of the described unauthenticated secondary identity was found. The accessor behavior predates the original P3 baseline. Defer as an unverified medium issue; a reachable additional-identity producer or an explicit accessor contract would settle it. |
| Resume Blind B5: malformed array-shaped permissions fall back to action text | high | The unchanged fallback emits projects:read from malformed text containing that standalone action. P3's fail-closed normalization review exposes this path. Patch group S3; demonstrate protected disclosure, then stop parsing malformed array-shaped evidence as whitespace permissions. |
| Resume Blind B6: UI OIDC startup can silently omit authentication | medium | The UI conditional exists, but P3 intent explicitly targets Hexalith.Projects.Server; the independently shippable authenticated Web surface belongs to Story 6.5. Rejected as outside this intent, with no UI edits. |
| Resume Blind B7: local AppHost fixture secret becomes production configuration | maybe-false | The value is explicitly a local E2E UI credential, and the inspected graph is the Development fixture. No production deployment consumer or approved production resource graph was identified. Defer as an unverified medium issue; inspect the actual deployment graph and external-secret binding when owners supply them. |
| Resume Blind B8: static discovery fixtures do not prove JWKS retrieval/rollover | low | These deterministic fixtures exercise the changed host policy; retrieval and rollover use the unchanged framework configuration manager. No discovery regression was identified. A new network/JWKS harness is more than a direct correction and lacks an everyday defect in this change; rejected. Live/provider acceptance stays separately open. |
| Resume Blind B9: manually assembled envelope misses real gateway mapping | medium | The existing P3 test constructs QueryEnvelope directly. Platform QueriesController tests cover the mapping, but a signed-token consumer fixture would verify the real public boundary and relay together. Patch group S4; use the real controller and unchanged forwarding handler without duplicating extraction logic. |
| Resume Blind B10: live curl controls omit a seed procedure | medium | The runbook requires seeded membership/Project evidence but provides no procedure link. The existing managed E2E runner and seededProject fixture supply that setup and cleanup. Patch group S5; link the executable fixture procedure and label manual curl prerequisites. |
| Resume Edge E1: trailing-slash audience permits a distinct token audience | high | Same verified IdentityModel default as B1. Patch group S1. |
| Resume Edge E2: conflicting subject alias authorizes a different actor | high | Same REST/platform identity split as B3. Patch group S2. |
| Resume Verification V1: invalid OIDC URI shapes have no startup regression fixture | medium | Pre-verified searches found no URI-shape fixture. Removing user-info/query/fragment checks would leave current startup tests green. Patch group S6; exercise both settings with prohibited URI shapes and no listening URLs. |

Groups S1-S6 are local implementation/fixture/documentation repairs with no new
public surface or platform pin/mapping selection. B4 and B7 remain unverified
external/pre-existing concerns. Owner acceptance and Story 6.1 gates remain open.


**Resumed repair results:** the pre-repair regressions ran 24 cases and failed
10: two trailing-slash audiences, six missing/blank/conflicting-subject cases,
and two malformed-permission protected reads. The current JWT reader already
rejected the two duplicate-subject cases; the host now makes its single-subject
contract explicit. The malformed permissions returned the seeded Project before
the parser correction. S1-S3 now enforce exact audience matching, a canonical
nonblank subject with matching aliases, and no grants from malformed arrays.
S4 relays real signed tokens over HTTP into the actual QueriesController and
real claims validators; its one test-only mediator handler captures the resulting
SubmitQuery without persistence. S5 links the existing managed seeding/cleanup
procedure. S6 covers ten invalid Authority/Issuer URI configurations at startup.
The gateway fixture uses existing framework/platform dependencies; no package
reference, version pin, or submodule revision was changed.

**Final verification:** focused authentication/claims **117/117**, full Server
**792/792**, and Integration **27/27** passed with zero failures, errors, skips,
or tests not run. Both final Debug builds passed with zero warnings/errors,
using the commands in Verification above. The focused lane ran after runtime
repairs; both full suites ran after that final source build. The retained
[evidence manifest](evidence/6-1-p3-local-2026-10-01/manifest.json) records the
local source/assembly hashes and compressed raw command results. An initial
gateway-fixture build found that NSubstitute was not referenced by this test
project; the fixture was corrected to use the real mediator and platform claims
validators without adding a dependency. The subsequent build and all final
checks passed.

**Runtime baseline:** `aspire start --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --no-build --isolated --non-interactive --format Json`
exited 0. `aspire describe` reported Projects, EventStore, Tenants, workers, UI,
security, and Dapr resources Running/Healthy; `aspire wait projects --timeout 30`
with the same AppHost and non-interactive flags exited 0. `aspire stop` exited 0
and final describe confirmed no running AppHost. The successful startup replaces
the earlier restore-timeout observation for this local baseline, without proving
persisted authenticated G-4 or production deployment/rollback acceptance.

The preserved original frozen intent and baseline remain authoritative. The
spec remains `in-review`, `local_implementation: complete`, and
`owner_acceptance: blocked`; approval prerequisites and sprint status were not
changed by this run. Concurrent edits to planning artifacts and sibling working
trees appeared during verification and remain user-owned. All three repository
HEAD coordinates remained the recorded values. No staging, commit, push,
production provider/audience selection, or dependency-pin change was performed.
Two unverified medium concerns, B4 and B7, were appended to the deferred ledger;
all local S1-S6 repair groups are complete.

### Current review verification (2026-10-01)

All three review lenses completed before this triage. The complete original-baseline
unified diff is `/tmp/hexalith-p3-review-gv01d9g6/baseline.diff` (37,053,607 bytes,
including untracked files). Review remained scoped to P3; unrelated historical and
concurrent changes are preserved. The current continuation diff is
`/tmp/hexalith-p3-review-gv01d9g6/local-p3.diff`.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Current Blind B1: object-shaped permissions become whitespace grants | high | `{ invalid projects:read }` is split into a valid Projects action. The seeded middleware regression will verify disclosure before the smallest parser correction. Patch group T1. |
| Current Blind B2: a null element in a stringified permission array leaves valid grants | high | `JsonSerializer.Deserialize<string[]>` retains a null array member; the current loop skips it and promotes `projects:read`. Verify against the seeded protected read, then reject the invalid array. Patch group T1. |
| Current Blind B3: duplicate raw subject properties select the final value | false | RFC 7519 section 4 explicitly permits a parser that selects the last duplicate member. The framework supplies one resulting subject, and both Projects and the real query controller consume that same value. No divergent consumer or violated raw-property policy was demonstrated; retain the existing platform parser. |
| Current Blind B4: numeric subjects become string identities | medium | The JWT decoder accepts numeric `sub` and presents its text as an actor; RFC 7519 section 4.1.2 defines a string subject. This exposes the host's required canonical subject guard. Patch group T2: prove the middleware outcome and require the validated payload's subject to retain its string type. |
| Current Blind B5: duplicate raw act properties discard the earlier malformed value | false | The permitted last-member JWT policy also applies to raw `act`. The resulting declaration is valid and is parsed by the unchanged platform helper; multiple effective nonblank act claims and duplicate inner act.sub members still produce unknown delegation and denial. No different Projects/platform decoding was demonstrated. |
| Current Blind B6: forwarding fixtures do not traverse registered gateway DI | low | The actual fixture deliberately captures the public query identity boundary. Gateway DI and its forwarding handler predate the original P3 baseline and are unchanged; no current miswiring was demonstrated. Reject speculative added integration-harness complexity. Downstream live/G-4 acceptance remains open. |
| Current Blind B7: static discovery cannot prove network retrieval or rollover | low | carried: the same location and claim were rejected in Resume Blind B8. The default framework configuration manager remains unchanged, and live/provider acceptance is still explicitly open. No new harness or policy change. |
| Current Blind B8: scope/scp precedence lacks a forwarded combined-claim fixture | false | carried: the same platform mapping is covered by its own DualPrincipalClaimsHelperTests for precedence and fallback, as recorded in Blind B4. P3 does not change that mapping; its signed forwarded fixtures cover each spelling. |
| Current Blind B9: future-nbf, missing-exp, and skew edges lack host fixtures | low | Framework lifetime validation remains enabled and the explicit one-minute skew is unchanged. Existing expiry rejection and strict-parameter assertions exercise the host adoption. No reachable lifetime defect was shown; reject additional framework-boundary fixture complexity. |
| Current Blind B10: runbook omits downstream token audience compatibility | medium | Forwarding preserves the original token, so independent Projects and EventStore audience choices can deny downstream requests. Add the compatibility obligation and required downstream authorized control without selecting a new audience or approving a deployment. Patch group T3. |
| Current Edge E1: a typed JWT permissions array retains grants alongside nonstring elements | high | The JWT decoder expands the array into claims; AddClaimsFromJwt checks values but ignores their ValueType. A seeded middleware regression will verify the real read before requiring all source evidence to be string-typed. Same root cause as B1/B2; patch group T1. |
| Current Edge E2: malformed optional act is rejected instead of remaining unknown | false | The unchanged platform helper still returns an unknown DelegationId for malformed evidence. The authorized local R6 repair then denies it before protected disclosure, as required by the frozen invalid-identity row; absent/blank optional act remains accepted. No platform mapping or frozen-intent edit is needed. |
| Current Verification V1: blank-subject rejection is masked by the alias guard | medium | Pre-verified gap: the existing blank-sub fixture includes alias actor-a, so changing IsNullOrWhiteSpace to IsNullOrEmpty still rejects via alias mismatch. Add missing/blank-subject fixtures without aliases in Production and Staging. Patch group T2. |

RFC verification: [JWT claim decoding and subject requirements](https://www.rfc-editor.org/rfc/rfc7519#section-4).
T1-T3 are local repairs with no new public surface, platform mapping selection,
dependency-pin change, or production owner approval. Frozen intent, baseline,
Story 6.1 blocking state, and external acceptance prerequisites remain intact.

**Current repair results:** T1 rejects unsupported object values, stringified arrays
containing null, and decoded arrays containing nonstring elements before any
source evidence is promoted. Production and Staging seeded controls remain
accessible for valid string permission arrays. T2 checks the original signed
subject type after framework validation, without replacing the platform claims
or delegation parser; numeric subjects fail with the same bare bearer challenge.
Missing and whitespace subjects without aliases now exercise their own guard.
T3 documents that unchanged forwarded tokens must satisfy both Projects and
EventStore audiences, with a required downstream authorized control at release.

The pre-repair lane ran 26 cases and reported 10 failures: six malformed
permission reads disclosed the seeded Project, two numeric subjects authenticated,
and two initially mislabeled null cases emitted an empty string rather than JSON
null. The null fixture was corrected to serialize an actual null and assert its
payload type; the supported empty-string behavior was not tightened. The first
focused repair lane reported those two fixture failures; the corrected final lane
passed. Both intermediate logs are retained separately from final passing evidence.

**Final current verification:** focused authentication/claims **133/133**, full
Server **808/808**, and Integration **27/27** passed, with zero errors, failures,
skips, or tests not run. Both final Debug builds passed with zero warnings/errors.
The exact commands remain those in Verification above. The new
[evidence packet](evidence/6-1-p3-review-2026-10-01/manifest.json) records final
source/assembly hashes, exact command arguments, review dispositions, and
compressed logs. The earlier local packet is preserved as historical evidence.
No new findings were deferred; prior deferred B4/B7 concerns remain unchanged.

**Runtime and acceptance:** isolated Aspire startup with `--no-build`, a healthy
Projects wait, and resource inspection succeeded before fixture/runtime edits;
all twelve observed resources were Running/Healthy. Aspire was stopped before
rebuilding, and final describe confirmed no running AppHost. This is local startup
evidence, not persisted authenticated G-4 or production rollback certification.
P1R now separately accepts its published 3.110.0 tuple; the current P2 owner
handoff still explicitly remains draft and unaccepted. No P3 acceptance decision,
production configuration/secret references, or executable rollback selection was
supplied by this invocation. Accordingly, the spec remains `in-review`, local
implementation is complete, owner acceptance is blocked, and Story 6.1 remains
blocked. The generic Build terminal done/sprint-sync/commit actions were not
applied because the recorded local authorization excludes acceptance transitions
and staging/committing, and existing user-owned changes must be preserved.
