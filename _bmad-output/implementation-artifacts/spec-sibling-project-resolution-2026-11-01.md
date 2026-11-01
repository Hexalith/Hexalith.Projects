---
title: 'Resolve declared sibling projects without nested submodule initialization'
type: 'bugfix'
created: '2026-11-01'
status: 'done'
route: 'dispatch'
context: []
---

## Intent and authorization

User explicitly requests latest-main submodules, all CI failures fixed and release without human validation; user clarified: never initialize nested submodules, deinitialize any accidentally initialized ones, and use sibling root-declared repositories when nested references are absent. Parent owns the build workflow and remote operations. Implement only from this spec, without starting another skill workflow, staging, committing or pushing. Preserve concurrent user changes and inspect owning guidance/baseline. Never load a skill under references.

## Ownership and defect

Two technical integration helpers still insist on direct nested paths, contradicting the umbrella layout used by their MSBuild source references:

- `/home/administrator/projects/hexalith/projects/references/Hexalith.Memories/src/Hexalith.Memories.Aspire/RepositoryProjectPaths.cs`; tests in `tests/Hexalith.Memories.Server.Tests/Deployment/RepositoryProjectPathsTests.cs`.
- `/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/RepositoryProjectPaths.cs`; tests in `tests/Hexalith.EventStore.AppHost.Tests/Configuration/RepositoryProjectPathsTests.cs`.

Both ResolveReferencedModuleProjectPath methods select self when a module-named solution exists, otherwise only root/references/module, and refuse siblings. Existing negative tests reject arbitrary siblings/ancestors. Preserve that boundedness while supporting the real umbrella layout. This is an internal path-resolution correction, not a new public hosting API or package dependency.

Folders fixture repairs now pass Contracts634, Server1315 and Testing72. Integration849 pass/four failures are precisely the published Memories helper refusing sibling paths in AspireTopologyTests: AddHexalithMemoriesSearchIndexServerShouldRegisterMemoriesSidecarComponentsAndContainers; AddHexalithMemoriesSearchIndexServerShouldReuseSharedStateStoreAndPubSubWithoutCreatingCopies; ComposingMemoriesAlongsideFoldersShouldRemainAdditiveWithStandaloneMemoriesSidecar; MemoriesServerShouldCarryFoldersToFoldersIndexSourceRoutingWhenRoutingApplied. The source fix belongs in the technical owners, never an anonymous endpoint or skipped test. Parent will handle any package publication/adoption needed for package-mode consumers; do not claim a source edit changes an already published package.

## Required behavior

Preserve self and available direct-reference precedence. When the consuming repository lies under a workspace's references/ directory and its nested dependency project is absent, allow the existing sibling under the same references/ directory only if the top workspace .gitmodules declares both the consumer repository and requested sibling at those paths. Use actual files; initialize nothing and make no checkout copies or symlinks. Keep failures for undeclared siblings, unrelated ancestors/root-level checkouts and missing projects; retain useful errors naming expected locations, without suggesting forbidden nested initialization. A current repository missing its own project must still fail rather than silently selecting another clone.

Reuse the existing helpers and mirror the same bounded rule in both intentionally independent technical packages. Follow type-per-file, XML docs and each owner's line-ending/configuration rules. Do not relax authentication, source identity/qualification, CI/package mode or production behavior outside this path choice. Do not alter package versions, catalogs, manifests, gitlinks, workflows or historical evidence.

## Verification

Extend the existing direct/self/negative path tests with declared sibling success, direct-reference precedence with a sibling present, undeclared consumer/sibling refusal, missing sibling project and self-project-missing refusal. Run narrow test classes and builds using repository guidance and an isolated cache; individual built xUnit assemblies are the baseline fallback if MTP launcher fails. No live hosts are needed for these filesystem resolvers. Keep existing negative coverage rather than deleting it. Verify formatting only for edited C# files. Report exact changed files, test counts/commands and any remaining blocker. Parent owns completion, publication and remote state.

## Implementation verification

Both resolver classes passed after review patches: Memories 25/25 and EventStore 34/34, zero skips. Debug/source builds and scoped whitespace checks passed. Empty quoted/unquoted overrides now invalidate old declarations; consumer comments describe the same bounded rule. Owner commits were pushed with commitlint: Memories e8956eb5c1bd49297f86702f9606eb9b013bc2f1 and EventStore 6e9f2fc736e8c7202fdc1069a5da85e9e1b1b5d7. Folders' actual Debug/source AspireTopologyTests now pass 14/14 with all nested checkouts absent, including the four previously failing Memories compositions. Published packages retain their original behavior until the owner release completes; the parent is monitoring the automatic Memories release.

The actual public Memories3.0.15 package passes the full Folders Release/package integration suite853/853 and hosted Folders CI41338377780 is green. The updated EventStore main7776e64fdb5b9b86d0d180a3a8f47c1f11379466 contains the sibling fix and its main CI41336998470 is green.
