# Reviewer Gate (Validate intent) — Security and tenant-isolation lens

Target: `ARCHITECTURE-SPINE.md` (status final, updated 2026-11-24, SHA-256 `01b89a2f5e076a52b17f253417b7472ae85c0c7d26ae5a3c21c428e5868eb5fe`, verified before writing). Repository HEAD `31b35637`. Inputs: spine, `.memlog.md`, PRD, epics, DW-63 and 7.1-P1 specs, `docs/`, `src/`, and EventStore source under `references/Hexalith.EventStore`. Earlier reviews and validation reports were not read.

**Verdict: FAIL.** One critical gap blocks the first writer. The spine never says where Projects actor/action/Project policy is enforced for a command that reaches the writer. The shipped writer authorizes through a test-allow validator, and nothing in the spine requires that to change before Story 7.1.

| Severity | Count |
| --- | --- |
| Critical | 1 |
| High | 4 |
| Medium | 3 |
| Low | 4 |

Gate legend: **7.1** means the gap must be closed before first-writer qualification or exposure (Story 7.1 / DW-63 acceptance). **E8** means it must be closed before release (Epic 8, 8.8-P1, G-5).

---

## Critical

### SEC-01 — Projects policy has no enforcement point at the writer, and the shipped writer authorizes with a test-allow validator

- **Severity:** Critical. **Gate:** 7.1 (re-proven at E8).
- **Spine anchor:** AD-20 Rule (line 297): "Projects evaluates actor/action/Project policy; each owner reauthorizes its resource ... Allow-all implementations exist only in explicit test composition." Structural Seed (line 487): "That runtime infrastructure also registers allowing EventStore-authorization and Dapr-policy validators, which AD-20 forbids outside explicit test composition." Line 266: "DW-63 gates Story 7.1. Prove SDK registration, authorization, dispatch ...". AD-17 (line 245) gives the SDK writer gate but says nothing about authorization.
- **Evidence:**
  - `src/Hexalith.Projects.Server/ProjectsServerServiceCollectionExtensions.cs:240-243` puts `AllowingProjectEventStoreAuthorizationValidator` and `AllowingProjectDaprPolicyEvidenceProvider` in place of the deny-all defaults at `:169-170`. `AddProjectsDomainService` always calls this at `:60`.
  - `src/Hexalith.Projects/Authorization/AllowingProjectEventStoreAuthorizationValidator.cs:22` returns `Allowed("eventstore_validator_test_allow")`. `AllowingProjectDaprPolicyEvidenceProvider.cs:8` describes itself as an "Explicit dev/test opt-in".
  - The single writer's only authorization check is that validator: `ProjectsDomainProcessor.cs:194-216`. Create, setup, archive, restore, set-folder, link/unlink and confirm-resolution all pass through it. The processor holds no Folder-derived check. That check exists only in the retained v1 adapter gate (`Authorization/ProjectAuthorizationGate.cs:504-557`, where both of these layers are also the allowing stubs).
  - The v1 adapter submits by forwarding the caller's bearer token to the EventStore human command API (`EventStoreGatewayTokenForwardingHandler.cs:17-22,39-50`, wired at `ProjectsServerServiceCollectionExtensions.cs:281-288`). That API is `[Authorize(HumanPolicy)]` (`references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs:74`). Its default RBAC fails open when a token has no domain or permission claims, and `command:submit` covers every command type (`.../Authorization/ClaimsRbacValidator.cs:40-58,70-82`). So any token the v1 adapter can forward is, by construction, accepted by EventStore for Projects commands.
  - The decision log sends the gap to release, not to the first writer: `.memlog.md:104` says "Route to the Projects Server owner and 8.8-P1 evidence". `epics.md:2770,2774` shows that 8.8-P1 is blocked by 8.3–8.5, G-4 and G-5.
- **Failure scenario:** A Project User with Folder read only, or a Tenant member with no Folder authority, posts `ArchiveProject`, `SetProjectFolder` or `LinkFileReference` for another user's Project directly to EventStore `/api/v1/commands`. The adapter gate never runs, and the processor's validator returns test-allow. The command commits against a confidential Project. Story 7.1 then qualifies a creation writer on the same composition, inheriting a writer that trusts every admitted command.
- **Proposed resolution:** Amend AD-20 (or AD-9) with a rule that binds the enforcement point:
  - "Projects actor/action/Project policy is evaluated at governed admission (Durable Task admission or the Gateway invocation that carries the AD-20 context), and the selected writer accepts a Projects command only with that admission's evidence. No ingress, including the EventStore human command API, can reach the `projects` writer without it."
  - "Before any writer qualification (Story 7.1, DW-63 acceptance), every shipped composition registers deny-by-default or real EventStore-authorization and Dapr-policy evidence. Allowing implementations are absent from the Release Server."
  - Move the Structural Seed note into the 7.1 entry gate, not only 8.8-P1.
- **Acceptance check:**
  1. A composition test on the Release Server binary asserts that neither registered implementation is an `Allowing*` type.
  2. A persisted test submits `ArchiveProject` through the EventStore human command API with a token that lacks Folder manage, and observes a rejection with the stream unchanged.
  3. Both rows appear as 7.1 entry-gate evidence.
- **Status:** CONFIRMED for the composition (code). PLAUSIBLE for exploitability through deployment ingress, which depends on EventStore command-API exposure and IdP claims.

---

## High

### SEC-02 — The ChatBot Project-level grant subsystem contradicts AD-3 and has no governing AD; its authority query discloses existence and version

- **Severity:** High. **Gate:** 7.1 for the disposition (Chatbot is a 7.1 creation surface). E8 for the disclosure fix.
- **Spine anchor:** AD-3 (line 132): "Permitted Projects are Folder-derived. Projects issues no Project-level grant". AD-19 (line 289) names `ResolveProjectChatBotAuthority` only inside the invalid-input exception: "Single-target access denial and absence share a safe `404`". AD-2 (line 126).
- **Evidence:**
  - `src/Hexalith.Projects.Contracts/Commands/GrantProjectChatBotAuthority.cs:3`: "Issues an exact, expiring ChatBot project grant after current owner admission". There are matching `Revoke...`, `ProjectChatBotAuthorityGranted/Revoked/Rejected` events (`docs/event-catalog.md:185-221`) and `ProjectState.ChatBotGrants`.
  - Grants are admitted on Tenants-owner and Parties evidence (`Authorization/ProjectOwnerBootstrapVerifier.cs:23-26,43-46`; `ProjectsDomainProcessor.cs:234-244`), not on Folders.
  - PRD `prd.md:37`: "Projects issues no Project-level grants of its own ... grants and revocations happen in Folders".
  - `Queries/ProjectChatBotAuthorityQueryHandler.cs:61-65`: an absent stream (`Head <= 0`) returns `Failure("Forbidden")`, which maps to 404. `:91-98`: an existing Project with no grant returns a 200 payload with `allowed=false` plus `source.Head`, `ObservationId` and `ObservedAt`. No Folder or read check on the delegated actor runs first (`:49-57` checks only workload `chatbot`, delegation and identity equality).
- **Failure scenario:**
  - A Chatbot workload acting for actor U can tell an existing Project (200 with its version) from a non-existent one (404) anywhere in U's Tenant, including Projects U cannot read. That is the denial-versus-absence oracle AD-19 forbids.
  - Separately, a Tenant owner's grant can let Chatbot treat U as authorized on P after Folders has revoked U's read. Projects and Folders then disagree on who authorizes, and the AD-3 Folder-derived model is bypassed.
- **Proposed resolution:** Add one bounded statement, in AD-3 or a dedicated AD, that fixes three things:
  1. Disposition: either retain the ChatBot grant as an owner-only, non-authorizing compatibility artifact with a named retirement trigger, or declare it a sanctioned exception to AD-3 with its authority source (Tenants owner) and how it composes with Folder-derived authority. The composition rule should be "grant is necessary, never sufficient; Folder authority is still required".
  2. Revocation: Folder revocation wins immediately.
  3. Disclosure: `ResolveProjectChatBotAuthority` returns the same safe 404 for absent, denied and ungranted, with no head or observation, unless the delegated actor currently has Folder read.
- **Acceptance check:**
  1. Handler test: Project exists and actor has no Folder read, versus Project absent. Both cases give an identical status and body.
  2. A grant present with Folder read revoked is denied at Chatbot.
  3. The spine names the disposition and the 7.1/7.12 consumers.
- **Status:** CONFIRMED (code and PRD). PLAUSIBLE for the Chatbot consumer behavior: Chatbot source was not inspected, and `docs/read-cutover.md:43` says only that the query is "owner-only".

### SEC-03 — Retained v1 confirmation routes accept Boolean confirmations from any human bearer token

- **Severity:** High. **Gate:** 7.1 (proposal and creation confirmation share the 7.1-P1 validator path). E8 for the McpCli containment claim.
- **Spine anchor:**
  - AD-13 (line 217): "Consumption occurs only inside the Projects-owned confirmation component (no confirm API)."
  - AD-20 (line 297): "Only a registered confidential Chatbot or Web adapter may host the human confirmation component ... the retained AD-24 v1 routes accept only authenticated human callers under the platform human policy."
  - AD-29 (line 353) and AD-33 (line 416): "both public heads refuse UI-only and confirmation-required actions".
  - AD-24 (line 321) governs v1 mutations only "From that cutover".
- **Evidence:**
  - v1 routes `POST .../resolution/confirm` and `POST /api/v1/projects/proposals/confirm` (`ProjectsDomainServiceEndpoints.cs:292,364`), plus move, unlink, folder PUT, archive and restore (`:344,386,406,521,532`).
  - They authorize only through claim-permission gates (`Queries/ConfirmProjectResolutionEndpoint.cs:70`; `ProposeNewProjectEndpoint.cs:205`).
  - The human policy is "bearer-only ... rejects workload and mixed credentials" with no interactive-session (A-7) or registered-adapter requirement (`references/.../EventStoreHttpHostSecurityExtensions.cs:53-69`).
  - The proposal ledger is per-replica (`ProposeNewProjectEndpoint.cs:309`; spine line 321).
  - `.memlog.md:101` acknowledges that the v1 shapes carry "the Boolean confirmations AD-5 forbids".
- **Failure scenario:**
  - An agent or script holding the user's OAuth token, for example from an MCP or CLI device login or any client of the same audience, calls `/resolution/confirm` or `/proposals/confirm` directly. It confirms an ambiguous resolution or a proposed creation without the confidential component, which bypasses AD-29 containment.
  - After the writer cutover, AD-24 does not say what these routes return. Implementers could map the Boolean to an artifact, which AD-13 forbids.
- **Proposed resolution:** Amend AD-24 or AD-20 with three rules:
  1. "Retained v1 routes for confirmation-required actions accept only the registered confidential Chatbot/Web adapter credential carrying the A-7 interactive-session claim. Any other human bearer receives the AD-19 safe denial."
  2. "From the writer cutover they never consume a Boolean confirmation. They return `RequestPreview` or `RenewPreview`, and confirmation happens only in the Projects-owned component."
  3. Name the interim (pre-A-7) disposition explicitly: fenced off, or accepted risk with an owner.
- **Acceptance check:**
  1. With a valid human token that carries no session or adapter credential, every v1 confirmation-required route gets refused, with no command, task or audit mutation.
  2. After cutover, the same routes return `RequestPreview` for a registered adapter as well.
- **Status:** CONFIRMED (code and EventStore policy source).

### SEC-04 — The retained adapter forwards raw human bearer tokens to EventStore, Conversations and Folders

- **Severity:** High. **Gate:** 7.1 (this is what makes SEC-01 reachable, and pre-cutover writes travel over it). E8 for audience and delegation evidence.
- **Spine anchor:** AD-20 Prevents (line 295): "service-only authority, raw-token forwarding, confused-deputy behavior". Rule (line 297): context carries "delegation identifier/scopes/audience". AD-24 (line 321) covers only inbound authentication of the v1 adapter.
- **Evidence:**
  - `EventStoreGatewayTokenForwardingHandler.cs:17-22`: "Forwards the authenticated caller's bearer token ... onto the outbound EventStore command-gateway call".
  - It is attached to the EventStore gateway client (`ProjectsServerServiceCollectionExtensions.cs:285-288`), the Conversations client (`:256-261`), the generated Folders client (`:263-268`) and the Folders v1 read client (`:270-274`).
  - Inbound validation pins one audience (`Authentication/ProjectsAuthenticationServiceCollectionExtensions.cs:88-90`), so EventStore, Conversations and Folders must accept that same audience.
- **Failure scenario:** One token audience is honored by four services. A token stolen or replayed from any of them, or a lower-trust client of that audience, can act directly at EventStore (SEC-01), Conversations or Folders with the user's full authority. Owner-side logs cannot tell Projects-mediated calls from direct ones, so actor/workload attribution in AD-26 audit is lost.
- **Proposed resolution:** AD-20 should say: "Projects never forwards an inbound bearer token. Outbound owner and EventStore calls use platform-issued, audience-scoped delegated credentials carrying the delegation identifier (dual principal)." If retained v1 behavior must keep forwarding until cutover, name it as a bounded exception with an owner, a distinct-audience requirement and a retirement trigger before the first writer is exposed.
- **Acceptance check:**
  1. A captured outbound request from the Release Server to each owner carries a callee-audience credential, never the inbound token bytes.
  2. The owner rejects a Projects-audience token presented directly.
- **Status:** CONFIRMED (code). PLAUSIBLE for the shared-audience deployment consequence.

### SEC-05 — Tenant selection for multi-tenant principals is token-order dependent on v1 routes and envelope-owned on the Gateway

- **Severity:** High. **Gate:** 7.1 (creation places a confidential Project and Folder in a Tenant).
- **Spine anchor:**
  - Line 43: "Tenant selection is envelope-owned". This appears only in the McpCli note.
  - AD-18 (line 283): "Tenant and actor come from authenticated server context".
  - AD-11 (line 205): the same-Tenant predicate covers "the acting session".
  - Conventions (line 448): "Tenant and actor are never trusted from body/header substitutes".
- **Evidence:**
  - `ProjectsClaimsTransformation.cs:58-71` expands a `tenants` array into several `eventstore:tenant` claims.
  - `HttpContextProjectTenantContextAccessor.cs:46-48` takes `FindFirst` of `eventstore:current-tenant`, then `eventstore:tenant`, then `tenantId`.
  - An explicit header or query selection is rejected as `tenant_mismatch` (`ProjectAuthorizationGate.cs:429-437,593-601`).
  - The SDK query path uses the verified envelope Tenant instead (`Queries/VerifiedProjectQueryTenantContextAccessor.cs:9`).
- **Failure scenario:**
  - A consultant in Tenants A and B uses a retained v1 surface intending B. The first-listed claim, ordered by the IdP, routes the create to A, so a confidential Project name and its Folder land in A, visible to A's administrators.
  - The same session on the Gateway path selects B, so v1 and Gateway disagree on tenant, actor attribution and audit for the same principal.
  - The acting-session predicate in AD-11 is undefined when the session holds several Tenants.
- **Proposed resolution:** AD-20 should say: "Each request binds exactly one Tenant: the Gateway envelope Tenant verified against the principal's Tenant set, or, on retained v1 routes, a single current-tenant claim. A principal with more than one Tenant and no verified explicit selection is denied with the safe 404. First-claim selection is forbidden."
- **Acceptance check:**
  1. A token with tenants `[A,B]` and no current-tenant claim is denied on v1 create and list.
  2. With `current-tenant=B`, both v1 and Gateway act in B only.
  3. A persisted check confirms A is untouched.
- **Status:** CONFIRMED (code).

---

## Medium

### SEC-06 — No rule rechecks the original actor's authority while a task runs

- **Severity:** Medium. **Gate:** 7.1 (the creation task spans a Folder call and a Project commit).
- **Spine anchor:**
  - AD-4 (line 138) rechecks the original actor only "From `NeedsAttention`".
  - AD-13 (line 217) binds "authorization-evidence version" at admission.
  - AD-20 (line 297) requires evidence "no older than the interactive dependency timeout" in the immutable context.
  - AD-12 (line 211): "each owner call carries ... expected owner version".
- **Evidence:** The spine is silent on revocation between admission and commit for `Running` and `WaitingForDependency`. It also never says which credential later owner steps use once the interactive session has ended. G-5 (line 557) lists "key rotation/revocation" only for keys.
- **Failure scenario:** U is admitted for creation, the Folder is created, and U is then removed from the Tenant or loses manage. The task resumes after a retry window and commits `ProjectCreated` for a principal who no longer has authority. Alternatively, a later step replays stale delegated evidence.
- **Proposed resolution:** AD-4/AD-12 should say: "Before the irreversible checkpoint and at each Project commit, re-evaluate the original actor's current Tenant membership and action authority, using the same predicate as reconciliation. A confirmed denial goes to `Rejected` (or to `NeedsAttention` under AD-8 when a Folder exists) with one audit outcome. An unavailable check means `WaitingForDependency` and never a commit. Owner steps use per-step platform-issued delegated credentials bounded by the task, never the interactive token."
- **Acceptance check:** In a persisted test, revoke tenant membership after the Folder receipt and before the commit. The result has no `ProjectCreated`, a terminal or attention state as specified, and one audit row.
- **Status:** PLAUSIBLE (design gap; the G-1 engine does not exist yet).

### SEC-07 — AD-31 never names who assigns `metadataClass` or what it governs

- **Severity:** Medium. **Gate:** 7.1 (production owner of FR-19 per `epics.md:126`).
- **Spine anchor:** AD-31 (line 371): "assigned by authenticated integration policy and never inferred from user text".
- **Evidence:**
  - v1 create takes `projectMetadata.metadataClass` from the caller's JSON body (`ProjectsDomainServiceEndpoints.cs:621-625`), and the resulting `CreateProject` command carries no classification (`:662-671`).
  - The 7.1-P1 spec matrix says: "The classification is not persisted."
- **Failure scenario:**
  - A human, an agent with a user token, or a Service caller labels credential-bearing descriptive metadata `public_metadata`.
  - Chatbot, Web and the Durable Task writer each derive or accept the class differently, because the spine names no assigning principal.
  - Downstream disclosure (AD-33 Safe Metadata, AD-21 export, AD-26 audit) cannot depend on a value that is never stored.
- **Proposed resolution:** AD-31 should name the assigning authority, for example "the registered integration adapter's workload identity under platform policy; a value presented by any other principal is rejected 400". It should also state whether the class is persisted in the creation event and which disclosure rules consume it, or say outright "validated only in v1; grants and restricts nothing".
- **Acceptance check:**
  1. A plain human token supplying `metadataClass` gets the defined outcome.
  2. Event inspection matches the stated persistence rule.
  3. Direct, proposal and Durable Task paths agree.
- **Status:** CONFIRMED (code and 7.1-P1 spec).

### SEC-08 — No system of record for Tenant-role and session-mode evidence

- **Severity:** Medium. **Gate:** 7.1 (Service/Workflow and Project User creation personas). E8 for Administrator flows.
- **Spine anchor:**
  - AD-33 (line 387): "A session exercises either Project User authority or Tenant-role authority."
  - AD-10 (line 199): "Tenant-role authority recognized by Conversations".
  - G-2 (line 554): "Folders and Conversations must recognize the applicable Tenant roles under A-3".
- **Evidence:** Three different sources currently establish tenant and role evidence:
  - JWT permission claims (`HttpContextProjectTenantContextAccessor.cs:89-90`).
  - Projects' own tenant-access projection (`src/Hexalith.Projects/Authorization/TenantAccessAuthorizer.cs:15`, used at `ProjectAuthorizationGate.cs:450-476`).
  - The Hexalith.Tenants owner query (`ProjectOwnerBootstrapVerifier.cs:23-26`).

  The spine names neither the authority for Tenant Operator and Tenant Project Administrator nor where the session's mode is recorded.
- **Failure scenario:**
  - Projects treats a principal as Tenant Project Administrator from a cached projection while Conversations or Folders deny it, or the reverse. The move or replace Preview then promises an outcome the owners refuse.
  - A Web session carrying both Folder grants and an Administrator role combines them, against AD-33.
- **Proposed resolution:** The spine should:
  - Name the Tenant-role system of record.
  - Add a versioned "Tenant-role evidence" and "session mode" field to the AD-20 immutable context, fixed at admission and passed to every owner.
  - State that any Projects-side projection is a freshness-bounded cache, never mutation authority.
- **Acceptance check:**
  1. When a role is revoked in the system of record, Projects, Folders and Conversations all deny within the stated freshness bound.
  2. A dual-capable principal on Web exercises only Tenant-role authority.
- **Status:** CONFIRMED (three sources in code). PLAUSIBLE for the owner disagreement.

---

## Low

### SEC-09 — The AD-19 bounded exception says "same safe 404" but bodies differ

- **Severity:** Low. **Gate:** 7.1 (DW-63 regression and v4 comparison expectations).
- **Spine anchor:** AD-19 (line 289): "invalid input to those queries returns the same safe `404` as denial, with no protected detail".
- **Evidence:**
  - `Queries/ProjectsQueryResponsePolicy.cs:21-26` returns `NotFound(result)` with the raw body for the four named types.
  - `GetProjectContextQueryHandler.cs:43-48` returns `safe-denial` before authorization but `InvalidEnvelope` after it. The ChatBot handler returns `Forbidden`. `.memlog.md:107` confirms "bodies carry only safe-denial, InvalidEnvelope or Forbidden".
- **Failure scenario:** Implementers or tests take "same as denial" literally and either normalize the bodies, which hides the invalid-paging signal from authorized callers, or treat `InvalidEnvelope` as leak-free. The latter holds only because validation follows authorization, and the spine does not state that ordering.
- **Proposed resolution:** Reword as "status `404`. The body is `safe-denial` for denial, absence or unparseable target; `InvalidEnvelope` only after successful authorization; and the ChatBot query follows SEC-02."
- **Acceptance check:** Handler tests pin body per cause, and the unauthorized-caller cases are all byte-identical.
- **Status:** CONFIRMED.

### SEC-10 — Pre-cutover v1 create accepts caller-chosen ProjectId, task ID and correlation ID

- **Severity:** Low. **Gate:** 7.1 cutover.
- **Spine anchor:** AD-18 Prevents (line 282): "caller-selected aggregate identity". AD-24 (line 321) requires platform identities only "From that cutover".
- **Evidence:** `ProjectsDomainServiceEndpoints.cs:650-652` uses `body.ProjectId` or else `Guid.NewGuid()`. `:554-566` takes `X-Hexalith-Task-Id` and `X-Correlation-Id` from headers.
- **Failure scenario:** A caller creates with a guessed ProjectId of a Project it cannot read. The command status (rejection versus acceptance) reveals whether it exists. Caller-chosen task and correlation IDs pollute audit correlation.
- **Proposed resolution:** AD-24 should state that pre-cutover create outcomes for an existing ProjectId are indistinguishable from other denials, and that the canonical `v1` shape rejects caller ProjectIds.
- **Acceptance check:** Create with an existing foreign-actor ProjectId and with an absent one, then poll both. Both yield the same observable outcome.
- **Status:** PLAUSIBLE (the oracle depends on the status surface).

### SEC-11 — The domain host reads a raw RSA private signing key from configuration

- **Severity:** Low. **Gate:** E8 (G-5).
- **Spine anchor:** AD-28 (line 347): "The platform AppHost owns ... identity/KMS/secrets and rotation". The Deferred row (line 592) leaves KMS mechanics to G-5.
- **Evidence:** `Authorization/ProjectChatBotAuthorityGateway.cs:74-85` imports a PKCS#8 key from `configuration["Authentication:BoundStreamRead:PrivateKey"]` to sign bound read proofs.
- **Failure scenario:** The key lives in an environment variable or settings file, is copied into CI or local manifests, and has no rotation or revocation path. Whoever holds it can sign stream-read proofs for any Project.
- **Proposed resolution:** AD-28/G-5 should say that workload signing material is platform-issued through the identity/KMS binding (handle or remote signer), never an `IConfiguration` value in a domain host.
- **Acceptance check:** The Release composition holds no private key in configuration, and the key can be rotated without redeploying Projects.
- **Status:** CONFIRMED.

### SEC-12 — AD-31 "authorization precedes parsing" is unconditional, but retained proposal confirm parses first

- **Severity:** Low. **Gate:** 7.12 (deferred). The spine anchor is missing.
- **Spine anchor:** AD-31 (line 371): "Authorization precedes protected metadata parsing".
- **Evidence:** `Queries/ProposeNewProjectEndpoint.cs:191` reads the body before `AuthorizeCreateAsync` at `:205`. The 7.1-P1 spec (Q2=A) routes the fix to a DW entry for Story 7.12.
- **Failure scenario:** An unauthorized human probes the confirm route and gets 400-versus-404 parsing feedback. Reviewers read AD-31 as already satisfied.
- **Proposed resolution:** Add a bounded exception to AD-31 that names the route, its owner and 7.12 as the point where it closes.
- **Acceptance check:** A zero-body-read test passes for an unauthorized confirm once 7.12 lands.
- **Status:** CONFIRMED.

---

## Checked and held up

- The Development anonymous bypass is unreachable on the shipped Server. `AddProjectsDomainService` throws (`ProjectsServerServiceCollectionExtensions.cs:54-57`), and options validation rejects the bypass outside Development.
- Inbound JWT validation pins exact issuer and audience, requires signed tokens and asymmetric algorithms outside Development, requires a single string `sub`, and rejects `act` without a delegation ID (`ProjectsAuthenticationServiceCollectionExtensions.cs:61-109`).
- Client-supplied tenant and principal headers or query values cannot override claims (`ProjectAuthorizationGate.cs:429-440,593-611`).
- The platform human policy rejects workload, channel and mixed credentials (`EventStoreHttpHostSecurityExtensions.cs:53-69`).
- The supported SDK query path binds Tenant and actor only from the verified envelope with authenticated-caller origin (`ProjectQueryEnvelopePrincipalBinding.cs`, `VerifiedProjectQueryTenantContextAccessor.cs`).
- For the six normalized query types, non-validation failures collapse to `safe-denial` 404. On the other handlers, denial precedes validation, so unauthorized callers get no validation oracle.
- v1 create authorizes before reading the body (`ProjectsDomainServiceEndpoints.cs:568-575`), and the submitter collapses 401/403/404 to safe denial (`EventStoreProjectCommandSubmitter.cs` `ToProblemOutcome`).
- AD-11 evaluates the same-Tenant predicate before authorization, and `ProjectQueryTenantFilter` applies it to detail.
- AD-13 is fail-closed on delegated or non-interactive claims, uses a hashed opaque token and atomic consume-admit. AD-5 idempotency is scoped by actor, and the AD-19 409 is nondisclosing.
- AD-4 and AD-12 reconciliation recheck the original actor at commit, and "unavailable cannot establish denial or authorize".
- AD-29, AD-30 (line 363) and AD-33 consistently bar public CLI/MCP confirmation and Selection Evidence, and Gateway, then Projects, then owner order is stated the same way in AD-2, AD-29 and G-3.
- AD-3 and AD-8 consistently give Projects' workload no Folder authorization, a sole manager, and no Tenant-default read.
- AD-35 requires a nondisclosing refusal for a competing claim.
- AD-26 and AD-21 consistently require surrogates, Tenant-salted pseudonymous actor IDs, and analytics that never authorize.
