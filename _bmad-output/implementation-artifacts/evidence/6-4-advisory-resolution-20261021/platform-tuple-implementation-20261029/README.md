# Approved Platform tuple correction — 2026-10-29

The user authorized the reviewed correction, CI artifact bindings and candidate recording with “I authorize”. Platform now records exact candidate **`9f43cefe11bd246ce0de1cd0740261b557c0c7a0`**, committed with normal validation. Its Builds gitlink is **`d1931b1e803144048d927fff16cb8119afde541e`**, matching tool **4.38.3** and floor **`[4.38.3, 5.0.0-0)`**. Intended catalog selections remain Tenants 5.11.1, EventStore 3.128.4, Conversations 1.3.1 and FrontComposer 4.7.1.

**This is a recorded local candidate, not accepted Platform runtime or published-tuple qualification.** Story 6.4 stays in progress. The original no-push constraint remains in force; no remote CI dispatch, publication, deployment or consumer adoption occurred. The owning Platform main was already divergent from its cached origin/main (ahead 1/behind 4); after this commit it is ahead 2/behind 4. No rebase/merge or user-history rewrite occurred.

## Recorded change

The two [previously reviewed patches](../platform-builds-tuple-qualification-20261029/README.md) are applied: exact tool/floor/catalog gitlink and README, plus direct file-host EventStore 3.128.4 and Toolkit 13.6.2-beta.959. All four existing analyzer corrections are included unchanged in the commit. No sibling Builds source, umbrella gitlink, consumer manifest or historical evidence is changed.

Owner CI now binds the published archive's Tooling and EventStore-host assemblies through `HexalithBuildsPublishedToolRoot`. Composition restores the same catalog-controlled Tooling dependency closure explicitly. Tests keep alias `buildsseed`, copy the host dependency manifest, and add its real assembly identity/content directory to MVC Testing's generated manifest. An assembly attribute alone was insufficient because the generated manifest takes precedence; the retained initial failure documents this correction.

`Directory.Build.targets` rejects CI Debug/source mode, foreign ProjectReferences and missing published assemblies. The existing Debug source defaults remain available. The new scripts verify the exact approved signed archive/source, compare extracted bytes, traverse the entire evaluated project-reference closure, retain restored assets, stream bounded command logs and retain source fingerprints plus SDK/runtime/runner identities. The CRLF whitespace attribute preserves the existing Directory.Build.props byte convention while retaining other whitespace checks.

The new manual `Platform tuple CI preflight` workflow accepts a full candidate SHA, uses a standalone owner checkout, initializes only its declared direct dependencies, and builds the owner solution in package-only Release mode. Its explicit required profile is 80 tuple/analyzer/host cases plus two owner pin/inventory cases, with the 14 MSBuild controls also required. Other Platform suites, gated live cases and policy-bound runtime qualification remain separate mandatory gates. Empty profiles, missing coverage and skipped selected cases cannot pass. Bootstrap provenance/outcomes survive checkout, dependency or SDK failures; failed preflight evidence is uploaded too. A passing preflight deliberately leaves `candidateAcceptance: false`.

## Executed verification

Each `*.command.json` records exact argv, cwd, explicit environment, UTC timing and exit; matching stdout/stderr files are retained. [COMMANDS.md](COMMANDS.md) is the readable index. Earlier failed commands remain alongside their successful corrected runs.

| Check | Actual result |
| --- | --- |
| Approved tool restore/version | exit 0; `4.38.3+d1931b1e803144048d927fff16cb8119afde541e` |
| Published archive preparation/signature/closure | exit 0; exact approved signed ZIP and Tooling hash |
| Corrected Debug build with published Builds assemblies | exit 0; zero warnings/errors |
| Existing analyzer and gateway host classes | exit 0; 80/80 passed, zero errors/failures/skips/not-run |
| Existing owner pin and inventory methods | exit 0; 2/2 passed in real standalone owner/tag context |
| MSBuild dependency-binding controls | exit 0; 14/14 passed |
| Empty/malformed profile negative controls | four rejected; valid profile requires exactly 82 cases |
| File-host catalog alignment | exit 0 |
| Recorded-candidate package graph audit | exit 0; all 27 owner/transitive project graphs, no foreign source references |
| Workflow YAML/outcome structure, whitespace, commitlint | passed |
| Candidate commit | exit 0; exact SHA above, 19 owner files |

The first artifact-bound build exposed the need for a direct Tooling reference in the test project (68 missing-type errors); the corrected build resolves it. Initial binding controls also contained stale assertions that all references were the Tooling project, although the owner already had additional internal references. Their assertions now verify the actual Builds selection; the new separate CI-negative case rejects foreign source fallback. The original host-content-root test failed two cases before its generated-manifest binding was corrected. None of these failures is hidden or counted as a pass.

The narrow owner-pin check uses a real standalone clone of the actual recorded candidate, with the actual published Builds commit and genuine v4.38.3 tag cloned from existing verified Git history. This does not initialize any nested submodule in the user's umbrella. Its Debug test output is copied byte-for-byte from the local build solely for the two metadata/Git checks; it is explicitly **not** an exact-candidate Release build. An untracked exact baseline copy supplies standalone instruction lookup and earns no clean-source/acceptance claim. See `standalone-owner-context.json` and its XML.

Local builds still reference sibling EventStore source. Their passing tests are local diagnostics with published Builds assemblies, not consumer package-mode/runtime acceptance. The fresh recorded graph audit records the actual committed SHA, source cleanliness and content fingerprint. Its `graph-verified` status means graph inspection only; no Release assets or GitHub CI result were fabricated.

## AppHost observation

Before editing, Aspire 13.6.4 start failed with NU1605 for the three approved file-host dependency mismatches. After alignment, the AppHost built and started (exit 0). Its local resources included healthy Dapr/Redis/support containers, but EventStore was unhealthy and dependants waited. `aspire wait eventstore --status healthy --timeout 10` exited **17**. This bounded source-mode observation is not a full startup-deadline or package/runtime acceptance result.

The exact AppHost was stopped normally (exit 0), without force; `aspire ps` then returned `[]`. `aspire-resources-sanitized.json` retains resource names/types/states only. The startup stdout's dashboard authorization URL was redacted; environments, resource arguments, credentials and private runtime logs are not archived as qualification evidence. No cleanup qualification is inferred from the stop command alone.

## Remaining exact next action

The candidate and new workflow exist only locally. GitHub-hosted CI cannot consume them until remote source transport is authorized. The next concrete authorization is **push this exact candidate/workflow to an owner branch, deliver/register the manual workflow through the approved owner default-branch process, then dispatch `Platform tuple CI preflight` with candidate SHA `9f43cefe11bd246ce0de1cd0740261b557c0c7a0`**. A branch-only upload does not register a new manual workflow: GitHub requires `workflow_dispatch` to exist on the default branch, as stated in its [manual workflow documentation](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow?tool=webui). Do not force-push the divergent main branch. If owner integration changes the selected candidate, capture its new full SHA and run fresh evidence for that revision. A PR merge decision must bind its real merged head/base candidate and fresh authenticated decision; this local SHA or preflight cannot be relabelled as that proof.

The preflight is followed by the remaining owner runtime/decision gates, including actual expected/loaded content identities and generated/direct-host qualification with ready services. Publication needs separate approval and a verified official Platform identity. Consumer adoption then needs separate approval, exact umbrella Platform/Builds pins, successful actual Projects package preparation, the retained 46 acceptance-inventory discrepancies resolved from real restored closures, explicit FrontComposer.Contracts.UI selection, durable per-package consumer evidence and all package/symbol acceptance. The previous qualification plan remains historical; this receipt advances only its authorized correction/recording portion.

Independent review closed all seven final findings and the earlier scoped corrections; detailed classifications are in `review-triage.md`. Preservation verification records every allowed change and checks original analyzer bytes, prior receipt hashes, all other original files, and the unchanged HEAD/index identities of the other twelve repositories. Projects' story/sprint bytes remain unchanged and in progress.
