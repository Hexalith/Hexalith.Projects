---
title: 'Inspect Projects through an authenticated FrontComposer read surface'
type: 'feature'
created: '2026-08-25'
updated: '2026-11-06'
status: in-progress
route: dispatch
readiness: approved
implementation_authorized: true
baseline_commit: 2e2b6cff6c59775ce06d5e26b5c5913ac54c8edd
baseline_revision: 2e2b6cff6c59775ce06d5e26b5c5913ac54c8edd
blocked_by:
  - "Live positive Folder-advisory acceptance returns empty/incomplete; original runtime cause remains unresolved."
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/evidence/story-6-5-prerequisite-readiness-20261105/readiness-final/contract-map.md'
---

<frozen-after-approval reason="Planning refresh requested; implementation requires a separate request">

## Intent

Expose accepted Project reads through authenticated FrontComposer inventory/detail/advisory journeys. Reuse delivered authentication; replace legacy REST adaptation with the existing SDK query client.

## Boundaries & Constraints

Require authenticated server identity/current Tenant and FrontComposer session credentials. Preserve Safe Metadata by default, separate audited descriptive inspection, Fluent UI V5 and WCAG 2.2 AA. No token storage/relay implementation in Projects, sibling edits, generated-client edits, domain writes, audit-history queries, maintenance/selection controls, payloads, secrets or protected denial detail. Retain legacy routing for 6.7 comparison; no cutover. Never edit sprint status here.

DW-1 remains done for the verified owner repair, not the unchanged gitlink. Accepted 6.1–6.4 and exact repair/test references are in [readiness evidence](evidence/story-6-5-prerequisite-readiness-20261105/readiness-final/README.md). Missing usable prerequisites block; publication/production enrollment are not added gates.

## I/O & Edge-Case Matrix

| Given | When | Then |
| --- | --- | --- |
| Anonymous/missing authority | Supported route loads | Challenge or fail closed before protected queries |
| Authorized page and degraded row | Inventory renders | Preserve page enumeration and each row snapshot independently |
| Open/context/start observations differ | Detail renders | Separate states, times, versions, omissions and recovery; no combined admission |
| Absent/denied/foreign/unverifiable target | Open requested | Identical generic safe absence without protected metadata |
| Incomplete advisory or unsupported File | Suggestions render | Explicit coverage limits; no exhaustive NoMatch, selection or admission claim |

</frozen-after-approval>

## Code Map

UI paths use `src/Hexalith.Projects.UI/`. The context contract map specifies DTOs, wire names and snapshots.

- `Program.cs`, `ProjectsUiSecurity.cs`, `Hexalith.Projects.UI.csproj`: add EventStore.Client/authorization using existing OIDC and source/package conventions. `src/Hexalith.Projects.AppHost/Program.cs`: gateway reference/address.
- `Diagnostics/{ProjectInventorySource,ProjectWarningsDashboardSource,ProjectDetailSource,ProjectResolutionTraceSource}.cs`: replace REST adaptation; adapt their interfaces/results/mappers and `src/Hexalith.Projects.Contracts/Ui/` descriptors.
- `Components/Pages/{Home,ProjectDiagnostics}.razor`, `Components/Shared/{ProjectNavigation,ProjectResolutionTraceWorkbench}.razor`: independent evidence and truthful labels.
- `Legacy*` pages, navigation and diagnostic sources: preserve the existing REST comparison routes.
- `src/Hexalith.Projects.Server/Queries/{GetProjectContextQueryHandler,GetConversationStartSetupQueryHandler}.cs`: leave computed-result `ProjectionType` unset for SDK routing; preserve delivered payload and admission semantics.
- `tools/qualification/{run_supported_project_reads,supported_browser_reads}.py`, private AppHost profiles and E2E fixture observers: real OIDC/browser proof with persisted-state comparison, continuous metadata-only telemetry checks and exact owned cleanup.

## Tasks & Acceptance

- [x] Compose SDK registration in the host/project files above. Attach `AddFrontComposerGatewayAuthorization()` after owner security registration. Derive query Tenant from `IFrontComposerTenantContextAccessor`; reject missing authority before querying, including local auth-disabled startup. Use `SubmitQueryAsync<T>`, exact targets/discriminators and typed payloads; propagate cancellation and sanitize gateway failures.
- [x] Adapt inventory/warnings sources and Home/navigation: opaque paging with Tenant/filter reset, lifecycle/quarantine selectors, explicit descriptive-inspection mode, independent page/row evidence; remove diagnostic N+1 and invented timestamps/warnings.
- [x] Adapt detail source/results/page: separately labelled open/context/start states, versions, components, omissions and recovery. Preserve the distinct Conversation-start snapshot and accepted owner-trust limits. Audit capability is unavailable; remove maintenance controls and unsupported Refresh calls.
- [x] Adapt advisory source/workbench/descriptors: nullable result, unselected candidates with IDs/rank/score/safe reasons, evaluation time distinct from source freshness, incompleteness distinct from lag, explicit unsupported File coverage, no paging, 32 raw attachments before deduplication.
- [x] Extend `tests/Hexalith.Projects.UI.Tests/Authentication/`, `Diagnostics/`, `Components/` for the frozen matrix and independent evidence. Extend `tests/Hexalith.Projects.Integration.Tests/AspireTopologyTests.cs` and `tests/e2e/specs/` for real authenticated SDK journeys and accessibility. Connect test-owned persisted observation to browser reads through `tests/Hexalith.Projects.E2E.Fixtures/`; isolate setup/cleanup/concurrent writes, expose no production state endpoint.

Given interleaved authenticated circuits, including same-subject sessions within/across Tenants, when supported queries execute, then each uses only its session credential/current Tenant. Given complete/degraded/empty/archived observations, when rendered, then delivered semantics survive without inferred admission. Given Safe Metadata versus separately authorized descriptive inspection, when reads execute, then protected values remain absent unless permitted and required inspection audits persist separately. Given browser inspection, when persisted key sets/bytes/ETags/events and telemetry are compared, then domain state is unchanged and telemetry leaks no protected values. Given keyboard/screen-reader/320px/200% zoom journeys, when axe/manual checks run, then evidence records no unresolved critical/serious findings.

## Verification

Apply every emitted Projects mapping value. From each `tests/Hexalith.Projects.{UI,Integration}.Tests/` directory run `dotnet build --configuration Debug -m:1 -p:NuGetAudit=false --disable-build-servers`; invoke built xUnit assemblies with `-class` and native XML. Run `npm --prefix tests/e2e run typecheck` and `npm --prefix tests/e2e run test:live:managed`; use local realm fixtures privately. Retain command/XML/browser/manual receipts, totals and canonical tested revisions. Readiness passes are not feature acceptance.

## Implementation Notes

2026-11-06: user approved and continued. Implementation is authorized; frozen intent remains locked. Its explicit sprint-status exclusion overrides workflow synchronization. The five implementation tasks are complete; overall acceptance remains pending below.

Historical readiness, before this approval: synthetic fixture migration and the mapped UI build passed, with authentication composition 12/12. FrontComposer HEAD/root gitlink is `f577f958ed1c043eec7feabc4885f59d48428818`; all 17 repair hashes match the retained owner repair. Its historical 114/114 receipts remain historical because binaries changed. Readiness evidence does not accept this feature.

Implemented the opt-in `/inspect/projects` inventory/detail/advisory surface through typed SDK queries and FrontComposer-owned authentication/current-Tenant/session credentials. Preserve the legacy routes for 6.7. Page, row, open, context and Conversation-start observations remain independent. Descriptive/quarantine inspection is explicit and audited. No Projects token store/relay, maintenance/selection control or audit-history query was introduced.

The two Projects-owned computed-query handlers now leave `ProjectionType` unset: the SDK router rejected their former query discriminator as projection metadata. Payloads and accepted snapshot/admission semantics are unchanged.

Verification: selected native UI 197/197, Server 185/185, Contracts 46/46 and topology/observer 28/28 pass with zero skips/errors; mapped Debug/source builds have zero warnings/errors. Launcher controls 133/133, TypeScript, Python and shell checks pass. The final wholly fresh managed run has native real-owner seed 1/1 and actual OIDC browser 1/1 passing, with unchanged persisted domain fingerprints, separate inspection-audit growth, continuous stdout/stderr leak checks, pre-existing resource preservation and verified owned cleanup. Exact commands, XML, artifact hashes, tested revisions and source manifest are in the [implementation evidence](evidence/story-6-5-implementation-20261106/README.md). The primary-agent [matrix audit](evidence/story-6-5-implementation-20261106/matrix-audit.json) binds all five frozen rows to 12 executed passing cases. Three native cases requiring separate owned `STORY61_DAPR` endpoints are explicitly excluded and not counted as passes or skips; their initial failure receipts are retained.

Concurrent live reads can return a current Project/authorization observation with an unavailable Folder and Retry guidance. The browser proof retains an initial actual Complete observation and checks the precise delivered degraded state during interleaving. No universal availability or recovery guarantee is inferred; no owner timeout or behavior was changed, and the cause of variable Folder availability remains unestablished.

Historical remaining acceptance at the previous checkpoint: manual screen-reader and native 200% browser zoom checks had not been performed. Automated 320px axe has no violations or incomplete rules; CSS 200% zoom has no violations but an inconclusive `color-contrast` rule requiring manual evaluation. The [manual availability receipt](evidence/story-6-5-implementation-20261106/manual-check-availability.json) is pending evidence, not a passing accessibility receipt. The ordinary managed comparison stopped before collection (CreateTenant HTTP 403; retained legacy probes HTTP 404; zero collected); no unrelated writer/bootstrap/routing repair was added. Failed diagnostic runs remain separately retained, including a browser-passing run whose pre-existing-resource preservation guard failed.

2026-11-06 continuation: at the user's request, Codex installed environment-only assistive tooling and executed actual Orca/AT-SPI/speech-dispatcher/espeak checks after real OIDC on a fresh managed actual-owner topology. Inventory/detail/advisory passed at native Chrome 100% and 200% (DPR 1 to 2, CSS zoom 1): 41 assertions, 74 visible focus observations, six axe scans with zero violations and no unresolved contrast targets. Codex independently inspected all six full-window captures; actual emitted announcements and synthesis counts are retained. Persisted domain/audit fingerprints were unchanged, continuous telemetry checks passed, pre-existing resources were preserved and exact owned cleanup passed. See [actual assistive evidence](evidence/story-6-5-native-assistive-20261106/README.md) and the [primary acceptance audit](evidence/story-6-5-native-assistive-20261106/parent-acceptance-audit.json). Failed observer-only captures remain separate diagnostics.

Refreshed current-source native UI 197/197, Server 185/185, Contracts 46/46 and topology/observer 28/28 pass without failures/skips/errors; all four mapped Debug builds have zero warnings/errors and TypeScript passes. The [current matrix audit](evidence/story-6-5-native-assistive-20261106/parent-matrix-audit.json) verifies all five frozen rows against 12 executed passing cases. This initial assistive checkpoint entered `in-review`; subsequent review verification below found an unresolved live acceptance failure. These are agent-driven real assistive-runtime checks with an ALSA null audio sink, not human audio evaluation or exhaustive WCAG certification. No package, production-readiness or 6.7 equivalence acceptance is claimed. Sprint status remains byte-unchanged; nothing was staged, committed or pushed.

Current review continuation: the 10 accepted patch groups are implemented, including per-consumer paging, advisory input invalidation/cancellation, optional authorized reference labels, standard Keycloak mappings and failure-artifact privacy. Current native UI 211/211, Server 185/185, Contracts 46/46 and Integration 45/45 (including 17 readiness controls) pass; launcher controls 142/142, axe safety2/2 and TypeScript pass. All five frozen matrix rows are re-audited against12 executed passing cases. See [final review verification](evidence/story-6-5-review-verification-20261106/README.md).

At the user's explicit request, current-source real Orca and native 100%/200% Chrome checks were performed. Seven axe scans have no violations or unresolved contrast targets; all 10 defined spoken state/coverage cases have passing evidence across the all-surface run and one native-keyboard inventory rerun. The original all-surface run remains50/51 with its inventory200 spoken capture inconclusive; the targeted rerun is 10/10. Actual spoken open/context/start states and coverage limits, synthesis counts, seven primary-agent visual inspections, unchanged persisted domain/audit fingerprints, continuous privacy telemetry and exact cleanup are retained. This is agent-driven real AT work, not human audible evaluation or exhaustive WCAG certification.

Acceptance is still blocked: the new positive live Folder browser assertion failed, and two fresh setup runs then failed 30 genuine single-Folder readiness queries each (HTTP200/null/incomplete/no candidates). Index/detail headers and bindings were current; no positive assertion, owner budget, privacy guard or sibling source was relaxed. A clearly failed diagnostic hold allowed differential reads and the current-source AT checks; it is never counted as a passing managed proof. Later held Folder staleness is established against the 300s source-age rule, but the original 30 observations finalized at Tenant age139.080s, so later staleness does not explain their failure. Original owner/runtime causality remains unproven; no safe Projects-owned repair was established.

Status is `in-progress`. The build review stops under its failed-verification rule rather than marking the story done. All managed attempts preserved pre-existing resources, removed exact owned sidecars/containers/private material, and released the diagnostic hold. Sprint status and frozen intent remain byte-unchanged. Nothing was staged, committed or pushed.

2026-11-06 implementation continuation at `32482b7e77e0b0faf46b523ece4ef2bbefd9e2fc`: two wholly fresh managed source/Debug runs again fail all 30 positive Folder-advisory readiness attempts each (HTTP 200, null result, incomplete, zero candidates); both native seed cases fail and neither browser acceptance case is collected. Launcher controls pass 142/142 in each run and TypeScript passes. The 17,154 source inputs match across both runs and the post-run check. Exact commands, XML, revisions, source manifest and receipts are retained in the [runtime continuation evidence](evidence/story-6-5-runtime-continuation-20261106/README.md).

During setup diagnostics, the existing authenticated Folder metadata routes independently return authorized, non-stale results. A separate public supported empty advisory returns complete NoMatch coverage in 532.5ms, a supported Safe Metadata Project open returns its own Complete snapshot in 2178.1ms, and a single-Folder advisory returns empty/incomplete in 2478.8ms. These independent observations do not grant advisory authority or explain its exact failed stage. The finite metadata-only EventPipe capture loses no events but has partial operation coverage; it does not establish a complete delegated call chain or a specific owner denial cause. No exact new-run Tenant-age bound was captured before cleanup. Three existing-binary native deadline/revocation/final-anchor controls pass; they are diagnostic controls, not a full current-source regression refresh.

No safe local repair was established. Required security observations and their final ordering, the two-second advisory deadline, owner budgets/policies, privacy suppression and sibling sources remain unchanged. Both attempts preserve pre-existing resources and verify exact owned process/container/private-material cleanup. The positive live Folder readiness/browser acceptance boundary remains open, status remains `in-progress`, and frozen intent plus sprint-status bytes are unchanged. No staging, commit or push occurred.

## Spec Change Log

2026-08-25 prerequisite hard gate and November 2 correction remain historical in the [exact pre-refresh spec](evidence/story-6-5-prerequisite-readiness-20261105/readiness-final/spec-before-refresh.md). The approved ordinary-input gate supersedes the former prerequisite_record ceremony.

2026-11-06: refreshed actual Diagnostics/authentication/SDK map and independent response/advisory semantics; cleared consumer fixture failure. Preserve no-token-bleed, safe disclosure and feature acceptance requirements. No Story 6.5 implementation dispatched.

2026-11-06: refine consumer/verification map; frozen intent preserved.

2026-11-06: implemented and verified the approved SDK read surface; all five frozen matrix rows pass. Manual accessibility acceptance remains pending; retain in-progress status and unchanged sprint status.

2026-11-06: completed the requested actual assistive-runtime/native browser zoom checks, refreshed current-source native receipts and independently verified matrix coverage and screenshots; enter review with explicit human-audio/WCAG scope limits. Frozen intent and sprint status are unchanged.

2026-11-06: implement the ten review patch groups and bounded real advisory setup controls; refresh native/matrix/actual AT evidence. Preserve the failing positive live owner-read case and unresolved initial cause; return to in-progress. No privacy/budget/sibling/sprint exception or feature acceptance is granted.

## Review Triage Log

All three independent layers completed before triage. Each finding was assessed before grouping. All accepted findings have a trivial private or test-only correction with no new production public surface; no intent/spec loopback is required.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patch (P1) | The circuit-scoped inventory source holds one cursor pair; FrontComposerShell mounts navigation when widening from compact. Its list overwrites the Home continuation and Next page resets to null. Give each consumer its own source instance. |
| B2 | medium | patch (P2) | The newly declared profile/email/roles scopes have empty mappers. Both clients attach them; FcAccountMenu.ResolveDisplayName reads only the removed friendly-name claims and the retained AX account name is blank after “Signed in as”. Preserve standard scope mappings. |
| B3 | medium | patch (P3) | All advisory inputs remain editable and bind directly while RunAsync awaits; the result publishes unconditionally and is retained after edits. Clear results on input changes and reject an outstanding response for an older input generation. |
| B4 | false | reject | The page already renders independent Snapshot.AsOf and component freshness for context/start. Goals/instructions are descriptive metadata and the supported default deliberately renders them only from the separately inspected open response. This feature does not promise exhaustive payload equivalence; exposing context setup by default would conflict with Safe Metadata. |
| B5 | low | patch (P4) | Open references deliver an authorized DisplayName only when descriptive and current (ProjectReadQueryExecutor.ToReferences); the page drops it. Directly render this optional delivered label beside the identifier, preserving null omission. |
| B6 | false | reject | The browser does compare separate persisted audit growth, while the executed native server cases SafeMetadataAndInspectedMetadataHaveDurableAuditOnlyEffects, InspectedAndChatbotListsAuditReturnedNamesOnceAndFailClosed and AuditFailurePreventsDescriptiveDisclosure assert exact field classes/counts, metadata absence, idempotent persistence and fail-closed audit writes. The unchanged accepted 6.1 distributed fixture also validates schema and Redis TTL=-1. A requirement to duplicate all owner audit qualification in this browser observer is unsupported. |
| B7 | medium | patch (P5) | The current telemetry list covers primary A/description/secrets but excludes seeded primary B, foreign, owned Folder and legacy naming/setup values. _LogObservation accepts those exact bytes while reporting protected absence. Complete the known seeded marker set for browser and launcher receipts. |
| B8 | medium | patch (P6) | expectNoA11yViolations attaches results.violations verbatim on failure, including node HTML despite sanitized receipts; run_browser also writes assertion stdout without screening protected values. Retain sanitized failure metadata and redact protected/JWT-shaped assertion output before writing artifacts. |
| B9 | false | reject | The browser fixture has two rows, but continuation is behaviorally covered by executed InventoryRendersIndependentEnumerationAndRowEvidenceWithoutInventedDatesOrWarnings and ContinuationResetsOnTenantOrFilterChangesAndRemainsOpaque; the accepted 6.1 distributed path exercises real two-page SDK cursors. No separate all-browser pagination acceptance requirement or additional product failure is established. The demonstrated shared-consumer paging defect is separately accepted as B1/E1. |
| B10 | medium | patch (P7) | Supported live advisory uses only unsupported File input and the positive cases were retained on Legacy paths. Add an existing seeded supported Folder positive journey and verify unselected candidate fields; combine with the corresponding grounded native/component gap V2. |
| B11 | low | patch (P8) | The supported-only branch execs Python after allocating describe/start files. A successful exec bypasses the Bash EXIT trap, so remove these two owned files before exec. |
| E1 | medium | patch (P1) | Independently confirmed the compact-to-desktop navigation remount can change the single scoped _issuedCursor while Home retains the prior cursor. Same defect as B1; isolate source instances per consumer. |
| E2 | medium | patch (P3) | Changing mode, attachment text or archived inclusion while a request is pending or after it completes leaves candidates attached to changed visible inputs. Same stale-publication defect as B3; invalidate on edits and suppress stale generations. |
| E3 | medium | patch (P9) | The OIDC readiness deadline and sleep are inside except. Repeated successful discovery with the wrong issuer takes neither, so the private launcher can hang without reaching cleanup. Apply the existing deadline/backoff to every unsuccessful iteration. |
| V1 | medium | patch (P10) | Pre-verified gap: current layout tests render once; session-composition/browser tests use separate stable scopes. Add a rendered authority-transition case proving old disclosed inventory is removed and recreated in Safe Metadata mode. |
| V2 | medium | patch (P7) | Pre-verified gap: supported adapter/component fixtures provide only empty advisory results while positive assertions exercise Legacy classes. Add nonempty single/multiple supported mapping/rendering cases with IDs/rank/score/reasons and no selection claim. |

Patch verification outcome: P1-P6/P8-P10 have passing focused/native evidence; P7 nonempty mapping/rendering cases pass natively but its positive live Folder case remains failed. The real readiness gate detects and preserves that boundary. Overall review/feature completion is not accepted.
