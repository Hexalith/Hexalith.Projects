# 6.1-P1R Builds Qualification Evidence

These are durable umbrella copies of the preserved Builds runner candidates.
They are implementation evidence only: Architecture remains `3.70.1`, P1R and
all four owner decisions remain pending, and no acceptance or publication is
inferred.

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
