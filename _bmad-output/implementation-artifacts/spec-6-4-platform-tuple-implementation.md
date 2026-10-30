---
title: 'Story 6.4 approved Platform tuple correction'
type: 'bugfix'
created: '2026-10-29'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="User approved the concrete tuple correction, CI artifact bindings and candidate recording with 'I authorize'">

## Intent

Apply the reviewed Platform tuple and direct AppHost patches for Builds/tool 4.38.3 at d1931b1e803144048d927fff16cb8119afde541e, preserving the four analyzer corrections and all other work/history. Resolve owner CI's foreign Builds project references with verified released assemblies and their required dependency/content-root metadata. Provide a reproducible owner CI check using exact pinned artifacts and package-only Release inputs. Run relevant local Debug/source checks, read-only package graph checks and available owner checks. Review and record the exact Platform candidate with normal commit validation. Keep story 6.4 in progress. No push, publication, deployment, consumer adoption or initialization of this umbrella's nested submodules is authorized; unavailable remote CI source transport remains a reported execution blocker.

</frozen-after-approval>

## Implementation Notes

- The authorization supersedes the earlier no-change/no-record restriction for the approved Platform correction only. Root Projects changes, sibling Builds checkout and all historical evidence remain preserved. Work stays on owning Platform main. The new scoped spec/receipt are additive; main story/sprint bytes are retained.
- Previously reviewed patches supply tool/floor/README and three file-host dependency changes. Record Platform's real Builds gitlink directly in its final index, without initializing the nested dependency or substituting a temporary identity. Do not change the umbrella Builds pin/checkout.
- Published4.38.3 contains Tooling.dll and g4-host/bin/EventStoreHost with DLL/deps/runtimeconfig/content. Preserve local Debug source defaults; published-root binding replaces only foreign Builds references. Restore Tooling's explicit catalog-controlled JsonSchema.Net/NuGet.Versioning/StackExchange.Redis dependency closure, retain alias buildsseed, copy host deps and register its content root for existing WebApplicationFactory tests.
- CI must fail on missing bindings or any foreign ProjectReference; retain exact-source/artifact hashes, evaluated/restored graphs, per-project test counts and logs. A passing build is CI evidence only; full policy-bound runtime qualification remains a separate gate and must never be fabricated.
- Owner evidence: root evidence/6-4-advisory-resolution-20261021/platform-tuple-implementation-20261029/. Initial preservation verified114238 original files plus475 prior receipt files. Baseline Aspire13.6.4 start failed with NU1605 for the three approved direct-host version mismatches. No existing AppHost was running.
- Reuse eng/test_host_builds_resolution.py for meaningful MSBuild binding/negative controls. Run the four GatewaySeedSubscriptionTests plus76 analyzer cases in freshly built Debug output and owner pin check with legitimate standalone release context. Keep local source evidence distinct from fresh GitHub-hosted package Release candidate acceptance.
- Applied and recorded Platform candidate 9f43cefe11bd246ce0de1cd0740261b557c0c7a0, with exact Builds gitlink d1931b1e803144048d927fff16cb8119afde541e. Normal pinned commitlint and Git commit validation passed; 19 owner files recorded. No push occurred.
- Final local checks: artifact-bound Debug build zero warnings/errors; 80 class-selected cases pass plus two real standalone owner pin/inventory checks; 14 MSBuild controls pass. Four malformed/empty profile controls reject. Recorded package graph is clean, binds the real candidate and covers all 27 projects including transitive fixture.
- Runtime baseline failed on the three approved downgrades; aligned AppHost starts but EventStore health did not pass the bounded 10-second readiness check. It was stopped normally and Aspire ps returned empty. No runtime acceptance is claimed.
- Scoped correction/recording complete. Owner GitHub CI execution remains pending source transport and default-branch workflow registration, forbidden by the original no-push scope unless separately authorized. Official owner workflow list is empty; remote main is c4d68ed739977c5e24e5c91e4bfd601e2a44dcd8. Publication and consumer adoption remain separate. Main story/sprint bytes remain in-progress and preserved.

## Review Triage Log

- Medium, patched: reject empty profiles, require nonempty selectors/counts and reconcile the exact required total.
- Medium, patched: walk the actual project-reference closure and validate restored project paths; includes the transitive archive fixture.
- Medium, patched: retain bootstrap provenance, setup outputs and all four workflow step outcomes even on early failure.
- Medium, patched: stream bounded command output, journal start/outcome and retain cancellation/timeout state.
- Medium, patched: bind dirty/untracked input hashes locally and reject them in recorded CI.
- Medium, patched: include the 14 focused binding controls in CI.
- Medium, patched: retain SDK/runtime/Python/platform/runner image identities.
- Earlier medium observations patched: declare the scoped 82-case preflight instead of claiming unprovisioned live suites; bind local artifact-root bytes to the authenticated archive. Earlier gitlink prerequisite resolved by permanent correction and the real candidate commit.
- Independent closure review confirmed all seven final findings closed. Detailed evidence is in the additive implementation receipt. No root story status or historical artifact is rewritten.
