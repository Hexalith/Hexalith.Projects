# 6.1-P1R published successor — owner decision request

**Status (2026-10-11): decision pending.** No documented owner decision selects a published successor and its rollback disposition. This packet requests that decision; it is not an approval, package run, publication request, or pin change. P1R remains unusable and Story 6.1 remains blocked.

## Decision basis

- The [approved remediation handoff](spec-6-1-p1r-remediation.md) reserves exact candidate, capable rollback, publication, pin, and acceptance decisions to their owners.
- The [completed support-boundary spec](spec-6-1-p1r-supported-successor-qualification.md), [case contract](../../references/Hexalith.EventStore/_bmad-output/specs/spec-6-1-p1r-remediation/qualification-contract.md), and [tooling receipt](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-support-boundary/README.md) retain all 163 sealed cases: 123 supported obligations, 39 old-route refusals requiring admission proof, and one incompatible containment case. This measured table is bound to 3.119.0; a different candidate needs its own measured boundary and owner disposition.
- The [sealed 3.119.0 packet](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/6-1-p1r-31190-published-run/README.md) is valid but `technically_qualified=false` and `p1r_usable=false`. Candidate stale-fence and three logical-alias checks fail. Its packet SHA-256 remains `1cb9d427c34db61ad250173b13016c69de7e13db522c4fa45cf478fa2d468b4c`.
- The [fixed October 1 acceptance](6-1-p1r-acceptance.json) names EventStore 3.110.0 and rollback 3.70.1 for the earlier tuple. It does not select a successor. The sealed 3.119.0 owner-decision receipt still records all four roles as pending.

Observed source baseline: Projects `f1c15360c09f039c7b7a4a6eb280c0d096233756`, EventStore gitlink `25f063e9fee2da1f4e00acb512083c2ca223d12a`, Builds gitlink `db20fea8b616dc0b0c57a72d65b0c785ff460f18`. These are checkout coordinates, not published-package selection.

## Requested pre-run owner decision

EventStore Owner and Builds Owner: record a dated, named decision that supplies **one exact, already published EventStore successor family**: package version and package IDs, tag, full source revision, the tag's Builds gitlink, actual package-build Builds version/revision, and the published repository or release location. Record archive SHA-256 digests if known; otherwise authorize a read-only capture of the named archives before execution, with their digests bound into the qualification packet and later owner acceptance. State whether this authorizes qualification of those immutable archives. If no suitable published family exists, record that outcome; the package run stays closed. A tag, catalog pin, source build, or checkout gitlink alone is not a selection.

For the same successor, select **one rollback disposition**:

1. `rollback=null`: explicitly retain the Projects AD-17 single-writer fence, drain, mutation freeze, and forward recovery. No older package is represented as a writable rollback.
2. A proposed capable **published rollback family**: name its exact EventStore package version and IDs, tag, full source revision, tag Builds gitlink, actual package-build Builds version/revision, and publication location; bind archive digests by the same rule. Qualification must prove restore of floor 5/head 12 with snapshot 9, append 13 by that writer, then restart/replay with original event hashes and Tenant isolation intact. The rollback writer must also replay every new supported event and emit spine-compliant events and observable command behavior. Until that passes, retain the AD-17 freeze.

After measurement and before qualification acceptance, the Solution Architect must disposition the candidate-specific support/refusal matrix and selected rollback envelope. Fixture admission observations alone do not establish production P2 enforcement; Platform and Identity/Security owners must prove the protected production routes refuse incapable calls before access or effect before operational use. This pre-run decision does not grant later four-role acceptance, Stack/catalog/consumer pin changes, deployment, or Story 6.1 readiness.

## Execution after an exact decision

Run a new isolated Release/published-package qualification on the approved PostgreSQL/Dapr 1.18.2 profile. Bind actual archive/signature provenance, resolved assets and lock graphs, physically loaded assemblies, source/Builds/configuration hashes, case-level assertions, persisted state, restore/restart, owned cleanup, and unchanged shared resources. Retain failed historical rows; derive the candidate-specific case table and prove each proposed old-route refusal before protected access. Candidate stale-fence and logical-alias obligations must pass. Independently validate the new packet, then seek exact EventStore, Builds, Solution Architect, and Test Architect decisions and same-baseline conformance. Only subsequent owner-approved transitions may change pins or P1R usability.
