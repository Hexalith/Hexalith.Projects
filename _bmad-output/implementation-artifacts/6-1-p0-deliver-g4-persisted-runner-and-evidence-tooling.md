---
work_package_id: 6.1-P0
story_key: 6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling
artifact_kind: external-prerequisite-story-handoff
created: 2026-07-17
source_action_status: open
status: blocked
blocked_by:
  - Builds-owner repository selection
  - owner-approved repository-local issue or story
  - owner-approved baseline revision and rollback pin
repository_authority: "Builds/platform tooling repository selected by the Builds owner"
owners: [Builds Owner, Platform Owner, Test Architect]
parallel_with: [6.1-P1]
unblocks: [6.1-P4]
traceability:
  requirements: [fr-2, fr-5]
  nfrs: [nfr-11]
  supporting_nfrs: [nfr-1, nfr-5, nfr-10]
  architecture: [AD-25, AD-30]
  findings: [TEST-001]
  enables_evidence_rows:
    - release-authenticated-persisted-boundary
    - release-cross-tenant-isolation
    - release-restart-concurrency
    - release-privacy
    - release-performance
    - release-smoke
    - release-rollback
target_date: uncommitted
estimate: L
risk: high evidence-chain risk
projects_baseline_commit: ec447e4
builds_observed_commit: 01e48ee
---

# Story 6.1-P0: Deliver the G-4 Persisted Runner and Evidence Tooling

Status: blocked

<!--
This is a portable handoff for the external 6.1-P0 enablement work package. It is not an
additional Hexalith.Projects user-value story and does not change the approved 33-story backlog.
It must be adopted as an owner-approved repository-local issue or story before implementation.
-->

## Story

As a Hexalith module developer and Test Architect,
I want pinned platform tools that compose the supported persisted multi-module runtime and validate deterministic evidence,
so that Story 6.1 and later work can prove supported-path behavior without Projects-owned topology, copied platform code, or hand-authored pass claims.

## Acceptance Criteria

1. **Owner authority and immutable delivery baseline.** Given this handoff, when 6.1-P0 is accepted for implementation, then the Builds owner records the authoritative repository, the accountable Builds/Platform/Test owners, an owner-approved baseline revision, final package IDs and versions, the manifest and evidence schema versions, the release channel, and the prior known-good rollback pin in a repository-local issue or story. If runner and evidence ownership are split, each repository has a coordinated local work item and their revision relationship is explicit. Until that record exists, this handoff remains `blocked`; neither Projects nor an implementing agent may select the repository or self-approve the gate.

2. **Pinned, independently consumable .NET tools.** Given a clean consumer checkout with no authoritative `bin` or `obj` output, when `dotnet tool restore` runs against a checked-in `.config/dotnet-tools.json`, then exact published versions of tools exposing `hexalith-module` and `hexalith-evidence` restore successfully and the documented `dotnet tool run` commands work without source-tree scripts, globally installed tools, or consumer-side platform code. The same public command contract fronts Debug/source qualification and Release/package qualification; neither mode becomes a second supported interface.

3. **Strict versioned module manifest.** Given a checked-in, non-secret manifest, when any `run`, `down`, or `test` command starts, then the tool validates the approved manifest schema before starting Aspire or changing runtime state. A valid consumer manifest may name one or more modules and names its descriptor assembly, sibling dependencies, deterministic domain/application/resource identifiers, and known fixture profiles using canonical repository-relative paths; the P0 qualification fixture specifically declares at least two modules. Unknown schema versions or fields, duplicate or nondeterministic identifiers, missing assemblies/profiles, malformed dependencies, absolute paths, `..` escape, unresolved placeholders, and secret-bearing values fail closed with stable diagnostics.

4. **Runner-owned composition and lifecycle.** Given a valid manifest and available prerequisites, when `hexalith-module run`, `down`, or `test` executes, then the runner owns EventStore, Dapr, identity and generated development-secret injection, FrontComposer, dynamic ports/endpoints, health/readiness, telemetry, Aspire lifecycle, run-state tracking, cancellation, and bounded cleanup. It changes no consumer or sibling repository. Each invocation operates only on resources bearing its run identity; `down` is idempotent; failure and cancellation attempt safe cleanup while retaining failure evidence.

5. **Real persisted multi-module control.** Given the approved persisted fixture, when the full profile runs, then the supported platform composes at least two modules and proves an authenticated write, the expected persisted event and projection/read state, stop, restart, rehydrated read, expected sequence/revision, retry/idempotency behavior, and two-instance access. A run-unique namespace prevents stale prior state from satisfying the assertions. Missing event state, missing projection/read state, wrong sequence, stale state, cross-Tenant state, or an in-memory/fake store is non-passing. This control proves the runner; it does not implement Story 6.1 reads.

6. **Portable profile and test-platform orchestration.** Given a selected profile and optional filter, when `hexalith-module test` invokes consumer tests, then the packaged runner supports pure-domain, host-contract/descriptor, persisted-boundary, restart, two-instance, and authenticated browser, CLI, and MCP profile classes with stable endpoint/identity handoff and artifact contracts. Product stories supply their own profile assertions and pass evidence; P0 proves the runner can orchestrate each class. The test command supports the repository-approved VSTest and Microsoft Testing Platform/xUnit v3 lanes, captures the actual process result and test report, and never treats missing/invalid reports, zero matching tests, all-skipped tests, unavailable prerequisites, a failed test step, or failed assertions as passed. Source and package modes use equivalent semantic profiles and fixture inputs.

7. **Deterministic evidence envelope.** Given a completed or failed invocation, when evidence is emitted, then a versioned machine-readable artifact records at least the evidence schema, run ID, start/end timestamps, repository revision and dirty-state marker, SDK/OS, tool and package versions, manifest/profile/fixture identities and hashes, exact command, module and topology pins, phase results, persisted assertions and expected sequences, test counts, report and artifact paths/hashes, final status, stable diagnostic rule IDs, and failure category. Ordering, UTF-8 encoding, newline convention, and repo-relative paths are canonical. Volatile timestamps, dynamic ports, and generated run IDs are explicitly identified so equal semantic inputs can be compared deterministically.

8. **Execution failure is distinguishable from evidence failure.** Given a runner, test, parser, or policy failure, when the command terminates, then a documented stable machine contract distinguishes usage/manifest, environment/prerequisite, topology/lifecycle, product/test, persisted-state, and evidence/schema/policy failures. Diagnostics contain a stable category and rule ID, and execution failure is distinct from evidence-validation failure through documented nonzero outcomes. Phase-aware short-circuiting preserves the causal failure; an arbitrary numeric-precedence rule cannot overwrite it. Partial artifacts never claim `passed`.

9. **Fail-closed AD-30 readiness validator.** Given a candidate `hexalith.readiness-evidence.v1` YAML matrix, when `hexalith-evidence validate` runs, then it parses YAML with duplicate-key detection, rejects unsupported schemas before business-rule validation, resolves row defaults to effective rows, and validates the canonical `key` identity. It rejects missing/duplicate keys, unresolved placeholders, missing owner/version/dependencies or gates/command/artifact-path/estimate/status/release disposition, incomplete FR/NFR/P1/P2/release coverage, failed critical evidence, unexplained critical skips, unavailable environments represented as passed, and Markdown/YAML row-identity drift. Pending, blocked, or not-verified rows may reference future artifact paths; actual artifact existence, readability, schema, and hash are mandatory when a row claims executed or passing evidence. Diagnostics are deterministically sorted and expose source file, row key, rule ID, field/location, and actionable hint in human and JSON modes.

10. **Status vocabulary is reconciled, not invented.** Given the current readiness matrix declares five legend values but uses `blocked` on its terminal release row, when the validator contract is approved, then the Product/Architecture/Test owners either add and define that status for the applicable category or correct the canonical matrix before claiming the first valid sample. The tool cannot silently accept an undeclared value or mutate the input. P0 supplies a separate conforming positive sample; it does not rewrite Projects planning evidence without its own authority.

11. **Blocking positive and negative controls.** Given the packaged commands and curated fixtures, when CI runs the P0 contract suite, then an approved positive sample passes and negative controls fail predictably with the expected stable category/rule IDs. Controls include unsupported manifest/evidence schema, absolute or escaping paths, duplicate IDs/YAML keys, secret or placeholder content, tampered tool/version pins, absent event or projection, stale prior-run or cross-Tenant state, missing/invalid report, zero/all-skipped tests, missing coverage/owner/version/command/artifact path, a missing or invalid actual artifact for a row claiming execution/pass, a critical failed or unexplained-skipped row, and `passed` on an unavailable environment. The controls invoke the packed tools and are not skipped or quarantined.

12. **Metadata-only security evidence.** Given any success or failure path, when logs, run-state, telemetry, reports, and evidence are inspected, then they contain only the approved metadata contract and no bearer token, credential, secret, raw environment dump, source payload, transcript, prompt, user content, or protected Tenant/resource detail. The runner owns development credential creation and injection, redacts command output before retention, and never serializes secret values into a manifest or evidence artifact.

13. **Clean-checkout Debug and CI package-mode parity.** Given a clean checkout at the accepted revisions, when the qualification workflow runs, then it builds and tests with the pinned SDK, packs the tools, restores them from a local/CI package source, executes the valid and negative fixtures, and proves the same supported commands in Debug/source and Release/package modes without stale artifacts. The workflow records exact tool/package revisions, supports the repository's approved VSTest/MTP reporting paths, and fails closed under G-6 when any required lane or prerequisite is unavailable.

14. **Rollback and consumer handoff.** Given an unsuccessful rollout or incompatible schema, when rollback is invoked, then the documented procedure runs idempotent `down`, retains failed evidence, restores the prior exact local-tool/package pin, reverts the consumer manifest/schema only through an authorized change, and reruns validation. P0 completes only when Projects can pin and invoke the owner-approved packages and schemas without copying or reimplementing runner, topology, credential, port, lifecycle, or evidence-validator logic.

15. **Gate disposition stays honest.** Given all P0 acceptance evidence passes, when the owners accept the exact repository revision, package/version pins, reproducible command, persisted fixture, schema/sample, negative-control result, and rollback procedure, then the Projects action ledger may record P0 evidence for 6.1-P4. P0 completion alone does not unblock Story 6.1, satisfy P1/P2/P3, create `6.1-entry-gate.yaml`, or authorize Projects implementation; P4 must accept all prerequisite pins and the Story 6.1 spec must independently pass readiness.

## Tasks / Subtasks

**Non-implementation entry condition.** Do not begin the implementation tasks until AC 1 has an
owner-approved repository-local record. The paths in the conditional file map are guidance, not a
repository decision.

- [ ] Establish owner-repository authority and freeze the contract (AC: 1, 10, 15)
  - [ ] Select the runner and validator repository or coordinated repositories; name accountable owners and create the local work item(s).
  - [ ] Record the baseline revision, package IDs, exact versions, release source, schema versions, accepted status vocabulary, evidence retention policy, and rollback pin.
  - [ ] Record P1-owned platform/version dependencies without choosing or normalizing them in P0. In particular, disposition any mismatch between the adopted architecture pins and current Builds central properties through 6.1-P1.
  - [ ] Freeze stable command, diagnostic, exit/failure-category, manifest, evidence, and run-state contracts before implementation.

- [ ] Package the supported local tools (AC: 2, 8, 13)
  - [ ] Add packable .NET tool entry points with the approved package IDs and `ToolCommandName` values for `hexalith-module` and `hexalith-evidence`.
  - [ ] Use central package management and the repository SDK/runtime rules; do not inline versions, suppress analyzers, or opportunistically upgrade dependencies.
  - [ ] Implement cancellation-aware commands, stable structured diagnostics, phase-aware failure classification, JSON output, and documented nonzero outcomes.
  - [ ] Build, test, pack, install/restore from a local package feed, and invoke the packed commands in CI.

- [ ] Implement the module manifest and profile contract (AC: 3, 6, 11, 12)
  - [ ] Define the versioned schema/model for descriptor assembly, modules, dependencies, deterministic identifiers, profiles, and repository-relative paths.
  - [ ] Validate duplicates, unsupported fields/versions, missing paths/profiles/assemblies, path canonicalization and containment, placeholders, and secret-bearing fields before lifecycle work.
  - [ ] Add a valid two-module sample plus one focused negative fixture per stable manifest rule.
  - [ ] Define stable pure-domain, host-contract/descriptor, persisted, restart, two-instance, browser, CLI, and MCP profile classes without embedding product-specific assertions in the runner.

- [ ] Implement supported composition and lifecycle (AC: 4, 5, 6, 8)
  - [ ] Adapt the platform-owned EventStore/Aspire composition seams to a manifest-driven runner without copying them into consumers.
  - [ ] Own dynamic ports/endpoints, Dapr resources, health/readiness, telemetry, identity/dev-secret injection, run-state, cancellation, and cleanup in the tool.
  - [ ] Make `down` idempotent and scoped to the invocation's resources; retain enough state for recovery without persisting secrets.
  - [ ] Preserve the current Projects AppHost/runtime as the migration baseline until replacement lanes pass; P0 does not remove or mutate it.

- [ ] Add the persisted G-4 control fixture (AC: 5, 6, 12, 13)
  - [ ] Compose the platform with at least two modules and real EventStore/Dapr persistence using a run-unique Tenant/domain/resource namespace.
  - [ ] Drive authenticated write, verify event and projection/read end state and sequence, stop/restart, rehydrate/read again, retry/idempotency, and two-instance access.
  - [ ] Add stale-state, absent-event, absent-projection, wrong-sequence, cross-Tenant, and prerequisite-unavailable controls.
  - [ ] Ensure critical unavailable prerequisites are non-passing evidence, not test skips; fake/in-memory topology tests remain unit regressions only.

- [ ] Emit deterministic invocation evidence (AC: 6, 7, 8, 12)
  - [ ] Define the evidence envelope, canonical serialization, phase model, hash rules, metadata allowlist, and volatile-field declarations.
  - [ ] Capture actual VSTest or MTP/xUnit v3 report outputs and normalize their semantic result without hiding native failures.
  - [ ] Apply redaction before logs/artifacts are retained and test the metadata-only boundary with seeded secrets/tokens.
  - [ ] Prove partial, cancelled, unavailable, runner-failed, test-failed, state-failed, and evidence-failed outcomes cannot serialize as passed.

- [ ] Implement `hexalith-evidence validate` (AC: 8-12)
  - [ ] Parse YAML with duplicate-key and unsupported-schema rejection separated from business-rule evaluation.
  - [ ] Resolve defaults, then apply deterministic AD-30 row, coverage, evidence, artifact, criticality, and release-disposition rules against effective rows.
  - [ ] Reconcile the Markdown view by stable row identity without treating Markdown as an alternate source of truth.
  - [ ] Emit deterministically ordered human and JSON diagnostics with stable rule IDs and separately test parse, schema, content, artifact, and policy failures.
  - [ ] Add conforming matrix/sample evidence and curated negative fixtures; the current Projects matrix may be an input control but is not modified by P0.

- [ ] Qualify release, rollback, and Projects consumption (AC: 2, 11, 13-15)
  - [ ] Add owner-repository build/test/pack/release automation and a clean-checkout package-consumer lane.
  - [ ] Run the valid and every blocking negative control through the packed tools; publish the deterministic sample, diagnostics, report hashes, and exact revision/package inventory.
  - [ ] Exercise `run`, `test`, `down`, cancellation, cleanup, prior-pin rollback, and revalidation from a clean checkout.
  - [ ] Hand the exact accepted revisions, versions, schemas, commands, fixture, evidence, negative-control result, and rollback procedure to 6.1-P4.

## Dev Notes

### Authority, Readiness, and Scope

- This artifact translates the approved `6.1-P0` action into implementation-ready behavior, but it cannot grant repository authority. The Sprint Change Proposal requires a Builds-owner-selected repository and owner-approved local issue/story before external mutation.
- P0 is an enablement work package, not one of the 33 approved Hexalith.Projects user-value stories. Keep Story 6.1 and its specification `blocked`; keep the Projects P0 action `open` until owner-approved evidence exists.
- P0 enables FR-2/FR-5 verification and traces to NFR-11, TEST-001, AD-25, and AD-30. It does not implement Projects read behavior, pick the P1 EventStore/platform baseline, implement P2 authorization or P3/G-5 identity, switch routing, remove the legacy AppHost, or self-accept P4.
- P0 has no UI implementation scope. Authenticated Web/CLI/MCP proof is a runner capability consumed by later story lanes; this work does not add presentation behavior or claim accessibility acceptance.
- The supported consumer commands are fixed by the Architecture Spine:

```text
dotnet tool restore
dotnet tool run hexalith-module run --manifest module/hexalith-projects.module.json
dotnet tool run hexalith-module down --manifest module/hexalith-projects.module.json
dotnet tool run hexalith-module test --manifest module/hexalith-projects.module.json --profile full
dotnet tool run hexalith-evidence validate _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml
```

- Story 6.1 later narrows the test command to `--profile reads --filter Story=6.1` and expects actual `.trx`, JSON summary, and shadow-equivalence evidence. P0 must support that contract but must not fabricate those Story 6.1 artifacts.

### Current Reality and Reuse Boundaries

- No `.config/dotnet-tools.json`, module manifest, packaged `hexalith-module`, or AD-30 `hexalith-evidence` implementation exists in this checkout. The readiness matrix truthfully marks the validator `not-available`.
- Current Projects AppHost/Aspire code owns hard-coded topology and is the migration baseline. Its static topology tests, fake dictionary-backed projection tests, and manual/offline E2E lane are not persisted G-4 evidence. Do not delete or expand that runtime under P0.
- EventStore exposes reusable composition and persisted-testing seams under `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/` and `Hexalith.EventStore.Testing.Integration/`. Its current generic AppHost fixture and skip-on-unavailable behavior do not by themselves satisfy a manifest-driven, fail-closed critical lane.
- `references/Hexalith.EventStore/scripts/validate-operational-evidence.py` is useful precedent for explicit schemas, stable rule IDs, sorted JSON diagnostics, parse/business-rule separation, redaction, and negative fixtures. It validates a different operational-evidence format and permits scenarios AD-30 must reject; do not rename or claim it as the new validator.
- Builds already differentiates VSTest and MTP/xUnit v3 execution. Reuse or centralize that behavior so the runner does not hard-code one test platform or silently translate a native failure to success.
- A previous persisted-smoke review found that key existence can be satisfied by stale data. Use run-unique identities and prove both the event and read-model/projection sequence across restart.

### Recent Git and Prior-Evidence Intelligence

- Current root commit `ec447e4` binds Story 6.1 to the G-4 command/evidence contract but does not implement the tools. Earlier corrective commits `aa20ff1`, `ec21c7e`, and `5e32ece` establish the PRD reconciliation, AD-30 matrix, and risk-first Epic 6 test design; treat them as planning authority, not capability proof.
- Architecture review commit `6f82b5e` records that a path-based Aspire manifest is feasible while clean-build, run/down, and package-mode proof remain absent. A checked-out revision or stale Release output is not tool-version evidence.
- EventStore Stories 3.8/3.10 and their smoke/validator scripts establish useful phase-classification and persisted-evidence patterns. Reuse their lessons, but do not copy their product-specific schema or let a numerically higher later diagnostic hide the causal phase failure.
- Story 5.12 evidence reinforces dynamic endpoint discovery, real identity, metadata-only artifacts, exact test counts, no silent skips, and cleanup. Its existing live failures are pre-existing evidence, not proof that P0 passed and not permission to weaken G-4.

### Manifest and Evidence Design Constraints

- The manifest is checked in, versioned, non-secret, assembly-explicit, and path-portable. Projects consumer fixtures remain thin: they declare module/profile intent but no topology, port, credential, Dapr, health, telemetry, or Aspire lifecycle definitions.
- Runtime resource names may include a run-specific suffix for isolation; the manifest's semantic domain/application/module identifiers remain deterministic. Evidence must distinguish deterministic semantic identity from volatile invocation identity.
- The readiness matrix is YAML authority under schema `hexalith.readiness-evidence.v1`; its stable row field is `key`. Apply defaults before checking effective completeness. The Markdown matrix is a human view over the same identities.
- Do not silently decide the current `blocked`-outside-legend inconsistency. Record an owner decision and validate the resulting canonical vocabulary.
- Critical missing prerequisites remain `unavailable` or failed and non-passing. Missing/invalid reports, zero selected tests, all skipped tests, incomplete artifacts, unsupported schemas, unexplained critical skips, and passed-on-unavailable are hard failures.
- Evidence contains metadata and hashes, not secrets or payloads. Preserve native reports as referenced artifacts, record their hashes, and emit a bounded canonical summary.

### Version and Dependency Boundary

- Work at the owner-approved revision with the repository-pinned SDK and central packages. At observation time Builds pins Aspire `13.4.6`, `System.CommandLine` `2.0.10`, YamlDotNet `18.1.0`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `6.0.0`; these are inventory, not authority to upgrade or normalize.
- The current Builds EventStore property and the adopted Architecture Spine baseline differ. Record the discrepancy and route it to 6.1-P1. P0 records and consumes the accepted versions; it does not choose them.
- Use a real YAML parser configuration that rejects duplicate mapping keys before model binding. Do not implement readiness parsing with regular expressions or a lossy deserialize-reserialize pass.
- Official .NET local-tool manifests support repository-scoped restore and `dotnet tool run`; packaged tools use `PackAsTool` and `ToolCommandName`. System.CommandLine supplies the supported parse/invoke model. Aspire testing APIs can manage distributed-application lifecycle, but the chosen test-host form must support the required builder; do not assume every file-based AppHost is test-builder compatible.

### Conditional Project Structure Notes

The Builds owner has not selected the implementation repository, so these are conditional impact
areas, not approved filenames.

If `Hexalith.Builds` is selected, likely **UPDATE** areas are:

- `Hexalith.Builds.slnx` to include tool, shared contract/rule, and test projects.
- `.github/workflows/build-release.yml` or an owner-approved reusable workflow to build, test, pack, package-qualify, and release the tools while preserving existing script validators.
- `Props/Directory.Packages.props` only for dependencies the approved tool design genuinely requires; prefer existing central pins and leave P1-owned version normalization out of P0.
- Root and `Tools/README.md` documentation plus package/release inventory.

Likely **NEW** owner-repository artifact groups are:

- Packable .NET tool projects for the approved `hexalith-module` and `hexalith-evidence` package identities.
- Shared manifest/evidence schema models, parser, deterministic diagnostics, lifecycle orchestration, and rule engine.
- Unit, contract, packaged-command, and live persisted integration test projects.
- Versioned schemas, valid two-module fixtures, deterministic samples, negative fixture corpus, negative-control record, and rollback documentation.

After P0 is owner-accepted, Projects consumer work may add `.config/dotnet-tools.json`,
`module/hexalith-projects.module.json`, the CI invocation, and thin profile/fixture metadata under its
own authorized change. Do not put platform topology or validator implementation in Projects.

### Verification and Evidence Contract

The owner-repository story must bind final project paths, but qualification includes at least:

```text
dotnet build <owner-solution-or-project> --configuration Release
dotnet test <unit-and-contract-test-projects> --configuration Release
dotnet test <persisted-integration-test-project> --configuration Release
dotnet pack <tool-projects> --configuration Release --output <local-package-source>
dotnet tool restore
dotnet tool run hexalith-module test --manifest <valid-two-module-manifest> --profile full
dotnet tool run hexalith-evidence validate <valid-readiness-sample>
dotnet tool run hexalith-evidence validate <each-negative-control>
```

Required P0 handoff evidence:

- owner-approved runner/validator repository revision and package inventory;
- reproducible clean-checkout commands for Debug/source and Release/package modes;
- valid versioned manifest and at-least-two-module persisted fixture;
- persisted event plus projection/read assertion before and after restart;
- deterministic evidence schema and passing sample generated by the runner;
- packaged-validator result for the passing sample and every blocking negative control;
- documented diagnostic/failure-category and metadata/redaction contracts;
- idempotent cleanup and prior-pin rollback procedure.

### Hard Stops

- Stop if owner repository, accountable owners, baseline revision, package identity, or rollback pin is unapproved.
- Stop if implementation would mutate Projects or a sibling repository without a separately authorized local change.
- Stop if the runner duplicates EventStore/Dapr/identity/FrontComposer/platform behavior in consumer fixtures or makes a source script a second public interface.
- Stop if a fake/in-memory store, handler return, static topology assertion, stale key, skipped prerequisite, or hand-authored JSON is offered as persisted G-4 proof.
- Stop if secrets, tokens, raw environment values, or protected payloads can reach logs, run-state, telemetry, evidence, or retained command output.
- Stop if the validator accepts duplicate YAML keys, undeclared statuses, placeholders, missing critical artifacts, incomplete coverage, failed critical evidence, unexplained critical skips, or passed-on-unavailable.
- Stop if current Projects AppHost/runtime is removed or default routing changes before equivalent G-4 lanes and later cutover/rollback gates pass.
- Stop if P0 attempts to choose P1 platform versions, satisfy P2/P3, self-approve P4, or mark Story 6.1 ready.

### References

- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md#6.1-P0-Deliver-the-G-4-Persisted-Runner-and-Evidence-Tooling]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#AD-25]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#AD-30]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#Development-Commands]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-projects-2026-07-15/ARCHITECTURE-SPINE.md#G-4]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-6-Authorized-Project-Reads-on-the-Supported-Platform]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-17.md]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-traceability-matrix.yaml]
- [Source: _bmad-output/test-artifacts/test-design-epic-6.md]
- [Source: _bmad-output/project-context.md]
- [Source: _bmad-output/implementation-artifacts/6-1-list-and-open-projects-through-supported-authenticated-paths.md]
- [Source: _bmad-output/implementation-artifacts/spec-6-1-list-and-open-projects-through-supported-authenticated-paths.md]
- [Source: references/Hexalith.AI.Tools/hexalith-llm-instructions.md]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreDomainModuleExtensions.cs]
- [Source: references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/AspireTopologyFixtureBase.cs]
- [Source: references/Hexalith.EventStore/scripts/validate-operational-evidence.py]
- [Source: references/Hexalith.Builds/Tools/test-domain-workflow-test-platforms.ps1]
- [Official .NET local tools: https://learn.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use]
- [Official `dotnet tool restore`: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-restore]
- [Official .NET tool packaging: https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create]
- [Official System.CommandLine overview: https://learn.microsoft.com/en-us/dotnet/standard/commandline/]
- [Official Aspire AppHost test lifecycle: https://learn.microsoft.com/en-us/dotnet/aspire/testing/manage-app-host]
- [Official Aspire advanced testing scenarios: https://aspire.dev/testing/advanced-scenarios/]
- [Official Microsoft Testing Platform features: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-features]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Story creation and source analysis only; no owner repository was selected and no implementation build or test was run.

### Completion Notes List

- Ultimate context engine analysis completed across the corrective epics, canonical Architecture Spine, readiness matrix/report, Sprint Change Proposal, Epic 6 test design, Story 6.1/spec, current Projects topology/tests/CI, EventStore and Builds reuse candidates, recent Git history, and official .NET/Aspire/test-platform guidance.
- Classified this artifact as an external prerequisite handoff and preserved its truthful blocked status because repository selection and owner approval are still uncommitted.
- Added fail-closed persisted controls, deterministic evidence fields, VSTest/MTP compatibility, negative fixtures, clean-checkout source/package parity, security/redaction, rollback, and exact P4 handoff boundaries.

### File List

- `_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md` (new)
