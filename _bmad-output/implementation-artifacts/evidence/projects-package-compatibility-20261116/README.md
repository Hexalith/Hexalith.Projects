# Projects package compatibility: owner publication decision

The two reported blockers have a qualified local compatibility resolution. Neither required
artifact is available on the configured nuget.org feed. No publication, pin
delivery, workflow change, adoption, deployment, v2 activation or tracking update
has been performed. Fresh hosted acceptance of the correction remains pending.

## Concrete package proposal

| Package coordinate | Exact producer source | Qualification archive SHA256 |
|---|---|---|
| Hexalith.EventStore.Contracts / 5.3.0 | EventStore `0be0c922883d4ea61b18e843e24472162261d934` | `1c3bdabc4778467d74b468a47f0413f5f85272539d9969891036c8865f6949c3` |
| Hexalith.Projects.Contracts / 2.0.0-story66.3 | Projects `5b4bdeacde318a1d948db7ef76843df0c548f084` | `349f938db77dd83ff45e200997b1cba97147c46b1855ec58fd6e2a4b0572eff5` |

The EventStore candidate uses its pinned Builds
`e2bbbd0893f586e4d447ef8358ff747ea3d34435`. Both public 5.1.1 and 5.2.0 lack
QueryResultContractAttribute. The candidate contains the constructor
`(Type, string, string)` and the three required properties; 163 query tests and
two generator tests pass in the initial focused qualification. A fresh producer
build uses an empty private cache and the approved public feed; its exact archive
above passes the final 207-case Projects Contracts consumer run. Binary comparison
reports zero breaking changes and 30 additive public types. This source also includes earlier production-profile
v1 declarations; the later remote v2 changes are excluded. The local archive is
unsigned and requires the package owner's approved publication/signing process.
[Fresh producer evidence](producer-replay-20261116T214600Z/eventstore-contracts-candidate-qualification.json)
binds the source, catalog, package and binary inspection. Its full Contracts run
executed 1,704 cases: all 1,328 outside Packaging passed, including all 163 query
and 130 deployment cases. Packaging retained 52 failures and two skips because
Git-backed owner dependencies and complete package-contract fixtures were absent.
[Full scope summary](producer-replay-20261116T214600Z/full-contracts-scope-summary.json)
retains every failure and skip; the full producer gate did not pass. Existing
exact-source owner CI43944683003 compiled successfully but was cancelled during
tests. Mandatory owner release gates remain prerequisites, and no nested
dependencies were initialized to satisfy this broad fixture.

The earlier local archive e367e3e9... remains preserved. The final proposal selects
the fresh public-cache archive explicitly. Both have identical public APIs;
compiler-generated private Regex type-name hashes and PE/PDB metadata can differ
between build paths. No DLL byte reproduction or complete producer-gate success
is claimed.

The Projects proposal reuses the exact retained Story 6.6 archive; it does not
rebuild current source under the historical coordinate. Its dependency closure
remains EventStore.Contracts 5.1.0, McpCli.Abstractions 2.0.1,
Commons.UniqueIds 3.0.0 and ByteAether.Ulid 1.4.2, with no UI/ASP.NET closure.
The original 119 Contracts source hashes match the recorded commit. A fresh
rebuild using that commit and its Builds
`801f268cd1b37ecae41d4fb20d115c055b4c5793` reproduces the nuspec, README, CLR
signatures, custom attributes and method IL. The DLL differs only in parsed
PE/PDB timestamps, MVID, PDB identity/path and checksum. Full ZIP/DLL byte
reproduction is not claimed. [Payload equivalence](mcp/historical-payload-equivalence.json)
and [source provenance](mcp/historical-source-provenance.json) retain the proof.

## Smallest consumer correction

The [two-file patch](projects/minimal-contracts.patch) updates only
EventStore.Contracts to 5.3.0 after the catalog import, plus its four exact
dependency expectations in tools/release-packages.json. All other EventStore
packages stay at 5.1.1. McpCli source, every unrelated pin, all 12 gitlinks,
workflows, catalog acquisition, Conversations preparation and completed gate
repairs remain unchanged. The current full candidate Git tree is
`080313f55def2d0972ab5cbaecfbaf5b5d27b7b3`, based on Projects
`cd1fcd62e3180e5b037850cacb1c1756668b2866`. The [fresh-source proof](current-source-candidate-qualification.json)
preserves all 64 concurrently delivered paths and all 12 gitlinks. All 578
production, Contracts-test, build/configuration and direct dependency entries
used by the focused qualification are unchanged from the tested85ff source, so
its final207-test result is reused without another run. The earlier candidate
tree61450315 remains retained as historical qualification, and complete
Release/G-6 results are not inferred from this equivalence.

The initial baseline reproduces 14 CS0234 errors. With the final exact EventStore
archive, Contracts and its test project compile cleanly and all 207 Contracts
tests pass, with zero failures or skips. The earlier candidate also passed
solution restore; the full solution was not rerun for the final archive because
its public API is identical and the independent subsequent failures are retained. Pinned McpCli `5008de388c5e37257ca78e48d8afe899cf7aaa2a` restores,
builds with Release warnings-as-errors, and discovers all four expected
Projects operations with the unchanged historical archive. No McpCli pin or
source substitution is needed. These feeds are disposable qualification inputs;
the production feed configuration is unchanged.

The earlier complete Release build failed with 13 subsequent errors: six
missing advisory/membership members in Conversations.Contracts 2.0.0 and seven
unresolved typed QueryAsync calls under the unchanged EventStore Client/generator
5.1.1 selection. Their logs and focused API diagnosis are retained separately;
neither dependency is changed by this proposal. QueryAsync is a generated
extension: source0be introduces TypedGatewayQueryEmitter, while the published
5.1.1 generator lacks that emitter. The existing Client interface already
provides SubmitQueryAsync. A full package/release gate or G-6 runtime success
is not claimed.

## Approval and continuation

The requested decision is approval for the two package owners to take these
exact coordinates, sources and qualified payloads through their existing approved
release/signing process. Actual publication remains conditional on successful
mandatory owner gates; this is not an immediate-upload approval request. It grants no gate bypass, new feed,
credential expansion, full release activation or container publication. The
EventStore and Projects package owners must retain their required release
checks and existing DW-82 owner/signing prerequisites. Completed publication/A8
work is reused, not repeated.

After approval, each owner must recheck coordinate absence and current authority,
publish only its approved artifact scope, and verify the signed public payload
against this qualification. Signing can change ZIP container bytes and add `.signature.p7s`; require every
qualified payload entry, including the nuspec and DLL, to remain byte-identical.
The machine-readable proposal binds every entry hash. Retain both archive hashes
and external owner provenance. Any changed payload requires new affected qualification. The two packages have independent dependency closures and can
be published independently.

Then recheck Projects and CiCd main, requalify the necessary consumer patch on
that exact source, and deliver through the qualified CiCd owner path. Its
existing nine-file ci-gates profile cannot carry this two-file patch: use a
separately qualified closed dependency profile if owner delivery is required;
do not widen either completed profile or patch module workflows. Retain the
subsequent Release blockers and all existing required gates. Collect fresh
exact-delivery CI/Commitlint/CodeQL/G-6 logs and artifacts, including catalog
and Conversations receipts. This approval request does not claim those later
actions have run or that the full Release gate can yet pass.

The newer existing hosted run on the unchanged `85ff9795` source also confirms
the same two blockers: [CI 43958099038](https://github.com/Hexalith/Hexalith.Projects/actions/runs/43958099038).
Its build log reproduces the 14 missing-attribute errors; G-6 fails NU1102 for
story66.3 after catalog acquisition and Conversations preparation pass. The
retained G-6 artifact `14274021540` verifies against GitHub digest
`fdbff2e058d5cfe6734a37de575fe3ca773d9fde5d93c0bc42cebdd94f95229a`.
This observation is of uncorrected source, not hosted acceptance of the proposal.

[Machine-readable proposal](package-release-proposal.json) records the exact
approval perimeter and pending steps. [Final exact-package consumer results](consumer-fresh-artifact-20261116T215857Z/qualification.json)
and [McpCli evidence](mcp/README.md) retain the commands and limits.

Three independent review layers completed. Ten evidence/replay findings were
addressed: immutable output guards, nonzero failure exits, a fresh public cache,
pinned inspector0.27.0, reproducible provenance, strict whole-ZIP comparison,
structured diagnostics and packet-local byte attributes. Twenty focused helper
checks pass. The broad producer failure and final focused consumer run are both
retained; partial qualification never becomes full Release/G-6 acceptance.
