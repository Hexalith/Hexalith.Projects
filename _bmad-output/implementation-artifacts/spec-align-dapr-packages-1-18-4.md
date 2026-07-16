---
title: 'Align Dapr package catalog on 1.18.4'
type: 'chore'
created: '2026-07-16'
status: 'draft'
review_loop_iteration: 0
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The shared central package catalog contains an unused `Dapr` entry at `1.17.9` beside the real Dapr .NET SDK package IDs at `1.18.4`. This creates apparent version skew even though current restored Projects and Memories graphs already resolve every Dapr SDK dependency to `1.18.4`.

**Approach:** Remove the invalid, unreferenced bare `Dapr` catalog row and retain `1.18.4` for every active Dapr SDK package declaration. Verify both MSBuild's evaluated central items and restored dependency graphs so the repository has no active Dapr SDK `1.17.9` dependency.

## Boundaries & Constraints

**Always:** Keep project `PackageReference` items versionless; manage Dapr SDK versions through `Directory.Packages.props`; preserve `Dapr.Client`, `Dapr.AspNetCore`, Actors, Generators, Workflow, and Memories AI packages at `1.18.4`; preserve existing unrelated work.

**Ask First:** Ask before changing Dapr CLI/runtime pins, adding or removing a real SDK package, changing `CommunityToolkit.Aspire.Hosting.Dapr`, rewriting historical evidence or generated caches, committing submodule changes, or updating parent submodule pointers.

**Never:** Do not replace the stale row with a nonexistent `Dapr` `1.18.4` package; do not add inline versions or `VersionOverride`; do not initialize nested submodules; do not modify unrelated architecture artifacts.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Shared SDK catalog | Evaluated `PackageVersion` items whose IDs are active Dapr SDK packages | Every version is exactly `1.18.4` | Fail verification on any other version |
| Legacy bare entry | Unreferenced exact package ID `Dapr` at `1.17.9` | Entry is absent and cannot suggest a second SDK line | Do not rename or bump the invalid entry |
| Operational Dapr | CI/runtime/CLI pins such as `1.18.0` | Remains unchanged because NuGet CPM does not manage it | Report it separately from SDK package resolution |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props` -- canonical imported package catalog; owns the stale `Dapr` row and the active `Dapr.*` 1.18.4 pins.
- `Directory.Packages.props` -- root CPM entry point importing the Builds catalog with transitive pinning enabled.
- `references/Hexalith.Memories/Directory.Packages.props` -- local central additions for `Dapr.AI` and `Dapr.AI.Microsoft.Extensions`, already at 1.18.4.
- `src/Hexalith.Projects.Infrastructure/Hexalith.Projects.Infrastructure.csproj` -- representative versionless direct references to `Dapr.Client` and `Dapr.AspNetCore`.
- `.github/workflows/ci.yml` -- separate Dapr CLI/runtime input, intentionally outside this package-only correction.

## Tasks & Acceptance

**Execution:**
- [ ] `references/Hexalith.Builds/Props/Directory.Packages.props` -- delete the unused `<PackageVersion Include="Dapr" Version="1.17.9" />` row while leaving all real Dapr SDK pins at `1.18.4` -- remove the false second version from the canonical catalog without inventing a package.

**Acceptance Criteria:**
- Given a representative Projects project imports the shared central catalog, when MSBuild evaluates `PackageVersion`, then no exact `Dapr` item exists and every active shared `Dapr.*` SDK item is `1.18.4`.
- Given Projects and Memories assets are restored, when direct and transitive package lists are inspected, then all resolved Dapr SDK packages are `1.18.4` and none resolve to `1.17.9`.
- Given the change is package-catalog-only, when the diff is reviewed, then Dapr CLI/runtime pins, Aspire integration versions, project references, historical evidence, generated outputs, and unrelated user changes are untouched.

## Spec Change Log

## Design Notes

NuGet central package management is keyed by exact package ID. It does not treat `Dapr`, `Dapr.Client`, and `Dapr.AspNetCore` as one version family, and it does not validate an unused catalog item during restore. The stale bare `Dapr` row therefore appeared in MSBuild's catalog but never entered `project.assets.json`; deleting it is safer than changing it to a version for a package that does not exist. Dapr CLI and runtime releases are configured outside NuGet and remain a separately tested compatibility tuple.

## Verification

**Commands:**
- `dotnet msbuild src/Hexalith.Projects.Infrastructure/Hexalith.Projects.Infrastructure.csproj -nologo -getItem:PackageVersion -getItem:PackageReference` -- expected: active Dapr package versions are 1.18.4 and no exact `Dapr` item is present.
- `dotnet restore Hexalith.Projects.CI.slnx -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons` -- expected: restore succeeds without Dapr downgrade/conflict diagnostics.
- `dotnet build Hexalith.Projects.CI.slnx --configuration Release --no-restore -warnaserror -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/projects/references/Hexalith.Commons` -- expected: build succeeds with zero warnings and errors, or the repository fallback ladder records any unrelated broad-gate blocker.
- `dotnet list references/Hexalith.Memories/src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj package --include-transitive --no-restore` -- expected: every listed Dapr SDK package resolves to 1.18.4.
- `git diff --check` -- expected: no whitespace errors in intended changes.
