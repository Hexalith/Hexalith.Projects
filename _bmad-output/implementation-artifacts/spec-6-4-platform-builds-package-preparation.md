---
title: 'Story 6.4: Platform/Builds package preparation'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
baseline_commit: '0e45105476902fcfce1f8dbd8416d7234bb16b41'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="User explicitly authorized upstream source/configuration preparation and testing on 2026-10-28">

## Intent

Investigate HXW011 and HXW009 against current remote release metadata and committed owner pins. Distinguish unavailable local tags from an unpublished exact Platform identity. Prepare and test the smallest supported Platform/Builds correction for Story 6.4, rerun package preparation and record exact evidence and the next remaining blocker. Preserve existing user changes, the approved advisory specification and every qualification gate. Do not commit, stage, push, publish, deploy or select unqualified dependencies. A prepared candidate does not establish accepted Platform integration or Story completion.

</frozen-after-approval>

## Implementation Notes

Investigation: Builds `PlatformIdentityResolver` correctly uses Projects HEAD's Platform gitlink, exact points-at release tags, the Platform-owned Builds pin, matching umbrella index and clean Builds checkout. Current root records Platform `a5e1389fe2c97b14e8c697a2e54f45631d07ef95` and Builds `1745ea0f09df9e518aeea3eeb1a7df836f67e09d`. Platform now checks out `8de90958bc41b7fd01b7ed22b8ab364447a800e4`; Builds checks out `abc0e1be8069d1c7a567515a13bb015adc407536` with pre-existing catalog edits. Both remote Platform tags already exist locally and neither identifies the committed root selection. No resolver patch is warranted.

The bounded candidate changes Platform `.config/dotnet-tools.json`, `hexalith-platform.json` and the module-tool README to the already released and Conversations-accepted CLI 4.38.1, exact source `16ff70a2321ad576faaac3fe53088c2f1bdacae3`. Bind its NuGet archive/source, release qualification inventory and current remote CI before editing. Prepare an unapplied owner Builds gitlink patch to that same source. Preserve real owner tests that require committed Git identity. They cannot establish acceptance before owner recording and qualification; no synthetic tag or HEAD/index substitution is permitted. Do not edit Builds source/catalog, umbrella index, release package selection or Story status.

Evidence lives in [the preparation receipt](evidence/6-4-advisory-resolution-20261021/platform-builds-preparation-20261028/README.md): initial and refreshed remote metadata, exact argument arrays and environments, tag checks, official feed package identity, focused existing Builds guard tests, available owner checks, final public package preparation, source hashes and user-change preservation. Publication and integration remain separate work.

Preparation completed within the authorized local scope. Platform's two candidate configuration pins and README are prepared; the exact owner gitlink patch remains unapplied. Official release source, signed NuGet bytes/catalog/signature, clean release eligibility and official inventory digest are verified. Existing Builds catalog changes and all four inspected repositories' HEAD/index identities remain unchanged. No commit, staging, push, publication, deployment or acceptance promotion occurred.

Verification: 88 existing Builds guard tests, one existing Platform inventory test and 37 planning-guard tests passed (126 total). The broad Platform test project remains blocked by three existing analyzer errors; the narrow unchanged owner-pin test fails on missing nested release-tag context and the actual committed pin mismatch remains independently proven. Final package preparation exits 2 with HXW008, HXW009 and two distinct HXW011 findings. Source preparation exits 2 with HXW004 for Commons checkout/index drift. The direct AppHost's Aspire 13.6.4 versus tool 13.6.3 prerequisite mismatch remains an integration blocker. Conversations 1.3.1 publication is now externally observed; consumer package verification and pins remain unfinished. Story 6.4 retains its original in-progress status and frozen scope.

The configured Blind Hunter review reported eight findings, all verified and patched. The reviewer confirmed closure of every finding and independently reran the read-only audit with the fresh official signed ZIP successfully. Parent negative controls reject changed source-baseline hashes, changed release inventory bytes and an unrelated owner-test failure without mutating their inputs. No new review item was deferred.

## Review Triage Log

1. medium, patched: reconcile candidate Aspire prerequisites and expose the unchanged AppHost mismatch.
2. medium, patched: reconcile Dapr text, official URLs, directories and environment with the actual released catalog.
3. low, patched: retain the pre-4.31 range enforcement qualification.
4. medium, patched: compare immutable source/frozen/prefix baselines; emit separate audit observations without rewriting inputs.
5. medium, patched: bind official inventory digest, clean exact source and release eligibility.
6. medium, patched: derive workspace location; accept an archive parameter; distinguish offline checks from fresh ZIP verification.
7. medium, patched: check actual recorded exits and every distinct diagnostic instance/path/identity.
8. medium, patched: check the exact failed owner test and missing-tag cause; retain separate committed-pin mismatch evidence.
