---
title: 'Story 6.4 Platform/Builds tuple qualification preparation'
type: 'bugfix'
created: '2026-10-29'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="User authorized read-only investigation, local checks and a reviewable proposal on 2026-10-29">

## Intent

Continue story 6.4 by selecting the exact published Builds/tool/catalog candidate compatible with current Platform and intended dependencies, including Tenants 5.11.1. Verify official release, source, archive and catalog identities against Platform's committed Builds gitlink, prepared tool and actual sibling source. Preserve all existing work, four local analyzer fixes and historical evidence. Prepare an unapplied correction and an exact-candidate qualification plan. Run relevant read-only and local checks without dependency changes or external writes; record exact commands, results, mismatches and blockers. Distinguish source diagnostics from published-tuple acceptance. Do not stage, commit, push, publish, deploy or initialize nested submodules. Leave main story 6.4 and its sprint state in progress; identify separate authorization needed to apply or record the proposal and qualify consumers.

</frozen-after-approval>

## Implementation Notes

- No intent gap or irreversible action in this preparation scope. Footprint is this new scoped spec and a new evidence directory; existing source/configuration/tracking bytes remain unchanged. Existing dirty work is explicitly authorized and must survive; the skill's dirty-tree stop adds no new approval requirement.
- Guidance: root-declared AI.Tools baseline, workspace EditorConfig/Git attributes/build files, Builds AGENTS/DEVELOPMENT/build configuration, Platform README/build/test files, architecture AD-4/AD-5 and candidate policy. No skill under references is loaded.
- Evidence directory: evidence/6-4-advisory-resolution-20261021/platform-builds-tuple-qualification-20261029/.
- Investigated paths: Builds Workspace/PlatformIdentityResolver.cs binds owner HEAD or consumer committed Platform gitlink and validates exact Builds pin; CatalogVersionMaterializer.cs evaluates pinned Props/Directory.Packages.props; Tooling.csproj generates the embedded runtime catalog, which has no Tenants field. Platform DomainEnrollmentKitTests verifies committed pin and release-tag equality. Nested Builds is uninitialized; no Platform CI workflows are tracked.
- Candidate under verification: published Builds 4.38.3 at d1931b1e803144048d927fff16cb8119afde541e. Prepared 4.38.2 still selects Tenants 5.11.0; recorded e129a29e6c6e2f8a200581e09b861d75a9f3a7b8 additionally selects EventStore 3.128.3. Sibling e909ffd9fe9a6097e36d932010b983d029396968 has the intended catalog but a different source identity. Catalog equality does not establish source or integration qualification.
- Verify official tag/release, feed SHA512/source, signed archive, embedded runtime resource, evaluated package catalog and producer CI/inventory. Prepare exact unapplied owner diff and preserve all dependency versions in actual checkouts. Rerun narrow built tests/evaluation and actual public package preparation only where they do not mutate dependencies or Git. Local Debug and producer qualification do not establish Platform candidate acceptance.
- Preparation selects published 4.38.3/d1931b1e803144048d927fff16cb8119afde541e as a proposed correction, not an accepted Platform tuple. Signed archive/source/producer inventory/CI identities verify; exact dependency metadata shows 195 satisfied selected-version constraints plus one explicit unresolved FrontComposer.Contracts.UI edge. Seventy-six analyzer regression cases and one inventory case pass. Owner-pin check fails on missing nested tag context; both actual public package preparations fail with two HXW011 diagnostics and HXW009.
- Two exact unapplied patches and complete proposed bytes are retained. Direct apphost EventStore/Toolkit drift, foreign Builds project references in the owner CI graph, absent tracked Platform CI, consumer acceptance inventory drift and consumer durable graph/symbol evidence are explicit prerequisites. Dependency changes, recording, CI dispatch, publication and adoption remain separately authorized future work.
- Preservation verification passes for 114238 original files and all 13 repository HEAD/index identities. The main story and sprint file remain byte-identical and in progress. No staging, commit, push, dependency edit, publication, deployment or nested initialization occurred.
- Scoped preparation is complete; this done status applies only to the new preparation spec. Main story 6.4 remains in-progress under the user's explicit constraint. Independent closure review confirms all nine findings addressed; no commit is performed because the user prohibited recording Git changes.

## Review Triage Log

1. Medium, patched: make both foreign Builds source references explicit package-CI blockers and require approved artifact bindings before candidate selection.
2. Medium, patched: bind all extracted execution bytes and exact CLI version/source output to the verified signed ZIP.
3. Medium, patched: bind materialized catalog bytes, evaluation wrappers and recorded MSBuild provenance to the selected Git blob.
4. Medium, patched: validate the exact eleven dependency packages against raw official registration/catalog hashes and content.
5. Medium, patched: retain the previously omitted 196th dependency constraint as explicitly unresolved.
6. Medium, patched: recompute NuGet version-range satisfaction and require nonempty complete coverage.
7. Medium, patched: bind complete CI job lists to exact run ID, attempt and source SHA.
8. Low, patched: make proposed README describe its resulting selection while retaining pending acceptance.
9. Medium, patched: require durable per-package assets/log retention before temporary cleanup and explicit symbol verification.

Detailed evidence and triage are in the new receipt's review-triage.md. No existing story acceptance state is advanced by this scoped preparation.
