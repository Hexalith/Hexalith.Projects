# McpCli package compatibility qualification

The configured public feed still publishes only Projects Contracts 1.0.0 and
1.2.0 through 1.2.4. The exact required 2.0.0-story66.3 download returns HTTP 404.
McpCli current remote main `ccbdd3e293697e0d38d429d5707ab67b4f1164a5` retains that
same pin; advancing its Projects gitlink cannot repair restore.

Published 1.2.4 carries ASP.NET Core, Fluxor, Fluent UI, FrontComposer and
Conversations dependencies and no McpCli module declaration. Its actual nuspec,
API inspection and source identity are retained. Selecting that older package
would violate the existing McpCli dependency boundary and lose enrollment.

The unchanged retained `Hexalith.Projects.Contracts` **2.0.0-story66.3** archive,
SHA256 `349f938db77dd83ff45e200997b1cba97147c46b1855ec58fd6e2a4b0572eff5`,
passes actual restore, Release compilation with warnings as errors, and strict
discovery of all four supported Projects operations against pinned McpCli
`5008de388c5e37257ca78e48d8afe899cf7aaa2a` and Builds
`c751d23aa4e73e8f7d74579db26a9fcf1df4c8e5`. The qualification used only an exact
package-ID mapping to a temporary local feed and changed no pin. This proves
compatibility, not public availability or hosted G-6 acceptance.

The archive binds Projects source `5b4bdeacde318a1d948db7ef76843df0c548f084` with
Builds `801f268cd1b37ecae41d4fb20d115c055b4c5793`. All 119 retained Contracts
source hashes match that commit. A fresh committed-source rebuild on its
historical main branch yields byte-identical nuspec and README, plus identical
CLR metadata, method bodies, contract attributes, references and resources.
Six DLL difference ranges are confined to PE/PDB reproducibility metadata:
timestamps, MVID, CodeView PDB identity/path and PDB checksum. The parsed
normalization proof and retained rebuilt archive distinguish these facts from
full DLL or ZIP byte reproduction. The initial detached-HEAD mismatch is also
retained. This source predates current query-result metadata; no current source
or metadata was removed or changed to construct it.

The smallest release proposal is package-owner publication of that exact
retained coordinate and archive to the already configured nuget.org feed.
No McpCli source/pin change is needed. The Projects release maintainer owns
publication under DW-82, with McpCli enrollment coordination. The existing
signer-capacity and live-qualification obligations stay visible; this packet
does not repeat A8, approve publication or grant runtime acceptance.

Receipts: `conclusion.json`, `historical-candidate-qualification.json`,
`historical-source-provenance.json`, `historical-exact-source-rebuild.json`, and
`historical-payload-equivalence.json`. Scripts are replayable with
`run-with-temp`; all disposable children completed before their workspaces
were removed.
