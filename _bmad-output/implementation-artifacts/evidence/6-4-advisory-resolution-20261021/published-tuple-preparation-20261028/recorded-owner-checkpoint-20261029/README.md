# Story 6.4 recorded-owner checkpoint — 2026-10-29

The advisory implementation is already present; no production or test source required a patch. External `/pushall` work has recorded the advanced owners and existing preparation. Fresh public source preparation and ordinary package-based Projects.Server Release compilation pass. The package qualification gate still refuses the exact current tuple, and Platform's four analyzer errors reproduce. Story 6.4 remains `in-progress`.

| Recorded input | Exact revision |
| --- | --- |
| Projects | `0338d6ebf9f1be1bd38a8745759984d2066da5e7` |
| Platform | `494ecb32802d1454c15704066487fa206f5bcec0` |
| Builds | `e909ffd9fe9a6097e36d932010b983d029396968` |
| Platform's committed Builds pin | `e129a29e6c6e2f8a200581e09b861d75a9f3a7b8` |
| Conversations | `a40cfb3ed68f20201cb759f754270b83ea02efb3` |
| Both exact prepared CLI pins | `4.38.2+2574bf2f3da5367d8801f0d7661d66155e5d90b0` |

Every command record retains exact argv, cwd, explicit environment, UTC times, exit and raw stdout/stderr. The original checkout and original indices were used throughout. The successful consumer assets retain Conversations Client/Contracts **1.3.1 packages** and no external module project references; the complete assets and their audit are retained here. Current source catalog selections are Conversations 1.3.1, Tenants 5.11.1, EventStore 3.128.4 and FrontComposer 4.7.1.

| Executed check | Result |
| --- | --- |
| Both owners' tool restore/version/help | Pass |
| Initial public package preparation | Exit 2: HXW011, HXW009, missing-object HXW007 |
| Initial public source preparation | Exit 2: missing pinned catalog object, HXW009 |
| Exact pinned Builds object retrieval | Pass; no ref/index/checkout update |
| Fresh public source retry | Pass, with HXW011/HXW009 warnings |
| Fresh public package retry | Exit 2: exactly HXW011 and HXW009 |
| Ordinary Projects.Server Release build | Pass; zero warnings/errors |
| Package tooling / planning guard | 19 / 37 passed |
| Story and index authority validators | Pass |
| Platform Composition.Tests Debug build | Exit 1; zero warnings, four analyzer errors |
| Narrow unchanged owner-test build | Pass; zero warnings/errors |
| Existing inventory/checklist test | 1 passed |
| Existing owner-pin test | 1 failed: missing nested release-tag context |

There are **57 current passing tests and one retained owner-pin failure**. The broad Platform build is separately blocked. No synthetic fixture-commit methods ran. The narrow harness compiles the unchanged existing `DomainEnrollmentKitTests.cs` and invokes only the two recorded methods, preserving the earlier fallback. It does not suppress analyzers.

The initial source failure came from the locally absent committed Builds object. `git fetch --no-tags --no-recurse-submodules --no-write-fetch-head origin e129a29e6c6e2f8a200581e09b861d75a9f3a7b8` retrieves only that exact object/history into the allowed root-declared Builds repository. Before/after `show-ref` bytes are identical. Both original failed preparations remain retained; corrected retries use fresh nonexistent output directories. The generated source mapping is copied unchanged.

The final package invocation, from Conversations' tool context, is:

```text
dotnet tool run hexalith-module -- prepare-build --manifest /home/administrator/projects/hexalith/projects/module/hexalith.module-manifest.v2.json --mode package --workspace /tmp/story64-recorded-owner-20261029-9q_w2ua_/package-object-available --output json
```

HXW011 identifies untagged exact Platform `494ecb3...`; HXW009 identifies its `e129a29...` Builds pin versus umbrella/index/checkout `e909ffd...`. Earlier HXW008 and dirty-Platform findings are superseded by the newly recorded clean owners. Fresh official read-only Platform refs still contain only v1.0.0/v1.0.1, and its releases API returns `[]`; neither tag identifies the current Platform. No compatible published Platform tuple is established.

The exact current broad command is `dotnet build tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal`, from Platform. Its unsuppressed errors are:

| File in `tests/Hexalith.Platform.Composition.Tests/` | Location | Error |
| --- | --- | --- |
| `AgentsAdmissionCurrentBoundaryTests.cs` | 93:62 | CA2007 |
| `AgentsHostStartupBoundaryTests.cs` | 36:16 | CA1062 (`fault`) |
| `PortableDevelopmentCompositionTests.cs` | 56:16 | CA1062 (`fault`) |
| `AcceptedLaunchBoundaryTests.cs` | 27:16 | CA1062 (`mutation`) |

The unchanged narrow owner-pin test fails while resolving `v4.38.2^{commit}` through the uninitialized nested Builds path. No nested submodule was initialized. That missing context remains distinct from the genuine mismatch: Platform records `e129a29...`, its pinned tool identifies published source `2574bf2...`, and the umbrella selects `e909ffd...`. No owner-pin pass or full Platform acceptance is claimed.

`current-publication-adoption-handoff.json` binds current identities and exact source/configuration hashes. Next work needs an approved compatible exact Builds/catalog selection, separately scoped analyzer repairs, a recorded matching Platform pin, exact-candidate integration/CI qualification, separate publication approval and verified consumer adoption. The historical gitlink patch stays unapplied and is not retargeted; no future commit is guessed.

Root `src/`, `tests/` and `docs/`, plus Conversations `src/` and `tests/`, have no changes from the historical full-suite inputs. This observation is recorded separately and does not claim a new full-suite, authenticated gateway/store, runtime or deployed execution. Existing File support remains explicitly incomplete.

Only the factual story append, its one tracking comment and this new receipt changed. The complete original story/tracking bytes are retained compressed. `verify-evidence.py` verifies original HEAD/index identities, unchanged owner/source/history bytes, original story prefix/frozen block, exact test/command outcomes, consumer package selection, ref preservation and absence of nested initialization. `SHA256SUMS.json` is a local integrity inventory. No source/configuration pin, staged content, commit, branch, push, remote write, dependency adoption, publication or deployment changed.
