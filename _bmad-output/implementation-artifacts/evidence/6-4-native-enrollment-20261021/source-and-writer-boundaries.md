# Conversation native proof enrollment and writer boundaries

This source slice makes explicit local Conversation proof enrollment compatible with the existing pure legacy handler. It does not perform native read enrollment, source-guard maintenance activation, installation acceptance, deployment, qualification or Story 6.4 promotion.

## Composition

An authorized host can explicitly call the existing domain registration:

```csharp
using Hexalith.Conversations.Server.Projections;

services.AddConversationResolutionNativeProof();
```

The production `Program.cs` is unchanged. A declaration is source configuration, never evidence that deployed writers are installed, stopped, qualified or enrolled. The supported configuration requires exactly `ConversationAsyncProjectionHandler` for `conversation/conversation-read-model` and exactly `ConversationProjectionHandler` for the response-only legacy seam. The SDK compares the actual same-domain inventory, exact implementation types, route, full-replay and reconciliation semantics. Missing, extra, duplicate, wrong-type or case-conflicting handlers refuse coexistence. Existing constructor signatures and undeclared legacy refusal remain compatible. Repeated Conversation composition is idempotent.

The host must retain its existing exact app/service identity, authenticated endpoint policies, SDK read-model/proof stores and current delegated owner authority. Guard negotiation is explicit; a historical ordinary fingerprint does not authorize guarded proof.

Native input admission additionally requires the existing authenticated `IEventStoreGatewayClient`, an available native-read lane and original producer proof. A wire hint, local cursor, synthetic receipt or current head is not admission. Protected/evolved input without supported exact native provenance remains unproven. Existing unproven generations gain no retrospective certificate. Hinted input uses a length-prefixed exact input fingerprint, separate from the first durable admission decision. The optional `ConversationProjectionNativeAdmission` follows the existing ledger and lasting generation; unhinted legacy JSON omits it. Older hinted retry formats without sufficient immutable input/admission evidence refuse repair rather than migrate, while existing proven generation reads retain their original evidence. A later supported generation can obtain its own original proof. Existing complete native read limits remain 10000 events and 16 MiB; a larger/changing latest stream can make original-cut verification unavailable. No capacity is widened.

## Trace

| Boundary | Owning source | Required behavior |
| --- | --- | --- |
| Capability negotiation | EventStore `DomainService/EventStoreDomainServiceExtensions.cs`, `AdminOperationalIndexMetadata.cs`, `DomainProjectionOwnerProof.cs` | Preserve ordinary catalog categories and legacy fingerprint when guard negotiation is absent. Advertise descriptors only for exact supported local declarations. Recompute current inventory when reading proof; a previous catalog cannot authorize a changed host. |
| Explicit local declaration | EventStore `DomainProjectionLegacyCoexistence.cs`, `DomainProjectionOwnerProofRegistration.cs`; Conversations `Projections/ConversationResolutionNativeProofRegistration.cs` | Bind the sole full-replay named owner and sole response-only legacy implementation. Do not infer installation coverage or activate source guards. |
| Native source admission | EventStore `DomainProjectionNativeSourceAdmission.cs`, Client `Streams/AuthoritativeEventStreamReader.cs`, Contracts `Projections/ProjectionRequest.cs` | Verify the exact original native incarnation/prefix/head and raw handler-visible input before evolution. Copy/cancel safely; expected unsupported transport leaves ordinary input unproven. |
| Ordinary producer | EventStore Server `NamedProjectionDispatchCoordinator.NativeSource.cs`, `NamedProjectionDispatchCoordinator.cs` | Compare raw persisted envelope prefix against native inspections; use a bounded admitted worker. Freeze original proof or absence in existing durable work. |
| Compatible production producer | EventStore Server `ProjectionUpdateOrchestrator.LegacyReplay.cs`, `INamedProjectionDispatchCoordinator.cs` | Carry only an already validated exact raw-prefix anchor through named compatibility delivery, including the independent delivery under an active legacy lease. Preserve original absence. |
| Durable retry | EventStore Server `ProjectionDeliveryRetryWorkItem.cs`, `DaprProjectionDeliveryRetryScheduler.cs`, named coordinator compatibility/recovery paths | Keep the original supplied proof/absence and scope immutable through ordinary, compatible, reconciliation and recovery requests. |
| Legacy response | Conversations `ConversationProjectionHandler.Project`; SDK `LegacyDomainProjectionHandlerAdapter` | Return the established gateway-owned opaque projection response. Neither seam persists isolated metadata, generation or selection. Legacy route behavior remains eligible. |
| Public direct rich writers | Conversations `ConversationProjectionReadModelWriter.PersistAsync` / `MarkPendingAsync` | Persist an outer content-free invalidation barrier before rich components can advance. The strict metadata reader refuses while a direct/resuming writer could compromise generation completion. A historical selected-owner receipt alone does not bypass that barrier. |
| Named normal/reconciliation | Conversations `ConversationAsyncProjectionHandler.ProjectAsync` / `ReconcileAsync`, `ConversationResolutionMetadataPersistence` | Use original durable admission decision, original clock/fingerprint, SDK recovery, rich completion checks, isolated metadata/fence/selection batch and lasting generation. Never promote an old unproven generation when verification later becomes available. |
| Rebuild preparation | Conversations `ConversationAsyncProjectionHandler.PrepareRebuildAsync`; SDK `DomainProjectionDispatcher`, `DomainProjectionOwnerProof.BindRebuildBatch` | Retain source proof in original immutable plans and exact coordinated owner selections. Legacy default output remains part of its established wider manifest. |
| Transported/mixed rebuild | SDK `DomainProjectionRebuildTransport`, `DomainProjectionGuardedReplay` | Freeze original native proof or absence in transport headers and original prepared manifests; re-admit reconstructed full-replay input before its consumer. Recover original conditional operations and owners rather than preparing against a new head. |
| Guarded canonical replay | EventStore Server `ProjectionSourceGuardHistoryReader.Replay.cs`, `ProjectionSourceGuardTailDelivery.Replay.cs` | Reuse the admitted raw-prefix binding and transport that same original cut. This does not qualify a legacy/unproven operator rebuild. |
| Persisted owner proof | SDK `DomainProjectionOwnerProof.ReadAsync`, Client `DaprReadModelStore.OwnerProof.cs` | Read the exact isolated selected row and its compact terminal SDK receipt, bound to scope/catalog. No domain handler, event/content read or repair occurs. |
| Resolution metadata read | Conversations `ConversationProjectionReadStore`, `ConversationResolutionNativeEvidence`, `GetConversationResolutionMetadataDomainQueryHandler` | Require local committed generation/barrier coherence, exact native selected-owner proof, exact resource and before/after current Tenant/actor/workload/delegation authority. No events, detail or index hydration; no query repair/certification. |
| Installation/source guard | EventStore Server `ProjectionSourceGuardMaintenance`, `ProjectionSourceGuardQualification`, `QueriesController.ProjectionCurrent` | Existing maintenance inventory, canonical deployed revisions/packages, quiescence, native lifecycle/prefix and current authority remain independently required. Local handler declarations do not replace them. |

Source paths above are relative to the named owner's `src/Hexalith.EventStore.*` or `src/Hexalith.Conversations.Server` directories. The final source manifest binds complete bytes and revisions, and the incremental patch distinguishes this run from existing uncommitted provenance work.

## Deliberately unavailable coverage

Unenrolled hosts, unsupported/protected/evolved or retained-away native input, old records without sufficient original evidence, unproven operator/mapped rebuilds and unresolved recovery remain unavailable. Coordinated rich rebuild selections still fail the strict Conversation current-proof adapter before receipt/manifest hydration. Content-free post-rebuild promotion remains a separate prerequisite even when the underlying SDK can prove the selected terminal rebuild receipt.

The generic selected-owner endpoint authenticates its unchanged isolated row/receipt. Direct rich-writer invalidation is enforced by the additional Conversation metadata gate; consumers cannot use the historical receipt alone as resolution authority. No generic proof response establishes installation completeness.

Cold activation may still recover/write. No installation-wide all-writer or zero-write result, deployed owner grant, native Folder/Organization generation binding, committed File/Projects mapping, production metadata mutation adoption, exact-current qualification, accepted Projects G-4 or parent acceptance is supplied here. RTI and resolution handlers remain gated. D1–D6, frozen intent, parent story and sprint stay unchanged.
