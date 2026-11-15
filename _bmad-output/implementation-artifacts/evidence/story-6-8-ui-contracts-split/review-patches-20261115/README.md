# Story 6.8 review patch verification — 2026-11-15

Both approved patch findings are resolved on the uncommitted working tree over
`51d0a418`. R1 updates the projection catalog and parity matrix to distinguish
UI.Contracts presentation descriptors from Contracts wire vocabulary. R2 extracts
the test-only `UnbadgedEnum` into its own internal type file.

`source-manifest.tsv` pins the four patched files. The earlier story evidence and
its manifest remain the receipts for the pre-patch tree; they are not replaced.

## Focused verification

Run under `run-with-temp`; the private dependency-mapping workspace and all build
outputs were below the supplied `$TMPDIR` and were removed when the job exited.
The retained command lines include the actual disposable paths.

1. Prepare the Projects source mapping with the existing local Builds CLI:
   `dotnet references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/bin/Debug/net11.0/Hexalith.Builds.Module.Cli.dll prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace "$TMPDIR/prepared" --output json`.
   Result: `prepared`; full output in `source-preparation.json`. Every emitted
   `buildPreparation.environment` value was used unchanged for the build and run.
2. `dotnet build tests/Hexalith.Projects.UI.Tests/Hexalith.Projects.UI.Tests.csproj --configuration Debug --artifacts-path "$TMPDIR/artifacts" -m:1`.
   Result: exit 0, zero warnings/errors (`ui-tests-debug-build.txt`).
3. Invoke the built test assembly with
   `-noColor -class Hexalith.Projects.UI.Tests.Contracts.ProjectVocabularyDescriptorTests -result-xml <retained-xml-path>`.
   Result: 42 total, 42 passed, zero errors/failed/skipped/not-run
   (`ui-vocabulary-tests.txt`, native `ui-vocabulary-tests.xml`).
4. Documentation checks: all nine presentation ownership entries name
   UI.Contracts; the shared badge row no longer refers to removed enum
   attributes. Both touched C# files use CRLF. `git diff --check` passes.

The first unprepared Debug/source build stopped at Conversations' required
Platform source-mapping guard. Its exact command and error remain in
`ui-tests-debug-build-unprepared.txt`. The prepared build above resolves that
setup failure without changing sibling code or weakening a gate.

## Planning publication

The active production-authority index passed before changes. A complete candidate
set contained sprint status, deferred-work ledger, P0 artifact, and the existing
P1R acceptance record. Only Story 6.8 changed, from `review` to `done`; all other
candidate bytes and comments were preserved. Candidate validation used all four
required path options, then the exact validated change was published and the
active index was validated again. See `authority-active-preflight.txt`,
`authority-candidate-index.txt`, `authority-active-index.txt`, and
`authority-story-6.8.txt`. Every command returned 0/PASS.
