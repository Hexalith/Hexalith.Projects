# Current tuple investigation — 2026-10-30

Read-only investigation only. Story 6.4 remains in progress. All captured commands retain argv/cwd/UTC/exit and separate raw stdout/stderr. No working source/configuration change was made.

Initial local owner identities:
- Projects full identity/index/refs are in initial-state.json.
- Platform a185bfaa5541f8de675fb82e4f553e1e49931201, committed Builds pin cc0db51b800be1d0d5badc3780395cfb5953d0f5, tool 4.38.3 and floor [4.38.3, 5.0.0-0).
- Builds 1160a3a7ddc0f4c6927456b5735a2963261016a0; its committed catalog SHA-256 abe630443a9f095a1bce171c4b10314a714a8bba9f3bde8980a5c40ef46ef172. CRLF checkout bytes hash 812cf5e21b5b77b52f9f1cf7475176ecb8877b3bf56b1af4f43ac6d3e3346652. Both must be distinguished.

Official observations at recorded command times:
- GitHub latest Builds release v4.38.3 and NuGet CLI index ending 4.38.3. Official tag resolves d1931b1e803144048d927fff16cb8119afde541e; source catalog hash 501d72650e3a42afc8fde0345a69208669e7fabdb98b6300f20afca07585d405.
- Platform release list empty.
- Builds remote main 6cc96b4ff23c40c77651e1e4f85ed3170c25c7a4; exactly the same committed catalog as local 1160a3a, plus security/provenance corrections. Its exact-source CI 41102168937 is pending; Commitlint 41102168872 queued at observation. Neither qualifies the candidate yet.
- Platform remote main a57383fd34faec3a6ff891b5c63f0a9f535ed699 pins Builds 69701dfcf54cb9c69fc6cb0c51b031af8d218142, distinct from both local and observed Builds main. Do not relabel local tests as remote-main evidence.
- Targeted fetch of local Platform's committed Builds pin cc0db51 failed exit 128: upload-pack not our ref. Official commit API returns 422. Pin must be corrected to an official qualified producer identity after producer publication; a catalog or source override does not cure this Git identity blocker.

No compatible published tuple preserves the current newer catalog. Differences include Commons 2.30.7, serialization 1.19.6, Memories 3.0.10; explicit FrontComposer.Contracts.UI, Parties.EventStoreExtensions, Memories.EventStore and EventStoreExtensions entries; Azure KeyVault 4.12.0, HotChocolate 16.7.3, Redis 3.4.29 and other versions. Machine-readable property and PackageVersion deltas are retained. Latest remote candidate 6cc96b4 is a concrete existing producer release candidate with newer selections intact. Do not guess a new release version or repack it as 4.38.3.

Minimal Platform correction surface:
- apphost.cs direct SDK/hosting package directives (currently Aspire 13.6.4; compare against selected producer) and project-local TenantRegistry Gateway pin (already 3.128.4).
- .config/dotnet-tools.json, hexalith-platform.json, README.md, committed references/Hexalith.Builds gitlink (future recording approval).
- eng/prepare_builds_artifacts.py currently hardcodes old version/source/archive/tooling hashes; .github/workflows/tuple-ci.yml hardcodes old source twice. A strict exact tuple manifest can centralize source/version/catalog/archive/tooling expected identity, reject incomplete/unpublished bindings, and drive both scripts/workflow. Keep require-recorded, signature verification, clean-source checks, extracted-byte check, package-only Release guards and candidateAcceptance:false. A preparation manifest with absent publication identity must fail closed; no guessed future archive hash/version.
- eng/run_tuple_ci.py retains 27 graph assets, foreign project/assembly rejection, two suite groups (80+2), identity output hashes and terminal failures. Update source/catalog binding coherently; never remove guards for new source.
- Hexalith.Platform.slnx already includes ArchiveProcessFixture, and retained worker barrier fix is in current source. No need to reapply historical fixes.

Producer candidate qualification:
Use official source 6cc96b4 plus exact catalog source blob/hash. Existing .github/workflows/build-release.yml verifies exact live main + successful exact-SHA push CI twice, qualifies source/packages, then protected production approval runs semantic-release. package.json uses commit-analyzer/default Angular; commits since 4.38.3 contain fixes. Compute proposed version only through the real analyzer/dry-run against exact recorded candidate, not an invented number. verify-g4-tool-release.ps1/Tools/publish-g4-tool-packages.ps1 qualify all three actual archives and publish/verify hashes and symbols/inventory. Actual publication is unapproved.

Focused local verification commands (from owning Platform; do not execute Release outside hosted CI):
- python3 -B eng/test_host_builds_resolution.py ; python3 eng/verify-platform-pins.py --catalog <exact producer catalog>.
- dotnet restore src/Hexalith.Platform.TenantRegistry.Host/Hexalith.Platform.TenantRegistry.Host.csproj --configfile <offline.NuGet.Config> -p:Configuration=Debug -p:UseHexalithProjectReferences=false
- dotnet build that project --no-restore -c Debug -m:1 -p:UseHexalithProjectReferences=false
- Capture real host obj/project.assets.json and compare every restored Hexalith package with evaluated catalog. Expected EventStore 3.128.4 plus Commons 2.30.7; direct Gateway change alone may leave transitive Commons below catalog because child CPM disables transitive pinning.
- Restore/build tests/Hexalith.Platform.Composition.Tests individually Debug, use verified exact producer artifacts for Builds assembly binding if available; foreign source Builds fallback is diagnostic only.
- dotnet tests/Hexalith.Platform.Composition.Tests/bin/Debug/net10.0/Hexalith.Platform.Composition.Tests.dll -class Hexalith.Platform.Composition.Tests.GatewaySeedSubscriptionTests -result-xml <new evidence.xml>. Historical count was 4; assert actual fresh count.
- Existing tenant-registry-gateway-alignment-20261030/run-local-host.py is reusable read-only harness: copy to new evidence and correct output locations, never overwrite old output. It starts command-owned host/Dapr/Redis loopback resources; prior liveness/subscription/auth controls passed while readiness stayed 503 (placement disconnected and writer protocol inactive). A fresh run must retain precise current runtime limitations, HTTP outcomes and cleanup. Prior receipt does not count as current execution.

Approval sequence: qualify exact producer source and source CI, independently approve protected producer publication; verify official tag/source, three signed archives and inventory/catalog/tool identity; complete strict Platform publication bindings, obtain approval to record real owner configuration/gitlink, run fresh hosted exact-revision preflight plus remaining runtime/policy/merge-result gates; separately approve Platform publication; separately approve consumer adoption and exact package/symbol qualification. No stage, commit, push, tags, publication or CI dispatch performed by this investigation.
