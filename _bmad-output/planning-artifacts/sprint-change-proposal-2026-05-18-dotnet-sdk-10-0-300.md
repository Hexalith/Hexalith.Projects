# Sprint Change Proposal - .NET SDK 10.0.300 Baseline

## 1. Issue Summary

**Trigger:** Jerome requested a Correct Course pass to use .NET SDK `10.0.300` across `D:\Hexalith.Projects`.

**Problem Statement:** The umbrella workspace and active Hexalith modules still document and pin the .NET SDK baseline as `10.0.103`, while the local environment now has SDK `10.0.300` installed. The current state creates SDK drift between desired local execution, module `global.json` pins, CI setup, project-context facts, and planning/implementation artifacts.

**Evidence:**

- `dotnet --list-sdks` reports `10.0.300` installed.
- The module `global.json` files in `Hexalith.EventStore`, `Hexalith.Folders`, `Hexalith.FrontComposer`, `Hexalith.Parties`, and `Hexalith.Tenants` pin `"version": "10.0.103"` with `"rollForward": "latestPatch"`.
- Root and module project-context files still instruct agents to use SDK `10.0.103`.
- `Hexalith.Commons\hexalith-build.yml` still configures `actions/setup-dotnet` with `dotnet-version: 9.0.x`.
- FrontComposer CI workflows use `dotnet-version: '10.0.x'`, which is compatible with .NET 10 but less deterministic than the requested `10.0.300` baseline.

**Mode:** Batch proposal.

**Approval:** Approved by Jerome on 2026-05-18. Implemented as a direct adjustment using exact SDK `10.0.300` for active pins and CI setup.

## 2. Impact Analysis

### Epic Impact

| Area | Impact |
| --- | --- |
| EventStore | Affects runtime/build foundation and release validation stories that reference SDK `10.0.103`. No product behavior changes expected. |
| Folders | Affects repository scaffolding/build configuration assumptions. No domain scope change expected. |
| FrontComposer | Affects project scaffolding, CI, IDE parity evidence, and release certification references. Tests that assert the IDE parity matrix SDK value may need updates. |
| Parties | Affects project scaffolding, architecture notes, README/getting-started prerequisites, and implementation stories that restate SDK `10.0.103`. |
| Tenants | Affects architecture, Epic 1 scaffolding acceptance criteria, and implementation artifacts that explicitly require SDK `10.0.103`. |
| Commons | No `global.json` found, but the build workflow currently targets `9.0.x`; this should be corrected to the workspace baseline. |
| Conversations | No `global.json` found in the current discovery pass. Planning research references sibling SDK pins and should avoid restating stale patch versions unless needed. |

### Artifact Conflicts

| Artifact Type | Conflict | Required Adjustment |
| --- | --- | --- |
| Runtime config | Five module `global.json` files pin `10.0.103`. | Update pins to `10.0.300`; preserve `rollForward: latestPatch`. |
| CI | Commons workflow uses `9.0.x`; FrontComposer workflows use broad `10.0.x`; other workflows rely on `global.json` auto-detection. | Align Commons to .NET 10.0.300 or auto-detected `global.json`; decide whether FrontComposer should remain broad or become exact. |
| Project context | Root and module `project-context.md` files mention `10.0.103`. | Update durable agent facts after runtime/config changes are approved. |
| Planning docs | PRD/epics/architecture/stories contain hard-coded `10.0.103`. | Update current planning docs where they define future work or active acceptance criteria; avoid rewriting historical logs unless they would mislead future implementation. |
| Test/evidence docs | FrontComposer IDE parity matrix/evidence records contain `10.0.103`; at least one test asserts that value. | Update generated or source-of-truth matrix plus tests/evidence if the baseline is part of current acceptance. |

### Technical Impact

- Expected implementation is mostly configuration and documentation.
- No target framework change: projects stay on `net10.0`.
- No package-version bump is implied.
- Verification should focus on SDK selection, restore/build/test behavior, and CI determinism.
- Because this is a submodule umbrella repo, implementation must happen inside each affected root-level submodule, without recursive nested-submodule updates.

## 3. Recommended Approach

**Recommendation:** Direct Adjustment, classified as **Moderate**.

**Rationale:** The requested change is a baseline/platform alignment. It does not require rollback, PRD MVP reduction, or epic resequencing. However, it crosses multiple submodule repositories, CI files, planning artifacts, and tests/evidence, so it should be handled as a coordinated backlog/configuration change rather than a casual search-and-replace.

**Effort Estimate:** Medium.

**Risk Level:** Medium.

Primary risks:

- Missing one `global.json` or CI lane and leaving a split SDK baseline.
- Updating historical BMad implementation records too broadly and muddying audit history.
- Making FrontComposer IDE parity evidence inconsistent with tests.
- CI runner images may not have SDK `10.0.300` preinstalled unless `setup-dotnet` installs it explicitly or global.json auto-detection is used.

## 4. Detailed Change Proposals

### Runtime Configuration

Affected files:

- `Hexalith.EventStore\global.json`
- `Hexalith.Folders\global.json`
- `Hexalith.FrontComposer\global.json`
- `Hexalith.Parties\global.json`
- `Hexalith.Tenants\global.json`

OLD:

```json
{
  "sdk": {
    "version": "10.0.103",
    "rollForward": "latestPatch"
  }
}
```

NEW:

```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestPatch"
  }
}
```

Rationale: Makes SDK `10.0.300` the explicit module build baseline while keeping patch roll-forward behavior.

### CI and Build Workflows

File: `Hexalith.Commons\hexalith-build.yml`

OLD:

```yaml
dotnet-version: 9.0.x
```

NEW:

```yaml
dotnet-version: 10.0.300
```

Rationale: Commons projects target `net10.0`; the workflow should install a compatible .NET 10 SDK baseline.

Files: FrontComposer workflows with `dotnet-version: '10.0.x'`

Options:

- Keep `10.0.x` if the module `global.json` pin is considered authoritative.
- Change to `10.0.300` if CI must be exact and reproducible.

Recommended: change exact CI setup to `10.0.300` for release and certification lanes; leave exploratory/nightly lanes at `10.0.x` only if intentional drift detection is desired and documented.

### Durable Agent Context

Affected files include:

- `_bmad-output\project-context.md`
- `Hexalith.EventStore\_bmad-output\project-context.md`
- `Hexalith.FrontComposer\_bmad-output\project-context.md`
- `Hexalith.Parties\_bmad-output\project-context.md`
- `Hexalith.Conversations\_bmad-output\project-context.md`

OLD:

```md
.NET SDK `10.0.103`
```

NEW:

```md
.NET SDK `10.0.300`
```

Rationale: Keeps future agent runs from reintroducing the older SDK baseline.

### Planning Artifacts

Affected sections:

- Architecture technology stack tables that define the current SDK baseline.
- Epic/story acceptance criteria for project scaffolding or CI runtime validation.
- Current implementation stories that instruct future developers to use SDK `10.0.103`.

Change pattern:

OLD:

```md
SDK 10.0.103
```

NEW:

```md
SDK 10.0.300
```

Rationale: Update active source-of-truth planning instructions. Historical changelog lines and completed dev logs should remain unchanged unless they are used as current implementation guidance.

### FrontComposer IDE Parity

Affected files include:

- `Hexalith.FrontComposer\docs\ide-parity-matrix.md`
- `Hexalith.FrontComposer\docs\ide-parity-matrix.json`
- `Hexalith.FrontComposer\tests\Hexalith.FrontComposer.SourceTools.Tests\IdeParity\IdeParityMatrixContractTests.cs`
- Current evidence JSON files under `Hexalith.FrontComposer\artifacts\ide-parity\evidence`

OLD:

```json
"dotnetSdk": "10.0.103"
```

NEW:

```json
"dotnetSdk": "10.0.300"
```

Rationale: The test suite asserts the IDE parity SDK value, so the source, test, and evidence need to move together.

## 5. Checklist Results

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger came from explicit user request, not a single discovered implementation story. |
| 1.2 Core problem | [x] | SDK baseline drift from `10.0.103` to requested `10.0.300`. |
| 1.3 Evidence | [x] | Local SDKs, `global.json` pins, CI refs, and project-context refs inspected. |
| 2.1 Current epic impact | [!] | Cross-module; no single current epic identified. Treat as shared platform/configuration change. |
| 2.2 Epic-level changes | [x] | No new epic required if handled as backlog/configuration story per affected module. |
| 2.3 Remaining epics | [x] | Future scaffolding, CI, release, and IDE parity epics can inherit the new baseline. |
| 2.4 Obsolete epics | [x] | None identified. |
| 2.5 Priority/order | [x] | Should be done before new runtime validation, CI, release certification, or IDE parity stories. |
| 3.1 PRD conflicts | [x] | MVP/product scope unaffected. |
| 3.2 Architecture conflicts | [x] | Architecture technology-stack sections require SDK baseline updates. |
| 3.3 UI/UX conflicts | [N/A] | No user-facing UX behavior affected. |
| 3.4 Other artifacts | [x] | CI, project-context, tests, and evidence artifacts affected. |
| 4.1 Direct adjustment | [x] | Viable; medium effort, medium risk. |
| 4.2 Rollback | [N/A] | No rollback needed. |
| 4.3 MVP review | [N/A] | MVP scope unchanged. |
| 4.4 Path selected | [x] | Direct Adjustment. |
| 5.1 Issue summary | [x] | Included above. |
| 5.2 Impact summary | [x] | Included above. |
| 5.3 Recommendation | [x] | Included above. |
| 5.4 Action plan | [x] | Included below. |
| 5.5 Handoff plan | [x] | Included below. |
| 6.1 Review completion | [x] | Proposal reviewed and implementation completed. |
| 6.2 Proposal accuracy | [x] | Active SDK pins, CI, current guidance, and FrontComposer parity references were updated. |
| 6.3 User approval | [x] | Jerome approved implementation on 2026-05-18. |
| 6.4 Sprint status update | [N/A] | No epics or stories were added, removed, renumbered, or resequenced. |
| 6.5 Handoff confirmation | [x] | Handoff is direct Developer implementation with verification notes below. |

## 6. Implementation Handoff

**Scope Classification:** Moderate.

**Recommended owners:**

- Developer agent: implement config, CI, project-context, and test/evidence updates.
- Product Owner / Developer: decide whether to create one workspace-level backlog item or per-module stories in the affected `sprint-status.yaml` files.

**Implementation order:**

1. Update module `global.json` pins to `10.0.300`.
2. Update CI setup to install or auto-detect the new baseline.
3. Update current architecture/planning/project-context instructions that define the SDK baseline.
4. Update FrontComposer IDE parity source, tests, and evidence together.
5. Run verification per affected module.
6. Update sprint-status only after the approved backlog structure is known.

**Verification criteria:**

- `dotnet --version` from each affected module root resolves to `10.0.300`.
- `dotnet build` or targeted solution build succeeds for each affected module.
- FrontComposer IDE parity tests pass after matrix updates.
- CI workflow SDK setup is consistent with the new baseline.
- No nested submodules are initialized or updated recursively.

## 7. Implementation Result

Implemented changes:

- Updated SDK pins in `Hexalith.EventStore`, `Hexalith.Folders`, `Hexalith.FrontComposer`, `Hexalith.Parties`, and `Hexalith.Tenants` from `10.0.103` to `10.0.300`.
- Updated Commons and FrontComposer CI SDK setup from `9.0.x` / `10.0.x` to exact `10.0.300`.
- Initialized only the root-level `Hexalith.Builds` submodule inside `Hexalith.Commons` to satisfy the Commons build prerequisite, then updated its reusable `initialize-dotnet` action to exact `10.0.300`.
- Updated current project-context, architecture, README/getting-started, troubleshooting, deployment, and FrontComposer IDE parity guidance that defined the active SDK baseline.
- Updated FrontComposer IDE parity matrix, test assertion, revalidation job minimum, and evidence manifests from `10.0.103` to `10.0.300`.

Verification:

- `dotnet --version` resolves to `10.0.300` from each affected module root: EventStore, Folders, FrontComposer, Parties, and Tenants.
- `dotnet --version` resolves to `10.0.300` from Commons after the SDK update.
- `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --filter FullyQualifiedName~IdeParity --no-restore --verbosity normal` completed with build success, 0 warnings, 0 errors. The runner did not print a test execution summary for the filter.
- `dotnet restore Hexalith.Commons.sln` followed by `dotnet build Hexalith.Commons.sln --no-restore --configuration Release --verbosity minimal` succeeded with 0 warnings and 0 errors.
- `dotnet build Hexalith.EventStore.slnx --no-restore --configuration Release --verbosity minimal` was blocked by missing nested `Hexalith.Tenants` project paths inside the EventStore repo and missing restore assets. Nested submodules were not initialized because the workspace rule forbids nested submodule initialization unless explicitly requested.
