# Projects root enrollment and Story 6.4 source verification — 2026-10-28

Projects root enrollment is prepared in an isolated workspace. Public source preparation succeeds; Projects.Contracts.Tests and Projects.Server.Tests Debug builds both pass with zero warnings/errors. **175 existing tests pass**, with zero failures/errors/skips/not-run cases. Eight evaluated metadata controls also pass. HXW006 no longer blocks these two current-source lanes. Story 6.4 remains in progress; this is a source-tested candidate, not a qualified release.

This continues [the previous current-source receipt](../source-verification-20261028/README.md), using newly inspected current local owner identities. Remote main observations were captured before selection; moving remote branches were not automatically adopted.

## Prepared change

The three candidate files are retained under [reviewed-candidate-source](reviewed-candidate-source/module/README.md) and in [the apply-ready patch](projects-root-enrollment-reviewed.patch). The actual worktree is `/tmp/story64-projects-root-enrollment-20261028/projects`. The patch passes `git apply --check` against the original repository; it has **not** been applied there by this session.

- `module/hexalith.module-manifest.v2.json` declares actual Projects identity, six existing dependencies, shared state binding, six existing workload-protected domain callbacks and `/ready`. The declaration inventories production domain contracts; its README maps every declaration to owning source. It is independent of Story verification, with no invented package, host, runtime callback, recovery hook or test-only identity. Legacy compatibility ingress remains outside the selected internal surface.
- `module/README.md` documents provenance, supported public preparation and remaining composition prerequisites. The existing Projects.Server executable is not packable; `runtime.extensions=[]` does not invent a released Server package.
- `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj` omits only five `GlobalPropertiesToRemove=Version;PackageVersion` entries when source mode, applies=true and a producer hash are all present. Ordinary source and partial markers preserve the prior metadata. Analyzer output, reference-output behavior, explicit netstandard2.0 targeting and package branches remain unchanged.

The existing Builds `WorkspaceRootResolver` maps root v2 module identity `projects` to `Hexalith.Projects`. Its schema/resolver match the qualified tooling source used here. No Builds code change, mapping edit, active-root override or containment exception was needed. The first build after enrollment correctly reached a second HXW006 rejection of conflicting Projects.Contracts reference globals; its logs are retained. The bounded metadata correction resolves that producer contract without weakening the producer.

## Selected sources and qualification boundaries

Projects baseline: `6cdf9bbdc0824e59529c9a99c16b7ec02e03e21b`. Root production source is unchanged from the prior receipt apart from this candidate; external pointer advancement was observed before the preservation baseline. `owner-identities.json`, the owner status/history/remotes/pins/remote-main streams, `selected-inputs.json`, `reviewed-source-inventory.json` and `reviewed-candidate-hashes.json` bind selection and actual source content.

| Direct input | Selected exact commit |
| --- | --- |
| references/Hexalith.AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| references/Hexalith.EventStore | `50221cf7dbe425becc333f6757ba37b93a22ab4e` |
| references/Hexalith.Tenants | `db83309146816602033320270f10b1cea28d3100` |
| references/Hexalith.FrontComposer | `28d2c7f47849bcfba19501f7c18cd9f67f0fc61c` |
| references/Hexalith.Conversations | `ef01d578a00efd527bae9226dca302f54cd012f0` |
| references/Hexalith.Folders | `56550414c58db9b2d46a1a8b45fc77a3156cbee0` |
| references/Hexalith.Parties | `9239c531f3b0cbb4a998c419e537463b597ff14b` |
| references/Hexalith.Commons | `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a` |
| references/Hexalith.Builds | `16ff70a2321ad576faaac3fe53088c2f1bdacae3` |
| references/Hexalith.Memories | `7520a601a1420fd238a48ef6415d47b409a824a1` |
| references/Hexalith.Platform | `fcee54288c75feaadd673e0484da1e5b5d742cc8` |
| references/Hexalith.McpCli | `dbce3b12a802beb0af5020ce89fa5e0ee6f12c6b` |

Each direct child was cloned nonrecursively at its real selected commit and absorbed as a direct submodule. Only the disposable root index was aligned with those checkouts, as supported by SubmoduleInitializer's stage-zero gitlink contract. Nested submodules were not initialized. The original Platform's dirty candidate files were excluded. Original Builds owner HEAD is `2574bf2f3da5367d8801f0d7661d66155e5d90b0`; the selected tooling source is the already qualified **4.38.1** release `16ff70a2321ad576faaac3fe53088c2f1bdacae3`.

The official Builds tag was freshly checked. The cached signed archive was freshly SHA-256 checked and `dotnet nuget verify --all` passed: `f5710df04af88744a48e73f51155dbe41c1e91489ca7cac92d2bb7f53948dff3`, 37,561,135 bytes. Qualification relies on the prior retained official release inventory (hash verified), not a new complete release qualification. No dependency/tool pins or accepted versions were changed.

Crucially, the producer selects **Platform's committed Builds catalog `81ab049208678bd6616828824729c0ddac02f7d1`**, not the tooling checkout or the older receipt's catalog. `catalog-binding.json` binds the exact committed bytes and producer-materialized UTF-8 bytes (BOM removed only). No unqualified version override substitutes for this selection.

## Public producer and narrow execution

`commands.json` retains exact argument arrays, cwd, explicit/effective allowlisted tool environments, UTC times, durations, exit codes and separate stdout/stderr for recorded subprocesses. Source selection/candidate creation and some outer Python/shell/render invocations were issued interactively; their complete outer environment/timing/streams were not captured. The transcript retains those observations; no missing receipt has been reconstructed. Other inherited variables/credentials are intentionally not inventoried.

From isolated `references/Hexalith.Conversations`, whose existing local tool manifest pins the qualified CLI:

```text
dotnet tool restore
dotnet tool run hexalith-module --version
dotnet tool run hexalith-module -- validate --manifest /tmp/story64-projects-root-enrollment-20261028/projects/module/hexalith.module-manifest.v2.json --output json
dotnet tool run hexalith-module -- prepare-build --manifest /tmp/story64-projects-root-enrollment-20261028/projects/module/hexalith.module-manifest.v2.json --mode source --workspace /tmp/story64-projects-root-enrollment-20261028/source-reviewed --output json
```

All succeed. CLI reports `4.38.1+16ff70a2321ad576faaac3fe53088c2f1bdacae3`. The **Projects-generated** mapping hash is `ff68ba1bc7d0e1e431445b6b6bb05621c579769eda46b79f2a84a27ed1b3d4f9`, with active `Hexalith.Projects` and all twelve direct references. `reviewed-generated-source-mapping/` retains every producer file unchanged; `reviewed-generated-input-hashes.json` binds them. Earlier pre-trim declaration preparation, command output and generated files are retained separately; the initial declaration itself was not preserved byte-for-byte. That initial declaration was not the final build input. The final declaration passes public validation and preparation independently.

Both final individual project builds use **all seven exact values** from `reviewed-source-preparation.stdout::buildPreparation.environment`: five producer imports, `HEXALITH_SOURCE_MAPPING_HASH`, and `Configuration=Debug`.

```text
dotnet build tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal
```

Both exit 0, zero warnings/errors. MinVer supplies a local Debug output version; it is not a dependency selection or qualified release. Existing targets incidentally create Debug package archives in isolated output directories; none was published or installed as a dependency. SDK info is retained (SDK 10.0.402 / .NET test runtime 10.0.13).

| Executed lane | Result |
| --- | --- |
| ProjectAdvisoryContractTests | 8 passed |
| ProjectAdvisoryQueryTests, DelegatedProjectAdvisoryConversationDirectoryTests, ProjectQueryEnvelopePrincipalBindingTests, ProjectQueryTenantFilterTests | 166 passed |
| SupportedProjectGatewayHttpTests | 1 passed |
| Evaluated actual Contracts project metadata, eight ordinary/producer/partial/package-marker cases | 8 controls passed, 5 references each |

Test assemblies run directly with xUnit v3 single-dash `-class`, `-result-xml` and `-failSkips`; exact filters are in the command ledger. No stale test assembly or solution-level test shortcut is used. `reviewed-test-results.json` contains every executed case; `reviewed-executable-inventory.json` binds every file in both output directories as captured after execution. This is a post-execution inventory, not a before/after immutability proof. Metadata controls are evaluation-only fixture inputs, not replacement production mappings or compiled acceptance evidence.

## Real gateway/store scope and limits

`reviewed-gateway-store-lane.py` runs the unchanged existing source-only HTTP fixture. It uses actual EventStore gateway, Projects and owner query handlers with bound synthetic signing, loopback service-discovery transport, and a real isolated Dapr 1.18.7 / Redis state store. The Redis image is the immutable digest from the qualified CLI's embedded catalog: `docker.io/library/redis@sha256:ff02b58f971e7d7d156a1267e283fcbbeee91773b6aa36c49dac28ecfe28eadf`. Dapr CLI is 1.18.2. The temporary component is a local fixture resource, not a module declaration or generated mapping.

The existing gateway test covers Conversation and Folder advisory suggestions, explicit unsupported File identity behavior, absent/denied owner equivalence, permission revocation and malformed/unavailable owner responses. It compares raw Dapr HTTP response bytes for **five known persisted keys** before and after the successful Conversation/Folder/File advisory calls. It does not prove unchanged ETags, the full store key set, absence of same-value rewrites, every denial path, or deployed zero-write acceptance. Focused in-memory guarded-store tests separately reject write attempts and cover tenant/principal isolation. These evidences have distinct scopes.

`reviewed-runtime-lane.json` records sidecar argv, binary hash, ports, container identity, readiness and cleanup. The owned sidecar exited 0 and the disposable container was removed. No existing runtime was stopped. The fixture's local signing, owner-head doubles, loopback discovery and replacement ETag service are not live OIDC, native-head authority or full Platform composition.

The declaration also inventories the required bound-stream signing key (with matching EventStore public trust) and symbolic bearer/workload metadata, JWKS and token egress; it supplies no secret or deployment URL. Fixed `statestore` and `projects-conversation-start-setup` component names remain mandatory for registered projections: changing Projects:StateStore does not redirect those paths. No schema configuration key was invented for the fixed names.

A full composed runtime still lacks an owner-supported Projects executable binding (`projects/domain`), Folders declaration, scoped identity/secret inputs and qualified exact Platform integration. The local Aspire CLI is 13.6.1; the released tool embeds 13.6.3 and the existing root AppHost pins 13.6.4. No tool version was upgraded to hide that prerequisite difference. No broad AppHost, distributed integration, CI, deployed runtime or official consumer-package acceptance is claimed. File mapping remains unsupported and explicitly incomplete.

## Next blocker and original-state drift

The next package/release blocker is the **exact qualified Platform/Builds tuple**. Public package preparation exits 2 with HXW011 for unreleased Platform `fcee54288c75feaadd673e0484da1e5b5d742cc8` and HXW009 for its committed catalog `81ab049208678bd6616828824729c0ddac02f7d1` versus tooling/source checkout `16ff70a2321ad576faaac3fe53088c2f1bdacae3`. Fresh official Platform tags identify only `b742b1b7a496ac72d6961ca6db5213d3df42d234` and `99ed78996f4ac23b3851a0609640c179d8822aea`; neither qualifies the selected identity. The same findings remain warnings in source mode. No HXW008 occurs because this isolated Platform checkout matches its selected committed root pin. Current source success does not qualify this tuple, package identities or consumer pins.

**Strict original-state preservation did not pass.** `preservation-check` exits 1, and its initial result is retained. `preservation-drift.json` records every difference without resetting its baseline. External commits advanced Projects from the captured `6cdf9bb...` to `c14adc5a...`, including this session's in-progress evidence/spec, and changed its refs/index. Platform's original three dirty file entries disappeared and its raw index changed while its HEAD/index entries stayed the same. The root reflog and read-only inspections retain that external activity. This session issued no original mutating Git command, commit, push, publication, deployment or nested initialization, and did not undo that external work. A blanket unchanged-original-workspace assertion is unavailable. All tested sources remain fixed in the isolated checkout, and no external source changes were silently adopted for testing.

The original advisory specification, frozen scope and sprint status were not edited by this session. Completing this bounded configuration preparation must not promote Story 6.4.


## Review and read-only evidence checks

The configured Blind Hunter reviewer returned four descriptor omissions. All four were verified and patched: Parties dependency, bound-stream proof key, fixed-name store prerequisites, and identity egress. [The complete triage](review-triage.md) records each finding and correction, with none deferred. `reviewed-verification.py` validates the reviewed declaration, obtains a fresh producer environment and repeats the same two builds and 174 focused cases. `reviewed-gateway-store-lane.py` repeats the existing real store case in a new isolated runtime. These are **175 unique existing tests**, not 350 distinct tests; earlier checkpoints are retained separately. The reviewed mapping JSON is byte-identical to the previous successful root mapping, with newly emitted import paths used unchanged.

Run `python3 <receipt>/verify-evidence.py --live` to compare retained candidates, selected isolated source status/bytes/gitlinks, producer files, catalog selection, executed case reports and complete post-run executable inventories. Omit `--live` for retained-evidence checks. The audit is read-only, rejects optimized Python and explicitly reports that the original shared workspace changed concurrently; evidence success does not convert that failed preservation check into a pass. Recreation scripts refuse workspace reuse and verify the originally observed owners; because the shared root advanced externally, rerunning initial preparation unchanged now correctly refuses. Recreate from the exact selected identities in a new location rather than assuming current main is the tested source.

`SHA256SUMS.json` binds retained evidence files, excluding itself. It is an integrity manifest, not an external signature or release authority. The workflow was rendered once and followed through bounded one-shot preparation/review. Only this preparation spec may be marked done; Story 6.4, package qualification, full runtime enrollment and deployed acceptance remain open.


The original reviewer independently confirmed all four findings closed. Its final follow-up corrected fallback wording to match the SDK: fallback applies only when the entire dedicated workload JWT section is absent. `review-closure.md` records the independent result; final public validation and apply-check pass. This wording-only closure did not change compiled sources, mapping identity or any selected dependency, so the already repeated focused tests were not unnecessarily repeated again.

The exploratory `isolated-selected-catalog-commit` command exited 128 because `git show HEAD:references/Hexalith.Builds` addresses a gitlink rather than a blob. Selection is independently read from committed gitlink metadata and producer catalog output, and exact catalog file bytes are successfully retained. This failed read probe did not change any input. All failed prerequisite/build/audit attempts remain in the command ledger.
