# Rubric review — Architecture Spine update 2026-11-24

- **Artifact:** `ARCHITECTURE-SPINE.md` (working tree, SHA-256 `8fa20f04f0484019e3c23fcccef5ce4aaf3c3c14fa84e38d13087e5a5828929b`; HEAD blob `80378a8e…`)
- **Inputs:** `.memlog.md` entries after "2026-11-24 update"; `reviews/update-2026-11-24.diff`
- **Code inspected at:** Projects `280b1eed` working tree (Server sources unchanged from HEAD); DW-63 spec baseline `5f1d955b`; EventStore gitlink `208af26a`
- **Lens:** good-spine rubric (divergence coverage for Epics 6–8, enforceable Rules, Deferred safety, brownfield ratification, diagram/seed consistency, envelope completeness)
- **Reviewer stance:** read-only; no repository file other than this review was written.

## Verdict: PASS WITH FIXES

The reality refresh is accurate: every Stack version, gitlink, policy file, CI job outcome, tracker status and relative link I checked matches the repository (see "Verified" below). The problem is the amended AD-24. Its adapter clause is the main change in this update, and the actual Server code contradicts it on two of its stated limits. Its route anchor is checkable only through archaeology. Its retirement trigger disagrees with three other binding passages. Apply fixes F1–F5 before using this spine in the superseding Section G byte manifest. F6–F10 can be applied in the same edit.

## Findings

### F1 — HIGH — AD-24 "translate-only" is false against the adapter it ratifies

**Location:** AD-24 Rule, line 321 ("…reads through the existing Projects read models and owner directories, and submits commands only through `IProjectCommandSubmitter`. It never appends events, holds durable state or gains a route.")

**Evidence.** `MapProjectsHumanEndpoints` → `MapProjectsDomainServiceEndpoints` (partial class across `ProjectsDomainServiceEndpoints.cs` and 11 `Queries/*Endpoint*.cs`/`Legacy*.cs` files) does more than translate:

1. **Sibling-owner writes that bypass `IProjectCommandSubmitter`.** Five of the 25 routes send Conversations-owned `ReassignConversationProjectCommand` through `IProjectConversationAssignmentDirectory` ("Projects-owned write ACL for conversation project assignment changes"):
   - link, `ProjectsDomainServiceEndpoints.cs:932`
   - move, `:1010`
   - unlink, `:1076`
   - resolution/confirm, `Queries/ConfirmProjectResolutionEndpoint.cs:96`
   - proposals/confirm, `Queries/ProposeNewProjectEndpoint.cs:339`

   All five reach `IConversationClient.ReassignConversationProjectAsync` at `Conversations/ConversationsProjectConversationAssignmentDirectory.cs:267`.
2. **Durable writes on the retained read path.** Retained v1 list/open call `AuthorizeLegacyInspectionAsync` (`ProjectsDomainServiceEndpoints.cs:712, 1845` → `Queries/LegacyProjectInspectionAuthorization.cs`). That method persists inspection-audit facts and per-Project index pages through `ReadModelWritePolicy.UpdateAsync` into the read-model state store (`Queries/ProjectReadAudit.cs:34, 67`). AD-33 and `docs/read-cutover.md` both require this ("Every page load writes a durable metadata-only inspection audit before names are shown"). As worded, AD-24 forbids what AD-33 requires.
3. **Orchestration and process-local idempotency.** proposals/confirm runs a composite create-then-assign. It guards that sequence with a per-replica `InMemoryProjectProposalConfirmationIdempotencyLedger` (`ProjectsServerServiceCollectionExtensions.cs:110`, `ProposeNewProjectEndpoint.cs:309`). The memlog's last correction calls this "retained v1 behavior, not an AD-5 implementation", but the spine never says so. A reader of AD-5 or AD-13 ("on any replica… recover the original admitted task") could treat it as non-compliant, or as a precedent to copy.

**Impact.** The memlog says the amendment exists to stop a compliant DW-63 host being read as an AD-24 violation. Read literally, it makes the actual host a violation on 5 of the 25 routes and on every retained list/open call. The memlog entry "(correction by architect)… verified against ProjectsDomainServiceEndpoints" is factually wrong.

**Fix.** Replace the clause with an exhaustive list of permitted effects. For example: "…it authenticates the human caller and binds the principal. It reads through the existing Projects read models and owner directories. It submits Projects commands only through `IProjectCommandSubmitter`. It dispatches Conversation membership changes only through the existing `IProjectConversationAssignmentDirectory` on the five retained membership routes. It writes only the AD-33 inspection-audit facts through `ProjectReadAudit`. It keeps only the process-local proposal-confirmation guard, which is retained v1 behavior and not an AD-5/13 idempotency record. It never appends Projects events directly, adds an owner write path or state-store key family, or gains a route." Append a memlog correction entry, because the log is append-only.

### F2 — MEDIUM — "the 25 `/api/v1/projects` routes present when DW-63 started" is checkable only by archaeology and is easily confused with another "25"

**Location:** AD-24 Rule, line 321.

**Evidence.** The set is stable today. The same 25 HTTP method + template pairs (24 distinct templates; `/api/v1/projects` has both GET and POST) exist at the DW-63 ledger origin (`b1a5f6b8`, 2026-09-05), at the DW-63 spec `baseline_commit` `5f1d955b` and at `280b1eed`. However:

- The spine names no commit. "Started" can mean the ledger origin (2026-09-05) or the spec baseline (2026-11-24).
- "Route" is undefined.
- No test pins the HTTP inventory. The only "25" assertions in the host count EventStore catalog declarations (12 commands + 10 queries + 3 projections) in `tests/Hexalith.Projects.Server.Tests/ProjectsRuntimeCatalogTests.cs:44, 56, 57, 82`, which is a different set that DW-63 explicitly changes.
- A route-count freeze does not stop behavior from accumulating behind existing routes. `includeQuarantined` and `inspectQuarantined` were added to retained list/open on 2026-11-02, after DW-63 opened.

**Fix.** Name the anchor: "the 25 HTTP method + route-template pairs mapped at `5f1d955b` (DW-63 spec `baseline_commit`)". Either list them or link a checked-in inventory, and require a Server test that asserts exactly that set under `HumanPolicy`. Extend "never gains a route" to the wire contract: "…or a query parameter, request/response field or status mapping, except where an AD requires it (for example AD-3 quarantine or AD-33 inspection), recorded with a compatibility fingerprint."

### F3 — MEDIUM — AD-24 legitimizes only the adapter call, while the DW-63-compliant host carries more Projects-owned composition

**Location:** AD-24 Rule, line 321 ("only as the assembly-explicit two-line DomainService host plus one `MapProjectsHumanEndpoints` call"); seed table, line 482; "Today…" sentence, line 487.

**Evidence.** `Program.cs` calls `builder.AddProjectsDomainService()`, `app.UseEventStoreDomainService()` and `app.MapProjectsHumanEndpoints()`. `AddProjectsDomainService` (`ProjectsServerServiceCollectionExtensions.cs:52–68`) also registers:

- the Projects-owned keyed `IDomainProcessor` and replay;
- `AddProjectsServerRuntimeInfrastructure`, including Dapr infrastructure and the allow-all `AllowingProjectEventStoreAuthorizationValidator` and `AllowingProjectDaprPolicyEvidenceProvider` in production composition (`:241, :243`; AD-20 gap tracked as Nov-20 audit finding 1 / 8.8-P1);
- `AddProjectsAuthentication`;
- `ProjectsQueryResponsePolicy`, which overrides SDK `/query` failure statuses (`:63`);
- `LegacyProjectionCatalogRegistration`;
- the Development catalog.

Discovery uses `typeof(ProjectsDomainProcessor).Assembly`, which is the Server assembly, not the domain assembly. The seed table allows "plus module registration", but the Rule does not. The sentence "DW-63 governs the transition" overstates DW-63's scope. Its spec keeps the existing processor. AD-17 and Story 7.1 retire the processor, and AD-25 retires Infrastructure and ServiceDefaults.

**Fix.** Add a transitional clause to AD-24 that lists each residue item with its retirement gate:

| Residue | Retirement gate |
| --- | --- |
| Processor and replay | AD-17 writer cutover |
| Runtime infrastructure | AD-25 |
| Allow-all providers | AD-20 / 8.8-P1 |
| Response policy and Projects authentication | Retire with the adapter |
| Development catalog | Development profile only |

Align the Rule with the seed table ("two-line host plus module service registration and one `MapProjectsHumanEndpoints` call"). Reword line 487 to "DW-63, AD-17 and AD-25 govern the transition".

### F4 — MEDIUM — AD-17 still mandates the v3 normalization that the refreshed boundary row and runbook supersede

**Location:** AD-17 Read cutover, line 264 ("Compare all 77 authenticated cells under `story67-read-normalization-2026-11-16-v3`"); AD-19, line 291 ("Under v3…").

**Evidence.**

- The refreshed Read-features row (line 50) makes v4 requalification the open Epic 6 obligation.
- `docs/read-cutover.md` requires `story67-read-normalization-2026-11-22-v4`, which adds a closed `reasonCode` spelling mapping, and records that a v3-packet `Supported` startup is refused (`supported-gate-rejected`).
- DW-63 verification runs the v4 matrix.

An implementer following AD-17 literally qualifies against a revision the gate rejects.

**Fix.** "Compare all 77 authenticated cells under the active normalization revision named in the runbook (currently `story67-read-normalization-2026-11-22-v4`, Story 6.7 review decision 2026-11-22). v3 receipts keep their recorded scope." Change AD-19 to "Under v3 and v4". Add a memlog decision entry for v4.

### F5 — MEDIUM — The adapter's retirement and writer-cutover paths diverge from NFR-10/AD-31, AD-25, Deferred and Epic 7

**Location:** AD-24 Rule, line 321 ("It retires when a platform-generated surface reproduces the v1 wire shapes… and supported consumers have moved"); Deferred row, line 595; AD-25 applicability, line 329; AD-31, line 367.

**Evidence.**

- NFR-10 and AD-31 require a major version, migration notice, usage evidence, compatibility tests and rollback evidence before v1 shapes retire.
- The Deferred row requires "approved major-version and rollback plan".
- AD-25 applicability and the runbook retain the legacy path "through Epic 8 acceptance and AD-25 retirement proof". `LegacyRollback` serves the seven reads through these v1 routes.
- AD-24 can be satisfied earlier by a different test.
- After Story 7.1, FR-1 requires the v1 name-only create to admit a Durable Task (epics.md FR-1). The retained v1 membership and archive routes must follow the AD-12 saga and AD-5/13 confirmation (epics.md:2077). AD-24 still says writes go "only through `IProjectCommandSubmitter`", so Epic 7 stories will diverge on where task, confirmation and saga admission plugs into the adapter.

**Fix.** Retire the adapter only when all of these hold: the AD-24 platform-parity condition, the AD-31/NFR-10 conditions, AD-25 retirement proof, and closure of the AD-17 rollback window. Name the write port abstractly: "…only through the Projects admission port (today `IProjectCommandSubmitter` and the owner assignment directory; after AD-17 cutover, the platform task and confirmation admission of AD-4/5/12/13)". Add "retained v1 adapter retirement" to the `Projects.Client` Deferred row.

### F6 — MEDIUM — The Stack's EventStore `5.5.1` does not build the Server at `280b1eed` in package mode

**Location:** Stack table, line 461.

**Evidence.** `ProjectsQueryResponsePolicy` implements `IDomainQueryResponsePolicy`. That interface exists only in EventStore source `208af26a` (`v5.5.2-11-g208af26a5`), not in the `v5.5.1` or `v5.5.2` tags. The DW-63 spec records the Release package-mode `CS0246`. The Conventions require Release + package references in CI.

**Fix.** Add to the EventStore row's scope: "Server source at `280b1eed` also needs the unpublished `IDomainQueryResponsePolicy` (EventStore gitlink `208af26a`). Package-mode Release fails until a published EventStore carrying it is selected in Builds (DW-63 acceptance input)."

### F7 — LOW — Diagrams and seed snippet still show only the post-retirement host

**Location:** Paradigm diagram, line 64 (`Projects.Server<br/>two-line DomainService host`); code snippet, line 490; topology diagram, line 530.

**Evidence.** No diagram shows the retained v1 REST ingress (generated `Projects.Client` calls `api/v1/projects/*`), and none shows the Host's direct Conversations writes. The snippet has two lines where AD-24 now allows three.

**Fix.** Add a dashed "retained v1 adapter (AD-24, retiring)" node or edge, or caption both diagrams "post-retirement target". Add `app.MapProjectsHumanEndpoints(); // retained v1 adapter until AD-24 retirement` to the snippet.

### F8 — LOW — AD-28 Server readiness probes have no home in the two-line host

**Location:** AD-28, line 343.

**Evidence.** The Server no longer calls `AddServiceDefaults`/`MapDefaultEndpoints`. The SDK maps `/health`, `/alive` and `/ready` (`EventStoreDomainServiceRoutes.AnonymousProbeRoutes`). Neither the old nor the new host registers Projects dependency probes (Conversations, Folders, Memories, gateway). AD-24 now limits what the host may add.

**Fix.** State the mechanism (for example, SDK-discovered health contributions from the discovered assemblies, or manifest-declared dependencies probed by the platform), or record it as open under G-5.

### F9 — LOW — Bookkeeping

- The memlog "(correction by architect)" entry needs an appended correction (see F1).
- This update lists a superseding Section G byte manifest as a deliverable, but it is not present. Section G still lists the spine at `80378a8e…` (HEAD), and the working tree is now `8fa20f04…`.

### F10 — LOW — AD-20 contradiction in the refreshed "reality" text is unstated

**Location:** Writers row, line 51; line 487.

**Evidence.** Both describe the host as running on the SDK and mapping the adapter. Neither notes that production composition still registers the allow-all validators that AD-20 forbids (tracked, see F3).

**Fix.** Add one clause: "…production composition still registers the AD-20 allow-all providers (Nov-20 audit finding 1, 8.8-P1)."

## Rubric checks

| Check | Result |
| --- | --- |
| Fixes the real divergence points for Epics 6–8 and misses none | Partial. The v1 adapter's write port at writer cutover (F5), the normalization revision (F4) and the host residue (F3) are unfixed divergence points. |
| Every AD Rule is enforceable and prevents its stated divergence | AD-24 fails as worded (F1, F2). Other ADs are unchanged by this update and remain enforceable as previously reviewed. |
| Nothing under Deferred lets two units diverge | The `Projects.Client` row and AD-24 adapter retirement are unaligned (F5). Other rows are safe. |
| Ratifies rather than contradicts brownfield code | `Program.cs` and the route count are ratified. The adapter effects (F1), host residue (F3) and Stack EventStore API (F6) are contradicted. |
| Diagrams and seed consistent with amended AD-24 | Seed table row: yes. Diagrams and snippet: no (F7). |
| Operational envelope decided, deferred or open | AD-27/28 decided and provider/DR deferred. The readiness-probe mechanism is unstated (F8). |

## Verified (no finding)

- Stack: Builds gitlink `bf8a28b4` at `280b1eed`. The catalog selects EventStore `5.5.1`, FrontComposer `6.1.0`, Toolkit `17.0.0-preview.1.261120-0454`, Dapr runtime `1.19.2` and CLI `1.19.1`. `global.json` is `11.0.100`/`latestPatch`. The FrontComposer CLI tool is `6.0.1` and Module.Cli is `6.4.2`. The Platform gitlink is `f7635b59`. The 2026-11-23 G-6 policy is wired in `ci.yml` and `release.yml`, and its tuple matches the Stack.
- CI run `45430692159` (`5f1d955b`): build-and-test, P1R replay and G-6 failed. Later `4c65c0a7` CI was cancelled, and `280b1eed` CI was pending at refresh time.
- Tracker: Epic 6 in-progress, with 6.1–6.8 and the retrospective done; 7.1 blocked; 7.16 backlog; 8.1, 8.8 and 8.11 blocked.
- All 26 relative links in the spine resolve, and their Markdown anchors exist.
- The 25 `/api/v1/projects` method + template pairs are identical at `b1a5f6b8`, `5f1d955b` and `280b1eed`. They are the only Projects-mapped HTTP endpoints, all under `HumanPolicy` (`ProjectsServerServiceCollectionExtensions.cs:77`).
- The Conversations precedent: its host is SDK host + module registrations + development catalog. This supports the F3 fix wording ("plus module registration").
