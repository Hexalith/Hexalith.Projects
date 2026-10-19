# Validation and evidence

## Results for this documentation change

- `python3 tools/planning/validate_production_authority.py --story-id 6.4`: exit 0, `PASS: Story 6.4 is within production authority`. This checks eligibility only; it does not approve implementation.
- Documentation checks passed: YAML/frontmatter and all nine companion paths, five unique intent/success capabilities, eight tasks, twelve acceptance cases, seven shell blocks checked with `bash -n`, authored-derivative CRLF/final newline/whitespace, and `git diff --check`. Coherence and preservation passes are recorded in the memlog. Scope checks found no tracked source/test/dependency, frozen-artifact, sprint-status or gitlink change.
- Read-only source/contract inspection covered all four requested owners and Builds at the revisions in `brownfield.md`.
- Cached published EventStore `3.125.1` API inspection confirmed `DelegatedOwnerQueryClient.InvokeAsync(SubmitQueryRequest, CancellationToken)` and the existing `QueryEnvelope` identity/authority properties. This is not verification of the current `3.126.0` package or qualification of either tuple.
- Initial `dnx dotnet-inspect ... --oneline` failed with exit 1, `Error: Unrecognized option '--oneline'.` Retried with the installed tool's supported syntax below; both inspections exited 0.
- `cat .config/dotnet-tools.json` and `cat module/hexalith-projects.module.json`: each reported `No such file or directory`. The G-4 Story 6.4 command was not run, and no TRX/JSON was generated.
- No product tests or runtime qualification were run for this spec-only change. Owner queries, RTI types, provenance additions and their proposed suites are future deliverables.

```bash
dnx dotnet-inspect -y -- member DelegatedOwnerQueryClient \
  --package /home/administrator/.nuget/packages/hexalith.eventstore.domainservice/3.125.1/hexalith.eventstore.domainservice.3.125.1.nupkg \
  -s Methods -v:q -n 15
dnx dotnet-inspect -y -- member QueryEnvelope \
  --package /home/administrator/.nuget/packages/hexalith.eventstore.contracts/3.125.1/hexalith.eventstore.contracts.3.125.1.nupkg \
  -s Properties -v:q -n 40
```

## Future owner qualification commands

Run only after the corresponding owner scope is accepted and implemented. Commands below are validation instructions, not claimed passing evidence. Review the owning repository's current guidance/configuration first. Use root-declared sibling source checkouts only; never initialize nested submodules to make a command pass.

From the Projects workspace root, prepare explicit source coordinates so sibling projects do not resolve nested copies:

```bash
set -euo pipefail
workspace_root=$(pwd)
qualification_dir=$(mktemp -d /tmp/6-4-owner-metadata-qualification.XXXXXX)
source_args=(
  -p:UseHexalithProjectReferences=true
  "-p:HexalithEventStoreRoot=$workspace_root/references/Hexalith.EventStore"
  "-p:HexalithCommonsRoot=$workspace_root/references/Hexalith.Commons"
  "-p:HexalithTenantsRoot=$workspace_root/references/Hexalith.Tenants"
  "-p:HexalithTenantsBasePath=$workspace_root/references/Hexalith.Tenants/src"
  "-p:HexalithConversationsRoot=$workspace_root/references/Hexalith.Conversations"
  "-p:HexalithFoldersRoot=$workspace_root/references/Hexalith.Folders"
  "-p:HexalithFrontComposerRoot=$workspace_root/references/Hexalith.FrontComposer"
  "-p:Hexalith1BuildPackageProps=$workspace_root/references/Hexalith.Builds/Props/Directory.Packages.props"
  "-p:HexalithBuildPackageVersions1=$workspace_root/references/Hexalith.Builds/Props/Directory.Packages.props"
)
run_owner_suite() (
  owner_repo=$1
  suite=$2
  class_pattern=$3
  cd "$owner_repo"
  dotnet build "tests/$suite/$suite.csproj" --configuration Debug -m:1 "${source_args[@]}"
  report_file=$(mktemp "$qualification_dir/$suite.XXXXXX.xml")
  dotnet "tests/$suite/bin/Debug/net10.0/$suite.dll" \
    -class "$class_pattern" -xml "$report_file"
)
```

These are xUnit v3 assembly filters, avoiding unsupported project-level `dotnet test --filter` assumptions under the pinned Microsoft.Testing.Platform runner. Require at least one executed test in every selected class and retain actual counts/failures/skips; a zero-test run is not a pass. The helper allocates a unique report for each invocation.

Existing regression anchors:

```bash
run_owner_suite "$workspace_root/references/Hexalith.EventStore" \
  Hexalith.EventStore.Server.Tests '*BoundOwnerQueryAuthorityTests'
run_owner_suite "$workspace_root/references/Hexalith.EventStore" \
  Hexalith.EventStore.DomainService.Tests '*QueryAuthorityCorrectionTests'
run_owner_suite "$workspace_root/references/Hexalith.Folders" \
  Hexalith.Folders.Server.Tests '*FolderOwnerReadQueryHandlerTests'
run_owner_suite "$workspace_root" \
  Hexalith.Projects.Tests 'Hexalith.Projects.Tests.Resolution.*'
```

Proposed suites to be delivered by T-1 through T-7; they do not exist merely because this spec names them:

```bash
run_owner_suite "$workspace_root/references/Hexalith.Conversations" \
  Hexalith.Conversations.Server.Tests '*ConversationResolutionMetadataQueryHandlerTests'
run_owner_suite "$workspace_root/references/Hexalith.Conversations" \
  Hexalith.Conversations.Contracts.Tests '*ConversationResolutionMetadataContractTests'
run_owner_suite "$workspace_root/references/Hexalith.Folders" \
  Hexalith.Folders.Server.Tests '*FolderOwnerMetadataBatchQueryHandlerTests'
run_owner_suite "$workspace_root/references/Hexalith.Folders" \
  Hexalith.Folders.Contracts.Tests '*FolderOwnerMetadataBatchContractTests'
run_owner_suite "$workspace_root" \
  Hexalith.Projects.Contracts.Tests '*OwnerFileReferenceContractTests'
run_owner_suite "$workspace_root" \
  Hexalith.Projects.Server.Tests '*ReferenceTrustIndexReadTests'
run_owner_suite "$workspace_root" \
  Hexalith.Projects.Integration.Tests '*OwnerMetadataReferenceTrustBoundaryTests'
```

T-7's persisted suite must use existing authenticated gateway/domain-service/owner adapters and actual supported store/provider paths, including restart and two instances. It must collect state contents before/after and prohibit the writer calls; in-memory handler mocks do not substitute. Use the platform fixture selected by the accepted owner test setup, record its provider/version and isolated namespace, and fail if unavailable. Do not build a Projects-owned topology or silently skip unavailable infrastructure. Any missing generic fixture support is separately delivered in EventStore; G-4 remains independent.

Package-mode parity is required against the approved published owner tuple after central enrollment. No package pin is changed by this proposal. Example for Projects' prerequisite suites in a separate clean validation checkout, after source-mode testing and owner enrollment:

```bash
cd "$workspace_root"
dotnet restore Hexalith.Projects.CI.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false
dotnet build Hexalith.Projects.CI.slnx --configuration Release --no-restore -m:1 -p:UseHexalithProjectReferences=false
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj \
  --configuration Release -m:1 -p:UseHexalithProjectReferences=false
dotnet tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests.dll \
  -class '*ReferenceTrustIndexReadTests' -xml "$qualification_dir/projects-rti-release.xml"
dotnet build tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj \
  --configuration Release -m:1 -p:UseHexalithProjectReferences=false
dotnet tests/Hexalith.Projects.Contracts.Tests/bin/Release/net10.0/Hexalith.Projects.Contracts.Tests.dll \
  -class '*OwnerFileReferenceContractTests' -xml "$qualification_dir/projects-contracts-release.xml"
```

Each sibling owner also repeats its changed test projects with `--configuration Release -p:UseHexalithProjectReferences=false` against its accepted catalog. Record restored dependency closure and generated contract fingerprints in the owner's evidence. Do not claim package parity from Debug source assemblies or an unaccepted current catalog. For build/environment failures, retain the exact command and first error, then apply the repository's focused-test/serialized-build fallback ladder without weakening gates or initializing nested dependencies.

## Independent G-4 prerequisite

Deliver the pinned platform tool, Projects module manifest and reads profile in a separate approved work package. After that prerequisite, owner acceptance, remaining exact-current gates and the later Story 6.4 implementation are complete, run:

```bash
dotnet tool run hexalith-module test --profile reads --filter Story=6.4
```

Expected actual artifacts remain `evidence/epic6/6.4-resolution-reads.trx` and `evidence/epic6/6.4-resolution-reads.json`. This prerequisite proposal cannot produce or accept them. Owner qualification artifacts do not substitute for the Story 6.4 authenticated handler-chain run.

## Documentation checks and preserved scope

For this change, check frontmatter/companion paths, unique stable `CAP-1` through `CAP-5` with intent/success, source anchors, proposal-versus-existing labels, frozen-decision preservation, task-to-case-to-command coverage, CRLF/final newline and whitespace. Re-run:

```bash
python3 tools/planning/validate_production_authority.py --story-id 6.4
git diff --check
git status --short
git diff --exit-code -- src tests Directory.Build.props Directory.Packages.props \
  _bmad-output/implementation-artifacts/sprint-status.yaml \
  _bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md \
  _bmad-output/implementation-artifacts/6-4-resolve-projects-with-transient-current-explanations.md
```

Verify root-declared gitlinks and sibling working trees also remain unchanged. Record coherence and source-preservation verdicts in the append-only memlog. No `stories.yaml` or new readiness/status entry is produced by this headless spec run.
