# Story 6.4 owner prerequisite delivery

Status: **in-progress; RTI and parent Story 6.4 handlers remain gated**. This run delivers owner source changes and preserves the distinction between executable behavior, installation completeness and accepted qualification. It does not register an RTI, accept G-4 consumption, migrate historical references or change the parent handlers.

This supplements the [starting revalidation](../6-4-projects-rti-revalidation-20261020/owner-contracts.md), [prerequisite spec](../../spec-6-4-prerequisite-current-authorized-resolution-evidence.md) and [contract annex](../../6-4-resolution-evidence-contracts.md). Historical evidence is unchanged. The closing source and verification manifests bind the exact owner tuple, working patches and commands; a commit SHA alone does not identify uncommitted source. Source comparisons include concurrent commits and identify reviewed bytes, not authorship of those commits.

## Owner source delivery

| Contract | Delivered behavior | Remaining gate |
| --- | --- | --- |
| SDK transport and Folders build | Canonical projection-only and metadata endpoint mappings reuse existing SDK policies while preserving custom owner routes. Folders CS1061 is resolved; owner and Projects integration projects compile with Debug source references. | Packaged execution and exact-current qualification remain separate. No pins were changed. |
| Installation inventory and writer admission | Explicit EventStore enrollment requires independent namespace and operation authorities. The actual actor metadata inspection supplies native incarnation, full-prefix digest, head identity and erasure/retention evidence. Published cuts retain this native vector across reads and persisted recovery, detecting equal-head replacement and native evidence downgrade. Persisted native zero seeds are distinct from uncreated sources. | No verified complete legacy/virgin installation census, production resolution namespace authority, all-writer credential/admission enrollment or supported-scale resolution coverage has been established. No default certifies completeness. |
| Conversation metadata | Reuse the newly committed isolated content-free metadata generation, lasting commit proof, native membership events, explicit scoring purpose/classification, narrow delegated read and persisted rebuild/recovery barriers. | The original producer source head and isolated receipt selection are persisted with metadata; native Tenant Read and current source/owner inspection use the existing signed SDK transport, with a final Tenant recheck after resource I/O. The production host lacks legacy-compatible native guard enrollment, and native post-rebuild selection promotion remains unsupported. A local cursor or health age is not native authority. |
| Folder/File source | Native accepted mutations allocate owner File identity independently of Projects FileReferenceId. Content-free isolated identity rows and strict delegated owner/inverse queries enforce exact parent and declared mutation lineage. Authorization and generation checks precede both positive and negative classification; authority is checked again after final persisted I/O. A separate opaque workspace/path target digest excludes mutable display/policy metadata. The 10,000-incarnation history bound is enforced before effects; tombstones are retained. Delete/add, replacement and undeclared movement invalidate continuity; tombstoned identities/references cannot be reused. | Native Folder/Organization source-to-generation proof and current Tenant authority must be executable before these queries disclose protected metadata. Sequence-only legacy permission reads do not establish this contract. |
| Projects FileReferenceId binding | The native mutation path requires independent current Projects binding authority before accepting a reference. No permissive authority is registered. | Current Projects events store FileReferenceId and FolderId, but no native OwnerFileId; there is no whole-Tenant unique native reference-binding registry/protocol. Generic Folder write permission, path/digest IDs and old links cannot fill this gap. Existing unmapped references require explicit owner re-verification. |

These Folder identities describe accepted native **workspace mutations**. They do not establish a complete committed File catalog or prove commit/abort/workspace-reset/branch lifecycle. No query can disclose these rows in the default composition without missing native authority. Production committed File identity/mapping remains incomplete; no path, digest or mutation ID is promoted into that authority.

The implemented failure paths are safeguards, not substitutes for the missing production contracts. Tasks with these dependencies remain incomplete. Existing committed EventStore transport repairs are reused. Dependency pins, protected parent handlers and accepted qualification state were not changed by this run; concurrent checkout and gitlink advances were retained.

## Platform check requested by the user

Inspected Hexalith.Platform source at `11893ca9fd85644cab8a0414b2076bf346f108f6`, including composition declarations, TenantRegistry host, Secrets.Inventory native reader and CurrentInventoryAuthority. The inventory reader uses restricted credentials and complete native Tenant folds for active secrets enrollment. CurrentInventoryAuthority verifies HMAC/OpenBao mapping; it does not certify Projects/Conversation/Folder/File inventory or actor Read authority. Corrected searches across `src`, `docs` and `eng` found no resolution namespace/installation authority implementation or enrollment. Failed path probes are retained and are not counted as absence evidence.

Read-only Kubernetes metadata inspection used the configured `jpiquot@local` context: 24 namespaces, 55 deployment/statefulset/daemonset objects, zero `applications.app.k8s.io` objects and 14 Dapr components. The inspected Tenant/EventStore/inventory workload metadata belongs to the isolated secrets environment. This establishes no qualified resolution authority in the inspected source/metadata; it does not prove absence in every environment. No Kubernetes Secrets or workload environment values were read, and no deployment or credential reuse was performed.

## Verification limits

The mandatory Projects Aspire baseline started, but security awaited missing client-credential parameters and remained unhealthy while dependent services waited. The exact owned AppHost was stopped without force. This is not a live owner integration or G-4 pass.

Focused test fixtures prove source behavior, persistence and failure handling. Substituted installation authorities and permission providers do not establish production completeness. Native actor/storage tests and in-process HTTP tests are identified separately from unavailable deployed lanes in the retained command results.

The broad Folders core suite has four failures that read an absent nested `references/Hexalith.Builds/Props/Directory.Packages.props`, plus two external provider fixture skips. No nested dependency was initialized and no build gate was weakened. The Projects Dapr gateway test is skipped because its isolated Story 6.1 source lane is unavailable.

## Required before RTI or parent implementation

1. Certified complete installation inventory, actual native source coverage and enforced all-writer admission for every owner, including initialized-empty proof and supported-scale bounds.
2. Executable native resource/permission-to-generation bindings for every disclosed owner row, including equal-head replacement, revocation/regrant, retention, recovery and final disclosure checks.
3. Native committed File catalog and authoritative Projects FileReferenceId-to-native File mapping with whole-Tenant uniqueness, exact parent and commit/abort/reset/branch lifecycle guarantees; explicit re-verification for historical unmapped links.
4. Exact-current qualification and independently accepted G-4 consumption by Projects. Local source tests and static receipts do not provide these acceptances.

No RTI or parent handler gate is opened by this evidence.

## Exact revisions and retained verification

| Repository | Closing HEAD | Comparison baseline |
| --- | --- | --- |
| Projects | `7096ee4caf4757a76e086f878c5f79bd52559d3e` | preservation/context only |
| Hexalith.EventStore | `5a38f36cab94e013183ed264b583da61e6eb91d4` | `6c56c27bc1a8d63c83f9631f7162cf79fdde0a1e` |
| Hexalith.Conversations | `beacc095f40e1cb254d3258bccab34659691cbaa` | `0c7bfac19cafbf4cbb2f67340b06e569280ef05a` |
| Hexalith.Folders | `eed43a86c94b49e030b233beb37fe6128456f649` | `9bacd7f62848c23222dc7309635eb65dc6e5ed9c` |
| Hexalith.Tenants | `1f0f4edb6be91dd07028c0303a3f308841c3b414` | `1f0f4edb6be91dd07028c0303a3f308841c3b414` |
| Hexalith.Builds | `e40ed9c7110d56a2f38c9291627728aea2f205cb` | preservation/context only |
| Hexalith.Platform | `11893ca9fd85644cab8a0414b2076bf346f108f6` | preservation/context only |

The [source manifest](source-manifest.json) binds 148 changed owner source/test files to SHA-256 hashes and complete patches from the comparison baselines. These deltas include concurrent commits; they are not an attribution claim. The archive retains the original reviewed snapshot and the final corrected snapshot separately. [Preservation evidence](preservation.json) confirms protected parent/spec/sprint/pin bytes and the frozen block remain unchanged. No stage, commit, push, dependency update or submodule update was performed by this run.

| Final checkpoint | Passed | Failed | Skipped | Command record |
| --- | ---: | ---: | ---: | --- |
| SDK Client native inventory/feed, reader and batch recovery | 362 | 0 | 0 | `final-eventstore-client` |
| SDK Server native actor/proof/current authority | 104 | 0 | 0 | `final-eventstore-server` |
| SDK canonical endpoint mapping | 92 | 0 | 0 | `final-eventstore-routes` |
| SDK real delegated-origin client | 9 | 0 | 0 | `review-domainservice-focused-01` |
| Tenant native Read/Owner authority | 19 | 0 | 0 | `final-tenants-authority` |
| Conversation Server full suite | 1013 | 0 | 0 | `final-conversations-full` |
| Folder core focused mutations/authorization | 93 | 0 | 0 | `final-folders-core-tests` |
| Folder Server full suite | 1055 | 0 | 0 | `final-folders-server-full` |
| Projects owner integration subset | 89 | 0 | 1 | `final-projects-owner-integration` |

All eight closing source builds listed in [commands.md](commands.md) exited 0. [Command records](command-records.json) retain exact working directories, argv, outcomes, source and log hashes. [Verification summary](verification-summary.json) identifies final, failed, stale and superseded evidence. The [archive](verification-artifacts.tar.xz) contains full command records/source maps, logs, native XML, patches, Platform checks, sanitized Aspire results and review correction evidence. [Artifact hashes](artifact-manifest.json) bind every archive member.

The focused native tests use the SDK source codec, serialized actor/read-model state and signed HTTP client path. Installation/permission test doubles remain test doubles. Passing recovery, revocation/regrant and initialized-empty cases establishes source behavior, not certified production coverage. In particular, warm actor inspection does not qualify cold activation, and successful legacy metadata rebuild/recovery does not complete native post-rebuild promotion.
