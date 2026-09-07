# 6.1-P1R `3.102.0` candidate / `3.70.1` rollback attempt — 2026-09-05 evidence

This bundle relocates the surviving evidence of the **2026-09-05** partially
executed, non-qualifying P1R attempt out of an ephemeral session scratch
directory and into the repository. It closes the retention gap the canonical
record admits in its §8 ("Evidence retention gap").

Relocated on **2026-09-07** under
`_bmad-output/implementation-artifacts/spec-6-1-p1r-requalify-with-sibling-materialization.md`.

**This bundle is retained failure evidence only.** It is not a qualification
packet, owner acceptance, Architecture authority, publication authority, or
permission to close P1R or any downstream gate. No qualification lane was run
to produce it, no coordinate was qualified, no gitlink was moved, and no nested
submodule was initialized. All four owner decisions remain pending.

Canonical record:
`references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p1r-eventstore-source-architecture-runner-revalidation-record.md`,
section "3.102.0 candidate / 3.70.1 rollback revalidation attempt — 2026-09-05",
as corrected by the appended section
"Correction and evidence relocation — 2026-09-07".

## Source

Everything here was copied byte-for-byte from the 2026-09-05 session scratch
directory:

```
/tmp/claude-1000/-home-administrator-projects-hexalith-projects/db9a45af-df47-47c7-b1c2-39d9e83626f8/scratchpad/
```

That directory was verified present and readable on 2026-09-07. **No artifact
was reconstructed, re-derived, re-run, or reformatted.** All 211 relocated
entries were compared to their sources with `sha256sum` after the copy; all
matched. See `manifest-verification.txt`.

## Contents

| Path | What it is |
| --- | --- |
| `results/eventstore-3102-selected-result.md` | Selected-coordinate EventStore lane narrative (`4ae9cee1…` / `v3.102.0`) |
| `results/eventstore-3701-rollback-result.md` | Rollback-coordinate EventStore lane narrative (`f13f9925…` / `v3.70.1`) |
| `results/builds-3102-candidate-result.md` | Builds candidate lane narrative (`3d16d3e0…`) |
| `results/builds-3701-rollback-result.md` | Builds rollback lane narrative (`6ea4c27a…`) |
| `results/commit-msg.txt` | Commit message used for the Builds candidate commit |
| `results/commit-msg-3701.txt` | Commit message used for the Builds rollback commit |
| `logs/eventstore-3102/` (34 files) | Per-row stdout/stderr logs and `.meta` sidecars, selected EventStore lane |
| `logs/eventstore-3701/` (30 files) | Per-row logs and `.meta` sidecars, rollback EventStore lane |
| `logs/builds-3102/` (17 files) | Per-row logs, Builds candidate lane |
| `logs/builds-3701/` (36 files) | Per-row logs, Builds rollback lane |
| `release-packages-3.102.0.json` | EventStore release manifest captured at `v3.102.0` |
| `release-packages-3.70.1.json` | EventStore release manifest captured at `v3.70.1` |
| `p1r-diff.txt` | Retained diff captured during the attempt |
| `remote-consumer-3102/RemoteConsumer.csproj` | Disposable remote-restore consumer project (13 `PackageReference` rows at `3.102.0`) |
| `remote-consumer-3102/NuGet.Config` | The nuget.org-only configuration used for that restore (`<clear/>` sources, `<clear/>` fallback folders) |
| `remote-consumer-3102/Program.cs` | Consumer entry point |
| `remote-consumer-3102/packages-cache-inventory.txt` | **Derived** listing of the disposable restore cache (see "Deliberate exclusions") |
| `retained-packages/3.102.0/` | G-4 packaged-command artifacts retained by `-RetainPackageDirectory` at the candidate revision, plus `g4-tool-package-inventory.json` and the 36-file control-output `qualification-evidence/` directory |
| `retained-packages/3.70.1/` | The same set at the rollback revision |
| `artifact-manifest.sha256` | Path + SHA-256 for every entry above |
| `manifest-verification.txt` | Result of re-hashing every manifest entry (SHA-256 `497bcaf44db6a39b6f198bf1fc6939affa172080497698526167b3b92ca2a9f7`) |
| `.gitignore` | Bundle-local negations so the logs and packages are actually tracked (see below) |
| `.gitattributes` | Byte preservation: `-text` so no clone rewrites line endings out from under the manifest, `-whitespace` so relocated evidence is never whitespace-"fixed" |

The bundle holds **217** files: **211** relocated byte-for-byte, **1** derived
(`packages-cache-inventory.txt`), and **5** authored here (`README.md`,
`.gitignore`, `.gitattributes`, `artifact-manifest.sha256`,
`manifest-verification.txt`). `artifact-manifest.sha256` covers **215** of them —
every file except itself and `manifest-verification.txt`, whose own SHA-256 is
recorded in the row above so no bundle file is left unattested.

### Why this bundle carries a `.gitignore`

The umbrella `.gitignore` has generic build-output rules — `[Bb]in/`, `[Oo]bj/`,
`[Ll]og/`, `[Ll]ogs/`, `*.meta`, `*.log`, `*.nupkg`, `*.snupkg`. Under those
rules **127 of this bundle's 217 files** would sit in the working tree untracked
(117 under `logs/` via `[Ll]ogs/`, the 2 `retained-packages/*/qualification.log`
files via `*.log`, and the 8 `.nupkg`/`.snupkg` packages), i.e. as perishable as
the scratch directory this relocation exists to escape. (The earlier `6-1-p1r-397-20260824*` bundles are in
exactly that state: their `logs/` directories are present on disk but ignored.)
A bundle-local `.gitignore` re-includes them. Its scope is this directory only;
no earlier bundle and no file outside this bundle is affected.

### Deliberate exclusions (excluded on purpose — not missing)

- `remote-consumer-3102/packages-cache/` — 630 MiB of restored NuGet content.
  Reproducible restore output, not primary evidence. Replaced by
  `packages-cache-inventory.txt` (227 lines: 215 restored `<id>/<version>`
  pairs plus the `dotnet tool install` entry, under a 6-line header), including
  all 13 `hexalith.eventstore.*` library packages at exactly `3.102.0`.
- `remote-consumer-3102/nuget-home/`, `remote-consumer-3102/tmp/` — scoped HOME
  and temp directories for the restore. The 14th release-manifest ID,
  `Hexalith.EventStore.Admin.Cli`, was proven separately via `dotnet tool
  install --tool-path`; its resolved `3.102.0` store path is recorded at the end
  of `packages-cache-inventory.txt`.
- `wt-eventstore-3102`, `wt-eventstore-3102-source`, `wt-eventstore-3102-package`,
  `wt-eventstore-3701`, `wt-eventstore-3701-source`, `wt-eventstore-3701-package`,
  `wt-builds-3102`, `wt-builds-3102-env`, `wt-builds-3701`, `wt-builds-3701-env`,
  `wt-builds-record`, `sdk-install` — ~8.5 GiB of disposable Git worktrees,
  per-lane scoped environments, and the isolated `global.json`-pinned SDK
  `10.0.302` install. These are execution environments, not artifacts. The only
  evidence they held that is not reproducible from Git is the retained G-4
  package set, which **is** relocated here under `retained-packages/`.

### Absent artifacts

**None.** Every artifact named as a relocation source was present and readable
on 2026-09-07 and is in this bundle. Nothing was listed as absent, and nothing
was reconstructed to fill a gap.

## Verification performed on 2026-09-07

1. **Copy fidelity** — every relocated file re-hashed and compared to its
   scratch source: **211/211 identical**, zero differences (`diff -rq` clean for
   all four `logs/` trees and both `retained-packages/` trees).
2. **Manifest self-check** — `sha256sum -c artifact-manifest.sha256` from this
   directory: **215/215 OK**. Recorded in `manifest-verification.txt`.
3. **Prior bundles preserved byte-for-byte** — `sha256sum -c` re-verified the
   manifests of `6-1-p1r-397-20260824`, `6-1-p1r-397-20260824-attempt2`, and
   `6-1-p1r-397-20260824-attempt3`: all pass, none modified.
4. **Independent corroboration of the record's §8 package hashes** — the four
   retained G-4 `.nupkg` files re-hash to exactly the values the canonical
   record's §8 table already published:

   | Package | SHA-256 | Matches record §8 |
   | --- | --- | --- |
   | `Hexalith.Builds.Module.Cli.0.0.0-p1r3102.1.nupkg` | `69c9c71a1dd3a89be397190d389813d8c0e0b7248160953600a481f6741ace45` | yes |
   | `Hexalith.Builds.Evidence.Cli.0.0.0-p1r3102.1.nupkg` | `1709c7194731ae8ad59643a881af5a0f7ee93db5c1eddb71ba991de5c350b904` | yes |
   | `Hexalith.Builds.Module.Cli.0.0.0-p1r3701.1.nupkg` | `2543a3da7c7246575dab659057ea239e47d13695a365840d84cd967c473c1c65` | yes |
   | `Hexalith.Builds.Evidence.Cli.0.0.0-p1r3701.1.nupkg` | `e544e22ae833fe76904a98352c413fe04cf4107cdef3ae4f4d75ec51948dbc1e` | yes |

## What the corrected record says about these logs

The canonical record's 2026-09-05 §5 attributed every EventStore lane failure
to a *nested-submodule boundary conflict* requiring an EventStore Owner ruling.
The 2026-09-07 correction restates that: the MSBuild catalog import resolves
through a **repository-supported sibling fallback with no nested submodule**.
The 2026-09-05 lanes failed because their worktrees were placed **flat**, so
none of `Directory.Packages.props`' four import candidates could resolve. The
`NU1010`/`MSB4019` rows in `logs/eventstore-3102/` and `logs/eventstore-3701/`
are therefore evidence of a **worktree-placement defect**, not of a structural
boundary blocker. Read them with the corrected section, not §5 alone.

The real, still-open blocker is narrower: EventStore's repository-governance
tests read dependency **bytes** at `<eventstore-root>/references/<Name>` and run
Git commands against those paths. That is what owners must rule on.
