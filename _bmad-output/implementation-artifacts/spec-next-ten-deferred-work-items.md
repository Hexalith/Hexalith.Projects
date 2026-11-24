---
title: 'Complete the next ten actionable Projects deferred items'
type: 'chore'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete ten actionable entries in ledger order. Verify each result, record its disposition in the ledger, and commit each completed entry separately. Record blockers and obsolete entries without counting them as completed work. Run the full build and test suite before pushing directly to `main`.

</frozen-after-approval>

## Implementation Notes

- The ledger combines numbered entries with unnumbered findings. Process their order within the file and count the original independent obligations. Verification-only entries need evidence against the exact relevant source or runtime.
- Start from Projects `6cd73dca5bb90076e809bf2a9fa28ce96e8c233b` on `main`.

### Partial direct dependency checkout

The disposable probe used the Memories build properties and compiled `RepositoryProjectPaths` from owner revision `667fbf2564ee2a1a30487e24cba20b37eebb76d0` under SDK 11.0.100. The synthetic umbrella declared Memories and EventStore as direct siblings. Inside Memories, a partial direct EventStore checkout had an Aspire project directory and lacked the host project. The sibling had both project files.

`dotnet msbuild Probe.csproj -getProperty:HexalithEventStoreRoot,HexalithEventStoreFromSource -p:Configuration=Debug -p:UseHexalithProjectReferences=true` selected the partial direct root. With the direct Aspire project file absent it left `HexalithEventStoreFromSource` empty. Adding that file changed the property to `true`. In both layouts, executing `RepositoryProjectPaths.ResolveReferencedModuleProjectPath` for the host returned the umbrella sibling. This verifies the selection mismatch. The owner still needs to make the build and runtime choose one coherent checkout.

## Skipped entries

| Entries | Reason |
| --- | --- |
| DW-34 | The accepted historical baseline is EventStore 3.110.0 with Builds 4.29.1. Propagating 3.90.0 would obscure that record. |
| DW-36 | `abcc588db40e9618ed7f8dd09c385cbca30896d6` already replaced sequential direct deletion with quarantine and rollback before final deletion. The existing cleanup suite passed all 23 cases. |
| DW-48 | Project detail has a watermark. The setup path still needs authoritative source-head and rebuild observations bound to that detail before it can prove freshness. |
| DW-54 | The ledger records incompatible decisions about preserving or breaking v1. A contract owner needs to settle the migration path. |
| DW-63 | The approved sequence puts the host migration alongside the blocked first Epic 7 writer slice and its replay, fencing and rollback proof. |
| DW-64 | Both concurrent commits are independently identifiable ancestors of main. The historical entry names no remaining product defect. |
| Durable legacy cleanup | Byte-complete recovery after partial physical deletion needs a retained backup and a decision about when destructive cleanup commits. Quarantine can restore a failed move before deletion. |
| DW-72 | The ledger reserves the retention-policy decision for the Test Architect. |
| DW-74 | Builds removed the bundled npm publisher chain. A fresh pinned Builds lock audit returned zero advisories. |
| DW-75 | The readiness flake and prepared hidden-file patch concern the Builds-owned qualification suite. The five-second case remains unqualified locally. |
| DW-77 | The direct Platform revision has no exact release tag and records a different Builds pin. It needs a coherent owner release. |
| DW-78 | The Folders query needs an exact native Folder/file binding. A Projects file-only input cannot supply that binding and the owner production binding authority is missing. |
| DW-79 | The current lock already bundles undici 6.29.1 via npm 11.21.3. A fresh audit omitted all three tracked advisories and reported 36 separate moderate findings. Registry signatures and release tooling tests passed. |
| DW-80 and the duplicate actual-owner advisory probe | The supported browser command timed out during isolated `aspire start`, before either Folder readiness probe. The approved two-second read deadline still needs a successful live acceptance run. |
| DW-82 | Contracts 2.0.0, McpCli enrollment and the inventory update landed. The green McpCli CI condition still depends on the owner resolving the Commons.UniqueIds package conflict. |
| DW-85 and the related context admission entry | Supported Refresh and owner-backed trust require the approved G-2 owner batch contracts and index schema. |
| Historical G-4 live lane, cleanup and startup diagnostics | Current shared CI uses a different Dapr cohort. Builds `7461fb05`, `643c6405` and `b7a9c9ec` contain the owner implementations of cleanup and diagnostics. |
| G-6 pin comment, dirty OQ8 selector and 14-package replay | Builds `00d91d52` replaced the comment. EventStore `6a0b6441f` retired the old controller and verifier. Active replay uses the accepted 3.110.0 baseline. |
| 3.108.1/3.109.0 images, containment, archive and API smoke | The older candidates lack immutable image/signature records and were superseded by 3.110.0. GitHub returned 404 for artifact 10905232490 and 410 for retained job 108394631248 logs. |
| Duplicate Builds xUnit consumer | Builds `af20682a` made the consumer/package identity explicit. The old rows are for distinct packages. |
| P3 deployment graph and historical no-build health receipt | The production owner graph is absent. The exact old process output and binary bytes needed to bind the retained health observation were not captured. |
| Folders metadata v2 package parity and old Platform preview tuple | The published Folders.Client 2.0.1 and Release adapter regressions cover v2. The currently qualified G-6 cohort supersedes the EventStore.Aspire 3.110.0 preview combination. |
| G-6 EOL reproducibility | Builds currently hashes executed checkout bytes and the Projects policy disables reuse. Cross-checkout canonicalization belongs to the Builds helper. |
| PostgreSQL backup provenance and isolated projection diagnostics | Both concern EventStore-owned evidence or application discovery. The requested historical inputs and owner test seam are outside Projects. |
| Story 6.1 callback, Development catalog and source-graph follow-ups | The recorded callback and route-catalog defects predate the accepted gateway/owner proof. The missing source APIs predate the current mapped dependency graph. |
| Story 6.4 snapshot, scale and owner launch work | The approved advisory reset supersedes the AD-32 snapshot condition. Story 8.9 owns latency proof. Cold zero-write inspection and the mutation dispatcher are EventStore and Conversations owner work. |
| EventStore authoring path, Admin source edges and synchronous OTLP | The implementation boundaries belong to EventStore. The Projects mapping cannot set those owner contracts. |
| Direct delegated advisory 404 | Production `/query` requires EventStore workload authority. Reclassifying outcomes for unsupported direct callers needs an API policy decision. |
| Folders catalog observer and deployed gateway advisory proof | The required independent owner authentication, catalog and deployment evidence is absent. |
| Managed accessibility/application dependency qualification | The live AppHost and authenticated browser verification need to complete. A browser framework smoke test alone cannot establish acceptance. |
| Tenants readiness, bootstrap buffer, package consumer, SDK pin, cleanup and transport budgets | The runtime implementations and coupling live in Tenants or shared EventStore transport, with separate owner verification. |
| Ambiguous actor commits and native query body consumption | These require EventStore recovery and transport policy changes. |
| Folders no-catchup drain and intermittent proposal confirmation | Neither retained result identifies a reachable failing seam that can be repaired safely. The first needs an owner source; the second has no record of the guard that returned 404. |
| McpCli inventory signer, live list and native topology | Signer capacity and live supported enrollment require the McpCli owner. |
| Released cutover binary identity and per-cell startup attestation | These are release/adoption obligations. Acceptance requires the exact published binary and evidence schema. |
| Folders generated client and owner ambiguity/correlation regressions | The client requires a published owner-generated v1 transport or routed v2. The owner endpoint tests belong in Conversations and EventStore. |
| Projects CLI/MCP legacy retirement | The recorded decision retains compatibility until McpCli migration and native verification gates pass. |
| Advisory equivalence above the supported limit | The approved normalization rules do not define equivalence for materially different coverage. An owner decision is required. |

## Review triage

- Medium, patched. The Conversation-start receiver checks the forwarded Project and Conversation. The native request controls use the real handler and observe all four instrumented services. The denied actor and missing Project exercise different depths by design.
- Medium, patched. The matrix separately proves cursor transport behavior and exact physical addresses. The closed unavailable pairs must match their distinct statuses and the entire safe retained problem body. Stale list and open still require their authorized safe denials. References with unavailable membership use the established required-evidence reason.
- Medium, scoped to the ledger's policy audit. The path test asserts the adapter's actual requested class and checks the real owner classifier. The HTTP adapter continues to deny the owner's safe 404. The test establishes classification and adapter behavior at their respective boundaries.
- Medium, patched. List checks explicit inspection authority before discovery and returns the metadata-only 503 above 200 Tenant candidates. The exact-limit control returns all 200 visible rows. The limit applies before per-Project authorization.
- Low, patched. The 2.0.0 upgrade note includes the retained Context Refresh route and overload.

## Focused verification

- Mapped Debug Server.Tests build passed with zero warnings and errors. Gateway boundary 2/2, owner path policy 5/5, Folders permissions adapter 47/47, and retained list 8/8 passed.
- The real authenticated Gateway test passed against an isolated Dapr 1.19.1 in-memory state component. All 77 cutover comparison cells executed and passed. Six interleaved Conversation-start refusals matched the approved safe-404 behavior and 50 ms floor.
