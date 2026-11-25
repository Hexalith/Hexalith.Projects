# Gate fixes — 2026-11-25

Spine `ARCHITECTURE-SPINE.md`: SHA-256 `865f8eed…` (95,244 bytes, 600 lines) → `024503d0…` (99,023 bytes, 601 lines). LF, UTF-8. Lint: 0 findings. Relative links: 27 (22 unique) resolve, anchors included. Mermaid blocks unchanged. Line numbers refer to the new spine.

## Applied

1. Cutover ordering (decision 22): Writers row L52; AD-17 Writer cutover L251 ("It follows G-5 … validator"); AD-20 L292 (allow-all removal no later than the real validator; real validator before the cutover, 8.8-P1 as evidence; token forwarding ends with G-5 delegated credentials, before the cutover); G-5 L553; Seed composition paragraph L482 (allowing validators leave no later than the real validator).
2. SDK aggregate isolation (decision 23): AD-17 L251 (non-shipped test-composition project until the cutover, moved into Hexalith.Projects; production-capable host refuses more than one keyed processor per domain); AD-24 L316; Seed artifact row L475 and composition paragraph L482.
3. Creation-abandoned fence event: AD-3 L136; AD-17 replay set and approved deltas L249; AD-22 fold set L304; AD-35 L426 names it. AD-8 unchanged (it does not say pre-activation progress is task-only).
4. Fenced release: AD-4 L142; AD-35 L426 (before the terminal transition, exempt from the AD-12 recheck, else NeedsAttention; never releases on an unconfirmed commit).
5. AD-12 L215: task/step key checked before the expected version; equivalent redelivery returns the original outcome, treated as success; mismatch is a rejection event that changes no state.
6. ChatBot grant: AD-3 L136; classification row L503; AD-33 matrix row L404 (Tenant owner only on Hexalith.Tenants evidence; all four role columns Denied).
7. AD-31 L366: name-only legacy create without metadataClass accepted throughout v1; supplied projectId only until the cutover (then the AD-18 400); metadataClass required only on canonical requests.
8. AD-24 L316: explicit 409 route list restored; 202 carries the Gateway task location (AD-19); 409s and the projectId 400 named as explicitly approved NFR-10 breaking changes; frozen v1 YAML gains the 409 RequestPreview response as a compatibility fix. AD-16 unchanged (not needed).
9. Regressions: AD-25 startup, teardown and identity proof L322 and G-4 L552 (from the previous revision); full retained-fold scope AD-17 L251; Deferred archive row "remain binding" L595; asOf "server time" AD-14 L229; backup-restore rule AD-28 L342; "writer selection replaces processor and replay together" AD-17 L251; lost gate wording: single-Tenant binding and DW-63 behavior-preserving in the Writers row L52, 7.1-P1 acceptance gate on the AD-31 exception L366.
10. AD-33 quarantine exits L408: FR-8 Administrator binding (Story 7.6) or Story 7.15 repair.
11. AD-27 L336: measurements on the production state-store/pub-sub provider class selected under G-5 (Deferred).
12. Claim identity: AD-18 L278; AD-35 L424 (Tenant/project-folder-claims/FolderId; generation at acquisition; holder; claim domain declared in route catalog, module manifest and idempotency declarations).
13. Inspection-audit carve-out AD-14 L229: covers supported query handlers writing the same keys (AD-24 L316 and AD-26 L330 reference it by name, unchanged).
14. AD-14 L229: supported Get, Explain and Conversation-start make no Folders call and still check Conversations membership.
15. Stack Aspire row L460 (AddDotnetProject(name, path)); runner contract L507 (Module.Cli pin moves to the version Builds selects).
16. "First writer": Writers row L52 (Story 7.1 qualifies creation on the SDK writer in test composition; deployed selection is the cutover); G-1 L549; G-2 L550. No "first writer" text remains.

## Not applied

- T-M2 (no lane runs Dapr 1.19.2): owner action, not a spine change.
- T-L1 (FrontComposer 6.1.0 G-6 wording): outside the brief.
- T-L3 (Redis Cluster topology caveat): outside the brief.
- T-L4 (SDK pin servicing owner): outside the brief.
- ADV-12 (per-slice and Projects-issued wording in epics.md): upstream epics edit.
- R-H2 AD-8 Prevents rewording: AD-8 does not state task-only progress.
- R-M2 extra NFR-10 exception text and AD-16 amendment: memlog names the changes approved and the inventory as evidence; nothing more.
- R-M3 `unclassified-v1` class value: not in the memlog.
- R-M9 Deferred revisit before Story 7.1 and every-candidate-class evidence: not in the memlog (AD-27 part applied as fix 11).
- R-M10 split of fencing as a cutover-only gate and the epics correct-course: not in the memlog (the "first writer" wording was clarified as fix 16).
- R-M11 PRD amendments Deferred row: not in the memlog.
- R-M12 RPO 0 for task, idempotency, confirmation and audit stores: not in the memlog.
- R-L1 claim aggregate-ID encoding helper: not in the memlog (identity fixed as fix 12).
- R-L2 RequestPreview status code on every surface: not in the memlog.
- R-L3 Story 7.15 decision limits: not in the memlog.
- R-L4 moving the allowing validator types out of the packable package: not in the memlog.
- R-L5 Rule/Transitional split: deferred as M17.
- R-L6 adapter-only namespace marker: not in the memlog.
- Other reviewer proposals that go past the memlog were not applied either: F-12 (AD-14 Binds FR-21, NFR-9 map row), the AD-19 and Conventions wording in F-14, the problem-body vocabulary in ADV-7, the shared audit writer in ADV-8, the cutover receipt for a reconstructed incumbent in ADV-9, the byte-for-byte FolderId rule in ADV-10, the AD-15 sentence in ADV-4, retry ordering for imported v1 keys in ADV-5, the Setup expected-version rule in ADV-6, the Deferred item in T-H1 (superseded by decision 23) and the full AppHost text in T-L2.

## Open

- Size is 99,023 bytes, over the 97 KB aim by about 2 KB. The fixes account for all of the growth, and the excess was not cut from unrelated text.
