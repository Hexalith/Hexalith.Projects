# Conversations publication CI repairs — 2026-10-22

The five current portable-conformance failures from CI run `39739114750` were reproduced locally before edits (5/5 failed). All five and an added retained-member guard now pass (6/6). The exhaustive Contracts serialization guard also passes after adding the two missing advisory DTO samples, including complete and incomplete result variants; the four strict DTOs already had samples.

| Final lane | Passed | Failed / skipped | Dependency mode |
| --- | ---: | --- | --- |
| Current Portable conformance, exact CI historical exclusions | 327 | 0 / 0 | Debug packages |
| Current Internal conformance, exact CI historical exclusions | 90 | 0 / 0 | Debug sibling source |
| Full Contracts | 638 | 0 / 0 | Debug sibling source |
| Full Server | 1135 | 0 / 0 | Debug sibling source |

These final lanes contain **2,190 unique passing tests**; focused 6/6 and cross-Tenant 1/1 are subsets. All four affected test projects built individually in Debug with explicit sibling roots. Server/Portable/Internal builds had zero warnings/errors. Contracts built successfully with one pre-existing MSB9008 warning: its direct historical nested Commons.Serialization reference does not exist in the umbrella layout; the transitive root source supplies that assembly and all 638 tests pass. No nested submodule was initialized.

The source-mode Portable run is retained honestly: 326/327 passed; its live compile-surface guard correctly rejected external umbrella project references. The additional Debug package-mode build and run use the existing `UseHexalithProjectReferences=false` override, inherited by the verifier subprocess, matching CI's package dependency policy. It passes 327/327. No exclusion, build configuration, package pin, frozen assertion inventory or gate was changed. `commands.json` contains both modes and exact CI exclusions; `portable-source-mode-failed.xml` retains the environment-mode failure.

The public surface diff is exactly six added query/scoring types and optional `ResolutionScoringLabel` properties on CreateConversationCommand, UpdateConversationMetadataCommand, ConversationCreated and ConversationMetadataUpdated. All 219 prior types and all previous members, positional constructors and signatures are preserved. The additive v3 current snapshot, complete member diff and its scope review are retained. The live equality/count guards target this successor; the new allowlist guard rejects every addition or change outside that approved delta. Actual approval sources are the recorded Story 6.4 native scoring-label owner work, the approved October 21 advisory reset preserving it, and the October 22 publication CI repair scope. No new owner/Quality decision or release approval is fabricated.

The immutable v1/v2 snapshots and v2 projection proof/run bytes remain unchanged; `preserved-historical-sha256.json` binds all eight retained files. The new projection successor preserves the historical zero-write claim as historical and binds the actual current cross-Tenant test source and fresh 1/1 passing XML. That test rejects the event, permits exactly one content-free accepted-route position barrier and persists no Conversation view or tenant index. The live validator still validates historical machine outcomes and requires the explicit current successor; there is no alias, guard exclusion or claim that the former zero-write assertion passed at HEAD. The new XML has an exact-path `text eol=lf` policy, verified in `xml-git-attributes.log`.

The gateway fixture now composes the real EventStoreGatewayClient with the existing SDK Testing MockHttpMessageHandler callback seam. Only fixture-native page data and availability controls remain module-owned; there is no in-module IEventStoreGatewayClient implementation. Fixture/enrollment/recovery consumers use the real client, and the full Server suite confirms unchanged behavior. There are no production source changes.

`source-manifest.json` binds all 14 reviewed Conversations files at this uncommitted execution tree, based on `591ee3b3fa09cee761f6d56b7f82fa0d16cd04ed`. JSON/XML results, exact commands, build logs and sanitized runner summaries are retained. Private runtime/application logs and credentials are excluded. Raw whitespace checking reports the repository's existing CRLF convention; the explicit CRLF-compatible check passes without rewriting unrelated bytes.

Parent owns committing, integrating the unrelated Story 12.1 planning-doc advance, pushing and checking exact-source CI. Conversations 1.3.0 publication still needs its separate explicit release go-ahead; the consumer pin and Story completion remain pending. This evidence establishes local source/contract/conformance readiness, not production deployment or release authority.
