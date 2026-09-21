# 6.1-P1R Builds Qualification Evidence

These are durable umbrella copies of the preserved Builds runner candidates.
They are implementation evidence only: Architecture remains `3.70.1`, P1R and
all four owner decisions remain pending, and no acceptance or publication is
inferred.

## Qualification storage policy

Qualification workspaces live on a disk-backed filesystem, never `/tmp` or
another `tmpfs`. A clean worktree and ordinary NuGet/Playwright caches are
reused while their binding inputs remain unchanged. Only the official-feed
restore proof receives an empty disposable package cache. Worktrees, caches,
browser binaries, extracted packages, and build output are not evidence and
must be cleaned on every exit.

Durable evidence is limited to the command ledger, relevant logs, inventories,
qualified produced packages, and their hashes. Store each artifact once by
content hash; retries reference prior passing rows and artifacts when their
binding is unchanged instead of copying them into another attempt tree.

| Bundle | Files | Inventory entries verified | Inventory SHA-256 |
| --- | ---: | ---: | --- |
| `6-1-p1r-390/` (`.8`) | 11 | 6 | `20d21a89db7baf174c81dc8c089ed688b1d96085e66cad16c5d8ac3043b869d4` |
| `6-1-p1r-390-loop3-9/` (`.9`) | 39 | 38 | `9e94b8cfe0f5052dbc305eb288ed545b2b2ded47ec1fc78f1a1a9708b4c77254` |
| `6-1-p1r-390-loop4-13/` (`.13`) | 41 | 38 | `89c63650da9001a341c35db86e4e60d332cc8420a415c79e6217fa53fe67eb67` |
| `6-1-p1r-390-loop5-16/` (`.16`) | 4 | 0 | none; exact null-rule regression failed closed before inventory |
| `6-1-p1r-390-loop5-17/` (`.17`) | 41 | 40 | `70e84d0ab7ad521557324197fbf71d3decc4f88dd3a3c69d2807f63d5895804b` |
| `6-1-p1r-390-loop5-18/` (`.18`) | 41 | 40 | `6a03cd08baea8fc3fdabde4ba564681de54d45f0aa7cbc7301b8ad1c18b812e4` |
| `6-1-p1r-390-loop6-20/` (`.20`) | 44 | 38 | `a93010ffeee7404759c8dc8f2f728f110abc73eeb576dbce1b3e1acc94533e4b` |
| `6-1-p1r-397-20260824/` | 11 | n/a; stopped before G-4 inventory | partial artifact manifest `1c535eac91ae9433a21a20ab1e948e3951a2f62184ac7af137316dfcf0fb0265` |
| `6-1-p1r-397-20260824-attempt2/` | 19 | n/a; stopped before G-4 inventory | artifact manifest `fbe5596f7dc1dd5d3a3bbc3b3ba02f9ff8b7e9da284b9b890fe5baccfe9f25d4` |
| `6-1-p1r-397-20260824-attempt3/` | 28 | n/a; stopped before G-4 inventory | artifact manifest `efe58b140a3a981d4035c1cd0e88b249e7ebf2463f1c57a946adf9d71070425b` |
| `6-1-p1r-3102-20260905/` | 217 | n/a; relocated evidence, not a G-4 inventory verification | artifact manifest `de3b59d4f4d84b2e568f56a9f1aa0f41119bfaa09beedb7412e1e8b7c89cf423` |
| `6-1-p1r-3106-20260921-attempt1/` | 13 | n/a; stopped harness attempt | artifact manifest recorded in the canonical P1R record |
| `6-1-p1r-3106-20260921-attempt2/` | 13 | n/a; stopped harness attempt | artifact manifest recorded in the canonical P1R record |
| `6-1-p1r-3106-20260921-attempt3/` | 28 | n/a; stopped contract execution | artifact manifest recorded in the canonical P1R record |
| `6-1-p1r-3106-20260921-attempt4/` | 32 | n/a; stopped contract execution | artifact manifest `a1b6750f82e0e6f767898d19160c0d1a3339c09d6257cfcba83e3a252721563a` |
| `6-1-p1r-3106-20260921-attempt5/` | 30 | n/a; stopped contract execution | artifact manifest `091678120a13c0ecb8e19ad1c7e4ab28e00886978aa161c862db5cdd52c7831e` |
| `6-1-p1r-3106-20260921-attempt6/` | 57 | n/a; stopped remote-consumer restore | artifact manifest `609416be33bc297f3d6c077d90a4851fdf35151ccff60a032ae45d8ee5411e6a` |
| `6-1-p1r-3106-20260921-attempt7/` | 52 | n/a; stopped pre-restore proof | artifact manifest `7c3a978c1b2e9e84e5d0ae2aeeddd2022490112a699986195d2b50d352b2d0d1` |
| `6-1-p1r-3106-20260921-attempt8/` | 29 | n/a; stopped source build | artifact manifest `b941f549ecb37380ddb5cb99e7362800cb889a0926c69be1f7ee14878c445a82` |
| `6-1-p1r-3106-20260921-attempt9/` | 77 | 14 remote packages verified; stopped Builds Evidence tests | artifact manifest `96a19ee04ff27c9ab235eb3e460a92f328d8644e71585d0eca9cb98b0f44e321` |
| `6-1-p1r-3106-20260921-attempt10/` | 142 | 14 remote packages and 58 retained G-4 files; stopped rollback commitlint | artifact manifest `f8fdf2c215c8a759596472dd5a5f0b0e6156bb139769fd1b44fa44ba91e50cb5` |
| `6-1-p1r-3106-20260921-attempt11/` | 41 | adopted phases 1–4; stopped rollback audit provenance | artifact manifest `8160f13b71756fbbc089f988599c20cf8225af6ce6e76ed582c323918f66c29a` |

Every package and qualification-evidence path named by each inventory was
resolved after relocation, and its current byte size and SHA-256 were matched
to the inventory. Every original file in the three preserved `/tmp` bundles
also matches its relocated copy byte-for-byte. The `.8` copy additionally
contains the four packages from its still-resolvable retained package directory
and evidence copies at the two inventory-relative paths, making the durable
copy self-contained without changing its original five preserved files.

The `.13` bundle also contains:

- `audit-validation.log`, SHA-256
  `7b4f3b9815caf6dcdf7184361d005e3142d56e4afcb7c561e916bb39d95c8e36`;
- `coordinate-verification.log`, SHA-256
  `b71021b1770d05912c2bbcf989b2551bd851fdf43a49ab7e6b91c176fa54667c`.

Those logs retain the exact commands and results proving the 284-package,
139-family, one-source audit; the audit and catalog hashes; the `v3.90.0` / full
EventStore revision identity; the 14-package release-manifest hash/count; the
13 Builds catalog rows; the `3.90.0` runner pin; both stale-pin controls; and
the unchanged Architecture `3.70.1` boundary.

The `.17` lane passed but is superseded because its repository-untracked
fixture mode rendered an inaccurate `<external>` root label. The final `.18`
lane corrects that label to `test/fixtures`, passes build and all `31 + 108 +
1` tests plus every packaged control, and retains 36 exact qualification
artifacts. It remains intentionally nonrelease: its inventory records Boolean
`releaseEligible=false` and `fixtures.mode=repository-untracked`, so the
publisher rejects it before token lookup or push. See the append-only P1R
revalidation record for the complete loop-5 command, artifact hashes, and
pending owner/rollback state.

The `.20` bundle closes the seven loop-6 gaps (see its own `README.md`):
independent `PackageReference`-vs-audit set equality, typed package/family
round-trip fidelity including collection-valued fields, content-parsed
qualification evidence (not hash/size/filename alone), a tracked-fixture-vs-
`HEAD` byte proof, clean/immutable source-tree binding, nupkg/snupkg canonical-
role rejection (zero/multiple-nuspec and swapped-role coverage included), and a
24-scenario regression suite for every new rejection path. It also finally
regenerates `Tools/package-version-audit.json` for real against live NuGet
data (284 packages, 139 families, zero mismatches) and adds the packaged
`hexalith-module test` positive control task 1 was still missing. Its
`releaseEligible=true` describes only the disposable, unpushed worktree used to
produce it (revision `da6490d9`, based on the still-unchanged tracked
`a5316653`); `references/Hexalith.Builds` itself was never modified, committed,
or pushed. See `6-1-p1r-390-loop6-20/README.md` for the full scope-and-honesty
notes and `source-changes.diff` for the complete reviewable diff.

The `6-1-p1r-397-20260824/` bundle is deliberately incomplete failure
evidence. Builds alignment produced local candidate commit
`fb05dd84625abdcd1a62d2664e8557379fd631bb`, but the first EventStore
coordinate row exited `2` after its command argument was malformed by outer
shell substitution. The exact failed log SHA-256 is
`eb0a2e11dc9bbb2bd4e94377b8a8c544c33f9a5dcc588da63e39a3411a8f1a8e`.
No rerun or later gate occurred; no qualification packet or acceptance is
claimed.

The `6-1-p1r-397-20260824-attempt2/` and `6-1-p1r-397-20260824-attempt3/`
bundles are the second and third stopped `3.97.0` attempts. Attempt 3 is the
one that proved repository-supported sibling materialization: the mandated Debug
source-mode restore and serialized build both passed through a sibling catalog
with no nested submodule initialized, and the lane then stopped at
`es-source-003-contracts-test` with 29 failures whose earliest causes are absent
nested dependency bytes. Their `logs/` directories are present on disk but
untracked, because the umbrella `.gitignore` rules apply to them; the
deferred-work ledger carries an entry on retrofitting all three
`6-1-p1r-397-*` bundles.

The `6-1-p1r-3102-20260905/` bundle relocates the 2026-09-05 `3.102.0`
candidate / `3.70.1` rollback attempt out of an ephemeral session scratch
directory, closing the retention gap that attempt's own record recorded. It is
the first bundle whose `logs/` and retained `.nupkg`/`.snupkg` files are
actually tracked: it carries a bundle-local `.gitignore` re-including the 127
files the umbrella rules would otherwise hide, and a bundle-local
`.gitattributes` pinning `-text` so a clone with `core.autocrlf=true` cannot
rewrite line endings out from under the manifest. It is retained failure
evidence only; all four P1R owner decisions remain pending. See its own
`README.md` for the contents table, the deliberate exclusions, and the
verification results.

The eleven `6-1-p1r-3106-20260921-attempt*/` bundles retain the `3.106.0`
candidate run. Attempts 1 and 2 stopped on harness-only static assertions.
Attempt 3 passed coordinate capture, candidate alignment, catalog, audit,
seven-API comparison, source restore, and source build, then stopped when the
contract-exact Microsoft.Testing.Platform command ending in `-m:1` exited `5`
with zero tests. After the human explicitly renegotiated only the test
serialization option, attempt 4 restarted from coordinate capture and the
renegotiated Contracts test passed. The Client test then stopped on `CS1704`
because source-mode evaluation and restored assets selected different
`Hexalith.Commons.UniqueIds` inputs. No later phase or acceptance decision ran.
Attempt 5 added `--no-build` to all test rows and restarted from coordinates.
Its source restore/build passed, but the first Contracts test row stopped after
one of 2,023 tests received process exit `126` instead of `0`. No later phase
or acceptance decision ran. Bundle-local ignore rules make every retained log
trackable.

Attempt 6 is the contract-permitted clean serialized rerun of attempt 5's
retained contention result. It passed both complete EventStore source and
package-source lanes, including 2,023/2,023 Contracts tests in each mode, then
stopped at remote restore with `NU1008` because its retained consumer inherited
umbrella central package management. It did not correct or rerun the first
failure. Builds runner, rollback, and acceptance remained unexecuted.

Attempt 7 again passed the complete source and package-source lanes, then
stopped before remote restore because the effective-CPM proof command had an
unmatched shell quote. The external consumer/config bytes and first failure
are retained; the command was not corrected or rerun.

Attempt 8 stopped during source build when `/tmp` filled. It retains the first
failure and disk-state capture; no prior attempt storage was removed and no
later row ran.

Attempt 9 ran from a fresh root after the earlier task-created worktrees and
caches were safely removed. Both complete EventStore lanes passed, as did the
corrected direct-command CPM proof and official-only remote restore from an
empty cache. Its retained inventory verifies all fourteen selected `3.106.0`
packages and hashes the external project, NuGet configuration, and generated
restore assets. Builds restore/build/Module tests then passed; Evidence tests
stopped the run with exit `2` (59 passed, 9 failed of 68), consistently adding
unexpected `HXE152` evidence-binding diagnostics. No later runner, packaged-
command, rollback, or acceptance row ran.

Attempt 10 used corrected candidate
`ad52f350a2f0bc47849179ae17b4594dafff5363`, whose fixture-binding-only fix
supersedes candidate `5730f5a60c0e106131498126cbee323e24db5f4e`.
Both EventStore matrices, the external consumer, candidate restore/build/test,
and the official required-control G-4 lane passed. The G-4 inventory retains
both CLI packages, both symbol packages, and 53 qualification artifacts under
the unique disposable version `0.0.0-p1r3106-attempt10.1`. The run then
created the requested two rollback commits, but the first rollback
qualification row stopped because both commit bodies exceeded commitlint's
200-character line maximum. The exact exit-1 result and clean post-failure
state are retained; no rollback build/test/G-4, `v3.70.1` EventStore, or owner
acceptance row ran.

Attempt 11 preserved attempt 10's failed rollback branch and created distinct
branch `fix/p1r-3106-rollback-candidate` with two newly messaged,
commitlint-valid commits. Their trees are exactly equal to the corresponding
attempt-10 rollback trees. Adoption of all 27 immutable passing phase 1–4 rows,
candidate-side reciprocal controls, rollback static alignment, restore/build,
117 Module tests, 68 Evidence tests, and catalog validation passed. The next
row stopped at deterministic audit validation: the exact copied audit names
old commit `44bd5eb...` as `generatedFromRevision`, but that old commit is not
an ancestor of the new linear branch. No rollback G-4, `v3.70.1` EventStore,
or acceptance row ran.
