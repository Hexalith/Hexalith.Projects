# Projects package compatibility — approved continuation

Projects.Contracts `2.0.0-story66.3` is public, repository-signed and byte-identical to every approved payload entry. EventStore.Contracts `5.3.0` publication is blocked by inherited required owner CI checks on [PR #401](https://github.com/Hexalith/Hexalith.EventStore/pull/401). The task is **not complete**; the two-file Projects pin correction remains prepared and undelivered.

The [machine-readable result](result.json) records exact sources, hashes, approvals, limitations and remaining work. No package upload was retried after an ambiguous result.

## Completed delivery and verification

- Qualified fixed publisher and exact archives: CiCd `264e5de34341187a0ad4724dd6a39248b9cb9b58`.
- Qualified narrow delivery: CiCd `cc4b8cc46f4e254e20518c46726fc5a57ed1164f`; additional protected proposal/dispatch/policy support `c47729d371295387f99b88f36388162623ef41d4`.
- Projects publisher `8b53d6b917957762949d39c21bc5de0919811413`; exact closed workflow-policy compatibility correction `78633a653c1a6768a795678203d836b3caac2c12`. The corrected policy accepts only the approved publisher jobs and their inherited root context; all previous full-release checks remain.
- One unchanged Projects.Contracts upload was accepted in [run44048396433](https://github.com/Hexalith/Hexalith.Projects/actions/runs/44048396433). Its initial observation timed out before NuGet indexing; that failed receipt remains. [Verification-only run44050366352](https://github.com/Hexalith/Hexalith.Projects/actions/runs/44050366352) subsequently passed signature and all payload checks.
- Public signed archive SHA256: `81888d8267dbf6593ca2cfd8c63bdd6da2732351a67400602058612dc70e5b8b`; approved unsigned archive remains `349f938db77dd83ff45e200997b1cba97147c46b1855ec58fd6e2a4b0572eff5`.
- The [current pinned McpCli consumer](current-public-mcp/qualification.json) restored from nuget.org only, built Release with warnings as errors and discovered all four Projects operations strictly. It restored the exact signed public archive. This is local package-consumer evidence, not full hosted G-6 acceptance.

## EventStore publication block

Main push was rejected by PR/eight-check protection. The owner mechanism instead published an atomic create-only proposal branch. A changed main was requalified without changing the workflow patch; current reviewed base is `392a7e4e9daade785ee9f1ae5f40e25931002735`, branch head `e67afa4519e3d9d0d37eab3c110077b14218dcd3`, result tree `330e35a6576060f0a94177ca9c3cf0eb205d5330`. CiCd proposal rebind source is `5abd918e58ab95a554c680c49d3bc1b044270011`.

The existing App’s PR token request was denied HTTP422. The user explicitly authorized the existing connected `jpiquot` account for PR creation, which opened draft PR401. No App grant was changed. The PR changes only `release.yml`; every other tree entry and gitlink is preserved.

Five required checks fail on the exact PR head. Build-and-test, tenants-source-mode and advisory cannot fetch existing CiCd gitlink `88fb9071e8b9cac526b51684bf92f891a0ef1bee`. Live-sidecar and Phase4 risk fixtures discover zero tests and exit5. [Parent comparison](eventstore-pr401-inherited-failure-comparison.json) confirms the same failures before this one-file proposal. Commitlint, CodeQL and dependency review passed. The PR is not merged; no required check was waived. The approved EventStore archive/source/package-only gates remain unchanged and publication has not been attempted.

## Ready pin correction and limits

The [current two-file candidate](pin-candidate-projects-78633a65/candidate-tree.json) is tree `09192422c484c8519e9a21547d13a4cb6200f735` on Projects `78633a653c1a6768a795678203d836b3caac2c12`. It changes only the Contracts override to5.3.0 and the four matching expected dependency entries. Family5.1.1, all other pins and all gitlinks remain unchanged. The earlier207-case exact-artifact test inputs are unchanged except for three CI files; no new full Release/G-6 pass is claimed.

[CI44047607339](https://github.com/Hexalith/Hexalith.Projects/actions/runs/44047607339) retains the pre-publication restore failure. An optional same-source G-6 rerun was denied HTTP403 by the connected integration; it did not run and no alternate writer was tried. Final corrected-source CI/G-6 will follow the actual pin delivery after both public prerequisites are verified.

The original13 subsequent Conversations/generator API errors remain retained in the earlier packet. No unrelated dependency pin, contract metadata, native runtime routing, v2 state, deployment or Story1.17 tracking was changed. Disposable local jobs used run-with-temp and no nested submodule was initialized.
