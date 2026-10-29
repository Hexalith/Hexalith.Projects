# Story 6.4 current owner advancement — 2026-10-28

The implementation and existing local preparation were preserved. Platform and Builds advanced externally after the previous receipt. The original published-tuple verifier correctly rejects the changed Platform identity; its retained assertions and historical evidence are unchanged. This is a current-input mismatch, not a defect in the prior receipt. Story 6.4 remains `in-progress`.

## Current identities and retained preparation

| Input | Current value |
| --- | --- |
| Projects HEAD | `22c99ad1ae0aa88cd563f2d85346c9d680acc2ca` |
| Root HEAD/index Platform | `fcee54288c75feaadd673e0484da1e5b5d742cc8` |
| Root HEAD/index Builds | `2574bf2f3da5367d8801f0d7661d66155e5d90b0` |
| That recorded Platform's Builds pin | `81ab049208678bd6616828824729c0ddac02f7d1` |
| Actual Platform HEAD | `1d748b70d5f728f237a7d86048e8f4636f770601` |
| Actual Platform HEAD's Builds pin | `e129a29e6c6e2f8a200581e09b861d75a9f3a7b8` |
| Actual Builds HEAD | `e909ffd9fe9a6097e36d932010b983d029396968` |

Platform's upstream changes do not modify the bases of its three prepared files or the existing DomainEnrollmentKit checks. The local tool/floor preparation remains **4.38.2**, as does Conversations' prepared tool. No owning file was changed here. Builds advances the catalog's Tenants selection from **5.11.0 to 5.11.1** and removes the prior temporary package-only override. Its Tooling, CLI and Workspace source directories are unchanged versus 4.38.2. The historical `81ab049...`→`2574bf2...` gitlink patch now fails its read-only cached apply check and remains unapplied. It was not retargeted to downgrade the new catalog.

`current-identities-and-source-delta.json` retains these Git observations. `initial-state.json.gz` captures all 13 current owner/root HEAD/index pairs, 112,242 tracked file hashes and 2,593 existing evidence/module file hashes before this continuation. The complete original story and tracking bytes are retained separately. No remote observation was refreshed; local tag state is not new official publication-absence evidence.

## Current executed checks

Every `*.command.json` records the exact argument array, cwd, explicit environment, UTC start/end, exit and stdout/stderr. `relevant-environment.json` records the existing build environment without credentials. Commands ran sequentially against the original checkout; no alternate index, source substitution, synthetic identity or guard suppression was used.

| Check | Result |
| --- | --- |
| Platform and Conversations tool restore/version/help | Pass; exact `4.38.2+2574bf2...` |
| Fresh actual public Projects package preparation | Exit **2**: HXW008, HXW011, HXW011, HXW009 |
| Fresh actual public Projects source preparation | Exit **0**, with the same four tuple diagnostics as warnings |
| Normal package-based Projects.Server Release build | Pass; zero warnings/errors |
| Actual retained consumer assets | Conversations Client/Contracts **1.3.1 packages**, no external module project references |
| Current Platform Composition.Tests Debug build | Exit **1**: four unsuppressed analyzer errors |
| Existing package tooling suite | **19 passed** |
| Production-authority planning guard | **37 passed** |
| Story/index authority and root/owner whitespace | Pass |

The **56 current passing tests** are the package tooling and planning cases only. Historical full source/store suites (3,592 cases) were not repeated or promoted to changed-input acceptance. No new signed gateway/store, full Platform integration, runtime or deployed acceptance is claimed. Source preparation alone does not qualify those results. The exact generated Projects source mapping is retained.

The new consumer build uses ordinary package mode and no dependency override. Its assets contain no direct Tenants package entry, so the catalog's 5.11.1 selection is not misrepresented as a direct consumer graph entry. An initial local assets-audit lookup incorrectly expected such an entry and raised KeyError; the failed assumption and corrected actual-graph audit are retained, and contribute no acceptance tests.

Platform still fails CA2007 at `AgentsAdmissionCurrentBoundaryTests.cs:93` and CA1062 at `AgentsHostStartupBoundaryTests.cs:36` and `AcceptedLaunchBoundaryTests.cs:27`. Its externally added `PortableDevelopmentCompositionTests.cs:56` adds another CA1062 for public theory parameter `fault`. No analyzer was suppressed and no unrelated source repair was made. The unchanged narrow owner-pin test was not rerun: the retained prior missing nested-tag failure is historical, while current direct Git inspection proves the committed pin mismatch without initializing nested sources.

## Exact current package gate and handoff

The actual package invocation from Conversations' prepared tool context uses the existing Projects manifest and a fresh nonexistent output directory. The exact temporary path is in `public-package-current.command.json`:

```text
dotnet tool run hexalith-module -- prepare-build --manifest /home/administrator/projects/hexalith/projects/module/hexalith.module-manifest.v2.json --mode package --workspace <fresh-output> --output json
```

The response refuses:

- **HXW008:** actual Platform `1d748b7...` differs from root-recorded `fcee542...`.
- **HXW011:** root-recorded `fcee542...` has no local SemVer release tag.
- **HXW011:** the actual Platform checkout retains its intentional local preparation changes.
- **HXW009:** root-staged Builds `2574bf2...`, actual Builds `e909ffd...` and recorded Platform's Builds pin `81ab049...` differ.

The actual Platform HEAD's newer `e129a29...` pin does not override the root's recorded identity. No stale patch, moving branch, staged update or fake release was used to clear the guards. Local 4.38.2 preparation remains preserved; its historical catalog no longer matches the advanced source catalog. No compatible current published Platform tuple is established by this continuation.

[current-publication-adoption-handoff.json](current-publication-adoption-handoff.json) binds all exact current coordinates and the unchanged prepared file hashes. Reconciliation of the intended published Builds/catalog tuple, four Platform analyzer fixes in separate scope, real owner recording, exact-candidate integration/CI, separate push/publication approval and verified consumer adoption remain open. No future Platform commit is guessed.

## Preservation

[verify-current-evidence.py](verify-current-evidence.py) is read-only. It verifies every original HEAD/index, unchanged tracked source/configuration and historical evidence, the complete original story prefix/frozen block, the one tracking-comment update, current command/result counts, actual consumer assets and the stale patch's refusal. `SHA256SUMS.json` binds this receipt's bytes as a local integrity inventory, not release authority. Parent audit outputs may be added separately.

Only a factual story checkpoint, its tracking comment and this child receipt were added. No source/configuration pin, historical receipt, root/module preparation, index/ref, dependency version, nested repository, remote state, publication or deployment changed. Unsupported File identity and deployed/production acceptance remain explicit limitations.
