# Sprint Change Proposal: Centralize Projects Package Versions in Hexalith.Builds

**Date:** 2026-07-06  
**Project:** Hexalith.Projects  
**Requested by:** Jerome  
**Workflow mode:** Batch  
**Status:** Approved and implemented

## 1. Issue Summary

The change trigger is: use the `Hexalith.Builds` package-version file for `Hexalith.Projects` the same way `Hexalith.Tenants` does.

Current evidence:

- `Hexalith.Tenants/Directory.Packages.props` imports `references/Hexalith.Builds/Props/Directory.Packages.props` and carries no module-local package pins.
- `Hexalith.Projects/Directory.Packages.props` imports the same shared Builds file but still defines local package versions for `NSwag.MSBuild` and `Fluxor.Blazor.Web`.
- `references/Hexalith.Builds/Props/Directory.Packages.props` already defines `NSwag.MSBuild` and did not define `Fluxor.Blazor.Web` at implementation time.
- Projects uses those packages through versionless `PackageReference` entries in:
  - `src/Hexalith.Projects.Client/Hexalith.Projects.Client.csproj`
  - `src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj`

Core problem: package-version ownership is split between Projects and Builds. That conflicts with the desired Tenants-style pattern where reusable package versions are centralized in `Hexalith.Builds`.

## 2. Impact Analysis

### Epic Impact

Affected epic: **Epic 1: Project Workspace Foundation**.

Affected completed stories:

- **Story 1.1: Module scaffold & build/CI wiring** because it owns root `Directory.Packages.props` and central package-management wiring.
- **Story 1.3: OpenAPI Contract Spine + generated typed client** because it introduced the `NSwag.MSBuild` package dependency.
- **Story 5.3 / Epic 5 FrontComposer surface work** indirectly depends on `Fluxor.Blazor.Web`, but no story scope or behavior changes are required.

No epic needs to be added, removed, resequenced, or redefined.

### Story Impact

No acceptance criteria need semantic changes. The implementation detail for central package pins should be corrected in code/config:

- Move the two Projects-local package pins into the shared Builds package-version file.
- Simplify Projects `Directory.Packages.props` to an import-only pattern aligned with Tenants.

The completed story records can remain historical; this proposal is the audit artifact for the correction.

### Artifact Conflicts

PRD: no conflict. This is a build-governance correction, not a product requirement change.

Architecture: aligns with the architecture and project-context rules:

- Central package management is required.
- Package versions belong in `Directory.Packages.props`, not inline `PackageReference` entries.
- Shared build/package configuration comes from `Hexalith.Builds`.

UX: no impact.

Implementation artifacts: Story 1.1 and 1.3 are the affected backlog references, but no backlog status update is needed because the change does not add/remove stories.

### Technical Impact

Expected touched files:

- `references/Hexalith.Builds/Props/Directory.Packages.props`
- `Directory.Packages.props`
- `references/Hexalith.FrontComposer/Directory.Packages.props`
- `references/Hexalith.Conversations/Directory.Packages.props`

Expected untouched files:

- Project `.csproj` files should keep versionless `PackageReference` entries.
- Generated `.g.cs` files should not be edited.
- Sprint status should not change unless approval adds a story or formal action item.

Submodule note: `references/Hexalith.Builds` is a root-declared submodule. Implementation should edit that submodule directly and avoid recursive submodule commands.

Validation note: after centralizing package pins in Builds, restore also required removing duplicate local package-version entries in root-declared submodules that import the same Builds file:

- `references/Hexalith.FrontComposer/Directory.Packages.props` no longer defines `Fluxor.Blazor.Web`.
- `references/Hexalith.Conversations/Directory.Packages.props` no longer defines `Microsoft.Playwright`, which was already centralized in Builds before this correction.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The issue is localized to central package-version ownership.
- No PRD, UX, API, domain model, or runtime behavior changes are needed.
- The correction reduces future version drift and brings Projects in line with Tenants.

Effort estimate: Low.

Risk level: Low to Medium. The main risk is that moving package pins into `Hexalith.Builds` affects any repo consuming that shared package file. The versions should therefore be copied exactly from the current Projects pins, with no package upgrades.

Timeline impact: no sprint resequencing required.

Rollback option: revert the two file changes if restore/build fails unexpectedly.

MVP impact: none.

## 4. Detailed Change Proposals

### Build Artifact: `references/Hexalith.Builds/Props/Directory.Packages.props`

Section: shared package versions.

OLD:

```xml
<PackageVersion Include="NSwag.MSBuild" Version="14.7.1" />
<!-- No PackageVersion entry for Fluxor.Blazor.Web -->
```

NEW:

```xml
<PackageVersion Include="Fluxor.Blazor.Web" Version="6.9.0" />
<PackageVersion Include="NSwag.MSBuild" Version="14.7.1" />
```

Rationale: these are reusable build/surface-generation package pins. `NSwag.MSBuild` was already centralized at implementation time; adding `Fluxor.Blazor.Web` lets Projects consume both packages the same way Tenants consumes shared package versions.

### Build Artifact: `Directory.Packages.props`

Section: local package-version entries.

OLD:

```xml
<ItemGroup Label="Client generation (Story 1.3 — OpenAPI Contract Spine + NSwag typed client)">
  <!-- NSwag.MSBuild drives typed-client generation. -->
  <PackageVersion Update="NSwag.MSBuild" Version="14.7.1" />
</ItemGroup>
<ItemGroup Label="FrontComposer UI">
  <PackageVersion Include="Fluxor.Blazor.Web" Version="6.9.0" />
</ItemGroup>
```

NEW:

```xml
<!-- No module-local PackageVersion entries. Shared versions come from Hexalith.Builds. -->
```

Also add Tenants-aligned transitive pinning:

```xml
<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
```

Rationale: this makes Projects follow the Tenants import-only package-version pattern while preserving central package management.

### Verification

After implementation:

```bash
dotnet restore Hexalith.Projects.slnx
dotnet build Hexalith.Projects.slnx --configuration Release
```

If build time is high, the minimum acceptable verification is:

```bash
dotnet restore Hexalith.Projects.slnx
```

and a targeted check confirming `NSwag.MSBuild` and `Fluxor.Blazor.Web` resolve from central package management.

## 5. Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | No active story revealed it; issue is a build-governance correction against completed Story 1.1 / 1.3 work. |
| 1.2 Core problem | [x] | Technical alignment issue: package pins live in Projects instead of the shared Builds package file. |
| 1.3 Evidence | [x] | `rg` confirms Projects had local `NSwag.MSBuild` / `Fluxor.Blazor.Web` package-version entries; Builds already had `NSwag.MSBuild` and lacked `Fluxor.Blazor.Web`; Tenants uses import-only package versions. |
| 2.1 Current epic completion | [x] | Epic 1 remains valid. |
| 2.2 Epic-level changes | [N/A] | No epic scope changes required. |
| 2.3 Remaining epics | [x] | No downstream epic changes required. |
| 2.4 Future epic validity | [x] | No future epics invalidated. |
| 2.5 Priority/order | [N/A] | No resequencing required. |
| 3.1 PRD conflicts | [N/A] | Product requirements unaffected. |
| 3.2 Architecture conflicts | [x] | Change reinforces central package-management architecture. |
| 3.3 UI/UX conflicts | [N/A] | No UX impact. |
| 3.4 Other artifacts | [x] | Build config and shared Builds submodule are impacted. |
| 4.1 Direct adjustment | [x] | Viable; low effort, low-medium shared-config risk. |
| 4.2 Rollback | [N/A] | Rollback not recommended unless restore/build fails. |
| 4.3 MVP review | [N/A] | MVP scope unaffected. |
| 4.4 Recommended path | [x] | Direct Adjustment. |
| 5.1 Issue summary | [x] | Included above. |
| 5.2 Impact summary | [x] | Included above. |
| 5.3 Recommendation | [x] | Included above. |
| 5.4 MVP/action plan | [x] | MVP unaffected; two-file implementation plan. |
| 5.5 Handoff plan | [x] | Developer agent can implement directly after approval. |
| 6.1 Checklist completion | [x] | All applicable items addressed. |
| 6.2 Proposal accuracy | [x] | Proposal grounded in current file inspection. |
| 6.3 User approval | [!] | Pending Jerome approval. |
| 6.4 Sprint status update | [N/A] | No epic/story add/remove/renumber. |
| 6.5 Next steps | [!] | Pending approval to implement. |

## 6. Implementation Handoff

Change scope: **Minor**.

Route to: **Developer agent** for direct implementation after approval.

Responsibilities:

- Add the missing shared `Fluxor.Blazor.Web` package pin to `references/Hexalith.Builds/Props/Directory.Packages.props`.
- Remove the local `NSwag.MSBuild` update and `Fluxor.Blazor.Web` package pin from Projects `Directory.Packages.props`.
- Remove duplicate local package-version entries in FrontComposer and Conversations that conflict with shared Builds pins during root restore.
- Add Tenants-aligned `CentralPackageTransitivePinningEnabled`.
- Run restore/build verification.
- Report whether the Builds submodule has local changes and whether a root submodule pointer update is needed for commit packaging.

Success criteria:

- Projects no longer has module-local `NSwag.MSBuild` or `Fluxor.Blazor.Web` `PackageVersion` entries.
- Builds owns those exact versions.
- Projects `PackageReference` entries remain versionless.
- `dotnet restore Hexalith.Projects.slnx` succeeds.
- Preferably `dotnet build Hexalith.Projects.slnx --configuration Release` succeeds.

## Approval

Approved by Jerome on 2026-07-06 and implemented as a minor direct adjustment.
