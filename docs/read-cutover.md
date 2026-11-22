# Supported read cutover and rollback

Story 6.7 adds an executable selection boundary. The [final source acceptance packet](../_bmad-output/implementation-artifacts/evidence/epic6/6.7/review-pass2-fixes/read-cutover-final-1.json)
binds all 77 passing authenticated comparisons to actual native xUnit XML. The
[explicit rollback proof](../_bmad-output/implementation-artifacts/evidence/epic6/6.7/review-pass2-fixes/rollback-final-proof-1.json)
executes all seven retained HTTP reads after the production selector verifies the packet.
Primary Web reads still default to Unavailable; selecting Supported or LegacyRollback
requires explicit startup configuration. Historical readiness remains NOT_READY;
Epic 7/8 and AD-25 pins are unchanged. This source acceptance does not publish or deploy.

## Inventory

The public supported reads use the existing EventStore Gateway and its authenticated
query envelope. The receiver remains the registered Projects `/query` composition.
Contracts owns each request/result pairing through `QueryResultContractAttribute`;
EventStore's existing RestApi.Generators emits typed Gateway bindings consumed by
Projects.Client. Runtime schemas use the same declarations. No new REST endpoints,
writer, replacement generator, or domain persistence is introduced.

| Read | Supported request → result | Retained v1 REST path |
| --- | --- | --- |
| List | ListProjectsQuery → ProjectListPage | GET /api/v1/projects |
| Open | GetProjectQuery → GetProjectResponse | GET /api/v1/projects/{projectId} |
| Conversation start | GetConversationStartSetupQuery → ConversationStartSetupResponse | GET /api/v1/projects/{projectId}/setup/conversation-start |
| Context | GetProjectContextQuery → ProjectContextReadResponse | GET /api/v1/projects/{projectId}/context |
| Explain | ExplainContextSelectionQuery → ExplainContextSelectionResponse | GET /api/v1/projects/{projectId}/context/explain |
| Conversation suggestions | ResolveProjectFromConversationQuery → ProjectAdvisoryResolutionResponse | GET /api/v1/projects/resolution/from-conversation |
| Attachment suggestions | ResolveProjectFromAttachmentsQuery → ProjectAdvisoryResolutionResponse | GET /api/v1/projects/resolution/from-attachments |

All discriminators retain `Hexalith.Projects.Queries.<operation>.v1` wire names.
List and suggestions target aggregate `projects`; other reads target the exact
opaque Project identity. Tenant and original actor, workload, delegation, scopes,
audience and action come from authenticated authority, never a result or discovery
row. Current Folder permission is independently checked where required. Ordinary
reads return Safe Metadata. Descriptive and quarantine inspection require separate
authorization and a successful metadata-only audit before disclosure.

Legacy-only reads remain: context refresh, Project conversations, operator
diagnostics, and proposal preview. Existing maintenance/write surfaces retain their
own admission. They are not substitute supported reads or accepted rollback proof.
`ResolveProjectChatBotAuthority` is owner-only and outside the seven public reads.

### History, keys and projections

The existing event folds retain ProjectCreated, ProjectSetupUpdated,
ProjectFolderSet, ProjectFolderCreationPending, FileReferenceLinked,
FileReferenceUnlinked, MemoryLinked, MemoryUnlinked, ProjectArchived,
ProjectRestored and ProjectResolutionConfirmed. The supported projection explicitly
classifies grant/revoke and rejection events as harmless position-advancing events;
unknown events fail closed. No historical event is renamed, rewritten or repaired.
Name-only creation remains accepted by the unchanged CreateProjectPayload and v1
client signature. Its UI-only/confirmation-required classifications are unchanged.

| Data | Store / physical key | Position and partial-record behavior |
| --- | --- | --- |
| Legacy list/detail/reference/audit folds | statestore / projects:projection-journal:{tenantId} | Tenant journal retains global position; Project row Sequence is its last source global position, distinct from the aggregate sequence. Absent, malformed and conflicting journals fail closed. |
| Supported enumeration | statestore / projects:reads:v1:{tenant-length}:{tenant}:index | ProjectReadIndex retains the exact global head and ordered identifiers; atomic batches retain prior values during incomplete writes. |
| Supported detail | statestore / projects:reads:v1:{tenant-length}:{tenant}:{project-length}:{project} | ProjectReadModel retains exact global position and Project sequence; stale/rebuilding/corrupt records cannot become healthy output. |
| Context/setup/explain source | projects-conversation-start-setup / {tenant}:projects:{project} | ConversationStartSetupProjectionHandler folds the same event history into ProjectDetailItem; sequence and UpdatedAt remain exact. |
| Tenant authority | statestore / projects:tenant-access:{tenant} | Tenant-owned watermark, observation, freshness and original-principal evidence remain independent of Project state. |
| Inspection/denial audit | statestore / projects:read-audit:v1:{SHA-256 surrogate} | Metadata-only audit writes are accounted separately from domain writes; audit failure prevents descriptive disclosure. |

Canonical key comparison uses EventStore AggregateIdentity, preserving physical
keys and rejecting collisions. Identifiers generated by the platform are ULIDs;
opaque historical Project, Conversation, Folder and owner reference identifiers
remain readable without GUID parsing. Folderless and Pending-without-Folder
Projects remain quarantined. Reads neither create a Folder nor finish an in-flight
command, repair a projection, confirm a suggestion, or mutate an event stream.

### Cursors

Supported list pages default to 50 and cap at 200; authority filters precede
pagination. The platform IQueryCursorCodec protects a ProjectReadCursorPosition
containing the Project boundary and original 15-minute expiry. Its independent
scope includes Tenant, actor, workload, delegation, scope/audience, action,
lifecycle, page size, inspection/quarantine mode, policy, Tenant/EventStore
watermarks, index ETag, visibility digest and exact global position. Continuations
retain their original expiry. Invalid, expired, wrong-filter and foreign-authority
cursors fail closed. Other supported reads reject paging rather than inherit an
unscoped continuation. Legacy list does not provide the same supported cursor;
legacy Conversation paging is a distinct legacy-only contract. Byte equality of
opaque tokens is never used as cursor equivalence.

### Consumers

| Consumer | Migration state |
| --- | --- |
| Primary Web `/`, `/projects`, `/projects/{id}` and warnings entry | One startup choice selects supported, explicit verified rollback, or unavailable. |
| Web `/inspect/projects` and detail/suggestions | Existing delivered inspection surface now consumes Contracts-generated Gateway bindings. |
| Existing IClient / v1 REST integrations | Original method signatures and creation payload remain; generated output/fingerprints are regenerated from the spine. |
| Obsolete `Hexalith.Projects.Cli` / `Hexalith.Projects.Mcp` | Retained v1 callers have not opted into descriptive inspection: list/describe safely return 404; unbound refresh/re-evaluate return 503. Compatibility repair is deferred (Story 6.7 review decision 1; [consumer migration deferral](../_bmad-output/implementation-artifacts/deferred-work.md)). These proprietary surfaces are superseded by McpCli. |
| Shared Hexalith.McpCli | Existing decorated list/describe/advisory enrollment remains; this change does not add a proprietary CLI or grant additional authority. |
| External Hexalith.Chatbot | Adoption not established. No external consumption or first-response evidence is inferred from Web tests. |

## Selection and rollback

Configure `Projects:Reads:Mode` as `Supported`, `LegacyRollback`, or `Unavailable`.
Use the same section's EvidencePath, EvidenceHash (SHA-256), SourceRevision (full
Git identifier) and ContractHash (SHA-256). Restart/redeploy applies a choice.
Relative `EvidencePath` values resolve against the process working directory; native receipt paths resolve against the evidence file directory. Use an absolute EvidencePath when the service working directory may vary. Startup logs the selected mode and a bounded refusal reason once; inspect that reason before correcting configuration or evidence and restarting.
Selection is immutable for the process. A denial, timeout, regression or dependency
failure never changes the route or retries through legacy.

The hash-bound receipt must use normalization revision
`story67-read-normalization-2026-11-22-v4`, match the selected application revision
and contract hash, prove contract/identity compatibility, and contain every
operation/scenario cell from ProjectReadCutoverGate. Duplicate, missing, skipped,
unexecuted or failing cells refuse activation. Every cell must also prove persisted
no-domain-write behavior; a passing summary cannot replace that field. Every cell binds to a named passing
test and an actual hash-verified native xUnit XML receipt with no failures or skips.
Native receipt paths are relative to the evidence file. Synthetic unit controls
are labeled as controls and never copied into the acceptance matrix as proof.

Explicit LegacyRollback requires separate current security receipts for all seven
legacy slices: current authority, Safe Metadata, indistinguishable denial and
persisted no-domain-write behavior. Missing or failed legacy proof selects
Unavailable. Writer rollback and legacy-runtime retirement remain separate gates.
Retain legacy implementations until Epic 8 acceptance and AD-25 retirement proof.
In LegacyRollback the primary Web pages (`/`, `/projects`, `/projects/warnings` and
`/projects/{id}`) read through retained v1 list/open with explicit descriptive
inspection (approved amendment 4). Their users need the descriptive-inspection
permission (`projects:inspect_descriptive_metadata`); other users receive the safe
not-found result. Every page load writes a durable metadata-only inspection audit
before names are shown.

Approved normalization permits object property order only without extra proof.
Lifecycle spelling, Folder field alignment, Sequence/ProjectVersion,
ObservedAt/AsOf, and null Setup/current-empty mappings require the identical enum,
authorized reference, source sequence, observation, or independently verified
current-empty evidence respectively. Exact timestamps and array order remain.
Raw keys, cursor behavior and supported-only admission assertions remain separate.
Revision v4 (Story 6.7 review decision, 2026-11-22) adds one closed spelling mapping
for the retained v1 reference-summary `reasonCode`: `reference_unavailable` to
`ReferenceUnavailable` and `owner_read_unverified` to `OwnerReadUnverified`. Retained
v1 open emits the snake_case spellings its schema requires; no other value is mapped.
The historical story61 comparator revision is retained as historical evidence.

Snapshot `asOf` and Conversation-start Setup `ObservedAt` are the current computation instant from the injected clock. Component/reference last-verified observations retain the original source evidence times and describe evidence age. This interpretation was explicitly retained by Story 6.7 review decision 2; it does not introduce timestamp tolerance into comparisons.

## Recorded repairs and release obligations

DW-57: retained Conversation-start reports its actual Fresh/Stale value and exact
authorized legacy-source global-position watermark, distinct from supported ProjectVersion. DW-59: its declared handler statuses are 200, 400,
safe 404 and retryable 503; Gateway authentication is separate. V1 client method
signatures remain. DW-60: all retryable context authorization outcomes return 503,
independent of reason spelling. DW-66: optional reference omissions preserve
Partial, References component evidence and recovery guidance in the supported
Conversation-start snapshot; the legacy ConversationStartSetup DTO is unchanged.
Previously completed cache, idempotency-key, collection-ownership and Folder-state
repairs are retained.

Context/setup/explain now accept additive `ConversationId` metadata. Legacy refresh
forwards the same explicit Conversation through its compatible client overload and UI
action, rechecks native membership after all owner reads, and sanitizes returned metadata. The existing
Conversation owner read verifies the original actor, native Tenant/Conversation/Project
identity and current membership. Its delegated Projects mode returns bounded native
evidence only; direct authorized owner reads retain their full result. First-response
usability is established by that separate named membership read, never by list/open
Complete or `contextActivation.enabled`. Missing or unavailable native membership yields
no usable Setup: retained reads return 503, and supported reads return Unavailable with
cause `MissingOrStaleRequiredContext` and recovery `RefreshContext` (Story 6.7 review
decision, 2026-11-22). Session/opened-Project fallback remains deferred.

Legacy list/open require `inspectDescriptiveMetadata=true`, the existing inspection
permission and successful durable metadata-only audit. Original generated client methods
and constructors remain callable; additive overloads carry inspection/Conversation input.
New generated IClient overloads have default implementations, so old implementers keep
working for old calls and safely reject unsupported new metadata. This source compatibility
is distinct from unperformed package publication and external consumer adoption.

The retained Folder read adapter explicitly uses the owner's two existing v1 GET
contracts (lifecycle-status and effective-permissions), forwarding the authenticated
bearer with the existing handler. The current Folders generated IClient is sourced
from its explicitly non-routed PD10 v2 candidate and would otherwise call `/api/v2/`
against a v1-only owner. The bounded read adapter consumes the authoritative v1
schemas directly; there is no prefix rewrite, retry after denial, owner edit, generator
fork or writer transport change.

The v3 comparator keeps legacy global Sequence and supported aggregate ProjectVersion
separate and checks each against original event envelopes. Legacy freshness observations
remain source timestamps; supported AsOf is the injected computation clock. There is no
timestamp tolerance. Typed optional nulls and current-empty Setup map only with exact
independent source proof; raw responses, keys, logical records, ETags and positions remain
in the receipt. Finite stale-Tenant, native-binding-unavailable, unsupported File and degraded
advisory outcome pairs require their own authority, nondisclosure and AD-32/coverage proof.
D6 additionally covers only original historical Pending Folder materialization evidence:
legacy 503 versus supported Unavailable/null Setup/PollTask, with current authority,
native membership, quarantine and unchanged history independently asserted.

The authenticated matrix writes diagnostics before requiring every one of its 77 cells
to pass. Its Partial row uses original MemoryLinked and ProjectSetupUpdated exclusion events,
with actual Partial admission, exact omission evaluations and recovery evidence; advisory
rows prove incomplete coverage, including mixed current Folder/unsupported File input.
Physical and logical persisted records, source envelopes, exact positions, ETags and
unexpected SDK writes are checked throughout cursor and rollback sequences. Rich native
Conversation detail with pending isolated advisory metadata is retained as a negative control.
Legacy advisory reads require a server-owned response-purpose marker from the existing owner
read, so an older owner ignoring the additive flag cannot qualify a rich Label as classified
scoring metadata. Native membership remains a separate proof.

The final packet cites both host typed-result/schema reconciliation and generated Client
fingerprint/compatibility tests. The rollback run uses that real packet and actual native XML:
Supported is accepted, a regressed comparison is refused without changing an existing
selection, explicit LegacyRollback executes seven retained reads, and incomplete legacy
security proof selects Unavailable. Prior blocked packets and XML remain unchanged.

DW-63 remains open under Projects/EventStore and gates Story 7.1 writer integration.
The source generator additions require the corresponding EventStore package
release before package-mode adoption; this local source implementation does not
publish packages, change version pins, accept production authority, or retire
runtime projects.

## Historical proposals superseded by the approved amendments

The following historical proposals led to the approved 2026-11-16 amendment record.
Current rules and implementation are described above; this retained discussion is not a passing receipt:

| Current observation | Required comparison | Smallest proposed amendment |
| --- | --- | --- |
| Legacy journal Sequence=101; source event SequenceNumber=2 and GlobalPosition=101; supported ProjectVersion=2 and GlobalPosition=101 | Compare exact global positions and original aggregate sequences independently | Map legacy Sequence only to the same source GlobalPosition; assert supported ProjectVersion against independently captured original SequenceNumber. Never alias 101 to 2. |
| File-only legacy read returns existing 503; supported read returns 200 with result=null, incomplete=true and unsupportedInputKinds=[file] | Preserve honest unavailable/partial semantics without claiming exhaustive NoMatch | Explicitly approve this pair of outcomes as compatible for unsupported coverage; retain/assert every supported coverage field and record both transport statuses. |
| Stale Tenant legacy read returns 503; supported context returns 200 Unavailable with null Setup, or advisory returns empty incomplete 200 | Neither response grants context or suggestions | Explicitly approve the status/envelope difference only when both independent authority and nondisclosure checks pass; preserve exact AD-32 fields. |
| Context/setup/explain accept only ProjectId; native Conversation membership exists but no session/opened-Project proof was found | Bind to the named Conversation or an authoritative opened-session Project | Owner identifies the existing proof and transport, or approves a narrower member-only scope. Advisory metadata cannot substitute. |
| Retained list/open return required name fields without the supported descriptive-inspection audit boundary | Safe Metadata and durable audit before descriptive disclosure | Keep these rollback slices unavailable until the owner authorizes a compatible inspection contract or a withdrawal of the unsafe legacy disclosure. No name difference is normalized away. |

### Native membership only option (historical proposal; implemented by approved D4)

An implementable narrower option is to require a named Conversation whose native
`ConversationDetailsV1.ProjectId` equals the requested Project. A null membership,
a mismatched Project/Tenant, a hidden Conversation, or unavailable/currentness
failure would disclose no context. Session fallback would stay Unavailable.
Neither `ProjectHydration.ProjectId` nor resolution/advisory metadata is admissible
for this check.

The existing transport candidate is the owner-registered `conversation-detail`
Gateway query in
`references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Routing/ConversationsCatalogDeclarations.cs`.
It returns `ConversationDetailResult` and delegates to GetConversationQuery through
`GetConversationDomainQueryHandler` and ConversationQueryHandler.GetAsync. The
public owner DTO is GetConversationQuery(SchemaVersion, TenantId,
CallerPrincipalId, CorrelationId, ConversationId); only the native Details.ProjectId
would be consumed. The SDK wire request is the existing internal
`GetConversationPayload(ConversationId)` (or the envelope EntityId/AggregateId),
not the public GetConversationQuery record itself. The adapter currently forwards
QueryEnvelope.UserId to CallerPrincipalId, and its catalog declaration does not
require delegated context. Thus the route cannot be presumed to carry the needed
original actor safely merely because it exists. This route is currently classified
internal/UI-only, while the other owner metadata routes explicitly permit scoring
only. Owner approval must
enroll the detail route for this original-actor-bound Projects use and verify the
existing authenticated transport; a Projects service identity must never replace
the original actor. No permission or first-response grant is inferred from the
membership check.

If approved, append an optional ConversationId to the three supported Contracts
queries while explicitly preserving their existing one-string constructors and
Deconstruct signatures. Use the existing delegated owner-query platform seam,
recheck the same Tenant/Conversation/native Project membership before disclosure,
and retain the independent Project, Folder, Setup and Tenant admission checks.
Existing unbound callers, including the current Web inspector, would receive
Unavailable until they provide a named Conversation. V1 REST/client methods would
retain their signatures; they cannot acquire a required new Conversation argument
silently. An additive REST query parameter and generated overloads preserving the
old signatures, or continued unavailability of unbound legacy context reads, is a
separate explicit compatibility choice. This option narrows the approved session
fallback behavior and therefore requires the user's approval before code changes.

For list/open, the exact constraint is OpenAPI `Project` and `ProjectListItem`:
`name` is required with minLength 1 and is classified tenant_sensitive. The v1
client's corresponding Name properties use JsonProperty Required.DisallowNull.
Omitting/nulling the field conflicts with that contract; empty or fabricated names
would violate or falsify it. The existing supported InspectDescriptiveMetadata
and ProjectReadAudit policy can be reused after an owner-approved additive legacy
inspection contract, with preserved old overloads, or the unsafe legacy slices can
remain unavailable for rollback. This proposal does not normalize that disclosure.

Authoritative investigation source files (read only):

- `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Queries/{GetConversationQuery,GetConversationPayload,ConversationDetailsV1,ConversationResolutionMetadataV1}.cs`
- `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Routing/ConversationsCatalogDeclarations.cs`
- `references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/{ConversationDomainQueryHandler,ConversationQueryHandler}.cs`

The smallest binding decision is whether the owner approves this native-membership-only
use, including original-actor enforcement on its existing transport, with unbound
callers unavailable; otherwise the owner must name an existing session/opened-Project
proof contract and transport. No upstream source is changed by this proposal.

## Independent review verification

The reviewed source passes 2909 full-suite tests, all 77 authenticated comparison cells and seven configured rollback reads. Current receipts are under `evidence/epic6/6.7/review-pass2-fixes`; prior packets remain historical. Valid-empty collection results are separate from genuine missing resources, unavailable evidence and denials. Storage evidence distinguishes the physical legacy journal container from logical aggregate keys, and each transport retains its own cursor proof.

Legacy list refuses retryable or stale row evidence and rechecks final authority. Legacy quarantine matches supported inspection, and diagnostics reload safely when the named Conversation changes. Pre-registering a configured generated Folders client retains the v1 read transport; explicit custom directory overrides remain effective. Successful stale v1 lifecycle/permission responses cannot advertise Included Folder evidence or its label.

The startup revision and contract values are deployment declarations, verified against source qualification receipts here. Intrinsic binding to running application/dependency fingerprints remains a production release obligation, alongside the deferred owner/RTI admission and existing retained attachment-link race. This source acceptance does not qualify a deployed topology or advance readiness/AD-25 gates.

The final review also rechecks fresh, unchanged Project/Tenant/EventStore evidence after inspection audit, Folder reads and native membership. Retained delegated ChatBot list/open share the supported current Folder boundary. Folder v1 metadata responses are capped at 16 KiB. The cursor proof enumerates three pages, advances the clock, retains the original expiry and rejects replay under four independently authorized changed identities. Additional ChatBot stale/unavailable transport outcomes are retained as safety controls and explicitly do not add an approved D2 normalization pair. Retained context, Conversation-start, explain and refresh reads likewise re-read Project/Tenant/EventStore authority after their last owner await and before disclosure (Story 6.7 review pass 3).
