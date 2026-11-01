---
title: 'Preserve bound bootstrap when the SDK configures infinite HTTP timeout'
type: 'bugfix'
created: '2026-11-01'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 1
baseline_commit: '48ba1fcb74779e8dc9dd4851d55e1055762eebab'
context: []
---

## Intent

The user authorizes fixes to all CI failures, latest-main submodule synchronization and release, without a human validation checkpoint. Nested submodules must remain deinitialized. Work in references/Hexalith.Tenants, read owning guidance and the workspace baseline, and never load a skill under references. Parent owns commits/push and the build workflow; implement only this scoped correction, preserve all other edits, and do not stage, commit, push or initialize dependencies.

## Evidence

Source-reference run41331746531 job124923135003 fails the actual source owner profile with bootstrap-command-timeout. Full logs /tmp/tenants-source-owner-failed.log show TenantBootstrapHostedService logs ArgumentOutOfRangeException during bound-administrator-validation at17:48:23.461. TenantBootstrapBoundCommand.CreateAsync rejects readinessTimeout <=0 while its production caller passes the SDK-managed HttpClient.Timeout, which can be Timeout.InfiniteTimeSpan under resilience handlers. The API accepts this legitimate timeout but the bootstrap never reaches catalog readiness or its owned receipt. Normal source identity, credentials, authority readiness and receipt gates must be preserved.

## Tasks & Acceptance

This continuing repair also adopts the current qualified SDK through the normal package default and release consumer, as detailed in Revised SDK131 implementation tasks below. The completed timeout/body fixes stay intact; current source/package/CI/release-path qualification is part of the user-authorized CI and dependency maintenance.

Inspect the real SDK HTTP defaults and confirm this cause. Correct the existing internal bound-bootstrap timeout handling to accept the legitimate infinite transport timeout while keeping catalog readiness bounded and caller cancellation effective; retain normal positive finite behavior and invalid-timeout refusal. Prefer the default HttpClient finite timeout (100seconds) for the readiness fallback, not an arbitrary long value. Do not disable source profile, relax authentication/catalog rules, weaken CI gates, invent receipts or extend fixture timeouts to hide it. Add meaningful regression coverage in the existing bound-bootstrap tests for finite/infinite/invalid inputs and cancellation; exercise the production helper rather than merely mirroring arithmetic. Build Debug/source and run only relevant individual built xUnit classes; record exact evidence. No live topology or shared Docker changes without parent coordination; parent owns final hosted profile verification.

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

## Confirmed request replay defect and correction

Run41342552395 at39243fcd0a770dbb9244950ac77baac486cfd0a5 reports HTTP400/problem-validation-error/none/ValidationField=request. The same actual invocation first reaches the gateway handler/actor, then Dapr logs an internal retry after its configured5-second timeout. This retry is inside caller Polly attempt0. The SDK contracts/handlers decompile identically across observed child output closures; no ABI or payload-casing defect is demonstrated.

Primary pinned sources explain the missing-body replay: Dapr1.18.7 pkg/api/http/directmessaging.go marks ContentLength<0 requests streaming and disables request replay; pkg/resiliency/policy.go outer timeout returns ctx.Err and can retry around that streaming invocation; pkg/messaging/v1/replayable_request.go returns the consumed reader when replay is disabled. .NET10 JsonContent.TryComputeLength returns false. Current TenantBootstrapBoundCommand.CreateAsync uses unbuffered JsonContent.Create(bound). Links: https://raw.githubusercontent.com/dapr/dapr/v1.18.7/pkg/api/http/directmessaging.go ; https://raw.githubusercontent.com/dapr/dapr/v1.18.7/pkg/resiliency/policy.go ; https://raw.githubusercontent.com/dapr/dapr/v1.18.7/pkg/messaging/v1/replayable_request.go ; https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Net.Http.Json/src/System/Net/Http/Json/JsonContent.cs .

Implement the smallest scoped producer correction in TenantBootstrapBoundCommand.cs: materialize the small exact bound command JSON to a known-length body before dispatch (buffered content or ByteArrayContent with unchanged JsonSerializerOptions.Web/content type). Preserve the signed object/bytes, requester/operation, all authority checks, cancellation, receipt policy and existing Dapr5-second timeout/retry configuration. Do not raise timeouts, disable retries, add an unsafe replay loop, or broaden SDK changes speculatively. Add meaningful regression coverage in the existing helper suite that inspects Content-Length BEFORE any read (which itself can mask JsonContent buffering), preserves JSON/proof roundtrip, and observes actual loopback HTTP framing/no chunked transfer plus repeated identical body bytes. A focused actual Dapr timeout/retry probe is desirable, but may touch only a fresh owned temporary loopback app/sidecar namespace and no shared backend; parent coordinates it separately. Parent owns commits/push/rootpin/hostedsourcequalification. No nested initialization.

## Actual Dapr retry proof

The isolated actual Dapr1.18.7 probe reproduced the defect under the unchanged5-second timeout and1-second retry policy. Streamed JsonContent sent304 bytes first, then0 bytes on retry and returned400 after6052ms. Known-length Web JSON sent identical304-byte bodies on both attempts (SHA256 52e403de16556a074bdba94422aff88a8e1cb0bb02e2757ea9737aeea2ecc8ae) and returned202 after6004ms. Both cases forced the first callback to6.5seconds. Only an owned loopback self-invocation sidecar and synthetic app were used, with a unique namespace and no backend, state store, actors or containers. All owned processes stopped and the temporary probe directory was removed. Credential-free framing/hash/status/cleanup evidence: release-evidence/tenants-dapr-replay-proof-2026-11-01.json.

## Request-replay Review Triage Log

- Blind1 — low, rejected: the automated loopback test proves the producer advertises a known length before any read and sends identical bytes twice; it does not claim to execute Dapr. The separately retained actual pinned-Dapr probe reproduces empty streamed retries and successful known-length retries. Adding another live Dapr harness for hypothetical future runtime behavior is more than a direct correction and is not needed to verify the current producer defect.
- Blind2 — low, rejected: removal of the new failure-path disposal would escape the existing assertions, but the request has not been sent and owns only its small in-memory content. No leaked socket, backend operation or user-visible failure is demonstrated. Observing this private request would require a new factory/interception seam for a rare configuration failure; the catch already disposes the request.
- Blind3 — low, patch: Kestrel endpoint configuration can override UseUrls in the new test listener. Clear ambient configuration before configuring the owned loopback endpoint; this is a direct fixture isolation correction.
- Blind4 — low, patch: the production request carries both workload assertion and requester proof, but the new receiver checks only the proof. Capture and compare the unchanged assertion header on both sends.
- Edge-case hunter — no findings.
- Verification-gap reviewer — no gaps; the real caller sends the returned request directly, pre-read length assertions detect removing buffering, both loopback cases check real framing/body/proof, and the normal CI integration tier runs this class.

## Final request-replay implementation verification

Commit48ba1fcb74779e8dc9dd4851d55e1055762eebab is pushed to Tenants main. The final full Debug/source test-project build with the global EventStore3.131.0 identity passed with zero warnings/errors. Parent independently ran24/24 bound-command and205/205 hosted-bootstrap tests, with no failures or skips. Both real wire cases also pass with deliberately invalid inherited Kestrel endpoint configuration. All three review layers completed; the two direct fixture corrections are applied and the other two low-impact proposals are documented above. The actual pinned-Dapr retry proof is retained independently. Hosted exact-source main/source profile qualification is pending (Source-reference integration run41345152608); no gate was relaxed.

## Catalog SDK continuation

After the latest-main catalog advanced to the independently published EventStore3.131.0 cohort, source run41345152608 fails during build (both jobs), before the live bootstrap. Its exact qualified-SDK allowlist ends3.130.2. Foreign root projects inherit Tenants Directory.Build.targets without a source-EventStore marker and legitimately trigger the unchanged qualification refusal. Full logs /tmp/projects-release-maintenance/tenants-body-fix-source-failed.log. Add the explicit stable3.131.0 selection through the existing narrow SDK qualification mechanism, with actual published API/contract verification and relevant current-matrix tests. Preserve all unknown-version/legacy refusal controls; do not remove or broaden the gate. Keep the verified request-body correction, authority checks and receipt assertions intact. Parent owns Git operations and final hosted rerun.

## SDK131 Review Triage Log

- Blind1 — medium, bad_spec: the user requested current packages, but the scoped continuation retained the default3.129.0 selection and only qualified3.131.0 synthetically. Published compiled enrollment must be continuously tested for the adopted default3.131.0; retain explicit older-version compatibility lanes.
- Blind2 — low, patch (subsumed by re-derivation): accepted selector cases assert versions but not the emitted catalog properties/constants. Assert qualified binary enrollment and absence of source-cohort-only compilation independently of mere gate acceptance.
- Blind3 — low, patch (subsumed by re-derivation): the qualification claim has actual passing public-binary tests/provenance under/tmp, but no reproducible owning-repository command/evidence link. Retain concise credential-free proof and current public qualification commands in the owner repository.
- Edge-case hunter — no findings.
- Verification1 — medium, bad_spec: pre-verified absence of a normal published3.131.0 compiled-enrollment lane. Existing exact-floor3.129.0 coverage remains useful, so retain it while verifying the newly adopted default3.131.0.
- Verification-other1 — medium, bad_spec: the active package-consumer verifier accepts only3.129.[01], and both release preparation scripts call it. Matching3.131.0 Contracts/Server archives fail before restore, so the newest qualified cohort cannot release. Extend its finite known-SDK normalization consistently and exercise actual packed candidate consumption, preserving source/SHA/hash/route checks and unknown/mixed/prerelease refusal.

## Spec Change Log

- SDK131 review loop1: the first narrow whitelist patch left the release path and default package selection on the old cohort. Revert only those three uncommitted SDK configuration/test/doc files to48ba1fcb; preserve the committed buffering correction and current Builds gitlink. Amend the continuation to adopt3.131.0 by default, run its existing compiled HTTP/catalog assertions in normal CI, and permit it through the active package-consumer verifier. KEEP the exact stable allowlist, normalized zero-revision/metadata aliases, refusals for unknown/prerelease/nonzero revisions, legacy explicit compatibility and nonpublishability, all existing auth/catalog/receipt checks, and actual public archive provenance. The frozen user intent already requests latest compatible packages and successful publication; no human clarification is needed.

## Revised SDK131 implementation tasks

Adopt the independently verified stable3.131.0 as Tenants default SDK while honoring explicit compatibility overrides. The normal current package CI lane must exercise published3.131.0 compiled enrollment, owner catalog and generated HTTP behavior; keep the existing explicit3.129.0 floor lane and frozen3.128.4 harness. Make the active package-consumer verifier accept the same finite already-qualified stable SDK selections (including3.131.0 and their established normalized forms), preserving rejection of unknown/prerelease/mixed cohorts, exact source metadata, archive hash checks and isolated real-API catalog discovery. Add relevant existing script/MSBuild regression cases and run actual freshly packed Contracts/Server candidates against the actual3.131.0 public SDK. Retain concise owning-repository qualification commands/evidence. Preserve all completed bootstrap behavior and no nested initialization. Parent owns commits/push and hosted verification.

## Revised SDK131 local verification

- [x] Default3.131.0 published package Contracts/Integration builds,104 selector/default/floor/catalog tests and66 generated HTTP tests pass.
- [x] Explicit3.129.0 clean builds,66 HTTP and3 compiled owner-catalog assertions pass.
- [x] Finite archive normalization/refusal coverage passes6 tests including24 accepted alias subcases; ordinary stale/legacy/foreign-owner pack guards pass2/2.
- [x] Fresh actual Contracts/Server candidates pass isolated public-API consumer/source/archive/DLL checks and discover22 routes,22 operations,12 adapters.
- [x] Parent verified final full Debug/source Integration build completed with zero warnings/errors. Bootstrap implementation/tests are unchanged from48ba.
- [x] Owning documentation retains reproducible commands and credential-free working-tree qualification proof; actionlint and diff checks pass.
- [ ] Hosted committed-source main/source CI and final repository/release qualification remain parent-owned.

## SDK131 release-path Review Triage Log

- Blind1 — medium, defer: lower-bound-only range parsing and absence of a resolved EventStore closure check both predate this change. An exclusive minimum can be labeled as the excluded cohort, so this verifier hardening is real; current fresh candidates use ordinary inclusive3.131.0 dependencies and pass actual isolated consumption. Record the preexisting malformed-range/closure gap separately rather than expand this stable-cohort adoption.
- Blind2 — low, defer: the temporary consumer has never copied global.json. Current observed consumer SDK is10.0.403, exactly the repository pin, and all other installed SDKs are older; current qualification is valid. A future newer installed SDK could change this preexisting selection behavior. Record the explicit SDK-pin follow-up without relabeling current evidence.
- Blind3 — low, patch: new reproduction commands use direct runners without the CI discovery/skip guards. Document the existing guarded invocation with minimum66 HTTP and1+2 catalog cases and skipped-test refusal.
- Blind4 — low, patch: unknown legacy3.128.x builds can pass the old floor while remaining nonpublishable. Scope the new unknown/prerelease refusal wording precisely to catalog qualification.
- Blind5 — false: scripts/verify-catalog-package-consumer.py prints the complete receipt JSON to retained job logs. Each matrix job has its own named profile; the source/archive/DLL/discovery evidence survives runner cleanup in those logs. An additional artifact upload is optional convenience rather than missing proof.
- Blind6 — low, patch: the record honestly labels a local uncommitted candidate and its base, but a working-tree patch digest plus declared Builds revision directly identifies its inputs. Add these fields without claiming a clean committed-source result.
- Edge-case hunter — no code findings. Its named-section claim mismatch is a historical-spec organization issue; the full revised SDK tasks and exact user intent governed implementation.
- Verification-gap reviewer — no gaps; normal current/default and explicit-floor lanes compile actual enrollment/catalog/HTTP behavior and consume fresh packages.

## Pushed SDK131 continuation

Tenants331c528219e921e990402a6134fed9bfc50f0441 is pushed with the current default, finite qualification, explicit older-floor CI matrix, active archive consumer and documented local proof. Parent recomputed the working-tree patch digest and verified the declared Builds gitlink before committing. Documentation/evidence-only review corrections pass scoped checks. All local package/source/consumer assertions are complete; fresh hosted main/source CI is pending.

## Full package-cohort continuation

Hosted main331c5282 CI41347795557 job124971250052 passes the complete Release build, then its all-package gate rejects Hexalith.Tenants.Aspire.0.0.0-ci-test.nupkg because its dependency closure now includes Hexalith.EventStore.ServiceDefaults. The actual official EventStore.Aspire3.131.0 nuspec requires that exact technical SDK package at3.131.0; CPVM transitive pinning promotes it into the consuming Aspire archive. Current validator has an exact technical allowance only for Tenants.Server. Logs /tmp/projects-release-maintenance/tenants-331-main-failed.log. Inspect the complete actual five-package candidate graph, repair only the demonstrated legitimate technical dependency boundary at its owning package, and preserve domain ServiceDefaults/host/sample/test/unknown/lookalike refusals plus exact range/restore/source validation. Do not remove required SDK behavior, disable the gate, globally whitelist ServiceDefaults, or change runtime/bootstrap code. Validate fresh complete Release/package solution and pack/validate all five declared packages, not only Contracts/Server, using the existing package gate and relevant boundary regression cases. Parent owns commits/push and final root gitlink qualification.

## Post-replay actual runtime continuation

Source run41347795093 at331c5282 passes source builds and projection provenance, then fails the actual owner-profile fixture with bootstrap-command-timeout after2m33s. Full runtime log /tmp/projects-release-maintenance/tenants-331-source-runtime-failed.log. The bounded streamed-body validation400 is gone; bootstrap event2002 now records Phase=command-dispatch, ExceptionType=TimeoutRejectedException at20:49:19.393 (dispatch around20:48:49). Preserve the demonstrated known-length replay fix, all readiness/authority/admission/receipt/persisted-effect checks and existing policies. Investigate the actual gateway/actor/transport timelines and identify the concrete stalled operation; do not invent authority or receipts, disable retries, relax the fixture or arbitrarily increase budgets. Read the state baseline before any persistence change. No shared local backend mutation; isolated read-only/static/focused request tests first. Parent coordinates final implementation and hosted rerun.
