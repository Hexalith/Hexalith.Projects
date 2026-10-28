---
title: 'Story 6.4: Projects root enrollment preparation'
type: 'bugfix'
created: '2026-10-28'
status: 'in-progress'
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
