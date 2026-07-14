---
title: "Sprint Change Proposal: CreateProject metadataClass Enforcement"
status: approved
created: 2026-07-14
approved: 2026-07-14
approved_by: Jerome
project: Hexalith.Projects
scope: minor
mode: incremental
---

# Sprint Change Proposal: CreateProject `metadataClass` Enforcement

## 1. Issue Summary

Story 4.5 exposed a pre-existing API-contract consistency gap in the Story 1.4
`CreateProject` endpoint. The shared OpenAPI `ProjectMetadata` schema requires both
`displayName` and `metadataClass`, and proposal confirmation now validates that contract.
The original CreateProject server path, however, deserializes `metadataClass` as nullable and
does not validate either its presence or its value before submitting the command.

The gap is observable in the current implementation:

- `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` declares
  `ProjectMetadata.metadataClass` required and constrains it to `SensitiveMetadataTier`.
- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs` reads but ignores the field.
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs` proves the historical
  unversioned name-only body is accepted, but has no missing/invalid-classification coverage for
  the canonical `projectMetadata` body.
- Story 4.5 and the Epic 4 retrospective explicitly carry the inconsistency forward for resolution.

This is an API-boundary defect discovered during implementation, compounded by a compatibility
constraint: FR-1, FR-19, and Story 1.4 state that the Project name is the only required user input.
The correction must therefore enforce the canonical OpenAPI request without silently removing the
historical name-only compatibility path.

## 2. Impact Analysis

### Epic Impact

- Epic 4 and Story 4.5 remain complete. Proposal confirmation correctly enforces the shared
  metadata classification contract.
- Stories 1.4 and Epic 1 remain closed. The historical name-only behavior is preserved.
- No epic must be added, removed, reopened, renumbered, or resequenced.
- The existing Epic 5 carry-forward action is the implementation vehicle and remains release-priority
  work until the focused contract/server evidence is green.

### Story Impact

No story acceptance criteria are rewritten. The existing sprint action is narrowed into an
executable compatibility contract:

- Canonical requests that submit `projectMetadata` require a valid `metadataClass`.
- Missing, blank, null, or unsupported classification is rejected before command submission.
- Historical unversioned name-only requests remain accepted.

### Artifact Conflicts

- **PRD:** No scope change. `metadataClass` is integration-supplied safety metadata rather than an
  additional user-authored field; the name remains the only required user input.
- **Architecture:** No component, persistence, event, projection, Dapr, or topology change. The
  correction is confined to HTTP-boundary validation.
- **OpenAPI:** No schema change. The existing required-field and enum contract is retained and
  explicitly pinned by tests.
- **UX:** No UI flow, form, accessibility, responsive, or visual change.
- **Generated client:** No regeneration is expected because the spine remains unchanged.

### Technical Impact

Affected production files:

- `src/Hexalith.Projects.Server/ProjectsDomainServiceEndpoints.cs`
- `src/Hexalith.Projects.Server/Queries/ProposeNewProjectEndpoint.cs`
- New server-owned `SensitiveMetadataTierValidator.cs`

Affected test files:

- `tests/Hexalith.Projects.Contracts.Tests/OpenApi/OpenApiContractSpineTests.cs`
- `tests/Hexalith.Projects.Server.Tests/CreateProjectEndpointTests.cs`

Affected tracking artifact after implementation:

- `_bmad-output/implementation-artifacts/sprint-status.yaml`

No deployment, infrastructure-as-code, monitoring, telemetry, event-catalog, projection-catalog,
or CI-pipeline changes are required.

## 3. Recommended Approach

### Selected Path: Direct Adjustment

Enforce the existing OpenAPI requirement whenever a CreateProject request submits
`projectMetadata`. Extract the current proposal-confirmation classification check into one
server-owned validator and reuse it from both endpoints. Preserve the historical request that
supplies only the Project name and does not submit `projectMetadata`.

This path is preferred because it:

- makes the documented canonical request fail or succeed consistently across OpenAPI and server;
- avoids weakening proposal-confirmation validation;
- preserves the existing name-only compatibility behavior;
- avoids schema splitting, generated-client regeneration, and fingerprint churn;
- creates one server validation source instead of duplicating the four allowed values.

### Alternatives Considered

**Rollback:** Not viable. Reverting Story 4.5 enforcement would recreate the same inconsistency on
proposal confirmation and weaken an implemented safety boundary.

**Schema relaxation / MVP review:** Not recommended. `ProjectMetadata` is shared by CreateProject and
proposal confirmation. Relaxing it would either make the proposal-confirmation OpenAPI inaccurate or
require new split schemas, generated-client regeneration, and a broader compatibility review. MVP
goals do not otherwise need revision.

### Estimate and Risk

- **Effort:** Low, approximately 2–4 development hours including focused verification.
- **Risk:** Low. Canonical requests that omitted or supplied an unknown required value are already
  non-conforming. The historical compatibility request is explicitly retained.
- **Timeline impact:** No epic resequencing and no material delivery delay. Complete before release
  handoff as a bounded hardening task.

## 4. Detailed Change Proposals

### 4.1 Sprint Action Scope

**OLD**

> Resolve or explicitly scope the pre-existing CreateProject metadataClass required-field
> enforcement gap with OpenAPI/server tests.

**NEW**

> Enforce the OpenAPI-required `ProjectMetadata.metadataClass` for CreateProject requests that
> submit `projectMetadata`; reject missing or unsupported values with metadata-only
> `400 ValidationFailure`; preserve the historical unversioned name-only compatibility request;
> add focused OpenAPI and server regression tests; do not change the OpenAPI schema or generated
> client.

**Rationale:** Replaces an ambiguous alternative with an executable compatibility and evidence
contract.

### 4.2 Server Boundary Validation

**OLD**

`CreateProjectAsync` reads `ProjectMetadata.MetadataClass` but never validates it. Proposal
confirmation owns a separate private four-value validator.

**NEW**

After authorization and JSON parsing, but before command construction/submission, reject a submitted
`projectMetadata` object whose `metadataClass` is not one of:

- `public_metadata`
- `tenant_sensitive`
- `credential_sensitive`
- `secret`

Return metadata-only `400 ValidationFailure` with
`details.rejectedField = "projectMetadata.metadataClass"`. Do not echo the submitted value and do
not invoke `IProjectCommandSubmitter`.

Extract the classification check into a focused server-owned `SensitiveMetadataTierValidator` and
reuse it from CreateProject and proposal confirmation. Proposal-confirmation behavior must remain
unchanged.

**Rationale:** Enforces the existing contract at the correct boundary while preventing endpoint
vocabulary drift.

### 4.3 OpenAPI Contract Test

**OLD**

`Spine_CreateProject_IsTheRealCommandAsyncMutation` verifies the CreateProject request schema
reference but does not inspect the nested `ProjectMetadata` requirements.

**NEW**

Extend the test to prove:

- `CreateProjectRequest` requires `requestSchemaVersion` and `projectMetadata`;
- `ProjectMetadata` requires `displayName` and `metadataClass`;
- `metadataClass` references `SensitiveMetadataTier`;
- the tier enum contains exactly the four supported wire values.

**Rationale:** Makes accidental contract relaxation or vocabulary drift a focused test failure.

### 4.4 Server Regression Tests

Retain the two positive cases:

- Historical unversioned name-only request returns `202 AcceptedCommand`.
- Canonical classified request returns `202 AcceptedCommand`.

Add two negative cases:

- Canonical `projectMetadata` without `metadataClass` returns `400` and submits no command.
- Canonical `projectMetadata` with an unsupported classification returns `400` and submits no
  command.

Both negative responses must identify only `projectMetadata.metadataClass`, omit the submitted
value, and pass the reusable metadata-leakage assertion.

**Rationale:** The four-case matrix proves both strict canonical behavior and preserved compatibility
at the actual HTTP boundary.

## 5. Implementation Handoff

### Scope Classification

**Minor** — direct implementation by the Developer agent. No backlog reorganization, product
replanning, or architecture escalation is required.

### Developer Responsibilities

1. Add the focused server-owned classification validator in its own C# file.
2. Reuse it from CreateProject and proposal confirmation without changing the latter's behavior.
3. Add the approved OpenAPI and server regression assertions.
4. Preserve authorization-before-parsing, metadata-only errors, and no-command-on-invalid-input.
5. Run focused contract and server tests, the OpenAPI fingerprint gate, and the Release solution
   build with warnings treated as errors.
6. Update the sprint action to `done` only after evidence is green, recording the exact commands and
   results.

### Success Criteria

- Canonical valid request: `202`; one CreateProject command submitted.
- Canonical request missing `metadataClass`: `400`; no command submitted.
- Canonical request with unsupported `metadataClass`: `400`; no command submitted.
- Historical unversioned name-only request: remains `202`.
- Error response contains only the rejected field name and passes leakage checks.
- OpenAPI test pins required-field and enum semantics.
- Focused Contracts/OpenAPI and Server test projects pass.
- OpenAPI fingerprint gate passes without schema drift.
- Release solution build passes with zero warnings/errors.
- No OpenAPI schema, generated-client, domain, event, projection, infrastructure, UX, package, or
  submodule changes are introduced.

### Handoff Recipient

Developer agent (Amelia role) for direct implementation and verification. Product Owner, Product
Manager, and Architect escalation are unnecessary unless the implementation reveals an undocumented
consumer dependency that requires the canonical missing-field request to remain accepted.

### Approval and Handoff Log

- **Approval:** Jerome explicitly approved the complete proposal on 2026-07-14.
- **Scope:** Minor.
- **Routed to:** Developer agent (Amelia role).
- **Deliverables:** Approved detailed edits, focused OpenAPI/server test matrix, verification gates,
  and sprint-action closure criteria.
- **Implementation state:** Not yet implemented; the existing sprint action remains open until all
  success criteria pass and evidence is recorded.

## Checklist Status at Proposal Review

- [x] Trigger, problem, and evidence documented.
- [x] Epic and remaining-work impact assessed.
- [x] PRD, architecture, UX, API, test, and secondary-artifact impacts assessed.
- [x] Direct adjustment selected over rollback or MVP/schema review.
- [x] Detailed changes approved incrementally by the user.
- [x] Implementation handoff and success criteria defined.
- [x] Final proposal explicitly approved by Jerome.
- [N/A] Sprint epic/story entries require no addition, removal, renumbering, or resequencing.
