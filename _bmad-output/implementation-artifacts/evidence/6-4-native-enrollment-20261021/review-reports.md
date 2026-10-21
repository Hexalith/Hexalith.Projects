# Independent enrollment review reports

The reviewers were context-free at dispatch and read the cumulative 201066-byte owner patch. The verification and edge reviewers read their rendered review instruction snapshots. The blind reviewer calculated N=min(floor(sqrt(201.066)+1),10)=10. Each claim is preserved separately in review-results.json with parent evidence and disposition; grouping does not remove claims.

## Blind review

1. Production gateway errors are EventStoreGatewayException, whereas expected-failure fallback catches other exception types. Ordinary dispatch can abort instead of remaining unproven.
2. VerifiedNativeSourceProof changes when a verifier outage recovers; a stable Conversation fingerprint can then change, stranding a partial generation.
3. Canonical compatible replay validates an anchored native prefix but TryDispatchHeadAsync drops available proof; envelope-based coordinator tests do not cover that production orchestrator.
4. Production operator rebuilds construct requests without NativeSourceProof; the new positive fixture supplies it manually. Clarify whether this path is qualified or remains unavailable.
5. Admission reads the complete latest stream before reconstructing the original cut. Existing 10000-event/16 MiB/stable-head bounds can make older work unverifiable after unrelated appends.
6. Null event Payload is cloned before typed native validation; malformed input can escape as a server error.
7. Admission copies payloads before cheap proof validation/cancellation and does not observe cancellation through that copying.
8. Direct public rich writers invalidate local metadata but leave the generic selected row/receipt proof intact. Clarify which gate supplies authority and test that distinction.
9. The new shared Dapr fake checks/replaces ETags without synchronization, permitting concurrent same-ETag winners.
10. Explicit enrollment, authenticated gateway/native-read requirements, existing unproven generations and protected/evolved input restrictions need integration documentation.

## Edge review

1. NamedProjectionDispatchCoordinator.NativeSource.cs uses deadline.ReadAsync for optional capture. Repeated noncooperative actor inspection tasks can outlive two-second deadlines and retain whole histories without a concurrency bound. Use existing ReadWithAdmissionAsync.
2. DomainProjectionOwnerProof.SupportsEnrollment checks rebuild interfaces but not RebuildSemantics.FullReplay. An exactly declared Incremental owner can advertise full replay and later reject it.

## Verification gap review

DomainProjectionDispatcher.PrepareStreamedRebuildBatchAsync newly invokes native admission for the reconstructed full-replay sibling. Existing GuardedCanonicalFallback_MixedStreamedAndFullRoutesResumeOneOriginalCoordinatedManifest verifies counts/owners/receipts/recovery, but GuardedFullReplayProjectionHandler does not inspect VerifiedNativeSourceProof. Ordinary native proof tests and the new Conversation fixture use other rebuild paths. Reverting only that admission call would not fail those tests. Extend the mixed production harness with supported native reads and persist/assert the exact private admitted proof before/after interruption and retry.

## Parent follow-up

DomainProjectionRebuildTransport.HeaderMatches omits newly transported NativeSourceProof. The immutable uploaded header must retain the exact original hint, including absence, across subsequent parts and first preparation. Add unchanged/gain/loss/replacement assertions.
