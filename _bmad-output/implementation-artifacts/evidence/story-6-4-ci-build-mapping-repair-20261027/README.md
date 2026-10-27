# Story 6.4 CI build mapping candidate — 2026-10-27

This is preparation and focused test evidence for the approved repair spec. Builds
HEAD is `cad925260713718895282c69bcbdb8bc72e7b6a2`; Conversations HEAD is
`536bf80d5fd5c2a64c2f64815edc67074fd5a6ea`. Candidate working-file identities are in
`candidate-source-hashes.json`; their exact source copies are under `candidate-sources`.
No user repository was staged, committed or pushed. No release, workflow, deployment,
held runtime or host retirement was performed.

The new public `prepare-build` validates the declaration and reuses the existing
workspace resolver/materializer. Its fresh private output carries the immutable mapping
and generated imports, with no run identity or runtime lifecycle. The opt-in action
restores the exact local tool pin, checks the observed tool version, structurally parses
JSON and exports only five verified import paths, the hash and Release configuration.
Reusable CI prepares every job from its actual executing Builds revision; direct
Conversations builds prepare from their qualified root Builds checkout. Pack inherits
producer selection; isolated package consumers remove the parent import environment.
Windows longpaths setup precedes checkout.

| Check | Observation |
| --- | --- |
| Builds affected Debug project/test build | PASS, 0 warnings/errors |
| Public prepare command, source and qualified synthetic package, actual MSBuild evaluation, failures/cancellation | 6 passed |
| Preparation helper and workflow opt-in checks | 5 passed |
| Governed action/provenance closure | 38 passed |
| Conversations release tooling | 27 passed |
| Established npm tooling gate, including the new overlay regression | 95 passed |
| Existing Windows workflow-contract test extended after review: longpaths before source checkout; full checkout retained | 1 passed (only this test rerun) |
| Compatibility overlay metadata/locks | 1 passed |
| Compatibility result fault controls | 8 passed |
| Original frozen source fixture locked restore | Expected NU1004, retained |
| External package successor graph, locked restore and Release build in standalone CI layout | PASS |
| Exact compatibility assertions | 7 passed, 0 failed/skipped/errors |
| Current source Contracts.Tests and Server.Tests Debug builds | PASS, 0 warnings/errors |
| Current source mapping/source-package boundary tests | 10 passed |
| Current source advisory metadata query tests | 48 passed |

`observations/verification-commands.json` records exact commands, working directories,
exit codes and result locations. Logs are retained as deterministic gzip files; XML
and JSON receipts retain their exact bytes. The generated source mapping/import XML is
under `generated-source-mapping`. Source creation scripts and exact temporary Git
commands are retained under `observations`.

All 17 original MinimalModule files, including four original locks, match the pre-edit
SHA-256 values in `frozen-fixture-hashes.json`. The external compatibility overlay
selects EventStore 3.128.0 packages (the selected Builds catalog version) and its own
four successor locks. They were deliberately generated once, then restored in locked
mode. CI never regenerates them. The umbrella checkout adds an ancestor EditorConfig
CA1062 diagnostic to the frozen source; that in-place build refusal is retained. The
standalone verification uses identical frozen source bytes and matches the actual
Conversations CI checkout, without analyzer suppressions or fixture edits.

Ordinary source preparation in the real umbrella refuses HXW004 because the selected
Conversations HEAD differs from the umbrella's staged gitlink. That refusal is retained
unchanged. Current source builds instead use an explicitly authorized disposable test
Git workspace with direct references only and actual owner HEADs plus copied current
source bytes; the real public resolver generates its mapping. This changes no user
indices or pins and does not inject a consumer-authored mapping. Source preparation
retains advisory HXW011 (untagged Platform), HXW009 (unqualified/dirty Builds catalog)
and HXW010 (candidate tool outside accepted range). Its successful Debug evidence is
separate from package or runtime acceptance.

Delivery remains pending. Accepted CLI 4.35.2 does not contain `prepare-build`, and the
accepted tool also refuses the legitimate APP_API_TOKEN declaration shape. The exact
candidate package-mode command refuses HXW011/HXW009/HXW010 on the current tuple.
A producer successor needs separate qualification/publication and exact acceptance;
consumer tool/gitlink selection needs a compatible tagged Platform/catalog/package
tuple and explicit subsequent authorization. No accepted pin was chosen or bumped.
The current actual tuple refusal is not a green upstream CI claim. Windows execution,
upstream CI, publication, runtime/live acceptance and Story 6.4 delivery remain pending.
Parent diff audit and independent workflow review are recorded separately.

## Review follow-up: targeted verification only

Normal Builds CI and release validation now execute the preparation helper's behavioral
suite. Its metadata path receives the same real-path/containment checks as imports,
and a boolean mapping mode refuses before environment export. Blank service arguments
use the existing HXC001 Usage phase/category. A portable catalog process double blocks
on a synchronization primitive and signals a file after mapping output is owned;
the test cancels the real preparation and requires that output to be removed. It runs
in an isolated native xUnit apphost child, without sleeps, new skips or production API
changes.

The compatibility overlay now evaluates the direct pinned Builds catalog independently
and refuses any EventStore version drift from its deliberately qualified selection.
The test derives expected versions from that catalog and verifies a mutated temporary
catalog fails. The isolated compatibility CI job initializes only its direct Builds
checkout. The exact db897eac9f2819220b32d237109d7255218b46a9 catalog bytes were read with
`git show` for the disposable standalone test layout; no user nested submodule was
initialized. All four successor locks and all 17 original frozen files remain unchanged.
No dependencies or locks were upgraded or regenerated.

After these review edits, only the affected producer build, preparation file's 11
cases (including the post-ownership cancellation and four usage cases), helper's eight
cases, and compatibility overlay's three cases ran; all passed. Exact commands and
fresh outputs are in `observations/review2-verification-commands.json` and the matching
review2 logs/XML. Earlier npm, package-build, seven-assertion and source-build evidence
above is retained as prior checkpoint evidence; none of those wider lanes was rerun
for this follow-up.

The parent independently recorded 2,174 full producer cases: 2,172 passed and two
existing layout/OS-dependent FAIL_SKIP failures. Its clean affected lane passed all
425 cases. Their original summaries, logs, XML and pre-review source hashes are
retained separately as `observations/parent-pre-review-*`; they describe the checkpoint
before this follow-up and are not presented as post-fix full verification.


Final parent audit: [resolved review and final checks](parent-final/README.md). The current source-preparation scope is complete; Story 6.4 delivery remains pending.
