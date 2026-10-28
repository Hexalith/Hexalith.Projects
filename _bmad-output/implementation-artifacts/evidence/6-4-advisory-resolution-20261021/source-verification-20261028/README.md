# Story 6.4 current-source verification — 2026-10-28

HXW004 is resolved in an isolated source-verification workspace. Public source preparation succeeds; two Conversations Debug test-project builds pass with zero warnings/errors and **253 tests pass**, with no failures, errors, skips or not-run cases. The next current-source blocker is **HXW006: Projects has no supported root mapping route**. Story 6.4 remains in progress. Package qualification remains blocked separately.

This receipt continues [the Platform/Builds preparation](../platform-builds-preparation-20261028/README.md). It records current **local owner snapshots**, not latest remote-main integration, release acceptance or Story completion. Remote main observations are retained individually; advancing remote branches were not adopted.

## Source selection and Commons diagnosis

The original Commons checkout and official remote main both identify `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a`; the umbrella HEAD/index identify `1663028fb6d9f46a0bf17919edeb21bb99be1587`. Their exact diff changes only `AGENTS.md`, `CLAUDE.md` and `.github/copilot-instructions.md` (15 added instruction lines). Code and build configuration do not differ. The original public source request reproduces HXW004, exit 2; it is retained as `original-source-refusal.stdout`.

The owning repositories' statuses, full HEAD identifiers, remotes, recent histories, committed dependency pins and contemporaneous `git ls-remote origin refs/heads/main` results were inspected before selection. `owner-identities.json`, `selected-inputs.json` and `commands.json` retain those observations and choices. No moving branch was selected automatically.

| Isolated input | Exact selected revision |
| --- | --- |
| Projects HEAD | `0e45105476902fcfce1f8dbd8416d7234bb16b41` |
| AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| Commons | `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a` |
| Conversations | `861f13a2e728c3847a191b0688070762b1bfa29e` |
| EventStore | `36c78f3435d2d492a73f1b0707cf865c8735d740` |
| Tenants | `7ddb864cda5e942925b056391617eae4895c1253` |
| Folders | `d7e0b3f307e4cd7e740b9f38923c86f8b5c30847` |
| FrontComposer | `5f665c4d38afb2255a7dde4620330e09824a40a1` |
| Parties | `9239c531f3b0cbb4a998c419e537463b597ff14b` |
| Memories | `72c36c7113c750c8b809f9d72b945a2a8c2d4555` |
| McpCli | `2d7311cbbcc592c0ea0348463037389251d1cb4b` |
| Platform committed source | `8de90958bc41b7fd01b7ed22b8ab364447a800e4` |
| Builds tooling source, qualified `v4.38.1` | `16ff70a2321ad576faaac3fe53088c2f1bdacae3` |

Builds is the deliberate exception to current owner HEAD: original `abc0e1be8069d1c7a567515a13bb015adc407536` and its dirty version-catalog changes are excluded. Platform's three existing candidate file edits are also excluded from selected committed source and preserved in the original checkout. No source patch was necessary.

The official Builds tag is freshly checked against the exact selected commit. The previously retained official release inventory still hashes to `5c11240a48feb2811620da1a23b81a7f82aad6dab61daa88f8d8cd10f73512a6`; it records package/source/control qualification passed. The cached signed CLI archive is freshly hashed and signature-verified, exit 0: SHA-256 `f5710df04af88744a48e73f51155dbe41c1e91489ca7cac92d2bb7f53948dff3`, 37,561,135 bytes, NuSpec source `16ff70a2321ad576faaac3fe53088c2f1bdacae3`. No fresh NuGet payload download or complete release requalification is claimed. See `qualified-builds-input.json` and the prior receipt for original official source/CI/publication evidence.

## Supported workspace and generated inputs

`/tmp/story64-source-verification-20261028/projects` is a shared-object clone at the unchanged actual Projects HEAD. Each root-declared direct reference was cloned nonrecursively, detached at its selected real owner revision and absorbed as a direct submodule. Only this disposable root's index was aligned with those real checkouts using `git update-index --cacheinfo 160000,<revision>,<declared-path>`. No composition commit, tag or declaration was invented. Each direct child's `--show-superproject-working-tree` equals the isolated root; every nested `references/*/.git` remains absent. The exact sequence is in `prepare-workspace.py` and `commands.json`.

This matches the existing producer contract: `SubmoduleInitializer` explicitly permits stage-zero gitlinks ahead of HEAD and requires the checkouts to equal them. Original roots, indexes and user files need no mutation. The clones borrow original object databases; the retained hashes/logs are durable evidence, while re-execution needs the workspace or recreation from the selected sources.

From isolated Conversations:

```text
dotnet tool restore
dotnet tool run hexalith-module --version
dotnet tool run hexalith-module -- prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace /tmp/story64-source-verification-20261028/source --output json
```

All exit 0. The tool reports `4.38.1+16ff70a2321ad576faaac3fe53088c2f1bdacae3`. The unchanged owner manifest produces source mapping hash `a369e13673a59c9165c87e0bf0ec8a0fa4b8f6974b16995a916d3a5764351961`, with twelve direct references including active module `Hexalith.Conversations`. `generated-source-mapping/` retains the actual emitted files byte-for-byte. No mapping field or import path was authored or altered by the consumer.

The producer preserves **Platform's committed catalog pin**, `51a71cb1cfe860d5fc135eb3b8521fd80c413810`, despite the qualified tooling source checkout at `16ff70a...`. `catalog-versions.json` and its producer-materialized `pinned-tree/Props/Directory.Packages.props` bind that selection. Git's exact catalog blob has SHA-256 `95622edbc970beea37bf22c2fbd5bfe43cf6e69f2087a8a0f6fac0e0b3097ef6`; the producer decodes UTF-8 and omits its three-byte BOM, yielding SHA-256 `f5084674b831d5649e204de37772ab307de32eeb60b7e1ad84f612ff60fc3f7c`. Those three bytes are their only difference. No dependency version override or dirty catalog was substituted. Source preparation keeps HXW008, HXW011 and HXW009 as warnings.

## Narrow checks and gate results

Every build receives the exact seven values from `buildPreparation.environment`, including all five producer import paths, `HEXALITH_SOURCE_MAPPING_HASH` and `Configuration=Debug`. `commands.json` records exact argument arrays, working directories, explicit environments, UTC start times, durations, exits and separate stdout/stderr for each captured command. The final repeated narrow builds/tests additionally retain a sanitized allowlist of effective build/tool environment variables, including unset values. Earlier observations predate that capture. Other inherited variables and credentials are not recorded; complete environment reconstruction is not claimed. `outer-invocations.json` explicitly identifies the two initially uncaptured outer processes.

| Check | Result |
| --- | --- |
| Conversations.Contracts.Tests Debug build | Exit 0, zero warnings/errors |
| Existing `PlatformEnrollmentMappingTest` | 10 passed |
| Conversations.Server.Tests Debug build | Exit 0, zero warnings/errors |
| Six existing advisory/strict/registration/projection/recovery classes | 243 passed |
| Public isolated package preparation | Exit 2: HXW008, HXW011, HXW009 |
| Projects.Contracts.Tests with actual generated environment | Exit 1: HXW006 before compilation |
| Projects.Server.Tests with actual generated environment | Exit 1: HXW006 before compilation |
| Original Git/user-state comparison | Pass |

Both individual project builds use `dotnet build <project> -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal`. NuGet audit is disabled only for these local checks; MinVer supplies a local Debug assembly/package-output version, not an accepted dependency version. Existing build targets incidentally create Debug `.nupkg`/`.snupkg` files in the isolated output directories. None was published, installed as a dependency or represented as qualified release output.

Built test assemblies run directly with xUnit v3 `-class` filters, `-result-xml` and `-failSkips`. Exact filters are in `verify-lanes.py`, `final-verification.py` and `commands.json`; `test-results.json` records every executed class and the compiled assembly hashes. Test processes have no inherited preparation import environment because mapping tests create independent synthetic fixture mappings; the production assemblies were built with the real emitted environment. The fixture tests are distinct from real workspace preparation and Story acceptance.

After review, the same two individual builds and 253 cases were repeated to bind effective environment, SDK and executable outputs. These final checks again pass with zero warnings/errors/failures/skips. `final-dotnet-info.stdout` records SDK **10.0.402**, commit `a8dd0577c5`, MSBuild `18.9.15+a8dd0577c` on Ubuntu 26.04/linux-x64; `final-dotnet-resolution.stdout` records the actual executable. `executable-closure.json` binds all **499 files** in both test executable output directories before and after execution, with equal inventories and hashes. It includes production/dependency DLLs and runtime/dependency JSON. Final XML is `final-contracts-tests.xml` and `final-server-tests.xml`; the earlier XML is retained as checkpoint evidence. Repetition establishes **253 unique cases**, not 506 distinct tests.

The package request uses the same unchanged Conversations manifest and public command with `--mode package --workspace /tmp/story64-source-verification-20261028/package --output json`. It still rejects the isolated staged Platform revision differing from committed identity `a5e1389fe2c97b14e8c697a2e54f45631d07ef95`, that identity's missing exact release, and the selected Builds checkout differing from its committed owner pin. Official Platform tags were freshly inspected; neither `v1.0.0` nor `v1.0.1` identifies that committed input. The original candidate and released tooling do not qualify it. No release gate was relaxed.

## Next blocker and limitations

The immediate remaining **current-source** blocker is Projects' supported root enrollment/mapping route. The actual mapping correctly includes Conversations and the root-declared references, but excludes umbrella Projects. Projects has no root module/platform declaration; the accepted public preparation command has no active-module/entry-project override. Both narrow Projects builds fail HXW006 for their own production project paths. No replacement manifest or mapping was fabricated to include them. A supported owner/producer route must be established before Projects' Story 6.4 query/gateway/store tests can run against this composition.

Platform exact-release/catalog qualification and package preparation remain independent blockers. This pass does not rerun broad Platform analyzer failures, AppHost/tool prerequisite integration, official Conversations package acceptance, upstream CI, signed gateway/store execution, live runtime or deployed acceptance. Historical evidence is not counted as newly executed tests. File mapping remains unsupported/incomplete.

`preservation-before.json` equals `preservation-after.json`: all thirteen original repository HEADs, branch refs, all refs, raw index bytes, index entries, existing changed/untracked file hashes and nested checkout markers are unchanged. The original advisory spec, its frozen block, sprint status and prior receipts were not edited. No original file was staged; no commit, push, publication, deployment or nested initialization occurred.

The activated `bmad-build` workflow was rendered once with `uv run --no-cache "/home/administrator/projects/hexalith/projects/_bmad/scripts/render_skill.py" --project-root "/home/administrator/projects/hexalith/projects" --skill "/home/administrator/projects/hexalith/projects/.agents/skills/bmad-build"`, exit 0. Its bounded preparation spec is `spec-6-4-current-source-verification.md`; finishing that preparation must not promote Story 6.4.

The initial `python3 <receipt>/capture-inputs.py` run exited 1 because its auxiliary byte-equality assertion had not accounted for the catalog BOM. `capture-inputs.initial.py.txt` retains that failed harness; the exact raw and emitted catalog bytes are retained. The corrected capture verifies the BOM-only difference and succeeds. This did not change source, generated inputs or build/test results.

Raw outer streams, UTC start time, duration and environment for that initial failed capture and the single workflow render are unavailable. `outer-invocations.json` distinguishes session-observed exits/excerpts from original process streams; missing data was not reconstructed. Final `reviewed-verification-harness` and `reviewed-source-capture` invocations have complete process receipts alongside their nested commands.

Run `python3 <receipt>/verify-evidence.py` for read-only retained evidence checks, or add `--live` to compare the original preservation baseline, actual isolated sources, generated imports and built assembly hashes. Offline success does not claim fresh live-workspace verification. `SHA256SUMS.json` binds the retained files; it is an integrity receipt, not an external signature or Story acceptance authority.

The configured Blind Hunter review found ten issues; [the complete triage](review-triage.md) records all ten patches and no deferred item. The tightened live audit also verifies isolated root HEAD/index/child ownership, full source working-tree status and the complete executable closure, and refuses optimized Python. `audit-controls.json` records reversible faults in the disposable workspace: added source, changed production dependency bytes and gitlink drift must fail; optimized execution must fail; positive checks must pass without modifying evidence or creating bytecode. Every temporary fault is restored before the final live audit. Review fixes affect evidence tooling only. Story and package/runtime acceptance remain open.

The original reviewer independently confirmed every finding closed and reran the live read-only audit successfully; [the closure receipt](review-closure.md) records that result. No review item remains deferred. The preparation is complete within its authorized local scope; Story 6.4 remains in progress.
