---
title: 'Complete five deferred read cutover follow-ups'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_revision: '619593f87f9fdd67e0fc6f549cd0a4fd6cc83ab6'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Four reviewed Story 6.7 Conversation-start changes lack closure in the deferred-work ledger. The retained attachment resolver can still suggest a Project after its authorized Folder has changed.

**Approach:** Close DW-57, DW-59, DW-60 and DW-66 with focused checks of the retained HTTP headers and status contract, retryable denial handling, and supported Partial admission. Close the deferred attachment linkage item by requiring the authorized final Project Folder to match the discovered Folder reference and the final authorized attachment inputs. Preserve retained request and response contracts and use the existing authorization and deadline. Record all five resolutions in the ledger.

</frozen-after-approval>

## Implementation Notes

- Story 6.7 already landed the four Conversation-start changes. The legacy response header reflects the returned Setup, and its watermark is the authorized Project source position. DW-48 concerns a separate authoritative source-head and rebuild comparison and remains open.
- The attachment resolver will reuse the Project details from the existing final authorization pass. Its discovery index continues to supply match evidence and its current candidate and response limits still apply.

- The retained attachment path now binds each match to the final authorized detail's exact Tenant, Project and Included Folder. The discovery index still supplies the match state. The final authorized lifecycle governs archived candidates. The resolver reuses the existing bounded owner and authorization calls.
- The focused checks cover distinct Project and Tenant positions, stale-authority withholding, the retained OpenAPI status set and the generated client's handling of both handler and Gateway errors. The retryable-denial fixture exercises the initial and final checks with identifiable private Setup fields. The supported Partial fixture asserts the retained Setup contents and exact AD-32 omission evidence.
- During verification, main advanced to 170da3088e5a282c82c381f6f16383bcb5481ee5. The work was fast-forwarded onto that revision before the final builds.

## Review Triage Log

| Finding | Verdict | Resolution |
| --- | --- | --- |
| A detail change after the final authorization read can escape that snapshot | Medium, deferred | The authorization gate reads Project detail before its downstream validators. The resolver uses the snapshot observed in that final pass. The ledger tracks the remaining observation gap across independent stores. |
| Discovery lifecycle can defeat the archived filter | Medium, patched | Score with the final authorized detail lifecycle and cover archival after discovery. Retained candidate names are redacted. |
| Displaced stale reference can affect an otherwise valid match | Low, patched | The regression presents both Folders and includes a displaced stale match on the still-linked Project. |
| Retryable-denial test did not pin the error shape and Setup privacy | Low, patched | Both phases assert a retryable unavailable response, no freshness headers and no private Setup data. |
| Spine check alone does not cover generated client behavior | Low, patched | Client cases cover typed 404 and 503, and generic Gateway 401 and 403. |
| Partial-admission test only checked that Setup existed | Low, patched | Assert the goals, instructions and explicit excluded source kind alongside the Partial snapshot. |
| Ledger resolutions needed an executable verification record | Low, patched | The verification section records the commands and outcomes for the five items. |

## Verification

- Generated a Debug source mapping with `hexalith-module` 6.6.1 using `prepare-build --manifest module/hexalith.module-manifest.v2.json --mode source --workspace <scratch-path> --output json`. The mapper reported HXW009 and HXW011 for the existing Builds and Platform pins. It produced a source mapping successfully.
- Built each of `tests/Hexalith.Projects.{Server,Contracts,Client}.Tests/Hexalith.Projects.{Server,Contracts,Client}.Tests.csproj` with `dotnet build --configuration Debug -m:1 --disable-build-servers --artifacts-path <scratch-path> -p:MinVerVersionOverride=1.0.0` and the emitted mapping environment. Each build reported zero warnings and errors. Dependency audits stayed enabled.
- Ran the built Server.Tests assembly with xUnit `-class` filters for `GetConversationStartSetupTests`, `GetConversationStartSetupQueryHandlerTests`, `ResolveProjectFromAttachmentsTests` and `ResolveProjectFromConversationTests` in `Hexalith.Projects.Server.Tests.Queries`. All 189 selected cases passed.
- Ran the built Contracts.Tests and Client.Tests assemblies in full. Contracts passed 208 tests and Client passed 167. All three native XML summaries reported zero failures, errors, skips or unrun cases.
- `pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1`, `python3 tools/planning/validate_production_authority.py --validate-index`, `git diff --check` and commitlint passed. A byte comparison restored the five ledger resolutions and the new review entry to reproduce the original ledger exactly.
- The mapped AppHost built successfully with the existing Aspire obsolete-API warnings. `aspire run --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj --launch-profile http --isolated --detach --non-interactive --format Json` and the `dotnet run --no-build` retry stopped at the read-only Aspire backchannel path `/home/agent/.aspire/cli/bch`. Live AppHost verification was unavailable in this environment.
