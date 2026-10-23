---
title: 'Complete five deferred supported-read verification items'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred verification obligations remain for supported Project reads: comparing actual legacy list/open HTTP responses, inspecting quarantined folderless history, checking Chatbot Folder visibility on both list and open, restricting Tenant descriptive metadata to authorized inspection without domain events, and routinely exercising the delegated Conversation advisory owner hop in package-mode CI.

**Approach:** Extend the existing server test project with deterministic HTTP and persisted-read fixtures that exercise these boundaries and the real delegated client. Assert authorized controls, protected failures, exact request routing and authority propagation, and unchanged persisted Project state. Close these five ledger entries with reproducible verification evidence, independently review, commit and push main as requested. These are local regression obligations; they do not accept production prerequisites, change qualification packets, schedule stories, or authorize cutover.

</frozen-after-approval>

## Implementation Notes

- Baseline: `18c646ab0b3fcabce1424794e160246c0b97f305`, captured directly from Git. Clean `main`; explicit push authorization. No unresolved intent gap. Small test-only footprint within the root repository plus this spec and five ledger resolutions; no product API, dependency, or submodule change is planned.
- Code map: `Queries/SupportedProjectReadHttpTests.cs` supplies signed HTTP hosting and SDK dispatch; make its class partial and put actual legacy GET comparison in a companion. `Queries/SupportedProjectReadFixture.cs` supplies deterministic persisted Project/tenant/Folder fixtures; a companion to `SupportedProjectReadTests` adds inspection and Chatbot controls. `DelegatedProjectDirectoryRegistrationTests.cs` and `DelegatedDirectoryTestQueryHandler.cs` demonstrate establishing a real SDK `VerifiedQueryCaller`; reuse this boundary for the actual `DelegatedProjectAdvisoryConversationDirectory` and published `DelegatedOwnerQueryClient` with a controlled HTTP transport.
- Architecture: AD-3 quarantines folderless history; AD-22 hides pre-activation work; AD-26 permits durable descriptive-inspection audit separately from domain history. Compare common authorized/current legacy fields explicitly: supported envelopes and access semantics intentionally differ. Retain the existing canonical-key, watermark and cursor shadow controls.
- Verification: build the individual server test project with Debug/source references and Release/package references, invoke its xUnit v3 assembly directly with class filters, run the complete package-mode server suite where available, and check Git whitespace plus commitlint. Routine CI already runs this test project. No live owner or production acceptance will be inferred from hermetic transport fixtures.

- Added 24 unconditional regression cases across the five obligations. Both builds pass with zero warnings/errors. Debug and Release focused suites each report 93 total, 92 passed, zero failures and one existing opt-in live Dapr persistence skip. The full Release/package server suite reports 1160 total, 1159 passed, zero failures and the same one skip.
- AppHost baseline: the exact root AppHost started and was inspected with `aspire describe`; identity parameters awaited input and dependent services were Waiting. The same AppHost was stopped successfully before builds. This proves startup inspection only; no ready live topology is claimed. Baseline output is retained at `/tmp/five-supported-read-aspire-baseline.json`.

- Final post-review verification supersedes the provisional counts above: 26 new unconditional regression cases; Release/package full server suite 1162 total, 1161 passed, zero failures, one existing live Dapr skip; Debug/source focused suite 95 total, 94 passed, zero failures, the same skip. Both final builds have zero warnings/errors. The independent reviewer confirmed all seven corrections and the narrowed evidence claims; no finding was deferred. The remote was fetched with zero divergence, and the conventional commit message passed commitlint.

## Reproducible Verification

Run from the repository root:

```bash
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -m:1 -p:NuGetAudit=false > /tmp/five-supported-read-release-build.log 2>&1
dotnet tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests.dll -noColor > /tmp/five-supported-read-release-suite.log 2>&1
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false > /tmp/five-supported-read-debug-build.log 2>&1
dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -class '*SupportedProjectReadTests' -class '*SupportedProjectReadHttpTests' -class '*SupportedProjectShadowTests' -class '*DelegatedProjectAdvisoryConversationDirectoryTests' -noColor > /tmp/five-supported-read-debug-focused.log 2>&1
git diff --check
```

The same focused filters were run against the Release assembly, with output in `/tmp/five-supported-read-release-focused.log`. Temporary logs are local developer checks, not qualification or acceptance artifacts.

## Review Triage Log

- BH1 — low, patched: the test name implied direct observation of event history. Renamed it to describe persisted Project state, and narrowed the ledger evidence. The executor has read/authorization/owner/cursor/audit dependencies and no event or command writer; this test does not measure a live event stream.
- BH2 — medium, patched: key count alone after retry could miss overwrites. Exact Project/index bytes and ETags are now asserted again after the retry.
- BH3 — medium, patched: populated description, setup metadata, goals, instructions and Folder label markers now accompany the name control. Safe list/open payloads must omit protected properties and marker values; authorized open must return the populated values.
- BH4 — medium, patched: the real HTTP comparison now includes a later setup update, populated setup metadata, an archived Project, and separate Active/Archived filter comparisons. Both update timestamps and aggregate versions are checked.
- BH5 — medium, patched: the outcome-filter tests alone could not establish actor propagation. Two additional real delegated Folder client cases query the same Folder under different original actors, assert the exact bound requester and signed proof, and observe Allowed versus Denied. These transport controls complement list/open filtering without claiming a live owner ACL qualification.
- BH6 — medium, patched: supported HTTP responses now undergo exact JSON round-trip comparison with their closed DTOs, including nested objects, before normalized comparison. Unknown payload properties cannot disappear silently during deserialization.
- BH7 — medium, patched: an incoming human bearer sentinel is now present; outbound owner requests must omit Authorization and the sentinel from headers and body. Workload assertions and signed originating evidence remain checked independently.
