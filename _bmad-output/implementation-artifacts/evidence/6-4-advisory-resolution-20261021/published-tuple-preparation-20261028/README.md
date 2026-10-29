# Story 6.4 published tuple preparation — 2026-10-28

The local preparation now targets the already published Builds **4.38.2**, exact
source `2574bf2f3da5367d8801f0d7661d66155e5d90b0`. Its source, official release
qualification and signed NuGet archive verify. No compatible published Platform
identity exists in the observed official refs. Actual public Projects package
preparation therefore still refuses the original committed tuple. Story 6.4 stays
`in-progress`; the candidate is not ready for immediate Platform release dispatch.

The [previous source/store receipt](../local-store-verification-20261028/README.md)
and [reviewed preparation](../platform-builds-preparation-20261028/README.md) remain
immutable. Current source/store evidence already contains 3,592 passing cases;
those suites were not repeated or added to this continuation's test count.
The parent independently reran their retained evidence verifier before these edits
(`parent-prior-evidence-audit.stdout`, exit 0).

## Exact published candidate

The official [Builds v4.38.2 release](https://github.com/Hexalith/Hexalith.Builds/releases/tag/v4.38.2),
tag API and original root HEAD/index/checkout all identify `2574bf2f3da5367d8801f0d7661d66155e5d90b0`.
Exact-source [CI 40660642386](https://github.com/Hexalith/Hexalith.Builds/actions/runs/40660642386)
and [Release 40664505738](https://github.com/Hexalith/Hexalith.Builds/actions/runs/40664505738)
succeeded. The official inventory's GitHub asset digest and size verify. It records
an official package build, executed source validation and controls, all passed,
with clean source at that exact revision, `releaseEligible: true` and no
ineligibility reasons. The retained audit explicitly asserts every binding.

The freshly downloaded official NuGet archive is 37,673,585 bytes. Its SHA-512
and size match the official NuGet catalog; its NuSpec repository commit matches
the tag. `dotnet nuget verify --all` passes with its NuGet.org repository signature
and no offline-revocation override. Signed archive SHA-256:
`31f9a94c8497c2c038cb922b078d5067f6b2fb7e11f96746e07bdbccf80d4305`.
`published-candidate-identity.json` retains the computed binding. The inventory's unsigned package digest
differs from the repository-signed feed ZIP; no unsigned ZIP reconstruction or
equivalence is claimed. The standalone `.nupkg.sha512` endpoint returned HTTP 404;
the official catalog supplies the successful independent SHA-512/size binding.

The actual archive's embedded runtime catalog selects EventStore `3.128.4`,
FrontComposer `4.7.1`, Aspire SDK/Hosting `13.6.4`, Dapr CLI `1.18.2` and runtime
`1.18.7`. Source catalog Conversations remains `1.3.1`. No .NET dependency version
or resolver code changes. `src/libraries/Hexalith.Builds.Tooling`,
`src/libraries/Hexalith.Builds.Module.Cli` and the Workspace test directory are
unchanged between 4.38.1 and 4.38.2. This candidate preserves the existing root
catalog and source/store inputs; a moving branch or catalog downgrade was not used.

Read-only Platform remote tag observation still returns only `v1.0.0` and
`v1.0.1`. Their exact annotated and peeled refs match the retained local tags;
the official GitHub releases response is empty. The newer tag is
`99ed78996f4ac23b3851a0609640c179d8822aea`, pins Builds
`b15dab4cc740b6fb2d2065682f76b8166a7e120a`, and its catalog selects Conversations
`1.3.0`. Neither tag identifies current Platform
`fcee54288c75feaadd673e0484da1e5b5d742cc8` or the prepared 4.38.2 tuple. No compatible
published Platform tuple was established, and none is invented.

## Minimal local preparation

Only five owner files changed:

- Platform `.config/dotnet-tools.json`: exact CLI 4.35.2 → **4.38.2**, preserving
  disabled roll-forward and the rest of the manifest.
- Platform `hexalith-platform.json`: range floor 4.35.2 → **4.38.2**.
- Platform `README.md`: identifies the local candidate and pending qualification,
  exact source/catalog, matching Aspire/Dapr prerequisites and unapplied gitlink.
- Conversations `.config/dotnet-tools.json`: 4.38.1 → **4.38.2**, necessary for
  the prepared Platform floor. The rest of that manifest is unchanged.
- Conversations `module/README.md`: an adjacent preparation note, preserving
  every historical observation below it.

`prepared-source-hashes.json` and `prepared-source/` retain the exact final inputs.
The complete owner correction also needs Platform's Builds gitlink changed from
`81ab049208678bd6616828824729c0ddac02f7d1` to the published `2574bf2...` source.
`platform-builds-gitlink.UNAPPLIED.patch` contains that exact change;
`git apply --check --cached` passes. It was not applied or staged. The current
root Builds catalog and all four pre-existing Conversations 1.3.1 constraints in
`tools/release-packages.json` are byte-identical to entry. The untracked Projects
module declaration and README remain byte-identical to their prior applied receipt.

## Executed checks and honest limits

`*.command.json` records exact argv, cwd, explicit environment, UTC start/end,
durations, exits and raw stdout/stderr paths. `commands.json` aggregates those
records. `relevant-ambient-environment.json` records the existing build-related
environment without credentials. No producer mapping, dependency override,
alternate Git index, synthetic identity/tag or suppression was supplied.

| Check | Result |
| --- | --- |
| Platform and Conversations tool restore | Pass |
| Both public versions and prepare-build help | Pass; `4.38.2+2574bf2...` |
| Normal package-based Projects.Server Release build | Pass; zero warnings/errors |
| Actual consumer assets | Conversations Client/Contracts 1.3.1 packages; no external module project references |
| Existing package tooling suite | 19 passed |
| Broad Platform Composition.Tests Debug build | Exit 1; three existing unsuppressed analyzer errors |
| Narrow build of unchanged DomainEnrollmentKitTests | Pass; zero warnings/errors |
| Existing inventory/checklist method | 1 passed |
| Existing tool-floor/owner-pin method | 1 failed on missing nested release-tag context |
| Public Projects package preparation, accepted 4.38.1 and prepared 4.38.2 contexts | Both exit 2 with HXW011/HXW009 |

The final planning guard passes **37 tests**, and both story/index authority
validators and root/owner whitespace checks pass. Together there are **57 newly
passing tests and one retained owner-pin failure**. No synthetic Builds identity/preparation/catalog test
classes ran: their shared `WorkspaceGitFixture` creates temporary commits, which
the current spec explicitly forbids. The unchanged production guards were
exercised through actual original-checkout public preparation instead. No test
was edited, bypassed or counted as acceptance without execution.

The broad Platform build retains CA2007 in `AgentsAdmissionCurrentBoundaryTests.cs:93`,
CA1062 in `AgentsHostStartupBoundaryTests.cs:36` and CA1062 in
`AcceptedLaunchBoundaryTests.cs:27`. The fallback compiles the actual unchanged
existing `DomainEnrollmentKitTests.cs`; its two invoked methods create no fixture
commits. The owner-pin method verifies the new exact manifest/range before failing
to resolve `v4.38.2^{commit}` from Platform's uninitialized nested Builds context.
No nested repository was initialized to repair it. This failure is separate from
the real committed-pin mismatch: direct allowed sibling/official source checks
prove Platform still records `81ab049...`, whereas the required published source
is `2574bf2...`. No owner-pin pass or full Platform integration is claimed.

## Actual package gate and handoff

Both original-checkout public invocations use the existing Projects manifest
from the Conversations tool context, with separate fresh nonexistent output paths.
The final command is:

```text
dotnet tool run hexalith-module -- prepare-build --manifest /home/administrator/projects/hexalith/projects/module/hexalith.module-manifest.v2.json --mode package --workspace /tmp/story64-published-tuple-20261028/package-prepared-4382 --output json
```

It exits **2**, preserving exactly three diagnostic instances:

- HXW011: committed Platform `fcee54288c75feaadd673e0484da1e5b5d742cc8` has no
  local release tag. The fresh official-ref observation confirms it is not either
  published tag.
- HXW011: Platform has the intentional uncommitted preparation changes.
- HXW009: that committed Platform pins Builds `81ab049208678bd6616828824729c0ddac02f7d1`,
  while original umbrella index and actual checkout select `2574bf2...`.

Local configuration cannot alter those committed identities. No new HXW010 is
manufactured: the original committed Platform range still governs this actual
request. The prepared consumer tool supports the future reviewed floor but
does not record it or clear any current guard.

`publication-adoption-handoff.json` binds the exact current Platform base, five
prepared file hashes, unapplied gitlink, official Builds release/source and
consumer base. The next owner must separately approve recording that correction,
resolve the existing build blockers in scope, complete exact-candidate integration
qualification/CI, and only then obtain separate push/publication approval. Verify
the resulting exact released Platform identity before actual consumer/umbrella
adoption and another public package preparation. No future commit is guessed.
Current complete release/consumer qualification remains unfinished.

## Preservation

`verify-evidence.py` is read-only. It checks the immutable entry snapshots,
prepared source hashes, every pre-existing tracked owner file except the exact
authorized five-file preparation and story/tracking append, all 13 original
HEAD/index pairs, prior untracked evidence hashes, absence of initialized nested
references, original complete story prefix and frozen block, current consumer
assets, actual diagnostic instances and executed result counts. The original
entry snapshot is losslessly stored as `initial-state.json.gz`, with its original
uncompressed SHA-256 and size retained in `initial-state.compression.json`. The preservation
audit passed for all **111,464 original tracked files**, with only the seven
authorized paths changed, all **486 prior untracked evidence files** unchanged
and all **13 original HEAD/index pairs** equal. Local integrity
inventories bind evidence bytes; they are not external signatures or acceptance.

No staging, commit (including fixture commits), branch, push, remote write,
publication, deployment, nested initialization, resolver change, qualification
weakening or unqualified adoption occurred. Unsupported File mapping and live
deployment/production acceptance remain the prior explicit limitations.
