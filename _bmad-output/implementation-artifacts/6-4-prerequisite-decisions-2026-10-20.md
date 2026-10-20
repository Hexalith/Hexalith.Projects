---
story_id: '6.4'
artifact_kind: 'prerequisite-design-decision-record'
decision_date: '2026-10-20'
approval_source: 'User instruction: apply recommended'
design_status: 'selected'
implementation_authorized: false
baseline_commit: '16886b7d9c15b5356fad860b0d7b0f97eb9f32b3'
---

# Story 6.4: selected prerequisite decisions

The user selected the pragmatic recommendations for D1–D6 after their mechanisms, tradeoffs and alternatives were explained. This records those selections in the [prerequisite specification](spec-6-4-prerequisite-current-authorized-resolution-evidence.md) and [contract annex](6-4-resolution-evidence-contracts.md). It does not record implementation, owner qualification or production acceptance. Missing source contracts and exact-current gates remain dependencies rather than unanswered design choices.

The [earlier source inspection](evidence/6-4-prerequisite-preflight-20261020/owner-readiness.md) and its JSON receipt are historical evidence captured before this selection. Their recorded open decisions and document hashes describe that earlier state; neither file is rewritten as an approval receipt.

## D1 — Repair EventStore's existing production query transport

**Selected:** Reuse the current gateway, delegated-owner route, issuer and verification mechanisms. Add the missing Tenant/domain query bindings and a narrowly defined query-digest issuance capability for the enrolled gateway. Keep exact workload, Tenant/domain, audience and operation restrictions; the owner independently reauthorizes its resource. A digest attests the exact query and does not itself grant actor permission. Arbitrary binding keys or wildcard values are not authorized by this selection.

Expose the required originating actor, workload, delegation identifier, scopes and audience through an additive SDK-verified context. Preserve the owner hop's own transport audience/workload separately from the original context. Payloads and caller-controlled headers cannot populate either authority. Use existing assertion lifetimes and cryptographic mechanisms.

**Required evidence:** Platform-owned binding/profile signatures and issuance policy, exact origin-field mapping, and production issuer tests for valid requests, tampering, wrong scopes/audiences/resources, expiry and revocation. The current digest-only decoder incompatibility and exact-value grant restriction remain source gaps until repaired. Development symmetric signing is insufficient evidence.

## D2 — Reuse real File identity; add only the missing owner mapping

**Selected:** Identify the actual file provider and reuse its stable identity where its lifecycle guarantees suffice. Explicitly map Projects' `FileReferenceId` to that owner identity. If no suitable identity exists, scope only the necessary Folders-owned mapping using supported owner persistence. Projects continues storing metadata references, while the owner retains internal addressing and verifies Tenant, parent Folder, existence, lifecycle and permission.

Do not assume that the two ID namespaces are equivalent. Existing references without a recoverable authoritative mapping require owner-backed re-verification; they cannot be silently validated, rewritten or converted to NoMatch. Any backfill or relink is separate approved work.

**Required evidence:** Named source provider/API, implementation files, identity namespace, parent binding and rename/move/delete/replacement behavior. No source API is invented in this record.

## D3 — One minimal RTI with complete candidate lookup

**Selected:** Reuse supported atomic projections/recovery for one rebuildable Tenant-scoped RTI. Project events maintain relation lookup by Folder/File identity. Conversation resolution also includes authoritative membership and every Project-name equality match under the existing mapper's comparison. Keep approved name lookup with Project-owned data; do not add labels or actor grants to the minimal RTI rows.

Generation/checkpoint evidence must distinguish current complete empty from absent, incomplete or rebuilding data. Use native owner revisions/checkpoints within their own scopes. Read-time actor authorization remains request-scoped; RTI rows never grant permission. Query execution performs no ingestion or repair.

Reuse real owner projection/checkpoint consumers where sufficient. Add a narrow adapter only after identifying a missing owner metadata/change/head contract. A general ingestion framework or a placeholder producer is outside the selected scope.

**Required evidence:** Callable owner source contract, Project/head and initialized-empty proof, collision-safe committed-generation lookup schema, invalidation/rebuild behavior and equivalence tests proving candidate discovery preserves every qualifying engine input.

## D4 — Bound raw work and measure the complete result

**Selected:** Accept 1–32 raw Folder/File inputs combined before deduplication on the additive supported path. Reject malformed/empty/overflow input; keep legacy parsing unchanged. Align the general aggregate proof boundary with the existing 256-character envelope contract through a tested platform amendment. Preserve domain-specific bounds such as the 128-character persisted reference limit. The separate current principal limit is not implicitly widened.

Start with four concurrent owner calls, the existing two-second interactive dependency deadline, cancellation and no query-level retries. Use indexed candidate discovery first. Add scoped owner batching only when measurements require it, with independent resource authorization. Every qualifying candidate remains represented; no arbitrary top-N cutoff is allowed.

The proposed 1 MiB response ceiling remains provisional until full-result serialization at supported shapes proves it suitable. Freeze a justified ceiling before readiness; overflow is honest Unavailable, never truncation or a passing performance result. Existing metadata latency requirements remain unchanged.

**Required evidence:** Raw-versus-unique regression tests, 200/201/256/257 aggregate-boundary tests, domain/principal isolation, bounded calls/cancellation, complete-result sizing and authenticated performance at approved data shapes.

## D5 — Explicit scoring-label purpose and closed failure meanings

**Selected:** Preserve the existing scoring rules. Authorize bounded owner-classified labels solely for internal equality scoring, omit names from candidates/exclusions and write no inspection audit. The proposed label bound is 160 characters; owner classification and purpose enforcement must substantiate it. Reuse the existing mapper comparison; do not truncate a label to create a match or silently drop a scoring reason.

Protected explicit Conversation/attachment targets use indistinguishable safe 404 for absence, denial or cross-Tenant access. Non-current required evidence produces Unavailable. Optional omissions produce Partial only when disclosure is authorized. Candidate-local failures expose no protected identity or existence detail and cannot manufacture a complete search or NoMatch.

**Required evidence:** Owner metadata-purpose/action implementation, label classification, native freshness and leakage/denial tests. The selected product policy does not claim an unimplemented owner capability exists.

## D6 — Additive native evidence and controlled enrollment

**Selected:** Prefer an optional evidence property on the existing v1 Folder result when compatibility tests pass and existing semantics remain intact. Use an additive version if those conditions fail. Old outcome-only results remain valid for existing consumers but cannot establish the new resolution evidence boundary.

Use owner-native resource/permission revision evidence, freshness and verification time. An expiry field is appropriate only if the owner actually guarantees a validity window. No new revision allocator, authorization lease or separate cryptographic receipt service is required.

Name the actual Projects deployment configuration, enroll exact approved query types and pin the tested immutable consumer tuple. Keep new operations disabled until the applicable evidence is accepted. G-4 remains existing Builds `6.1-P0` work; adopt its supported profile/manifest command rather than creating another runner or relying on the rejected platform-v2 `--filter` example.

**Required evidence:** Old/new consumer compatibility, native field mappings, real environment/issuer/allowlist configuration, exact packages/revisions, applicable qualification and rollback/disable procedure. This record selects no package release or deployment environment.

## Execution order and retained gates

1. Establish D1's platform contract and D2's actual File source, alongside existing G-4 adoption work.
2. Deliver narrow owner metadata reads and native evidence with owner-specific authorization tests.
3. Deliver and measure the minimal RTI/candidate lookup against complete source coverage.
4. Validate exact package/enrollment compatibility and prerequisite acceptance before implementing parent Story 6.4 with the existing engine.

The parent specification, implementation story, sprint tracking, scoring and legacy routes remain unchanged. The prerequisite stays draft until remaining API, coverage, compatibility, performance and qualification evidence makes its tasks executable. User selection is retained independently from those delivery conditions; the same design choices need not be asked again.
