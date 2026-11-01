---
title: 'Repair Folders CI fixture composition after dependency updates'
type: 'bugfix'
created: '2026-11-01'
status: 'done'
route: 'dispatch'
context: []
---

## Intent and authorization

The user requests all changes pushed, every root-declared submodule on latest main, latest stable SDK/packages, CI failures repaired, and release without human validation. No nested submodule may be initialized; all references must fall back to sibling root-declared repositories when nested checkouts are absent. Parent owns the build workflow and remote operations; implement this scoped repair without invoking another skill workflow, staging, committing or pushing. Preserve concurrent user changes. Read owning repository guidance and the workspace Hexalith baseline first; never load any skill under references.

## Owning repository and code map

Work in `/home/administrator/projects/hexalith/projects/references/Hexalith.Folders`.
CI run 41330384463 at 7d396697 fails four existing test projects. Logs are `/tmp/folders-job-124919331116.log` and `/tmp/folders-job-124919331184.log`.

- Contracts has five stale assertions demanding domain-ci.yml@main; actual workflow intentionally pins an immutable Builds commit beginning 1cc94bbb. Validate the intended immutable caller contract rather than reintroducing a mutable reference or removing coverage.
- Server test ProgramShouldUseAuthenticationBeforeAuthorizationAndEndpointMapping searches routes.MapFoldersServerEndpoints() while production correctly calls app.MapFoldersServerEndpoints(). Correct the stale source assertion while retaining the authentication/authorization ordering check.
- Testing has two stale scaffold assumptions: SDK10.0.402 must follow current stable10.0.403, and Folders.ServiceDefaults legitimately references Hexalith.EventStore.ServiceDefaults. Preserve meaningful dependency direction checks.
- Integration has68 failures largely caused by synthetic hosts bypassing authorization middleware for endpoints that now have named HumanPolicy metadata. Inspect MixedSurfaceHandoff, GoldenLifecycle, ContextReadAuthority and routing fixtures. Some hosts use UseRouting then MapFoldersServerEndpoints without UseAuthorization. RoutingModeTestHost uses a synthetic RoutingTest scheme while HumanPolicy explicitly chooses Bearer, yielding401. Reuse AddFoldersServerTestDefaults and existing fake-human authentication helpers. SDK PostConfigure ordering may matter.

## Required implementation

Repair existing test composition and stale conformance expectations. Maintain production HumanPolicy requirements, authenticated actor/tenant semantics, business authorization, negative cases and fail-closed behavior. Do not suppress failures, skip tests, remove required assertions, make endpoints anonymous, weaken production authorization, or claim synthetic fixtures establish live acceptance. If an actual production defect is necessary to fix, report its exact evidence before expanding beyond the described fixtures; parent can decide from the already authorized maintenance scope.

Keep C# type-per-file, reuse helper types, and preserve LF source policy. Fix formatter output on changed C# files. Do not initialize child submodules or modify gitlinks; sibling paths are available and parent owns synchronization.

## Verification and completion

Run focused Release/package builds and each of the four affected test projects individually with CI=true, an isolated NUGET_PACKAGES cache, source references disabled, and serial builds. Follow the baseline built-assembly xUnit fallback when MTP launcher fails. Use existing /tmp/projects-release-maintenance/nuget cache if useful. Run only relevant tests as needed while iterating, then all four affected suites; record exact pass/fail/skip counts and commands. Do not start Aspire or other hosts outside the test fixtures. Verify formatting only for edited C# files. Report files changed, actual verification, and anything unresolved. Leave this spec status in-progress; parent owns overall completion.

## Implementation verification

Committed and pushed 0de035bab991e2de3e3414112fae8fe7a68842e8. Release/package builds passed without warnings/errors. Contracts 634, Server 1315 and Testing 72 passed. Integration 849 passed/four failed only in the preexisting published Memories 3.0.14 path resolver. After the technical-owner sibling fixes, actual Debug/source AspireTopologyTests passed 14/14 including all four former failures; no nested checkout was initialized. Review patches passed six focused helper cases and 16 inventory tests, with scoped format/diff checks. Hosted CI 41335230353 is running against the committed standalone fixture; all specialized, accessibility and E2E jobs have passed, main unit lane remains active. Synthetic fixtures do not establish live acceptance.

## Hosted prerequisite repair

CI41335230353 proved all stale assertions and fixture composition fixes pass; only the four known Memories topology cases fail. The reusable main job initializes build prerequisites rather than every Folders root declaration. Removing build-time automatic initialization therefore exposed an absent direct Memories checkout even in standalone CI. The specialized jobs already explicitly initialize their declared roots and pass.

Add scripts/prepare-ci-topology-projects.sh and configure the existing pinned reusable caller's package-prepare-script input to invoke it. This workflow-owned preparation may initialize only the root-declared references/Hexalith.Memories checkout in a standalone GitHub Hexalith/Hexalith.Folders workspace. It MUST refuse a superproject/submodule checkout, a nonmatching GitHub repository, wrong working workspace/root, and a missing or changed root declaration before any Git initialization. Do not initialize anything in this umbrella checkout. The script must use nonrecursive, recorded-gitlink initialization, never remote/recursive updates. Keep build/test/package mode unchanged and do not reintroduce automatic MSBuild initialization.

Use existing script test conventions to verify allowed standalone dispatch and refusal paths using isolated fake Git/workspace fixtures, without actual network/submodule initialization. Add the new script to the story-owned pending inventory and regenerate only pending evidence. Preserve all prior fixture and review corrections. No staging/commit/push/dependency/gitlink updates by the agent; parent owns those. Report narrow checks.

## Hosted-hook Review Triage Log

- Blind1 — low, rejected: URL rewriting could change the transport location, but the recorded gitlink SHA independently authenticates the exact repository content and is checked after checkout. A mirror containing that same commit does not change the selected metadata; rewrite isolation adds configuration machinery for an unencountered runner state.
- Blind2 — low, patch: a preexisting checkout could retain modified project files at the recorded HEAD. Reject a dirty selected Memories worktree before reporting preparation complete and cover the refusal.
- Blind3 — low, rejected: launch profile validity is exercised by the actual Aspire topology tests, while the preparation hook supplies the missing project checkout. Adding a parallel profile validator duplicates SDK/test behavior for a nonexistent profile defect.
- Blind4 — false: the fixture asserts the exact nonrecursive recorded-checkout command and post-update SHA. A local update=none either leaves a different HEAD/missing project and fails the existing postconditions, or already has the exact required metadata. No broken success or nested initialization was demonstrated; actual nested-checkout test setup is unnecessary for this narrow command contract.
- Blind5 — low, patch: current operations documentation still attributes preparation to the removed MSBuild target. Update it to the explicit standalone workflow hook and its refusal boundary.
- Edge-case layer — no findings; named claims headings were unavailable, so its claim comparison was limited while code/caller tracing completed.
- Verification-gap layer — no gaps found; the normal specialized gate discovers the Python suite, and the immutable reusable workflow invokes the configured hook before restore/build.

### Hosted-hook patch verification

The final standalone hook passed26 isolated guard cases, Bash syntax, pending-inventory reproducibility and34 existing built workflow/release contract cases. It rejects a dirty selected dependency and current docs name the workflow boundary. It was committed/pushed as11aea2162ce42b7ce1e110e4dde4cae94aa6f2db, including the Builds51939a70bf87a769cbab3735c3fea248738d6423 declaration without child initialization. Fresh CI41338377780 is active; no local nested checkout was created. Full Release/package IntegrationTests with real public Memories3.0.15 passed853/853, zero skips.

Hosted CI41338377780 completed successfully for11aea2162ce42b7ce1e110e4dde4cae94aa6f2db. All main, specialized, accessibility and E2E jobs passed.
