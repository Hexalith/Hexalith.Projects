---
title: 'Story 6.4: Projects root enrollment preparation'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
baseline_commit: '6cdf9bbdc0824e59529c9a99c16b7ec02e03e21b'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="User authorized necessary isolated source/configuration preparation and testing; no delivery or Story completion">

## Intent

Resolve HXW006 blocking Projects current-source verification through the smallest supported Projects/Builds root enrollment route. The root declaration must describe actual Projects contracts and pass the supported schema. Verify owner revisions before source selection; preserve original working trees, indexes and user changes in an isolated workspace. Use the actual public producer environment for narrow Projects Story 6.4 builds and available advisory, authorization, isolation and zero-write tests. Preserve Platform release qualification, committed catalog selection, package identity and containment gates. Record exact commands, source identities, generated inputs, results, unavailable prerequisites and the next blocker. Never fabricate a verification-only manifest, edit generated mappings, impose the Conversations mapping on Projects, select unqualified dependency versions, commit, push, publish, deploy or initialize nested submodules. A source-tested candidate is not a qualified release. Story 6.4 remains in progress.

</frozen-after-approval>

## Implementation Notes

Bounded root configuration preparation, separate from the advisory story's acceptance. No intent gap or irreversible action; footprint is the supported Projects domain declaration and its usage documentation, with separate evidence tooling. No story or sprint status mutation. Initial Git observations preceded external pointer advancement; the preservation baseline and selected identities were captured afterward at the canonical baseline above. This session made no commits or original Git mutations.

Code investigation: Builds WorkspaceRootResolver resolves a root v2 moduleId projects to Hexalith.Projects at the real root; the unchanged containment validator can then accept Projects references. Qualified Builds 4.38.1 source 16ff70a2321ad576faaac3fe53088c2f1bdacae3 has the same relevant schema/resolver as current owner source. Reuse the published CLI from Conversations' existing exact local tool manifest, with an absolute Projects manifest path; no tool/dependency pin changes. Select other verified owner HEADs, excluding Platform's existing dirty candidate. Align only the disposable root index with its real direct checkouts, as permitted by SubmoduleInitializer. Preserve Platform's committed catalog selection even when it differs from tooling source.

Planned verification: public validate and prepare-build; actual generated-environment Debug Contracts.Tests and Server.Tests builds; direct xUnit v3 focused classes and explicit gateway/store prerequisite probes. Record failures rather than disabling analyzers, inventing synthetic project substitutes or weakening guards. Review the bounded change and evidence using the configured Blind Hunter layer before final handoff.


The implemented candidate is isolated at `/tmp/story64-projects-root-enrollment-20261028/projects`; only the normal Projects v2 domain declaration, its provenance/usage README, and five producer-aware reference metadata entries in Projects.Contracts changed. Public source preparation includes the Projects root. Both individually built Debug test projects pass with zero warnings/errors. The focused contracts/advisory/delegation/identity/tenant lanes pass 174 existing cases; the unchanged signed HTTP gateway fixture passes one additional case against actual Dapr/Redis with persisted five-key before/after bytes. Eight actual-project metadata evaluation controls preserve ordinary/partial-marker behavior and analyzer metadata. Generated mappings are untouched. Review prompted declaration additions for Parties, the bound-stream proof key, symbolic identity egress and explicit fixed-store prerequisites; these represent actual production contracts. The reviewed public preparation/build/test runs are retained separately, without double-counting repeated tests.

Public package preparation still rejects unreleased Platform fcee54288c75feaadd673e0484da1e5b5d742cc8 and its committed Builds catalog 81ab049208678bd6616828824729c0ddac02f7d1 versus qualified tooling source 16ff70a2321ad576faaac3fe53088c2f1bdacae3 (HXW011/HXW009). The committed catalog is producer-selected and retained; no dependency override or release acceptance is inferred. Full runtime binding, fixed projection components, identity setup and deployed acceptance remain open.

Strict original-state audit failed because concurrent external commits captured in-progress evidence and advanced root HEAD/refs/index, while Platform's original dirty files disappeared and its raw index changed. This session issued no original mutating Git command and did not restore or overwrite that external state. The failure and every observed difference are retained rather than represented as an unchanged-workspace pass. Tested source identities remain fixed in the disposable checkout. No Story or sprint-state advancement is made.

## Review Triage Log

- medium / patch: missing Parties dependency — verified grant/revoke bootstrap invokes Parties authority; added dependency and provenance.
- medium / patch: missing bound-stream private-key secret — verified ProjectChatBotAuthorityGateway requires the key; added logical inventory plus matching EventStore trust prerequisite, without values.
- medium / patch: fixed projection components omitted — documented statestore and projects-conversation-start-setup and the absence of an owner-supported remapping; invented no schema key.
- medium / patch: identity egress omitted — declared actual bearer/workload discovery, JWKS and explicit/discovered token destinations with unresolved deployment values.

No review finding is deferred. Full evidence and the uninstalled patch are under `evidence/6-4-advisory-resolution-20261021/projects-root-enrollment-20261028/README.md`. Source-tested preparation is not a qualified release or Story completion. The explicit user Git/publication/deployment prohibition overrides the generic one-shot commit step.


Independent reviewer closure confirms all four findings resolved, including the final SDK fallback wording correction. Final public schema validation and unapplied patch checks pass. Bounded root-enrollment preparation is complete; the source-tested candidate remains isolated and Story 6.4 remains in progress. Original-state drift remains an explicit failed check, not a completed preservation claim.
