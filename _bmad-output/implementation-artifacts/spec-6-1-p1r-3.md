---
title: '6.1-P1R Establish a supportable successor qualification path'
type: 'bugfix'
created: '2026-10-10'
status: 'done'
baseline_commit: '827904d7b403d77f1a6d44437d212183ec91fcaa'
route: 'dispatch'
review_loop_iteration: 1
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1-p1r-remediation.md'
  - '{project-root}/references/Hexalith.EventStore/_bmad-output/specs/spec-6-1-p1r-remediation/qualification-contract.md'
---

<frozen-after-approval reason="human-owned P1R scope and supported envelope">

## Intent

**Problem:** The 3.119.0 published packet is valid but nonqualifying. Stale-fence refusal is unproved, logical alias execution is missing, and immutable old packages fail required comparison directions. P1R remains unusable.

**Approach:** Diagnose candidate-specific results, repair demonstrated defects, decide the supported boundary for old packages, then qualify an owner-selected published successor on PostgreSQL/Dapr 1.18.2. Present independent evidence for four owner decisions.

**Decision (2026-10-10):** Pursue an architect-approved support matrix from measured safe directions; retain failures and refuse incapable routes. This build repairs source/fixtures and proposes the matrix. Keep the gate unchanged until approval. No successor tuple, authoritative pin or publication scope is authorized; stop before a published run. P1R remains unusable.

## Boundaries & Constraints

**Always:** Preserve sealed packets, October 1 acceptance, event bytes, retained floor, sequence, Tenant isolation, one writer, loaded package identity and measured checks. Keep historical failures visible. With `rollback=null`, use Projects AD-17 mutation freeze and forward recovery. Publication, pins and acceptance retain separate owner gates.

**Never:** Count an opaque exception as a proved refusal, infer alias execution from metadata, rebuild an existing version, omit failed directions silently, advance Projects readiness from source evidence, or deploy without authorization.

## I/O & Edge-Case Matrix

| Case | Input | Expected behavior |
| --- | --- | --- |
| Stale fence | Sequence 12; stale context and forged proof | Both deny with unchanged persisted state; opaque transport error stays nonpassing until diagnosed. |
| Logical alias | Registered old event and supported reader | Real alias replay preserves original envelope; unknown version refuses before application. |
| Old packages | 3.70.1 and 3.110.0 | Retain actual losses/refusals; incapable operations fail closed within an approved support boundary. |
| Recovery | Floor 5/head 12/snapshot 9; two Tenants | Restore, append 13 and restart preserve floor, hashes and other Tenant; otherwise freeze mutation. |

</frozen-after-approval>

## Code Map

Paths below are relative to `references/Hexalith.EventStore` unless stated otherwise.

- `tools/p1r-published-consumers/host/QualificationCapabilities.cs:79` routes both fences; `ExpectedDenial.cs:14` matches exact refusal. The sealed stale path is opaque; forged proof has a nested denial. Diagnose before changing the matcher.
- `tools/p1r_published_executor.py:1014,1287` checks refusal and persisted inventory; evolution proves only V1 hydration and hardcodes alias execution false. Preserve raw outcomes.
- `tools/p1r-published-consumers/domain/Program.cs:5` registers bounded V1 only. Reuse `src/Hexalith.EventStore.Client/Registration/EventEvolutionServiceCollectionExtensions.cs:64` and `tests/Hexalith.EventStore.Client.Tests/Registration/EventEvolutionManifestRegistrationTests.cs`. A test fingerprint grants no gateway authority.
- `tools/p1r_published_qualification.py:306,350,367` enumerates old/new directions and requires compatible measured receipts. Revise only under an approved boundary; retain old rows.
- `tools/tests/test_p1r_published_executor.py`, `tools/tests/test_p1r_published_qualification.py` hold regressions.
- `_bmad-output/implementation-artifacts/evidence/6-1-p1r-31190-published-run/README.md` is sealed and nonqualifying; Projects `_bmad-output/implementation-artifacts/sprint-status.yaml` remains blocked.

## Tasks & Acceptance

**Execution:**
- [x] tools/p1r-published-consumers/host/QualificationCapabilities.cs, tools/p1r-published-consumers/host/ExpectedDenial.cs, src/Hexalith.EventStore.Server/Actors/AggregateActor.cs if needed, tools/p1r_published_executor.py — route stale and forged requests through the aggregate and require a specific refusal plus unchanged persisted inventory. A preflight may add diagnostics but cannot substitute for the routed attempt; opaque actor transport remains nonpassing. Add a source actor regression for any runtime denial propagation change.
- [x] tools/p1r-published-consumers/domain/Program.cs, tools/p1r-published-consumers/host/Program.cs, tools/p1r_published_executor.py — seed a distinct old alias through the bounded serializer and replay it through the registered test-only manifest. Anchor its SHA-256 independently of the manifest file. Prove unknown-version refusal from the actor's logical-read diagnostic, zero events, unchanged sequence and inventory; record actual routed/readback evidence instead of deriving execution from expected acceptance. Preserve original event bytes and other Tenant.
- [x] tools/p1r_check_witnesses.py and tools/tests/test_p1r_published_executor.py — bind each logical readback to its exact actor state URL and add compatible-receipt witness tests that reject altered denial, URL, and inventory evidence.
- [x] tools/tests/test_p1r_published_executor.py and tools/tests/test_p1r_published_qualification.py — cover the routed fences, alias execution, original bytes, incompatible routes, and absent/failed/unmeasured lanes; keep the current gate strict.
- [x] Projects _bmad-output/implementation-artifacts/spec-6-1-p1r-3.md and a bounded EventStore source-evidence artifact — retain reviewable source observations and propose supported/refused directions for Architect review, without altering the sealed packet or claiming published qualification.

**Acceptance Criteria:**
- Given the sealed 3.119.0 packet, when re-read, then its false qualification and historical failures remain unchanged.
- Given the repaired source fixtures, when stale/forged fences and alias replay run, then the measured persisted state proves refusal or replay without claiming published qualification.
- Given no approved support matrix or successor publication, when the current gate evaluates results, then incompatible required lanes keep qualification, P1R usability and Projects readiness false.

## Handoff Gates

Architect: approve required/refused directions before gate edits. EventStore/Builds and Release Engineering: approve tuple, authoritative pin and publication before a unique published run. Four owner decisions and conformance gate acceptance.

## Implementation Notes

The sealed 3.119.0 packet at `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-31190-published-run/packet-677345025f7b42449cbf8ce4afb43813/` remains unchanged: its validator reported `valid=true`, `technically_qualified=false`, `p1r_usable=false`. The packet's stale path contained an opaque outer actor exception and did not prove denial. The repaired source fixture validates the stale execution context at the admission actor; its nested `System.InvalidOperationException` says exactly `The idempotency execution authority is no longer current.` The forged proof still returns its separate exact fence denial. Both are required together with unchanged persisted domain inventory. The matcher does not accept opaque errors.

The logical fixture pins five test-only manifest rows to SHA-256 `5a5748913258b5832b333fe507bc689f1ac9e53a9201b4bb5cb983b535da933b`. The bounded serializer writes `P1R.Legacy.CounterIncremented` during seed, then a normal candidate reader replays that committed alias using the registered manifest. The Host and Domain both register the manifest because replay occurs in the Host aggregate. The test pin has `gateway_authority=false`. Dapr application-envelope readback covers events 1–12, and the raw event inventory hash is equal before and after each read. A metadata version of 987 is refused before application; this negative diagnostic intentionally changes only the stopped fixture's version after seed.

Historical first-pass source-only smoke on PostgreSQL `state.postgresql` v1 and Dapr 1.18.2 (superseded by the durable review-loop 2 evidence below; these 55/55 results are not the current verification):

| Probe | Outcome | Executed checks | Domain inventory |
| --- | --- | --- | --- |
| Stale context plus forged proof | Both exact nested refusals | 41/41 | Before/after SHA-256 `9bca71d0aae334b271d65dee28a3dd91aab190874f20edb1a4e6c45fd2628990` |
| Registered old alias replay | Accepted at sequence 12 | 55/55 | Before/after SHA-256 `6d3dc59ebd4b60202ec42eb308f591beef18a7505062a3e5fe91c634ec031d70` |
| Unknown version | Refused, no application | 55/55 | Before/after SHA-256 `ab6ad027c8bba5852db301097df08dabac952e6ee99b1eecf2628ec5dea15c9b` |
| Original old-alias envelope | Replayed; original bytes preserved | 55/55 | Before/after SHA-256 `d8e5f145786c7f1274ba286ad31fdc0ed2ec8956695e276a7a5721754c3f5712` |

The local smoke records are under `/tmp/p1r-source-repair-8b4914cc6ff240618b2230de150141fd/` and `/tmp/p1r-source-alias-776e58f3bde7413a8465e4947e2a4edb/`; the latter's owned-resource cleanup passed 7/7. These temporary observations are superseded by the durable review-loop 2 receipts below and must not be used as current verification. They are not successor package evidence or a qualification claim.

### Review-loop 2 retained source observations

The repaired aggregate route now obtains a current/stale decision from the admission actor and emits the exact stale-authority refusal inside `ProcessFencedCommandAsync`. A second owned PostgreSQL/Dapr 1.18.2 source smoke passed all four cases: routed stale plus forged proof 41/41, registered alias replay 64/64, unknown metadata version 987 refusal 65/65, and original alias-envelope preservation 64/64. The before/after domain inventory hashes were respectively `a8d10de91bb92173ceb6d6dbdef165527515504b0bd77ed0d4540144351215d8`, `0e228d92acaf8309808aded1edda50efc3deb41419edc29be73bd37e063162a3`, `6e94a13c2a1610956e026f4460c1488a7f238dc6dfd6d4619f2629e53b89f935`, and `f5b761e9586e97e84ac4a4dc65e766e7f856d3e02e7d870383f1b33cb33253bc`. The unknown-version actor returned the bounded `logical-event-read-rejected` diagnostic with zero events and unchanged sequence 12. Cleanup passed 7/7. Exact case commands, readback URLs, persisted inventories, source/runtime bindings and independent witness checks are retained in [the bounded EventStore source artifact](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-source-repair-2026-10-10/README.md). The post-run diagnostic-type re-audit in that artifact confirms that both retained denial messages came from `System.InvalidOperationException`; it preserves the historical run binding. This is source evidence only; it does not change the sealed packet, the support-matrix approval requirement, the current gate, P1R usability or Projects readiness.

### Proposed support boundary for Architect review

The following proposal comes from the sealed packet's retained case rows. “Supported” means the named direction passed its measured checks in that packet; it does not approve a successor or change the current gate. Every refused row stays visible as a negative receipt.

| Area | Proposed supported directions | Proposed refused directions / reason |
| --- | --- | --- |
| Metadata | Measured 3.110.0 and 3.119.0 reads; passing write directions `3.70.1→3.110.0`, `3.70.1→3.119.0`, and `3.110.0↔3.119.0` | 3.70.1 Pascal/web floor reads; writes `3.110.0→3.70.1` and `3.119.0→3.70.1` failed |
| Invalid persisted evidence | 3.119.0 candidate refusal checks | 3.70.1 and 3.110.0 invalid floor, protected payload, and unknown-version checks failed |
| Query/projection wire | Passing legacy-principal query directions; measured 3.110.0↔3.119.0 positive-watermark projections | Every cross-version dual-principal JSON/XML query failed; positive-watermark JSON projection to or from 3.70.1 failed |
| Protected mixed API | Individually passing operations only, with the repaired source candidate stale/forged path requiring new package proof | 3.70.1 unsupported cursor, fence, trusted effect, unauthorized effect and retained floor; old status-field losses; 3.110.0 stale-fence error; sealed 3.119.0 stale-fence error |
| Recovery | Measured post-upgrade restore and forward recovery where the floor, hashes, sequence and other Tenant stay intact | Containment-only pre-upgrade backup restore cannot preserve later committed writes; retain `rollback=null`, mutation freeze and forward recovery |

The Architect must decide the required/refused directions and their fail-closed contract before a gate change. The sealed 3.119.0 failures cannot be retroactively repaired; a separately approved published successor must be measured against the approved matrix.

## Spec Change Log

- 2026-10-10 review loop 1: The first implementation let stale prevalidation short-circuit aggregate routing, accepted a broad unknown-version outcome, derived execution from the expected result, kept the fixture pin in the same JSON as its rows, matched readbacks by sequence without exact keys, and left source receipts only in temporary storage. The execution tasks now require routed causal proof, independent pinning and key binding, compatible-receipt witness tests, and durable bounded source evidence. Known-bad state to avoid: a passing source or future package lane that never exercised the protected aggregate or cannot be independently inspected. KEEP: exact nested admission/fence denial diagnostics; fail-closed handling of opacity; old-alias seed through the bounded serializer with Host and Domain registration; original event hashes, other-Tenant preservation, passing 99-test focused suite, and the unchanged qualification gate, sealed packet, and blocked Projects readiness.

## Review Triage Log

| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| VG1 stale aggregate path | high / bad_spec | ValidateAsync throws before RouteFencedCommandAsync; the mocked Python response and inventory cannot prove aggregate refusal. |
| EH1 stale aggregate path | high / bad_spec | Same verified short circuit as VG1; a broken aggregate route could be hidden. |
| EH2 unknown-version cause | medium / bad_spec | accepted=false alone can come from another logical-read failure; error, event count and sequence are available but unchecked. |
| EH3 state readback URL | medium / bad_spec | Witness accepts any successful /state/ response with the same sequence and hash, regardless of actor ID or event key. |
| BH1 stale aggregate path | high / bad_spec | Same verified short circuit as VG1; the routed method is not reached on expected preflight denial. |
| BH2 unknown-version cause | medium / bad_spec | Same verified broad acceptance check as EH2. |
| BH3 derived evolution execution | medium / bad_spec | executed is computed from actor.accepted is expected; an unknown-version refusal therefore reports executed=true without a separate execution witness. |
| BH4 colocated fixture pin | medium / bad_spec | FixtureEvolutionManifest reads fingerprint and rows from one JSON, so replacing both can silently change the purported pin. |
| BH5 state readback URL | medium / bad_spec | Same verified sequence-only witness matching as EH3. |
| BH6 compatible witness test | medium / bad_spec | Existing logical witness tests exercise an incompatible receipt and a readiness contradiction; none reaches the new compatible branch. |
| BH7 successor constant | medium / reject, explicit owner gate | Executor candidate 3.119.0 and version conditions predate this source slice; the approved decision forbids successor selection or publication in this run. A later approved tuple requires separate binding work. |
| BH8 matrix precision | medium / reject, spec edit | Proposed directions omit exact case IDs and formats, which would impede gate translation; the suggested remedy edits this build's spec, which this review route explicitly rejects. Architect approval remains pending. |
| BH9 temporary source receipts | medium / bad_spec | Implementation Notes point only to /tmp records; after cleanup the reported hashes and check counts cannot be inspected. |

| VG2-1 aggregate call receipt | false / reject | Current Host code calls RouteFencedCommandAsync before either catch, and the router dispatches this valid context to ProcessFencedCommandAsync; the exact stale denial is emitted by the aggregate's new decision path. The hypothetical prevalidation regression is absent from this bound source run. |
| VG2-2 actor target identity | medium / patch | Compatible logical witness finds a matching accepted probe but does not validate tenant, aggregate, expected count or AssertCounter arguments; the existing positive test omits them. |
| EH2-1 actor target identity | medium / patch | Same verified missing target binding as VG2-2. |
| EH2-2 nonzero actor probe | medium / patch | Probe collection includes instrumented JSON even when exit_code is nonzero, so a failed process can supply an accepted observation. |
| EH2-3 readback versus inventory | medium / patch | Retained source readback and provider event SHA-256 match for sequence 1, but the witness never joins them; altered provider bytes could be attributed to the readback. |
| EH2-4 version input key | medium / patch | The unknown-version witness accepts any provider key ending in events:7; another actor's key can satisfy this without changing tenant-a's event. |
| BH2-1 Current decision test | low / patch | New EvaluateAuthorityAsync Current branch is exercised by source smoke but not by an actor unit test; a focused success test will guard the normal path. |
| BH2-2 transported fallback test | low / patch | The existing test exercises the old ValidateAuthorityAsync fallback after Unknown, not transport failure from the new EvaluateAuthorityAsync call; one focused test can distinguish them. |
| BH2-3 literal method labels | false / reject | Literal labels alone would be weak, but the bound Host implementation executes RouteFencedCommandAsync before returning them and the specific denial comes from the aggregate path; no preflight remains. |
| BH2-4 inner exception matching | false / reject | No reachable unrelated outer failure carrying the exact InvalidOperationException denial was shown; the fixture also requires unchanged persisted state and rejects other errors. |
| BH2-5 duplicate positive cases | low / reject | Both selected case IDs intentionally apply one source operation to replay and envelope-preservation claims; each retains the same checks, and duplicate execution causes no incorrect qualification. |
| BH2-6 absent-manifest control | false / reject | AggregateActor creates the logical reader only from a registered EventEvolutionManifestCandidate; the distinct legacy alias lacks the typed fallback name. A second negative smoke is unnecessary for the current causal path. |
| BH2-7 unused before_events map | medium / patch | The witness builds provider event hashes but does not compare them to Dapr readback hashes; this is the same unjoined inventory defect as EH2-3. |
| BH2-8 unvalidated payload hash | low / patch | The retained payload_sha256 field is not recomputed from the exact HTTP response, so an altered hash claim can pass. |
| BH2-9 broad logical-read code | false / reject | The controlled source cases show successful alias replay from the same seed, then only metadata version 987 is changed at the target event before the bounded logical-read rejection; no competing failure is demonstrated. |
| BH2-10 source inputs hash | low / reject | The source-only execution's selection hash is not independently reconstructable, but the runtime profile, commands and case outcomes are retained; reconstructing a transient synthetic input object adds complexity without changing source proof. |
| BH2-11 post-run re-audit log | low / patch | diagnostic-type-reaudit.json records current_witness_revalidated=true but omits the command and output that produced that post-run result; retain a reproducible bounded check. |
| BH2-12 successor constant | medium / reject | carried: 3.119.0 remains the pre-existing selected tuple, and the explicit owner decision forbids binding or publishing a successor in this run. |
| BH2-13 matrix precision | medium / reject | carried: proposed directions are not exact case IDs, but the suggested fix edits this build's spec and the Architect decision is pending. |

## Verification

**Commands:**
- `PYTHONPATH=tools python3 -m unittest discover -s tools/tests -p 'test_p1r_published_*.py'` from EventStore — 101 focused tests pass.
- `dotnet build` of source Host and Domain consumers with `UseCurrentSource=true`, `UseHexalithProjectReferences=true`, and `EventStoreSourceRoot=<EventStore root>` — both pass with zero warnings or errors.
- `dotnet restore` and `dotnet build` of the Host and Domain consumers against the existing published 3.119.0 package, without running them — both compile with zero warnings or errors; this is not successor qualification.
- `git diff --check` in each changed repository — no whitespace errors.

**Review-loop 2 commands and results:**
- `PYTHONPATH=tools python3 -m unittest discover -s tools/tests -p 'test_p1r_published_*.py'` — 101/101 pass.
- `dotnet build tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 --nologo`, then the built assembly with `-class Hexalith.EventStore.Server.Tests.Actors.AggregateActorFencingTests` and `-class Hexalith.EventStore.Server.Tests.Actors.IdempotencyAdmissionExpiryTests` — zero build warnings/errors; 23/23 and 24/24 pass.
- Debug source Host/Domain builds, and Release consumer Host/Domain builds against existing 3.119.0, 3.110.0 and 3.70.1 packages — zero warnings/errors. These compile checks do not qualify a successor.
- The four retained source cases passed independent `p1r_check_witnesses.validate_case` checks against their source binding and predicate bytes; `SHA256SUMS.json` validated all 12 retained files; the post-run diagnostic-type re-audit passed.
- `sha256sum -c SHA256SUMS` in the sealed 3.119.0 packet passed for every file; re-reading its unchanged `packet.json` still reports `technically_qualified=false` and `p1r_usable=false`.
- `git diff --check` in EventStore and Projects — no whitespace errors. The earlier `aspire run --isolated --detach --non-interactive --format Json --apphost src/Hexalith.EventStore.AppHost/Hexalith.EventStore.AppHost.csproj` baseline still exits 2 after build because nested Tenants host projects are absent; no nested submodule was initialized.

The wider `aspire run --isolated --detach --non-interactive --format Json --apphost src/Hexalith.EventStore.AppHost/Hexalith.EventStore.AppHost.csproj` baseline exited 2 after build because the nested `references/Hexalith.Tenants` projects are absent. Repository guidance prohibits initializing nested submodules; the bounded consumer builds and operational source smoke above provide the relevant verification.
