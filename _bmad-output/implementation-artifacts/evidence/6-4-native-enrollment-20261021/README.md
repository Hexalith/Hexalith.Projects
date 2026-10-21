# Story 6.4 Conversation native guard enrollment

Status: **reviewed local source slice complete; owner prerequisite in progress; Story 6.4 blocked**.

The SDK now supports explicit Conversation proof enrollment alongside its exact response-only legacy handler. It checks the complete local same-domain handler inventory, exact types/routes and actual full-replay semantics. Ordinary catalogs, legacy routes and default production composition are preserved. Missing, conflicting or unsupported enrollment withholds proof.

Original producer input and first durable admission are separate: retries retain their original verified proof or absence across verifier outage, reconciliation and ledger loss. Exact hinted input uses length-prefixed hashing. Production compatible replay carries its already validated native cut; optional capture uses bounded admitted workers. Rebuild transport freezes original proof/absence, and mixed streamed/full replay proves admission at the actual persisted consumer. Native input validation, caller-first cancellation and expected gateway failures preserve the required rejection/fallback boundaries. Existing SDK storage, immutable manifests, exact resource bindings and current-authority checks remain authoritative.

## Final verification

| Independent Debug source check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Full Conversation Server | 1077 | 0 | 0 |
| SDK DomainService projection/native/endpoints/storage | 201 | 0 | 0 |
| SDK Server producer/scheduler/guard/rebuild/controllers | 543 | 0 | 0 |
| SDK Client authoritative/native readers | 108 | 0 | 0 |
| Focused SDK plus full Conversation | 1929 | 0 | 0 |

All four independent test-project builds succeeded with zero warnings/errors. Final test sources matched the independently reviewed snapshot, and source/binary hashes were unchanged before and after every final test invocation. [Exact commands](command-records.json) retain cwd, argv, exit codes, elapsed time and snapshot/binary bindings. [Results](verification-summary.json), [full revisions and source/binary hashes](source-manifest.json), the [incremental enrollment patch](enrollment-incremental.patch) and [cumulative owner patch](owner-cumulative.patch) bind the evidence to the uncommitted sources. Original XML, full logs, executed test names, recorders, review snapshots and original dirty files are in [verification artifacts](verification-artifacts.tar.xz).

The separate full SDK DomainService run was **1126/1127**, zero skips. Its sole failure is the existing `DomainModuleAuthoringGuardrailTests.TenantsDomainService_DoesNotReferenceGeneratedApiHostOrDeclarePerMessageControllers`: the expected nested `EventStore/references/Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj` is absent. The exact broad command and failed result are retained; the focused result does not replace that broad failure. Nested initialization is unauthorized, and the guard/dependency layout is unchanged. Failed and superseded implementation/review runs retain distinct labels, XML and logs; none is reported as a final pass.

[Executed coverage](coverage-map.json) includes ordinary legacy and compatibility delivery; explicit guarded negotiation and actual persisted SDK owner proof; unsupported handler enrollment; tampering, replacement and exact binding; unchanged producer proof/absence across durable retries; verifier outage/recovery and lost ledgers; production compatible replay under ordinary and active legacy leases; bounded abandoned capture; immutable transport headers; full-replay sibling admission through mixed transported recovery; direct rich-writer invalidation; and content-free persisted reads with current native resource/permission checks. The supported storage/transport fixtures demonstrate source behavior; they do not establish sidecar, installation or deployment qualification.

## Review and preservation

Three context-free reviewers filed thirteen claims; the parent added one immutable-header claim. Eleven claims were addressed. Three existing safe limits/boundaries were retained: unproven operator rebuild coverage, bounded complete-prefix reads, and the additional mandatory Conversation metadata gate beyond a historical owner receipt. [Individual dispositions](review-results.json), [original reports](review-reports.md) and [independent correction confirmations](review-confirmations.json) preserve every claim. All reviewers found no remaining concrete defects in the corrected paths; final suite execution was independent of those source inspections.

The [source and writer guide](source-and-writer-boundaries.md) documents opt-in composition, capability negotiation, native admission, every relevant writer/recovery boundary and unsupported coverage. [Preservation checks](preservation.json) bind the original user changes, frozen intent, selected D1–D6, parent story/sprint, previous provenance bundle, dependency/configuration bytes, Git indexes and full revisions. No staging, commits, pushes, branch/dependency/gitlink changes, nested initialization or deployment occurred. Existing dirty bytes remain reviewable in their initial snapshots and the incremental patch.

## Remaining prerequisites

Production native-read enrollment, installed/all-writer source-guard acceptance and deployed owner authority are not supplied by a local handler declaration. Unsupported operator/mapped rebuilds, protected/evolved or retained-away input, older hinted retry formats without sufficient immutable admission evidence and reads beyond existing complete-prefix bounds remain unavailable. No retrospective certificate or historical migration is created.

Content-free post-rebuild promotion, cold-activation zero-write qualification, native Folder/Organization generation binding, committed File/Projects reference mapping, production metadata mutation adoption, exact-current qualification and accepted Projects G-4 remain separate prerequisites. RTI and resolution handlers stay gated. The prerequisite stays in-progress; parent Story 6.4 and sprint remain blocked.

The [runtime baseline](runtime-baseline.json) started the exact Projects AppHost with source references, observed security Unhealthy and downstream resources Waiting, then stopped it normally before edits. Other running AppHosts were untouched. No healthy live topology, installation-wide zero-write result or production acceptance is claimed.
