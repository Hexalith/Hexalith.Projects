# Story 6.4 resumption after external landing — 2026-10-27

Story 6.4 remains **in-progress**. The advisory implementation and reviewed CI repair are already landed. This pass changes only the story's resumption record and evidence; no source, dependency/tool selection, owning index, commit, push, publication or runtime was changed.

The actual root was externally advanced to `c978a5e86c8134ba09a166e093a1a4d6b4db77a8` before this pass, containing the previous parent-final evidence/spec checkpoint. Its initial tree was clean. Local Builds is `22569245259c70f212e3073b5241c660b5d01487`, Conversations `cec9a1e1bbdf7e8efb2cb3aa299add7f52c2a6ef`, and Tenants `2cf3a9660f71f6877923772cd145aa6856907945`. No external operation was duplicated. The original frozen advisory block retains SHA-256 `78e8a29557f949b064db4111f8d487ff4c4d15f3f7d57284e72905f797b9a7df`.

## Current local observations

The candidate producer's runtime-free `prepare-build --mode source` now succeeds directly against the clean actual umbrella. It generates mapping hash `162bcbb45ca899333bdf62ebe3df12e3ded3fe6c1b03afd9bbacf285cbcc10d7`; no disposable source workspace or user Git mutation is needed. The source-mode HXW011/HXW009/HXW010 diagnostics remain advisory and do not establish package qualification. Exact argv, private output and environment are in [commands.json](commands.json) and [the owner build receipt](conversations-build.json); [source-mapping.json](source-mapping.json) retains the actual producer result.

| Check | Current result |
| --- | --- |
| Planning guard | 37 passed, zero failures |
| Story and index authority | Both pass |
| Accepted CLI version | 4.35.2 |
| Accepted CLI `prepare-build` | Exit 1, HXC001, unavailable command |
| Candidate source preparation | Exit 0, actual umbrella, no runtime |
| Candidate package preparation | Exit 2, HXW011/HXW009/HXW010 |
| Mapped Conversations.Server.Tests Debug build | Exit 1, two HXW006 errors, zero warnings |
| Ordinary Projects.Server.Tests source Debug build | Exit 1, four Conversations enrollment errors, zero warnings |
| Narrow Projects Contracts/core source test-project builds | Exit 1, same missing Conversations producer mapping |
| Reusing the Conversation mapping for root Projects | Exit 1, correctly refuses unmapped Projects root; invalid reuse attempt, not a separate defect |

The current owner command is `dotnet build tests/Hexalith.Conversations.Server.Tests/Hexalith.Conversations.Server.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 -v:minimal`, executed from Conversations with the producer-generated environment recorded in its JSON receipt. HXW006 names the retained Tenants.Client→Tenants.Contracts and Tenants.Contracts→EventStore.Contracts references because `AdditionalProperties` is set. Tenants `Directory.Build.targets:7` appends `HexalithEventStoreVersion` to every source reference; the producer reconciliation permits only one exact selected-catalog `Version=` assignment. This distinct Tenants/producer compatibility mismatch was retained for scope assessment; no source or guard was changed. The other direct and fallback attempts, exact commands, logs and results are retained alongside this receipt. No stale assembly was run or labeled as fresh current-source proof.

## Read-only release observations

[Release checks](release-checks.json) observe Builds live main `cbe58fa72f92327752fe0d75617ae91fe18da5d9` and queued [exact-source CI 40561467073](https://github.com/Hexalith/Hexalith.Builds/actions/runs/40561467073). This supersedes canceled prior run 40560052493. Conversations live main remains `26298b31eb4d362f88e008246ac3acb160128330`; [CI 40559488680](https://github.com/Hexalith/Hexalith.Conversations/actions/runs/40559488680) has successful repository/package-compatibility jobs and failing accepted-tool package-preparation jobs, while other work remains pending. These are observations at the recorded time, not a claim that either workflow completed successfully.

The unchanged official source verifier exits 1 with `EXACT_CI_ABSENT`: newest push CI for live Conversations main is not successful. Its official destination verifier exits 0 and confirms Conversations 1.3.1 destinations remain absent. No release dispatch was performed. Producer qualification/publication, a compatible accepted tuple/tool, then separately approved Conversations 1.3.1 publication and verified consumer pins remain delivery prerequisites.

The original File gap and runtime/deployment limits remain. No fresh .NET suite, signed gateway/store or live AppHost acceptance is claimed. Earlier passing XML remains historical; [audit.json](audit.json) records current source hashes and the exact local revisions. Logs are retained as deterministic gzip with their bytes hashed in [manifest.json](manifest.json); no credentials or private runtime logs are archived.
