---
title: 'Cut over supported reads while preserving compatibility and rollback'
type: 'feature'
created: '2026-11-16'
status: 'ready-for-dev'
route: 'dispatch'
baseline_commit: 'c6541a305a420078b16dceaba47f36468022850b'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Supported reads coexist with legacy REST reads; exhaustive equivalence, contract alignment and reversible selection are missing.

**Approach:** Prove seven supported reads through the existing Gateway, align generated contracts, and switch primary Web routing reversibly.

## Decisions

Jerome selected the recommendations on 2026-11-16: "apply recommended".

- **D1 Transport:** Existing Gateway with typed supported consumption; preserve v1 REST/client signatures. One explicit configuration choice switches primary Web entry points; restart/redeploy applies it. Record migrated consumers; external Chatbot adoption requires separate evidence. Limit EventStore export work to these reads; a substantial generator redesign becomes a prerequisite, never a Projects replacement generator.
- **D2 Comparison:** Version `story67-read-normalization-2026-11-16-v1` permits the conditional mappings below only. Approval defines comparison rules, not a passing gate.
- **D3 Hosting:** DW-63 stays open, owned by Projects/EventStore, as a Story 7.1 prerequisite alongside writer integration. The approved planning correction removes its 6.7 gate; AD-25 retirement remains gated.

## Boundaries & Constraints

**Always:** Tenant/original-actor authority, Safe Metadata, descriptive-inspection audit, exact positions, scoped cursors and AD-32. Preserve historical identifiers/events, including `ProjectFolderCreationPending`, and name-only creation. Reuse platform transport/generation. Retain legacy until Epic 8 acceptance.

**Never:** Rewrite history, add writers, weaken security, invent owner contracts, fall back after denial, hand-edit generated output, or retire runtime before AD-25. No deployment, publication, Git mutations or `sprint_plan.py`; readiness/Epic 7/8 pins stay unchanged.

## I/O & Edge-Case Matrix

| Scenario | Expected behavior |
| --- | --- |
| Complete approved comparison | Supported route selectable; rollback remains executable |
| Missing case, mismatch or regression | Block activation; explicit rollback only to a security-verified legacy path, otherwise unavailable |
| Foreign/absent/denied target | Same safe denial; no protected detail |
| Historical/pending/folderless record | Readable history; quarantine retained; no repair by read |
| Reads and rollback | No domain writes; inspection audits accounted separately |

## Comparison Contract

D2 mappings: JSON property order; lifecycle spelling to the same enum; folder fields to the identical authorized reference; Sequence to ProjectVersion only for the same source sequence; ObservedAt to AsOf only for the same observation; null Setup to empty only with independently verified current-empty evidence. Map physical keys to AggregateIdentity while retaining raw keys/collision checks. Decode cursors independently and compare boundaries/scopes plus continuation, expiry and rejection behavior. No timestamp tolerance, array reordering, discarded differences or inferred authority. Assert supported-only snapshots/components independently. Stale-Tenant admission, missing Conversation binding and advisory disclosure/incomplete-coverage differences require repair or explicitly approved behavior changes before cutover.

</frozen-after-approval>

## Code Map

`Server`, `Client`, `Testing`, `UI` mean `src/Hexalith.Projects.<name>`.

- `Server/ProjectsDomainServiceEndpoints.cs`: legacy routes/creation DTOs. `Queries/ProjectQueryRequestPreamble.cs`: existing ULID correlation IDs.
- `Server/Queries/{ProjectReadQueryExecutor,ProjectContextQueryExecutor,ProjectAdvisoryQueryExecutor}.cs`: supported reads; service registration retains both paths.
- `Testing/Reads/{ProjectReadShadowComparator,ProjectContextShadowComparator}.cs`: strict comparators; existing local revision remains historical.
- `Server/ProjectsRuntimeCatalogDeclarations.cs`: typed runtime schema/digests. EventStore `RestApi.Generators` lacks typed result export.
- `Client/Hexalith.Projects.Client.csproj`: legacy NSwag/fingerprints. `UI/Components/Pages/{LegacyHome,LegacyProjectDiagnostics,Home,ProjectDiagnostics}.razor`, `Components/Layout/MainLayout.razor`: Web selection.
- `tests/Hexalith.Projects.Server.Tests/Queries/{SupportedProjectShadowTests,SupportedProjectReadHttpTests.Shadow}.cs`: complementary local/HTTP coverage, not exhaustive equivalence.

## Tasks & Acceptance

**Execution:**
- [ ] `docs/read-cutover.md` — inventory events, keys, projections, cursors, routes, identities, consumers and partial/in-flight records across the seven reads; distinguish legacy-only refresh/conversations/diagnostics.
- [ ] `Server/ProjectsRuntimeCatalogDeclarations.cs`, `src/Hexalith.Projects.Contracts/` and platform `RestApi.Generators/` — add minimal typed export/host reconciliation; consume it in `Client/`. Preserve creation/classification and v1 signatures; regenerate fingerprints.
- [ ] `Testing/Reads/`, `tests/Hexalith.Projects.Server.Tests/Queries/ReadCutoverTests.cs` — compare authenticated paths on identical history; independently capture keys/positions/cursor scope. Test every failure; repair safety defects or block affected slices.
- [ ] `Server/Queries/`, Contracts OpenAPI and generated Client — resolve DW-57/59/60/66 with tests and recorded decisions; preserve completed repairs. DW-63 is assigned to 7.1.
- [ ] `Client/ProjectsClientServiceCollectionExtensions.cs`, `UI/Components/Pages/` and `UI/Components/Layout/MainLayout.razor` — implement D1 selection and explicit security-verified rollback; reject incomplete evidence; errors retain supported denial/unavailability.
- [ ] `tests/Hexalith.Projects.Tests/SchemaEvolution/`, Server/Client/Contracts/UI tests — prove history, ULIDs, opaque historical IDs, contract parity, rollback and persisted no-domain-write behavior.
- [ ] `_bmad-output/implementation-artifacts/evidence/epic6/6.7-read-cutover.json` and `6.7-rollback.log` — retain revisions, hashes, matrix, rule approval, XML, counts and rollback receipts; update tracking after acceptance.

**Acceptance Criteria:**
- Given all seven delivered reads, when the gate runs, then every required case passes canonical output, key, exact watermark, cursor/scope and isolation checks; no missing/skipped cell passes.
- Given a passing gate, when regression triggers explicit rollback, then a security-verified legacy path preserves domain bytes/history; otherwise reads become unavailable.
- Given generated contracts, when compared with the live host and legacy consumers, then schemas, identity handling and compatibility fingerprints agree.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Use `run-with-temp`; retain results outside `$TMPDIR`. Individually build the csproj files in `tests/Hexalith.Projects{,.Server,.Contracts,.Client,.UI}.Tests/` with Debug, `-m:1 --disable-build-servers`; run built xUnit v3 assemblies with native XML. Preserve audits; report lane-policy conflicts. Run production-authority validation with `--story-id 6.7` and `--validate-index`, and `git diff --check`; retain exact commands/results.
